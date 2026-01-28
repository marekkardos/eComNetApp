import { Component, input } from '@angular/core';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-order-totals',
  standalone: true,
  imports: [CurrencyPipe],
  templateUrl: './order-totals.component.html',
  styleUrl: './order-totals.component.scss'
})
export class OrderTotalsComponent {
  shippingPrice = input.required<number>();
  subtotal = input.required<number>();
  total = input.required<number>();
}
