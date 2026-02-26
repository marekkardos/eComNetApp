# Plan: Phase 4.3 — Account Feature Wiring

## Context

Phases 4.1 (Home) and 4.2 (Shop) are already complete — both use real API calls with no mock data.
Phase 4.3 Account Feature components are also already built:
- `login.component.ts` — calls `AccountService.login()`, handles returnUrl, busy state
- `register.component.ts` — calls `AccountService.register()`, email exists check, password strength
- `AccountService` — login, register, logout, refreshToken, initializeAuth, JWT token management
- `auth.guard.ts` — redirects to `/account/login?returnUrl=...` if not authenticated

**What's missing** is the plumbing that connects these pieces:
1. Session restoration on app startup (no `provideAppInitializer` yet)
2. Protected routes not guarded (commented-out `canActivate` in app.routes.ts)
3. Navbar uses default change detection — in a zoneless app, signal-based state won't update without OnPush

## Changes

### 1. `src/app/app.config.ts` — Add `provideAppInitializer`
Call `accountService.initializeAuth()` at app startup so the refresh-token cookie restores the
session before the first render. Without this, the navbar shows "Login" on every page refresh even
for logged-in users, and the authGuard can't rely on `isAuthInitialized`.

```typescript
// Add to providers array:
provideAppInitializer(() => {
  const accountService = inject(AccountService);
  return accountService.initializeAuth();
})
```

Imports to add: `provideAppInitializer` from `@angular/core`, `AccountService` from `./core/services/account.service`.

### 2. `src/app/app.routes.ts` — Apply `authGuard` to protected routes
Uncomment/add `canActivate: [authGuard]` on the `/checkout` and `/orders` routes.
Import `authGuard` from `./core/guards/auth.guard`.

```typescript
{
  path: 'checkout',
  loadChildren: () => import('./features/checkout/checkout.routes').then(m => m.CHECKOUT_ROUTES),
  canActivate: [authGuard],
  data: { breadcrumb: 'Checkout' }
},
{
  path: 'orders',
  loadChildren: () => import('./features/orders/orders.routes').then(m => m.ORDERS_ROUTES),
  canActivate: [authGuard],
  data: { breadcrumb: 'Orders' }
},
```

### 3. `src/app/core/components/nav-bar/nav-bar.component.ts` — Add `OnPush`
The app uses `provideZonelessChangeDetection()`. Without `OnPush`, signal reads in the template
(`accountService.currentUser()`, `basketService.itemCount()`) won't trigger re-renders.
Add `changeDetection: ChangeDetectionStrategy.OnPush` to the `@Component` decorator and import
`ChangeDetectionStrategy` from `@angular/core`.

## Files to Modify

| File | Change |
|------|--------|
| `src/app/app.config.ts` | Add `provideAppInitializer` calling `accountService.initializeAuth()` |
| `src/app/app.routes.ts` | Add `canActivate: [authGuard]` to `/checkout` and `/orders` |
| `src/app/core/components/nav-bar/nav-bar.component.ts` | Add `ChangeDetectionStrategy.OnPush` |

## Key Utilities to Reuse

- `AccountService.initializeAuth()` — `src/app/core/services/account.service.ts:53` — already implemented, just needs to be called at startup
- `authGuard` — `src/app/core/guards/auth.guard.ts` — already implemented, just needs to be applied to routes

## No New Files Needed

All services, guards, interceptors, and components already exist. This plan only wires existing pieces together.

## Verification

1. **Session persistence**: Log in → refresh page → navbar should show "Welcome, {name}" (not "Login")
2. **Protected routes**: While logged out, navigate to `/checkout` → should redirect to `/account/login?returnUrl=%2Fcheckout`
3. **Post-login redirect**: After logging in via the redirect, should return to the original route
4. **Login flow**: Submit login form → on success, navigates to `returnUrl` or `/shop`
5. **Register flow**: Fill form, check email uniqueness, submit → navigates to `/shop`
6. **Logout**: Click logout in navbar dropdown → navigates to `/`, navbar shows "Login" again
