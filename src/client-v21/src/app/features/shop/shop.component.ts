import { Component, ChangeDetectionStrategy, signal, computed } from '@angular/core';
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
import { MOCK_PRODUCTS, MOCK_BRANDS, MOCK_TYPES } from '../../shared/mock-data';
import { Product } from '../../shared/models';

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
          <p class="text-teal-600 mt-1 font-semibold">{{ filteredProducts().length }} products found</p>
        </div>

        <!-- Search and Sort -->
        <div class="flex flex-col sm:flex-row gap-4 w-full md:w-auto">
          <mat-form-field appearance="outline" class="w-full sm:w-64">
            <mat-label>Search</mat-label>
            <input matInput [ngModel]="searchTerm()" (ngModelChange)="searchTerm.set($event)" placeholder="Search products...">
            <mat-icon matSuffix>search</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full sm:w-48">
            <mat-label>Sort By</mat-label>
            <mat-select [ngModel]="sortOption()" (ngModelChange)="sortOption.set($event)">
              <mat-option value="name">Name (A-Z)</mat-option>
              <mat-option value="priceAsc">Price: Low to High</mat-option>
              <mat-option value="priceDesc">Price: High to Low</mat-option>
            </mat-select>
          </mat-form-field>
        </div>
      </div>

      <div class="flex flex-col gap-8">
        <!-- Sidebar Filters -->
        <aside class="w-full">
          <div class="bg-white rounded-lg shadow-md p-6" style="border-left: 4px solid var(--color-primary-600);">
            <h2 class="font-semibold text-gray-900 mb-6 text-lg">Filters</h2>

            <!-- Brand Filter -->
            <div class="mb-6">
              <h3 class="text-sm font-semibold text-teal-700 mb-2">Brand</h3>
              <mat-form-field appearance="outline" class="w-full">
                <mat-select [ngModel]="selectedBrandId()" (ngModelChange)="selectedBrandId.set($event)">
                  <mat-option [value]="0">All Brands</mat-option>
                  @for (brand of brands; track brand.id) {
                    <mat-option [value]="brand.id">{{ brand.name }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
            </div>

            <!-- Type Filter -->
            <div class="mb-6">
              <h3 class="text-sm font-semibold text-teal-700 mb-2">Type</h3>
              <mat-form-field appearance="outline" class="w-full">
                <mat-select [ngModel]="selectedTypeId()" (ngModelChange)="selectedTypeId.set($event)">
                  <mat-option [value]="0">All Types</mat-option>
                  @for (type of types; track type.id) {
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
        <main class="flex-1">
          @if (paginatedProducts().length > 0) {
            <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-6">
              @for (product of paginatedProducts(); track product.id) {
                <mat-card class="cursor-pointer overflow-hidden transition-shadow hover:shadow-lg" style="border: 1px solid #f3f4f6; display: flex; flex-direction: column;">
                  <a [routerLink]="['/shop', product.id]"
                     class="block"
                     style="text-decoration: none; color: inherit; flex: 1;"
                     tabindex="0"
                     [attr.aria-label]="'View details for ' + product.name">
                    <div class="relative overflow-hidden bg-gray-50">
                      <img [src]="product.pictureUrl"
                           [alt]="product.name"
                           class="w-full h-48 object-cover transition-transform hover:scale-105" style="transition-duration: 0.5s;">
                    </div>
                    <mat-card-content class="p-5">
                      <p class="text-sm uppercase tracking-wider text-teal-600 font-semibold mb-2">{{ product.productBrand }}</p>
                      <h3 class="font-bold text-gray-900 mb-3" style="min-height: 2.5rem; overflow: hidden; text-overflow: ellipsis;">{{ product.name }}</h3>
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
              [length]="filteredProducts().length"
              [pageSize]="pageSize()"
              [pageIndex]="pageIndex()"
              [pageSizeOptions]="[6, 12, 24]"
              (page)="onPageChange($event)"
              showFirstLastButtons>
            </mat-paginator>
          } @else {
            <div class="text-center py-16">
              <mat-icon class="text-primary-200" style="font-size: 4rem; width: 4rem; height: 4rem;">inventory_2</mat-icon>
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
export class ShopComponent {
  // Mock data
  brands = MOCK_BRANDS;
  types = MOCK_TYPES;
  private allProducts = MOCK_PRODUCTS;

  // Filter signals
  searchTerm = signal('');
  selectedBrandId = signal(0);
  selectedTypeId = signal(0);
  sortOption = signal('name');

  // Pagination signals
  pageIndex = signal(0);
  pageSize = signal(6);

  // Computed filtered products
  filteredProducts = computed(() => {
    let products = [...this.allProducts];

    // Filter by search
    const search = this.searchTerm().toLowerCase();
    if (search) {
      products = products.filter(p =>
        p.name.toLowerCase().includes(search) ||
        p.productBrand.toLowerCase().includes(search) ||
        p.productType.toLowerCase().includes(search)
      );
    }

    // Filter by brand
    const brandId = this.selectedBrandId();
    if (brandId > 0) {
      const brand = this.brands.find(b => b.id === brandId);
      if (brand) {
        products = products.filter(p => p.productBrand === brand.name);
      }
    }

    // Filter by type
    const typeId = this.selectedTypeId();
    if (typeId > 0) {
      const type = this.types.find(t => t.id === typeId);
      if (type) {
        products = products.filter(p => p.productType === type.name);
      }
    }

    // Sort
    switch (this.sortOption()) {
      case 'priceAsc':
        products.sort((a, b) => a.price - b.price);
        break;
      case 'priceDesc':
        products.sort((a, b) => b.price - a.price);
        break;
      case 'name':
      default:
        products.sort((a, b) => a.name.localeCompare(b.name));
    }

    return products;
  });

  // Computed paginated products
  paginatedProducts = computed(() => {
    const start = this.pageIndex() * this.pageSize();
    const end = start + this.pageSize();
    return this.filteredProducts().slice(start, end);
  });

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
  }

  resetFilters(): void {
    this.searchTerm.set('');
    this.selectedBrandId.set(0);
    this.selectedTypeId.set(0);
    this.sortOption.set('name');
    this.pageIndex.set(0);
  }

  addToCart(product: Product, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    // Mock - will be implemented with real service in Phase 2
    console.log('Added to cart:', product.name);
  }
}
