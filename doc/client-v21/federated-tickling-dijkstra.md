# Angular 21 Migration - Static UI Implementation Plan

> **Parent Document**: [ANGULAR-21-MIGRATION-PLAN.md](./ANGULAR-21-MIGRATION-PLAN.md) - This document is Phase 1.7 of the main migration plan.

## Overview

Build a new Angular 21 e-commerce application with Material + Tailwind CSS, starting with static/hardcoded data components before wiring up to the real API.

**Design Choices**:
- **Theme**: Teal/Cyan primary color (#00897b) - fresh, modern startup feel
- **Images**: Placeholder images via picsum.photos (easy to replace later)
- **Layout**: Auth pages use centered card layout, Google sign-in placeholder included
- **Filters**: Sidebar on desktop, collapsible drawer on mobile
- **Checkout**: Horizontal Material stepper with accordion sections

**Design Philosophy**: Clean, minimal, modern e-commerce aesthetic with generous whitespace, subtle shadows, and product imagery as hero elements.

**Tech Stack**:
- Angular 21 (standalone components, signals)
- Angular Material (forms, dialogs, stepper, progress indicators)
- Tailwind CSS (layout, cards, navigation, product grids)
- Stripe.js (payment integration - later phase)

---

## Phase 1A: Project Scaffolding & Auth Pages

### 1.1 Create Angular 21 Project

```bash
cd C:\work\github\repos\eComNetApp\src
ng new client-v21 --style=scss --routing=true --ssr=false --standalone=true --skip-git
cd client-v21
```

### 1.2 Install Dependencies

```bash
# Angular Material
ng add @angular/material

# Tailwind CSS
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init

# Additional packages
npm install ngx-toastr ngx-spinner xng-breadcrumb @stripe/stripe-js
```

### 1.3 Configure Tailwind (`tailwind.config.js`)

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

### 1.4 Configure Angular (`angular.json`)

- Set dev server port to 4201
- Add Tailwind to styles preprocessing
- Configure Material theme

### 1.5 Project Structure

```
client-v21/src/app/
├── core/
│   ├── components/
│   │   ├── navbar/
│   │   ├── not-found/
│   │   └── server-error/
│   ├── guards/
│   ├── interceptors/
│   ├── services/
│   └── layouts/
│       ├── main-layout/      # Layout with navbar
│       └── auth-layout/      # Centered card layout
├── shared/
│   ├── components/
│   │   ├── text-input/
│   │   ├── order-totals/
│   │   └── basket-summary/
│   ├── models/
│   └── mock-data/            # Hardcoded data for static phase
├── features/
│   ├── auth/
│   │   ├── login/
│   │   └── register/
│   ├── home/
│   ├── shop/
│   │   ├── product-list/
│   │   └── product-detail/
│   ├── cart/
│   ├── checkout/
│   │   ├── checkout-address/
│   │   ├── checkout-delivery/
│   │   ├── checkout-review/
│   │   ├── checkout-payment/
│   │   └── checkout-success/
│   └── orders/
│       ├── order-list/
│       └── order-detail/
├── app.component.ts
├── app.config.ts
└── app.routes.ts
```

### 1.6 Create Mock Data

**File: `shared/mock-data/products.mock.ts`**
```typescript
export const MOCK_PRODUCTS: Product[] = [
  {
    id: 1,
    name: 'Angular Purple Boots',
    description: 'Premium leather boots with Angular logo',
    price: 199.99,
    pictureUrl: 'https://picsum.photos/seed/boot1/400/400',
    productType: 'Boots',
    productBrand: 'Angular'
  },
  // ... 12 products total
];

export const MOCK_BRANDS = [
  { id: 1, name: 'Angular' },
  { id: 2, name: 'React' },
  { id: 3, name: 'Vue' },
  { id: 4, name: 'TypeScript' }
];

export const MOCK_TYPES = [
  { id: 1, name: 'Boots' },
  { id: 2, name: 'Sneakers' },
  { id: 3, name: 'Sandals' },
  { id: 4, name: 'Hats' }
];
```

**File: `shared/mock-data/user.mock.ts`**
```typescript
export const MOCK_USER: User = {
  email: 'test@test.com',
  displayName: 'Test User',
  token: 'mock-jwt-token'
};

export const MOCK_ADDRESS: Address = {
  firstName: 'John',
  lastName: 'Doe',
  street: '123 Main St',
  city: 'New York',
  state: 'NY',
  zipcode: '10001'
};
```

**File: `shared/mock-data/basket.mock.ts`**
```typescript
export const MOCK_BASKET_ITEMS: BasketItem[] = [
  {
    id: 1,
    productName: 'Angular Purple Boots',
    price: 199.99,
    quantity: 2,
    pictureUrl: 'https://picsum.photos/seed/boot1/400/400',
    brand: 'Angular',
    type: 'Boots'
  },
  {
    id: 2,
    productName: 'React Blue Hat',
    price: 29.99,
    quantity: 1,
    pictureUrl: 'https://picsum.photos/seed/hat1/400/400',
    brand: 'React',
    type: 'Hats'
  }
];

export const MOCK_DELIVERY_METHODS: DeliveryMethod[] = [
  { id: 1, shortName: 'UPS1', deliveryTime: '1-2 Days', description: 'Fastest delivery', price: 10 },
  { id: 2, shortName: 'UPS2', deliveryTime: '2-5 Days', description: 'Get it within 5 days', price: 5 },
  { id: 3, shortName: 'UPS3', deliveryTime: '5-10 Days', description: 'Slower but cheap', price: 2 },
  { id: 4, shortName: 'FREE', deliveryTime: '1-2 Weeks', description: 'Free! You get what you pay for', price: 0 }
];
```

---

### 1.7 Auth Layout (Centered Card)

**File: `core/layouts/auth-layout/auth-layout.component.ts`**
```typescript
@Component({
  selector: 'app-auth-layout',
  standalone: true,
  imports: [RouterOutlet],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-slate-100 to-slate-200 flex items-center justify-center p-4">
      <div class="w-full max-w-md">
        <router-outlet />
      </div>
    </div>
  `
})
export class AuthLayoutComponent {}
```

### 1.8 Login Page

**File: `features/auth/login/login.component.ts`**

Design elements:
- Centered white card with shadow
- Logo/brand at top
- Material form fields (email, password)
- Primary "Sign in" button (Material raised)
- "Sign in with Google" button (outlined with Google icon)
- Link to register page
- Error message display

```html
<div class="bg-white rounded-2xl shadow-xl p-8">
  <!-- Logo -->
  <div class="text-center mb-8">
    <h1 class="text-3xl font-bold text-gray-900">Skinet</h1>
    <p class="text-gray-500 mt-2">Sign in to your account</p>
  </div>

  <!-- Form -->
  <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
    <mat-form-field appearance="outline" class="w-full">
      <mat-label>Email</mat-label>
      <input matInput formControlName="email" type="email">
      <mat-error>Valid email required</mat-error>
    </mat-form-field>

    <mat-form-field appearance="outline" class="w-full mt-4">
      <mat-label>Password</mat-label>
      <input matInput formControlName="password" type="password">
      <mat-error>Password required</mat-error>
    </mat-form-field>

    @if (loginError) {
      <div class="text-red-500 text-sm mt-2">Invalid email or password</div>
    }

    <button mat-raised-button color="primary" class="w-full mt-6 py-3" type="submit">
      Sign in
    </button>
  </form>

  <!-- Divider -->
  <div class="flex items-center my-6">
    <div class="flex-1 border-t border-gray-300"></div>
    <span class="px-4 text-gray-500 text-sm">or</span>
    <div class="flex-1 border-t border-gray-300"></div>
  </div>

  <!-- Google Sign In -->
  <button mat-stroked-button class="w-full py-3 flex items-center justify-center gap-2">
    <img src="assets/google-icon.svg" class="w-5 h-5" alt="Google">
    Sign in with Google
  </button>

  <!-- Register Link -->
  <p class="text-center mt-6 text-gray-600">
    Don't have an account?
    <a routerLink="/auth/register" class="text-primary font-medium hover:underline">Sign up</a>
  </p>
</div>
```

### 1.9 Register Page

Similar to login with additional fields:
- Display Name (Material form field)
- Email with async validation indicator
- Password with strength indicator (optional)
- Same Google sign-up option
- Link back to login

---

## Phase 1B: Core Shopping Experience

### 2.1 Main Layout (Navbar)

**File: `core/layouts/main-layout/main-layout.component.ts`**

Design elements:
- Fixed top navbar with white background + shadow
- Logo on left
- Navigation links (Shop)
- Search bar (expandable on mobile)
- Cart icon with badge (item count)
- User menu (login/signup or dropdown)

```html
<nav class="fixed top-0 left-0 right-0 bg-white shadow-sm z-50">
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
    <div class="flex justify-between items-center h-16">
      <!-- Logo -->
      <a routerLink="/" class="text-2xl font-bold text-primary">Skinet</a>

      <!-- Nav Links -->
      <div class="hidden md:flex items-center space-x-8">
        <a routerLink="/shop" routerLinkActive="text-primary" class="text-gray-600 hover:text-primary">
          Shop
        </a>
      </div>

      <!-- Right Section -->
      <div class="flex items-center space-x-4">
        <!-- Cart -->
        <a routerLink="/cart" class="relative p-2">
          <mat-icon>shopping_cart</mat-icon>
          @if (cartItemCount() > 0) {
            <span class="absolute -top-1 -right-1 bg-accent text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
              {{ cartItemCount() }}
            </span>
          }
        </a>

        <!-- User Menu -->
        @if (isLoggedIn()) {
          <button mat-button [matMenuTriggerFor]="userMenu">
            {{ currentUser()?.displayName }}
            <mat-icon>arrow_drop_down</mat-icon>
          </button>
          <mat-menu #userMenu="matMenu">
            <a mat-menu-item routerLink="/orders">My Orders</a>
            <button mat-menu-item (click)="logout()">Logout</button>
          </mat-menu>
        } @else {
          <a mat-stroked-button routerLink="/auth/login">Login</a>
        }
      </div>
    </div>
  </div>
</nav>

<main class="pt-16">
  <router-outlet />
</main>
```

### 2.2 Home Page

Design elements:
- Hero carousel (3 slides with product imagery)
- "New Arrivals" section with product grid (4 items)
- Optional: Categories section, Newsletter signup

```html
<div>
  <!-- Hero Carousel -->
  <div class="relative h-[500px] bg-gradient-to-r from-slate-800 to-slate-900">
    <!-- Carousel implementation with swiper or custom -->
    <div class="absolute inset-0 flex items-center justify-center text-white">
      <div class="text-center">
        <h1 class="text-5xl font-bold mb-4">Welcome to Skinet</h1>
        <p class="text-xl mb-8">Discover our latest collection</p>
        <a routerLink="/shop" mat-raised-button color="primary" class="text-lg px-8 py-3">
          Shop Now
        </a>
      </div>
    </div>
  </div>

  <!-- New Arrivals -->
  <section class="max-w-7xl mx-auto px-4 py-16">
    <h2 class="text-3xl font-bold text-gray-900 mb-8">New Arrivals</h2>
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
      @for (product of newArrivals; track product.id) {
        <app-product-card [product]="product" />
      }
    </div>
  </section>
</div>
```

### 2.3 Shop Page (Product List)

Design elements:
- Sidebar filters (desktop) / Drawer (mobile)
  - Sort dropdown
  - Brands list with checkmarks
  - Types list with checkmarks
- Search bar
- Results header ("Showing X-Y of Z results")
- Product grid (3 columns desktop, 2 tablet, 1 mobile)
- Pagination

```html
<div class="max-w-7xl mx-auto px-4 py-8">
  <div class="flex flex-col lg:flex-row gap-8">

    <!-- Filters Sidebar (Desktop) -->
    <aside class="hidden lg:block w-64 flex-shrink-0">
      <div class="sticky top-24">
        <!-- Sort -->
        <div class="mb-6">
          <h3 class="font-semibold text-gray-900 mb-3">Sort By</h3>
          <mat-form-field appearance="outline" class="w-full">
            <mat-select [(value)]="selectedSort" (selectionChange)="onSortChange($event)">
              @for (option of sortOptions; track option.value) {
                <mat-option [value]="option.value">{{ option.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        </div>

        <!-- Brands -->
        <div class="mb-6">
          <h3 class="font-semibold text-gray-900 mb-3">Brands</h3>
          <ul class="space-y-2">
            @for (brand of brands; track brand.id) {
              <li>
                <button
                  (click)="onBrandSelect(brand.id)"
                  [class.text-primary]="selectedBrandId === brand.id"
                  [class.font-semibold]="selectedBrandId === brand.id"
                  class="text-gray-600 hover:text-primary">
                  {{ brand.name }}
                </button>
              </li>
            }
          </ul>
        </div>

        <!-- Types -->
        <div class="mb-6">
          <h3 class="font-semibold text-gray-900 mb-3">Types</h3>
          <ul class="space-y-2">
            @for (type of types; track type.id) {
              <li>
                <button
                  (click)="onTypeSelect(type.id)"
                  [class.text-primary]="selectedTypeId === type.id"
                  [class.font-semibold]="selectedTypeId === type.id"
                  class="text-gray-600 hover:text-primary">
                  {{ type.name }}
                </button>
              </li>
            }
          </ul>
        </div>
      </div>
    </aside>

    <!-- Mobile Filter Button -->
    <button mat-stroked-button class="lg:hidden mb-4" (click)="openFilterDrawer()">
      <mat-icon>filter_list</mat-icon> Filters
    </button>

    <!-- Products Section -->
    <div class="flex-1">
      <!-- Search & Results Header -->
      <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
        <span class="text-gray-600">
          Showing {{ startIndex }}-{{ endIndex }} of {{ totalCount }} results
        </span>
        <mat-form-field appearance="outline" class="w-full sm:w-64">
          <mat-label>Search products</mat-label>
          <input matInput [(ngModel)]="searchTerm" (keyup.enter)="onSearch()">
          <mat-icon matSuffix>search</mat-icon>
        </mat-form-field>
      </div>

      <!-- Product Grid -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
        @for (product of products; track product.id) {
          <app-product-card [product]="product" />
        }
      </div>

      <!-- Pagination -->
      <div class="mt-8 flex justify-center">
        <mat-paginator
          [length]="totalCount"
          [pageSize]="pageSize"
          [pageIndex]="pageIndex"
          (page)="onPageChange($event)"
          [pageSizeOptions]="[6, 12, 24]">
        </mat-paginator>
      </div>
    </div>
  </div>
</div>
```

### 2.4 Product Card Component

```html
<div class="group bg-white rounded-xl shadow-sm overflow-hidden hover:shadow-lg transition-shadow">
  <!-- Image Container -->
  <div class="relative aspect-square bg-gray-100">
    <img [src]="product.pictureUrl" [alt]="product.name"
         class="w-full h-full object-cover group-hover:scale-105 transition-transform">

    <!-- Hover Overlay -->
    <div class="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-3">
      <button mat-fab color="primary" (click)="addToCart($event)">
        <mat-icon>add_shopping_cart</mat-icon>
      </button>
      <a mat-fab color="accent" [routerLink]="['/shop', product.id]">
        <mat-icon>visibility</mat-icon>
      </a>
    </div>
  </div>

  <!-- Content -->
  <div class="p-4">
    <a [routerLink]="['/shop', product.id]" class="block">
      <h3 class="font-semibold text-gray-900 truncate hover:text-primary">
        {{ product.name }}
      </h3>
    </a>
    <p class="text-primary font-bold text-lg mt-2">
      {{ product.price | currency }}
    </p>
  </div>
</div>
```

### 2.5 Product Detail Page

```html
<div class="max-w-7xl mx-auto px-4 py-8">
  <div class="grid grid-cols-1 lg:grid-cols-2 gap-12">
    <!-- Image -->
    <div class="aspect-square bg-gray-100 rounded-2xl overflow-hidden">
      <img [src]="product.pictureUrl" [alt]="product.name" class="w-full h-full object-cover">
    </div>

    <!-- Details -->
    <div>
      <h1 class="text-3xl font-bold text-gray-900">{{ product.name }}</h1>
      <p class="text-3xl text-primary font-bold mt-4">{{ product.price | currency }}</p>

      <!-- Quantity Selector -->
      <div class="flex items-center gap-4 mt-8">
        <button mat-icon-button (click)="decrementQuantity()" [disabled]="quantity <= 1">
          <mat-icon>remove_circle</mat-icon>
        </button>
        <span class="text-2xl font-semibold w-12 text-center">{{ quantity }}</span>
        <button mat-icon-button (click)="incrementQuantity()">
          <mat-icon>add_circle</mat-icon>
        </button>
      </div>

      <!-- Add to Cart -->
      <button mat-raised-button color="primary" class="mt-8 py-3 px-8 text-lg" (click)="addToCart()">
        <mat-icon class="mr-2">add_shopping_cart</mat-icon>
        Add to Cart
      </button>

      <!-- Back Button -->
      <button mat-stroked-button class="mt-4 ml-4" (click)="goBack()">
        Back to Shop
      </button>

      <!-- Description -->
      <div class="mt-12">
        <h3 class="text-xl font-semibold text-gray-900 mb-4">Description</h3>
        <p class="text-gray-600 leading-relaxed">{{ product.description }}</p>
      </div>
    </div>
  </div>
</div>
```

---

## Phase 1C: Cart & Checkout

### 3.1 Cart Page

```html
<div class="max-w-7xl mx-auto px-4 py-8">
  <h1 class="text-3xl font-bold text-gray-900 mb-8">Shopping Cart</h1>

  @if (cartItems.length === 0) {
    <div class="text-center py-16">
      <mat-icon class="text-6xl text-gray-300">shopping_cart</mat-icon>
      <p class="text-xl text-gray-500 mt-4">Your cart is empty</p>
      <a routerLink="/shop" mat-raised-button color="primary" class="mt-6">
        Continue Shopping
      </a>
    </div>
  } @else {
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
      <!-- Cart Items -->
      <div class="lg:col-span-2">
        <div class="bg-white rounded-xl shadow-sm overflow-hidden">
          <table class="w-full">
            <thead class="bg-gray-50">
              <tr>
                <th class="text-left p-4">Product</th>
                <th class="text-center p-4">Price</th>
                <th class="text-center p-4">Quantity</th>
                <th class="text-center p-4">Total</th>
                <th class="p-4"></th>
              </tr>
            </thead>
            <tbody>
              @for (item of cartItems; track item.id) {
                <tr class="border-t">
                  <td class="p-4">
                    <div class="flex items-center gap-4">
                      <img [src]="item.pictureUrl" class="w-16 h-16 rounded-lg object-cover">
                      <div>
                        <a [routerLink]="['/shop', item.id]" class="font-semibold hover:text-primary">
                          {{ item.productName }}
                        </a>
                        <p class="text-sm text-gray-500">{{ item.type }}</p>
                      </div>
                    </div>
                  </td>
                  <td class="text-center p-4">{{ item.price | currency }}</td>
                  <td class="text-center p-4">
                    <div class="flex items-center justify-center gap-2">
                      <button mat-icon-button (click)="decrementItem(item)">
                        <mat-icon>remove</mat-icon>
                      </button>
                      <span class="font-semibold">{{ item.quantity }}</span>
                      <button mat-icon-button (click)="incrementItem(item)">
                        <mat-icon>add</mat-icon>
                      </button>
                    </div>
                  </td>
                  <td class="text-center p-4 font-semibold">
                    {{ item.price * item.quantity | currency }}
                  </td>
                  <td class="p-4">
                    <button mat-icon-button color="warn" (click)="removeItem(item)">
                      <mat-icon>delete</mat-icon>
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>

      <!-- Order Summary -->
      <div>
        <app-order-totals [totals]="cartTotals" />
        <a routerLink="/checkout" mat-raised-button color="primary" class="w-full mt-4 py-3">
          Proceed to Checkout
        </a>
      </div>
    </div>
  }
</div>
```

### 3.2 Checkout Page (Single Page with Horizontal Stepper + Accordions)

```html
<div class="max-w-7xl mx-auto px-4 py-8">
  <h1 class="text-3xl font-bold text-gray-900 mb-8">Checkout</h1>

  <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
    <!-- Main Content -->
    <div class="lg:col-span-2">
      <!-- Horizontal Stepper -->
      <mat-horizontal-stepper linear #stepper>

        <!-- Step 1: Address -->
        <mat-step [stepControl]="addressForm" label="Address">
          <mat-accordion>
            <mat-expansion-panel expanded>
              <mat-expansion-panel-header>
                <mat-panel-title>Shipping Address</mat-panel-title>
              </mat-expansion-panel-header>

              <form [formGroup]="addressForm" class="grid grid-cols-2 gap-4 py-4">
                <mat-form-field appearance="outline">
                  <mat-label>First Name</mat-label>
                  <input matInput formControlName="firstName">
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Last Name</mat-label>
                  <input matInput formControlName="lastName">
                </mat-form-field>
                <mat-form-field appearance="outline" class="col-span-2">
                  <mat-label>Street Address</mat-label>
                  <input matInput formControlName="street">
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>City</mat-label>
                  <input matInput formControlName="city">
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>State</mat-label>
                  <input matInput formControlName="state">
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Zip Code</mat-label>
                  <input matInput formControlName="zipcode">
                </mat-form-field>
              </form>
            </mat-expansion-panel>
          </mat-accordion>

          <div class="flex justify-between mt-6">
            <a routerLink="/cart" mat-stroked-button>Back to Cart</a>
            <button mat-raised-button color="primary" matStepperNext>
              Continue to Delivery
            </button>
          </div>
        </mat-step>

        <!-- Step 2: Delivery -->
        <mat-step [stepControl]="deliveryForm" label="Delivery">
          <mat-accordion>
            <mat-expansion-panel expanded>
              <mat-expansion-panel-header>
                <mat-panel-title>Delivery Method</mat-panel-title>
              </mat-expansion-panel-header>

              <mat-radio-group formControlName="deliveryMethod" class="flex flex-col gap-4 py-4">
                @for (method of deliveryMethods; track method.id) {
                  <mat-radio-button [value]="method.id">
                    <div class="flex justify-between items-center w-full">
                      <div>
                        <span class="font-semibold">{{ method.shortName }}</span>
                        <span class="text-gray-500 ml-2">- {{ method.deliveryTime }}</span>
                        <p class="text-sm text-gray-500">{{ method.description }}</p>
                      </div>
                      <span class="font-bold">{{ method.price | currency }}</span>
                    </div>
                  </mat-radio-button>
                }
              </mat-radio-group>
            </mat-expansion-panel>
          </mat-accordion>

          <div class="flex justify-between mt-6">
            <button mat-stroked-button matStepperPrevious>Back</button>
            <button mat-raised-button color="primary" matStepperNext>
              Continue to Review
            </button>
          </div>
        </mat-step>

        <!-- Step 3: Review -->
        <mat-step label="Review">
          <mat-accordion>
            <mat-expansion-panel expanded>
              <mat-expansion-panel-header>
                <mat-panel-title>Order Review</mat-panel-title>
              </mat-expansion-panel-header>

              <app-basket-summary [items]="cartItems" [isReadOnly]="true" />
            </mat-expansion-panel>
          </mat-accordion>

          <div class="flex justify-between mt-6">
            <button mat-stroked-button matStepperPrevious>Back</button>
            <button mat-raised-button color="primary" matStepperNext>
              Continue to Payment
            </button>
          </div>
        </mat-step>

        <!-- Step 4: Payment -->
        <mat-step label="Payment">
          <mat-accordion>
            <mat-expansion-panel expanded>
              <mat-expansion-panel-header>
                <mat-panel-title>Payment Details</mat-panel-title>
              </mat-expansion-panel-header>

              <form class="py-4">
                <mat-form-field appearance="outline" class="w-full">
                  <mat-label>Name on Card</mat-label>
                  <input matInput formControlName="nameOnCard">
                </mat-form-field>

                <!-- Stripe Elements Placeholders -->
                <div class="grid grid-cols-3 gap-4 mt-4">
                  <div class="col-span-2">
                    <label class="block text-sm font-medium text-gray-700 mb-2">Card Number</label>
                    <div id="card-number" class="border rounded-lg p-4"></div>
                  </div>
                  <div>
                    <label class="block text-sm font-medium text-gray-700 mb-2">Expiry</label>
                    <div id="card-expiry" class="border rounded-lg p-4"></div>
                  </div>
                  <div>
                    <label class="block text-sm font-medium text-gray-700 mb-2">CVC</label>
                    <div id="card-cvc" class="border rounded-lg p-4"></div>
                  </div>
                </div>
              </form>
            </mat-expansion-panel>
          </mat-accordion>

          <div class="flex justify-between mt-6">
            <button mat-stroked-button matStepperPrevious>Back</button>
            <button mat-raised-button color="primary" (click)="submitOrder()" [disabled]="isSubmitting">
              @if (isSubmitting) {
                <mat-spinner diameter="20" class="mr-2"></mat-spinner>
              }
              Place Order
            </button>
          </div>
        </mat-step>

      </mat-horizontal-stepper>
    </div>

    <!-- Order Summary Sidebar -->
    <div>
      <app-order-totals [totals]="cartTotals" />
    </div>
  </div>
</div>
```

### 3.3 Checkout Success Page

```html
<div class="max-w-2xl mx-auto px-4 py-16 text-center">
  <mat-icon class="text-green-500 text-6xl">check_circle</mat-icon>
  <h1 class="text-3xl font-bold text-gray-900 mt-6">Thank You!</h1>
  <p class="text-xl text-gray-600 mt-4">Your order has been placed successfully.</p>

  @if (order) {
    <p class="text-gray-500 mt-2">Order #{{ order.id }}</p>
    <a [routerLink]="['/orders', order.id]" mat-raised-button color="primary" class="mt-8">
      View Order
    </a>
  } @else {
    <a routerLink="/orders" mat-raised-button color="primary" class="mt-8">
      View Orders
    </a>
  }
</div>
```

---

## Phase 1D: Orders

### 4.1 Orders List Page

```html
<div class="max-w-7xl mx-auto px-4 py-8">
  <h1 class="text-3xl font-bold text-gray-900 mb-8">My Orders</h1>

  @if (orders.length === 0) {
    <div class="text-center py-16">
      <mat-icon class="text-6xl text-gray-300">receipt_long</mat-icon>
      <p class="text-xl text-gray-500 mt-4">No orders yet</p>
      <a routerLink="/shop" mat-raised-button color="primary" class="mt-6">
        Start Shopping
      </a>
    </div>
  } @else {
    <div class="bg-white rounded-xl shadow-sm overflow-hidden">
      <table mat-table [dataSource]="orders" class="w-full">
        <ng-container matColumnDef="id">
          <th mat-header-cell *matHeaderCellDef>Order #</th>
          <td mat-cell *matCellDef="let order">{{ order.id }}</td>
        </ng-container>
        <ng-container matColumnDef="date">
          <th mat-header-cell *matHeaderCellDef>Date</th>
          <td mat-cell *matCellDef="let order">{{ order.orderDate | date:'medium' }}</td>
        </ng-container>
        <ng-container matColumnDef="total">
          <th mat-header-cell *matHeaderCellDef>Total</th>
          <td mat-cell *matCellDef="let order">{{ order.total | currency }}</td>
        </ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let order">
            <span class="px-3 py-1 rounded-full text-sm"
              [ngClass]="{
                'bg-yellow-100 text-yellow-800': order.status === 'Pending',
                'bg-green-100 text-green-800': order.status === 'Delivered'
              }">
              {{ order.status }}
            </span>
          </td>
        </ng-container>
        <ng-container matColumnDef="action">
          <th mat-header-cell *matHeaderCellDef></th>
          <td mat-cell *matCellDef="let order">
            <a mat-stroked-button [routerLink]="['/orders', order.id]">View</a>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
      </table>
    </div>
  }
</div>
```

### 4.2 Order Detail Page

```html
<div class="max-w-7xl mx-auto px-4 py-8">
  <h1 class="text-3xl font-bold text-gray-900 mb-2">Order #{{ order.id }}</h1>
  <p class="text-gray-500 mb-8">{{ order.orderDate | date:'full' }}</p>

  <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
    <!-- Order Items -->
    <div class="lg:col-span-2">
      <div class="bg-white rounded-xl shadow-sm p-6">
        <h2 class="text-xl font-semibold mb-4">Items</h2>
        <app-basket-summary [items]="order.orderItems" [isReadOnly]="true" />
      </div>
    </div>

    <!-- Order Summary -->
    <div>
      <app-order-totals [totals]="orderTotals" />

      <!-- Shipping Address -->
      <div class="bg-white rounded-xl shadow-sm p-6 mt-4">
        <h3 class="font-semibold mb-2">Shipping Address</h3>
        <p class="text-gray-600">
          {{ order.shipToAddress.firstName }} {{ order.shipToAddress.lastName }}<br>
          {{ order.shipToAddress.street }}<br>
          {{ order.shipToAddress.city }}, {{ order.shipToAddress.state }} {{ order.shipToAddress.zipcode }}
        </p>
      </div>
    </div>
  </div>
</div>
```

---

## Routing Configuration

```typescript
// app.routes.ts
export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      { path: '', redirectTo: 'shop', pathMatch: 'full' },
      { path: 'home', loadComponent: () => import('./features/home/home.component') },
      { path: 'shop', loadComponent: () => import('./features/shop/product-list/product-list.component') },
      { path: 'shop/:id', loadComponent: () => import('./features/shop/product-detail/product-detail.component') },
      { path: 'cart', loadComponent: () => import('./features/cart/cart.component') },
      { path: 'checkout', loadComponent: () => import('./features/checkout/checkout.component'), canActivate: [authGuard] },
      { path: 'checkout/success', loadComponent: () => import('./features/checkout/checkout-success/checkout-success.component') },
      { path: 'orders', loadComponent: () => import('./features/orders/order-list/order-list.component'), canActivate: [authGuard] },
      { path: 'orders/:id', loadComponent: () => import('./features/orders/order-detail/order-detail.component'), canActivate: [authGuard] },
      { path: 'not-found', loadComponent: () => import('./core/components/not-found/not-found.component') },
      { path: 'server-error', loadComponent: () => import('./core/components/server-error/server-error.component') },
    ]
  },
  {
    path: 'auth',
    component: AuthLayoutComponent,
    children: [
      { path: 'login', loadComponent: () => import('./features/auth/login/login.component') },
      { path: 'register', loadComponent: () => import('./features/auth/register/register.component') },
    ]
  },
  { path: '**', redirectTo: 'not-found' }
];
```

---

## Verification Checklist

After each phase, verify:

1. **Phase 1A (Auth)**
   - [ ] `ng serve` runs on port 4201
   - [ ] Login page displays with Material form fields
   - [ ] Register page displays with all fields
   - [ ] Form validation works (required, email pattern)
   - [ ] Google button placeholder present
   - [ ] Navigation between login/register works

2. **Phase 1B (Shopping)**
   - [ ] Home page displays with hero and product carousel
   - [ ] Shop page shows product grid with mock data
   - [ ] Filters sidebar renders brands/types
   - [ ] Product cards show hover overlay
   - [ ] Product detail page displays correctly
   - [ ] Pagination displays (even if not functional)

3. **Phase 1C (Cart & Checkout)**
   - [ ] Cart page shows mock items
   - [ ] Quantity controls render
   - [ ] Order totals component works
   - [ ] Checkout stepper navigates between steps
   - [ ] All form fields render in each step
   - [ ] Success page displays

4. **Phase 1D (Orders)**
   - [ ] Orders list displays mock orders
   - [ ] Order detail page shows items and totals

5. **Responsive**
   - [ ] All pages work on mobile (375px)
   - [ ] All pages work on tablet (768px)
   - [ ] All pages work on desktop (1920px)

---

## Files to Create/Modify

### New Files (client-v21/)
```
src/app/
├── app.config.ts
├── app.routes.ts
├── core/
│   ├── layouts/main-layout/main-layout.component.ts
│   ├── layouts/auth-layout/auth-layout.component.ts
│   └── components/navbar/navbar.component.ts
├── shared/
│   ├── models/*.ts (7 files)
│   ├── mock-data/*.ts (4 files)
│   └── components/order-totals/order-totals.component.ts
├── features/
│   ├── auth/login/login.component.ts
│   ├── auth/register/register.component.ts
│   ├── home/home.component.ts
│   ├── shop/product-list/product-list.component.ts
│   ├── shop/product-detail/product-detail.component.ts
│   ├── cart/cart.component.ts
│   ├── checkout/checkout.component.ts
│   ├── checkout/checkout-success/checkout-success.component.ts
│   ├── orders/order-list/order-list.component.ts
│   └── orders/order-detail/order-detail.component.ts
tailwind.config.js
```

### Configuration Files to Modify
```
angular.json (port, styles)
package.json (scripts)
tsconfig.json (paths if needed)
styles.scss (Tailwind imports, Material theme)
```
