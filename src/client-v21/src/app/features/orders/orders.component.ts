import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MOCK_ORDERS } from '../../shared/mock-data';
import { Order } from '../../shared/models';

@Component({
  selector: 'app-orders',
  imports: [RouterLink, CurrencyPipe, DatePipe, MatCardModule, MatButtonModule, MatIconModule, MatChipsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-5xl mx-auto px-4 py-8">
      <h1 class="text-3xl font-bold text-gray-900 mb-8">My Orders</h1>

      @if (orders.length > 0) {
        <div class="space-y-4">
          @for (order of orders; track order.id) {
            <mat-card class="hover:shadow-lg transition-shadow">
              <mat-card-content class="p-6">
                <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
                  <!-- Order Info -->
                  <div class="flex-1">
                    <div class="flex items-center gap-3 mb-2">
                      <h2 class="text-lg font-semibold text-gray-900">Order #{{ order.id }}</h2>
                      <mat-chip-set>
                        <mat-chip [class]="getStatusClass(order.status)">
                          {{ order.status }}
                        </mat-chip>
                      </mat-chip-set>
                    </div>
                    <p class="text-gray-500 text-sm">
                      Placed on {{ order.orderDate | date:'mediumDate' }}
                    </p>
                  </div>

                  <!-- Items Preview -->
                  <div class="flex gap-2">
                    @for (item of order.orderItems.slice(0, 3); track item.productId) {
                      <img [src]="item.pictureUrl"
                           [alt]="item.productName"
                           class="w-16 h-16 rounded-lg border border-gray-200 object-cover">
                    }
                    @if (order.orderItems.length > 3) {
                      <div class="w-16 h-16 rounded-lg bg-gray-100 border border-gray-200 flex items-center justify-center text-sm font-medium text-gray-600">
                        +{{ order.orderItems.length - 3 }}
                      </div>
                    }
                  </div>

                  <!-- Price & Action -->
                  <div class="text-right">
                    <p class="text-lg font-bold" style="color: var(--color-primary-600)">
                      {{ order.total | currency }}
                    </p>
                    <a [routerLink]="['/orders', order.id]" mat-stroked-button color="primary" class="mt-2">
                      View Details
                      <mat-icon>arrow_forward</mat-icon>
                    </a>
                  </div>
                </div>
              </mat-card-content>
            </mat-card>
          }
        </div>
      } @else {
        <!-- No Orders -->
        <div class="text-center py-16">
          <mat-icon class="text-8xl text-gray-300">receipt_long</mat-icon>
          <h2 class="text-2xl font-semibold text-gray-600 mt-4">No orders yet</h2>
          <p class="text-gray-500 mt-2">When you place orders, they will appear here.</p>
          <a routerLink="/shop" mat-raised-button color="primary" class="mt-6">
            <mat-icon>storefront</mat-icon>
            Start Shopping
          </a>
        </div>
      }
    </div>
  `
})
export class OrdersComponent {
  orders: Order[] = MOCK_ORDERS;

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'payment received':
        return 'bg-blue-100 text-blue-800';
      case 'shipped':
        return 'bg-purple-100 text-purple-800';
      case 'delivered':
        return 'bg-green-100 text-green-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }
}
