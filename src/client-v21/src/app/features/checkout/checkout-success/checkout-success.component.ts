import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-checkout-success',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container py-8 text-center">
      <h1 class="text-2xl font-bold text-green-600 mb-4">Order Successful!</h1>
      <p class="text-gray-600">Thank you for your purchase.</p>
    </div>
  `
})
export class CheckoutSuccessComponent {}
