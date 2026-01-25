import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Routes, RouterModule } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { ExternalLoginCallbackComponent } from './external-login-callback/external-login-callback.component';
import { LinkedAccountsComponent } from './linked-accounts/linked-accounts.component';

const routes: Routes = [
  {path: 'login', component: LoginComponent},
  {path: 'register', component: RegisterComponent},
  {path: 'external-login-callback', component: ExternalLoginCallbackComponent},
  {path: 'linked-accounts', component: LinkedAccountsComponent}
];

@NgModule({
  declarations: [],
  imports: [
    RouterModule.forChild(routes)
  ],
  exports: [RouterModule]
})
export class AccountRoutingModule { }
