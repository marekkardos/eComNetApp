```
Login → HttpOnly cookie (refresh token, 7-30 days)
      + Response body (access token, 15 min)
        ↓
Angular stores access token in memory only (not localStorage)
        ↓
Token expires → Call /refresh → Cookie auto-sent → New access token
```


**Flow:**
1. User logs in → receives short-lived access token in response body
2. Access token stored **in memory only** (not localStorage) - protects against XSS
3. Refresh token stored in HttpOnly cookie by API (automatically sent with requests)
4. On app startup → calls `/api/account/refresh` to get new access token if refresh token exists
5. On 401 error → automatically calls refresh endpoint → retries original request

**Security Features:**
- Access tokens never stored in localStorage
- Automatic token refresh on app initialization
- Request retry after token refresh
- Clean logout clears in-memory token


---
  ###  Layer 1 — The cookie Path (server-side scope)

  From CookieExtensions.cs:
  Path = "/api"
  This tells the browser: "send this cookie to any request under /api/*." On its own, this means /api/orders,
  /api/payments/*, etc. would all receive the cookie. The Path does not restrict it to /refresh alone.

  ---
  ###  Layer 2 — withCredentials (the actual gate, client-side)

  Angular and the API run on different origins (e.g. localhost:4200 vs localhost:44370). For cross-origin requests,
  browsers block cookies by default unless the request explicitly opts in with withCredentials: true.

  Look at where withCredentials: true is set in account.service.ts:

  account.service.ts:76   login()        → withCredentials: true
  account.service.ts:83   register()     → withCredentials: true
  account.service.ts:95   refreshToken() → withCredentials: true
  account.service.ts:112  logout()       → withCredentials: true

  And where it is not:
  account.service.ts:131  checkEmailExists()    → no withCredentials
  account.service.ts:135  getUserAddress()      → no withCredentials
  account.service.ts:139  updateUserAddress()   → no withCredentials

  The jwt.interceptor.ts also never adds withCredentials — it only adds the Authorization: Bearer header. 
  So every other API call (orders, payments, shop, etc.) goes out without cookies.

  ---
  The full flow, end to end:

  1. POST /account/login          ← withCredentials: true
         ↓
  2. Server sets HttpOnly cookie "refreshToken"   (Path="/api")
     Server returns access token in response body
         ↓
  3. account.service stores access token IN MEMORY only
         ↓
  4. GET /orders, POST /payments/...
         ↓
  5. jwt.interceptor adds:  Authorization: Bearer <access-token>
     No withCredentials → browser does NOT send the cookie
         ↓
  6. Access token expires → API returns 401
         ↓
  7. jwt.interceptor catches 401 → calls accountService.refreshToken()
         ↓
  8. POST /account/refresh        ← withCredentials: true
     Browser sends the HttpOnly cookie automatically
         ↓
  9. Server reads Request.Cookies["refreshToken"], validates, rotates,
     sets a NEW cookie, returns a new access token in the body
         ↓
  10. jwt.interceptor retries the original failed request with the new token

  ---
  One thing to be aware of: this scoping relies on the cross-origin gap between Angular and the API. If you ever deploy
  them on the same origin (same host and port), withCredentials becomes irrelevant — the browser sends cookies
  automatically based on Path alone. In that scenario, the refresh token cookie would travel with /api/orders and every
  other request. It wouldn't break anything (only /account/refresh reads Request.Cookies["refreshToken"]).