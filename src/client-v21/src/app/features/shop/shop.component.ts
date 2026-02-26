import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { firstValueFrom } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { ShopService } from '../../core/services/shop.service';
import { BasketService } from '../../core/services/basket.service';
import { Product, Brand, ProductType } from '../../shared/models';

@Component({
  selector: 'app-shop',
  imports: [
    RouterLink,
    FormsModule,
    CurrencyPipe,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatPaginatorModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-7xl mx-auto px-4 py-8">
      <!-- Header -->
      <div class="flex flex-col justify-between items-start mb-8" style="gap: 1rem;">
        <div>
          <h1 class="text-3xl font-bold text-gray-900">Shop</h1>
          <p class="text-teal-600 mt-1 font-semibold">{{ totalCount() }} products found</p>
        </div>

        <!-- Search and Sort -->
        <div class="flex flex-col sm:flex-row gap-4 w-full md:w-auto">
          <mat-form-field appearance="outline" class="w-full sm:w-64">
            <mat-label>Search</mat-label>
            <input matInput
                   [ngModel]="searchTerm()"
                   (ngModelChange)="searchTerm.set($event)"
                   (keyup.enter)="onSearch()"
                   placeholder="Search products...">
            <button matSuffix mat-icon-button (click)="onSearch()" aria-label="Search products">
              <mat-icon>search</mat-icon>
            </button>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full sm:w-48">
            <mat-label>Sort By</mat-label>
            <mat-select [ngModel]="shopParams().sort" (ngModelChange)="onSortSelected($event)">
              <mat-option value="name">Name (A-Z)</mat-option>
              <mat-option value="priceAsc">Price: Low to High</mat-option>
              <mat-option value="priceDesc">Price: High to Low</mat-option>
            </mat-select>
          </mat-form-field>
        </div>
      </div>

      <div class="flex flex-col gap-8">
        <!-- Sidebar Filters -->
        <aside class="w-full" aria-label="Product filters">
          <div class="bg-white rounded-lg shadow-md p-6" style="border-left: 4px solid var(--color-primary-600);">
            <h2 class="font-semibold text-gray-900 mb-6 text-lg">Filters</h2>

            <!-- Brand Filter -->
            <div class="mb-6">
              <h3 class="text-sm font-semibold text-teal-700 mb-2" id="brand-filter-label">Brand</h3>
              <mat-form-field appearance="outline" class="w-full">
                <mat-select [ngModel]="shopParams().brandId"
                            (ngModelChange)="onBrandSelected($event)"
                            aria-labelledby="brand-filter-label">
                  <mat-option [value]="0">All Brands</mat-option>
                  @for (brand of brands(); track brand.id) {
                    <mat-option [value]="brand.id">{{ brand.name }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
            </div>

            <!-- Type Filter -->
            <div class="mb-6">
              <h3 class="text-sm font-semibold text-teal-700 mb-2" id="type-filter-label">Type</h3>
              <mat-form-field appearance="outline" class="w-full">
                <mat-select [ngModel]="shopParams().typeId"
                            (ngModelChange)="onTypeSelected($event)"
                            aria-labelledby="type-filter-label">
                  <mat-option [value]="0">All Types</mat-option>
                  @for (type of types(); track type.id) {
                    <mat-option [value]="type.id">{{ type.name }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
            </div>

            <!-- Reset Filters -->
            <button mat-stroked-button color="primary" class="w-full font-semibold" (click)="resetFilters()">
              <mat-icon>filter_alt_off</mat-icon>
              Reset Filters
            </button>
          </div>
        </aside>

        <!-- Product Grid -->
        <main class="flex-1" aria-live="polite" aria-label="Product listing">
          @if (loading()) {
            <!-- Loading Skeleton -->
            <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-6">
              @for (item of [1,2,3,4,5,6]; track item) {
                <div class="rounded-lg overflow-hidden bg-white shadow-md animate-pulse">
                  <div class="w-full h-48 bg-gray-200"></div>
                  <div class="p-5 space-y-3">
                    <div class="h-3 bg-gray-200 rounded w-1/2"></div>
                    <div class="h-5 bg-gray-200 rounded w-3/4"></div>
                    <div class="h-6 bg-gray-200 rounded w-1/3"></div>
                  </div>
                  <div class="p-5 pt-0">
                    <div class="h-10 bg-gray-200 rounded w-full"></div>
                  </div>
                </div>
              }
            </div>
          } @else if (products().length > 0) {
            <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-6">
              @for (product of products(); track product.id) {
                <mat-card class="cursor-pointer overflow-hidden transition-shadow hover:shadow-lg"
                          style="border: 1px solid #f3f4f6; display: flex; flex-direction: column;">
                  <a [routerLink]="['/shop', product.id]"
                     class="block"
                     style="text-decoration: none; color: inherit; flex: 1;"
                     [attr.aria-label]="'View details for ' + product.name">
                    <div class="relative overflow-hidden bg-gray-50">
                      <img [src]="product.pictureUrl"
                           [alt]="product.name"
                           class="w-full h-48 object-cover transition-transform hover:scale-105"
                           style="transition-duration: 0.5s;">
                    </div>
                    <mat-card-content class="p-5">
                      <p class="text-sm uppercase tracking-wider text-teal-600 font-semibold mb-2">{{ product.productBrand }}</p>
                      <h3 class="font-bold text-gray-900 mb-3"
                          style="min-height: 2.5rem; overflow: hidden; text-overflow: ellipsis;">{{ product.name }}</h3>
                      <p class="text-2xl font-bold text-teal-700">{{ product.price | currency }}</p>
                    </mat-card-content>
                  </a>
                  <mat-card-actions class="p-5" style="padding-top: 0;">
                    <button mat-raised-button
                            color="primary"
                            class="w-full font-semibold"
                            [attr.aria-label]="'Add ' + product.name + ' to cart'"
                            (click)="addToCart(product, $event)">
                      <mat-icon>add_shopping_cart</mat-icon>
                      Add to Cart
                    </button>
                  </mat-card-actions>
                </mat-card>
              }
            </div>

            <!-- Pagination -->
            <mat-paginator
              class="mt-8 bg-white rounded-lg shadow"
              [length]="totalCount()"
              [pageSize]="shopParams().pageSize"
              [pageIndex]="shopParams().pageNumber - 1"
              [pageSizeOptions]="[6, 12, 24]"
              (page)="onPageChanged($event)"
              showFirstLastButtons
              aria-label="Product pagination">
            </mat-paginator>
          } @else {
            <div class="text-center py-16">
              <mat-icon style="font-size: 4rem; width: 4rem; height: 4rem; color: #d1d5db;"
                        aria-hidden="true">inventory_2</mat-icon>
              <h3 class="text-xl font-semibold text-gray-600 mt-4">No products found</h3>
              <p class="text-gray-500 mt-2">Try adjusting your search or filters</p>
              <button mat-raised-button color="primary" class="mt-4 font-semibold" (click)="resetFilters()">
                Clear Filters
              </button>
            </div>
          }
        </main>
      </div>
    </div>
  `
})
export class ShopComponent implements OnInit {
  private shopService = inject(ShopService);
  private basketService = inject(BasketService);
  private toastr = inject(ToastrService);

  private productsSignal = signal<Product[]>([]);
  private brandsSignal = signal<Brand[]>([]);
  private typesSignal = signal<ProductType[]>([]);
  private loadingSignal = signal(false);

  readonly products = this.productsSignal.asReadonly();
  readonly brands = this.brandsSignal.asReadonly();
  readonly types = this.typesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly totalCount = computed(() => this.shopService.pagination().count);
  readonly shopParams = this.shopService.shopParams;

  searchTerm = signal('');

  async ngOnInit(): Promise<void> {
    this.searchTerm.set(this.shopService.shopParams().search);
    this.loadingSignal.set(true);
    try {
      const [brands, types, result] = await Promise.all([
        firstValueFrom(this.shopService.getBrands()),
        firstValueFrom(this.shopService.getTypes()),
        firstValueFrom(this.shopService.getProducts(false))
      ]);
      this.brandsSignal.set(brands);
      this.typesSignal.set(types);
      this.productsSignal.set(result.data);
    } finally {
      this.loadingSignal.set(false);
    }
  }

  private async loadProducts(useCache: boolean): Promise<void> {
    this.loadingSignal.set(true);
    try {
      const result = await firstValueFrom(this.shopService.getProducts(useCache));
      this.productsSignal.set(result.data);
    } finally {
      this.loadingSignal.set(false);
    }
  }

  onBrandSelected(brandId: number): void {
    const p = this.shopService.getShopParams();
    this.shopService.setShopParams({ ...p, brandId, pageNumber: 1 });
    this.loadProducts(false);
  }

  onTypeSelected(typeId: number): void {
    const p = this.shopService.getShopParams();
    this.shopService.setShopParams({ ...p, typeId, pageNumber: 1 });
    this.loadProducts(false);
  }

  onSortSelected(sort: string): void {
    const p = this.shopService.getShopParams();
    this.shopService.setShopParams({ ...p, sort });
    this.loadProducts(false);
  }

  onSearch(): void {
    const p = this.shopService.getShopParams();
    this.shopService.setShopParams({ ...p, search: this.searchTerm(), pageNumber: 1 });
    this.loadProducts(false);
  }

  onPageChanged(event: PageEvent): void {
    const p = this.shopService.getShopParams();
    this.shopService.setShopParams({
      ...p,
      pageNumber: event.pageIndex + 1,
      pageSize: event.pageSize
    });
    this.loadProducts(true);
  }

  resetFilters(): void {
    this.searchTerm.set('');
    this.shopService.setShopParams({
      brandId: 0, typeId: 0, sort: 'name', pageNumber: 1, pageSize: 6, search: ''
    });
    this.loadProducts(false);
  }

  addToCart(product: Product, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.basketService.addItemToBasket(product);
    this.toastr.success(`${product.name} added to basket`);
  }
}
