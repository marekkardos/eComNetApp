import { Routes } from '@angular/router';

export const ORDERS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./orders.component').then(m => m.OrdersComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./order-details/order-details.component').then(m => m.OrderDetailsComponent),
    data: { breadcrumb: { alias: 'orderDetails' } }
  }
];
