import { Component, OnInit } from '@angular/core';
import { AccountService } from '../account.service';
import { IExternalLoginInfo, EXTERNAL_LOGIN_PROVIDERS, IExternalLoginProvider } from '../../shared/models/external-login';
import { Router } from '@angular/router';

@Component({
  selector: 'app-linked-accounts',
  templateUrl: './linked-accounts.component.html',
  styleUrls: ['./linked-accounts.component.scss']
})
export class LinkedAccountsComponent implements OnInit {
  linkedLogins: IExternalLoginInfo[] = [];
  availableProviders: IExternalLoginProvider[] = EXTERNAL_LOGIN_PROVIDERS;
  isLoading: boolean = true;
  isUnlinking: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  constructor(private accountService: AccountService, private router: Router) { }

  ngOnInit() {
    this.loadLinkedAccounts();
  }

  loadLinkedAccounts(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.accountService.getLinkedLogins().subscribe(
      (logins) => {
        this.linkedLogins = logins;
        this.isLoading = false;
      },
      (error) => {
        this.errorMessage = 'Failed to load linked accounts. Please try again.';
        this.isLoading = false;
      }
    );
  }

  isProviderLinked(providerName: string): boolean {
    return this.linkedLogins.some(login =>
      login.loginProvider.toLowerCase() === providerName.toLowerCase()
    );
  }

  getLinkedLogin(providerName: string): IExternalLoginInfo | undefined {
    return this.linkedLogins.find(login =>
      login.loginProvider.toLowerCase() === providerName.toLowerCase()
    );
  }

  linkProvider(provider: IExternalLoginProvider): void {
    const returnUrl = this.router.url;
    this.accountService.initiateLinkExternalLogin(provider.name, returnUrl);
  }

  unlinkProvider(provider: IExternalLoginProvider): void {
    if (this.isUnlinking) {
      return;
    }

    this.isUnlinking = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.accountService.unlinkExternalLogin(provider.name).subscribe(
      (response) => {
        this.successMessage = `${provider.displayName} account has been unlinked successfully.`;
        this.loadLinkedAccounts();
        this.isUnlinking = false;
      },
      (error) => {
        if (error.error?.message) {
          this.errorMessage = error.error.message;
        } else {
          this.errorMessage = `Failed to unlink ${provider.displayName} account. Please try again.`;
        }
        this.isUnlinking = false;
      }
    );
  }
}
