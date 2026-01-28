import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { map, tap, catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { User, Address } from '../../shared/models/user.model';

@Injectable({ providedIn: 'root' })
export class AccountService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private baseUrl = environment.apiUrl;

  private currentUserSignal = signal<User | null>(null);
  private accessToken = signal<string | null>(null);
  private tokenExpiresAt = signal<Date | null>(null);
  private authInitialized = signal(false);

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isLoggedIn = computed(() => !!this.currentUserSignal());
  readonly isAuthInitialized = this.authInitialized.asReadonly();

  private initialRefreshAttempted = false;
  private refreshInProgress$: Observable<User | null> | null = null;

  getToken(): string | null {
    if (this.accessToken() && this.tokenExpiresAt()) {
      const bufferMs = 30 * 1000;
      if (new Date().getTime() + bufferMs < this.tokenExpiresAt()!.getTime()) {
        return this.accessToken();
      }
    }
    return null;
  }

  login(values: { email: string; password: string }): Observable<User> {
    return this.http.post<User>(`${this.baseUrl}account/login`, values, {
      withCredentials: true
    }).pipe(
      tap(user => this.handleAuthSuccess(user))
    );
  }

  register(values: { email: string; password: string; displayName: string }): Observable<User> {
    return this.http.post<User>(`${this.baseUrl}account/register`, values, {
      withCredentials: true
    }).pipe(
      tap(user => this.handleAuthSuccess(user))
    );
  }

  async initializeAuth(): Promise<void> {
    if (this.initialRefreshAttempted) {
      return;
    }

    this.initialRefreshAttempted = true;
    try {
      await firstValueFrom(this.refreshToken());
    } catch {
      this.currentUserSignal.set(null);
    } finally {
      this.authInitialized.set(true);
    }
  }

  refreshToken(): Observable<User | null> {
    if (this.refreshInProgress$) {
      return this.refreshInProgress$;
    }

    this.refreshInProgress$ = this.http.post<User>(`${this.baseUrl}account/refresh`, {}, {
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
      map(() => this.currentUserSignal())
    );

    return this.refreshInProgress$;
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}account/logout`, {}, {
      withCredentials: true
    }).pipe(
      tap(() => {
        this.clearAuth();
        this.router.navigateByUrl('/');
      }),
      catchError(() => {
        this.clearAuth();
        this.router.navigateByUrl('/');
        return of(undefined);
      })
    );
  }

  checkEmailExists(email: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.baseUrl}account/emailexists?email=${email}`);
  }

  getUserAddress(): Observable<Address> {
    return this.http.get<Address>(`${this.baseUrl}account/address`);
  }

  updateUserAddress(address: Address): Observable<Address> {
    return this.http.put<Address>(`${this.baseUrl}account/address`, address);
  }

  private handleAuthSuccess(user: User): void {
    if (user.token) {
      this.accessToken.set(user.token);
      this.tokenExpiresAt.set(this.getTokenExpiration(user.token));
    }
    this.currentUserSignal.set(user);
  }

  private clearAuth(): void {
    this.accessToken.set(null);
    this.tokenExpiresAt.set(null);
    this.currentUserSignal.set(null);
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
