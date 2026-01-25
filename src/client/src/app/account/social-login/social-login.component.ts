import { Component, OnInit, Input } from '@angular/core';
import { AccountService } from '../account.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-social-login',
  templateUrl: './social-login.component.html',
  styleUrls: ['./social-login.component.scss']
})
export class SocialLoginComponent implements OnInit {
  @Input() returnUrl: string = '/shop';
  isLoading: boolean = false;
  errorMessage: string = '';

  constructor(private accountService: AccountService, private activatedRoute: ActivatedRoute) { }

  ngOnInit() {
    // Use returnUrl from query params if available, otherwise use input or default
    const queryReturnUrl = this.activatedRoute.snapshot.queryParams.returnUrl;
    if (queryReturnUrl) {
      this.returnUrl = queryReturnUrl;
    }
  }

  loginWithGoogle(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.accountService.initiateExternalLogin('Google', this.returnUrl);
  }

}
