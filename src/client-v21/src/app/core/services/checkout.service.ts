import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Order, OrderToCreate } from '../../shared/models/order.model';
import { DeliveryMethod } from '../../shared/models/delivery-method.model';

@Injectable({ providedIn: 'root' })
export class CheckoutService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  createOrder(order: OrderToCreate): Observable<Order> {
    return this.http.post<Order>(`${this.baseUrl}orders`, order);
  }

  getDeliveryMethods(): Observable<DeliveryMethod[]> {
    return this.http.get<DeliveryMethod[]>(`${this.baseUrl}orders/deliveryMethods`).pipe(
      map(dm => dm.sort((a, b) => b.price - a.price))
    );
  }
}
