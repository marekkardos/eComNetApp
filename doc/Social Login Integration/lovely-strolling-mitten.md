# Google OAuth Implementation Plan Review

## Summary

The existing plan at `\doc\Social Login Integration\quizzical-crafting-pie.md` is **well-structured and technically sound**. After validating against the actual codebase, I've identified a few corrections and additions needed.

---

## Validation Results

### Confirmed Assumptions (All Correct)

| Assumption | Status | Evidence |
|------------|--------|----------|
| AspNetUserLogins table exists | ✅ | Found in `Data/Identity/Migrations/20201225194245_InitialCreate.cs` |
| Redis is configured | ✅ | Singleton `IConnectionMultiplexer` in `Startup.cs:20-26` |
| Cookie path is `/api/account` | ✅ | `CookieExtensions.cs:17` |
| JWT Bearer is primary auth | ✅ | `IdentityServiceExtensions.cs` - only JWT scheme configured |
| TokenService/RefreshTokenService exist | ✅ | `Api/Identity/` folder has all services |
| No Google auth package | ✅ | Not in `Directory.Packages.props` |

### Issues Found

#### 1. Package Version (Minor)
**Plan says:** `8.0.22`
**Reality:** Version should match other auth packages. Current `Microsoft.AspNetCore.Authentication.JwtBearer` is at `8.0.22`, so this is correct. However, verify latest available version before implementation.

#### 2. Missing: Angular JwtInterceptor Update
**Problem:** The `JwtInterceptor` (line 78) excludes auth endpoints:
```typescript
const authEndpoints = ['/account/login', '/account/register', '/account/refresh', '/account/logout'];
```
**Fix needed:** Add `/externalauth/exchange` to excluded endpoints.

#### 3. Distributed Cache Not Currently Registered
**Plan says:** Use `AddStackExchangeRedisCache` for auth codes
**Reality:** Currently only `IConnectionMultiplexer` is registered as singleton
**Recommendation:** Either:
- Add `AddStackExchangeRedisCache` (simpler, plan's approach)
- Or use existing `IConnectionMultiplexer` directly (no new registration)

Both work. Plan's approach is cleaner.

#### 4. Cookie Path Change Impact
**Plan says:** Change cookie path from `/api/account` to `/api`
**Impact:** This is a breaking change for existing sessions - refresh tokens will stop working until users re-login. Consider a migration strategy or accept one-time re-login.

#### 5. Missing: Angular Error Handling Route
**Plan mentions:** Handle `error`, `error_description` params in LoginComponent
**Missing:** No dedicated error display for OAuth failures. Recommend reusing login component's error display.

---

## Recommended Corrections to Plan

### Phase 3 Addition: Angular Interceptor Update

Add step after frontend modifications:

**Update JwtInterceptor** (`src/app/core/interceptors/jwt.interceptor.ts`)
- Add `/externalauth/exchange` to `authEndpoints` array (line 78)

### Phase 5 Clarification: DisplayName Handling

When creating a new user from Google login, the `ExternalAuthService.FindOrCreateUserAsync()` should:
- Use Google's `name` claim as `DisplayName`
- Fall back to email prefix if name not provided

### Security Addition: State Parameter

The plan mentions ASP.NET Core handles state automatically. Confirm this works with BFF pattern:
- State is stored in correlation cookie
- Callback validates state
- Important: Ensure correlation cookie path is `/api` not `/api/account`

---

## Files to Modify (Validated List)

### Backend - Confirmed Paths

| File | Exists | Changes |
|------|--------|---------|
| `Api/Api.csproj` | ✅ | Add Google auth package reference |
| `Directory.Packages.props` | ✅ | Add `Microsoft.AspNetCore.Authentication.Google` version |
| `Api/StartupConfigurations/IdentityServiceExtensions.cs` | ✅ | Add `.AddGoogle()` configuration |
| `Api/Startup.cs` | ✅ | Register services, add distributed cache |
| `Api/Extensions/CookieExtensions.cs` | ✅ | Change path to `/api` |
| `Api/Identity/IAuthEventsLog.cs` | ✅ | Add external auth event methods |
| `Api/Identity/AuthEventsLog.cs` | ✅ | Implement external auth events |
| `Api/appsettings.Development.json` | ✅ | Add `GoogleAuth` section |

### Frontend - Confirmed Paths

| File | Exists | Changes |
|------|--------|---------|
| `src/app/account/account.service.ts` | ✅ | Add Google OAuth methods |
| `src/app/account/login/login.component.ts` | ✅ | Add Google button, callback handling |
| `src/app/account/login/login.component.html` | ✅ | Add Google button UI |
| `src/app/shared/models/user.ts` | ✅ | Add external login interfaces |
| `src/app/core/interceptors/jwt.interceptor.ts` | ✅ | **ADD TO PLAN**: Exclude exchange endpoint |

### New Files to Create (Confirmed)

| File | Purpose |
|------|---------|
| `Api/Identity/GoogleAuthSettings.cs` | Configuration POCO |
| `Api/Identity/IExternalAuthCodeService.cs` | Code service interface |
| `Api/Identity/ExternalAuthCodeService.cs` | Redis-backed code service |
| `Api/Identity/IExternalAuthService.cs` | External auth interface |
| `Api/Identity/ExternalAuthService.cs` | Find/create/link logic |
| `Api/Controllers/ExternalAuthController.cs` | OAuth endpoints |
| `Api/Dtos/ExternalAuthCodeExchangeDto.cs` | Code exchange DTO |
| `Api/Dtos/ExternalLoginInfoDto.cs` | Provider info DTO |
| `src/app/account/external-logins/` | New Angular component |

---

## Implementation Order (Validated)

### Phase 1: Backend Package & Config
1. Add package to `Directory.Packages.props`
2. Reference in `Api.csproj`
3. Create `GoogleAuthSettings.cs`
4. Add config section to appsettings

### Phase 2: Backend Services
5. Create DTOs
6. Create `IExternalAuthCodeService` + `ExternalAuthCodeService`
7. Create `IExternalAuthService` + `ExternalAuthService`
8. Update `IAuthEventsLog` + `AuthEventsLog`

### Phase 3: Backend Infrastructure
9. Modify `IdentityServiceExtensions.cs` - add Google auth
10. Modify `Startup.cs` - register services
11. Modify `CookieExtensions.cs` - broaden path

### Phase 4: Backend Controller
12. Create `ExternalAuthController` with all endpoints

### Phase 5: Angular Frontend
13. Update `JwtInterceptor` - exclude exchange endpoint
14. Update `AccountService` - add Google methods
15. Update `LoginComponent` - add button and callback
16. Update `user.ts` interfaces
17. Create `ExternalLoginsComponent` (optional for MVP)

---

## Verification Plan (Enhanced)

### Automated Tests
- Integration test for code exchange endpoint
- Unit test for `ExternalAuthCodeService` (code expiration, single-use)

### Manual Tests
1. **New user flow**: Google login → account created → logged in
2. **Auto-link flow**: Create password account → logout → Google login with same email → linked
3. **Expired code**: Wait 61 seconds → exchange should fail
4. **Reused code**: Exchange same code twice → second should fail
5. **Invalid returnUrl**: Use non-whitelisted host → should reject
6. **Cookie path**: Verify refresh works after Google login

---

## Ready for Implementation

**Assessment:** The plan is **ready to implement** with the corrections noted above.

**Risk Level:** Low - the BFF pattern is standard, infrastructure is in place.

**Estimated Scope:**
- Backend: ~9 new files, ~5 modified files
- Frontend: ~4 modified files, ~1 new component
