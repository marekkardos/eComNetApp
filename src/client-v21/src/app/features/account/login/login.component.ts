import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { RouterLink, Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AccountService } from '../../../core/services/account.service';
import { BusyService } from '../../../core/services/busy.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-login',
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-white rounded-2xl shadow-xl p-8">
      <!-- Logo -->
      <div class="text-center mb-8">
        <h1 class="text-3xl font-bold text-gray-900">Skishop</h1>
        <p class="text-gray-500 mt-2">Sign in to your account</p>
      </div>

      <!-- Form -->
      <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Email</mat-label>
          <input matInput formControlName="email" type="email" autocomplete="email">
          <mat-icon matSuffix>mail</mat-icon>
          @if (loginForm.get('email')?.hasError('required') && loginForm.get('email')?.touched) {
            <mat-error>Email is required</mat-error>
          }
          @if (loginForm.get('email')?.hasError('email') && loginForm.get('email')?.touched) {
            <mat-error>Please enter a valid email</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="w-full mt-4">
          <mat-label>Password</mat-label>
          <input matInput formControlName="password" [type]="hidePassword() ? 'password' : 'text'" autocomplete="current-password">
          <button mat-icon-button matSuffix type="button" (click)="hidePassword.set(!hidePassword())">
            <mat-icon>{{ hidePassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
          </button>
          @if (loginForm.get('password')?.hasError('required') && loginForm.get('password')?.touched) {
            <mat-error>Password is required</mat-error>
          }
        </mat-form-field>

        @if (loginError()) {
          <div class="bg-red-50 border border-red-200 rounded-lg p-3 mt-4">
            <p class="text-red-600 text-sm">Invalid email or password</p>
          </div>
        }

        <button mat-raised-button color="primary" class="w-full mt-6 py-3" type="submit" [disabled]="loginForm.invalid || this.busyRequestCount() > 0">
          @if (this.busyRequestCount() > 0) {
            <span>Signing in...</span>
          } @else {
            <span>Sign in</span>
          }
        </button>
      </form>

      <!-- Divider -->
      <div class="flex items-center my-6">
        <div class="flex-1 border-t border-gray-300"></div>
        <span class="px-4 text-gray-500 text-sm">or</span>
        <div class="flex-1 border-t border-gray-300"></div>
      </div>

      <!-- Google Sign In (Placeholder) -->
      <button mat-stroked-button class="w-full py-3" type="button">
        <div class="flex items-center justify-center gap-2">
          <svg class="w-5 h-5" viewBox="0 0 24 24">
            <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
            <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
            <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
            <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
          </svg>
          <span>Sign in with Google</span>
        </div>
      </button>

      <!-- Register Link -->
      <p class="text-center mt-6 text-gray-600">
        Don't have an account?
        <a routerLink="/account/register" class="font-medium hover:underline" style="color: var(--color-primary-600)">
          Sign up
        </a>
      </p>
    </div>
  `
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private accountService = inject(AccountService);
  private busyService = inject(BusyService);

  loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  hidePassword = signal(true);
  loginError = signal(false);

  busyRequestCount(): number {
    return this.busyService.busyRequestCount();
  }

  async onSubmit(): Promise<void> {
    if (!this.loginForm.valid) {
      return;
    }

    const { email, password } = this.loginForm.value;
    if (!email || !password) {
      return;
    }

    this.loginError.set(false);
    this.busyService.busy();

    try {
      await firstValueFrom(this.accountService.login({ email, password }));
      const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/shop';
      this.router.navigateByUrl(returnUrl);
    } catch {
      this.loginError.set(true);
    } finally {
      this.busyService.idle();
    }
  }
}
