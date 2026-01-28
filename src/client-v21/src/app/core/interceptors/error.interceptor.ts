import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const toastr = inject(ToastrService);

  return next(req).pipe(
    catchError(error => {
      if (error.status === 400) {
        if (error.error.errors) {
          const errors = Object.values(error.error.errors).flat();
          throw errors;
        }
        toastr.error(error.error.message || 'Bad request');
      }
      if (error.status === 401) {
        toastr.error('Unauthorized');
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
