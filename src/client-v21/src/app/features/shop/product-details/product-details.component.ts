import { Component, ChangeDetectionStrategy, input, signal, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { firstValueFrom } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { ShopService } from '../../../core/services/shop.service';
import { BasketService } from '../../../core/services/basket.service';
import { Product } from '../../../shared/models';

@Component({
  selector: 'app-product-details',
  imports: [RouterLink, CurrencyPipe, MatButtonModule, MatIconModule, MatTabsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <!-- Loading Skeleton -->
      <div class="max-w-7xl mx-auto px-4 py-8">
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-12 animate-pulse" aria-label="Loading product details">
          <div class="rounded-lg bg-gray-200 h-[500px]"></div>
          <div class="space-y-4">
            <div class="h-4 bg-gray-200 rounded w-1/4"></div>
            <div class="h-8 bg-gray-200 rounded w-3/4"></div>
            <div class="h-8 bg-gray-200 rounded w-1/4"></div>
            <div class="h-20 bg-gray-200 rounded"></div>
            <div class="h-12 bg-gray-200 rounded w-1/2"></div>
            <div class="h-14 bg-gray-200 rounded w-full"></div>
          </div>
        </div>
      </div>
    } @else {
      <div class="max-w-7xl mx-auto px-4 py-8">
        <!-- Breadcrumb -->
        <nav class="flex mb-8" aria-label="Breadcrumb">
          <ol class="flex items-center space-x-2">
            <li>
              <a routerLink="/" class="text-gray-500 hover:text-gray-700">Home</a>
            </li>
            <li class="flex items-center">
              <mat-icon class="text-gray-400 text-sm" aria-hidden="true">chevron_right</mat-icon>
              <a routerLink="/shop" class="text-gray-500 hover:text-gray-700 ml-2">Shop</a>
            </li>
            @if (product()) {
              <li class="flex items-center">
                <mat-icon class="text-gray-400 text-sm" aria-hidden="true">chevron_right</mat-icon>
                <span class="ml-2 text-gray-900">{{ product()!.name }}</span>
              </li>
            }
          </ol>
        </nav>

        @if (product(); as prod) {
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-12">
            <!-- Product Image -->
            <div class="bg-white rounded-lg shadow-lg overflow-hidden max-h-[600px]">
              <img [src]="prod.pictureUrl"
                   [alt]="prod.name"
                   class="w-full h-full object-cover">
            </div>

            <!-- Product Info -->
            <div>
              <p class="text-sm text-gray-500 mb-2">{{ prod.productBrand }}</p>
              <h1 class="text-3xl font-bold text-gray-900 mb-4">{{ prod.name }}</h1>
              <p class="text-3xl font-bold mb-6" style="color: var(--color-primary-600)">
                {{ prod.price | currency }}
              </p>

              <p class="text-gray-600 mb-8">{{ prod.description }}</p>

              <!-- Quantity Selector -->
              <div class="flex items-center gap-4 mb-6">
                <span class="text-gray-700 font-medium" id="quantity-label">Quantity:</span>
                <div class="flex items-center border rounded-lg" role="group" aria-labelledby="quantity-label">
                  <button mat-icon-button
                          (click)="decrementQuantity()"
                          [disabled]="quantity() <= 1"
                          aria-label="Decrease quantity">
                    <mat-icon>remove</mat-icon>
                  </button>
                  <span class="px-4 py-2 text-lg font-medium min-w-[3rem] text-center"
                        aria-live="polite"
                        aria-label="Current quantity">{{ quantity() }}</span>
                  <button mat-icon-button
                          (click)="incrementQuantity()"
                          [disabled]="quantity() >= 10"
                          aria-label="Increase quantity">
                    <mat-icon>add</mat-icon>
                  </button>
                </div>
              </div>

              <!-- Add to Cart Button -->
              <button mat-raised-button
                      color="primary"
                      class="w-full sm:w-auto px-12 py-3 text-lg"
                      (click)="addToCart()">
                <mat-icon class="mr-2">add_shopping_cart</mat-icon>
                Add to Cart
              </button>

              <!-- Product Details Tabs -->
              <mat-tab-group class="mt-12">
                <mat-tab label="Details">
                  <div class="py-6">
                    <dl class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                      <div>
                        <dt class="text-sm font-medium text-gray-500">Brand</dt>
                        <dd class="text-gray-900">{{ prod.productBrand }}</dd>
                      </div>
                      <div>
                        <dt class="text-sm font-medium text-gray-500">Type</dt>
                        <dd class="text-gray-900">{{ prod.productType }}</dd>
                      </div>
                      <div>
                        <dt class="text-sm font-medium text-gray-500">SKU</dt>
                        <dd class="text-gray-900">SKU-{{ prod.id.toString().padStart(6, '0') }}</dd>
                      </div>
                      <div>
                        <dt class="text-sm font-medium text-gray-500">Availability</dt>
                        <dd class="text-green-600 font-medium">In Stock</dd>
                      </div>
                    </dl>
                  </div>
                </mat-tab>
                <mat-tab label="Shipping">
                  <div class="py-6">
                    <ul class="space-y-3 text-gray-600">
                      <li class="flex items-start gap-2">
                        <mat-icon class="text-green-500 text-lg" aria-hidden="true">check_circle</mat-icon>
                        <span>Free shipping on orders over $100</span>
                      </li>
                      <li class="flex items-start gap-2">
                        <mat-icon class="text-green-500 text-lg" aria-hidden="true">check_circle</mat-icon>
                        <span>Standard delivery: 3-5 business days</span>
                      </li>
                      <li class="flex items-start gap-2">
                        <mat-icon class="text-green-500 text-lg" aria-hidden="true">check_circle</mat-icon>
                        <span>Express delivery available at checkout</span>
                      </li>
                    </ul>
                  </div>
                </mat-tab>
                <mat-tab label="Returns">
                  <div class="py-6">
                    <p class="text-gray-600 mb-4">
                      We offer a 30-day return policy for all unworn items in original condition with tags attached.
                    </p>
                    <ul class="space-y-2 text-gray-600">
                      <li class="flex items-start gap-2">
                        <mat-icon class="text-blue-500 text-lg" aria-hidden="true">info</mat-icon>
                        <span>Free returns within 30 days</span>
                      </li>
                      <li class="flex items-start gap-2">
                        <mat-icon class="text-blue-500 text-lg" aria-hidden="true">info</mat-icon>
                        <span>Full refund to original payment method</span>
                      </li>
                    </ul>
                  </div>
                </mat-tab>
              </mat-tab-group>
            </div>
          </div>
        } @else {
          <!-- Product Not Found -->
          <div class="text-center py-16">
            <mat-icon style="font-size: 6rem; width: 6rem; height: 6rem; color: #d1d5db;"
                      aria-hidden="true">error_outline</mat-icon>
            <h2 class="text-2xl font-semibold text-gray-600 mt-4">Product Not Found</h2>
            <p class="text-gray-500 mt-2">The product you're looking for doesn't exist or has been removed.</p>
            <a routerLink="/shop" mat-raised-button color="primary" class="mt-6">
              <mat-icon>arrow_back</mat-icon>
              Back to Shop
            </a>
          </div>
        }
      </div>
    }
  `
})
export class ProductDetailsComponent implements OnInit {
  id = input<string>();

  private shopService = inject(ShopService);
  private basketService = inject(BasketService);
  private toastr = inject(ToastrService);

  private productSignal = signal<Product | null>(null);
  private loadingSignal = signal(true);

  readonly product = this.productSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  quantity = signal(1);

  async ngOnInit(): Promise<void> {
    const productId = parseInt(this.id() ?? '0', 10);
    if (productId > 0) {
      try {
        const product = await firstValueFrom(this.shopService.getProduct(productId));
        this.productSignal.set(product);
      } catch {
        this.productSignal.set(null);
      } finally {
        this.loadingSignal.set(false);
      }
    } else {
      this.loadingSignal.set(false);
    }
  }

  incrementQuantity(): void {
    if (this.quantity() < 10) {
      this.quantity.update(q => q + 1);
    }
  }

  decrementQuantity(): void {
    if (this.quantity() > 1) {
      this.quantity.update(q => q - 1);
    }
  }

  addToCart(): void {
    const prod = this.product();
    if (prod) {
      this.basketService.addItemToBasket(prod, this.quantity());
      this.toastr.success(`${prod.name} added to basket`);
    }
  }
}
