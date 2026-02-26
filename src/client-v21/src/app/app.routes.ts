import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'home',
    pathMatch: 'full'
  },
  {
    path: 'home',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent),
    data: { breadcrumb: 'Home' }
  },
  {
    path: 'shop',
    loadChildren: () => import('./features/shop/shop.routes').then(m => m.SHOP_ROUTES),
    data: { breadcrumb: 'Shop' }
  },
  {
    path: 'basket',
    loadComponent: () => import('./features/basket/basket.component').then(m => m.BasketComponent),
    data: { breadcrumb: 'Basket' }
  },
  {
    path: 'checkout',
    loadChildren: () => import('./features/checkout/checkout.routes').then(m => m.CHECKOUT_ROUTES),
    canActivate: [authGuard],
    data: { breadcrumb: 'Checkout' }
  },
  {
    path: 'orders',
    loadChildren: () => import('./features/orders/orders.routes').then(m => m.ORDERS_ROUTES),
    canActivate: [authGuard],
    data: { breadcrumb: 'Orders' }
  },
  {
    path: 'account',
    loadChildren: () => import('./features/account/account.routes').then(m => m.ACCOUNT_ROUTES),
    data: { breadcrumb: { skip: true } }
  },
  {
    path: 'not-found',
    loadComponent: () => import('./core/components/not-found/not-found.component').then(m => m.NotFoundComponent),
    data: { breadcrumb: 'Not Found' }
  },
  {
    path: 'server-error',
    loadComponent: () => import('./core/components/server-error/server-error.component').then(m => m.ServerErrorComponent),
    data: { breadcrumb: 'Server Error' }
  },
  {
    path: '**',
    redirectTo: 'not-found'
  }
];
