import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ToastrService } from 'ngx-toastr';
import { firstValueFrom } from 'rxjs';
import { AccountService } from '../../../core/services/account.service';
import { ExternalLoginInfo } from '../../../shared/models/user.model';

@Component({
  selector: 'app-external-logins',
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-lg mx-auto p-8">
      <h2 class="text-2xl font-bold text-gray-900 mb-6">Linked Accounts</h2>

      @if (loading()) {
        <div class="flex justify-center py-8">
          <mat-spinner diameter="40"></mat-spinner>
        </div>
      } @else {
        <div class="bg-white rounded-xl border border-gray-200 divide-y divide-gray-200">
          @for (login of externalLogins(); track login.provider) {
            <div class="flex items-center justify-between p-4">
              <div class="flex items-center gap-3">
                @if (login.provider === 'Google') {
                  <svg class="w-6 h-6" viewBox="0 0 24 24">
                    <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
                    <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                    <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
                    <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
                  </svg>
                } @else {
                  <mat-icon class="text-gray-400">link</mat-icon>
                }
                <div>
                  <p class="font-medium text-gray-900">{{ login.providerDisplayName }}</p>
                  @if (login.isLinked) {
                    <p class="text-sm text-green-600">Connected</p>
                  } @else {
                    <p class="text-sm text-gray-500">Not connected</p>
                  }
                </div>
              </div>

              <div>
                @if (login.isLinked) {
                  @if (!hasPassword() && isOnlyLinkedProvider(login.provider)) {
                    <span class="text-xs text-gray-400 italic">Cannot unlink — no password set</span>
                  } @else {
                    <button mat-stroked-button color="warn" (click)="onUnlinkGoogle()" [disabled]="unlinking()">
                      @if (unlinking()) {
                        <mat-spinner diameter="16"></mat-spinner>
                      } @else {
                        Unlink
                      }
                    </button>
                  }
                } @else {
                  <button mat-stroked-button (click)="onLinkGoogle()">
                    Link {{ login.providerDisplayName }}
                  </button>
                }
              </div>
            </div>
          }
        </div>

        @if (!hasPassword() && linkedProviders().length === 1) {
          <div class="bg-amber-50 border border-amber-200 rounded-lg p-3 mt-4">
            <p class="text-amber-700 text-sm">
              You sign in with Google only. To unlink Google, first set a password for your account.
            </p>
          </div>
        }
      }
    </div>
  `
})
export class ExternalLoginsComponent implements OnInit {
  private accountService = inject(AccountService);
  private toastr = inject(ToastrService);

  externalLogins = signal<ExternalLoginInfo[]>([]);
  hasPassword = signal(false);
  loading = signal(true);
  unlinking = signal(false);

  readonly linkedProviders = () => this.externalLogins().filter(l => l.isLinked);

  ngOnInit(): void {
    this.loadExternalLogins();
  }

  isOnlyLinkedProvider(provider: string): boolean {
    const linked = this.externalLogins().filter(l => l.isLinked);
    return linked.length === 1 && linked[0].provider === provider;
  }

  onLinkGoogle(): void {
    this.accountService.initiateGoogleLink(window.location.href);
  }

  async onUnlinkGoogle(): Promise<void> {
    this.unlinking.set(true);
    try {
      await firstValueFrom(this.accountService.unlinkGoogle());
      this.toastr.success('Google account unlinked successfully');
      await this.loadExternalLogins();
    } catch {
      this.toastr.error('Failed to unlink Google account. Please try again.');
    } finally {
      this.unlinking.set(false);
    }
  }

  private async loadExternalLogins(): Promise<void> {
    this.loading.set(true);
    try {
      const logins = await firstValueFrom(this.accountService.getExternalLogins());
      this.externalLogins.set(logins);
    } catch {
      this.toastr.error('Failed to load linked accounts.');
    } finally {
      this.loading.set(false);
    }
  }
}
