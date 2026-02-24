# Code Review — `feature/social-login-google`

**Compared against:** `feature/modernize`
**Date:** 2026-02-19
**Scope:** Clean Architecture compliance · .NET 8 / C# 12 · Web Security

**Files reviewed:** `ExternalAuthController`, `ExternalAuthService`, `ExternalAuthCodeService`,
`AuthEventsLog`, `CookieExtensions`, `IdentityServiceExtensions`, DTOs, interfaces

---

## Summary

| Category | Critical | Medium | Low / Info |
|---|---|---|---|
| Clean Architecture | — | 2 | 2 |
| .NET 8 / C# 12 | — | 1 | 3 |
| Security | — | 2 | 3 |

No critical issues. The implementation follows the BFF pattern correctly with solid security foundations
(HttpOnly cookies, short-lived single-use codes, token rotation, stolen-token detection).
The most impactful items to address are: moving interfaces to `Core/`, fixing the `IDistributedCache`
TOCTOU race, propagating `CancellationToken`, and verifying Angular `withCredentials`.

---

## 1. Clean Architecture

### MEDIUM — Interfaces co-located with implementations in `Api/Identity/`

`IExternalAuthService` and `IExternalAuthCodeService` are defined in the same folder as their
implementations, both living in the `Api` (Presentation) layer. The project's established pattern
places interfaces in `Core/Interfaces/` and implementations in `Services/`. Pre-existing services
(`TokenService`, `RefreshTokenService`) already violate this, but new code should not deepen the debt.

**Recommended move:**
- `IExternalAuthService` + `ExternalAuthResult` → `Core/Interfaces/`
- `IExternalAuthCodeService` → `Core/Interfaces/`
- Implementations remain in `Api/Identity/` (or move to `Services/` for full compliance)

**Resolution (partial):**
- `IExternalAuthCodeService` → moved to `Core/Interfaces/` ✓
- `ExternalAuthResult` → moved to `Core/Entities/Identity/` ✓
- `IExternalAuthService` → **remains in `Api/Identity/`** (known deviation — see below)

**Known deviation — `IExternalAuthService` stays in `Api/Identity/`:**
`IExternalAuthService` depends on `ExternalLoginInfo` from `Microsoft.AspNetCore.Identity`.
That type is part of the ASP.NET Core framework package, which is not available in the `Core`
project's `netstandard2.0` target. Moving the interface to `Core/Interfaces/` would require
either adding an ASP.NET Core framework dependency to the domain layer (violating Clean
Architecture) or wrapping `ExternalLoginInfo` in a domain abstraction (significant refactor).
The interface is kept in `Api/Identity/` as a deliberate pragmatic choice. If `Core` is ever
retargeted to `net8.0`, this interface should be moved to `Core/Interfaces/`.

### MEDIUM — `ExternalAuthResult` is a Presentation-layer type

`ExternalAuthResult` is defined inside `Api/Identity/IExternalAuthService.cs`. It wraps an `AppUser`
and is a domain result type — it belongs in `Core/` alongside `AppUser`.

### LOW — DRY violation: token + cookie orchestration duplicated across controllers

`ExchangeCode` in `ExternalAuthController` contains:

```csharp
var (accessToken, jwtId) = _tokenService.CreateToken(user);
var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user, jwtId);
Response.SetRefreshTokenCookie(refreshToken, _tokenSettings);
```

This is identical to the private `GenerateAuthResponseAsync` helper in `AccountController`.
The logic should move into a shared application service (e.g. extend `IAuthenticationServices`
or introduce a dedicated `ILoginResponseService`).

### LOW — Unused constructor parameter

`ExternalAuthController` injects `IWebHostEnvironment environment` (constructor line 29) but never
uses it. This produces a CS9113 compiler warning. Remove the parameter.

---

## 2. .NET 8 / C# 12

### MEDIUM — `CancellationToken` not propagated to `IDistributedCache`

`ExternalAuthCodeService` makes three cache calls without passing a cancellation token:

```csharp
await cache.GetStringAsync(cacheKey);        // no CancellationToken
await cache.RemoveAsync(cacheKey);           // no CancellationToken
await cache.SetStringAsync(..., options);    // no CancellationToken
```

All `IDistributedCache` methods accept `CancellationToken`. Pass `HttpContext.RequestAborted`
through the service interface to allow clean cancellation on client disconnect.

**Resolution:**
- `CancellationToken cancellationToken = default` added to both `IExternalAuthCodeService` methods ✓
- All three cache calls now pass `cancellationToken` ✓
- `ExternalAuthController` passes `HttpContext.RequestAborted` at both call sites ✓

### LOW — `ExternalAuthResult` should be a `record`

It is an immutable result container with no behaviour. C# 12 positional records are the idiomatic
choice:

```csharp
// Replace the current class + static factory methods with:
public record ExternalAuthResult(
    bool Succeeded,
    AppUser? User = null,
    string? Error = null,
    bool IsNewUser = false,
    bool WasLinked = false);
```

**Known deviation — `ExternalAuthResult` stays a class:**
`record` types (and `init`-only setters) require `System.Runtime.CompilerServices.IsExternalInit`,
which is not part of `netstandard2.0`. Using records in the `Core` project requires either adding
an `IsExternalInit` polyfill or retargeting `Core` to `net8.0`. Neither change is warranted for
this feature branch. If `Core` is ever retargeted to `net8.0`, convert `ExternalAuthResult` to a
positional record at that time.

### LOW — Missing nullable annotations

The following members lack `?` annotations, preventing the nullable analyzer from catching null
dereferences at call sites:

| Member | Current | Should be |
|---|---|---|
| `ExternalAuthResult.User` | `AppUser` | `AppUser?` |
| `ExternalAuthResult.Error` | `string` | `string?` |
| `ValidateAndConsumeCodeAsync` return | `Task<AppUser>` | `Task<AppUser?>` |

**Known deviation — nullable annotations deferred:**
Both `Core` (`netstandard2.0`) and `Api` (`<Nullable>disable</Nullable>`) have nullable analysis
disabled. Enabling it project-wide would produce warnings across all existing files. Annotations
are deferred until nullable is enabled solution-wide.

### LOW — `DateTime` instead of `DateTimeOffset` in `AuthCodeData`

`AuthCodeData.CreatedAt` uses `DateTime`. `DateTimeOffset` is the .NET best practice for timestamps —
it avoids timezone ambiguity and is recommended by the framework team. Although `CreatedAt` is not
currently used for expiry logic (Redis TTL handles that), aligning to the standard prevents future
bugs if the field is ever used for comparison.

**Resolution:** `AuthCodeData.CreatedAt` changed to `DateTimeOffset`; `ExternalAuthCodeService`
now assigns `DateTimeOffset.UtcNow` ✓

---

## 3. Web Security

### MEDIUM — TOCTOU race condition in code exchange

`ExternalAuthCodeService.ValidateAndConsumeCodeAsync` performs a non-atomic read-then-delete:

```csharp
var cachedValue = await cache.GetStringAsync(cacheKey);  // read
// ... async gap — another request can read the same value here
await cache.RemoveAsync(cacheKey);                        // delete
```

Two concurrent requests with the same code can both pass the null check before deletion runs.
`IDistributedCache` does not expose an atomic `GETDEL`. Recommended mitigations (in order of
preference):

1. Inject `IConnectionMultiplexer` directly alongside `IDistributedCache` and call
   `database.StringGetDeleteAsync(key)` which maps to Redis `GETDEL` (atomic).
2. Wrap the get+delete in a Lua script executed via `ScriptEvaluateAsync`.
3. Use a distributed lock (e.g. RedLock.net) around the operation.

The 60-second TTL and the expectation of a single valid code per login attempt reduce practical
exploitability, but it is a real race under concurrent load.

**Resolution:**
`IDistributedCache` was replaced entirely with `IConnectionMultiplexer` (option 1). The service
now calls `IDatabase.StringGetDeleteAsync(key)` which maps directly to Redis `GETDEL` — a single
atomic command. Side effects:
- `AddStackExchangeRedisCache` registration removed from `Startup.cs` (no longer needed)
- `IDistributedCache` is no longer a dependency of `ExternalAuthCodeService`
- Key format is now fully controlled by the service (no hidden instance-name prefix)
- Note: StackExchange.Redis 2.10.1 does not accept `CancellationToken` on `IDatabase` methods;
  the token is accepted in the interface signature but cannot be forwarded to Redis calls ✓

### MEDIUM — No max-length validation on `ExternalAuthCodeExchangeDto.Code`

Generated codes are always 43 characters (32 bytes base64url, no padding). An attacker can submit
arbitrarily long strings, which are passed directly into a Redis key lookup. Add a defensive length
constraint:

```csharp
[Required]
[MaxLength(64)]
public string Code { get; set; } = string.Empty;
```

**Resolution:** `[MaxLength(64)]` added to `ExternalAuthCodeExchangeDto.Code` ✓

### LOW — Angular `withCredentials` must be verified on `exchangeAuthCode`

Angular (`:4200`) and the API (`:44370`) are different origins. The refresh token cookie set by
`POST /api/externalauth/exchange` will only be stored by the browser if the HTTP request includes
`withCredentials: true`. If this option is absent, the cookie is silently dropped and every
subsequent `POST /api/account/refresh` after a Google login will return 401.

Verify that `account.service.ts → exchangeAuthCode()` passes `{ withCredentials: true }` (or that
the Angular HTTP interceptor adds it for all API requests).

**Resolution:** `account.service.ts → exchangeAuthCode()` explicitly passes `{ withCredentials: true }`
(line 149). `jwt.interceptor.ts` lists `/externalauth/exchange` in `isAuthEndpoint()` so it correctly
skips token injection for that call; cookie handling is fully owned by the service method itself ✓

### INFO — Auth code exposed as query parameter

`GoogleCallback` redirects to Angular as `returnUrl?code=xxx`. The code appears in browser history,
server access logs, and outgoing `Referer` headers. This is standard OAuth authorization code
behaviour and is fully mitigated by the 60-second TTL and single-use enforcement. Document this in
the threat model for completeness.

**Resolution:** `GoogleAuthFlow.md` already documents this explicitly — "Redirect URLs are visible
in browser history, server logs, and the Referer header" — and explains the BFF code-exchange
pattern as the mitigation (tokens are never placed in URLs). Single-use guarantee is documented
under Security Measures ✓

### INFO — `AllowedReturnUrlHosts` defaults to `["localhost"]` only

The default value is safe for production (an unlisted host falls through to `return false`, causing
the redirect to fall back to `/`). However, Google login will silently fail in any non-localhost
environment where this config is not explicitly set, making it a subtle misconfiguration trap.

**Recommendation:** Add a startup validation that logs a warning when `AllowedReturnUrlHosts`
still contains only `"localhost"` in a non-Development environment.

**Resolution:** `Startup.ConfigureApp()` now checks `IOptions<GoogleAuthSettings>` on startup.
When the environment is not Development and `AllowedReturnUrlHosts` is still `["localhost"]`,
a `LogWarning` is emitted naming the correct `appsettings.{EnvironmentName}.json` file ✓

---

## Fixed issues (applied in this branch)

The following findings from the REST API design review were already resolved:

| Issue | Resolution |
|---|---|
| Refresh token cookie set before code exchange (orphaned cookie risk) | Cookie now set atomically inside `POST /exchange` |
| Missing `[ProducesResponseType(400)]` on `ExchangeCode` | Added |
| Spurious access token generation in `GET /providers` | Removed; `Token` dropped from `UserWithExternalLoginsDto` |
| Browser-redirect endpoints (`GET /google`, `GET /google/link`) visible in Swagger as JSON | `[ApiExplorerSettings(IgnoreApi = true)]` added |
