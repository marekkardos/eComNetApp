import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { SocialLoginComponent } from './social-login/social-login.component';
import { ExternalLoginCallbackComponent } from './external-login-callback/external-login-callback.component';
import { LinkedAccountsComponent } from './linked-accounts/linked-accounts.component';
import { AccountRoutingModule } from './account-routing.module';
import { SharedModule } from '../shared/shared.module';

@NgModule({
  declarations: [LoginComponent, RegisterComponent, SocialLoginComponent, ExternalLoginCallbackComponent, LinkedAccountsComponent],
  imports: [
    CommonModule,
    AccountRoutingModule,
    SharedModule
  ],
  exports: [SocialLoginComponent]
})
export class AccountModule { }
