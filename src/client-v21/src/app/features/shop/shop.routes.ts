import { Routes } from '@angular/router';

export const SHOP_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./shop.component').then(m => m.ShopComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./product-details/product-details.component').then(m => m.ProductDetailsComponent),
    data: { breadcrumb: { alias: 'productDetails' } }
  }
];
