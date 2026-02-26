import { Component, ChangeDetectionStrategy, inject, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { ToastrService } from 'ngx-toastr';
import { BasketService } from '../../core/services/basket.service';
import { BasketItem } from '../../shared/models';

@Component({
  selector: 'app-basket',
  imports: [RouterLink, CurrencyPipe, MatButtonModule, MatIconModule, MatCardModule, MatDividerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-7xl mx-auto px-4 py-8">
      <h1 class="text-3xl font-bold text-gray-900 mb-8">Shopping Basket</h1>

      @if (items().length > 0) {
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <!-- Cart Items -->
          <div class="lg:col-span-2">
            <mat-card>
              <mat-card-content class="p-0">
                @for (item of items(); track item.id; let last = $last) {
                  <div class="flex items-center gap-4 p-4">
                    <!-- Product Image -->
                    <a [routerLink]="['/shop', item.id]" class="shrink-0">
                      <img [src]="item.pictureUrl" [alt]="item.productName" class="w-32 h-32 object-cover rounded-lg">
                    </a>

                    <!-- Product Info -->
                    <div class="flex-1 min-w-0">
                      <a [routerLink]="['/shop', item.id]" class="block">
                        <p class="text-sm text-gray-500">{{ item.brand }}</p>
                        <h3 class="font-semibold text-gray-900 truncate">{{ item.productName }}</h3>
                        <p class="text-sm text-gray-500">{{ item.type }}</p>
                      </a>
                    </div>

                    <!-- Quantity Controls -->
                    <div class="flex items-center border rounded-lg">
                      <button mat-icon-button
                              (click)="decrementQuantity(item)"
                              [disabled]="item.quantity <= 1"
                              [attr.aria-label]="'Decrease quantity of ' + item.productName">
                        <mat-icon>remove</mat-icon>
                      </button>
                      <span class="px-3 text-lg font-medium min-w-[2.5rem] text-center">{{ item.quantity }}</span>
                      <button mat-icon-button
                              (click)="incrementQuantity(item)"
                              [disabled]="item.quantity >= 10"
                              [attr.aria-label]="'Increase quantity of ' + item.productName">
                        <mat-icon>add</mat-icon>
                      </button>
                    </div>

                    <!-- Price -->
                    <div class="text-right shrink-0 w-24">
                      <p class="font-bold text-gray-900">{{ item.price * item.quantity | currency }}</p>
                      <p class="text-sm text-gray-500">{{ item.price | currency }} each</p>
                    </div>

                    <!-- Remove Button -->
                    <button mat-icon-button
                            color="warn"
                            (click)="removeItem(item)"
                            [attr.aria-label]="'Remove ' + item.productName + ' from cart'">
                      <mat-icon>delete</mat-icon>
                    </button>
                  </div>
                  @if (!last) {
                    <mat-divider></mat-divider>
                  }
                }
              </mat-card-content>
            </mat-card>

            <!-- Continue Shopping -->
            <div class="mt-4">
              <a routerLink="/shop" mat-stroked-button>
                <mat-icon>arrow_back</mat-icon>
                Continue Shopping
              </a>
            </div>
          </div>

          <!-- Order Summary -->
          <div class="lg:col-span-1">
            <mat-card class="sticky top-24">
              <mat-card-header>
                <mat-card-title>Order Summary</mat-card-title>
              </mat-card-header>
              <mat-card-content class="pt-4">
                <div class="space-y-3">
                  <div class="flex justify-between">
                    <span class="text-gray-600">Subtotal ({{ totalItems() }} items)</span>
                    <span class="font-medium">{{ subtotal() | currency }}</span>
                  </div>

                  <div class="flex justify-between">
                    <span class="text-gray-600">Shipping</span>
                    <span class="font-medium">{{ shipping() | currency }}</span>
                  </div>

                  <mat-divider></mat-divider>

                  <div class="flex justify-between text-lg">
                    <span class="font-semibold text-gray-900">Total</span>
                    <span class="font-bold" style="color: var(--color-primary-600)">{{ total() | currency }}</span>
                  </div>
                </div>

                <!-- Shipping Note -->
                <div class="mt-4 p-3 bg-blue-50 rounded-lg">
                  <div class="flex items-start gap-2">
                    <mat-icon class="text-blue-500 text-lg">info</mat-icon>
                    <p class="text-sm text-blue-700">
                      @if (subtotal() >= 100) {
                        <span class="font-medium">You qualify for free shipping!</span>
                      } @else {
                        Add {{ 100 - subtotal() | currency }} more for free shipping
                      }
                    </p>
                  </div>
                </div>
              </mat-card-content>
              <mat-card-actions class="px-4 pb-4">
                <a routerLink="/checkout" mat-raised-button color="primary" class="w-full py-3">
                  Proceed to Checkout
                  <mat-icon>arrow_forward</mat-icon>
                </a>
              </mat-card-actions>
            </mat-card>

            <!-- Accepted Payment Methods -->
            <div class="mt-4 text-center">
              <p class="text-sm text-gray-500 mb-2">We accept</p>
              <div class="flex justify-center gap-2">
                <mat-icon class="text-gray-400">credit_card</mat-icon>
                <span class="text-gray-400 text-sm">Visa, Mastercard, American Express</span>
              </div>
            </div>
          </div>
        </div>
      } @else {
        <!-- Empty Cart -->
        <div class="text-center py-16">
          <mat-icon class="text-8xl text-gray-300">shopping_cart</mat-icon>
          <h2 class="text-2xl font-semibold text-gray-600 mt-4">Your basket is empty</h2>
          <p class="text-gray-500 mt-2">Looks like you haven't added any items yet.</p>
          <a routerLink="/shop" mat-raised-button color="primary" class="mt-6">
            <mat-icon>storefront</mat-icon>
            Start Shopping
          </a>
        </div>
      }
    </div>
  `
})
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
