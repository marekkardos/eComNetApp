import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MOCK_ORDERS } from '../../../shared/mock-data';

@Component({
  selector: 'app-order-details',
  imports: [RouterLink, CurrencyPipe, DatePipe, MatCardModule, MatButtonModule, MatIconModule, MatChipsModule, MatDividerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-5xl mx-auto px-4 py-8">
      <!-- Back Button -->
      <a routerLink="/orders" class="inline-flex items-center text-gray-600 hover:text-gray-900 mb-6">
        <mat-icon>arrow_back</mat-icon>
        <span class="ml-1">Back to Orders</span>
      </a>

      @if (order(); as ord) {
        <!-- Order Header -->
        <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
          <div>
            <h1 class="text-3xl font-bold text-gray-900">Order #{{ ord.id }}</h1>
            <p class="text-gray-500 mt-1">Placed on {{ ord.orderDate | date:'fullDate' }}</p>
          </div>
          <mat-chip-set>
            <mat-chip [class]="getStatusClass(ord.status)" class="text-lg px-4 py-2">
              {{ ord.status }}
            </mat-chip>
          </mat-chip-set>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <!-- Order Items -->
          <div class="lg:col-span-2">
            <mat-card>
              <mat-card-header>
                <mat-card-title class="mb-4">Order Items</mat-card-title>
              </mat-card-header>
              <mat-card-content class="p-0">
                @for (item of ord.orderItems; track item.productId) {
                  <a [routerLink]="['/shop', item.productId]" class="flex items-center gap-4 p-4 hover:bg-gray-50 transition-colors border-b border-gray-100 last:border-b-0">
                    <img [src]="item.pictureUrl"
                         [alt]="item.productName"
                         class="w-20 h-20 object-cover rounded-lg shrink-0">
                    <div class="flex-1 min-w-0">
                      <h3 class="text-base font-semibold text-gray-900">{{ item.productName }}</h3>
                      <p class="text-base text-gray-500 mt-1">Quantity: {{ item.quantity }}</p>
                      <p class="text-base text-gray-500">{{ item.price | currency }} each</p>
                    </div>
                    <span class="text-lg font-bold text-gray-900 shrink-0">{{ item.price * item.quantity | currency }}</span>
                  </a>
                }
              </mat-card-content>
            </mat-card>
          </div>

          <!-- Order Summary -->
          <div class="lg:col-span-1 space-y-6">
            <!-- Shipping Address -->
            <mat-card>
              <mat-card-header>
                <mat-card-title class="text-lg">Shipping Address</mat-card-title>
              </mat-card-header>
              <mat-card-content class="pt-2">
                <p class="text-gray-700">{{ ord.shipToAddress.firstName }} {{ ord.shipToAddress.lastName }}</p>
                <p class="text-gray-600">{{ ord.shipToAddress.street }}</p>
                <p class="text-gray-600">{{ ord.shipToAddress.city }}, {{ ord.shipToAddress.state }} {{ ord.shipToAddress.zipcode }}</p>
              </mat-card-content>
            </mat-card>

            <!-- Delivery Method -->
            <mat-card>
              <mat-card-header>
                <mat-card-title class="text-lg">Delivery Method</mat-card-title>
              </mat-card-header>
              <mat-card-content class="pt-2">
                <p class="text-gray-700 font-medium">{{ ord.deliveryMethod }}</p>
                <p class="text-gray-600">{{ ord.shippingPrice | currency }}</p>
              </mat-card-content>
            </mat-card>

            <!-- Order Summary -->
            <mat-card>
              <mat-card-header>
                <mat-card-title class="text-lg">Order Summary</mat-card-title>
              </mat-card-header>
              <mat-card-content class="pt-2">
                <div class="space-y-2">
                  <div class="flex justify-between">
                    <span class="text-gray-600">Subtotal</span>
                    <span>{{ ord.subtotal | currency }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span class="text-gray-600">Shipping</span>
                    <span>{{ ord.shippingPrice | currency }}</span>
                  </div>
                  <mat-divider></mat-divider>
                  <div class="flex justify-between text-lg font-bold pt-2">
                    <span>Total</span>
                    <span style="color: var(--color-primary-600)">{{ ord.total | currency }}</span>
                  </div>
                </div>
              </mat-card-content>
            </mat-card>

            <!-- Actions -->
            <div class="space-y-2">
              <button mat-stroked-button class="w-full">
                <mat-icon>print</mat-icon>
                Print Receipt
              </button>
              <button mat-stroked-button class="w-full">
                <mat-icon>help_outline</mat-icon>
                Need Help?
              </button>
            </div>
          </div>
        </div>
      } @else {
        <!-- Order Not Found -->
        <div class="text-center py-16">
          <mat-icon class="text-6xl text-gray-300">error_outline</mat-icon>
          <h2 class="text-2xl font-semibold text-gray-600 mt-4">Order Not Found</h2>
          <p class="text-gray-500 mt-2">The order you're looking for doesn't exist.</p>
          <a routerLink="/orders" mat-raised-button color="primary" class="mt-6">
            <mat-icon>arrow_back</mat-icon>
            Back to Orders
          </a>
        </div>
      }
    </div>
  `
})
export class OrderDetailsComponent {
  id = input<string>();

  order = computed(() => {
    const orderId = parseInt(this.id() || '0', 10);
    return MOCK_ORDERS.find(o => o.id === orderId) || null;
  });

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
