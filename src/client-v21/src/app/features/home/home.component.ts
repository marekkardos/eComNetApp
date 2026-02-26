import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';
import { ShopService } from '../../core/services/shop.service';
import { Product } from '../../shared/models';

@Component({
  selector: 'app-home',
  imports: [RouterLink, CurrencyPipe, MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Hero Section -->
    <div class="relative bg-gradient-to-br from-teal-600 to-cyan-700 overflow-hidden h-[500px]">
      <!-- Hero Content -->
      <div class="relative max-w-7xl mx-auto px-4 h-full flex items-center">
        <div class="text-white max-w-2xl z-10">
          <h1 class="text-6xl font-bold mb-6 drop-shadow-lg">Welcome to Skinet</h1>
          <p class="text-2xl text-teal-50 mb-8 leading-relaxed">
            Discover our curated collection of premium products. Quality meets style in every item we offer.
          </p>
          <a routerLink="/shop" mat-raised-button class="!text-lg !bg-white !text-teal-700 !font-semibold !shadow-xl !px-10 !py-4">
            Shop Now
            <mat-icon class="ml-2">arrow_forward</mat-icon>
          </a>
        </div>
      </div>

      <!-- Decorative Circles -->
      <div class="absolute overflow-hidden right-0 top-0 w-2/3 h-full opacity-10 pointer-events-none">
        <svg viewBox="0 0 200 200" class="w-full h-full text-white">
          <circle cx="150" cy="100" r="90" fill="none" stroke="currentColor" stroke-width="0.3"></circle>
          <circle cx="150" cy="100" r="70" fill="none" stroke="currentColor" stroke-width="0.3"></circle>
          <circle cx="150" cy="100" r="50" fill="none" stroke="currentColor" stroke-width="0.3"></circle>
          <circle cx="150" cy="100" r="30" fill="none" stroke="currentColor" stroke-width="0.3"></circle>
        </svg>
      </div>
    </div>

    <!-- New Arrivals Section -->
    <section class="py-16 bg-gray-50">
      <div class="max-w-7xl mx-auto px-4">
        <div class="text-center mb-8">
          <h2 class="text-3xl font-bold text-gray-900 mb-4">New Arrivals</h2>
          <p class="text-gray-600">Check out our latest products</p>
        </div>

        @if (loading()) {
          <!-- Loading Skeleton -->
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-8">
            @for (item of [1,2,3,4]; track item) {
              <div class="rounded-lg overflow-hidden bg-white shadow-md animate-pulse">
                <div class="w-full h-64 bg-gray-200"></div>
                <div class="p-5 space-y-3">
                  <div class="h-3 bg-gray-200 rounded w-1/2"></div>
                  <div class="h-5 bg-gray-200 rounded w-3/4"></div>
                  <div class="h-6 bg-gray-200 rounded w-1/3"></div>
                </div>
              </div>
            }
          </div>
        } @else {
          <!-- Product Grid -->
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-8">
            @for (product of newArrivals(); track product.id) {
              <mat-card class="cursor-pointer overflow-hidden transition-shadow hover:shadow-xl border border-gray-100">
                <a [routerLink]="['/shop', product.id]" class="block no-underline text-inherit">
                  <div class="relative overflow-hidden bg-gray-50">
                    <img [src]="product.pictureUrl"
                         [alt]="product.name"
                         class="w-full h-64 object-cover transition-transform hover:scale-110 duration-500">
                    <span class="absolute top-3 left-3 rounded-full text-white text-sm font-semibold shadow-lg bg-gradient-accent px-3 py-2">
                      NEW
                    </span>
                  </div>
                  <mat-card-content class="p-5">
                    <p class="text-sm uppercase tracking-wider text-teal-600 font-semibold mb-2">{{ product.productBrand }}</p>
                    <h3 class="font-bold text-gray-900 mb-3 text-lg min-h-[3.5rem] overflow-hidden">{{ product.name }}</h3>
                    <p class="text-2xl font-bold text-teal-700">{{ product.price | currency }}</p>
                  </mat-card-content>
                </a>
              </mat-card>
            }
          </div>
        }

        <!-- View All Button -->
        <div class="text-center mt-8">
          <a routerLink="/shop" mat-stroked-button color="primary" class="!font-semibold !px-12 !py-3 !border-2">
            View All Products
            <mat-icon class="ml-2">arrow_forward</mat-icon>
          </a>
        </div>
      </div>
    </section>

    <!-- Features Section -->
    <section class="py-16 bg-gradient-white-teal">
      <div class="max-w-7xl mx-auto px-4">
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-8">
          <!-- Feature 1 -->
          <div class="text-center p-6 bg-white rounded-2xl shadow-md transition-shadow hover:shadow-xl">
            <div class="w-20 h-20 mx-auto mb-6 rounded-full flex items-center justify-center bg-gradient-primary-light">
              <mat-icon class="text-teal-700 icon-3xl">local_shipping</mat-icon>
            </div>
            <h3 class="text-xl font-bold text-gray-900 mb-3">Free Shipping</h3>
            <p class="text-gray-600 leading-relaxed">Free shipping on orders over $100</p>
          </div>

          <!-- Feature 2 -->
          <div class="text-center p-6 bg-white rounded-2xl shadow-md transition-shadow hover:shadow-xl">
            <div class="w-20 h-20 mx-auto mb-6 rounded-full flex items-center justify-center bg-gradient-primary-light">
              <mat-icon class="text-teal-700 icon-3xl">verified_user</mat-icon>
            </div>
            <h3 class="text-xl font-bold text-gray-900 mb-3">Secure Payment</h3>
            <p class="text-gray-600 leading-relaxed">100% secure payment processing</p>
          </div>

          <!-- Feature 3 -->
          <div class="text-center p-6 bg-white rounded-2xl shadow-md transition-shadow hover:shadow-xl">
            <div class="w-20 h-20 mx-auto mb-6 rounded-full flex items-center justify-center bg-gradient-primary-light">
              <mat-icon class="text-teal-700 icon-3xl">support_agent</mat-icon>
            </div>
            <h3 class="text-xl font-bold text-gray-900 mb-3">24/7 Support</h3>
            <p class="text-gray-600 leading-relaxed">Dedicated support around the clock</p>
          </div>
        </div>
      </div>
    </section>
  `
})
export class HomeComponent implements OnInit {
  private shopService = inject(ShopService);

  private newArrivalsSignal = signal<Product[]>([]);
  private loadingSignal = signal(true);

  readonly newArrivals = this.newArrivalsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  async ngOnInit(): Promise<void> {
    try {
      const products = await firstValueFrom(this.shopService.getNewArrivals(4));
      this.newArrivalsSignal.set(products);
    } finally {
      this.loadingSignal.set(false);
    }
  }
}
