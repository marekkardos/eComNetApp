# Phase 0: Pre-Migration Documentation

**Generated**: 2026-01-26
**Status**: Complete

---

## 0.1 Angular 9 Features Review

### Application Structure

```
src/app/
├── account/          # User authentication module
├── basket/           # Shopping cart module
├── checkout/         # Checkout flow module
├── core/             # Core singleton services and components
├── home/             # Home page module
├── orders/           # Order history module
├── shared/           # Shared components, models, pipes
└── shop/             # Product catalog module
```

### Feature Modules

| Module | Components | Services | Routes |
|--------|------------|----------|--------|
| **Account** | LoginComponent, RegisterComponent | AccountService | /account/login, /account/register |
| **Basket** | BasketComponent | BasketService | /basket |
| **Checkout** | CheckoutComponent, CheckoutAddressComponent, CheckoutDeliveryComponent, CheckoutPaymentComponent, CheckoutReviewComponent, CheckoutSuccessComponent | CheckoutService | /checkout |
| **Home** | HomeComponent | - | / (redirects to /shop) |
| **Orders** | OrdersComponent, OrderDetailedComponent | OrdersService | /orders, /orders/:id |
| **Shop** | ShopComponent, ProductItemComponent, ProductDetailsComponent | ShopService | /shop, /shop/:id |

### Core Module Components

| Component | Purpose |
|-----------|---------|
| NavBarComponent | Main navigation with cart badge and user menu |
| SectionHeaderComponent | Page header with breadcrumb |
| NotFoundComponent | 404 error page |
| ServerErrorComponent | 500 error page |
| TestErrorComponent | Development error testing |

### Core Services

| Service | Purpose | State Management |
|---------|---------|------------------|
| AccountService | Authentication, token management | BehaviorSubject/ReplaySubject |
| BusyService | Loading spinner state | NgxSpinner |

### Interceptors

| Interceptor | Purpose |
|-------------|---------|
| JwtInterceptor | Attach JWT token, handle 401 with refresh |
| ErrorInterceptor | Global error handling and toasts |
| LoadingInterceptor | Show/hide loading spinner |

### Guards

| Guard | Purpose |
|-------|---------|
| AuthGuard | Protect routes requiring authentication |

### Shared Components

| Component | Purpose |
|-----------|---------|
| TextInputComponent | Form input wrapper with validation |
| PagerComponent | Pagination using ngx-bootstrap |
| PagingHeaderComponent | "Showing X - Y of Z results" |
| OrderTotalsComponent | Display basket/order totals |
| BasketSummaryComponent | Display basket items |
| StepperComponent | Custom CDK stepper for checkout |

### Shared Models

| Model | Fields |
|-------|--------|
| IUser | email, displayName, token |
| IAddress | firstName, lastName, street, city, state, zipcode, country |
| IProduct | id, name, description, price, pictureUrl, productType, productBrand |
| IBasket | id, items[], clientSecret?, paymentIntentId?, deliveryMethodId?, shippingPrice? |
| IBasketItem | id, productName, price, quantity, pictureUrl, brand, type |
| IBasketTotals | shipping, subtotal, total |
| IOrder | id, buyerEmail, orderDate, shipToAddress, deliveryMethod, shippingPrice, orderItems, subtotal, total, status |
| IPagination | pageIndex, pageSize, count, data[] |
| ShopParams | brandId, typeId, sort, pageNumber, pageSize, search |
| IDeliveryMethod | id, shortName, deliveryTime, description, price |
| IBrand | id, name |
| IType | id, name |

---

## 0.2 Deprecated APIs and Patterns

### Angular 9 → Angular 21 Breaking Changes

| Angular 9 Pattern | Angular 21 Replacement | Migration Notes |
|-------------------|------------------------|-----------------|
| `@NgModule` declarations | Standalone components | Remove NgModules, use `standalone: true` |
| `*ngIf`, `*ngFor` | `@if`, `@for`, `@switch` | New control flow syntax |
| `constructor(private svc: Svc)` | `inject(Svc)` | Functional DI preferred |
| `BehaviorSubject` for state | `signal()` | Signals for reactive state |
| `HttpClientModule` | `provideHttpClient()` | Functional providers |
| `RouterModule.forRoot()` | `provideRouter()` | Functional router config |
| Class-based interceptors | Functional interceptors (`HttpInterceptorFn`) | Complete rewrite |
| Class-based guards | Functional guards (`CanActivateFn`) | Complete rewrite |
| `@Input()` decorator | `input()` / `input.required()` | Signal inputs |
| `@Output()` decorator | `output()` | Signal outputs |
| `.toPromise()` | `firstValueFrom()` / `lastValueFrom()` | RxJS 7+ change |

### RxJS 6 → RxJS 7+ Changes

```typescript
// Before (RxJS 6)
import { throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

// After (RxJS 7+)
import { throwError, catchError, map } from 'rxjs';
```

### Deprecated Dependencies

| Current Package | Version | Replacement | Notes |
|-----------------|---------|-------------|-------|
| `bootstrap` | 4.1.1 | Tailwind CSS | Utility-first CSS |
| `ngx-bootstrap` | 5.3.2 | @angular/material | Material components |
| `font-awesome` | 4.7.0 | Material Icons | Bundled with Material |
| `jquery` | 3.5.1 | Remove | Not needed with Angular |
| `popper.js` | 1.16.1 | Remove | Bootstrap dependency |
| `uuid` | 3.4.0 | `crypto.randomUUID()` | Browser API |
| `bootswatch` | 4.4.1 | Custom Tailwind theme | Tailwind config |

---

## 0.3 API Contract Documentation

### Account API

| Method | Endpoint | Request Body | Response | Notes |
|--------|----------|--------------|----------|-------|
| POST | `/api/account/login` | `{ email, password }` | `IUser` | Sets HttpOnly refresh cookie |
| POST | `/api/account/register` | `{ displayName, email, password }` | `IUser` | Sets HttpOnly refresh cookie |
| POST | `/api/account/refresh` | (empty) | `IUser` | Uses HttpOnly cookie |
| POST | `/api/account/logout` | (empty) | void | Clears HttpOnly cookie |
| GET | `/api/account/emailexists?email=` | - | `boolean` | Email validation |
| GET | `/api/account/address` | - | `IAddress` | Requires auth |
| PUT | `/api/account/address` | `IAddress` | `IAddress` | Requires auth |

### Products API

| Method | Endpoint | Query Params | Response |
|--------|----------|--------------|----------|
| GET | `/api/products` | brandId, typeId, sort, pageIndex, pageSize, search | `IPagination<IProduct>` |
| GET | `/api/products/:id` | - | `IProduct` |
| GET | `/api/products/brands` | - | `IBrand[]` |
| GET | `/api/products/types` | - | `IType[]` |

### Basket API

| Method | Endpoint | Request Body | Response |
|--------|----------|--------------|----------|
| GET | `/api/basket/:id` | - | `IBasket` |
| POST | `/api/basket` | `IBasket` | `IBasket` |
| DELETE | `/api/basket/:id` | - | void |

### Payments API

| Method | Endpoint | Request Body | Response |
|--------|----------|--------------|----------|
| POST | `/api/payments/:basketId` | (empty) | `IBasket` (with clientSecret) |

### Orders API

| Method | Endpoint | Request Body | Response | Auth |
|--------|----------|--------------|----------|------|
| POST | `/api/orders` | `IOrderToCreate` | `IOrder` | Required |
| GET | `/api/orders` | - | `IOrder[]` | Required |
| GET | `/api/orders/:id` | - | `IOrder` | Required |
| GET | `/api/orders/deliveryMethods` | - | `IDeliveryMethod[]` | No |

### Authentication Flow

1. **Login/Register**: Returns access token in response body, sets HttpOnly refresh cookie
2. **Token Storage**: Access token stored in-memory only (XSS protection)
3. **API Requests**: JwtInterceptor attaches Bearer token
4. **401 Handling**: JwtInterceptor calls refresh endpoint, retries request
5. **App Init**: Calls `/account/refresh` to restore session from cookie

---

## 0.4 Comparison Testing Environment

### Port Configuration

| Application | Port | URL | Purpose |
|-------------|------|-----|---------|
| Angular 9 | 4200 | http://localhost:4200 | Reference baseline |
| Angular 21 | 4201 | http://localhost:4201 | New implementation |
| API (Local) | 5001 | https://localhost:5001 | Backend |
| API (Docker) | 44369 | http://localhost:44369 | Container backend |

### Running Both Apps

```bash
# Terminal 1 - Angular 9
cd src/client
npm start

# Terminal 2 - Angular 21
cd src/client-v21
npm start
```

---

## 0.5 Node.js and TypeScript Requirements

### Current Versions (Angular 9)

| Tool | Version | Required By |
|------|---------|-------------|
| Node.js | 12.x | Angular 9 |
| TypeScript | 3.7.5 | Angular 9 |
| Angular CLI | 9.1.15 | - |

### Target Versions (Angular 21)

| Tool | Version | Notes |
|------|---------|-------|
| Node.js | ^20.19.0 \|\| ^22.12.0 \|\| ^24.0.0 | Required for Angular 21 |
| TypeScript | >=5.9.0 <6.0.0 | Required for Angular 21 |
| Angular CLI | 21.x | Latest |

### Verification Command

```bash
# Check Node.js version
node --version

# Required: v20.19.0+, v22.12.0+, or v24.0.0+
```

---

## 0.6 Environment Configurations

### Environment Files

| File | API URL | Purpose |
|------|---------|---------|
| `environment.ts` | `http://localhost:44369/api/` | Default (API in Docker) |
| `environment.local.template.ts` | `https://localhost:5001/api/` | Local full-stack dev |
| `environment.container.template.ts` | `http://localhost:44369/api/` | Angular in Docker |
| `environment.stage.ts` | Production URL | Staging/Production |

### Configuration Values

```typescript
export const environment = {
  production: boolean,
  apiUrl: string,           // Backend API base URL
  stripeSettings: {
    PublishableKey: string  // Stripe frontend key
  }
};
```

### Angular 21 Environment Structure

```typescript
// Recommended structure for Angular 21
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api/',
  stripe: {
    publishableKey: 'REPLACE_WITH_YOUR_KEY'
  }
};
```

---

## 0.7 Feature Parity Checklist

### User-Facing Features

| Feature | Priority | Status |
|---------|----------|--------|
| **Home Page** | | |
| Hero section | Medium | ☐ |
| **Shop** | | |
| Product grid display | High | ☐ |
| Filter by brand | High | ☐ |
| Filter by type | High | ☐ |
| Search products | High | ☐ |
| Sort products (name, price) | High | ☐ |
| Pagination | High | ☐ |
| Product detail page | High | ☐ |
| Add to cart button | High | ☐ |
| **Account** | | |
| Login form | High | ☐ |
| Register form | High | ☐ |
| Email validation (async) | High | ☐ |
| Session persistence (refresh) | High | ☐ |
| Logout | High | ☐ |
| **Basket** | | |
| View basket items | High | ☐ |
| Update quantity (+/-) | High | ☐ |
| Remove item | High | ☐ |
| Basket totals | High | ☐ |
| Persist basket ID | High | ☐ |
| **Checkout** | | |
| Address form | High | ☐ |
| Save address | High | ☐ |
| Delivery method selection | High | ☐ |
| Order review | High | ☐ |
| Stripe payment integration | High | ☐ |
| Order success page | High | ☐ |
| **Orders** | | |
| Order list | High | ☐ |
| Order detail | High | ☐ |
| Order status display | High | ☐ |
| **Navigation** | | |
| Nav bar with logo | High | ☐ |
| Shop link | High | ☐ |
| Cart icon with badge | High | ☐ |
| User dropdown menu | High | ☐ |
| Login/Register buttons | High | ☐ |
| **Global** | | |
| Breadcrumb navigation | Medium | ☐ |
| Loading spinner | Medium | ☐ |
| Toast notifications | Medium | ☐ |
| 404 page | Medium | ☐ |
| 500 error page | Medium | ☐ |
| Responsive design | High | ☐ |

### Protected Routes

| Route | Guard |
|-------|-------|
| `/checkout` | AuthGuard |
| `/orders` | AuthGuard |

---

## Summary

Phase 0 documentation complete. Key findings:

1. **6 Feature Modules** to migrate: Account, Basket, Checkout, Home, Orders, Shop
2. **3 Interceptors** to rewrite as functional: JWT, Error, Loading
3. **1 Guard** to rewrite as functional: AuthGuard
4. **6 Shared Components** to port as standalone
5. **12+ Models** to preserve (interfaces remain compatible)
6. **Node.js upgrade required**: 12.x → 20.x+
7. **TypeScript upgrade required**: 3.7 → 5.9+

Ready to proceed to **Phase 1: Angular 21 Project Scaffolding**.
