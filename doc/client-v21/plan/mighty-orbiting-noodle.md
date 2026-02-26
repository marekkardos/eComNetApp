# Plan: Phase 4.5 — Checkout Feature (Real API + Stripe)

## Context

The `CheckoutComponent` was built in Phase 1 as a static UI with mock data. Phase 4.5 wires it up to real APIs:
- Delivery methods from `CheckoutService.getDeliveryMethods()`
- User's saved address from `AccountService.getUserAddress()`
- Payment via Stripe (`@stripe/stripe-js@^8.6.4` already installed)
- Order creation via `CheckoutService.createOrder()`
- Basket cleanup + navigation to `checkout/success`

The existing monolithic stepper structure is kept (no sub-component split) — only the data layer changes.

---

## Files to Change

### 1. `src/app/features/checkout/checkout.component.ts` — Major update

**Remove:**
- `MOCK_BASKET_ITEMS`, `MOCK_DELIVERY_METHODS`, `MOCK_ADDRESS` imports from mock-data
- Local `items` static array
- Local `subtotal`, `shipping`, `total` computed signals (replace with basket service)

**Add imports:**
- `afterNextRender`, `ViewChild`, `ElementRef` (Angular core)
- `StepperSelectionEvent` from `@angular/cdk/stepper`
- `firstValueFrom` from `rxjs`
- `loadStripe`, `Stripe`, `StripeCardElement` from `@stripe/stripe-js`
- `CheckoutService`, `BasketService`, `AccountService`
- `environment` from environments
- `OrderToCreate` from shared models
- `ToastrService` from ngx-toastr

**New signals:**
```typescript
deliveryMethods = signal<DeliveryMethod[]>([]);
loading = signal(true);           // delivery methods + address loading
submitting = signal(false);       // order submission in progress
stripeReady = signal(false);      // stripe card element mounted
cardErrors = signal('');

// Stripe instances (not signals — raw refs)
private stripe: Stripe | null = null;
private cardElement: StripeCardElement | null = null;
```

**Remove local computed, delegate to BasketService:**
```typescript
readonly items = computed(() => this.basketService.basket()?.items ?? []);
readonly subtotal = computed(() => this.basketService.totals()?.subtotal ?? 0);
readonly shipping = computed(() => this.basketService.totals()?.shipping ?? 0);
readonly total = computed(() => this.basketService.totals()?.total ?? 0);
```

**Constructor — async data loading + Stripe init:**
```typescript
constructor() {
  afterNextRender(() => { void this.initStripe(); });
}

async ngOnInit(): Promise<void> {
  await Promise.all([this.loadDeliveryMethods(), this.loadUserAddress()]);
  this.loading.set(false);
}
```

**`loadDeliveryMethods()`:** `firstValueFrom(checkoutService.getDeliveryMethods())` → set `deliveryMethods` signal, pre-select first, patch `deliveryForm`.

**`loadUserAddress()`:** `firstValueFrom(accountService.getUserAddress())` → patch `addressForm`. Catch errors silently (user may not have saved address).

**`initStripe()` (called via afterNextRender):**
```typescript
private async initStripe(): Promise<void> {
  const stripe = await loadStripe(environment.stripe.publishableKey);
  if (!stripe || !this.cardElementRef?.nativeElement) return;
  this.stripe = stripe;
  const elements = stripe.elements();
  this.cardElement = elements.create('card');
  this.cardElement.mount(this.cardElementRef.nativeElement);
  this.cardElement.on('change', e => this.cardErrors.set(e.error?.message ?? ''));
  this.stripeReady.set(true);
}
```

**Template ref for Stripe:** `@ViewChild('cardElement') cardElementRef!: ElementRef;`

**`selectDeliveryMethod(method)`:** Also call `this.basketService.setShippingPrice(method)` to update basket totals + persist delivery method to API before payment intent.

**Stepper `(selectionChange)` → create payment intent when reaching payment step (index 3):**
```typescript
onStepChange(event: StepperSelectionEvent): void {
  if (event.selectedIndex === 3) {
    const basket = this.basketService.basket();
    if (basket && !basket.clientSecret) {
      this.basketService.createPaymentIntent().subscribe({
        error: () => this.toastr.error('Could not create payment intent')
      });
    }
  }
}
```

**`placeOrder()` — full Stripe + order flow:**
```typescript
async placeOrder(): Promise<void> {
  if (this.submitting()) return;
  this.submitting.set(true);
  const basket = this.basketService.basket();
  if (!this.stripe || !this.cardElement || !basket?.clientSecret) {
    this.submitting.set(false); return;
  }
  try {
    const { error, paymentIntent } = await this.stripe.confirmCardPayment(
      basket.clientSecret, { payment_method: { card: this.cardElement } }
    );
    if (error) { this.cardErrors.set(error.message ?? 'Payment failed'); return; }
    if (paymentIntent?.status === 'succeeded') {
      const addr = this.addressForm.value;
      const order = await firstValueFrom(this.checkoutService.createOrder({
        basketId: basket.id,
        deliveryMethodId: this.selectedDeliveryMethod()!.id,
        shipToAddress: {
          firstName: addr.firstName!, lastName: addr.lastName!,
          street: addr.street!, city: addr.city!, state: addr.state!,
          zipcode: addr.zipCode!   // form uses zipCode, model uses zipcode
        }
      }));
      this.basketService.deleteLocalBasket();
      this.router.navigate(['/checkout/success'], { state: { order } });
    }
  } catch {
    this.cardErrors.set('An unexpected error occurred. Please try again.');
  } finally {
    this.submitting.set(false);
  }
}
```

**Template changes:**
- Stepper: add `(selectionChange)="onStepChange($event)"`
- Delivery methods list: use `deliveryMethods()` signal instead of `deliveryMethods` array
- Review step items: use `items()` (computed from basket service)
- Order Summary sidebar: use `subtotal()`, `shipping()`, `total()` from basket service
- Payment step: replace mock form with:
  ```html
  <div #cardElement class="p-3 border border-gray-300 rounded-lg min-h-[40px]"></div>
  @if (cardErrors()) { <p class="text-red-600 text-sm mt-2">{{ cardErrors() }}</p> }
  @if (!stripeReady()) { <p class="text-gray-400 text-sm">Loading payment form...</p> }
  ```
- Place Order button: `[disabled]="submitting() || !stripeReady()"`, show spinner when `submitting()`
- Add `@if (loading())` skeleton/spinner for delivery step
- Add `MatProgressSpinnerModule` or use `mat-icon` spinner for submitting state

### 2. `src/app/features/checkout/checkout-success/checkout-success.component.ts` — Minor update

Display order details from navigation state:
```typescript
import { Router, RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Order } from '../../../shared/models/order.model';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export class CheckoutSuccessComponent {
  private router = inject(Router);
  order: Order | undefined = this.router.getCurrentNavigation()?.extras?.state?.['order'];
}
```

Template: check icon, "Order Confirmed!", order ID + date + total if available, links to `/orders` and `/shop`.

---

## Key Implementation Notes

- **`zipCode` vs `zipcode`**: Address form control is `zipCode`, model field is `zipcode`. Map explicitly in `placeOrder()`.
- **Stripe mount timing**: `afterNextRender` ensures DOM is ready for Material Stepper (which renders all steps in DOM upfront).
- **Payment intent lifecycle**: Created lazily when user reaches payment step (step index 3). If basket already has `clientSecret`, skip.
- **Delivery method selection → basket update**: Calling `basketService.setShippingPrice(method)` persists the delivery method ID + price to the basket API before the payment intent is created, ensuring the PI amount is correct.
- **No `standalone: true`** in decorator (Angular 20+ default, per best practices).
- **`ChangeDetectionStrategy.OnPush`** kept.
- **Error handling**: Delivery method load errors → toastr. Address load errors → silent (optional). Payment errors → `cardErrors` signal displayed inline.

---

## Verification

1. `npm start` — app compiles with no errors
2. Navigate to `/shop`, add item to basket
3. Log in → go to `/basket` → "Proceed to Checkout"
4. **Address step**: should auto-populate with saved address; fill in if blank; click Continue
5. **Delivery step**: real delivery methods load from API; select one; click Continue
6. **Review step**: items from basket appear; address + delivery summary visible
7. **Payment step**: Stripe card element appears (test key: 4242 4242 4242 4242, any expiry/CVC)
8. Click "Place Order" → Stripe confirms → order created → basket cleared → redirected to `/checkout/success` with order ID visible
9. **Error case**: use card 4000000000000002 → payment declined → error shown inline
