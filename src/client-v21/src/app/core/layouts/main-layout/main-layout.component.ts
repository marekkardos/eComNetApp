import { Component, ChangeDetectionStrategy, signal, computed } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MOCK_BASKET_ITEMS } from '../../../shared/mock-data';
import { MOCK_USER } from '../../../shared/mock-data';

@Component({
  selector: 'app-main-layout',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatBadgeModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Navigation Bar -->
    <nav class="fixed top-0 left-0 right-0 bg-white shadow-sm z-50">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between items-center h-16">
          <!-- Logo -->
          <a routerLink="/" class="text-2xl font-bold" style="color: var(--color-primary-600)">
            Skishop
          </a>

          <!-- Nav Links -->
          <div class="hidden md:flex items-center space-x-8">
            <a routerLink="/shop"
               routerLinkActive="font-semibold"
               [routerLinkActiveOptions]="{exact: false}"
               class="text-gray-600 hover:text-primary-600 transition-colors"
               [class.text-primary-600]="false">
              Shop
            </a>
          </div>

          <!-- Right Section -->
          <div class="flex items-center space-x-4">
            <!-- Cart Icon -->
            <a routerLink="/basket"
               class="relative p-2 text-gray-600 hover:text-primary-600 transition-colors"
               [attr.aria-label]="'Shopping cart, ' + cartItemCount() + ' items'">
              <mat-icon>shopping_cart</mat-icon>
              @if (cartItemCount() > 0) {
                <span class="absolute -top-1 -right-1 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center"
                      style="background-color: var(--color-accent-500)"
                      aria-hidden="true">
                  {{ cartItemCount() }}
                </span>
              }
            </a>

            <!-- User Menu -->
            @if (isLoggedIn()) {
              <button mat-button [matMenuTriggerFor]="userMenu" class="text-gray-700">
                {{ currentUser().displayName }}
                <mat-icon>arrow_drop_down</mat-icon>
              </button>
              <mat-menu #userMenu="matMenu">
                <a mat-menu-item routerLink="/orders">
                  <mat-icon>receipt_long</mat-icon>
                  <span>My Orders</span>
                </a>
                <button mat-menu-item (click)="logout()">
                  <mat-icon>logout</mat-icon>
                  <span>Logout</span>
                </button>
              </mat-menu>
            } @else {
              <a mat-stroked-button routerLink="/account/login" style="border-color: var(--color-primary-600); color: var(--color-primary-600)">
                Login
              </a>
            }
          </div>
        </div>
      </div>
    </nav>

    <!-- Main Content -->
    <main class="pt-16 min-h-screen bg-gray-50">
      <router-outlet />
    </main>

    <!-- Footer -->
    <footer class="bg-gray-800 text-white py-8">
      <div class="max-w-7xl mx-auto px-4 text-center">
        <p class="text-gray-400">&copy; 2026 Skishop. All rights reserved.</p>
      </div>
    </footer>
  `
})
export class MainLayoutComponent {
  // Mock state - will be replaced with real services in Phase 2
  private mockLoggedIn = signal(true);
  private mockUser = signal(MOCK_USER);
  private mockCartItems = signal(MOCK_BASKET_ITEMS);

  isLoggedIn = this.mockLoggedIn.asReadonly();
  currentUser = this.mockUser.asReadonly();

  cartItemCount = computed(() =>
    this.mockCartItems().reduce((sum, item) => sum + item.quantity, 0)
  );

  logout(): void {
    this.mockLoggedIn.set(false);
    this.mockUser.set(null as any);
  }
}
