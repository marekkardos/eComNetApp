> **Parent Document**: [ANGULAR-21-MIGRATION-PLAN.md](./ANGULAR-21-MIGRATION-PLAN.md) - This document is Phase 4 of the main migration plan.

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

- [x] Home feature (landing page)
- [x] Shop feature (products, filtering, pagination, details)
- [x] Account feature (login, register, external/social logins)
- [x] Basket feature (cart management)
- [x] Checkout feature (multi-step with Stripe)
- [x] Orders feature (history, details)
- [x] All routes configured

---