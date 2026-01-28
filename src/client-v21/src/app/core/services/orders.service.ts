import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Order } from '../../shared/models/order.model';

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  getOrdersForUser(): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.baseUrl}orders`);
  }

  getOrderDetailed(id: number): Observable<Order> {
    return this.http.get<Order>(`${this.baseUrl}orders/${id}`);
  }
}
