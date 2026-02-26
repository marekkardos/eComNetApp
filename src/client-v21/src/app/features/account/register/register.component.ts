import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AccountService } from '../../../core/services/account.service';
import { BusyService } from '../../../core/services/busy.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-register',
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-white rounded-2xl shadow-xl p-8">
      <!-- Logo -->
      <div class="text-center mb-8">
        <h1 class="text-3xl font-bold text-gray-900">Skishop</h1>
        <p class="text-gray-500 mt-2">Create your account</p>
      </div>

      <!-- Form -->
      <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Display Name</mat-label>
          <input matInput formControlName="displayName" autocomplete="name">
          <mat-icon matSuffix>person</mat-icon>
          @if (registerForm.get('displayName')?.hasError('required') && registerForm.get('displayName')?.touched) {
            <mat-error>Display name is required</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="w-full mt-4">
          <mat-label>Email</mat-label>
          <input matInput formControlName="email" type="email" autocomplete="email" (blur)="checkEmail()">
          @if (checkingEmail()) {
            <mat-spinner matSuffix diameter="20"></mat-spinner>
          } @else {
            <mat-icon matSuffix>mail</mat-icon>
          }
          @if (registerForm.get('email')?.hasError('required') && registerForm.get('email')?.touched) {
            <mat-error>Email is required</mat-error>
          }
          @if (registerForm.get('email')?.hasError('email') && registerForm.get('email')?.touched) {
            <mat-error>Please enter a valid email</mat-error>
          }
          @if (emailExists() && !checkingEmail()) {
            <mat-error>Email is already taken</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="w-full mt-4">
          <mat-label>Password</mat-label>
          <input matInput formControlName="password" [type]="hidePassword() ? 'password' : 'text'" autocomplete="new-password">
          <button mat-icon-button matSuffix type="button" (click)="hidePassword.set(!hidePassword())">
            <mat-icon>{{ hidePassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
          </button>
          @if (registerForm.get('password')?.hasError('required') && registerForm.get('password')?.touched) {
            <mat-error>Password is required</mat-error>
          }
          @if (registerForm.get('password')?.hasError('minlength') && registerForm.get('password')?.touched) {
            <mat-error>Password must be at least 6 characters</mat-error>
          }
        </mat-form-field>

        <!-- Password Strength Indicator -->
        @if (registerForm.get('password')?.value) {
          <div class="mt-2">
            <div class="flex gap-1">
              <span>Strength:</span>
              <div class="h-1 flex-1 rounded" [class]="getPasswordStrengthClass(0)">&nbsp;</div>
              <div class="h-1 flex-1 rounded" [class]="getPasswordStrengthClass(1)">&nbsp;</div>
              <div class="h-1 flex-1 rounded" [class]="getPasswordStrengthClass(2)">&nbsp;</div>
              <div class="h-1 flex-1 rounded" [class]="getPasswordStrengthClass(3)">&nbsp;</div>
            </div>
            <p class="text-xs text-gray-500 mt-1">{{ getPasswordStrengthText() }}</p>
          </div>
        }

        @if (errors().length > 0) {
          <div class="bg-red-50 border border-red-200 rounded-lg p-3 mt-4">
            @for (error of errors(); track error) {
              <p class="text-red-600 text-sm">{{ error }}</p>
            }
          </div>
        }

        <button mat-raised-button color="primary" class="w-full mt-6 py-3" type="submit" [disabled]="registerForm.invalid || this.busyRequestCount() > 0">
          @if (this.busyRequestCount() > 0) {
            <span>Creating Account...</span>
          } @else {
            <span>Create Account</span>
          }
        </button>
      </form>

      <!-- Divider -->
      <div class="flex items-center my-6">
        <div class="flex-1 border-t border-gray-300"></div>
        <span class="px-4 text-gray-500 text-sm">or</span>
        <div class="flex-1 border-t border-gray-300"></div>
      </div>

      <!-- Google Sign Up -->
      <button mat-stroked-button class="w-full py-3" type="button" (click)="onGoogleRegister()">
        <div class="flex items-center justify-center gap-2">
          <svg class="w-5 h-5" viewBox="0 0 24 24">
            <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
            <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
            <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
            <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
          </svg>
          <span>Sign up with Google</span>
        </div>
      </button>

      <!-- Login Link -->
      <p class="text-center mt-6 text-gray-600">
        Already have an account?
        <a routerLink="/account/login" class="font-medium hover:underline" style="color: var(--color-primary-600)">
          Sign in
        </a>
      </p>
    </div>
  `
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private accountService = inject(AccountService);
  private busyService = inject(BusyService);

  registerForm = this.fb.group({
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  hidePassword = signal(true);
  errors = signal<string[]>([]);
  emailExists = signal(false);
  checkingEmail = signal(false);
 
  busyRequestCount(): number {
    return this.busyService.busyRequestCount();
  }
  
  getPasswordStrength(): number {
    const password = this.registerForm.get('password')?.value || '';
    let strength = 0;
    if (password.length >= 6) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[a-z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    return strength;
  }

  getPasswordStrengthClass(index: number): string {
    const strength = this.getPasswordStrength();
    if (index < strength) {
      if (strength <= 1) return 'bg-red-400';
      if (strength <= 2) return 'bg-yellow-400';
      if (strength <= 3) return 'bg-blue-400';
      return 'bg-green-400';
    }
    return 'bg-gray-200';
  }

  getPasswordStrengthText(): string {
    const strength = this.getPasswordStrength();
    if (strength <= 1) return 'Weak';
    if (strength <= 2) return 'Fair';
    if (strength <= 3) return 'Good';
    return 'Strong';
  }

  async checkEmail(): Promise<void> {
    const emailControl = this.registerForm.get('email');
    if (!emailControl || emailControl.invalid) {
      this.emailExists.set(false);
      return;
    }

    const email = emailControl.value;
    if (!email || email.trim() === '') {
      this.emailExists.set(false);
      return;
    }

    if (emailControl.pristine) {
      return;
    }

    this.checkingEmail.set(true);
    this.emailExists.set(false);

    try {
      const exists = await firstValueFrom(this.accountService.checkEmailExists(email));
      this.emailExists.set(exists);
      if (exists) {
        emailControl.setErrors({ emailExists: true });
      }
    } catch {
      this.emailExists.set(false);
    } finally {
      this.checkingEmail.set(false);
    }
  }

  onGoogleRegister(): void {
    const returnUrl = `${window.location.origin}/account/login`;
    this.accountService.initiateGoogleLogin(returnUrl);
  }

  async onSubmit(): Promise<void> {
    if (!this.registerForm.valid) {
      return;
    }

    const { displayName, email, password } = this.registerForm.value;
    if (!displayName || !email || !password) {
      return;
    }

    this.errors.set([]);
    this.busyService.busy();

    try {
      await firstValueFrom(this.accountService.register({ displayName, email, password }));
      this.router.navigateByUrl('/shop');
    } catch (err) {
      console.warn('Registration error:', err);
      
      if (Array.isArray(err)) {
        this.errors.set(err);
      } else {
        const error = err as { error?: { errors?: string[]; message?: string; statusCode?: number } };
        if (error.error?.errors && Array.isArray(error.error.errors)) {
          this.errors.set(error.error.errors);
        } else if (error.error?.message) {
          this.errors.set([error.error.message]);
        } else {
          this.errors.set(['Registration failed. Please try again.']);
        }
      }
    } finally {
      this.busyService.idle();
    }
  }
}
