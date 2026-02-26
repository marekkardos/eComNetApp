# Plan: Phase 4.1 Home Feature — Replace Mock Data with Real API

## Goal
Replace `MOCK_PRODUCTS` in `HomeComponent` with real product data from `GET /api/Products?sort=newest&pageSize=4`.

## Current State
- `home.component.ts` uses `MOCK_PRODUCTS.slice(0, 4)` from `shared/mock-data`
- `ShopService` in `core/services/shop.service.ts` has `getProducts()` (shared params) and `getProduct()`, `getBrands()`, `getTypes()`
- No dedicated method for "new arrivals" (4 newest products)

## Files to Change

### 1. `src/app/core/services/shop.service.ts`
Add `getNewArrivals(count: number = 4): Observable<Product[]>` method:
- Calls `GET products?sort=newest&pageSize={count}&pageIndex=1`
- Does NOT touch the shared `shopParams` signal (home is independent of shop filters)
- Returns just the `data[]` array from the pagination response

### 2. `src/app/features/home/home.component.ts`
- Remove `MOCK_PRODUCTS` import from `shared/mock-data`
- Inject `ShopService`
- Add `firstValueFrom` import from `rxjs`
- Add private `newArrivalsSignal = signal<Product[]>([])` and `loadingSignal = signal(true)`
- Expose `readonly newArrivals` and `readonly loading`
- Implement `async ngOnInit()` to call `shopService.getNewArrivals(4)` and update signals
- Add `MatProgressSpinnerModule` import for loading state
- Update template:
  - `featuredProducts` → `newArrivals()`
  - Product grid wrapped in `@if (!loading()) { ... } @else { skeleton grid }`
  - "NEW" badge shown for all items (all are new arrivals from the API)

## No other files change
Routes, models, and other services are untouched.

## Key Design Decisions
- **No shared params mutation**: `getNewArrivals()` uses its own local `HttpParams`, not `shopParams` signal
- **OnPush compatible**: signals are used; async ngOnInit signal updates trigger re-render correctly
- **Minimal scope**: only what is needed for Home 4.1; no refactoring of existing ShopService logic
- **Best practices compliance**: `inject()`, signals, `OnPush`, no `standalone: true` decorator flag (v20+ default)
