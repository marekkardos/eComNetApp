# Google OAuth Social Login Implementation Plan

## Overview

Implement Google OAuth social login using the BFF (Backend-for-Frontend) pattern with code exchange for the eComNetApp.

**Status:** Ready for Implementation
**Risk Level:** Low - BFF pattern is standard, infrastructure is in place
**Last Updated:** 2026-02-11

---

## Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| OAuth Flow | BFF - all OAuth logic on backend | Security best practice, protects client secret |
| Token Delivery | Code exchange (short-lived code → access token) | Prevents token exposure in browser history |
| Auto-link | Trust Google's email verification | Google verifies emails, safe to auto-link accounts |
| External Login Storage | Built-in `AspNetUserLogins` table (already exists) | Uses ASP.NET Core Identity standard |
| Code Storage | Redis with 60s TTL, single-use | Fast, distributed, automatic expiration |

---

## Authentication Flow

```
Angular                        .NET API                      Google
   |                              |                             |
   |---(1) Redirect /api/externalauth/google?returnUrl=...      |
   |                              |---(2) Redirect to Google--->|
   |                              |<--(3) Auth code-------------|
   |                              |---(4) Exchange for tokens-->|
   |                              |<--(5) ID token + user info--|
   |                              |---(6) Find/create user, set refresh cookie
   |<--(7) Redirect returnUrl?code=xyz (short-lived)            |
   |---(8) POST /api/externalauth/exchange { code }------------>|
   |<--(9) { accessToken } (refresh cookie already set)---------|
```

### Flow Details

1. **User clicks "Sign in with Google"** - Angular redirects to `/api/externalauth/google?returnUrl=/dashboard`
2. **API redirects to Google** - Google OAuth middleware handles OAuth protocol
3. **Google authenticates user** - User approves app, Google sends auth code to callback
4. **API receives callback** - Google middleware exchanges code for ID token
5. **API processes user** - `ExternalAuthService` finds or creates user account
6. **API sets refresh cookie** - HttpOnly, Secure cookie for session management
7. **API redirects to Angular** - Short-lived code (60s) passed as query param
8. **Angular exchanges code** - POST to `/api/externalauth/exchange` with code
9. **API returns access token** - JWT access token for API calls

---

## User Scenarios

| Scenario | Action | Expected Behavior |
|----------|--------|-------------------|
| New Google user | Click Google login, complete auth | Create account with Google info, link Google ID, log in |
| Existing user (same email) | Google login with registered email | Auto-link Google to existing account (email verified by Google) |
| Logged-in user linking | Navigate to settings, click "Link Google" | `/api/externalauth/google/link` endpoint, links account |
| User unlinking | Settings → Unlink Google | DELETE `/api/externalauth/google/unlink` (requires password or other login method) |
| Unlink only login method | Try to unlink Google when it's the only way to log in | Block with error message "You must have a password or another login method" |
| Google account already linked | Login with Google account linked to different user | Block with error "This Google account is already linked to another user" |

---

## Implementation Steps

### Phase 1: Backend Package & Configuration

#### 1. Add Package Reference

**File: `Directory.Packages.props`**
```xml
<PackageVersion Include="Microsoft.AspNetCore.Authentication.Google" Version="8.0.22" />
```

**File: `Api/Api.csproj`**
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.Google" />
```

> **Note:** Verify latest compatible version before implementation. Should match other auth packages (currently 8.0.22).

#### 2. Create Configuration Class

**File: `Api/Identity/GoogleAuthSettings.cs`**
```csharp
public class GoogleAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public int AuthCodeExpirationSeconds { get; set; } = 60;
    public string[] AllowedReturnUrlHosts { get; set; } = ["localhost"];
}
```

#### 3. Update Configuration Files

**File: `Api/appsettings.Development.json`**
```json
{
  "GoogleAuth": {
    "ClientId": "placeholder-use-user-secrets",
    "ClientSecret": "placeholder-use-user-secrets",
    "AuthCodeExpirationSeconds": 60,
    "AllowedReturnUrlHosts": ["localhost"]
  }
}
```

**User Secrets (for development):**
```bash
dotnet user-secrets set "GoogleAuth:ClientId" "your-client-id"
dotnet user-secrets set "GoogleAuth:ClientSecret" "your-client-secret"
```

---

### Phase 2: Backend Services

#### 4. Create DTOs

**File: `Api/Dtos/ExternalAuthCodeExchangeDto.cs`**
```csharp
public class ExternalAuthCodeExchangeDto
{
    [Required]
    public string Code { get; set; } = string.Empty;
}
```

**File: `Api/Dtos/ExternalLoginInfoDto.cs`**
```csharp
public class ExternalLoginInfoDto
{
    public string Provider { get; set; } = string.Empty;
    public string ProviderDisplayName { get; set; } = string.Empty;
    public bool IsLinked { get; set; }
}
```

**File: `Api/Dtos/UserWithExternalLoginsDto.cs`**
```csharp
public class UserWithExternalLoginsDto : UserDto
{
    public List<ExternalLoginInfoDto> ExternalLogins { get; set; } = new();
    public bool HasPassword { get; set; }
}
```

#### 5. Create ExternalAuthCodeService

**File: `Api/Identity/IExternalAuthCodeService.cs`**
```csharp
public interface IExternalAuthCodeService
{
    Task<string> GenerateCodeAsync(ApplicationUser user);
    Task<ApplicationUser?> ValidateAndConsumeCodeAsync(string code);
}
```

**File: `Api/Identity/ExternalAuthCodeService.cs`**
- Redis-backed implementation
- Single-use codes (delete after validation)
- 60-second TTL (configurable via `GoogleAuthSettings.AuthCodeExpirationSeconds`)
- Cryptographically secure code generation
- Key format: `external-auth-code:{code}`
- Value: UserId

#### 6. Create ExternalAuthService

**File: `Api/Identity/IExternalAuthService.cs`**
```csharp
public interface IExternalAuthService
{
    Task<(ApplicationUser user, bool isNewUser)> FindOrCreateUserAsync(ExternalLoginInfo loginInfo);
    Task LinkExternalLoginAsync(ApplicationUser user, ExternalLoginInfo loginInfo);
    Task UnlinkExternalLoginAsync(ApplicationUser user, string loginProvider, string providerKey);
    Task<List<ExternalLoginInfoDto>> GetExternalLoginsAsync(ApplicationUser user);
}
```

**File: `Api/Identity/ExternalAuthService.cs`**

**Key Logic:**
- **FindOrCreateUserAsync:**
  - Check if external login exists → return existing user
  - Check if email exists → auto-link (Google verifies email) → return user
  - Create new user with Google info (DisplayName from `name` claim, fallback to email prefix)
- **LinkExternalLoginAsync:**
  - Verify user is authenticated
  - Check if Google account already linked to different user → throw exception
  - Add external login via `UserManager.AddLoginAsync()`
- **UnlinkExternalLoginAsync:**
  - Check user has password or other login method → throw exception if not
  - Remove external login via `UserManager.RemoveLoginAsync()`

#### 7. Update AuthEventsLog

**File: `Api/Identity/IAuthEventsLog.cs`**
```csharp
void Monitor_ExternalLogin(string userId, string provider, string email);
void Monitor_ExternalAccountLinked(string userId, string provider);
void Monitor_ExternalAccountUnlinked(string userId, string provider);
```

**File: `Api/Identity/AuthEventsLog.cs`**
- Implement the three methods above
- Log to configured logging provider

---

### Phase 3: Backend Infrastructure

#### 8. Modify IdentityServiceExtensions.cs

**File: `Api/StartupConfigurations/IdentityServiceExtensions.cs`**

**Changes:**
- Add `.AddGoogle()` after `.AddJwtBearer()` configuration
- Configure Google options:
  - `ClientId` and `ClientSecret` from `GoogleAuthSettings`
  - Scopes: `"email"`, `"profile"`
  - `SaveTokens = true` (to access tokens in callback)
- Handle redirect events for API routes (prevent default UI redirects)
- Set correlation cookie path to `/api` (not `/api/account`)

**Example:**
```csharp
services.AddAuthentication()
    .AddJwtBearer(/* existing config */)
    .AddGoogle(options =>
    {
        var googleSettings = configuration.GetSection("GoogleAuth").Get<GoogleAuthSettings>();
        options.ClientId = googleSettings.ClientId;
        options.ClientSecret = googleSettings.ClientSecret;
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.SaveTokens = true;

        // Prevent automatic redirects for API routes
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect($"{context.Properties.Items["returnUrl"]}?error=access_denied");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });
```

#### 9. Modify Startup.cs

**File: `Api/Startup.cs`**

**Changes:**
- Register `GoogleAuthSettings` configuration
- Register `IExternalAuthService` and implementation (scoped)
- Register `IExternalAuthCodeService` and implementation (scoped)
- Add `AddStackExchangeRedisCache` for distributed cache

**Example:**
```csharp
services.Configure<GoogleAuthSettings>(Configuration.GetSection("GoogleAuth"));
services.AddScoped<IExternalAuthService, ExternalAuthService>();
services.AddScoped<IExternalAuthCodeService, ExternalAuthCodeService>();

// Add distributed cache for auth codes
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = Configuration.GetConnectionString("Redis");
    options.InstanceName = "ExternalAuth_";
});
```

> **Note:** Existing Redis `IConnectionMultiplexer` singleton can be reused, but `AddStackExchangeRedisCache` provides cleaner abstraction.

#### 10. Modify CookieExtensions.cs

**File: `Api/Extensions/CookieExtensions.cs`**

**Change (line 17):**
```csharp
// OLD:
Path = "/api/account",

// NEW:
Path = "/api",
```

> **⚠️ Breaking Change:** This will invalidate existing refresh tokens. Users will need to re-login once. Accept this one-time disruption or implement a migration strategy.

---

### Phase 4: Backend Controller

#### 11. Create ExternalAuthController

**File: `Api/Controllers/ExternalAuthController.cs`**

**Endpoints:**

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/externalauth/google` | Anonymous | Initiates OAuth flow, redirects to Google |
| GET | `/api/externalauth/google/callback` | Anonymous | OAuth callback, creates code, redirects to Angular |
| POST | `/api/externalauth/exchange` | Anonymous | Exchanges code for access token |
| GET | `/api/externalauth/google/link` | Authenticated | Initiates linking (for logged-in users) |
| GET | `/api/externalauth/google/link/callback` | Authenticated | Link callback |
| DELETE | `/api/externalauth/google/unlink` | Authenticated | Unlinks Google account |
| GET | `/api/externalauth/providers` | Authenticated | Lists linked external providers |

**Key Implementation Details:**

- **Return URL Validation:** Check `returnUrl` against `GoogleAuthSettings.AllowedReturnUrlHosts`
- **State Parameter:** ASP.NET Core Google middleware handles state automatically (stored in correlation cookie)
- **Error Handling:** Redirect to `returnUrl?error={code}&error_description={message}`
- **Cookie Setting:** Use `CookieExtensions.AddRefreshTokenCookie()` after successful authentication

---

### Phase 5: Angular Frontend

#### 12. Update JwtInterceptor

**File: `src/app/core/interceptors/jwt.interceptor.ts`**

**Change (line 78):**
```typescript
// Add exchange endpoint to excluded list
const authEndpoints = [
  '/account/login',
  '/account/register',
  '/account/refresh',
  '/account/logout',
  '/externalauth/exchange'  // ADD THIS
];
```

> **Correction from validation:** This was missing from original plan.

#### 13. Update AccountService

**File: `src/app/account/account.service.ts`**

**New Methods:**
```typescript
initiateGoogleLogin(returnUrl: string): void {
  window.location.href = `/api/externalauth/google?returnUrl=${encodeURIComponent(returnUrl)}`;
}

exchangeAuthCode(code: string): Observable<IUser> {
  return this.http.post<{ accessToken: string }>('/api/externalauth/exchange', { code })
    .pipe(
      tap(response => this.handleAuthSuccess(response.accessToken)),
      switchMap(() => this.getCurrentUser())
    );
}

getExternalLogins(): Observable<IExternalLoginInfo[]> {
  return this.http.get<IExternalLoginInfo[]>('/api/externalauth/providers');
}

initiateGoogleLink(returnUrl: string): void {
  window.location.href = `/api/externalauth/google/link?returnUrl=${encodeURIComponent(returnUrl)}`;
}

unlinkGoogle(): Observable<void> {
  return this.http.delete<void>('/api/externalauth/google/unlink');
}
```

#### 14. Update LoginComponent

**File: `src/app/account/login/login.component.ts`**

**Changes:**
- Add method to handle Google login button click
- Add `ngOnInit()` to check for OAuth callback (query params: `code`, `error`, `error_description`)
- If `code` exists, call `accountService.exchangeAuthCode(code)`
- If `error` exists, display error message

**File: `src/app/account/login/login.component.html`**

**Changes:**
- Add "Sign in with Google" button
- Style with Google branding guidelines
- Show error message if OAuth fails

#### 15. Update User Interfaces

**File: `src/app/shared/models/user.ts`**

**Add:**
```typescript
export interface IExternalLoginInfo {
  provider: string;
  providerDisplayName: string;
  isLinked: boolean;
}

export interface IUserWithExternalLogins extends IUser {
  externalLogins: IExternalLoginInfo[];
  hasPassword: boolean;
}
```

#### 16. Create ExternalLoginsComponent (Optional for MVP)

**File: `src/app/account/external-logins/` (new component)**

**Purpose:** User settings page for managing linked accounts

**Features:**
- Display list of linked external providers
- "Link Google" button (if not linked)
- "Unlink" button (if linked)
- Warning if trying to unlink only login method
- Success/error messages

---

## Files Summary

### New Files to Create (9 backend + 1 frontend)

| File | Purpose |
|------|---------|
| `Api/Identity/GoogleAuthSettings.cs` | Configuration POCO |
| `Api/Identity/IExternalAuthCodeService.cs` | Code service interface |
| `Api/Identity/ExternalAuthCodeService.cs` | Redis-backed code service |
| `Api/Identity/IExternalAuthService.cs` | External auth orchestration interface |
| `Api/Identity/ExternalAuthService.cs` | Find/create/link user logic |
| `Api/Controllers/ExternalAuthController.cs` | OAuth endpoints |
| `Api/Dtos/ExternalAuthCodeExchangeDto.cs` | Code exchange request DTO |
| `Api/Dtos/ExternalLoginInfoDto.cs` | Provider info DTO |
| `Api/Dtos/UserWithExternalLoginsDto.cs` | User + providers response DTO |
| `src/app/account/external-logins/` | Angular component for managing linked accounts |

### Files to Modify (5 backend + 4 frontend)

| File | Changes | Validated |
|------|---------|-----------|
| `Api/Api.csproj` | Add Google auth package reference | ✅ |
| `Directory.Packages.props` | Add package version | ✅ |
| `Api/StartupConfigurations/IdentityServiceExtensions.cs` | Add Google OAuth config | ✅ |
| `Api/Startup.cs` | Register services, distributed cache | ✅ |
| `Api/Extensions/CookieExtensions.cs` | Broaden cookie path to `/api` | ✅ |
| `Api/Identity/IAuthEventsLog.cs` | Add external auth event methods | ✅ |
| `Api/Identity/AuthEventsLog.cs` | Implement external auth events | ✅ |
| `Api/appsettings.Development.json` | Add GoogleAuth section | ✅ |
| `src/app/core/interceptors/jwt.interceptor.ts` | Exclude exchange endpoint | ✅ |
| `src/app/account/account.service.ts` | Add Google OAuth methods | ✅ |
| `src/app/account/login/login.component.ts` | Google button, callback handling | ✅ |
| `src/app/account/login/login.component.html` | Google button UI | ✅ |
| `src/app/shared/models/user.ts` | Add external login interfaces | ✅ |

---

## Security Considerations

### Authentication & Authorization

- **Return URL Validation:** Whitelist allowed hosts in `GoogleAuthSettings.AllowedReturnUrlHosts` to prevent open redirect vulnerabilities
- **State Parameter:** Handled automatically by ASP.NET Core Google middleware (CSRF protection via correlation cookie)
- **Auth Code:** Single-use, 60-second TTL, stored in Redis, cryptographically secure random generation
- **Cookie Security:** HttpOnly (prevents XSS), Secure flag in production (HTTPS only), SameSite=Strict (CSRF protection)

### Data Protection

- **Email Verification:** Trust Google's email verification (Google confirms email ownership)
- **Auto-linking:** Safe because Google verifies emails, preventing account takeover
- **Explicit Linking:** Requires authenticated session, prevents unauthorized linking

### Edge Cases

- **Prevent Unlink of Only Login Method:** Check if user has password or other external logins before allowing unlink
- **Prevent Duplicate Links:** Check if Google account already linked to different user
- **Token Storage:** Access token in memory only, refresh token in HttpOnly cookie

---

## Google Cloud Console Setup

Follow these steps to create OAuth credentials:

1. **Create Project**
   - Navigate to https://console.cloud.google.com/
   - Create new project or select existing

2. **Configure OAuth Consent Screen**
   - Go to "APIs & Services" → "OAuth consent screen"
   - Choose "External" user type
   - Add app name, support email, developer email
   - Add scopes: `email`, `profile`
   - Add test users (for development)

3. **Create OAuth 2.0 Credentials**
   - Go to "APIs & Services" → "Credentials"
   - Click "Create Credentials" → "OAuth client ID"
   - Application type: "Web application"
   - Name: "eComNetApp"

4. **Add Authorized Redirect URIs**
   - Development:
     - `http://localhost:44369/api/externalauth/google/callback`
     - `http://localhost:44369/api/externalauth/google/link/callback`
   - Production:
     - `https://yourdomain.com/api/externalauth/google/callback`
     - `https://yourdomain.com/api/externalauth/google/link/callback`

5. **Copy Credentials**
   - Copy Client ID and Client Secret
   - Add to user-secrets (development) or secure configuration (production)

6. **Enable Google+ API**
   - Go to "APIs & Services" → "Library"
   - Search for "Google+ API"
   - Click "Enable"

---

## Implementation Order (Recommended)

### Phase 1: Backend Package & Config (30 min)
1. Add package to `Directory.Packages.props` and `Api.csproj`
2. Create `GoogleAuthSettings.cs`
3. Add config section to appsettings
4. Set user-secrets

### Phase 2: Backend Services (2-3 hours)
5. Create DTOs (15 min)
6. Create `IExternalAuthCodeService` + `ExternalAuthCodeService` (1 hour)
7. Create `IExternalAuthService` + `ExternalAuthService` (1.5 hours)
8. Update `IAuthEventsLog` + `AuthEventsLog` (15 min)

### Phase 3: Backend Infrastructure (1 hour)
9. Modify `IdentityServiceExtensions.cs` - add Google auth (30 min)
10. Modify `Startup.cs` - register services (15 min)
11. Modify `CookieExtensions.cs` - broaden path (5 min)

### Phase 4: Backend Controller (2 hours)
12. Create `ExternalAuthController` with all endpoints

### Phase 5: Angular Frontend (2-3 hours)
13. Update `JwtInterceptor` - exclude exchange endpoint (5 min)
14. Update `AccountService` - add Google methods (30 min)
15. Update `LoginComponent` - add button and callback (1 hour)
16. Update `user.ts` interfaces (10 min)
17. Create `ExternalLoginsComponent` (optional, 1 hour)

**Total Estimated Time:** 7-9 hours

---

## Validation Against Codebase

### Confirmed Assumptions ✅

| Assumption | Status | Evidence |
|------------|--------|----------|
| AspNetUserLogins table exists | ✅ | Found in `Data/Identity/Migrations/20201225194245_InitialCreate.cs` |
| Redis is configured | ✅ | Singleton `IConnectionMultiplexer` in `Startup.cs:20-26` |
| Cookie path is `/api/account` | ✅ | `CookieExtensions.cs:17` |
| JWT Bearer is primary auth | ✅ | `IdentityServiceExtensions.cs` - only JWT scheme configured |
| TokenService/RefreshTokenService exist | ✅ | `Api/Identity/` folder has all services |
| No Google auth package currently | ✅ | Not in `Directory.Packages.props` |

### Corrections Applied

1. **JwtInterceptor Update:** Added missing step to exclude `/externalauth/exchange` endpoint
2. **DisplayName Handling:** Specified to use Google's `name` claim, fallback to email prefix
3. **Correlation Cookie Path:** Noted to ensure path is `/api` for state validation
4. **Breaking Change Warning:** Added note about cookie path change requiring re-login

---

## Next Steps

1. **Setup Google Cloud Console** - Create OAuth credentials
2. **Start Phase 1** - Backend package & configuration
3. **Follow Implementation Order** - Complete phases sequentially
4. **Run Verification Plan** - See `doc/Social Login Integration/Verification-Plan.md`
5. **Deploy** - Update production configuration and Google OAuth redirect URIs

---

## References

- [Google OAuth 2.0 Documentation](https://developers.google.com/identity/protocols/oauth2)
- [ASP.NET Core Google Authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins)
- [ASP.NET Core Identity External Login](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/)
