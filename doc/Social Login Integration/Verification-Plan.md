# Google OAuth Social Login - Verification Plan

## Overview

This document provides comprehensive testing procedures for the Google OAuth social login implementation. All tests should be executed in order, and results documented.

**Test Environment:**
- Backend API: `http://localhost:44369`
- Angular App: `http://localhost:4200`
- Test Google Account: Use dedicated test account from Google Cloud Console

---

## Pre-Verification Checklist

Before starting verification, ensure:

- [ ] Google Cloud Console OAuth credentials configured
- [ ] Backend configuration (`GoogleAuth` section) properly set
- [ ] User-secrets configured with Client ID and Secret
- [ ] Redis is running and accessible
- [ ] Database migrations applied
- [ ] Backend API running on port 44369
- [ ] Angular app running on port 4200
- [ ] Browser with clean session (incognito/private mode recommended)

---

## Test Categories

1. [Happy Path Scenarios](#1-happy-path-scenarios)
2. [Auto-Link Scenarios](#2-auto-link-scenarios)
3. [Explicit Link/Unlink Scenarios](#3-explicit-linkunlink-scenarios)
4. [Security & Validation Tests](#4-security--validation-tests)
5. [Error Handling Tests](#5-error-handling-tests)
6. [Edge Cases & Boundary Tests](#6-edge-cases--boundary-tests)
7. [Integration Tests](#7-integration-tests)
8. [Performance & Load Tests](#8-performance--load-tests)

---

## 1. Happy Path Scenarios

### Test 1.1: New User Registration via Google

**Objective:** Verify that a new user can register using Google OAuth

**Prerequisites:**
- Use Google account not previously registered in system
- User email: `newuser@gmail.com`

**Test Flow:**
1. Navigate to login page: `http://localhost:4200/login`
2. Click "Sign in with Google" button
3. **Expected:** Redirect to Google OAuth consent screen
4. Enter Google credentials and approve consent
5. **Expected:** Redirect back to Angular app
6. **Expected:** User is logged in, redirect to dashboard/home
7. Verify user in database:
   ```sql
   SELECT * FROM AspNetUsers WHERE Email = 'newuser@gmail.com';
   SELECT * FROM AspNetUserLogins WHERE LoginProvider = 'Google';
   ```
8. Check `DisplayName` field populated from Google profile
9. Verify refresh token cookie set (check browser DevTools → Application → Cookies)
10. Verify access token stored in Angular service (check Network tab)

**Expected Results:**
- ✅ User created in `AspNetUsers` table
- ✅ External login record created in `AspNetUserLogins` table
- ✅ User logged in with valid access token
- ✅ Refresh token cookie set (HttpOnly, SameSite=Strict)
- ✅ DisplayName populated from Google `name` claim
- ✅ Email confirmed (TrustEmailVerification = true)
- ✅ No errors in browser console or API logs

**Success Criteria:** All expectations met, user can access protected routes

---

### Test 1.2: Existing Google User Login

**Objective:** Verify that an existing Google user can log in

**Prerequisites:**
- Use same Google account from Test 1.1 (`newuser@gmail.com`)
- User already registered via Google

**Test Flow:**
1. Logout current user
2. Navigate to login page
3. Click "Sign in with Google" button
4. **Expected:** Redirect to Google (may auto-approve if session active)
5. **Expected:** Redirect back to Angular app
6. **Expected:** User logged in, redirect to dashboard
7. Verify no duplicate user created (check database)
8. Verify same `UserId` retrieved from database

**Expected Results:**
- ✅ User logged in successfully
- ✅ No duplicate user records created
- ✅ Same `UserId` as Test 1.1
- ✅ Access token and refresh cookie valid

**Success Criteria:** User logs in seamlessly without creating duplicates

---

### Test 1.3: Code Exchange Success

**Objective:** Verify the code exchange mechanism works correctly

**Prerequisites:**
- Fresh browser session

**Test Flow:**
1. Initiate Google login
2. Complete Google OAuth flow
3. **Capture:** Note the `code` parameter in redirect URL (browser address bar)
4. **Observe:** Angular automatically calls `/api/externalauth/exchange`
5. **Verify:** Response contains `{ accessToken: "..." }`
6. **Verify:** Access token is valid JWT (decode at jwt.io)
7. **Verify:** Token contains claims: `sub`, `email`, `name`
8. Try to reuse the same code by replaying exchange request:
   ```bash
   curl -X POST http://localhost:44369/api/externalauth/exchange \
     -H "Content-Type: application/json" \
     -d '{"code":"<captured-code>"}'
   ```
9. **Expected:** Second request fails with "Invalid or expired code"

**Expected Results:**
- ✅ Code successfully exchanged for access token
- ✅ Access token is valid JWT
- ✅ Code is single-use (fails on replay)
- ✅ Code expires after 60 seconds

**Success Criteria:** Code exchange works securely with single-use enforcement

---

## 2. Auto-Link Scenarios

### Test 2.1: Auto-Link Existing Email Account

**Objective:** Verify Google account auto-links to existing email/password account

**Prerequisites:**
- Create user via traditional registration: `existinguser@gmail.com` with password `Test123!`
- Use Google account with same email: `existinguser@gmail.com`

**Test Flow:**
1. Register via traditional form: email `existinguser@gmail.com`, password `Test123!`
2. **Verify:** User created with email/password only (no external logins)
   ```sql
   SELECT * FROM AspNetUsers WHERE Email = 'existinguser@gmail.com';
   SELECT * FROM AspNetUserLogins WHERE UserId = '<user-id>';
   -- Should return 0 rows
   ```
3. Logout
4. Click "Sign in with Google"
5. Use Google account with email `existinguser@gmail.com`
6. **Expected:** Auto-link occurs, user logged in
7. **Verify:** External login record created:
   ```sql
   SELECT * FROM AspNetUserLogins WHERE UserId = '<user-id>' AND LoginProvider = 'Google';
   -- Should return 1 row
   ```
8. **Verify:** Same `UserId` as step 2 (no new user created)
9. Logout and try logging in with password → Should still work
10. Logout and try logging in with Google → Should still work

**Expected Results:**
- ✅ Google account linked to existing user
- ✅ No new user created
- ✅ User can log in with both password and Google
- ✅ `AspNetUserLogins` table has Google provider entry
- ✅ Auth event logged: `Monitor_ExternalLogin` and implicit link

**Success Criteria:** Auto-link works, user has two login methods

---

### Test 2.2: Auto-Link Security Verification

**Objective:** Ensure auto-link only occurs for verified emails

**Test Flow:**
1. Create user via traditional registration: `testuser@gmail.com`
2. Manually update database to mark email as unconfirmed:
   ```sql
   UPDATE AspNetUsers SET EmailConfirmed = 0 WHERE Email = 'testuser@gmail.com';
   ```
3. Attempt Google login with `testuser@gmail.com`
4. **Expected:** Auto-link should still occur (Google verification is trusted)
5. **Verify:** Email marked as confirmed after auto-link:
   ```sql
   SELECT EmailConfirmed FROM AspNetUsers WHERE Email = 'testuser@gmail.com';
   -- Should return 1 (True)
   ```

**Expected Results:**
- ✅ Auto-link occurs even if local email unconfirmed
- ✅ Email marked as confirmed after Google auth (trust Google's verification)
- ✅ Security not compromised (Google verifies email ownership)

**Success Criteria:** Auto-link trusts Google's email verification appropriately

---

## 3. Explicit Link/Unlink Scenarios

### Test 3.1: Link Google to Existing Account (Explicit)

**Objective:** Verify authenticated user can explicitly link Google account

**Prerequisites:**
- User logged in with email/password: `linktest@gmail.com`
- Google account not yet linked: `linktest@gmail.com`

**Test Flow:**
1. Login with email/password (`linktest@gmail.com`)
2. Navigate to profile/settings page (or external logins component)
3. Click "Link Google Account" button
4. **Expected:** Redirect to `/api/externalauth/google/link?returnUrl=/settings`
5. Complete Google OAuth flow
6. **Expected:** Redirect back to `/settings`
7. **Expected:** Success message displayed "Google account linked"
8. **Verify:** External login record created:
   ```sql
   SELECT * FROM AspNetUserLogins WHERE UserId = '<user-id>' AND LoginProvider = 'Google';
   ```
9. Refresh page
10. **Expected:** "Link Google" button replaced with "Unlink Google" button
11. **Verify:** Auth event logged: `Monitor_ExternalAccountLinked`

**Expected Results:**
- ✅ Google account linked successfully
- ✅ Database record created in `AspNetUserLogins`
- ✅ User remains logged in during linking process
- ✅ UI updated to reflect linked status
- ✅ Event logged

**Success Criteria:** Explicit linking works for authenticated users

---

### Test 3.2: Unlink Google Account (Safe)

**Objective:** Verify user can unlink Google when other login method exists

**Prerequisites:**
- User has both password and Google login linked
- User logged in

**Test Flow:**
1. Ensure user has password set (from previous tests)
2. Navigate to external logins management page
3. Verify UI shows:
   - Google account linked with "Unlink" button
   - "Has Password: Yes"
4. Click "Unlink Google" button
5. **Expected:** Confirmation prompt: "Are you sure? You can re-link anytime."
6. Confirm unlink
7. **Expected:** Success message "Google account unlinked"
8. **Verify:** External login record deleted:
   ```sql
   SELECT * FROM AspNetUserLogins WHERE UserId = '<user-id>' AND LoginProvider = 'Google';
   -- Should return 0 rows
   ```
9. **Verify:** Auth event logged: `Monitor_ExternalAccountUnlinked`
10. Try to login with Google → Should create new unlinked Google login attempt (don't allow, as it's unlinked)
11. Login with password → Should work

**Expected Results:**
- ✅ Google account unlinked successfully
- ✅ Database record removed
- ✅ User can still log in with password
- ✅ Google login no longer works (until re-linked)
- ✅ Event logged

**Success Criteria:** Unlink works when user has alternative login method

---

### Test 3.3: Prevent Unlink of Only Login Method

**Objective:** Verify system prevents unlinking when it's the only way to log in

**Prerequisites:**
- User with ONLY Google login (no password set)

**Test Flow:**
1. Create new user via Google OAuth only (no password)
2. Navigate to external logins management page
3. Verify UI shows:
   - Google account linked
   - "Has Password: No" (or password section indicates no password)
4. Click "Unlink Google" button
5. **Expected:** Error message displayed immediately or after confirmation:
   - "You must set a password or link another account before unlinking Google"
6. **Verify:** Unlink request blocked (check API response)
7. **Verify:** External login record still exists in database
8. Set password via "Set Password" form
9. Try unlinking Google again
10. **Expected:** Now allowed (password is alternative login method)

**Expected Results:**
- ✅ Unlink blocked when Google is only login method
- ✅ Clear error message shown to user
- ✅ Database record preserved
- ✅ Unlink allowed after setting password

**Success Criteria:** User cannot lock themselves out by unlinking only login method

---

## 4. Security & Validation Tests

### Test 4.1: Return URL Validation

**Objective:** Verify return URL is validated against whitelist

**Test Flow:**
1. Attempt Google login with allowed return URL:
   ```
   /api/externalauth/google?returnUrl=http://localhost:4200/dashboard
   ```
   **Expected:** Flow succeeds
2. Attempt Google login with disallowed return URL (open redirect attack):
   ```
   /api/externalauth/google?returnUrl=https://evil.com/phishing
   ```
   **Expected:** Either rejected with error or redirected to default URL (not evil.com)
3. Verify API logs show validation warning
4. Check `GoogleAuthSettings.AllowedReturnUrlHosts` contains only trusted hosts

**Expected Results:**
- ✅ Valid return URLs allowed
- ✅ Invalid return URLs blocked or defaulted
- ✅ No open redirect vulnerability
- ✅ Security event logged for invalid attempts

**Success Criteria:** Return URL validation prevents open redirect attacks

---

### Test 4.2: Auth Code Expiration

**Objective:** Verify auth codes expire after 60 seconds

**Test Flow:**
1. Initiate Google login
2. Complete Google OAuth flow
3. **Capture:** Extract `code` parameter from redirect URL
4. **Wait:** Do not exchange code immediately
5. **After 61 seconds:** Attempt to exchange code:
   ```bash
   curl -X POST http://localhost:44369/api/externalauth/exchange \
     -H "Content-Type: application/json" \
     -d '{"code":"<expired-code>"}'
   ```
6. **Expected:** Response 400 or 401 with error "Invalid or expired code"
7. Verify Redis key no longer exists:
   ```bash
   redis-cli GET external-auth-code:<code>
   # Should return (nil)
   ```

**Expected Results:**
- ✅ Code expires after 60 seconds
- ✅ Expired code cannot be exchanged
- ✅ Clear error message returned
- ✅ Redis key automatically deleted (TTL)

**Success Criteria:** Auth codes have proper TTL enforcement

---

### Test 4.3: Single-Use Code Enforcement

**Objective:** Verify auth codes can only be used once

**Test Flow:**
1. Initiate Google login and complete flow
2. **Capture:** Extract `code` from redirect URL
3. **Immediately** exchange code (within 60 seconds):
   ```bash
   curl -X POST http://localhost:44369/api/externalauth/exchange \
     -H "Content-Type: application/json" \
     -d '{"code":"<valid-code>"}'
   ```
4. **Expected:** Success response with access token
5. **Immediately** attempt to reuse same code:
   ```bash
   curl -X POST http://localhost:44369/api/externalauth/exchange \
     -H "Content-Type: application/json" \
     -d '{"code":"<same-code>"}'
   ```
6. **Expected:** Error "Invalid or expired code"
7. Verify Redis key deleted after first use

**Expected Results:**
- ✅ First exchange succeeds
- ✅ Second exchange fails (code consumed)
- ✅ Redis key deleted after first use
- ✅ Replay attacks prevented

**Success Criteria:** Auth codes are single-use, preventing replay attacks

---

### Test 4.4: State Parameter CSRF Protection

**Objective:** Verify ASP.NET Core Google middleware state parameter protects against CSRF

**Test Flow:**
1. Initiate Google login
2. **Observe:** Check OAuth redirect URL includes `state` parameter
3. **Capture:** Note the `state` value
4. **Tamper:** Attempt to replay callback with different state:
   ```
   /api/externalauth/google/callback?code=valid_code&state=tampered_state
   ```
5. **Expected:** Request rejected with error "Correlation failed"
6. Complete legitimate flow with correct state
7. **Expected:** Success

**Expected Results:**
- ✅ State parameter included in OAuth flow
- ✅ State validation occurs in callback
- ✅ Tampered state rejected
- ✅ CSRF attacks prevented

**Success Criteria:** State parameter provides CSRF protection

---

### Test 4.5: Cookie Security Attributes

**Objective:** Verify refresh token cookie has proper security attributes

**Test Flow:**
1. Complete Google login
2. Open browser DevTools → Application → Cookies
3. Find refresh token cookie (typically named `refreshToken` or similar)
4. **Verify attributes:**
   - `HttpOnly`: ✅ (prevents JavaScript access)
   - `Secure`: ✅ in production (HTTPS only) / ⚠️ false in dev (localhost)
   - `SameSite`: ✅ `Strict` (CSRF protection)
   - `Path`: ✅ `/api` (not `/api/account`)
   - `Domain`: ✅ localhost or production domain
   - `Expires`: ✅ Set (not session-only)
5. Attempt to access cookie via JavaScript console:
   ```javascript
   document.cookie
   ```
   **Expected:** Refresh token not visible (HttpOnly blocks access)

**Expected Results:**
- ✅ All security attributes properly set
- ✅ HttpOnly prevents XSS cookie theft
- ✅ SameSite prevents CSRF attacks
- ✅ Secure flag enforced in production
- ✅ Path updated to `/api` (covers externalauth endpoints)

**Success Criteria:** Cookie security attributes meet best practices

---

### Test 4.6: Duplicate Google Account Prevention

**Objective:** Verify system prevents linking same Google account to multiple users

**Prerequisites:**
- User A logged in with Google account `shared@gmail.com`
- User B exists (different email, password-based)

**Test Flow:**
1. User A registers/logs in with Google (`shared@gmail.com`)
2. **Verify:** Google account linked to User A
3. Logout User A
4. Login as User B (password-based, different email)
5. Navigate to external logins page
6. Click "Link Google Account"
7. Authenticate with same Google account (`shared@gmail.com`)
8. **Expected:** Error message:
   - "This Google account is already linked to another user"
9. **Verify:** Link request blocked (API returns 400/409)
10. **Verify:** User B still has no Google login linked

**Expected Results:**
- ✅ Duplicate link blocked
- ✅ Clear error message displayed
- ✅ Original link (User A) preserved
- ✅ Database constraint prevents duplicates

**Success Criteria:** System prevents account hijacking via duplicate Google links

---

## 5. Error Handling Tests

### Test 5.1: Google OAuth Denial

**Objective:** Verify graceful handling when user denies Google OAuth consent

**Test Flow:**
1. Click "Sign in with Google"
2. On Google consent screen, click "Cancel" or "Deny"
3. **Expected:** Redirect back to Angular app
4. **Expected:** URL contains error parameters:
   ```
   /login?error=access_denied&error_description=User%20denied%20consent
   ```
5. **Expected:** Error message displayed: "Authentication cancelled. Please try again."
6. **Verify:** No user created in database
7. **Verify:** No error stack traces in console (graceful error handling)

**Expected Results:**
- ✅ Graceful redirect on denial
- ✅ User-friendly error message
- ✅ No partial user records created
- ✅ User can retry login

**Success Criteria:** OAuth denial handled gracefully without system errors

---

### Test 5.2: Invalid Client Configuration

**Objective:** Verify error handling for misconfigured Google OAuth credentials

**Test Flow:**
1. Temporarily update user-secrets with invalid Client ID:
   ```bash
   dotnet user-secrets set "GoogleAuth:ClientId" "invalid_client_id"
   ```
2. Restart API
3. Attempt Google login
4. **Expected:** Error response from Google or redirect with error
5. **Expected:** API logs show clear error message
6. **Verify:** User sees meaningful error (not internal exception)
7. Restore correct Client ID
8. Retry login
9. **Expected:** Success

**Expected Results:**
- ✅ Invalid config detected
- ✅ Clear error logged in API
- ✅ User sees generic error (not exposing secrets)
- ✅ System remains stable (no crashes)

**Success Criteria:** Misconfiguration handled without exposing sensitive details

---

### Test 5.3: Network/Timeout Errors

**Objective:** Verify handling of network failures during Google OAuth

**Test Flow:**
1. Initiate Google login
2. **Simulate:** Disconnect network during Google authentication
3. **Alternative:** Use browser DevTools → Network → Offline mode
4. **Expected:** Timeout or network error
5. **Expected:** Error message: "Network error. Please check your connection."
6. **Verify:** No hanging state (request times out appropriately)
7. Restore network
8. Retry login
9. **Expected:** Success

**Expected Results:**
- ✅ Network errors detected
- ✅ Timeout enforced (no infinite wait)
- ✅ User-friendly error message
- ✅ Retry works after network restoration

**Success Criteria:** Network failures handled gracefully with retry capability

---

### Test 5.4: Redis Connection Failure

**Objective:** Verify error handling when Redis is unavailable

**Test Flow:**
1. Stop Redis service:
   ```bash
   # Linux
   sudo systemctl stop redis
   # Windows
   net stop Redis
   # Docker
   docker stop redis-container
   ```
2. Attempt Google login and complete OAuth flow
3. **Expected:** Error during code generation (callback fails)
4. **Expected:** Error logged in API: "Failed to connect to Redis"
5. **Expected:** User sees generic error: "Authentication failed. Please try again."
6. **Verify:** No code stored (since Redis unavailable)
7. Start Redis service
8. Retry login
9. **Expected:** Success

**Expected Results:**
- ✅ Redis failure detected
- ✅ Clear error logged
- ✅ User sees generic error (not exposing infrastructure)
- ✅ System recovers when Redis restored

**Success Criteria:** Redis failures don't crash system, provide clear logs

---

## 6. Edge Cases & Boundary Tests

### Test 6.1: Simultaneous Login Attempts

**Objective:** Verify system handles concurrent login attempts from same user

**Test Flow:**
1. Open two browser tabs/windows (incognito mode)
2. Initiate Google login in both tabs simultaneously
3. Complete OAuth in Tab 1
4. Complete OAuth in Tab 2 (shortly after Tab 1)
5. **Expected:** Both tabs result in successful login
6. **Verify:** Only one user record created (not duplicates)
7. **Verify:** Both tabs have valid access tokens
8. **Verify:** Only one external login record exists

**Expected Results:**
- ✅ Concurrent logins handled
- ✅ No duplicate user records
- ✅ Both sessions valid
- ✅ No race conditions

**Success Criteria:** Concurrent operations handled safely without duplicates

---

### Test 6.2: Email Case Sensitivity

**Objective:** Verify email matching is case-insensitive for auto-link

**Test Flow:**
1. Register user with email: `TestUser@Gmail.com` (mixed case) via password
2. Logout
3. Attempt Google login with email: `testuser@gmail.com` (lowercase)
4. **Expected:** Auto-link occurs (case-insensitive match)
5. **Verify:** No duplicate user created
6. **Verify:** External login linked to existing user

**Expected Results:**
- ✅ Case-insensitive email matching
- ✅ Auto-link works regardless of case
- ✅ No duplicates created

**Success Criteria:** Email matching is case-insensitive (standard best practice)

---

### Test 6.3: Special Characters in Display Name

**Objective:** Verify handling of special characters from Google profile

**Test Flow:**
1. Use Google account with special characters in name:
   - Examples: `José García`, `李明`, `O'Brien`, `Müller`
2. Complete Google login (new user)
3. **Verify:** DisplayName stored correctly in database:
   ```sql
   SELECT DisplayName FROM AspNetUsers WHERE Email = '<email>';
   ```
4. **Verify:** Special characters render correctly in UI
5. **Verify:** No encoding issues or data truncation

**Expected Results:**
- ✅ Special characters stored correctly (UTF-8)
- ✅ No data loss or corruption
- ✅ Characters render properly in UI
- ✅ Database collation supports Unicode

**Success Criteria:** Unicode characters handled properly throughout system

---

### Test 6.4: Missing or Empty Google Claims

**Objective:** Verify graceful handling when Google doesn't provide expected claims

**Test Flow:**
1. **Simulate:** Mock Google OAuth response with missing `name` claim
   - This requires modifying `ExternalAuthService` temporarily to handle null names
2. Complete Google login
3. **Expected:** Fallback to email prefix for DisplayName:
   - Email: `testuser@gmail.com` → DisplayName: `testuser`
4. **Verify:** User created successfully despite missing claim
5. **Verify:** No null reference exceptions

**Expected Results:**
- ✅ Missing claims handled gracefully
- ✅ Fallback logic works
- ✅ User creation succeeds
- ✅ No exceptions or crashes

**Success Criteria:** System resilient to missing or incomplete data from Google

---

### Test 6.5: Large Number of External Logins

**Objective:** Verify performance with user having multiple external logins (future-proofing)

**Test Flow:**
1. Create user with password
2. Link Google account
3. **Future:** If/when adding Facebook, Microsoft, etc., link those too
4. Navigate to external logins page
5. **Verify:** All providers displayed correctly
6. **Verify:** Page loads quickly (no performance degradation)
7. Unlink and re-link each provider
8. **Verify:** Operations succeed without errors

**Expected Results:**
- ✅ Multiple external logins supported
- ✅ UI handles multiple providers
- ✅ No performance issues
- ✅ Future-proof for additional providers

**Success Criteria:** Architecture supports multiple OAuth providers

---

## 7. Integration Tests

### Test 7.1: End-to-End User Journey

**Objective:** Complete realistic user journey from registration to logout

**Test Flow:**
1. **New User Registration:**
   - Navigate to login page
   - Click "Sign in with Google"
   - Complete Google OAuth
   - **Verify:** Redirect to dashboard
   - **Verify:** User info displayed correctly (name, email)
2. **Access Protected Routes:**
   - Navigate to profile page (authenticated route)
   - **Verify:** Access granted
   - Navigate to settings page
   - **Verify:** Access granted
3. **Make API Calls:**
   - Trigger API call requiring authentication (e.g., get user data)
   - **Verify:** JWT token sent in Authorization header
   - **Verify:** API returns data successfully
4. **Token Refresh:**
   - Wait for access token expiration (if short-lived)
   - Make another API call
   - **Verify:** Token automatically refreshed (refresh cookie used)
   - **Verify:** New access token obtained
5. **Logout:**
   - Click logout button
   - **Verify:** Redirect to login page
   - **Verify:** Access token cleared
   - **Verify:** Refresh cookie invalidated
6. **Post-Logout:**
   - Attempt to access protected route
   - **Verify:** Redirect to login page
   - Attempt API call with old token
   - **Verify:** 401 Unauthorized response

**Expected Results:**
- ✅ Complete journey works seamlessly
- ✅ All authenticated features accessible
- ✅ Token refresh automatic
- ✅ Logout clears session completely
- ✅ Post-logout access denied

**Success Criteria:** Full user journey from registration to logout functions correctly

---

### Test 7.2: Cookie Synchronization Across Tabs

**Objective:** Verify refresh cookie works consistently across browser tabs

**Test Flow:**
1. Open Tab 1: Complete Google login
2. Open Tab 2: Navigate to authenticated route
3. **Verify:** Tab 2 authenticated (shared cookie)
4. In Tab 1: Make API call to trigger token refresh
5. In Tab 2: Make API call
6. **Verify:** Tab 2 uses updated refresh cookie
7. In Tab 1: Logout
8. In Tab 2: Make API call
9. **Expected:** Tab 2 receives 401 (cookie invalidated)

**Expected Results:**
- ✅ Cookie shared across tabs
- ✅ Refresh works consistently
- ✅ Logout invalidates all tabs
- ✅ Synchronization works properly

**Success Criteria:** Cookie-based authentication consistent across browser tabs

---

### Test 7.3: API Endpoint Authorization

**Objective:** Verify all external auth endpoints have correct authorization requirements

**Test Flow:**
1. **Anonymous Endpoints (should work without auth):**
   - GET `/api/externalauth/google` → ✅ Should redirect
   - GET `/api/externalauth/google/callback` → ✅ Should process callback
   - POST `/api/externalauth/exchange` → ✅ Should exchange code
2. **Authenticated Endpoints (should require auth):**
   - GET `/api/externalauth/google/link` → ❌ Should return 401 if not authenticated
   - GET `/api/externalauth/google/link/callback` → ❌ Should return 401 if not authenticated
   - DELETE `/api/externalauth/google/unlink` → ❌ Should return 401 if not authenticated
   - GET `/api/externalauth/providers` → ❌ Should return 401 if not authenticated
3. Test each authenticated endpoint without token:
   ```bash
   curl -X GET http://localhost:44369/api/externalauth/google/link
   # Expected: 401 Unauthorized
   ```
4. Test each authenticated endpoint with valid token:
   ```bash
   curl -X GET http://localhost:44369/api/externalauth/google/link \
     -H "Authorization: Bearer <valid-token>"
   # Expected: Redirect to Google
   ```

**Expected Results:**
- ✅ Anonymous endpoints accessible without auth
- ✅ Authenticated endpoints blocked without token
- ✅ Authenticated endpoints accessible with valid token
- ✅ Proper 401 responses with error messages

**Success Criteria:** All endpoints have correct authorization requirements

---

## 8. Performance & Load Tests

### Test 8.1: Code Generation Performance

**Objective:** Verify auth code generation handles concurrent requests

**Test Flow:**
1. Use load testing tool (e.g., Apache Bench, k6, or custom script)
2. Simulate 100 concurrent OAuth callbacks (code generation)
3. **Verify:** All requests succeed
4. **Measure:** Response time for code generation
5. **Expected:** < 500ms per request
6. **Verify:** No Redis connection exhaustion
7. **Verify:** No duplicate codes generated

**Expected Results:**
- ✅ 100 concurrent requests handled
- ✅ Response time acceptable (< 500ms)
- ✅ No errors or timeouts
- ✅ Unique codes generated

**Success Criteria:** System handles concurrent OAuth callbacks efficiently

---

### Test 8.2: Code Expiration Cleanup

**Objective:** Verify Redis TTL automatically cleans up expired codes

**Test Flow:**
1. Generate 1000 auth codes (simulate 1000 OAuth callbacks)
2. **Monitor:** Redis memory usage
3. **Wait:** 61 seconds (code expiration)
4. **Verify:** Redis memory decreases (keys expired)
5. Check Redis key count:
   ```bash
   redis-cli KEYS external-auth-code:*
   # Should return empty or minimal keys
   ```
6. **Verify:** No manual cleanup required

**Expected Results:**
- ✅ Expired codes automatically removed
- ✅ Redis memory freed
- ✅ No memory leaks
- ✅ TTL mechanism working

**Success Criteria:** Redis TTL efficiently cleans up expired codes

---

### Test 8.3: Database Performance with Large User Base

**Objective:** Verify performance with large number of users and external logins

**Test Flow:**
1. **Setup:** Seed database with 100,000 users and external logins
2. Perform Google login (new user)
3. **Measure:** Time for `FindOrCreateUserAsync` to check existing email
4. **Expected:** < 100ms (assuming proper indexes)
5. Verify database indexes exist:
   ```sql
   -- Check index on AspNetUsers.Email
   -- Check index on AspNetUserLogins (LoginProvider, ProviderKey)
   ```
6. Perform explicit link operation
7. **Measure:** Time for duplicate check
8. **Expected:** < 100ms

**Expected Results:**
- ✅ Queries performant with large dataset
- ✅ Proper indexes in place
- ✅ No N+1 query issues
- ✅ Response times acceptable

**Success Criteria:** Database operations remain fast at scale

---

## 9. Automated Test Recommendations

### 9.1 Unit Tests

**ExternalAuthCodeService Tests:**
```csharp
[Fact]
public async Task GenerateCodeAsync_CreatesUniqueCode() { }

[Fact]
public async Task ValidateAndConsumeCodeAsync_ValidCode_ReturnsUser() { }

[Fact]
public async Task ValidateAndConsumeCodeAsync_ExpiredCode_ReturnsNull() { }

[Fact]
public async Task ValidateAndConsumeCodeAsync_SingleUse_SecondCallFails() { }
```

**ExternalAuthService Tests:**
```csharp
[Fact]
public async Task FindOrCreateUserAsync_NewUser_CreatesUser() { }

[Fact]
public async Task FindOrCreateUserAsync_ExistingEmail_AutoLinks() { }

[Fact]
public async Task LinkExternalLoginAsync_DuplicateProvider_ThrowsException() { }

[Fact]
public async Task UnlinkExternalLoginAsync_OnlyLoginMethod_ThrowsException() { }
```

---

### 9.2 Integration Tests

**ExternalAuthController Tests:**
```csharp
[Fact]
public async Task GoogleCallback_ValidCode_RedirectsWithCode() { }

[Fact]
public async Task ExchangeCode_ValidCode_ReturnsAccessToken() { }

[Fact]
public async Task ExchangeCode_ExpiredCode_Returns400() { }

[Fact]
public async Task LinkGoogle_NotAuthenticated_Returns401() { }

[Fact]
public async Task UnlinkGoogle_OnlyLoginMethod_Returns400() { }
```

---

## 10. Test Completion Checklist

### Core Functionality
- [ ] New user registration via Google (Test 1.1)
- [ ] Existing user login via Google (Test 1.2)
- [ ] Code exchange mechanism (Test 1.3)
- [ ] Auto-link existing email account (Test 2.1)
- [ ] Explicit link Google account (Test 3.1)
- [ ] Unlink Google account (Test 3.2)
- [ ] Prevent unlink of only login method (Test 3.3)

### Security
- [ ] Return URL validation (Test 4.1)
- [ ] Auth code expiration (Test 4.2)
- [ ] Single-use code enforcement (Test 4.3)
- [ ] State parameter CSRF protection (Test 4.4)
- [ ] Cookie security attributes (Test 4.5)
- [ ] Duplicate Google account prevention (Test 4.6)

### Error Handling
- [ ] Google OAuth denial (Test 5.1)
- [ ] Invalid client configuration (Test 5.2)
- [ ] Network/timeout errors (Test 5.3)
- [ ] Redis connection failure (Test 5.4)

### Edge Cases
- [ ] Simultaneous login attempts (Test 6.1)
- [ ] Email case sensitivity (Test 6.2)
- [ ] Special characters in display name (Test 6.3)
- [ ] Missing or empty Google claims (Test 6.4)

### Integration
- [ ] End-to-end user journey (Test 7.1)
- [ ] Cookie synchronization across tabs (Test 7.2)
- [ ] API endpoint authorization (Test 7.3)

### Performance
- [ ] Code generation performance (Test 8.1)
- [ ] Code expiration cleanup (Test 8.2)
- [ ] Database performance at scale (Test 8.3)

---

## 11. Test Reporting Template

For each test, document results using this template:

```markdown
### Test ID: [e.g., 1.1]
**Test Name:** [e.g., New User Registration via Google]
**Date:** [YYYY-MM-DD]
**Tester:** [Name]
**Environment:** [Dev/Staging/Prod]

**Status:** [✅ Pass | ❌ Fail | ⚠️ Partial]

**Results:**
- Expected Result 1: [✅ Pass | ❌ Fail] - [Notes]
- Expected Result 2: [✅ Pass | ❌ Fail] - [Notes]
- ...

**Issues Found:**
- [Issue description, severity, screenshot/logs if applicable]

**Action Items:**
- [Any follow-up tasks or fixes needed]

**Notes:**
- [Additional observations, edge cases discovered, etc.]
```

---

## 12. Sign-Off

Once all tests pass:

- [ ] All core functionality tests passed
- [ ] All security tests passed
- [ ] All error handling tests passed
- [ ] All edge case tests passed
- [ ] All integration tests passed
- [ ] Performance benchmarks met
- [ ] Documentation updated
- [ ] Code reviewed

**Verified By:** ___________________
**Date:** ___________________
**Approved for Production:** [ ] Yes [ ] No

---

## Additional Resources

- **Google OAuth 2.0 Playground:** https://developers.google.com/oauthplayground/
- **JWT Decoder:** https://jwt.io/
- **Redis CLI Reference:** https://redis.io/commands/
- **Browser DevTools Guide:** [Chrome DevTools](https://developer.chrome.com/docs/devtools/)

---

**Document Version:** 1.0
**Last Updated:** 2026-02-11
**Next Review:** After implementation completion
