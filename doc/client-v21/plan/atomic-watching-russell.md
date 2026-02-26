# Plan: Phase 4.4 — Basket Feature Wiring

## Context

Phase 1 delivered a fully-styled basket UI with mock data. Phase 2 delivered `BasketService` with all API methods. Phase 4.4 wires them together: replace the hardcoded mock state in `BasketComponent` with the real service signals, connect "Add to Cart" stubs in shop pages to the service, and bootstrap the basket from localStorage on app start.

**No new files needed.** Everything is already scaffolded — this is pure wiring.

---

## Angular Best Practices (v20+ rules applied)

- No `standalone: true` in decorators (it's the default)
- No `@HostBinding`/`@HostListener` — use `host` object instead
- Use `input()` / `output()` functions not decorators (ProductDetails already uses `input()`)
- `ChangeDetectionStrategy.OnPush` on all components ✓
- `inject()` for DI ✓
- Native control flow `@if`/`@for` ✓

---

## Files to Modify (5 files)

### 1. `src/app/features/basket/basket.component.ts`

**What changes:**
- Remove `MOCK_BASKET_ITEMS`, `MOCK_DELIVERY_METHODS` imports from `../../shared/mock-data`
- Add `inject(BasketService)`, `inject(ToastrService)`
- Add `BasketService` to imports list (add `import` line)
- Replace `items = signal<BasketItem[]>([...MOCK_BASKET_ITEMS])` with a `computed` reading from the service
- Replace four computed signals (`totalItems`, `subtotal`, `shipping`, `total`) with delegates to service signals
- Replace three local methods with service method calls

**No template changes needed** — signal names are preserved.

```typescript
// Remove these two imports:
import { MOCK_BASKET_ITEMS, MOCK_DELIVERY_METHODS } from '../../shared/mock-data';
// Keep: import { BasketItem } from '../../shared/models'; (still needed for method param types)

// Add these imports:
import { inject, computed } from '@angular/core';  // computed already needed, add inject
import { ToastrService } from 'ngx-toastr';
import { BasketService } from '../../core/services/basket.service';
```

New class body (replace everything after `export class BasketComponent {`):
```typescript
export class BasketComponent {
  private basketService = inject(BasketService);
  private toastr = inject(ToastrService);

  readonly items = computed(() => this.basketService.basket()?.items ?? []);
  readonly totalItems = this.basketService.itemCount;
  readonly subtotal = computed(() => this.basketService.totals()?.subtotal ?? 0);
  readonly shipping = computed(() => this.basketService.totals()?.shipping ?? 0);
  readonly total = computed(() => this.basketService.totals()?.total ?? 0);

  incrementQuantity(item: BasketItem): void {
    this.basketService.incrementItemQuantity(item);
  }

  decrementQuantity(item: BasketItem): void {
    this.basketService.decrementItemQuantity(item);
  }

  removeItem(item: BasketItem): void {
    this.basketService.removeItemFromBasket(item);
    this.toastr.info(`${item.productName} removed from basket`);
  }
}
```

---

### 2. `src/app/features/shop/shop.component.ts`

**What changes:** Wire `addToCart()` stub (line 277) to real service.

Add two injections to the class:
```typescript
private basketService = inject(BasketService);
private toastr = inject(ToastrService);
```

Replace the `addToCart` stub:
```typescript
addToCart(product: Product, event: Event): void {
  event.preventDefault();
  event.stopPropagation();
  this.basketService.addItemToBasket(product);
  this.toastr.success(`${product.name} added to basket`);
}
```

Add imports:
```typescript
import { ToastrService } from 'ngx-toastr';
import { BasketService } from '../../core/services/basket.service';
```

---

### 3. `src/app/features/shop/product-details/product-details.component.ts`

**What changes:** Wire `addToCart()` stub (line 222) to real service, passing the selected `quantity()`.

Add two injections to the class:
```typescript
private basketService = inject(BasketService);
private toastr = inject(ToastrService);
```

Replace the `addToCart` stub:
```typescript
addToCart(): void {
  const prod = this.product();
  if (prod) {
    this.basketService.addItemToBasket(prod, this.quantity());
    this.toastr.success(`${prod.name} added to basket`);
  }
}
```

Add imports:
```typescript
import { ToastrService } from 'ngx-toastr';
import { BasketService } from '../../../core/services/basket.service';
```

---

### 4. `src/app/app.config.ts`

**What changes:** Add a second `provideAppInitializer` to load the basket from localStorage on startup. This ensures the navbar badge populates immediately, not only after visiting the basket page.

```typescript
// Add to imports:
import { BasketService } from './core/services/basket.service';
import { firstValueFrom } from 'rxjs';

// Add alongside the existing provideAppInitializer:
provideAppInitializer(() => {
  const basketService = inject(BasketService);
  const basketId = localStorage.getItem('basket_id');
  if (basketId) {
    return firstValueFrom(basketService.getBasket(basketId)).catch(() => {
      localStorage.removeItem('basket_id');
    });
  }
  return Promise.resolve();
})
```

The `inject` token is already imported (used by the AccountService initializer).

---

### 5. `src/app/core/services/basket.service.ts`

**What changes:** Fix two deprecated `subscribe(next, error)` calls (deprecated in RxJS 7) to the modern object form. No logic change.

```typescript
// setBasket() — change:
.subscribe(
  response => { ... },
  error => console.log(error)
);
// to:
.subscribe({
  next: response => { ... },
  error: err => console.error(err)
});

// deleteBasket() — same pattern fix
```

---

## Implementation Order

1. `basket.service.ts` — fix subscribe pattern (no impact on tests)
2. `app.config.ts` — add basket init (enables basket count in navbar from page load)
3. `basket.component.ts` — wire to service (replaces mock data)
4. `shop.component.ts` — wire addToCart
5. `product-details.component.ts` — wire addToCart

---

## Verification

1. `npm start` — no compile errors
2. Add item from shop grid → basket badge in navbar increments immediately
3. Add item from product detail page with quantity > 1 → badge shows correct count
4. Navigate to `/basket` → items show, totals calculate correctly
5. Increment/decrement quantities in basket → API call fires (network tab), totals update
6. Remove item → item disappears, totals recalculate; if last item, basket clears
7. Refresh page → basket reloads from localStorage (items persist across refreshes)
8. Add same product twice → quantity increases (addOrUpdateItem logic), not duplicated
9. Navbar badge shows 0 / hidden when basket is empty
