import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap } from 'rxjs';
import { AccountService } from '../services/account.service';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const accountService = inject(AccountService);

  const skipAuthEndpoints = ['account/login', 'account/register', 'account/refresh', 'externalauth/exchange'];
  if (skipAuthEndpoints.some(endpoint => req.url.includes(endpoint))) {
    return next(req);
  }

  const token = accountService.getToken();

  if (token) {
    return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
  }

  // Token missing or expired — if user is logged in, refresh silently then retry once.
  // AccountService.refreshToken() deduplicates concurrent refreshes, so parallel
  // requests all share the same in-flight refresh call.
  if (!accountService.currentUser()) {
    return next(req); // anonymous request, no refresh needed
  }

  return accountService.refreshToken().pipe(
    switchMap(() => {
      const freshToken = accountService.getToken();
      const retryReq = freshToken
        ? req.clone({ setHeaders: { Authorization: `Bearer ${freshToken}` } })
        : req;
      return next(retryReq);
    }),
    catchError(() => next(req)) // refresh failed — let the request go as-is (server will 401)
  );
};
