import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const toastr = inject(ToastrService);

  const isLogin = req.url.includes('/login');
  const isRefresh = req.url.includes('/refresh');

  return next(req).pipe(
    catchError(error => {
      if (error.status === 400) {
        if (error.error.errors) {
          const errors = Array.isArray(error.error.errors) 
            ? error.error.errors 
            : Object.values(error.error.errors).flat();
          throw errors;
        }
        toastr.error(error.error.message || 'Bad request');
      }
      // Don't show toast for 401 on refresh (handled by interceptor)
      if (error.status === 401 && !isLogin && !isRefresh) {
          toastr.error(error.error?.message || 'Unauthorized', error.error?.statusCode || '401');
      }
      if (error.status === 404) {
        router.navigateByUrl('/not-found');
      }
      if (error.status === 500) {
        router.navigateByUrl('/server-error', { state: error.error });
      }
      return throwError(() => error);
    })
  );
};
