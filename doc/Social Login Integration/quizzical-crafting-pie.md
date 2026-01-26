# Google OAuth Social Login Implementation Plan

## Overview

Implement Google OAuth social login using the BFF (Backend-for-Frontend) pattern with code exchange for the eComNetApp.

## Technical Decisions

| Decision | Choice |
|----------|--------|
| OAuth Flow | BFF - all OAuth logic on backend |
| Token Delivery | Code exchange (short-lived code → access token) |
| Auto-link | Trust Google's email verification |
| External Login Storage | Built-in `AspNetUserLogins` table (already exists) |

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

## User Scenarios

| Scenario | Action |
|----------|--------|
| New Google user | Create account with Google info, link Google ID |
| Existing user (same email) | Auto-link (Google verified email) |
| Logged-in user linking | `/api/externalauth/google/link` endpoint |
| User unlinking | DELETE `/api/externalauth/google/unlink` (requires password or other login) |

---

## Implementation Steps

### Phase 1: Backend Package & Configuration

1. **Add package reference**
   - `Api/Api.csproj`: Add `Microsoft.AspNetCore.Authentication.Google`
   - `Directory.Packages.props`: Add version `8.0.22`

2. **Create configuration class**
   - `Api/Identity/GoogleAuthSettings.cs`
   ```csharp
   public class GoogleAuthSettings
   {
       public string ClientId { get; set; } = string.Empty;
       public string ClientSecret { get; set; } = string.Empty;
       public int AuthCodeExpirationSeconds { get; set; } = 60;
       public string[] AllowedReturnUrlHosts { get; set; } = ["localhost"];
   }
   ```

3. **Update appsettings**
   - Add `GoogleAuth` section with placeholders
   - Use user-secrets for actual credentials

### Phase 2: Backend Services

4. **Create DTOs** (`Api/Dtos/`)
   - `ExternalAuthCodeExchangeDto` - { Code: string }
   - `ExternalLoginInfoDto` - { Provider, ProviderDisplayName, IsLinked }
   - `UserWithExternalLoginsDto` - extends UserDto with ExternalLogins[], HasPassword

5. **Create ExternalAuthCodeService** (`Api/Identity/`)
   - Interface: `IExternalAuthCodeService`
   - Implementation: Redis-backed, short-lived (60s), single-use codes
   - Methods: `GenerateCodeAsync(user)`, `ValidateAndConsumeCodeAsync(code)`

6. **Create ExternalAuthService** (`Api/Identity/`)
   - Interface: `IExternalAuthService`
   - Methods:
     - `FindOrCreateUserAsync(loginInfo)` - handles auto-link logic
     - `LinkExternalLoginAsync(user, loginInfo)` - explicit linking
     - `UnlinkExternalLoginAsync(user, provider, key)` - with password check

7. **Update AuthEventsLog** (`Api/Identity/`)
   - Add: `Monitor_ExternalLogin`, `Monitor_ExternalAccountLinked`, `Monitor_ExternalAccountUnlinked`

### Phase 3: Backend Infrastructure

8. **Modify IdentityServiceExtensions.cs**
   - Add `.AddGoogle()` configuration after JWT Bearer
   - Configure scopes: email, profile
   - Handle redirect events for API routes

9. **Modify Startup.cs**
   - Register `GoogleAuthSettings` configuration
   - Register `IExternalAuthService`, `IExternalAuthCodeService`
   - Add `AddStackExchangeRedisCache` for distributed cache (auth codes)

10. **Modify CookieExtensions.cs**
    - Change cookie `Path` from `/api/account` to `/api` (covers both account and externalauth)

### Phase 4: Backend Controller

11. **Create ExternalAuthController** (`Api/Controllers/`)
    - `GET /api/externalauth/google` - Initiates OAuth flow
    - `GET /api/externalauth/google/callback` - OAuth callback, creates code, redirects
    - `POST /api/externalauth/exchange` - Exchanges code for access token
    - `GET /api/externalauth/google/link` - Initiates linking (authenticated)
    - `GET /api/externalauth/google/link/callback` - Link callback
    - `DELETE /api/externalauth/google/unlink` - Unlinks Google
    - `GET /api/externalauth/providers` - Lists linked providers

### Phase 5: Angular Frontend

12. **Update AccountService** (`src/app/account/account.service.ts`)
    - `initiateGoogleLogin(returnUrl)` - redirects to API
    - `exchangeAuthCode(code)` - exchanges code, calls `handleAuthSuccess`
    - `getExternalLogins()` - gets linked providers
    - `initiateGoogleLink(returnUrl)` - for explicit linking
    - `unlinkGoogle()` - removes Google link

13. **Update LoginComponent** (`src/app/account/login/`)
    - Add Google login button
    - Handle OAuth callback (check for `code` query param)
    - Handle error display (`error`, `error_description` params)

14. **Create ExternalLoginsComponent** (`src/app/account/external-logins/`)
    - Display linked providers
    - Link/unlink buttons
    - Show warning if unlinking only login method

15. **Update interfaces** (`src/app/shared/models/user.ts`)
    - Add `IExternalLoginInfo`, `IUserWithExternalLogins`

---

## Files to Create

| File | Purpose |
|------|---------|
| `Api/Identity/GoogleAuthSettings.cs` | Configuration POCO |
| `Api/Identity/IExternalAuthCodeService.cs` | Code service interface |
| `Api/Identity/ExternalAuthCodeService.cs` | Redis-backed code service |
| `Api/Identity/IExternalAuthService.cs` | External auth orchestration interface |
| `Api/Identity/ExternalAuthService.cs` | Find/create/link user logic |
| `Api/Controllers/ExternalAuthController.cs` | OAuth endpoints |
| `Api/Dtos/ExternalAuthCodeExchangeDto.cs` | Code exchange request |
| `Api/Dtos/ExternalLoginInfoDto.cs` | Provider info |
| `Api/Dtos/UserWithExternalLoginsDto.cs` | User + providers response |

## Files to Modify

| File | Changes |
|------|---------|
| `Api/Api.csproj` | Add Google auth package |
| `Directory.Packages.props` | Add package version |
| `Api/StartupConfigurations/IdentityServiceExtensions.cs` | Add Google OAuth config |
| `Api/Startup.cs` | Register services, distributed cache |
| `Api/Extensions/CookieExtensions.cs` | Broaden cookie path to `/api` |
| `Api/Identity/IAuthEventsLog.cs` | Add external auth events |
| `Api/Identity/AuthEventsLog.cs` | Implement external auth events |
| `appsettings.Development.json` | Add GoogleAuth section |
| `src/app/account/account.service.ts` | Add Google OAuth methods |
| `src/app/account/login/login.component.ts` | Google button, callback handling |
| `src/app/account/login/login.component.html` | Google button UI |
| `src/app/shared/models/user.ts` | Add interfaces |

---

## Security Considerations

- **Return URL validation**: Whitelist allowed hosts in `AllowedReturnUrlHosts`
- **State parameter**: Handled automatically by ASP.NET Core Google middleware (CSRF protection)
- **Auth code**: Single-use, 60-second TTL, stored in Redis, cryptographically secure
- **Cookie security**: HttpOnly, Secure (prod), SameSite=Strict

---

## Google Cloud Console Setup

1. Create project at https://console.cloud.google.com/
2. Enable OAuth consent screen
3. Create OAuth 2.0 credentials (Web application)
4. Add authorized redirect URIs:
   - `http://localhost:44369/api/externalauth/google/callback`
   - `http://localhost:44369/api/externalauth/google/link/callback`
5. Copy Client ID and Secret to user-secrets

---

## Verification Plan

1. **New user flow**: Click Google login → Complete Google auth → Verify account created → Verify logged in
2. **Auto-link flow**: Create account with email X → Logout → Google login with same email → Verify linked
3. **Explicit link flow**: Login with password → Go to settings → Link Google → Verify linked
4. **Unlink flow**: User with password + Google → Unlink Google → Verify Google removed
5. **Security tests**:
   - Invalid returnUrl → Should reject/default
   - Expired code → Should fail exchange
   - Reused code → Should fail
6. **Edge cases**:
   - User with only Google login tries to unlink → Should fail with message
   - Google account already linked to another user → Should fail with message
