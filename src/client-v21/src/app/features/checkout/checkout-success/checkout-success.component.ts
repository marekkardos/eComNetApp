import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Order } from '../../../shared/models/order.model';

@Component({
  selector: 'app-checkout-success',
  imports: [RouterLink, CurrencyPipe, DatePipe, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-2xl mx-auto px-4 py-16 text-center">
      <div class="mb-6">
        <mat-icon style="font-size: 72px; width: 72px; height: 72px; color: var(--mdc-filled-button-container-color, #16a34a)">check_circle</mat-icon>
      </div>
      <h1 class="text-3xl font-bold text-gray-900 mb-4">Order Confirmed!</h1>
      <p class="text-gray-600 mb-8">Thank you for your purchase. We'll send you a confirmation email shortly.</p>

      @if (order) {
        <div class="bg-gray-50 rounded-lg p-6 mb-8 text-left">
          <div class="grid grid-cols-2 gap-4">
            <div>
              <p class="text-sm text-gray-500">Order Number</p>
              <p class="font-semibold text-gray-900">#{{ order.id }}</p>
            </div>
            <div>
              <p class="text-sm text-gray-500">Order Date</p>
              <p class="font-semibold text-gray-900">{{ order.orderDate | date:'mediumDate' }}</p>
            </div>
            <div>
              <p class="text-sm text-gray-500">Total</p>
              <p class="font-semibold text-gray-900">{{ order.total | currency }}</p>
            </div>
            <div>
              <p class="text-sm text-gray-500">Status</p>
              <p class="font-semibold text-gray-900">{{ order.status }}</p>
            </div>
          </div>
        </div>
      }

      <div class="flex justify-center gap-4">
        <a mat-stroked-button routerLink="/orders">
          <mat-icon>list</mat-icon>
          View Orders
        </a>
        <a mat-raised-button color="primary" routerLink="/shop">
          <mat-icon>shopping_bag</mat-icon>
          Continue Shopping
        </a>
      </div>
    </div>
  `
})
export class CheckoutSuccessComponent {
  private router = inject(Router);
  order: Order | undefined = this.router.getCurrentNavigation()?.extras?.state?.['order'];
}
