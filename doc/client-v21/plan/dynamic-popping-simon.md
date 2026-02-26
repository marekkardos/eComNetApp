# Fix: Payment Intent Race Condition in Checkout

## Context

The `/api/payments/{basketId}` endpoint is called **inconsistently** when transitioning from the Review step to the Payment step. The old Angular 9 client always calls it reliably. With a 1-minute JWT token lifetime in dev, this causes intermittent payment failures.

**Root cause**: The old app's Review step button explicitly calls `createPaymentIntent()` and only advances the stepper on success. The new app uses `matStepperNext` (instant advance) and calls the API as a fire-and-forget side effect in `onStepChange()`, with a `!basket.clientSecret` guard that skips the call if a stale secret already exists.

## File to Modify

`src/app/features/checkout/checkout.component.ts` — the only file that needs changes.

## Changes

### 1. Add `MatStepper` import and `ViewChild` reference

- Line 15: Add `MatStepper` to the import from `@angular/material/stepper`
- After line 309: Add `@ViewChild('stepper') stepper!: MatStepper;`

The template already has `#stepper` on the `<mat-stepper>` element (line 57).

### 2. Add `preparingPayment` signal

After line 321 (state signals block):
```typescript
preparingPayment = signal(false);
```

### 3. Replace Review step button (lines 222-225)

Remove `matStepperNext`, add explicit click handler with loading state:

```html
<button mat-raised-button color="primary"
        (click)="continueToPayment()"
        [disabled]="preparingPayment()">
    @if (preparingPayment()) {
        <mat-spinner diameter="20"></mat-spinner>
        Preparing Payment...
    } @else {
        Continue to Payment
        <mat-icon>arrow_forward</mat-icon>
    }
</button>
```

### 4. Add `continueToPayment()` method (replaces `onStepChange` logic)

```typescript
async continueToPayment(): Promise<void> {
    this.preparingPayment.set(true);
    try {
        await firstValueFrom(this.basketService.createPaymentIntent());
        this.stepper.next();
    } catch {
        this.toastr.error('Could not create payment intent. Please try again.');
    } finally {
        this.preparingPayment.set(false);
    }
}
```

Key: **No `!basket.clientSecret` check** — always call the API (matches old app behavior; endpoint is idempotent).

### 5. Remove `onStepChange()` and its wiring

- Delete `onStepChange()` method (lines 398-407)
- Remove `(selectionChange)="onStepChange($event)"` from `<mat-stepper>` on line 57
- Remove `StepperSelectionEvent` import (line 24)

## Why This Works

| Aspect | Old App (working) | New App (broken) | After Fix |
|--------|-------------------|------------------|-----------|
| API call timing | Before step advance | After step advance (side effect) | Before step advance |
| Step advance gate | On API success only | Immediate (`matStepperNext`) | On API success only |
| Stale clientSecret | Always refreshed | Skipped if exists | Always refreshed |
| Loading feedback | N/A | None | Spinner + disabled button |

## Verification

1. `npm start` — run the dev server
2. Log in, add items to basket, go to checkout
3. Fill address, select delivery, reach Review step
4. Click "Continue to Payment" — should see spinner, then advance to Payment step
5. Verify in browser DevTools Network tab that `POST /api/payments/{basketId}` fires every time
6. Test with expired token (wait >1 min on Review step) — JWT interceptor should refresh, then payment intent should succeed
7. Test error case: stop the API server, click button — should show toast error and stay on Review step
8. Navigate back from Payment to Review, then forward again — payment intent should be called again (no stale secret skip)
