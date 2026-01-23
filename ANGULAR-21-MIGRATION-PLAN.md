# Angular 9 to 21 Migration Plan

## Executive Summary

This document outlines a comprehensive phased approach for migrating the Skinet e-commerce application from Angular 9 to Angular 21. Rather than performing an incremental upgrade through multiple Angular versions, we will build a completely new Angular 21 application alongside the existing Angular 9 app, allowing for parallel development, feature parity verification, and minimal disruption to ongoing development.

---

## Table of Contents

1. [Migration Strategy Overview](#migration-strategy-overview)
2. [Project Structure During Migration](#project-structure-during-migration)
3. [Technology Stack Comparison](#technology-stack-comparison)
4. [Phase 0: Pre-Migration Preparation](#phase-0-pre-migration-preparation)
5. [Phase 1: Angular 21 Project Scaffolding](#phase-1-angular-21-project-scaffolding)
6. [Phase 2: Core Infrastructure](#phase-2-core-infrastructure)
7. [Phase 3: Shared Module](#phase-3-shared-module)
8. [Phase 4: Feature Modules](#phase-4-feature-modules)
9. [Phase 5: Integration & Testing](#phase-5-integration--testing)
10. [Phase 6: Production Cutover](#phase-6-production-cutover)
11. [Risk Mitigation](#risk-mitigation)
12. [Appendix: Breaking Changes Reference](#appendix-breaking-changes-reference)

---

## Migration Strategy Overview

### Approach: Parallel Development with Incremental Module Port

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Migration Approach                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   Angular 9 (client/)          Angular 21 (client-v21/)             │
│   ┌─────────────────┐          ┌─────────────────┐                  │
│   │  Production     │          │  Development    │                  │
│   │  Reference      │   ───►   │  New Build      │                  │
│   │  Baseline       │          │  Modern Stack   │                  │
│   └─────────────────┘          └─────────────────┘                  │
│          │                              │                            │
│          └──────────────┬───────────────┘                            │
│                         │                                            │
│                         ▼                                            │
│              ┌─────────────────┐                                     │
│              │   Shared API    │                                     │
│              │   (Api/)        │                                     │
│              │   Port: 5001    │                                     │
│              └─────────────────┘                                     │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Benefits of This Approach

| Benefit | Description |
|---------|-------------|
| **Zero Production Risk** | Angular 9 app remains stable and deployable |
| **Side-by-Side Comparison** | Easy visual and functional comparison |
| **Incremental Progress** | Migrate one module at a time |
| **Modern Best Practices** | Adopt Angular 21 patterns from scratch |
| **No Legacy Baggage** | Clean slate without migration artifacts |
| **Team Learning** | Developers learn new Angular while building |

---

## Project Structure During Migration

```
eComNetApp/
├── client/                    # Angular 9 app (comparison baseline)
│   ├── src/
│   │   ├── app/
│   │   │   ├── account/
│   │   │   ├── basket/
│   │   │   ├── checkout/
│   │   │   ├── core/
│   │   │   ├── home/
│   │   │   ├── orders/
│   │   │   ├── shared/
│   │   │   └── shop/
│   │   └── environments/
│   ├── angular.json
│   └── package.json
│
├── client-v21/                # Angular 21 app (new implementation)
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/          # Standalone components
│   │   │   ├── shared/        # Shared utilities & components
│   │   │   ├── features/      # Feature modules
│   │   │   │   ├── account/
│   │   │   │   ├── basket/
│   │   │   │   ├── checkout/
│   │   │   │   ├── home/
│   │   │   │   ├── orders/
│   │   │   │   └── shop/
│   │   │   ├── app.component.ts
│   │   │   ├── app.config.ts
│   │   │   └── app.routes.ts
│   │   └── environments/
│   ├── angular.json
│   └── package.json
│
├── Api/                       # Backend API (shared by both)
│
├── docker-compose.yml         # Run both frontends + API
├── docker-compose.migration.yml  # Migration-specific compose
│
└── ANGULAR-21-MIGRATION-PLAN.md  # This document
```

### Development Ports

| Application | Port | Purpose |
|-------------|------|---------|
| Angular 9 | `http://localhost:4200` | Reference baseline |
| Angular 21 | `http://localhost:4201` | New implementation |
| API | `https://localhost:5001` | Shared backend |

---

## Technology Stack Comparison

### Angular Version Changes

| Feature | Angular 9 | Angular 21 |
|---------|-----------|------------|
| **Components** | Class-based with decorators | Standalone components (default) |
| **Modules** | NgModules required | Optional (standalone preferred) |
| **Routing** | RouterModule.forRoot() | provideRouter() functional |
| **HTTP** | HttpClientModule | provideHttpClient() |
| **Forms** | ReactiveFormsModule import | Standalone imports |
| **Signals** | N/A | Full signals support |
| **Control Flow** | *ngIf, *ngFor | @if, @for, @switch |
| **Change Detection** | Zone.js | Zoneless optional |
| **Build System** | Webpack | esbuild/Vite |
| **SSR** | Universal (complex) | Built-in hydration |

### Dependency Migration Map

| Angular 9 Package | Version | Angular 21 Replacement | Notes |
|-------------------|---------|------------------------|-------|
| `@angular/core` | 9.0.1 | 21.x | Complete API changes |
| `rxjs` | 6.5.4 | 7.8+ | Operator imports changed |
| `bootstrap` | 4.1.1 | 5.3.x | CSS class changes |
| `ngx-bootstrap` | 5.3.2 | 18.x+ or **ng-bootstrap** | Consider switching |
| `ngx-toastr` | 11.3.3 | 19.x+ | Standalone support |
| `ngx-spinner` | 8.1.0 | 17.x+ | Standalone support |
| `xng-breadcrumb` | 5.0.1 | 11.x+ | Standalone support |
| `font-awesome` | 4.7.0 | **@fortawesome/angular-fontawesome** | Modern FA integration |
| `uuid` | 3.4.0 | `crypto.randomUUID()` | Built-in browser API |
| `zone.js` | 0.10.2 | 0.15.x (optional) | Zoneless possible |

---

## Phase 0: Pre-Migration Preparation

**Duration**: 1-2 days
**Branch**: `feature/modernize`

### Checklist

- [ ] **0.1** Review and document all Angular 9 features
- [ ] **0.2** Identify deprecated APIs and patterns
- [ ] **0.3** Create API contract documentation
- [ ] **0.4** Set up comparison testing environment
- [ ] **0.5** Update Node.js to v22+ (required for Angular 21)
- [ ] **0.6** Document all environment configurations
- [ ] **0.7** Create feature parity checklist

### API Endpoints to Preserve

```
Account API:
  POST   /api/account/login
  POST   /api/account/register
  POST   /api/account/refresh
  POST   /api/account/logout
  GET    /api/account/emailexists
  GET    /api/account/address
  PUT    /api/account/address

Products API:
  GET    /api/products
  GET    /api/products/:id
  GET    /api/products/brands
  GET    /api/products/types

Basket API:
  GET    /api/basket/:id
  POST   /api/basket
  DELETE /api/basket/:id

Payments API:
  POST   /api/payments/:basketId

Orders API:
  POST   /api/orders
  GET    /api/orders
  GET    /api/orders/:id
  GET    /api/orders/deliveryMethods
```

---

## Phase 1: Angular 21 Project Scaffolding

**Duration**: 1-2 days
**Dependencies**: Phase 0

### Tasks

#### 1.1 Create New Angular 21 Project

```bash
# Navigate to project root
cd /home/user/eComNetApp

# Create Angular 21 project with modern defaults
ng new client-v21 \
  --style=scss \
  --routing=true \
  --ssr=false \
  --standalone=true \
  --skip-git \
  --package-manager=npm

cd client-v21
```

#### 1.2 Configure Project Structure

```bash
# Create feature directories
mkdir -p src/app/core/{guards,interceptors,services,components}
mkdir -p src/app/shared/{components,models,pipes,directives}
mkdir -p src/app/features/{account,basket,checkout,home,orders,shop}
```

#### 1.3 Install Dependencies

```bash
# Core dependencies
npm install bootstrap@5.3 @popperjs/core
npm install @ng-bootstrap/ng-bootstrap
npm install ngx-toastr
npm install ngx-spinner
npm install xng-breadcrumb
npm install @fortawesome/fontawesome-free
npm install @stripe/stripe-js

# Dev dependencies
npm install -D @types/node
```

#### 1.4 Configure Angular.json

```json
{
  "projects": {
    "client-v21": {
      "architect": {
        "build": {
          "options": {
            "outputPath": "dist/client-v21",
            "styles": [
              "node_modules/bootstrap/dist/css/bootstrap.min.css",
              "node_modules/@fortawesome/fontawesome-free/css/all.min.css",
              "node_modules/ngx-toastr/toastr.css",
              "node_modules/ngx-spinner/animations/ball-spin-clockwise.css",
              "src/styles.scss"
            ]
          }
        },
        "serve": {
          "options": {
            "port": 4201
          }
        }
      }
    }
  }
}
```

#### 1.5 Create Environment Files

```typescript
// src/environments/environment.ts
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api/',
  stripe: {
    publishableKey: 'REPLACE_WITH_YOUR_KEY'
  }
};

// src/environments/environment.development.ts
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api/',
  stripe: {
    publishableKey: 'REPLACE_WITH_YOUR_KEY'
  }
};

// src/environments/environment.docker.ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:44369/api/',
  stripe: {
    publishableKey: 'REPLACE_WITH_YOUR_KEY'
  }
};
```

#### 1.6 Configure App with Providers

```typescript
// src/app/app.config.ts
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';

import { routes } from './app.routes';
import { jwtInterceptor } from './core/interceptors/jwt.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { loadingInterceptor } from './core/interceptors/loading.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(
      withInterceptors([jwtInterceptor, errorInterceptor, loadingInterceptor])
    ),
    provideAnimations(),
    provideToastr({
      positionClass: 'toast-bottom-right',
      preventDuplicates: true
    })
  ]
};
```

### Deliverables

- [ ] New Angular 21 project in `client-v21/`
- [ ] Project structure with feature directories
- [ ] Dependencies installed and configured
- [ ] Environment files created
- [ ] App config with providers
- [ ] Development server running on port 4201

---

## Phase 2: Core Infrastructure

**Duration**: 3-4 days
**Dependencies**: Phase 1

### 2.1 Models (Day 1)

Port all TypeScript interfaces from `client/src/app/shared/models/`:

```typescript
// src/app/shared/models/user.model.ts
export interface User {
  email: string;
  displayName: string;
  token: string;
}

// src/app/shared/models/product.model.ts
export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  pictureUrl: string;
  productType: string;
  productBrand: string;
}

// src/app/shared/models/basket.model.ts
export interface Basket {
  id: string;
  items: BasketItem[];
  deliveryMethodId?: number;
  shippingPrice?: number;
  clientSecret?: string;
  paymentIntentId?: string;
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

export interface BasketTotals {
  shipping: number;
  subtotal: number;
  total: number;
}

// Additional models: Order, Address, Pagination, ShopParams, etc.
```

### 2.2 Core Services with Signals (Days 1-2)

#### Account Service (Modern Pattern)

```typescript
// src/app/core/services/account.service.ts
import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { User } from '../../shared/models/user.model';

@Injectable({ providedIn: 'root' })
export class AccountService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private baseUrl = environment.apiUrl;

  // Signals for reactive state
  private currentUserSignal = signal<User | null>(null);
  private accessToken = signal<string | null>(null);
  private authInitialized = signal(false);

  // Public computed signals
  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isLoggedIn = computed(() => !!this.currentUserSignal());
  readonly isAuthInitialized = this.authInitialized.asReadonly();

  // Token management
  getToken(): string | null {
    return this.accessToken();
  }

  async login(email: string, password: string): Promise<void> {
    const user = await firstValueFrom(
      this.http.post<User>(`${this.baseUrl}account/login`, { email, password })
    );
    this.setCurrentUser(user);
  }

  async register(values: any): Promise<void> {
    const user = await firstValueFrom(
      this.http.post<User>(`${this.baseUrl}account/register`, values)
    );
    this.setCurrentUser(user);
  }

  private setCurrentUser(user: User): void {
    this.accessToken.set(user.token);
    this.currentUserSignal.set(user);
  }

  async initializeAuth(): Promise<void> {
    try {
      const user = await firstValueFrom(
        this.http.post<User>(`${this.baseUrl}account/refresh`, {})
      );
      this.setCurrentUser(user);
    } catch {
      // Not logged in, that's fine
    } finally {
      this.authInitialized.set(true);
    }
  }

  logout(): void {
    this.http.post(`${this.baseUrl}account/logout`, {}).subscribe();
    this.accessToken.set(null);
    this.currentUserSignal.set(null);
    this.router.navigateByUrl('/');
  }
}
```

#### Basket Service (Modern Pattern)

```typescript
// src/app/core/services/basket.service.ts
import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Basket, BasketItem, BasketTotals } from '../../shared/models/basket.model';

@Injectable({ providedIn: 'root' })
export class BasketService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  // Signals
  private basketSignal = signal<Basket | null>(null);

  // Public computed
  readonly basket = this.basketSignal.asReadonly();
  readonly itemCount = computed(() =>
    this.basketSignal()?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0
  );
  readonly totals = computed<BasketTotals | null>(() => {
    const basket = this.basketSignal();
    if (!basket) return null;

    const subtotal = basket.items.reduce(
      (sum, item) => sum + item.price * item.quantity, 0
    );
    const shipping = basket.shippingPrice ?? 0;

    return { shipping, subtotal, total: subtotal + shipping };
  });

  // Methods...
}
```

### 2.3 HTTP Interceptors (Day 2)

#### JWT Interceptor (Functional)

```typescript
// src/app/core/interceptors/jwt.interceptor.ts
import { HttpInterceptorFn, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AccountService } from '../services/account.service';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const accountService = inject(AccountService);
  const token = accountService.getToken();

  // Skip auth endpoints
  if (req.url.includes('account/login') ||
      req.url.includes('account/register') ||
      req.url.includes('account/refresh')) {
    return next(req);
  }

  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(req);
};
```

#### Error Interceptor (Functional)

```typescript
// src/app/core/interceptors/error.interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const toastr = inject(ToastrService);

  return next(req).pipe(
    catchError(error => {
      if (error.status === 400) {
        if (error.error.errors) {
          const errors = Object.values(error.error.errors).flat();
          throw errors;
        }
        toastr.error(error.error.message || 'Bad request');
      }
      if (error.status === 401) {
        toastr.error('Unauthorized');
      }
      if (error.status === 404) {
        router.navigateByUrl('/not-found');
      }
      if (error.status === 500) {
        router.navigateByUrl('/server-error', { state: error.error });
      }
      return throwError(() => error);
    })
  );
};
```

### 2.4 Guards (Day 3)

```typescript
// src/app/core/guards/auth.guard.ts
import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AccountService } from '../services/account.service';
import { firstValueFrom } from 'rxjs';
import { filter, map } from 'rxjs/operators';
import { toObservable } from '@angular/core/rxjs-interop';

export const authGuard: CanActivateFn = async (route, state) => {
  const accountService = inject(AccountService);
  const router = inject(Router);

  // Wait for auth initialization
  if (!accountService.isAuthInitialized()) {
    await firstValueFrom(
      toObservable(accountService.isAuthInitialized).pipe(
        filter(initialized => initialized)
      )
    );
  }

  if (accountService.isLoggedIn()) {
    return true;
  }

  return router.createUrlTree(['/account/login'], {
    queryParams: { returnUrl: state.url }
  });
};
```

### 2.5 Core Components (Days 3-4)

#### Nav Bar Component (Standalone)

```typescript
// src/app/core/components/nav-bar/nav-bar.component.ts
import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NgClass } from '@angular/common';
import { AccountService } from '../../services/account.service';
import { BasketService } from '../../services/basket.service';

@Component({
  selector: 'app-nav-bar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, NgClass],
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.scss'
})
export class NavBarComponent {
  accountService = inject(AccountService);
  basketService = inject(BasketService);
}
```

```html
<!-- nav-bar.component.html -->
<nav class="navbar navbar-expand-lg navbar-dark bg-dark">
  <div class="container">
    <a class="navbar-brand" routerLink="/">Skinet</a>

    <ul class="navbar-nav me-auto">
      <li class="nav-item">
        <a class="nav-link" routerLink="/shop" routerLinkActive="active">Shop</a>
      </li>
    </ul>

    <div class="d-flex align-items-center">
      <!-- Basket Icon -->
      <a routerLink="/basket" class="position-relative me-3">
        <i class="fa fa-shopping-cart fa-2x text-light"></i>
        @if (basketService.itemCount() > 0) {
          <span class="badge bg-warning position-absolute">
            {{ basketService.itemCount() }}
          </span>
        }
      </a>

      <!-- User Menu -->
      @if (accountService.isLoggedIn()) {
        <div class="dropdown">
          <a class="dropdown-toggle text-light" data-bs-toggle="dropdown">
            {{ accountService.currentUser()?.email }}
          </a>
          <ul class="dropdown-menu">
            <li><a class="dropdown-item" routerLink="/orders">Orders</a></li>
            <li><hr class="dropdown-divider"></li>
            <li><a class="dropdown-item" (click)="accountService.logout()">Logout</a></li>
          </ul>
        </div>
      } @else {
        <a routerLink="/account/login" class="btn btn-outline-light">Login</a>
      }
    </div>
  </div>
</nav>
```

### Deliverables

- [ ] All TypeScript models ported
- [ ] Core services with signals (Account, Basket, Shop, Busy)
- [ ] Functional HTTP interceptors (JWT, Error, Loading)
- [ ] Auth guard with signal support
- [ ] Core components (NavBar, SectionHeader, NotFound, ServerError)
- [ ] Breadcrumb integration

---

## Phase 3: Shared Module

**Duration**: 2-3 days
**Dependencies**: Phase 2

### 3.1 Shared Components

| Component | Priority | Angular 21 Changes |
|-----------|----------|-------------------|
| `TextInputComponent` | High | Standalone, ControlValueAccessor |
| `PagerComponent` | High | Standalone, signal inputs |
| `PagingHeaderComponent` | High | Standalone, signal inputs |
| `OrderTotalsComponent` | Medium | Standalone, computed signals |
| `BasketSummaryComponent` | Medium | Standalone, @for control flow |
| `StepperComponent` | High | CDK Stepper integration |

### 3.2 Text Input Component (Example)

```typescript
// src/app/shared/components/text-input/text-input.component.ts
import { Component, input, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-text-input',
  standalone: true,
  imports: [ReactiveFormsModule],
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => TextInputComponent),
    multi: true
  }],
  template: `
    <div class="mb-3">
      <label class="form-label">{{ label() }}</label>
      <input
        [type]="type()"
        class="form-control"
        [class.is-invalid]="touched && invalid"
        [value]="value"
        (input)="onInput($event)"
        (blur)="onTouched()">
      @if (touched && invalid) {
        <div class="invalid-feedback">{{ errorMessage }}</div>
      }
    </div>
  `
})
export class TextInputComponent implements ControlValueAccessor {
  label = input.required<string>();
  type = input<string>('text');

  value = '';
  touched = false;
  invalid = false;
  errorMessage = '';

  onChange: (value: string) => void = () => {};
  onTouched: () => void = () => {};

  writeValue(value: string): void {
    this.value = value;
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.value = value;
    this.onChange(value);
  }
}
```

### 3.3 Pager Component

```typescript
// src/app/shared/components/pager/pager.component.ts
import { Component, input, output } from '@angular/core';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-pager',
  standalone: true,
  imports: [NgbPaginationModule],
  template: `
    <ngb-pagination
      [collectionSize]="totalCount()"
      [pageSize]="pageSize()"
      [page]="pageNumber()"
      [maxSize]="5"
      [rotate]="true"
      [boundaryLinks]="true"
      (pageChange)="pageChanged.emit($event)">
    </ngb-pagination>
  `
})
export class PagerComponent {
  totalCount = input.required<number>();
  pageSize = input.required<number>();
  pageNumber = input.required<number>();
  pageChanged = output<number>();
}
```

### Deliverables

- [ ] TextInputComponent (form control wrapper)
- [ ] PagerComponent (pagination)
- [ ] PagingHeaderComponent (results info)
- [ ] OrderTotalsComponent (totals display)
- [ ] BasketSummaryComponent (basket items)
- [ ] StepperComponent (checkout stepper)
- [ ] All shared models

---

## Phase 4: Feature Modules

**Duration**: 8-10 days
**Dependencies**: Phase 3

### Module Migration Order

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Feature Module Migration Order                    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   Week 1                          Week 2                             │
│   ┌─────────┐                     ┌─────────┐                       │
│   │  Home   │ ──► Foundational    │ Account │ ──► Auth Required     │
│   │ (1 day) │                     │ (2 days)│     For Next Phases   │
│   └─────────┘                     └─────────┘                       │
│        │                               │                             │
│        ▼                               ▼                             │
│   ┌─────────┐                     ┌─────────┐                       │
│   │  Shop   │ ──► Core Feature    │ Basket  │ ──► Cart Logic        │
│   │ (3 days)│                     │ (2 days)│                       │
│   └─────────┘                     └─────────┘                       │
│                                        │                             │
│                                        ▼                             │
│                               ┌─────────────────┐                    │
│                               │    Checkout     │ ──► Complex Flow   │
│                               │    (3 days)     │     + Stripe       │
│                               └─────────────────┘                    │
│                                        │                             │
│                                        ▼                             │
│                               ┌─────────────────┐                    │
│                               │     Orders      │ ──► Final Feature  │
│                               │    (1-2 days)   │                    │
│                               └─────────────────┘                    │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### 4.1 Home Feature (Day 1)

```typescript
// src/app/features/home/home.component.ts
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="container mt-5">
      <section class="hero text-center">
        <h1>Welcome to Skinet</h1>
        <p class="lead">Your one-stop shop for quality products</p>
        <a routerLink="/shop" class="btn btn-primary btn-lg">Shop Now</a>
      </section>
    </div>
  `
})
export class HomeComponent {}

// Route: src/app/features/home/home.routes.ts
import { Routes } from '@angular/router';
import { HomeComponent } from './home.component';

export const HOME_ROUTES: Routes = [
  { path: '', component: HomeComponent }
];
```

### 4.2 Shop Feature (Days 2-4)

#### Components Structure

```
features/shop/
├── shop.component.ts              # Main catalog page
├── shop.routes.ts                 # Shop routing
├── components/
│   ├── product-item/              # Product card
│   └── product-details/           # Product detail page
└── services/
    └── shop.service.ts            # Product data service
```

#### Shop Service (Signals)

```typescript
// src/app/features/shop/services/shop.service.ts
import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { Product, Brand, ProductType } from '../../../shared/models';
import { Pagination } from '../../../shared/models/pagination.model';
import { ShopParams } from '../../../shared/models/shop-params.model';

@Injectable({ providedIn: 'root' })
export class ShopService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  // Signals
  private productsSignal = signal<Product[]>([]);
  private brandsSignal = signal<Brand[]>([]);
  private typesSignal = signal<ProductType[]>([]);
  private paginationSignal = signal<Pagination | null>(null);
  private paramsSignal = signal<ShopParams>(new ShopParams());
  private loadingSignal = signal(false);

  // Public readonly signals
  readonly products = this.productsSignal.asReadonly();
  readonly brands = this.brandsSignal.asReadonly();
  readonly types = this.typesSignal.asReadonly();
  readonly pagination = this.paginationSignal.asReadonly();
  readonly params = this.paramsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  // Computed
  readonly totalCount = computed(() => this.paginationSignal()?.count ?? 0);

  async loadProducts(): Promise<void> {
    this.loadingSignal.set(true);
    const params = this.buildParams();

    try {
      const response = await firstValueFrom(
        this.http.get<Pagination<Product[]>>(`${this.baseUrl}products`, { params })
      );
      this.productsSignal.set(response.data);
      this.paginationSignal.set(response);
    } finally {
      this.loadingSignal.set(false);
    }
  }

  updateParams(updates: Partial<ShopParams>): void {
    this.paramsSignal.update(current => ({ ...current, ...updates }));
  }

  private buildParams(): HttpParams {
    const p = this.paramsSignal();
    let params = new HttpParams()
      .set('pageIndex', p.pageNumber.toString())
      .set('pageSize', p.pageSize.toString());

    if (p.brandId > 0) params = params.set('brandId', p.brandId.toString());
    if (p.typeId > 0) params = params.set('typeId', p.typeId.toString());
    if (p.search) params = params.set('search', p.search);
    if (p.sort) params = params.set('sort', p.sort);

    return params;
  }
}
```

#### Shop Component

```typescript
// src/app/features/shop/shop.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ShopService } from './services/shop.service';
import { ProductItemComponent } from './components/product-item/product-item.component';
import { PagerComponent } from '../../shared/components/pager/pager.component';
import { PagingHeaderComponent } from '../../shared/components/paging-header/paging-header.component';

@Component({
  selector: 'app-shop',
  standalone: true,
  imports: [
    FormsModule,
    ProductItemComponent,
    PagerComponent,
    PagingHeaderComponent
  ],
  templateUrl: './shop.component.html',
  styleUrl: './shop.component.scss'
})
export class ShopComponent implements OnInit {
  shopService = inject(ShopService);

  sortOptions = [
    { name: 'Alphabetical', value: 'name' },
    { name: 'Price: Low to High', value: 'priceAsc' },
    { name: 'Price: High to Low', value: 'priceDesc' }
  ];

  ngOnInit(): void {
    this.shopService.loadBrands();
    this.shopService.loadTypes();
    this.shopService.loadProducts();
  }

  onBrandSelected(brandId: number): void {
    this.shopService.updateParams({ brandId, pageNumber: 1 });
    this.shopService.loadProducts();
  }

  onTypeSelected(typeId: number): void {
    this.shopService.updateParams({ typeId, pageNumber: 1 });
    this.shopService.loadProducts();
  }

  onSortSelected(sort: string): void {
    this.shopService.updateParams({ sort });
    this.shopService.loadProducts();
  }

  onSearch(search: string): void {
    this.shopService.updateParams({ search, pageNumber: 1 });
    this.shopService.loadProducts();
  }

  onPageChanged(page: number): void {
    this.shopService.updateParams({ pageNumber: page });
    this.shopService.loadProducts();
  }
}
```

```html
<!-- shop.component.html -->
<div class="container mt-4">
  <div class="row">
    <!-- Filters Sidebar -->
    <div class="col-3">
      <h5>Sort</h5>
      <select class="form-select mb-3" (change)="onSortSelected($any($event.target).value)">
        @for (option of sortOptions; track option.value) {
          <option [value]="option.value">{{ option.name }}</option>
        }
      </select>

      <h5>Brands</h5>
      <ul class="list-group mb-3">
        <li class="list-group-item"
            [class.active]="shopService.params().brandId === 0"
            (click)="onBrandSelected(0)">
          All
        </li>
        @for (brand of shopService.brands(); track brand.id) {
          <li class="list-group-item"
              [class.active]="shopService.params().brandId === brand.id"
              (click)="onBrandSelected(brand.id)">
            {{ brand.name }}
          </li>
        }
      </ul>

      <h5>Types</h5>
      <ul class="list-group">
        <li class="list-group-item"
            [class.active]="shopService.params().typeId === 0"
            (click)="onTypeSelected(0)">
          All
        </li>
        @for (type of shopService.types(); track type.id) {
          <li class="list-group-item"
              [class.active]="shopService.params().typeId === type.id"
              (click)="onTypeSelected(type.id)">
            {{ type.name }}
          </li>
        }
      </ul>
    </div>

    <!-- Products Grid -->
    <div class="col-9">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <app-paging-header
          [currentPage]="shopService.params().pageNumber"
          [pageSize]="shopService.params().pageSize"
          [totalCount]="shopService.totalCount()" />

        <input type="text"
               class="form-control w-50"
               placeholder="Search..."
               (keyup.enter)="onSearch($any($event.target).value)">
      </div>

      <div class="row">
        @for (product of shopService.products(); track product.id) {
          <div class="col-4 mb-4">
            <app-product-item [product]="product" />
          </div>
        }
      </div>

      @if (shopService.totalCount() > shopService.params().pageSize) {
        <app-pager
          [totalCount]="shopService.totalCount()"
          [pageSize]="shopService.params().pageSize"
          [pageNumber]="shopService.params().pageNumber"
          (pageChanged)="onPageChanged($event)" />
      }
    </div>
  </div>
</div>
```

### 4.3 Account Feature (Days 5-6)

```
features/account/
├── account.routes.ts
├── login/
│   └── login.component.ts
└── register/
    └── register.component.ts
```

#### Login Component

```typescript
// src/app/features/account/login/login.component.ts
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { AccountService } from '../../../core/services/account.service';
import { TextInputComponent } from '../../../shared/components/text-input/text-input.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TextInputComponent],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private accountService = inject(AccountService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  returnUrl: string;

  constructor() {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/shop';
  }

  async onSubmit(): Promise<void> {
    if (this.loginForm.invalid) return;

    try {
      await this.accountService.login(
        this.loginForm.value.email!,
        this.loginForm.value.password!
      );
      this.router.navigateByUrl(this.returnUrl);
    } catch (error) {
      console.error('Login failed', error);
    }
  }
}
```

### 4.4 Basket Feature (Days 7-8)

```typescript
// src/app/features/basket/basket.component.ts
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BasketService } from '../../core/services/basket.service';
import { BasketSummaryComponent } from '../../shared/components/basket-summary/basket-summary.component';
import { OrderTotalsComponent } from '../../shared/components/order-totals/order-totals.component';

@Component({
  selector: 'app-basket',
  standalone: true,
  imports: [RouterLink, BasketSummaryComponent, OrderTotalsComponent],
  templateUrl: './basket.component.html'
})
export class BasketComponent {
  basketService = inject(BasketService);

  incrementQuantity(itemId: number): void {
    this.basketService.incrementItemQuantity(itemId);
  }

  decrementQuantity(itemId: number): void {
    this.basketService.decrementItemQuantity(itemId);
  }

  removeItem(itemId: number): void {
    this.basketService.removeItemFromBasket(itemId);
  }
}
```

### 4.5 Checkout Feature (Days 9-11)

```
features/checkout/
├── checkout.component.ts          # Stepper container
├── checkout.routes.ts
├── checkout-address/              # Step 1: Address
├── checkout-delivery/             # Step 2: Delivery
├── checkout-payment/              # Step 3: Payment (Stripe)
├── checkout-review/               # Step 4: Review
├── checkout-success/              # Success page
└── services/
    └── checkout.service.ts
```

#### Stripe Integration

```typescript
// src/app/features/checkout/checkout-payment/checkout-payment.component.ts
import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { loadStripe, Stripe, StripeCardElement } from '@stripe/stripe-js';
import { environment } from '../../../../environments/environment';
import { BasketService } from '../../../core/services/basket.service';
import { CheckoutService } from '../services/checkout.service';

@Component({
  selector: 'app-checkout-payment',
  standalone: true,
  templateUrl: './checkout-payment.component.html'
})
export class CheckoutPaymentComponent implements OnInit, OnDestroy {
  private basketService = inject(BasketService);
  private checkoutService = inject(CheckoutService);

  stripe: Stripe | null = null;
  cardElement: StripeCardElement | null = null;
  cardErrors = signal<string>('');

  async ngOnInit(): Promise<void> {
    this.stripe = await loadStripe(environment.stripe.publishableKey);

    if (this.stripe) {
      const elements = this.stripe.elements();
      this.cardElement = elements.create('card');
      this.cardElement.mount('#card-element');

      this.cardElement.on('change', event => {
        this.cardErrors.set(event.error?.message ?? '');
      });
    }
  }

  ngOnDestroy(): void {
    this.cardElement?.destroy();
  }

  async submitPayment(): Promise<boolean> {
    const basket = this.basketService.basket();
    if (!basket?.clientSecret || !this.stripe || !this.cardElement) {
      return false;
    }

    const result = await this.stripe.confirmCardPayment(basket.clientSecret, {
      payment_method: {
        card: this.cardElement
      }
    });

    if (result.error) {
      this.cardErrors.set(result.error.message ?? 'Payment failed');
      return false;
    }

    return true;
  }
}
```

### 4.6 Orders Feature (Days 12-13)

```typescript
// src/app/features/orders/orders.component.ts
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrdersService } from './services/orders.service';
import { Order } from '../../shared/models/order.model';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="container mt-4">
      <h2>Your Orders</h2>

      @if (orders().length === 0) {
        <p>No orders found.</p>
      } @else {
        <table class="table">
          <thead>
            <tr>
              <th>Order #</th>
              <th>Date</th>
              <th>Total</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (order of orders(); track order.id) {
              <tr>
                <td>{{ order.id }}</td>
                <td>{{ order.orderDate | date }}</td>
                <td>{{ order.total | currency }}</td>
                <td>{{ order.status }}</td>
                <td>
                  <a [routerLink]="['/orders', order.id]" class="btn btn-sm btn-primary">
                    View
                  </a>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `
})
export class OrdersComponent implements OnInit {
  private ordersService = inject(OrdersService);
  orders = signal<Order[]>([]);

  async ngOnInit(): Promise<void> {
    const orders = await this.ordersService.getOrders();
    this.orders.set(orders);
  }
}
```

### Deliverables

- [ ] Home feature (landing page)
- [ ] Shop feature (products, filtering, pagination, details)
- [ ] Account feature (login, register)
- [ ] Basket feature (cart management)
- [ ] Checkout feature (multi-step with Stripe)
- [ ] Orders feature (history, details)
- [ ] All routes configured

---

## Phase 5: Integration & Testing

**Duration**: 3-4 days
**Dependencies**: Phase 4

### 5.1 App Routes Configuration

```typescript
// src/app/app.routes.ts
import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'shop', pathMatch: 'full' },
  {
    path: 'shop',
    loadChildren: () => import('./features/shop/shop.routes').then(m => m.SHOP_ROUTES)
  },
  {
    path: 'basket',
    loadChildren: () => import('./features/basket/basket.routes').then(m => m.BASKET_ROUTES)
  },
  {
    path: 'checkout',
    canActivate: [authGuard],
    loadChildren: () => import('./features/checkout/checkout.routes').then(m => m.CHECKOUT_ROUTES)
  },
  {
    path: 'orders',
    canActivate: [authGuard],
    loadChildren: () => import('./features/orders/orders.routes').then(m => m.ORDERS_ROUTES)
  },
  {
    path: 'account',
    loadChildren: () => import('./features/account/account.routes').then(m => m.ACCOUNT_ROUTES)
  },
  {
    path: 'not-found',
    loadComponent: () => import('./core/components/not-found/not-found.component')
      .then(m => m.NotFoundComponent)
  },
  {
    path: 'server-error',
    loadComponent: () => import('./core/components/server-error/server-error.component')
      .then(m => m.ServerErrorComponent)
  },
  { path: '**', redirectTo: 'not-found' }
];
```

### 5.2 Testing Checklist

#### Functional Testing

| Feature | Test Cases |
|---------|------------|
| **Shop** | Browse products, filter by brand/type, search, pagination, view details |
| **Account** | Register, login, logout, email validation, session persistence |
| **Basket** | Add items, update quantity, remove items, persist across sessions |
| **Checkout** | Address form, delivery selection, Stripe payment, order creation |
| **Orders** | View order list, view order details |
| **Auth** | Protected routes redirect, token refresh |
| **Errors** | 404 page, 500 page, validation errors |

#### Cross-Browser Testing

- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)

#### Responsive Testing

- [ ] Desktop (1920x1080)
- [ ] Tablet (768x1024)
- [ ] Mobile (375x667)

### 5.3 Performance Comparison

```bash
# Build production bundles
cd client && npm run build:prod
cd ../client-v21 && npm run build

# Compare bundle sizes
du -sh client/dist
du -sh client-v21/dist

# Run Lighthouse audits on both
npx lighthouse http://localhost:4200 --output html --output-path ./reports/ng9-lighthouse.html
npx lighthouse http://localhost:4201 --output html --output-path ./reports/ng21-lighthouse.html
```

### Deliverables

- [ ] Full integration testing complete
- [ ] All routes functional
- [ ] Performance benchmarks documented
- [ ] Bug fixes applied
- [ ] Feature parity verified

---

## Phase 6: Production Cutover

**Duration**: 2-3 days
**Dependencies**: Phase 5

### 6.1 Pre-Cutover Checklist

- [ ] All functional tests pass
- [ ] Performance meets or exceeds Angular 9 baseline
- [ ] Environment configurations verified
- [ ] CI/CD pipeline updated
- [ ] Rollback plan documented
- [ ] Team trained on new codebase

### 6.2 Docker Configuration

```yaml
# docker-compose.migration.yml
version: '3.8'

services:
  api:
    build: ./Api
    ports:
      - "5001:5001"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development

  client-v9:
    build:
      context: ./client
      dockerfile: Dockerfile
    ports:
      - "4200:80"
    depends_on:
      - api

  client-v21:
    build:
      context: ./client-v21
      dockerfile: Dockerfile
    ports:
      - "4201:80"
    depends_on:
      - api
```

### 6.3 Cutover Steps

1. **Final QA Sign-off**
   - Complete regression testing
   - Stakeholder approval

2. **Deploy to Staging**
   - Deploy Angular 21 to staging environment
   - Run smoke tests
   - Performance validation

3. **Production Deployment**
   - Schedule maintenance window
   - Deploy Angular 21 app
   - Verify all features
   - Monitor error rates

4. **Post-Cutover**
   - Monitor performance metrics
   - Address any issues
   - Archive Angular 9 codebase
   - Update documentation

### 6.4 Rollback Plan

If critical issues are discovered post-cutover:

1. Revert to Angular 9 deployment
2. Document issues encountered
3. Fix issues in Angular 21 codebase
4. Re-test thoroughly
5. Reschedule cutover

---

## Risk Mitigation

### Identified Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| API incompatibility | High | Low | Thorough API contract testing |
| Stripe integration issues | High | Medium | Early integration testing, Stripe test mode |
| Performance regression | Medium | Low | Continuous benchmarking |
| Team skill gap | Medium | Medium | Pair programming, documentation |
| Timeline slippage | Medium | Medium | Buffer time in estimates |
| Browser compatibility | Low | Low | Cross-browser testing matrix |

### Contingency Plans

1. **API Issues**: Maintain API contract tests; API team on standby
2. **Stripe Issues**: Have Stripe support contact ready; test with multiple accounts
3. **Performance**: Profile early; optimize critical paths first
4. **Timeline**: Prioritize core features; defer nice-to-haves

---

## Appendix: Breaking Changes Reference

### RxJS 6 to 7 Changes

```typescript
// Before (RxJS 6)
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

// After (RxJS 7+)
import { throwError, catchError } from 'rxjs';

// firstValueFrom replaces .toPromise()
import { firstValueFrom } from 'rxjs';
const result = await firstValueFrom(observable$);
```

### Angular Control Flow

```html
<!-- Before (Angular 9) -->
<div *ngIf="condition">Content</div>
<div *ngFor="let item of items">{{ item }}</div>
<div [ngSwitch]="value">
  <span *ngSwitchCase="'a'">A</span>
</div>

<!-- After (Angular 21) -->
@if (condition) {
  <div>Content</div>
}

@for (item of items; track item.id) {
  <div>{{ item }}</div>
}

@switch (value) {
  @case ('a') {
    <span>A</span>
  }
}
```

### Standalone Components

```typescript
// Before (Angular 9)
@NgModule({
  declarations: [MyComponent],
  imports: [CommonModule],
  exports: [MyComponent]
})
export class MyModule {}

// After (Angular 21)
@Component({
  selector: 'app-my',
  standalone: true,
  imports: [CommonModule]
})
export class MyComponent {}
```

### Dependency Injection

```typescript
// Before (Angular 9)
constructor(private service: MyService) {}

// After (Angular 21)
private service = inject(MyService);
```

### Signal-based State

```typescript
// Before (Angular 9) - BehaviorSubject
private dataSubject = new BehaviorSubject<Data>(null);
data$ = this.dataSubject.asObservable();

// After (Angular 21) - Signals
private dataSignal = signal<Data | null>(null);
readonly data = this.dataSignal.asReadonly();
```

---

## Summary Timeline

| Phase | Duration | Key Deliverables |
|-------|----------|------------------|
| Phase 0: Preparation | 1-2 days | Documentation, environment setup |
| Phase 1: Scaffolding | 1-2 days | New Angular 21 project |
| Phase 2: Core | 3-4 days | Services, interceptors, guards |
| Phase 3: Shared | 2-3 days | Reusable components |
| Phase 4: Features | 8-10 days | All feature modules |
| Phase 5: Integration | 3-4 days | Testing, bug fixes |
| Phase 6: Cutover | 2-3 days | Production deployment |
| **Total** | **~4-5 weeks** | Complete migration |

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-23 | Claude | Initial plan created |

---

## References

- [Angular Update Guide](https://angular.dev/update-guide)
- [Angular Signals](https://angular.dev/guide/signals)
- [Standalone Components](https://angular.dev/guide/standalone-components)
- [New Control Flow](https://angular.dev/guide/templates/control-flow)
- [Stripe.js Documentation](https://stripe.com/docs/js)
