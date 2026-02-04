import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AccountService } from 'src/app/account/account.service';
import { Router } from '@angular/router';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
    private isRefreshing = false;
    private refreshTokenSubject = new BehaviorSubject<string | null>(null);

    constructor(private accountService: AccountService, private router: Router) {}

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        // Skip token attachment for auth endpoints
        if (this.isAuthEndpoint(req.url)) {
            return next.handle(req);
        }

        const token = this.accountService.getAccessToken();

        if (token) {
            req = this.addToken(req, token);
        }

        return next.handle(req).pipe(
            catchError(error => {
                if (error instanceof HttpErrorResponse && error.status === 401) {
                    return this.handle401Error(req, next);
                }
                return throwError(error);
            })
        );
    }

    private addToken(request: HttpRequest<any>, token: string): HttpRequest<any> {
        return request.clone({
            setHeaders: {
                Authorization: `Bearer ${token}`
            }
        });
    }

    private handle401Error(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        if (!this.isRefreshing) {
            this.isRefreshing = true;
            this.refreshTokenSubject.next(null);

            return this.accountService.refreshToken().pipe(
                switchMap(user => {
                    this.isRefreshing = false;
                    const newToken = user?.token || null;
                    this.refreshTokenSubject.next(newToken);

                    if (newToken) {
                        return next.handle(this.addToken(request, newToken));
                    }
                    return throwError(new Error('No token after refresh'));
                }),
                catchError(err => {
                    this.isRefreshing = false;
                    this.refreshTokenSubject.next(null);
                    // Refresh failed - user needs to login again
                    const redirectUrl = '/account/login?returnUrl=' + encodeURIComponent(this.router.url);
                    console.log('Redirecting to login page:', redirectUrl);
                    this.router.navigateByUrl(redirectUrl)
                    return throwError(err);
                })
            );
        }

        // Wait for the refresh to complete
        return this.refreshTokenSubject.pipe(
            filter(token => token !== null),
            take(1),
            switchMap(token => next.handle(this.addToken(request, token!)))
        );
    }

    private isAuthEndpoint(url: string): boolean {
        const authEndpoints = ['/account/login', '/account/register', '/account/refresh', '/account/logout'];
        return authEndpoints.some(endpoint => url.includes(endpoint));
    }
}
