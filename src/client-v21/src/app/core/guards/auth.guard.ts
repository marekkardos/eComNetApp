import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AccountService } from '../services/account.service';

export const authGuard: CanActivateFn = async (route, state) => {
  const accountService = inject(AccountService);
  const router = inject(Router);

  if (!accountService.isAuthInitialized()) {
    await accountService.initializeAuth();
  }

  if (accountService.isLoggedIn()) {
    return true;
  }

  return router.createUrlTree(['/account/login'], {
    queryParams: { returnUrl: state.url }
  });
};
