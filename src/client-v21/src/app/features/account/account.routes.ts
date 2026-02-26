import { Routes } from '@angular/router';

export const ACCOUNT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('../../core/layouts/auth-layout/auth-layout.component').then(m => m.AuthLayoutComponent),
    children: [
      {
        path: 'login',
        loadComponent: () => import('./login/login.component').then(m => m.LoginComponent),
        data: { breadcrumb: 'Login' }
      },
      {
        path: 'register',
        loadComponent: () => import('./register/register.component').then(m => m.RegisterComponent),
        data: { breadcrumb: 'Register' }
      }
    ]
  },
  {
    path: 'external-logins',
    loadComponent: () => import('./external-logins/external-logins.component').then(m => m.ExternalLoginsComponent),
    data: { breadcrumb: 'Linked Accounts' }
  }
];
