import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { AccountService } from '../account.service';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  loginForm: FormGroup;
  returnUrl: string;
  isLoginError: boolean;
  errorMessage: string;
  isProcessingOAuth: boolean;

  constructor(private accountService: AccountService, private router: Router, private activatedRoute: ActivatedRoute) { }

  ngOnInit() {
    this.returnUrl = this.activatedRoute.snapshot.queryParams.returnUrl || '/shop';
    this.createLoginForm();
    this.handleOAuthCallback();
  }

  createLoginForm() {
    this.loginForm = new FormGroup({
      email: new FormControl('', [Validators.required, Validators
        .pattern('^[\\w-\\.]+@([\\w-]+\\.)+[\\w-]{2,4}$')]),
      password: new FormControl('', Validators.required)
    });
  }

  onSubmit() {
    this.accountService.login(this.loginForm.value).subscribe(() => {
      this.router.navigateByUrl(this.returnUrl);
    }, error => {
      console.log(error);
      this.isLoginError = true;
      this.errorMessage = 'Login or Password is not correct.';
    });
  }

  onGoogleLogin() {
    // Store the return URL before redirecting to OAuth
    const currentUrl = window.location.origin + '/account/login?returnUrl=' + encodeURIComponent(this.returnUrl);
    this.accountService.initiateGoogleLogin(currentUrl);
  }

  private handleOAuthCallback() {
    const params = this.activatedRoute.snapshot.queryParams;

    // Check for OAuth error
    if (params.error) {
      this.isLoginError = true;
      this.errorMessage = params.error_description || 'Authentication failed. Please try again.';
      // Clean up URL
      this.router.navigate([], {
        relativeTo: this.activatedRoute,
        queryParams: { returnUrl: this.returnUrl },
        replaceUrl: true
      });
      return;
    }

    // Check for OAuth authorization code
    if (params.code) {
      this.isProcessingOAuth = true;
      this.accountService.exchangeAuthCode(params.code).subscribe(
        () => {
          this.isProcessingOAuth = false;
          this.router.navigateByUrl(this.returnUrl);
        },
        error => {
          this.isProcessingOAuth = false;
          this.isLoginError = true;
          this.errorMessage = 'Failed to complete authentication. Please try again.';
          console.error('OAuth code exchange failed:', error);
          // Clean up URL
          this.router.navigate([], {
            relativeTo: this.activatedRoute,
            queryParams: { returnUrl: this.returnUrl },
            replaceUrl: true
          });
        }
      );
    }
  }
}
