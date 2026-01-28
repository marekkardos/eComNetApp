import { Component, input, output } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BasketItem } from '../../models/basket.model';
import { OrderItem } from '../../models/order.model';

type BasketOrOrderItem = BasketItem | OrderItem;

@Component({
  selector: 'app-basket-summary',
  standalone: true,
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './basket-summary.component.html',
  styleUrl: './basket-summary.component.scss'
})
export class BasketSummaryComponent {
  isBasket = input(true);
  isOrder = input(false);
  items = input.required<BasketOrOrderItem[]>();

  decrement = output<BasketItem>();
  increment = output<BasketItem>();
  remove = output<BasketItem>();

  decrementItemQuantity(item: BasketOrOrderItem): void {
    if (this.isBasketItem(item)) {
      this.decrement.emit(item);
    }
  }

  incrementItemQuantity(item: BasketOrOrderItem): void {
    if (this.isBasketItem(item)) {
      this.increment.emit(item);
    }
  }

  removeBasketItem(item: BasketOrOrderItem): void {
    if (this.isBasketItem(item)) {
      this.remove.emit(item);
    }
  }

  getItemPrice(item: BasketOrOrderItem): number {
    return item.price;
  }

  getItemQuantity(item: BasketOrOrderItem): number {
    return item.quantity;
  }

  getItemTotal(item: BasketOrOrderItem): number {
    return item.price * item.quantity;
  }

  getItemId(item: BasketOrOrderItem): number {
    return (item as OrderItem).productId || (item as BasketItem).id;
  }

  getProductName(item: BasketOrOrderItem): string {
    return item.productName;
  }

  getPictureUrl(item: BasketOrOrderItem): string {
    return item.pictureUrl;
  }

  getType(item: BasketOrOrderItem): string {
    return (item as BasketItem).type || '';
  }

  isBasketItem(item: BasketOrOrderItem): item is BasketItem {
    return this.isBasket();
  }
}
