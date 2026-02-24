import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError, ReplaySubject } from 'rxjs';
import { IUser, IUserWithExternalLogins } from '../shared/models/user';
import { tap, catchError, shareReplay } from 'rxjs/operators';
import { Router } from '@angular/router';
import { IAddress } from '../shared/models/address';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  baseUrl = environment.apiUrl;

  // In-memory token storage (NOT localStorage)
  private accessToken: string | null = null;
  private tokenExpiresAt: Date | null = null;

  private currentUserSource = new BehaviorSubject<IUser | null>(null);
  currentUser$ = this.currentUserSource.asObservable();

  // Track auth initialization - guards should wait for this
  private authInitializedSource = new ReplaySubject<boolean>(1);
  authInitialized$ = this.authInitializedSource.asObservable();

  // Track if initial refresh has been attempted
  private initialRefreshAttempted = false;
  private refreshInProgress$: Observable<IUser | null> | null = null;

  constructor(private http: HttpClient, private router: Router) { }

  // Called on app initialization
  initializeAuth(): Observable<IUser | null> {
    if (this.initialRefreshAttempted) {
      return this.currentUser$;
    }

    this.initialRefreshAttempted = true;
    return this.refreshToken().pipe(
      tap(() => this.authInitializedSource.next(true)),
      catchError(() => {
        // No valid refresh token - user needs to login
        this.currentUserSource.next(null);
        this.authInitializedSource.next(true);
        return of(null);
      })
    );
  }

  getAccessToken(): string | null {
    // Check if token is expired (with 30-second buffer)
    if (this.accessToken && this.tokenExpiresAt) {
      const bufferMs = 30 * 1000;
      if (new Date().getTime() + bufferMs < this.tokenExpiresAt.getTime()) {
        return this.accessToken;
      }
    }
    return null;
  }

  login(values: any): Observable<IUser> {
    return this.http.post<IUser>(this.baseUrl + 'account/login', values, {
      withCredentials: true
    }).pipe(
      tap(user => this.handleAuthSuccess(user))
    );
  }

  register(values: any): Observable<IUser> {
    return this.http.post<IUser>(this.baseUrl + 'account/register', values, {
      withCredentials: true
    }).pipe(
      tap(user => this.handleAuthSuccess(user))
    );
  }

  refreshToken(): Observable<IUser | null> {
    // Prevent multiple simultaneous refresh requests
    if (this.refreshInProgress$) {
      return this.refreshInProgress$;
    }

    this.refreshInProgress$ = this.http.post<IUser>(this.baseUrl + 'account/refresh', {}, {
      withCredentials: true
    }).pipe(
      tap(user => {
        this.handleAuthSuccess(user);
        this.refreshInProgress$ = null;
      }),
      catchError(error => {
        this.refreshInProgress$ = null;
        this.clearAuth();
        return throwError(() => error);
      }),
      shareReplay(1)
    );

    return this.refreshInProgress$;
  }

  logout(): Observable<void> {
    return this.http.post<void>(this.baseUrl + 'account/logout', {}, {
      withCredentials: true
    }).pipe(
      tap(() => {
        this.clearAuth();
        this.router.navigateByUrl('/');
      }),
      catchError(() => {
        // Clear local state even if server call fails
        this.clearAuth();
        this.router.navigateByUrl('/');
        return of(undefined);
      })
    );
  }

  checkEmailExists(email: string): Observable<boolean> {
    return this.http.get<boolean>(this.baseUrl + 'account/emailexists?email=' + email);
  }

  getUserAddress(): Observable<IAddress> {
    return this.http.get<IAddress>(this.baseUrl + 'account/address');
  }

  updateUserAddress(address: IAddress): Observable<IAddress> {
    return this.http.put<IAddress>(this.baseUrl + 'account/address', address);
  }

  // --- External Authentication (Google OAuth) ---

  /**
   * Initiates Google OAuth login by redirecting to the API endpoint.
   * @param returnUrl The URL to redirect to after successful authentication.
   */
  initiateGoogleLogin(returnUrl: string = '/'): void {
    const encodedReturnUrl = encodeURIComponent(returnUrl);
    window.location.href = `${this.baseUrl}externalauth/google?returnUrl=${encodedReturnUrl}`;
  }

  /**
   * Exchanges an authorization code for an access token.
   * Called after redirect from OAuth callback.
   * @param code The short-lived authorization code.
   */
  exchangeAuthCode(code: string): Observable<IUser> {
    return this.http.post<IUser>(this.baseUrl + 'externalauth/exchange', { code }, {
      withCredentials: true
    }).pipe(
      tap(user => this.handleAuthSuccess(user))
    );
  }

  /**
   * Gets the current user's external login providers and link status.
   */
  getExternalLogins(): Observable<IUserWithExternalLogins> {
    return this.http.get<IUserWithExternalLogins>(this.baseUrl + 'externalauth/providers');
  }

  /**
   * Initiates Google OAuth linking for authenticated users.
   * @param returnUrl The URL to redirect to after linking.
   */
  initiateGoogleLink(returnUrl: string = '/'): void {
    const encodedReturnUrl = encodeURIComponent(returnUrl);
    window.location.href = `${this.baseUrl}externalauth/google/link?returnUrl=${encodedReturnUrl}`;
  }

  /**
   * Unlinks Google from the current user's account.
   */
  unlinkGoogle(): Observable<void> {
    return this.http.delete<void>(this.baseUrl + 'externalauth/google/unlink');
  }

  private handleAuthSuccess(user: IUser): void {
    if (user.token) {
      this.accessToken = user.token;
      this.tokenExpiresAt = this.getTokenExpiration(user.token);
    }
    this.currentUserSource.next(user);
  }

  private clearAuth(): void {
    this.accessToken = null;
    this.tokenExpiresAt = null;
    this.currentUserSource.next(null);
  }

  private getTokenExpiration(token: string): Date | null {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      if (payload.exp) {
        return new Date(payload.exp * 1000);
      }
    } catch {
      console.error('Failed to parse token expiration');
    }
    return null;
  }
}
