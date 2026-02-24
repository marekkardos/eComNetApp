# Authentication Security Refactoring Plan

## Summary of Changes

This plan covers three main areas:
1. **Token configuration cleanup** - Remove unused Audience, fix Issuer
2. **Secure refresh token implementation** - HttpOnly cookies + short-lived access tokens
3. **Enable account lockout** - Brute force protection

**Status: COMPLETED**

---

## Part 1: Token Configuration Cleanup - DONE

- Removed `Token:Audience` from appsettings
- Changed `Token:Issuer` to `"eComNetApp"`
- Removed commented audience validation code

---

## Part 1.5: Enable Account Lockout - DONE

### Changes in `Api/StartupConfigurations/IdentityServiceExtensions.cs`

Configured lockout settings:
```csharp
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.AllowedForNewUsers = true;
```

### Changes in `Api/Controllers/AccountController.cs`

Updated login to enable lockout tracking:
```csharp
var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

if (result.IsLockedOut)
{
    return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized,
        "Account locked due to multiple failed attempts. Try again later."));
}
```

---

## Part 2: Secure Refresh Token Implementation - DONE

### Target Architecture
```
Login -> HttpOnly cookie (refresh token, 7 days)
       + Response body (access token, 15 min)
         |
Angular stores access token in memory only (not localStorage)
         |
Token expires -> Call /refresh -> Cookie auto-sent -> New access token
```

### Backend Changes

#### 1. New Entity: `Core/Entities/Identity/RefreshToken.cs`
- Id, Token (SHA256 hashed), AppUserId, JwtId
- ExpiresAt, CreatedAt, IsRevoked, RevokedAt
- ReplacedByToken (for rotation tracking)

#### 2. New Configuration: `Api/Identity/TokenSettings.cs`
```csharp
public class TokenSettings
{
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public string CookieName { get; set; } = "refreshToken";
}
```

#### 3. New Service: `Api/Identity/RefreshTokenService.cs`
- `GenerateRefreshTokenAsync(user, jwtId)` - Create and store hashed token
- `ValidateRefreshTokenAsync(token)` - Validate, check expiry/revocation
- `RotateRefreshTokenAsync(oldToken, user, newJwtId)` - Revoke old, create new
- `RevokeTokenAsync(token)` - Mark as revoked
- `RevokeAllUserTokensAsync(userId)` - Security: revoke all on reuse detection

#### 4. Update: `Api/Identity/TokenService.cs`
- Returns tuple `(string Token, string JwtId)`
- Uses configurable expiration (15 min default)
- Adds JTI claim for refresh token linkage

#### 5. New Helper: `Api/Extensions/CookieExtensions.cs`
- `SetRefreshTokenCookie()` - HttpOnly, Secure, SameSite=Strict
- `ClearRefreshTokenCookie()`

#### 6. Update: `Api/Controllers/AccountController.cs`
- **Login/Register**: Set refresh token cookie, return short-lived access token
- **New `POST /refresh`**: Validate cookie, rotate token, return new access token
- **New `POST /logout`**: Revoke refresh token, clear cookie (AllowAnonymous)

#### 7. Database Migration
- Added `RefreshTokens` table to Identity context
- Added navigation property to `AppUser`
- Migration: `20260113141218_AddRefreshTokens`

### Frontend Changes

#### 1. Update: `client/src/app/account/account.service.ts`
- Access token stored in memory property (not localStorage)
- Added `authInitialized$` observable for guards to wait on
- Added `initializeAuth()` - Call /refresh on app startup
- Added `refreshToken()` - With request deduplication
- Updated `logout()` - Calls server endpoint

#### 2. Update: `client/src/app/core/interceptors/jwt.interceptor.ts`
- Gets token from AccountService (memory)
- On 401: Calls refresh, queues other requests, retries with new token
- Skips auth endpoints (`/login`, `/register`, `/refresh`, `/logout`)

#### 3. Update: `client/src/app/core/guards/auth.guard.ts`
- Waits for `authInitialized$` before checking user
- Prevents race condition on page load

#### 4. Update: `client/src/app/app.component.ts`
- Calls `accountService.initializeAuth()` on startup
- Handles page refresh (memory cleared -> refresh endpoint restores session)

#### 5. Update: `client/src/app/core/interceptors/error.interceptor.ts`
- Doesn't show toast for 401 on /refresh (expected behavior)

---

## Files Modified/Created

### Backend (Created)
- `Core/Entities/Identity/RefreshToken.cs`
- `Data/Identity/Config/RefreshTokenConfiguration.cs`
- `Api/Identity/TokenSettings.cs`
- `Api/Identity/IRefreshTokenService.cs`
- `Api/Identity/RefreshTokenService.cs`
- `Api/Extensions/CookieExtensions.cs`
- `Data/Identity/Migrations/20260113141218_AddRefreshTokens.cs`

### Backend (Modified)
- `Api/appsettings.Development.json` - TokenSettings section added
- `Api/Identity/ITokenService.cs` - Returns tuple
- `Api/Identity/TokenService.cs` - JTI claim, configurable expiration
- `Api/Controllers/AccountController.cs` - New endpoints, cookie handling
- `Api/Startup.cs` - Register new services
- `Api/StartupConfigurations/IdentityServiceExtensions.cs` - Lockout enabled
- `Data/Identity/AppIdentityDbContext.cs` - RefreshTokens DbSet
- `Core/Entities/Identity/AppUser.cs` - RefreshTokens navigation

### Frontend (Modified)
- `client/src/app/account/account.service.ts` - Complete rewrite
- `client/src/app/core/interceptors/jwt.interceptor.ts` - Complete rewrite
- `client/src/app/core/interceptors/error.interceptor.ts` - Update 401 handling
- `client/src/app/core/guards/auth.guard.ts` - Wait for auth init
- `client/src/app/app.component.ts` - Auth initialization
- `client/src/app/core/nav-bar/nav-bar.component.ts` - Subscribe to logout

---

## Verification

### Manual Testing
1. Login -> Check browser DevTools for HttpOnly cookie
2. Page refresh -> Session persists (refresh endpoint called)
3. Wait 15+ min -> Next API call triggers transparent refresh
4. Logout -> Cookie cleared, session gone
5. Navigate to protected route -> Guard waits for auth init
6. Multiple tabs → Refresh in one tab works for all

### Automated Testing
- Backend: Integration tests for /refresh, /logout endpoints
- Backend: Token rotation and reuse detection
- Frontend: E2E login/logout/refresh flow

### Database Migration
```bash
dotnet ef database update --project Data/Data.csproj --startup-project Api/Api.csproj --context AppIdentityDbContext
```

---

## Security Benefits

| Before | After |
|--------|-------|
| Token in localStorage (XSS vulnerable) | Access token in memory only |
| 7-day token exposure | 15-min token exposure |
| No refresh mechanism | Automatic transparent refresh |
| No token revocation | Server-side revocation + rotation |
| No reuse detection | Detects stolen refresh tokens |
| Unlimited login attempts | Account locks after 5 failures (15 min) |
| Unused Token:Audience config | Clean config, Issuer = "eComNetApp" |



## Security Improvements to Consider

### Fix: Add rate limiting middleware
  Complexity: Medium
  Impact: IP-based throttling
  Tier 2: Hardening
  
### Fix: Increase password minimum to 12+ chars
  Complexity: Low
  Impact: Stronger passwords

### Fix: Add security headers middleware
  Complexity: Low
  Impact: XSS/clickjacking protection

### Fix: Tighten CORS (remove AllowAny*)
  Complexity: Low
  Impact: Reduce attack surface
  Tier 3: Advanced

### Fix: Add failed login monitoring/alerting
  Complexity: Medium
  Impact: Detect attacks in progress

### Fix: Implement progressive delays
  Complexity: Medium
  Impact: Slow down attackers

### Fix: Add CAPTCHA after N failures
  Complexity: High
  Impact: Block automated attacks