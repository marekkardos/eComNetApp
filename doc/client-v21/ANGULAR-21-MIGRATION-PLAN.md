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
   - [1.7 Static UI Implementation](#17-static-ui-implementation-mock-data-phase) → See [Static UI Plan](./federated-tickling-dijkstra.md)
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
├── src/
│   ├── client/                # Angular 9 app (comparison baseline)
│   │   ├── src/
│   │   │   ├── app/
│   │   │   │   ├── account/
│   │   │   │   ├── basket/
│   │   │   │   ├── checkout/
│   │   │   │   ├── core/
│   │   │   │   ├── home/
│   │   │   │   ├── orders/
│   │   │   │   ├── shared/
│   │   │   │   └── shop/
│   │   │   └── environments/
│   │   ├── angular.json
│   │   └── package.json
│   │
│   └── client-v21/            # Angular 21 app (new implementation)
│       ├── src/
│       │   ├── app/
│       │   │   ├── core/      # Standalone components
│       │   │   ├── shared/    # Shared utilities & components
│       │   │   ├── features/  # Feature modules
│       │   │   │   ├── account/
│       │   │   │   ├── basket/
│       │   │   │   ├── checkout/
│       │   │   │   ├── home/
│       │   │   │   ├── orders/
│       │   │   │   └── shop/
│       │   │   ├── app.component.ts
│       │   │   ├── app.config.ts
│       │   │   └── app.routes.ts
│       │   └── environments/
│       ├── angular.json
│       └── package.json
│
├── API/                       # Backend API (shared by both)
│
├── docker-compose.yml         # Run both frontends + API
├── docker-compose.migration.yml  # Migration-specific compose
│
└── doc/client-v21/            # Migration documentation
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
| `bootstrap` | 4.1.1 | **Tailwind CSS** | Complete redesign with utility-first CSS |
| `ngx-bootstrap` | 5.3.2 | **@angular/material** | Material components for forms, dialogs, stepper |
| `ngx-toastr` | 11.3.3 | 19.x+ | Standalone support |
| `ngx-spinner` | 8.1.0 | 17.x+ | Standalone support |
| `xng-breadcrumb` | 5.0.1 | 11.x+ | Standalone support |
| `font-awesome` | 4.7.0 | **Material Icons** | Bundled with Angular Material |
| `uuid` | 3.4.0 | `crypto.randomUUID()` | Built-in browser API |
| `zone.js` | 0.10.2 | 0.15.x (optional) | Zoneless possible |

### UI Framework Decision

The Angular 21 app will use **Angular Material + Tailwind CSS** instead of Bootstrap:

| Concern | Technology | Rationale |
|---------|------------|-----------|
| **Forms & Dialogs** | Angular Material | Consistent form controls, validation, accessibility |
| **Stepper/Wizard** | Angular Material | Built-in `mat-horizontal-stepper` for checkout |
| **Layout & Cards** | Tailwind CSS | Utility-first responsive design |
| **Navigation** | Tailwind CSS | Custom navbar with modern styling |
| **Product Grids** | Tailwind CSS | Flexible responsive grids |
| **Icons** | Material Icons | Consistent with Material components |

**Design Theme**: Teal/Cyan primary (#00897b) with clean, minimal, modern e-commerce aesthetic.

---

## Phase 0: Pre-Migration Preparation

**Duration**: 1-2 days
**Branch**: `feature/modernize`

### Checklist

- [ ] **0.1** Review and document all Angular 9 features
- [ ] **0.2** Identify deprecated APIs and patterns
- [ ] **0.3** Create API contract documentation
- [ ] **0.4** Set up comparison testing environment
- [ ] **0.5** Update Node.js to ^20.19.0 || ^22.12.0 || ^24.0.0 (Angular 21 requirement), install TypeScript >=5.9.0 <6.0.0
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
# Navigate to src folder
cd C:\work\github\repos\eComNetApp\src

# Create Angular 21 project with modern defaults
ng new client-v21 --style=scss --routing=true --ssr=false --standalone=true --skip-git --package-manager=npm

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
# Angular Material
ng add @angular/material

# Tailwind CSS
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init

# Additional packages
npm install ngx-toastr ngx-spinner xng-breadcrumb @stripe/stripe-js

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
              "@angular/material/prebuilt-themes/indigo-pink.css",
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

#### 1.4.1 Configure Tailwind (`tailwind.config.js`)

```js
module.exports = {
  content: ["./src/**/*.{html,ts}"],
  theme: {
    extend: {
      colors: {
        primary: '#00897b',    // Teal (Material teal-600)
        accent: '#ff4081',     // Material pink accent
      }
    }
  },
  plugins: []
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
import { provideHttpClient } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(),  // Interceptors will be added in Phase 2
    provideAnimations(),
    provideToastr({
      positionClass: 'toast-bottom-right',
      preventDuplicates: true
    })
  ]
};
```

#### 1.7 Static UI Implementation (Mock Data Phase)

Before wiring up to the real API, build all UI components with static/hardcoded data. This approach allows:

- **Rapid UI iteration** without API dependencies
- **Visual verification** of designs before backend integration
- **Parallel development** - UI can be built while API contracts are finalized
- **Easy testing** of component layouts and responsiveness

**Detailed Implementation**: See [Static UI Implementation Plan](./federated-tickling-dijkstra.md) for:

| Sub-Phase | Description | Key Components |
|-----------|-------------|----------------|
| **1A** | Project Scaffolding & Auth Pages | Login, Register, Auth Layout |
| **1B** | Core Shopping Experience | Navbar, Home, Shop, Product Cards, Product Detail |
| **1C** | Cart & Checkout | Cart Page, Checkout Stepper, Payment UI |
| **1D** | Orders | Order List, Order Detail |

**Mock Data Structure** (`shared/mock-data/`):
- `products.mock.ts` - Sample products, brands, types
- `user.mock.ts` - Test user and address
- `basket.mock.ts` - Cart items and delivery methods
- `orders.mock.ts` - Order history

**Key Design Decisions** (from Static UI Plan):
- Centered card layout for auth pages
- Sidebar filters on desktop, drawer on mobile for shop
- Horizontal Material stepper with accordion sections for checkout
- Placeholder images via picsum.photos

### Deliverables

- [ ] New Angular 21 project in `client-v21/`
- [ ] Project structure with feature directories
- [ ] Dependencies installed and configured (Material + Tailwind)
- [ ] Tailwind configuration with custom theme colors
- [ ] Environment files created
- [ ] App config with providers
- [ ] Development server running on port 4201
- [ ] **Static UI Phase Complete**:
  - [ ] Auth layout and pages (login/register) with mock login
  - [ ] Main layout with navbar (cart badge, user menu)
  - [ ] Home page with hero section
  - [ ] Shop page with product grid, filters, pagination (mock data)
  - [ ] Product detail page
  - [ ] Cart page with quantity controls
  - [ ] Checkout stepper (address → delivery → review → payment)
  - [ ] Orders list and detail pages
  - [ ] Responsive design verified (mobile/tablet/desktop)

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
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';

@Component({
  selector: 'app-pager',
  standalone: true,
  imports: [MatPaginatorModule],
  template: `
    <mat-paginator
      [length]="totalCount()"
      [pageSize]="pageSize()"
      [pageIndex]="pageNumber() - 1"
      [pageSizeOptions]="[6, 12, 24]"
      (page)="onPageChange($event)">
    </mat-paginator>
  `
})
export class PagerComponent {
  totalCount = input.required<number>();
  pageSize = input.required<number>();
  pageNumber = input.required<number>();
  pageChanged = output<number>();

  onPageChange(event: PageEvent): void {
    this.pageChanged.emit(event.pageIndex + 1);
  }
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

**Detailed Implementation**: See [Phase 4: Feature Modules](./Phase-4-Feature-Modules.md) for:

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
| Phase 1: Scaffolding | 1-2 days | New Angular 21 project, Material + Tailwind setup |
| Phase 1.7: Static UI | 3-5 days | All UI components with mock data ([details](./federated-tickling-dijkstra.md)) |
| Phase 2: Core | 3-4 days | Services, interceptors, guards |
| Phase 3: Shared | 2-3 days | Reusable components |
| Phase 4: Features | 8-10 days | All feature modules wired to API |
| Phase 5: Integration | 3-4 days | Testing, bug fixes |
| Phase 6: Cutover | 2-3 days | Production deployment |
| **Total** | **~5-6 weeks** | Complete migration |

---

## Related Documents

- **[Static UI Implementation Plan](./federated-tickling-dijkstra.md)** - Detailed UI component designs, mock data structures, and verification checklists for Phase 1.7

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-23 | Claude | Initial plan created |
| 1.1 | 2026-01-26 | Claude | Integrated Static UI plan (Phase 1.7), updated tech stack to Material + Tailwind |

---

## References

- [Angular Update Guide](https://angular.dev/update-guide)
- [Angular Signals](https://angular.dev/guide/signals)
- [Standalone Components](https://angular.dev/guide/standalone-components)
- [New Control Flow](https://angular.dev/guide/templates/control-flow)
- [Angular Material](https://material.angular.io/)
- [Tailwind CSS](https://tailwindcss.com/)
- [Stripe.js Documentation](https://stripe.com/docs/js)
