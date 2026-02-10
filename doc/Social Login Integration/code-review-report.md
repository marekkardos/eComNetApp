# API Code Review

## Summary

Review of changes in the `feature/social-login-google` branch across 5 files: `ExternalAuthController.cs`, `IdentityServiceExtensions.cs`, `appsettings.Development.json`, `Directory.Packages.props`, and `docker-compose.yml`. The changes fix Google OAuth social login (CallbackPath, Data Protection package mismatch, SignInScheme) and add cookie authentication schemes required for the external login flow.

---

## Critical Issues

### 1. Dead code: commented-out diagnostic block (~90 lines)
**File:** `IdentityServiceExtensions.cs:112-206`
**Severity:** Critical (code hygiene)

The entire `OAuthEvents` diagnostic block is commented out. This is ~90 lines of debugging scaffolding that should be removed before merging. It clutters the file, makes it harder to read, and could confuse future maintainers.

**Action:** Delete lines 112-206 entirely.

### 2. Unused cookie scheme registered
**File:** `IdentityServiceExtensions.cs:72-77`

The **default** `CookieAuthenticationDefaults.AuthenticationScheme` ("Cookies") is registered but never used as a `SignInScheme` (the Google handler uses `IdentityConstants.ExternalScheme`). The only reference is the `SignOutAsync` in the controller (line 98), which signs out of a scheme that the Google flow never signed into.

```csharp
.AddCookie(options =>  // "Cookies" scheme - not used by Google OAuth
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
})
```

This adds an unnecessary authentication handler to the pipeline. If it's not needed for a future feature, remove it.

### 3. SignOutAsync targets wrong scheme
**File:** `ExternalAuthController.cs:97-98`

```csharp
await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
```

The Google OAuth flow signs in to `IdentityConstants.ExternalScheme`, not `CookieAuthenticationDefaults.AuthenticationScheme`. The cleanup should sign out of the scheme that was actually used:

```csharp
await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
```

### 4. Data Protection keys volume commented out
**File:** `docker-compose.yml:18-19`

```yaml
# volumes:
#   - .data/dataprotection-keys:/app/dataprotection-keys
```

Without persisted Data Protection keys, every container restart generates new keys. This means:
- Active OAuth flows in progress will fail (state can't be decrypted)
- Any Data Protection-encrypted data becomes unreadable after restart

This should be re-enabled for any environment beyond throwaway local testing.

### 5. Commented-out `SignInScheme` alternative left in code
**File:** `IdentityServiceExtensions.cs:89`

```csharp
// options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
options.SignInScheme = IdentityConstants.ExternalScheme;
```

Remove the commented-out alternative. It adds confusion about which scheme is intended.

---

## Improvements

### 1. Missing `HttpOnly` flag on the External scheme cookie
**File:** `IdentityServiceExtensions.cs:79-82`

The `Identity.External` cookie should explicitly set `HttpOnly = true`, `Secure`, and `SameSite` to match the security posture of the rest of the application:

```csharp
.AddCookie(IdentityConstants.ExternalScheme, options =>
{
    options.Cookie.Name = IdentityConstants.ExternalScheme;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
})
```

### 2. Debug logging levels left in appsettings
**File:** `appsettings.Development.json`

```json
"Microsoft.AspNetCore.DataProtection": "Debug",
"Microsoft.AspNetCore.Authentication": "Debug"
```

These are very verbose and were added for debugging the OAuth issue. Consider downgrading to `"Information"` now that the issue is resolved, or remove them entirely. `Debug` level for Authentication will log sensitive token/claim details.

### 3. Unused `using` directives
**File:** `IdentityServiceExtensions.cs`

These usings were added for the diagnostic code and are no longer needed if the diagnostic block is removed:
- `Microsoft.AspNetCore.Authentication.OAuth`
- `Microsoft.AspNetCore.DataProtection`
- `Microsoft.AspNetCore.WebUtilities`

### 4. Comment numbering mismatch
**File:** `ExternalAuthController.cs:97`

```csharp
// 2. Clean up the temporary cookie
```

There is no "1." comment preceding this. Either add the full numbered sequence or remove the number.

### 5. `GoogleLinkCallback` should also clean up the external cookie
**File:** `ExternalAuthController.cs:157-182`

`GoogleCallback` (line 98) signs out of the temporary cookie, but `GoogleLinkCallback` does not. The same external cookie is set during the linking flow and should be cleaned up.

---

## Architecture Notes

- The two-cookie setup (`CookieAuthenticationDefaults.AuthenticationScheme` + `IdentityConstants.ExternalScheme`) is one cookie more than needed. Since `options.SignInScheme = IdentityConstants.ExternalScheme`, only the `IdentityConstants.ExternalScheme` cookie is required for the OAuth flow. Removing the default "Cookies" scheme simplifies the authentication pipeline.
- The `Microsoft.Extensions.Configuration.Abstractions` package remains at **10.0.1** in a .NET 8 project. While it doesn't cause the `CryptoUtil` crash (that was `Identity.Stores`), mixing .NET 10 extension packages with a .NET 8 runtime is fragile and could surface similar `MissingMethodException` issues in other areas. Consider aligning it to 8.x if transitive dependency resolution allows.

---

## Skills Applied
- `security-web.md` — Cookie flags, CSRF/SameSite, HTTPS enforcement, secrets in logs
- `api-design.md` — Endpoint structure, response consistency
- `dotnet.md` — Async patterns, unused usings, modern C# conventions
