import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { Router, NavigationExtras } from '@angular/router';
import { Injectable } from '@angular/core';
import { catchError } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
    constructor(private router: Router, private toastr: ToastrService) {}

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        const isLogin = req.url.includes('/login');
        const isRefresh = req.url.includes('/refresh');

        return next.handle(req).pipe(
            catchError(error => {
                if (error) {
                    if (error.status === 400) {
                        if (error.error.errors) {
                            throw error.error;
                        } else {
                            this.toastr.error(error.error.message, error.error.statusCode);
                        }
                    }
                    // Don't show toast for 401 on refresh (handled by interceptor)
                    if (error.status === 401 && !isLogin && !isRefresh) {
                        this.toastr.error(error.error?.message || 'Unauthorized', error.error?.statusCode || '401');
                    }
                    if (error.status === 404) {
                        this.router.navigateByUrl('/not-found');
                    }
                    if (error.status === 500) {
                        const navigationExtras: NavigationExtras = {state: {error: error.error}};
                        this.router.navigateByUrl('/server-error', navigationExtras);
                    }
                }
                return throwError(error);
            })
        );
    }
}
