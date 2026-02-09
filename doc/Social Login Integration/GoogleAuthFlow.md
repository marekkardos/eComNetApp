```
Click "Google Login"
  → API redirects to Google (sets correlation cookie)
    → Google consent screen → user approves
      → Google redirects to /signin-google (middleware handles OAuth exchange)
        → Middleware redirects to GoogleCallback action
          → API: find/create user, set refresh token cookie, generate short-lived code
            → Redirect to Angular with ?code=xyz
              → Angular: POST /exchange { code } → gets access token
```

---
### Layer 1 — Two callback paths (middleware vs controller)

The Google OAuth flow uses two different paths in sequence:

`/signin-google` (CallbackPath in IdentityServiceExtensions.cs)
This is where Google redirects the browser after the user approves consent. The ASP.NET Core OAuth **middleware**
intercepts this path silently — it never reaches a controller. The middleware:
  - Validates the state parameter (CSRF protection via Data Protection encryption)
  - Exchanges the authorization code for tokens with Google's token endpoint
  - Reads the user's claims (email, name, profile) from Google
  - Stores the result in a temporary `Identity.External` cookie
  - Redirects the browser to the `RedirectUri` from `AuthenticationProperties`

`/api/externalauth/google/callback` (GoogleCallback action in ExternalAuthController.cs)
This is where the middleware redirects **after** it finishes the OAuth protocol. This is application code that:
  - Reads the external login info from the `Identity.External` cookie via `SignInManager.GetExternalLoginInfoAsync()`
  - Finds or creates a user (auto-links if an account with the same verified email exists)
  - Generates a short-lived code (60s TTL, single-use, stored in Redis)
  - Sets the refresh token in an HttpOnly cookie
  - Cleans up the temporary `Identity.External` cookie
  - Redirects to the Angular frontend with `?code=xyz`

---
### Layer 2 — Cookies in play during the flow

There are **four cookies** involved at various stages:

| Cookie | Set by | Lifetime | Purpose |
|--------|--------|----------|---------|
| `.AspNetCore.Correlation.*` | OAuth middleware (Challenge) | ~15 min | CSRF protection: correlates the outgoing Challenge with the incoming callback from Google. Deleted after validation. |
| `Identity.External` | OAuth middleware (Callback) | 5 min | Temporary storage of Google's claims between `/signin-google` and `/api/externalauth/google/callback`. Deleted after `GetExternalLoginInfoAsync()` reads it. |
| `refreshToken` | GoogleCallback action | 7 days | HttpOnly; Secure; SameSite=Strict; Path=/api. The long-lived refresh token — same cookie as the normal login/password flow. |
| (none for access token) | — | — | Access token lives in Angular memory only, never in a cookie. |

The correlation and Identity.External cookies are transient — they exist only during the OAuth redirect dance
and are cleaned up before the user reaches the Angular app.

---
### Layer 3 — The code exchange pattern (BFF)

The API cannot return the access token directly in the redirect URL because:
  - Redirect URLs are visible in browser history, server logs, and the Referer header
  - Tokens in URLs are a known security anti-pattern (OWASP)

Instead, the API uses a **short-lived authorization code** (similar to OAuth's own authorization code concept):

1. GoogleCallback generates a 256-bit cryptographically random code
2. Stores `{ userId, createdAt }` in Redis with key `external_auth_code:{code}`, TTL 60 seconds
3. Redirects to Angular: `http://localhost:4200/account/login?code=xyz`
4. Angular detects the `code` query param and immediately calls `POST /api/externalauth/exchange { code }`
5. API validates the code (single-use — deleted from Redis on read), looks up the user, returns the access token

The refresh token cookie was already set in step 1 (during GoogleCallback), so after the exchange,
Angular has both tokens — exactly like the normal login flow.

---
### The full flow, end to end:

```
1.  User clicks "Sign in with Google" button in Angular
        ↓
2.  Angular navigates to:
    GET /api/externalauth/google?returnUrl=http://localhost:4200/account/login?returnUrl=%2Fshop
        ↓
3.  ExternalAuthController.GoogleLogin validates returnUrl, calls Challenge()
    Response: 302 → https://accounts.google.com/o/oauth2/v2/auth?client_id=...&state=...&redirect_uri=...
    Sets cookie: .AspNetCore.Correlation.{id}=N  (Path=/signin-google; Secure; SameSite=Lax; HttpOnly)
        ↓
4.  User sees Google consent screen, clicks "Continue"
        ↓
5.  Google redirects:
    GET /signin-google?state=...&code=GOOGLE_AUTH_CODE&scope=email+profile+openid
    Browser sends the correlation cookie
        ↓
6.  OAuth MIDDLEWARE intercepts /signin-google (this never reaches a controller):
    - Decrypts state using Data Protection → gets AuthenticationProperties + correlation ID
    - Validates correlation cookie matches → deletes the correlation cookie
    - Sends GOOGLE_AUTH_CODE to Google's token endpoint → gets id_token + access_token
    - Reads user claims from Google (email, name, picture)
    - Signs into IdentityConstants.ExternalScheme → sets Identity.External cookie
    - Redirects to RedirectUri from properties:
      302 → /api/externalauth/google/callback?returnUrl=http://localhost:4200/...
        ↓
7.  ExternalAuthController.GoogleCallback:
    a. signInManager.GetExternalLoginInfoAsync() → reads Identity.External cookie → gets claims
    b. externalAuthService.FindOrCreateUserAsync(info):
       - Checks if user has this Google login linked → sign in
       - OR finds user with same verified email → auto-links Google login
       - OR creates a new user with Google's email/name
    c. externalAuthCodeService.GenerateCodeAsync(user) → 256-bit random code, stored in Redis (60s TTL)
    d. tokenService.CreateToken(user) → generates JWT (for the JTI claim needed by refresh token)
    e. refreshTokenService.GenerateRefreshTokenAsync(user, jwtId) → creates refresh token in DB
    f. Response.SetRefreshTokenCookie(refreshToken, ...) → HttpOnly cookie (Path=/api; SameSite=Strict)
    g. SignOutAsync(IdentityConstants.ExternalScheme) → deletes the temporary Identity.External cookie
    h. Redirect: 302 → http://localhost:4200/account/login?returnUrl=%2Fshop&code=xyz
        ↓
8.  Angular login component detects ?code=xyz in query params
        ↓
9.  Angular calls:
    POST /api/externalauth/exchange  { "code": "xyz" }   ← withCredentials: true
    (refresh token cookie travels automatically because Path=/api matches)
        ↓
10. ExternalAuthController.ExchangeCode:
    a. externalAuthCodeService.ValidateAndConsumeCodeAsync(code):
       - Reads from Redis → deletes immediately (single-use guarantee)
       - Deserializes { userId } → finds user
    b. tokenService.CreateToken(user) → new access token
    c. Returns: { email, displayName, token: "eyJ..." }
        ↓
11. Angular stores access token in memory (accountService.currentUserSource)
    Normal app flow begins — jwt.interceptor adds Bearer header to API calls
        ↓
12. Access token expires → jwt.interceptor catches 401 → calls refreshToken()
    POST /api/account/refresh  ← withCredentials: true
    The refresh token cookie (set in step 7f) is sent automatically
    → Server rotates refresh token, returns new access token
    (identical to the normal login/password refresh flow from here on)
```

---
### How this compares to the normal login/password flow

| Aspect | Login/Password | Google OAuth |
|--------|---------------|--------------|
| Entry point | `POST /api/account/login` | `GET /api/externalauth/google` → Google → callback chain |
| Identity verification | Server checks password hash | Google verifies identity, returns signed claims |
| Refresh token cookie | Set in the login response | Set in GoogleCallback (step 7f) |
| Access token delivery | In the login response body | Via code exchange (steps 8-10) — cannot put tokens in redirect URLs |
| Ongoing token refresh | `POST /api/account/refresh` | Same — identical from step 12 onward |
| Cookies during auth | None (just the POST body) | Correlation + Identity.External (both transient, cleaned up) |
| withCredentials needed | login, register, refresh, logout | exchange, refresh, logout |

After the initial authentication, both flows converge: the Angular app holds an access token in memory
and a refresh token in an HttpOnly cookie. The rest of the application (orders, payments, shop) works
identically regardless of how the user originally logged in.

---
### Security notes

- **State parameter**: Encrypted with Data Protection (AES-256-CBC + HMACSHA256). Prevents CSRF during the
  OAuth redirect dance. The correlation cookie adds a second layer — even if an attacker intercepts the state,
  they don't have the cookie.
- **Code exchange timing**: The 60-second TTL is generous enough for a slow redirect but tight enough
  that leaked codes (e.g., in server logs) expire quickly.
- **Single-use codes**: Redis delete-on-read prevents replay attacks.
- **Auto-linking by email**: Trusts Google's email verification. If Google says the email is verified
  and a local account exists with that email, the accounts are linked automatically. This is safe because
  Google is a trusted identity provider that verifies email ownership.
- **No client secrets in the browser**: The entire OAuth exchange happens server-side (BFF pattern).
  Angular never sees the Google client secret or any Google tokens.
