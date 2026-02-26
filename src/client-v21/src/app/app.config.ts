import { ApplicationConfig, inject, provideBrowserGlobalErrorListeners, provideAppInitializer, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding, withViewTransitions } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';

import { firstValueFrom } from 'rxjs';
import { routes } from './app.routes';
import { jwtInterceptor, errorInterceptor, loadingInterceptor } from './core/interceptors';
import { AccountService } from './core/services/account.service';
import { BasketService } from './core/services/basket.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withViewTransitions()
    ),
    provideHttpClient(
      withInterceptors([
        jwtInterceptor,
        errorInterceptor,
        loadingInterceptor
      ])
    ),
    provideAnimations(),
    provideToastr({
      positionClass: 'toast-bottom-right',
      preventDuplicates: true,
      timeOut: 3000,
      progressBar: true
    }),
    provideAppInitializer(() => {
      const accountService = inject(AccountService);
      return accountService.initializeAuth();
    }),
    provideAppInitializer(() => {
      const basketService = inject(BasketService);
      const basketId = localStorage.getItem('basket_id');
      if (basketId) {
        return firstValueFrom(basketService.getBasket(basketId)).catch(() => {
          localStorage.removeItem('basket_id');
        });
      }
      return Promise.resolve();
    })
  ]
};
