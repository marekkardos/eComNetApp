# Authentication Security Refactoring Plan

## Summary of Changes

This plan covers three main areas:
1. **Token configuration cleanup** - Remove unused Audience, fix Issuer
2. **Secure refresh token implementation** - HttpOnly cookies + short-lived access tokens
3. **Enable account lockout** - Brute force protection

---

## Part 1: Token Configuration Cleanup - DONE

~~Already completed:~~
- ~~Remove `Token:Audience` from appsettings~~
- ~~Change `Token:Issuer` to `"eComNetApp"`~~
- ~~Remove commented audience validation code~~

---

## Part 1.5: Enable Account Lockout

### Changes in `Api/StartupConfigurations/IdentityServiceExtensions.cs`

Uncomment and configure lockout settings:
```csharp
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.AllowedForNewUsers = true;
```

### Changes in `Api/Controllers/AccountController.cs`

Update login to enable lockout tracking:
```csharp
// Change from:
var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

// To:
var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, true);
//                                                                                 ^^^^ lockoutOnFailure: true
```

Add locked account handling:
```csharp
if (result.IsLockedOut)
{
    return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized, "Account locked due to multiple failed attempts. Try again later."));
}
```

---

## Part 2: Secure Refresh Token Implementation

### Target Architecture
```
Login → HttpOnly cookie (refresh token, 7-30 days)
      + Response body (access token, 15 min)
        ↓
Angular stores access token in memory only (not localStorage)
        ↓
Token expires → Call /refresh → Cookie auto-sent → New access token
```

### Backend Changes

#### 1. New Entity: `Core/Entities/Identity/RefreshToken.cs`
- Id, Token (hashed), AppUserId, JwtId
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
- Return tuple `(string Token, string JwtId)`
- Use configurable expiration (15 min default)
- Add JTI claim for refresh token linkage

#### 5. New Helper: `Api/Extensions/CookieExtensions.cs`
- `SetRefreshTokenCookie()` - HttpOnly, Secure, SameSite=Strict
- `ClearRefreshTokenCookie()`

#### 6. Update: `Api/Controllers/AccountController.cs`
- **Login/Register**: Set refresh token cookie, return short-lived access token
- **New `POST /refresh`**: Validate cookie, rotate token, return new access token
- **New `POST /logout`**: Revoke refresh token, clear cookie

#### 7. Database Migration
- Add `RefreshTokens` table to Identity context
- Add navigation property to `AppUser`

### Frontend Changes

#### 1. Update: `client/src/app/account/account.service.ts`
- Store access token in memory property (not localStorage)
- Remove all localStorage token operations
- Add `initializeAuth()` - Call /refresh on app startup
- Add `refreshToken()` - With request deduplication
- Update `logout()` - Call server endpoint

#### 2. Update: `client/src/app/core/interceptors/jwt.interceptor.ts`
- Get token from AccountService (memory)
- On 401: Call refresh, queue other requests, retry with new token
- Skip auth endpoints from interception

#### 3. Update: `client/src/app/app.component.ts`
- Call `accountService.initializeAuth()` on startup
- Handle page refresh (memory cleared → refresh endpoint restores session)

#### 4. Update: `client/src/app/core/interceptors/error.interceptor.ts`
- Don't show toast for 401 on /refresh (expected behavior)

---

## Files to Modify/Create

### Backend (Create)
- `Core/Entities/Identity/RefreshToken.cs`
- `Data/Identity/Config/RefreshTokenConfiguration.cs`
- `Api/Identity/TokenSettings.cs`
- `Api/Identity/IRefreshTokenService.cs`
- `Api/Identity/RefreshTokenService.cs`
- `Api/Extensions/CookieExtensions.cs`

### Backend (Modify)
- `Api/appsettings.Development.json` - Token config + TokenSettings section
- `Api/Identity/ITokenService.cs` - Return tuple
- `Api/Identity/TokenService.cs` - JTI claim, configurable expiration
- `Api/Controllers/AccountController.cs` - New endpoints, cookie handling
- `Api/Startup.cs` - Register new services
- `Api/StartupConfigurations/IdentityServiceExtensions.cs` - Remove audience code
- `Data/Identity/AppIdentityDbContext.cs` - Add RefreshTokens DbSet
- `Core/Entities/Identity/AppUser.cs` - Add RefreshTokens navigation

### Frontend (Modify)
- `client/src/app/account/account.service.ts` - Complete rewrite
- `client/src/app/core/interceptors/jwt.interceptor.ts` - Complete rewrite
- `client/src/app/core/interceptors/error.interceptor.ts` - Update 401 handling
- `client/src/app/app.component.ts` - Add auth initialization
- `client/src/app/shared/models/user.ts` - Make token optional

---

## Implementation Order

1. ~~**Token config cleanup** (Part 1)~~ - DONE
2. **Enable lockout** (Part 1.5) - IdentityServiceExtensions + AccountController
3. **Backend entity + migration** - RefreshToken entity, DbContext, migration
4. **Backend services** (TokenSettings, RefreshTokenService, update TokenService)
5. **Backend controller** (AccountController new endpoints)
6. **Frontend service** (account.service.ts)
7. **Frontend interceptors** (jwt.interceptor.ts, error.interceptor.ts)
8. **Frontend app component** (initialization)

---

## Verification

### Manual Testing
1. Login → Check browser DevTools for HttpOnly cookie
2. Page refresh → Session persists (refresh endpoint called)
3. Wait 15+ min → Next API call triggers transparent refresh
4. Logout → Cookie cleared, session gone
5. Multiple tabs → Refresh in one tab works for all

### Automated Testing
- Backend: Integration tests for /refresh, /logout endpoints
- Backend: Token rotation and reuse detection
- Frontend: E2E login/logout/refresh flow

---

## Security Benefits

| Current | After |
|---------|-------|
| Token in localStorage (XSS vulnerable) | Access token in memory only |
| 7-day token exposure | 15-min token exposure |
| No refresh mechanism | Automatic transparent refresh |
| No token revocation | Server-side revocation + rotation |
| No reuse detection | Detects stolen refresh tokens |
| Unlimited login attempts | Account locks after 5 failures (15 min) |
| Unused Token:Audience config | Clean config, Issuer = "eComNetApp" |
