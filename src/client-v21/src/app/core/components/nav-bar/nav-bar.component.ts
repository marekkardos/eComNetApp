import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AccountService } from '../../services/account.service';
import { BasketService } from '../../services/basket.service';

@Component({
  selector: 'app-nav-bar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.scss'
})
export class NavBarComponent {
  accountService = inject(AccountService);
  basketService = inject(BasketService);

  logout(): void {
    this.accountService.logout().subscribe();
  }
}
