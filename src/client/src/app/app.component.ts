import { Component, OnInit } from '@angular/core';
import { BasketService } from './basket/basket.service';
import { AccountService } from './account/account.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = 'Skinet';

  constructor(private basketService: BasketService, private accountService: AccountService) { }

  ngOnInit(): void {
    this.initializeApp();
  }

  private initializeApp(): void {
    // Try to restore session from refresh token
    this.accountService.initializeAuth().subscribe({
      next: (user) => {
        if (user) {
          console.log('Session restored for:', user.displayName);
        } else {
          console.log('No active session');
        }
        this.loadBasket();
      },
      error: (error) => {
        console.error('Auth initialization failed:', error);
        this.loadBasket();
      }
    });
  }

  private loadBasket(): void {
    const basketId = localStorage.getItem('basket_id');
    if (basketId) {
      this.basketService.getBasket(basketId).subscribe({
        next: () => console.log('Basket initialized'),
        error: (error) => console.error('Basket load error:', error)
      });
    }
  }
}
