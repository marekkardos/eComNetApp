# Plan: Add "Save as Default Address" to Checkout Shipping Step

## Context

The old Angular 9 app has a "Save as default address" button in the checkout shipping step that lets users persist their address via the API. The new Angular 21 app already loads the saved address on init (`loadUserAddress()`) and has `AccountService.getUserAddress()` / `updateUserAddress()` methods, but is **missing the save button** in the UI.

## Changes

### File: `src/app/features/checkout/checkout.component.ts`

**1. Add a `savingAddress` signal** (alongside existing signals ~line 332):
```typescript
savingAddress = signal(false);
```

**2. Add `saveUserAddress()` method** (after `loadUserAddress()`):
- Calls `accountService.updateUserAddress()` with the current address form values (mapping `zipCode` → `zipcode` to match the API model)
- On success: show toast "Address saved", mark form as pristine + untouched (disables button)
- On error: show error toast
- Uses the `savingAddress` signal to disable the button during the request

**3. Update the Address Step template** (~lines 59-120):
- Add a header row above the form fields with "Shipping Address" title and a "Save as default address" button
- Button is disabled when: form is invalid OR form is not dirty OR `savingAddress()` is true
- Uses `mat-stroked-button` with `save` icon for consistent Material styling

### Template change (conceptual):
```
Before the form grid, add:
<div class="flex justify-between items-center mb-4">
  <h2 class="text-lg font-semibold text-gray-900">Shipping Address</h2>
  <button mat-stroked-button
          (click)="saveUserAddress()"
          [disabled]="addressForm.invalid || !addressForm.dirty || savingAddress()">
    <mat-icon>save</mat-icon>
    Save as default address
  </button>
</div>
```

## Verification

1. `npm start` — navigate to `/checkout`
2. With no saved address: form is empty, save button is disabled (invalid form)
3. Fill in all fields: button becomes enabled (form is valid + dirty)
4. Click save: address saved via PUT `/api/account/address`, toast shown, button disabled again (form pristine)
5. Reload page: address auto-populates from API, save button disabled (form not dirty)
6. Edit a field: button re-enables (form dirty again)
