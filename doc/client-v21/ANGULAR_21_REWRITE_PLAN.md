# Angular 21 Rewrite - Implementation Plan

**Project:** eComNetApp Frontend Modernization
**Current Version:** Angular 9.0.1
**Target Version:** Angular 21
**Strategy:** Complete rewrite from scratch
**Timeline:** 11 weeks (60-90 hours)
**Date Created:** January 23, 2026

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current App Inventory](#current-app-inventory)
3. [Why Angular 21?](#why-angular-21)
4. [Implementation Strategy](#implementation-strategy)
5. [Phased Implementation Plan](#phased-implementation-plan)
   - [Phase 0: Setup & Foundation](#phase-0-setup--foundation-week-1)
   - [Phase 1: Core Infrastructure](#phase-1-core-infrastructure-week-2)
   - [Phase 2: Authentication Module](#phase-2-authentication-module-week-3)
   - [Phase 3: Shop Module](#phase-3-shop-module-week-4)
   - [Phase 4: Basket Module](#phase-4-basket-module-week-5)
   - [Phase 5: Checkout Module](#phase-5-checkout-module-week-6-7)
   - [Phase 6: Orders Module](#phase-6-orders-module-week-8)
   - [Phase 7: Shared Components & Polish](#phase-7-shared-components--polish-week-9)
   - [Phase 8: Testing & Bug Fixes](#phase-8-testing--bug-fixes-week-10)
   - [Phase 9: Deployment & Cutover](#phase-9-deployment--cutover-week-11)
6. [Feature Parity Checklist](#feature-parity-checklist)
7. [Project Structure](#project-structure)
8. [Technical Stack](#technical-stack)
9. [Risk Mitigation](#risk-mitigation)
10. [Success Criteria](#success-criteria)
11. [Next Steps](#next-steps)

---

## Executive Summary

This document outlines a comprehensive plan to rewrite the eComNetApp frontend from Angular 9 to Angular 21. Rather than performing an incremental upgrade through multiple Angular versions, we will build a completely new Angular 21 application alongside the existing Angular 9 app.

### Key Benefits of This Approach

- **Zero Technical Debt** - Start with modern Angular 21 patterns from day one
- **Zoneless by Default** - Better performance, smaller bundle size (~50KB saved)
- **Signal-Based Architecture** - Modern reactive state management
- **Vitest Integration** - 10x faster test execution
- **16 Months of LTS Support** - Until May 2027
- **Learning Investment** - Team becomes Angular 21 experts
- **Safety Net** - Old app remains available for comparison testing

### Effort Estimate

**Total: 60-90 hours across 11 weeks**

- Foundation & Infrastructure: 18-26 hours
- Feature Development: 38-54 hours
- Testing & Deployment: 18-24 hours

### Timeline

**Start Date:** TBD
**Estimated Completion:** 11 weeks from start
**Target Cutover:** Week 11

---

## Current App Inventory

### Feature Modules (5)

1. **Shop** - Product listing, filtering, sorting, search, details
2. **Basket** - Shopping cart with Redis persistence
3. **Checkout** - Multi-step checkout with Stripe payment
4. **Orders** - Order history and details
5. **Account** - Login, register, JWT auth with refresh tokens

### Components (27)

- **Authentication (2):** Login, Register
- **Home (1):** Landing page
- **Shop (3):** Product listing, Product item card, Product details
- **Basket (1):** Basket view
- **Checkout (6):** Main checkout, Address, Delivery, Payment, Review, Success
- **Orders (2):** Orders list, Order details
- **Core (5):** Nav bar, Section header, Not found (404), Server error (500), Test error
- **Shared (6):** Pager, Paging header, Order totals, Text input, Stepper, Basket summary

### Services (6)

1. **AccountService** - Authentication, user management, refresh token
2. **BasketService** - Shopping cart, Redis integration, totals calculation
3. **ShopService** - Products, brands, types, filtering, pagination
4. **CheckoutService** - Delivery methods, order creation
5. **OrdersService** - Order retrieval
6. **BusyService** - Loading spinner state management

### Infrastructure (4)

- **3 HTTP Interceptors**
  - JWT Interceptor (token attachment, 401 handling, refresh)
  - Error Interceptor (error handling, navigation)
  - Loading Interceptor (busy state management)
- **1 Route Guard**
  - Auth Guard (protect checkout and orders routes)
- **10 TypeScript Models/Interfaces**
- **Multiple Environments** (local, container, stage)

### Third-Party Dependencies

- **ngx-bootstrap** - UI components (pagination, carousel, dropdown)
- **ngx-spinner** - Loading indicator
- **ngx-toastr** - Toast notifications
- **xng-breadcrumb** - Breadcrumb navigation
- **Stripe.js** - Payment processing
- **UUID** - Basket ID generation
- **Bootstrap 4.1.1** - CSS framework
- **Bootswatch** - Bootstrap themes

### Code Statistics

- **117 total files** (TypeScript, HTML, SCSS)
- **27 components**
- **~2,200 lines of TypeScript**
- **~640 lines of HTML templates**
- **Medium-sized, well-structured application**

---

## Why Angular 21?

### Angular 21 Features & Benefits

| Feature | Benefit |
|---------|---------|
| **Zoneless by Default** | 50KB smaller bundles, better performance, predictable change detection |
| **Vitest Test Runner** | 10x faster tests, better DX, hot module reload |
| **Signal-Based Forms (Experimental)** | Type-safe reactive forms, better performance |
| **Angular Aria** | New accessible UI library option |
| **Optimized esbuild** | Faster builds (~50% faster than Angular 19) |
| **LTS Until May 2027** | 16 months of support remaining |

### Comparison: Angular 19 vs Angular 21

| Aspect | Angular 19 | Angular 21 |
|--------|------------|------------|
| **Release Date** | Nov 2024 | Nov 2025 |
| **LTS Support Ends** | May 2026 (4 months) | May 2027 (16 months) |
| **Zoneless** | Optional | Default |
| **Test Runner** | Jasmine/Karma | Vitest (default) |
| **Signal Forms** | Not available | Experimental |
| **Bundle Size** | Includes Zone.js | No Zone.js (-50KB) |
| **Performance** | Good | Better |
| **Developer Experience** | Good | Better |

**Verdict:** Angular 21 is the clear winner for a rewrite project.

### Why Not Incremental Upgrade?

An incremental upgrade from Angular 9 → 21 would require:

- Upgrading through versions: 9 → 12 → 14 → 15 → 17 → 18 → 19 → 21
- Bootstrap 4 → Bootstrap 5 migration
- TSLint → ESLint migration
- Adding `standalone: false` flags throughout
- Testing at each version
- Dealing with breaking changes across 12+ versions
- **Estimated effort: 45-60 hours + future refactoring needed**

Even after upgrading, you'd still have:
- Old NgModule patterns
- Zone.js overhead
- Technical debt
- Need to refactor to standalone components later

**Rewrite effort: 60-90 hours, but with zero technical debt and modern patterns from day one.**

---

## Implementation Strategy

### Approach: Incremental Module-by-Module Port

We'll build the Angular 21 app alongside the Angular 9 app, porting one feature at a time while keeping both apps running for comparison testing.

### Key Principles

1. **Port, Don't Lift-and-Shift** - Rewrite each component with modern Angular 21 patterns
2. **Signals First** - Use signals for all reactive state
3. **Functional Style** - Use functional guards and interceptors
4. **Test as We Go** - Write tests during development, not after
5. **Compare Continuously** - Test against Angular 9 app after each module
6. **Document Everything** - Capture decisions and patterns

### Project Structure During Migration

```
eComNetApp/
├── client/              # Angular 9 app (comparison baseline)
├── client-v21/          # Angular 21 app (new implementation)
├── Api/                 # Backend API (shared by both)
└── docker-compose.yml   # Run both frontends simultaneously
```

### Port Mapping

Ports during development:
- **Angular 9:** http://localhost:4200
- **Angular 21:** http://localhost:4201
- **Backend API:** http://localhost:44369
- **SQL Server:** localhost:1433
- **Redis:** localhost:6379

---

## Phased Implementation Plan

---

## Phase 0: Setup & Foundation (Week 1)

**Duration:** 8-12 hours
**Objective:** Create Angular 21 project skeleton with proper configuration

### Tasks

#### 1. Create Angular 21 Project (2 hours)

```bash
# Navigate to project root
cd /home/user/eComNetApp

# Create new Angular 21 workspace
ng new client-v21 --standalone --routing --style scss --skip-git

# Verify configuration
cd client-v21
cat angular.json | grep -A 5 "zoneless"  # Should be enabled by default
cat package.json | grep vitest            # Should be configured
```

**Verification:**
- ✅ Project created successfully
- ✅ Zoneless enabled by default
- ✅ Vitest configured
- ✅ Standalone components by default
- ✅ Project builds: `ng build`
- ✅ Project runs: `ng serve`

---

#### 2. Install Dependencies (1 hour)

```bash
cd /home/user/eComNetApp/client-v21

# UI libraries
npm install ngx-bootstrap bootstrap bootswatch

# Utilities
npm install ngx-spinner ngx-toastr xng-breadcrumb

# Payment processing
npm install @stripe/stripe-js

# UUID for basket IDs
npm install uuid
npm install --save-dev @types/uuid

# Verify installations
npm list --depth=0
```

**Expected Dependencies:**
- ngx-bootstrap: ^21.x (Angular 21 compatible)
- bootstrap: ^5.x (Bootstrap 5)
- ngx-spinner: Latest compatible version
- ngx-toastr: Latest compatible version
- @stripe/stripe-js: Latest version
- uuid: ^10.x

---

#### 3. Setup Environments (2 hours)

Create environment files for different deployment scenarios:

**File: `src/environments/environment.ts` (Default)**
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:44369/api',
  stripePublishableKey: 'pk_test_...'
};
```

**File: `src/environments/environment.local.ts` (Local development)**
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
  stripePublishableKey: 'pk_test_...'
};
```

**File: `src/environments/environment.container.ts` (Docker)**
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://api:80/api',
  stripePublishableKey: 'pk_test_...'
};
```

**File: `src/environments/environment.stage.ts` (Staging/Production)**
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.yourdomain.com/api',
  stripePublishableKey: 'pk_live_...'
};
```

**Update `angular.json` configurations:**
```json
"configurations": {
  "production": {
    "fileReplacements": [
      {
        "replace": "src/environments/environment.ts",
        "with": "src/environments/environment.stage.ts"
      }
    ],
    ...
  },
  "local": {
    "fileReplacements": [
      {
        "replace": "src/environments/environment.ts",
        "with": "src/environments/environment.local.ts"
      }
    ]
  },
  "container": {
    "fileReplacements": [
      {
        "replace": "src/environments/environment.ts",
        "with": "src/environments/environment.container.ts"
      }
    ]
  }
}
```

---

#### 4. Setup Project Structure (2 hours)

Create the recommended folder structure:

```bash
cd src/app

# Create core directories
mkdir -p core/{guards,interceptors,services,layout}

# Create feature directories
mkdir -p features/{auth,shop,basket,checkout,orders}

# Create shared directories
mkdir -p shared/{components,models,utils}
```

**Final structure:**
```
src/app/
├── core/
│   ├── guards/              # Route guards
│   ├── interceptors/        # HTTP interceptors
│   ├── services/            # Singleton services
│   └── layout/              # Layout components (nav, header, footer)
├── features/
│   ├── auth/                # Authentication module
│   ├── shop/                # Shop module
│   ├── basket/              # Basket module
│   ├── checkout/            # Checkout module
│   └── orders/              # Orders module
├── shared/
│   ├── components/          # Reusable components
│   ├── models/              # TypeScript interfaces/types
│   └── utils/               # Utility functions
├── app.component.ts         # Root component
├── app.config.ts            # App configuration (providers, interceptors)
└── app.routes.ts            # Route configuration
```

---

#### 5. Setup Docker Configuration (1 hour)

**Update `docker-compose.yml`:**
```yaml
services:
  # Existing services...

  # Angular 9 (existing)
  angular:
    container_name: ecom-angular-9
    build:
      context: ./client
      dockerfile: Dockerfile
    ports:
      - "4200:4200"
    volumes:
      - ./client:/app
      - /app/node_modules
    environment:
      - NODE_ENV=development
    networks:
      - ecom-network

  # Angular 21 (new)
  angular-v21:
    container_name: ecom-angular-21
    build:
      context: ./client-v21
      dockerfile: Dockerfile
    ports:
      - "4201:4200"
    volumes:
      - ./client-v21:/app
      - /app/node_modules
    environment:
      - NODE_ENV=development
    networks:
      - ecom-network
```

**Create `client-v21/Dockerfile`:**
```dockerfile
FROM node:20-alpine

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

EXPOSE 4200

CMD ["npm", "start", "--", "--host", "0.0.0.0", "--disable-host-check", "--poll", "3000"]
```

---

#### 6. Port TypeScript Models (2 hours)

Port all interfaces and types from Angular 9 app to Angular 21 app.

**File: `src/app/shared/models/user.ts`**
```typescript
export interface User {
  email: string;
  displayName: string;
  token: string;
}
```

**File: `src/app/shared/models/address.ts`**
```typescript
export interface Address {
  firstName: string;
  lastName: string;
  street: string;
  city: string;
  state: string;
  zipCode: string;
}
```

**File: `src/app/shared/models/product.ts`**
```typescript
export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  pictureUrl: string;
  productType: string;
  productBrand: string;
}
```

**File: `src/app/shared/models/basket.ts`**
```typescript
import { v4 as uuid } from 'uuid';

export interface Basket {
  id: string;
  items: BasketItem[];
  clientSecret?: string;
  paymentIntentId?: string;
  deliveryMethodId?: number;
  shippingPrice?: number;
}

export interface BasketItem {
  id: number;
  productName: string;
  price: number;
  quantity: number;
  pictureUrl: string;
  brand: string;
  type: string;
}

export class Basket implements Basket {
  id = uuid();
  items: BasketItem[] = [];
}

export interface BasketTotals {
  shipping: number;
  subtotal: number;
  total: number;
}
```

**File: `src/app/shared/models/order.ts`**
```typescript
import { Address } from './address';

export interface OrderToCreate {
  basketId: string;
  deliveryMethodId: number;
  shipToAddress: Address;
}

export interface Order {
  id: number;
  buyerEmail: string;
  orderDate: string;
  shipToAddress: Address;
  deliveryMethod: string;
  shippingPrice: number;
  orderItems: OrderItem[];
  subtotal: number;
  total: number;
  status: string;
}

export interface OrderItem {
  productId: number;
  productName: string;
  pictureUrl: string;
  price: number;
  quantity: number;
}
```

**File: `src/app/shared/models/pagination.ts`**
```typescript
export interface Pagination<T> {
  pageIndex: number;
  pageSize: number;
  count: number;
  data: T[];
}
```

**File: `src/app/shared/models/shop-params.ts`**
```typescript
export class ShopParams {
  brandId = 0;
  typeId = 0;
  sort = 'name';
  pageNumber = 1;
  pageSize = 6;
  search = '';
}
```

**File: `src/app/shared/models/delivery-method.ts`**
```typescript
export interface DeliveryMethod {
  id: number;
  shortName: string;
  deliveryTime: string;
  description: string;
  price: number;
}
```

**File: `src/app/shared/models/brand.ts`**
```typescript
export interface Brand {
  id: number;
  name: string;
}
```

**File: `src/app/shared/models/product-type.ts`**
```typescript
export interface ProductType {
  id: number;
  name: string;
}
```

**Create barrel export: `src/app/shared/models/index.ts`**
```typescript
export * from './user';
export * from './address';
export * from './product';
export * from './basket';
export * from './order';
export * from './pagination';
export * from './shop-params';
export * from './delivery-method';
export * from './brand';
export * from './product-type';
```

---

### Deliverables - Phase 0

- ✅ Angular 21 project running on port 4201
- ✅ All environments configured (local, container, stage)
- ✅ All TypeScript models ported
- ✅ Docker configuration updated
- ✅ Project structure established
- ✅ Dependencies installed and verified
- ✅ Project builds successfully
- ✅ Basic routing working

---

## Phase 1: Core Infrastructure (Week 2)

**Duration:** 10-14 hours
**Objective:** Build foundational services, interceptors, and guards using modern Angular 21 patterns

### Tasks

#### 1. Port HTTP Interceptors (Functional Style) (4 hours)

In Angular 21, interceptors use functional style instead of class-based approach.

**File: `src/app/core/interceptors/jwt.interceptor.ts`**
```typescript
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError, BehaviorSubject, filter, take } from 'rxjs';
import { AccountService } from '../../features/auth/account.service';

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const accountService = inject(AccountService);

  // Skip token attachment for auth endpoints
  if (isAuthEndpoint(req.url)) {
    return next(req);
  }

  const token = accountService.getAccessToken();

  if (token) {
    req = addToken(req, token);
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        return handle401Error(req, next, accountService);
      }
      return throwError(() => error);
    })
  );
};

function addToken(request: any, token: string) {
  return request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
}

function handle401Error(request: any, next: any, accountService: AccountService) {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    return accountService.refreshToken().pipe(
      switchMap(user => {
        isRefreshing = false;
        const newToken = user?.token || null;
        refreshTokenSubject.next(newToken);

        if (newToken) {
          return next(addToken(request, newToken));
        }
        return throwError(() => new Error('No token after refresh'));
      }),
      catchError(err => {
        isRefreshing = false;
        refreshTokenSubject.next(null);
        accountService.logout();
        return throwError(() => err);
      })
    );
  }

  // Wait for the refresh to complete
  return refreshTokenSubject.pipe(
    filter(token => token !== null),
    take(1),
    switchMap(token => next(addToken(request, token!)))
  );
}

function isAuthEndpoint(url: string): boolean {
  const authEndpoints = ['/account/login', '/account/register', '/account/refresh', '/account/logout'];
  return authEndpoints.some(endpoint => url.includes(endpoint));
}
```

**File: `src/app/core/interceptors/error.interceptor.ts`**
```typescript
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const toastr = inject(ToastrService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error) {
        if (error.status === 400) {
          if (error.error.errors) {
            // Validation errors
            throw error.error;
          } else {
            toastr.error(error.error.message || 'Bad request', error.status.toString());
          }
        }

        if (error.status === 401) {
          toastr.error(error.error.message || 'Unauthorized', error.status.toString());
        }

        if (error.status === 404) {
          router.navigate(['/not-found']);
        }

        if (error.status === 500) {
          router.navigate(['/server-error'], { state: { error: error.error } });
        }
      }

      return throwError(() => error);
    })
  );
};
```

**File: `src/app/core/interceptors/loading.interceptor.ts`**
```typescript
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { BusyService } from '../services/busy.service';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const busyService = inject(BusyService);

  // Skip loading for certain endpoints
  if (req.url.includes('emailExists') || req.method === 'DELETE') {
    return next(req);
  }

  busyService.busy();

  return next(req).pipe(
    finalize(() => {
      busyService.idle();
    })
  );
};
```

---

#### 2. Port Auth Guard (Functional Style) (2 hours)

**File: `src/app/core/guards/auth.guard.ts`**
```typescript
import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { map, switchMap, take } from 'rxjs/operators';
import { AccountService } from '../../features/auth/account.service';

export const authGuard: CanActivateFn = (route, state) => {
  const accountService = inject(AccountService);
  const router = inject(Router);

  // Wait for auth initialization to complete before checking user
  return accountService.authInitialized$.pipe(
    take(1),
    switchMap(() => accountService.currentUser$.pipe(
      take(1),
      map(user => {
        if (user) {
          return true;
        }
        router.navigate(['/auth/login'], {
          queryParams: { returnUrl: state.url }
        });
        return false;
      })
    ))
  );
};
```

---

#### 3. Create Core Services (4 hours)

**File: `src/app/core/services/busy.service.ts`**
```typescript
import { Injectable, signal, computed } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class BusyService {
  private busyRequestCount = signal(0);

  // Public computed signal for loading state
  loading = computed(() => this.busyRequestCount() > 0);

  busy() {
    this.busyRequestCount.update(count => count + 1);
  }

  idle() {
    this.busyRequestCount.update(count => Math.max(0, count - 1));
  }
}
```

---

#### 4. Create Layout Components (4 hours)

**File: `src/app/core/layout/nav-bar/nav-bar.component.ts`**
```typescript
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { AccountService } from '../../../features/auth/account.service';
import { BasketService } from '../../../features/basket/basket.service';

@Component({
  selector: 'app-nav-bar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, BsDropdownModule],
  template: `
    <nav class="navbar navbar-expand-md navbar-dark bg-primary">
      <div class="container">
        <a class="navbar-brand" routerLink="/">eComNet</a>

        <button class="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarNav">
          <span class="navbar-toggler-icon"></span>
        </button>

        <div class="collapse navbar-collapse" id="navbarNav">
          <ul class="navbar-nav mr-auto">
            <li class="nav-item">
              <a class="nav-link" routerLink="/shop" routerLinkActive="active">Shop</a>
            </li>
            @if (accountService.currentUser()) {
              <li class="nav-item">
                <a class="nav-link" routerLink="/orders" routerLinkActive="active">Orders</a>
              </li>
            }
          </ul>

          <ul class="navbar-nav">
            <li class="nav-item">
              <a class="nav-link" routerLink="/basket">
                <i class="fa fa-shopping-cart fa-2x"></i>
                @if (basketService.itemCount() > 0) {
                  <span class="badge badge-info">{{ basketService.itemCount() }}</span>
                }
              </a>
            </li>

            @if (accountService.currentUser(); as user) {
              <li class="nav-item dropdown" dropdown>
                <a class="nav-link dropdown-toggle" dropdownToggle>
                  Welcome {{ user.displayName }}
                </a>
                <div class="dropdown-menu" *dropdownMenu>
                  <a class="dropdown-item" routerLink="/orders">Orders</a>
                  <div class="dropdown-divider"></div>
                  <a class="dropdown-item" (click)="logout()">Logout</a>
                </div>
              </li>
            } @else {
              <li class="nav-item">
                <a class="nav-link" routerLink="/auth/login">Login</a>
              </li>
              <li class="nav-item">
                <a class="nav-link" routerLink="/auth/register">Register</a>
              </li>
            }
          </ul>
        </div>
      </div>
    </nav>
  `,
  styles: [`
    .badge {
      position: absolute;
      top: -5px;
      right: -5px;
    }
  `]
})
export class NavBarComponent {
  accountService = inject(AccountService);
  basketService = inject(BasketService);

  logout() {
    this.accountService.logout().subscribe();
  }
}
```

**File: `src/app/core/layout/section-header/section-header.component.ts`**
```typescript
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BreadcrumbComponent } from 'xng-breadcrumb';

@Component({
  selector: 'app-section-header',
  standalone: true,
  imports: [CommonModule, BreadcrumbComponent],
  template: `
    <section class="py-3">
      <div class="container">
        <xng-breadcrumb></xng-breadcrumb>
      </div>
    </section>
  `
})
export class SectionHeaderComponent {}
```

**Create error page components similarly:**
- `not-found.component.ts`
- `server-error.component.ts`
- `test-error.component.ts` (for development testing)

---

#### 5. Update App Configuration (1 hour)

**File: `src/app/app.config.ts`**
```typescript
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';

import { routes } from './app.routes';
import { jwtInterceptor } from './core/interceptors/jwt.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { loadingInterceptor } from './core/interceptors/loading.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    // Zoneless change detection (default in Angular 21)
    provideZoneChangeDetection({ eventCoalescing: true }),

    // Routing
    provideRouter(routes),

    // HTTP client with interceptors
    provideHttpClient(
      withInterceptors([
        loadingInterceptor,
        jwtInterceptor,
        errorInterceptor
      ])
    ),

    // Animations
    provideAnimations(),

    // Toastr notifications
    provideToastr({
      timeOut: 3000,
      positionClass: 'toast-bottom-right',
      preventDuplicates: true
    })
  ]
};
```

**File: `src/app/app.routes.ts`**
```typescript
import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/shop',
    pathMatch: 'full'
  },
  {
    path: 'shop',
    loadChildren: () => import('./features/shop/shop.routes')
      .then(m => m.SHOP_ROUTES),
    data: { breadcrumb: 'Shop' }
  },
  {
    path: 'basket',
    loadChildren: () => import('./features/basket/basket.routes')
      .then(m => m.BASKET_ROUTES),
    data: { breadcrumb: 'Basket' }
  },
  {
    path: 'checkout',
    canActivate: [authGuard],
    loadChildren: () => import('./features/checkout/checkout.routes')
      .then(m => m.CHECKOUT_ROUTES),
    data: { breadcrumb: 'Checkout' }
  },
  {
    path: 'orders',
    canActivate: [authGuard],
    loadChildren: () => import('./features/orders/orders.routes')
      .then(m => m.ORDERS_ROUTES),
    data: { breadcrumb: 'Orders' }
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes')
      .then(m => m.AUTH_ROUTES),
    data: { breadcrumb: { skip: true } }
  },
  {
    path: 'not-found',
    loadComponent: () => import('./core/layout/not-found/not-found.component')
      .then(m => m.NotFoundComponent)
  },
  {
    path: 'server-error',
    loadComponent: () => import('./core/layout/server-error/server-error.component')
      .then(m => m.ServerErrorComponent)
  },
  {
    path: '**',
    redirectTo: 'not-found',
    pathMatch: 'full'
  }
];
```

---

### Deliverables - Phase 1

- ✅ All interceptors ported (functional style)
- ✅ Auth guard ported (functional style)
- ✅ Busy service with signals
- ✅ Layout components created (nav bar, header)
- ✅ Error pages created
- ✅ App configuration complete with zoneless
- ✅ Routing configured with lazy loading
- ✅ Infrastructure ready for feature development

---

## Phase 2: Authentication Module (Week 3)

**Duration:** 10-12 hours
**Objective:** Complete authentication with JWT + refresh token flow using signals

### Tasks

#### 1. Account Service (4 hours)

**File: `src/app/features/auth/account.service.ts`**
```typescript
import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, BehaviorSubject } from 'rxjs';
import { User } from '../../shared/models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private http = inject(HttpClient);
  private router = inject(Router);

  // Signal-based state
  private currentUserSource = signal<User | null>(null);
  currentUser = this.currentUserSource.asReadonly();

  // Observable for backward compatibility with guard
  currentUser$ = new BehaviorSubject<User | null>(null);

  // Auth initialization state
  private authInitializedSource = signal(false);
  authInitialized = this.authInitializedSource.asReadonly();
  authInitialized$ = new BehaviorSubject<boolean>(false);

  // Access token stored in memory only (security best practice)
  private accessToken: string | null = null;

  // Computed values
  isLoggedIn = computed(() => this.currentUser() !== null);
  userDisplayName = computed(() => this.currentUser()?.displayName ?? 'Guest');

  constructor() {
    this.initializeAuth();
  }

  private initializeAuth() {
    // Try to refresh token on app startup
    this.refreshToken().subscribe({
      next: () => {
        this.authInitializedSource.set(true);
        this.authInitialized$.next(true);
      },
      error: () => {
        this.authInitializedSource.set(true);
        this.authInitialized$.next(true);
      }
    });
  }

  login(values: any): Observable<User> {
    return this.http.post<User>(`${environment.apiUrl}/account/login`, values).pipe(
      tap(user => this.setCurrentUser(user))
    );
  }

  register(values: any): Observable<User> {
    return this.http.post<User>(`${environment.apiUrl}/account/register`, values).pipe(
      tap(user => this.setCurrentUser(user))
    );
  }

  refreshToken(): Observable<User> {
    return this.http.post<User>(`${environment.apiUrl}/account/refresh`, {}).pipe(
      tap(user => this.setCurrentUser(user))
    );
  }

  logout(): Observable<any> {
    return this.http.post(`${environment.apiUrl}/account/logout`, {}).pipe(
      tap(() => {
        this.clearCurrentUser();
        this.router.navigate(['/']);
      })
    );
  }

  checkEmailExists(email: string): Observable<boolean> {
    return this.http.get<boolean>(`${environment.apiUrl}/account/emailExists?email=${email}`);
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  private setCurrentUser(user: User) {
    this.accessToken = user.token;
    this.currentUserSource.set(user);
    this.currentUser$.next(user);
  }

  private clearCurrentUser() {
    this.accessToken = null;
    this.currentUserSource.set(null);
    this.currentUser$.next(null);
  }
}
```

---

#### 2. Login Component (3 hours)

**File: `src/app/features/auth/login/login.component.ts`**
```typescript
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AccountService } from '../account.service';
import { TextInputComponent } from '../../../shared/components/text-input/text-input.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TextInputComponent],
  template: `
    <div class="container mt-5">
      <div class="row">
        <div class="col-md-6 offset-md-3">
          <h2>Login</h2>

          @if (accountService.currentUser()) {
            <p>You are already logged in. <a routerLink="/shop">Go to shop</a></p>
          } @else {
            <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
              <app-text-input
                formControlName="email"
                label="Email"
                type="email">
              </app-text-input>

              <app-text-input
                formControlName="password"
                label="Password"
                type="password">
              </app-text-input>

              @if (errorMessage) {
                <div class="alert alert-danger">
                  {{ errorMessage }}
                </div>
              }

              <button
                type="submit"
                class="btn btn-primary"
                [disabled]="!loginForm.valid">
                Login
              </button>

              <div class="mt-3">
                <p>Don't have an account? <a routerLink="/auth/register">Register here</a></p>
              </div>
            </form>
          }
        </div>
      </div>
    </div>
  `
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  accountService = inject(AccountService);

  loginForm: FormGroup;
  returnUrl = '';
  errorMessage = '';

  constructor() {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/shop';

    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  onSubmit() {
    if (this.loginForm.valid) {
      this.accountService.login(this.loginForm.value).subscribe({
        next: () => {
          this.router.navigate([this.returnUrl]);
        },
        error: err => {
          this.errorMessage = err.error?.message || 'Login failed';
        }
      });
    }
  }
}
```

---

#### 3. Register Component (3 hours)

**File: `src/app/features/auth/register/register.component.ts`**
```typescript
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { AccountService } from '../account.service';
import { TextInputComponent } from '../../../shared/components/text-input/text-input.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TextInputComponent],
  template: `
    <div class="container mt-5">
      <div class="row">
        <div class="col-md-6 offset-md-3">
          <h2>Register</h2>

          <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">
            <app-text-input
              formControlName="displayName"
              label="Display Name">
            </app-text-input>

            <app-text-input
              formControlName="email"
              label="Email"
              type="email">
            </app-text-input>

            <app-text-input
              formControlName="password"
              label="Password"
              type="password">
            </app-text-input>

            @if (errorMessage) {
              <div class="alert alert-danger">
                {{ errorMessage }}
              </div>
            }

            <button
              type="submit"
              class="btn btn-primary"
              [disabled]="!registerForm.valid">
              Register
            </button>

            <div class="mt-3">
              <p>Already have an account? <a routerLink="/auth/login">Login here</a></p>
            </div>
          </form>
        </div>
      </div>
    </div>
  `
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private accountService = inject(AccountService);

  registerForm: FormGroup;
  errorMessage = '';

  constructor() {
    this.registerForm = this.fb.group({
      displayName: ['', Validators.required],
      email: ['',
        [Validators.required, Validators.email],
        [this.validateEmailNotTaken()]
      ],
      password: ['', [
        Validators.required,
        Validators.minLength(6),
        Validators.pattern(/^(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).*$/)
      ]]
    });
  }

  validateEmailNotTaken() {
    return (control: AbstractControl) => {
      if (!control.value) {
        return of(null);
      }

      return this.accountService.checkEmailExists(control.value).pipe(
        map(result => result ? { emailExists: true } : null)
      );
    };
  }

  onSubmit() {
    if (this.registerForm.valid) {
      this.accountService.register(this.registerForm.value).subscribe({
        next: () => {
          this.router.navigate(['/shop']);
        },
        error: err => {
          this.errorMessage = err.error?.message || 'Registration failed';
        }
      });
    }
  }
}
```

---

#### 4. Auth Routes (1 hour)

**File: `src/app/features/auth/auth.routes.ts`**
```typescript
import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./login/login.component')
      .then(m => m.LoginComponent),
    data: { breadcrumb: 'Login' }
  },
  {
    path: 'register',
    loadComponent: () => import('./register/register.component')
      .then(m => m.RegisterComponent),
    data: { breadcrumb: 'Register' }
  }
];
```

---

#### 5. Testing (2 hours)

**File: `src/app/features/auth/account.service.spec.ts`**
```typescript
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AccountService } from './account.service';

describe('AccountService', () => {
  let service: AccountService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AccountService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AccountService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should login and set current user', () => {
    const mockUser = { email: 'test@test.com', displayName: 'Test', token: 'token123' };

    service.login({ email: 'test@test.com', password: 'password' }).subscribe(user => {
      expect(user).toEqual(mockUser);
      expect(service.currentUser()).toEqual(mockUser);
      expect(service.isLoggedIn()).toBe(true);
    });

    const req = httpMock.expectOne(request => request.url.includes('/account/login'));
    expect(req.request.method).toBe('POST');
    req.flush(mockUser);
  });

  it('should logout and clear current user', () => {
    service.logout().subscribe(() => {
      expect(service.currentUser()).toBeNull();
      expect(service.isLoggedIn()).toBe(false);
    });

    const req = httpMock.expectOne(request => request.url.includes('/account/logout'));
    expect(req.request.method).toBe('POST');
    req.flush({});
  });
});
```

---

### Deliverables - Phase 2

- ✅ Account service with signals
- ✅ Login component (standalone)
- ✅ Register component (standalone)
- ✅ JWT + refresh token flow working
- ✅ Email validation (async validator)
- ✅ Password validation
- ✅ Error handling
- ✅ Return URL support
- ✅ Tests passing
- ✅ Feature parity with Angular 9 verified

---

## Phase 3: Shop Module (Week 4)

**Duration:** 12-16 hours
**Objective:** Product catalog with filtering, sorting, pagination, and details

### Key Features

- Product listing with pagination
- Filter by brand and type
- Sort by price and name
- Search functionality
- Product details page
- Add to basket from listing and details
- Signal-based reactive state

### Files to Create

```
features/shop/
├── shop.service.ts              # Shop service with signals
├── shop.routes.ts               # Shop routes
├── product-list/
│   └── product-list.component.ts
├── product-item/
│   └── product-item.component.ts
└── product-details/
    └── product-details.component.ts
```

### Implementation Notes

- Use signals for product state, filters, pagination
- Implement debounced search with RxJS
- Use computed signals for filtered/sorted products
- Integrate with basket service for add-to-cart
- Use new control flow syntax (@for, @if)

**Estimated Time:** 12-16 hours

---

## Phase 4: Basket Module (Week 5)

**Duration:** 8-10 hours
**Objective:** Shopping cart with Redis persistence and computed totals

### Key Features

- Display basket items
- Add/remove/update item quantities
- Computed totals (subtotal, shipping, total)
- Redis persistence
- Empty basket state
- Proceed to checkout

### Files to Create

```
features/basket/
├── basket.service.ts            # Basket service with signals
├── basket.routes.ts             # Basket routes
└── basket/
    └── basket.component.ts
```

### Implementation Notes

- Use signals for basket state
- Computed signals for item count and totals
- localStorage for basket ID persistence
- Optimistic UI updates
- Error handling for API failures

**Estimated Time:** 8-10 hours

---

## Phase 5: Checkout Module (Week 6-7)

**Duration:** 14-18 hours
**Objective:** Multi-step checkout with Stripe payment integration

### Key Features

- Multi-step form (address, delivery, payment, review)
- Custom stepper component (Angular CDK)
- Step validation and navigation
- Stripe Elements integration
- Payment Intent creation
- Order submission
- Success page

### Files to Create

```
features/checkout/
├── checkout.service.ts
├── checkout.routes.ts
├── checkout/
│   └── checkout.component.ts
├── checkout-address/
│   └── checkout-address.component.ts
├── checkout-delivery/
│   └── checkout-delivery.component.ts
├── checkout-payment/
│   └── checkout-payment.component.ts
├── checkout-review/
│   └── checkout-review.component.ts
└── checkout-success/
    └── checkout-success.component.ts
```

### Implementation Notes

- Use reactive forms with validation
- Integrate Stripe.js library
- Handle Payment Intent creation/update
- Clear basket after successful order
- Comprehensive error handling

**Estimated Time:** 14-18 hours

---

## Phase 6: Orders Module (Week 8)

**Duration:** 6-8 hours
**Objective:** Order history and order details pages

### Key Features

- Order list with filtering/sorting
- Order details page
- Order status display
- Empty state handling

### Files to Create

```
features/orders/
├── orders.service.ts
├── orders.routes.ts
├── orders-list/
│   └── orders-list.component.ts
└── order-details/
    └── order-details.component.ts
```

**Estimated Time:** 6-8 hours

---

## Phase 7: Shared Components & Polish (Week 9)

**Duration:** 8-12 hours
**Objective:** Port remaining shared components and polish UI

### Components to Create

- Pager component
- Paging header component
- Text input component (form control wrapper)
- Order totals component
- Basket summary component
- Home page

### Tasks

- Consistent styling across all pages
- Responsive design verification
- Loading states
- Error states
- Accessibility improvements
- Toast notifications

**Estimated Time:** 8-12 hours

---

## Phase 8: Testing & Bug Fixes (Week 10)

**Duration:** 10-14 hours
**Objective:** Comprehensive testing and bug fixing

### Testing Strategy

1. **Unit Testing** (4 hours)
   - All services tested
   - All components tested
   - Vitest watch mode

2. **Integration Testing** (3 hours)
   - API integration tests
   - Authentication flow
   - Checkout flow

3. **E2E Testing** (3 hours)
   - Full user journeys
   - Cross-browser testing

4. **Comparison Testing** (2 hours)
   - Side-by-side with Angular 9
   - Feature parity verification

5. **Bug Fixes** (2 hours)
   - Fix identified issues
   - Performance optimization

**Estimated Time:** 10-14 hours

---

## Phase 9: Deployment & Cutover (Week 11)

**Duration:** 8-10 hours
**Objective:** Deploy to staging and production

### Tasks

1. **Build Configuration** (2 hours)
   - Production build optimization
   - Environment configuration

2. **Deployment to Staging** (2 hours)
   - Deploy Angular 21 app
   - Parallel running with Angular 9

3. **Monitoring Setup** (2 hours)
   - Error tracking
   - Performance monitoring

4. **Documentation** (2 hours)
   - Developer docs
   - Deployment guide

5. **Cutover Planning** (2 hours)
   - Rollback plan
   - Communication plan

**Estimated Time:** 8-10 hours

---

## Feature Parity Checklist

### Authentication Module

- [ ] Login with email/password
- [ ] Register new user
- [ ] Logout
- [ ] JWT token in memory only
- [ ] Refresh token in HttpOnly cookie
- [ ] Automatic token refresh on 401
- [ ] Automatic token refresh on app startup
- [ ] Return URL after login
- [ ] Account lockout handling
- [ ] Form validation (email, password strength)
- [ ] Async email validation (check if exists)
- [ ] Display validation errors
- [ ] Navigate to return URL after login
- [ ] Display current user in nav bar

### Shop Module

- [ ] Product listing with pagination
- [ ] Display product cards (image, name, price, brand, type)
- [ ] Filter by brand (multi-select)
- [ ] Filter by type (multi-select)
- [ ] Clear filters button
- [ ] Sort by price (low to high, high to low)
- [ ] Sort by name (A-Z, Z-A)
- [ ] Search products by name (debounced)
- [ ] Clear search button
- [ ] Pagination controls (previous, next, page numbers)
- [ ] Display result count
- [ ] Product details page
- [ ] Product image carousel
- [ ] Product description
- [ ] Quantity selector on details
- [ ] Add to basket from listing
- [ ] Add to basket from details
- [ ] Breadcrumb navigation
- [ ] Loading states
- [ ] Empty state (no products found)
- [ ] Error handling

### Basket Module

- [ ] View basket items
- [ ] Display item details (image, name, price, quantity)
- [ ] Add item to basket
- [ ] Remove item from basket (single quantity)
- [ ] Update item quantity with + button
- [ ] Update item quantity with - button
- [ ] Remove item completely when quantity reaches 0
- [ ] Calculate subtotal
- [ ] Calculate shipping (when delivery method selected)
- [ ] Calculate total (subtotal + shipping)
- [ ] Display item count in nav bar
- [ ] Update nav bar count in real-time
- [ ] Persist basket in Redis
- [ ] Load basket on app startup from localStorage ID
- [ ] Create new basket if none exists
- [ ] Empty basket message
- [ ] Proceed to checkout button
- [ ] Disable checkout if basket empty
- [ ] Continue shopping button
- [ ] Loading states
- [ ] Error handling (show toast on failure)

### Checkout Module

- [ ] Authentication required (redirect to login if not authenticated)
- [ ] Multi-step stepper (address, delivery, payment, review)
- [ ] Display current step
- [ ] Navigate between steps (next, previous)
- [ ] Step validation (cannot proceed without completing step)
- [ ] Disable future steps until current completed

**Address Step:**
- [ ] Address form (firstName, lastName, street, city, state, zipCode)
- [ ] Form validation
- [ ] Save address to user profile
- [ ] Load saved address on component init
- [ ] Display saved address message
- [ ] Next button (validates and proceeds)

**Delivery Step:**
- [ ] Fetch delivery methods from API
- [ ] Display delivery options (name, price, delivery time, description)
- [ ] Select delivery method (radio buttons)
- [ ] Update basket with shipping price
- [ ] Display updated totals
- [ ] Next button (validates selection)
- [ ] Previous button (returns to address)

**Payment Step:**
- [ ] Stripe Elements integration
- [ ] Card number input field
- [ ] Expiry date input field
- [ ] CVC input field
- [ ] Create/update Payment Intent on component init
- [ ] Handle client secret
- [ ] Display payment amount
- [ ] Submit payment button
- [ ] Process payment with Stripe
- [ ] Handle payment errors (display to user)
- [ ] Handle network errors
- [ ] Loading state during payment processing
- [ ] Disable submit during processing
- [ ] Previous button (returns to delivery)

**Review Step:**
- [ ] Display order summary
- [ ] Display basket items with quantities
- [ ] Display delivery method and price
- [ ] Display shipping address
- [ ] Display totals (subtotal, shipping, total)
- [ ] Submit order button
- [ ] Create order in database
- [ ] Handle order creation errors
- [ ] Loading state during order submission
- [ ] Previous button (returns to payment)
- [ ] Navigate to success page after order created

**Success Page:**
- [ ] Display order confirmation message
- [ ] Display order number
- [ ] Display order details (items, totals, delivery, address)
- [ ] Clear basket after successful order
- [ ] Link to view order details
- [ ] Link to continue shopping
- [ ] Link to view all orders

### Orders Module

- [ ] Authentication required (redirect to login)
- [ ] Display order history
- [ ] Order list sorted by date (newest first)
- [ ] Display order card (order number, date, total, status)
- [ ] Click order to view details
- [ ] Order details page
- [ ] Display all order information
- [ ] Display order items with quantities
- [ ] Display delivery method
- [ ] Display shipping address
- [ ] Display order status
- [ ] Display totals
- [ ] Back to orders button
- [ ] Empty state when no orders
- [ ] Loading states
- [ ] Error handling

### Infrastructure

**JWT Interceptor:**
- [ ] Add Authorization header to all requests
- [ ] Skip adding token to auth endpoints
- [ ] Handle 401 errors
- [ ] Automatically refresh token on 401
- [ ] Retry original request after refresh
- [ ] Handle refresh failure (logout user)
- [ ] Queue requests during token refresh
- [ ] Resume queued requests after refresh

**Error Interceptor:**
- [ ] Handle 400 errors (validation errors)
- [ ] Display validation errors to user
- [ ] Handle 401 errors (show toast)
- [ ] Handle 404 errors (navigate to not-found page)
- [ ] Handle 500 errors (navigate to server-error page)
- [ ] Pass error details to error page
- [ ] Display toast notifications for errors
- [ ] Log errors to console (development)

**Loading Interceptor:**
- [ ] Show loading spinner during HTTP requests
- [ ] Hide spinner after request completes
- [ ] Skip spinner for specific endpoints (emailExists, DELETE)
- [ ] Handle multiple concurrent requests
- [ ] Display spinner in nav bar or overlay

**Auth Guard:**
- [ ] Check if user is authenticated
- [ ] Wait for auth initialization before checking
- [ ] Redirect to login if not authenticated
- [ ] Pass return URL to login page
- [ ] Allow access if authenticated

**General Infrastructure:**
- [ ] Breadcrumb navigation works correctly
- [ ] Breadcrumb displays current location
- [ ] Responsive nav bar
- [ ] Nav bar displays user info when logged in
- [ ] Nav bar displays basket item count
- [ ] Toast notifications work for all scenarios
- [ ] Loading spinner displays during operations
- [ ] Error pages display correctly (404, 500)
- [ ] Test error page works (development only)

### UI/UX

- [ ] Responsive design on mobile (320px+)
- [ ] Responsive design on tablet (768px+)
- [ ] Responsive design on desktop (1024px+)
- [ ] Loading states for all async operations
- [ ] Error states for all components
- [ ] Empty states (no products, no basket, no orders)
- [ ] Consistent styling (Bootstrap + Bootswatch)
- [ ] Consistent spacing and typography
- [ ] Buttons have hover states
- [ ] Links have hover states
- [ ] Form inputs have focus states
- [ ] Disabled states are visually distinct
- [ ] Accessibility: ARIA labels on interactive elements
- [ ] Accessibility: Keyboard navigation works
- [ ] Accessibility: Color contrast meets WCAG AA
- [ ] Smooth animations/transitions
- [ ] Images have loading states
- [ ] Images have error fallbacks

### Performance

- [ ] Bundle size < 500KB (gzipped)
- [ ] First Contentful Paint < 1.5s
- [ ] Time to Interactive < 3.5s
- [ ] Lazy loading works for all routes
- [ ] Images are optimized
- [ ] No unnecessary re-renders
- [ ] Signals prevent over-computation
- [ ] Zoneless provides performance benefits

### Browser Compatibility

- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)
- [ ] Mobile Safari (iOS 14+)
- [ ] Mobile Chrome (Android 10+)

---

## Project Structure

```
client-v21/
├── src/
│   ├── app/
│   │   ├── core/                           # Singleton services and infrastructure
│   │   │   ├── guards/
│   │   │   │   └── auth.guard.ts
│   │   │   ├── interceptors/
│   │   │   │   ├── jwt.interceptor.ts
│   │   │   │   ├── error.interceptor.ts
│   │   │   │   └── loading.interceptor.ts
│   │   │   ├── services/
│   │   │   │   └── busy.service.ts
│   │   │   └── layout/
│   │   │       ├── nav-bar/
│   │   │       ├── section-header/
│   │   │       ├── not-found/
│   │   │       └── server-error/
│   │   ├── features/                       # Feature modules
│   │   │   ├── auth/
│   │   │   │   ├── account.service.ts
│   │   │   │   ├── auth.routes.ts
│   │   │   │   ├── login/
│   │   │   │   └── register/
│   │   │   ├── shop/
│   │   │   │   ├── shop.service.ts
│   │   │   │   ├── shop.routes.ts
│   │   │   │   ├── product-list/
│   │   │   │   ├── product-item/
│   │   │   │   └── product-details/
│   │   │   ├── basket/
│   │   │   │   ├── basket.service.ts
│   │   │   │   ├── basket.routes.ts
│   │   │   │   └── basket/
│   │   │   ├── checkout/
│   │   │   │   ├── checkout.service.ts
│   │   │   │   ├── checkout.routes.ts
│   │   │   │   ├── checkout/
│   │   │   │   ├── checkout-address/
│   │   │   │   ├── checkout-delivery/
│   │   │   │   ├── checkout-payment/
│   │   │   │   ├── checkout-review/
│   │   │   │   └── checkout-success/
│   │   │   └── orders/
│   │   │       ├── orders.service.ts
│   │   │       ├── orders.routes.ts
│   │   │       ├── orders-list/
│   │   │       └── order-details/
│   │   ├── shared/                         # Shared code
│   │   │   ├── components/
│   │   │   │   ├── pager/
│   │   │   │   ├── paging-header/
│   │   │   │   ├── text-input/
│   │   │   │   ├── order-totals/
│   │   │   │   ├── basket-summary/
│   │   │   │   └── stepper/
│   │   │   ├── models/
│   │   │   │   ├── index.ts
│   │   │   │   ├── user.ts
│   │   │   │   ├── address.ts
│   │   │   │   ├── product.ts
│   │   │   │   ├── basket.ts
│   │   │   │   ├── order.ts
│   │   │   │   ├── pagination.ts
│   │   │   │   ├── shop-params.ts
│   │   │   │   ├── delivery-method.ts
│   │   │   │   ├── brand.ts
│   │   │   │   └── product-type.ts
│   │   │   └── utils/
│   │   ├── app.component.ts
│   │   ├── app.config.ts                   # App configuration
│   │   └── app.routes.ts                   # Route configuration
│   ├── environments/
│   │   ├── environment.ts                  # Default
│   │   ├── environment.local.ts            # Local development
│   │   ├── environment.container.ts        # Docker
│   │   └── environment.stage.ts            # Staging/Production
│   ├── assets/                             # Static assets
│   ├── styles.scss                         # Global styles
│   └── index.html
├── angular.json                            # Angular CLI configuration
├── package.json                            # Dependencies
├── tsconfig.json                           # TypeScript configuration
├── vitest.config.ts                        # Vitest configuration
├── Dockerfile                              # Docker configuration
└── README.md                               # Project documentation
```

---

## Technical Stack

### Core Framework

- **Angular:** 21.1.0
- **TypeScript:** 5.5+
- **Node.js:** 20.x LTS
- **RxJS:** 7.8+

### UI Libraries

- **Bootstrap:** 5.3+
- **Bootswatch:** 5.3+
- **ngx-bootstrap:** 21.x
- **Font Awesome:** 4.7.0

### Utilities

- **ngx-spinner:** Latest
- **ngx-toastr:** Latest
- **xng-breadcrumb:** Latest
- **UUID:** 10.x

### Payment Processing

- **@stripe/stripe-js:** Latest

### Testing

- **Vitest:** Latest (default in Angular 21)
- **@angular/core/testing:** 21.x
- **@vitest/ui:** Latest (optional, for UI)

### Build & Development

- **esbuild:** Built into Angular CLI
- **Angular CLI:** 21.x

---

## Risk Mitigation

### Identified Risks & Mitigation Strategies

#### Risk 1: Feature Gaps
**Impact:** Medium
**Probability:** Medium
**Mitigation:**
- Comprehensive feature parity checklist
- Side-by-side comparison testing with Angular 9 app
- Involve QA team throughout development
- Regular demos to stakeholders

#### Risk 2: Business Logic Changes
**Impact:** High
**Probability:** Low
**Mitigation:**
- Careful code review during porting
- Unit tests for all services
- Integration tests for critical flows
- Domain expert validation

#### Risk 3: Third-Party Library Compatibility
**Impact:** Medium
**Probability:** Low
**Mitigation:**
- Verify all library versions support Angular 21
- Test integrations early (especially Stripe)
- Have fallback options ready
- Community support for ngx-bootstrap is strong

#### Risk 4: Performance Regression
**Impact:** Medium
**Probability:** Low
**Mitigation:**
- Performance benchmarks from Angular 9 app
- Lighthouse scores before/after
- Load testing critical flows
- Angular 21 should be faster with zoneless

#### Risk 5: Timeline Overrun
**Impact:** Medium
**Probability:** Medium
**Mitigation:**
- Buffer time built into estimates
- Phased approach allows for delays
- Can release incrementally by module
- Regular progress reviews

#### Risk 6: Team Learning Curve
**Impact:** Medium
**Probability:** Medium
**Mitigation:**
- Pair programming sessions
- Code reviews for knowledge sharing
- Document patterns and decisions
- Angular 21 docs are excellent

#### Risk 7: Deployment Issues
**Impact:** High
**Probability:** Low
**Mitigation:**
- Test deployment to staging early
- Run both apps in parallel during cutover
- Comprehensive rollback plan
- Monitor closely during cutover

---

## Success Criteria

### Must Have (Required for Cutover)

1. **Feature Parity:** 100% of Angular 9 features replicated
2. **Tests Passing:** All unit, integration, and E2E tests pass
3. **Performance:** Meets or exceeds Angular 9 performance metrics
4. **Browser Compatibility:** Works on all target browsers
5. **Mobile Responsive:** Works on mobile devices
6. **Security:** JWT + refresh token flow working correctly
7. **Payment Processing:** Stripe integration working end-to-end
8. **Zero Critical Bugs:** No showstopper issues

### Should Have (Nice to Have)

1. **Better Performance:** Faster than Angular 9 (zoneless benefits)
2. **Smaller Bundle Size:** < 500KB gzipped
3. **Better Lighthouse Scores:** 90+ on all metrics
4. **Improved Accessibility:** WCAG AA compliant
5. **Better Test Coverage:** > 80% code coverage

### Could Have (Future Improvements)

1. **PWA Support:** Service worker, offline capability
2. **Internationalization:** Multi-language support
3. **Advanced Analytics:** User behavior tracking
4. **A/B Testing:** Feature flag system

---

## Next Steps

### Immediate Actions (Week 1)

1. **Create new branch:** `claude/angular-21-rewrite-HZ35J`
2. **Execute Phase 0:** Setup & Foundation
   - Create Angular 21 project
   - Install dependencies
   - Setup environments
   - Port TypeScript models
   - Configure Docker
3. **Verify setup:** Ensure everything builds and runs
4. **Commit progress:** Regular commits with clear messages
5. **Push to remote:** Keep remote branch updated

### Short-Term (Weeks 2-5)

1. **Execute Phases 1-4:** Core infrastructure and main features
2. **Regular testing:** Compare with Angular 9 app after each phase
3. **Team reviews:** Code reviews and demos
4. **Documentation:** Update docs as patterns emerge

### Medium-Term (Weeks 6-9)

1. **Execute Phases 5-7:** Checkout, orders, polish
2. **Comprehensive testing:** Full regression testing
3. **Performance testing:** Benchmark against Angular 9
4. **Stakeholder demos:** Show progress

### Long-Term (Weeks 10-11)

1. **Execute Phases 8-9:** Final testing and deployment
2. **Staging deployment:** Deploy to staging environment
3. **Parallel running:** Both apps running simultaneously
4. **Cutover:** Switch production traffic to Angular 21
5. **Monitor:** Close monitoring for issues
6. **Celebrate:** Team celebration! 🎉

---

## Conclusion

This implementation plan provides a comprehensive roadmap for rewriting the eComNetApp frontend from Angular 9 to Angular 21. The phased approach allows for incremental progress, continuous testing, and risk mitigation.

**Key Takeaways:**

- ✅ **Angular 21 is the right choice** - Zoneless, signals, Vitest, 16 months LTS
- ✅ **Rewrite is better than upgrade** - Zero technical debt, modern patterns
- ✅ **Phased approach reduces risk** - Test after each module
- ✅ **11 weeks is realistic** - 60-90 hours of focused work
- ✅ **Old app as safety net** - Compare and validate continuously

**Ready to Start?**

The plan is ready for execution. Phase 0 can begin immediately and will establish the foundation for all subsequent work.

---

**Document Version:** 1.0
**Last Updated:** January 23, 2026
**Status:** Ready for Review
**Next Action:** Approve plan and begin Phase 0
