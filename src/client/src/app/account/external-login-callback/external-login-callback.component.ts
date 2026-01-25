import { Component, OnInit } from '@angular/core';
import { AccountService } from '../account.service';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-external-login-callback',
  templateUrl: './external-login-callback.component.html',
  styleUrls: ['./external-login-callback.component.scss']
})
export class ExternalLoginCallbackComponent implements OnInit {
  isLoading: boolean = true;
  isError: boolean = false;
  errorMessage: string = '';

  constructor(
    private accountService: AccountService,
    private router: Router,
    private activatedRoute: ActivatedRoute
  ) { }

  ngOnInit() {
    this.handleCallback();
  }

  private handleCallback(): void {
    const params = this.activatedRoute.snapshot.queryParams;
    const token = params.token;
    const email = params.email;
    const displayName = params.displayName;
    const returnUrl = params.returnUrl || '/shop';
    const error = params.error;

    // Check for error from OAuth provider
    if (error) {
      this.isLoading = false;
      this.isError = true;
      this.errorMessage = error || 'An error occurred during external login.';
      return;
    }

    // Validate required parameters
    if (!token || !email || !displayName) {
      this.isLoading = false;
      this.isError = true;
      this.errorMessage = 'Invalid callback parameters. Please try again.';
      return;
    }

    // Handle the successful OAuth callback
    try {
      this.accountService.handleExternalLoginCallback(token, email, displayName);
      this.router.navigateByUrl(returnUrl);
    } catch (err) {
      this.isLoading = false;
      this.isError = true;
      this.errorMessage = 'Failed to complete login. Please try again.';
    }
  }

  goToLogin(): void {
    this.router.navigateByUrl('/account/login');
  }
}
