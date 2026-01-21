import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { AccountService } from 'src/app/account/account.service';
import { map, switchMap, take } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(private accountService: AccountService, private router: Router) {}

  canActivate(
    next: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): Observable<boolean> {
    // Wait for auth initialization to complete before checking user
    return this.accountService.authInitialized$.pipe(
      take(1),
      switchMap(() => this.accountService.currentUser$.pipe(
        take(1),
        map(user => {
          if (user) {
            return true;
          }
          this.router.navigate(['account/login'], {queryParams: {returnUrl: state.url}});
          return false;
        })
      ))
    );
  }
}
