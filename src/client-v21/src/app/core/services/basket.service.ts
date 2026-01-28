import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Basket, BasketItem, BasketTotals, DeliveryMethod } from '../../shared/models';
import { Product } from '../../shared/models/product.model';

@Injectable({ providedIn: 'root' })
export class BasketService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  private basketSignal = signal<Basket | null>(null);
  private basketTotalSignal = signal<BasketTotals | null>(null);
  private shippingPrice = signal(0);

  readonly basket = this.basketSignal.asReadonly();
  readonly totals = this.basketTotalSignal.asReadonly();
  readonly itemCount = computed(() =>
    this.basketSignal()?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0
  );

  createPaymentIntent(): Observable<void> {
    const basketId = this.getCurrentBasketValue().id;
    return this.http.post<Basket>(`${this.baseUrl}payments/${basketId}`, {}).pipe(
      tap(basket => {
        this.basketSignal.set(basket);
      }),
      map(() => undefined)
    );
  }

  setShippingPrice(deliveryMethod: DeliveryMethod): void {
    this.shippingPrice.set(deliveryMethod.price);
    const basket = this.getCurrentBasketValue();
    if (basket) {
      basket.deliveryMethodId = deliveryMethod.id;
      basket.shippingPrice = deliveryMethod.price;
      this.calculateTotals();
      this.setBasket(basket);
    }
  }

  getBasket(id: string): Observable<void> {
    return this.http.get<Basket>(`${this.baseUrl}basket/${id}`).pipe(
      tap(basket => {
        this.basketSignal.set(basket);
        this.shippingPrice.set(basket.shippingPrice ?? 0);
        this.calculateTotals();
      }),
      map(() => undefined)
    );
  }

  setBasket(basket: Basket): void {
    this.http.post<Basket>(`${this.baseUrl}basket`, basket).subscribe(
      response => {
        this.basketSignal.set(response);
        this.calculateTotals();
      },
      error => console.log(error)
    );
  }

  getCurrentBasketValue(): Basket {
    return this.basketSignal() ?? this.createBasket();
  }

  addItemToBasket(item: Product, quantity = 1): void {
    const itemToAdd: BasketItem = this.mapProductItemToBasketItem(item, quantity);
    const basket = this.getCurrentBasketValue();
    basket.items = this.addOrUpdateItem(basket.items, itemToAdd, quantity);
    this.setBasket(basket);
  }

  incrementItemQuantity(item: BasketItem): void {
    const basket = this.getCurrentBasketValue();
    const foundItemIndex = basket.items.findIndex(x => x.id === item.id);
    if (foundItemIndex !== -1) {
      basket.items[foundItemIndex].quantity++;
      this.setBasket(basket);
    }
  }

  decrementItemQuantity(item: BasketItem): void {
    const basket = this.getCurrentBasketValue();
    const foundItemIndex = basket.items.findIndex(x => x.id === item.id);
    if (foundItemIndex !== -1) {
      if (basket.items[foundItemIndex].quantity > 1) {
        basket.items[foundItemIndex].quantity--;
        this.setBasket(basket);
      } else {
        this.removeItemFromBasket(item);
      }
    }
  }

  removeItemFromBasket(item: BasketItem): void {
    const basket = this.getCurrentBasketValue();
    if (basket.items.some(x => x.id === item.id)) {
      basket.items = basket.items.filter(i => i.id !== item.id);
      if (basket.items.length > 0) {
        this.setBasket(basket);
      } else {
        this.deleteBasket(basket);
      }
    }
  }

  deleteLocalBasket(): void {
    this.basketSignal.set(null);
    this.basketTotalSignal.set(null);
    localStorage.removeItem('basket_id');
  }

  deleteBasket(basket: Basket): void {
    this.http.delete(`${this.baseUrl}basket/${basket.id}`).subscribe(() => {
      this.basketSignal.set(null);
      this.basketTotalSignal.set(null);
      localStorage.removeItem('basket_id');
    }, error => console.log(error));
  }

  private calculateTotals(): void {
    const basket = this.getCurrentBasketValue();
    const shipping = this.shippingPrice();
    const subtotal = basket.items.reduce((a, b) => b.price * b.quantity + a, 0);
    const total = subtotal + shipping;
    this.basketTotalSignal.set({ shipping, total, subtotal });
  }

  private addOrUpdateItem(items: BasketItem[], itemToAdd: BasketItem, quantity: number): BasketItem[] {
    const index = items.findIndex(i => i.id === itemToAdd.id);
    if (index === -1) {
      itemToAdd.quantity = quantity;
      items.push(itemToAdd);
    } else {
      items[index].quantity += quantity;
    }
    return items;
  }

  private createBasket(): Basket {
    const basket: Basket = {
      id: crypto.randomUUID(),
      items: []
    };
    localStorage.setItem('basket_id', basket.id);
    return basket;
  }

  private mapProductItemToBasketItem(item: Product, quantity: number): BasketItem {
    return {
      id: item.id,
      productName: item.name,
      price: item.price,
      pictureUrl: item.pictureUrl,
      quantity,
      brand: item.productBrand,
      type: item.productType
    };
  }
}
