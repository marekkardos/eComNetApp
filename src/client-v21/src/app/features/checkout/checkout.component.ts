import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { MatStepperModule } from '@angular/material/stepper';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatRadioModule } from '@angular/material/radio';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MOCK_BASKET_ITEMS, MOCK_DELIVERY_METHODS, MOCK_ADDRESS } from '../../shared/mock-data';
import { BasketItem, DeliveryMethod } from '../../shared/models';

@Component({
  selector: 'app-checkout',
  imports: [
    ReactiveFormsModule,
    CurrencyPipe,
    MatStepperModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatRadioModule,
    MatCardModule,
    MatDividerModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-5xl mx-auto px-4 py-8">
      <h1 class="text-3xl font-bold text-gray-900 mb-8">Checkout</h1>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <!-- Stepper -->
        <div class="lg:col-span-2">
          <mat-stepper [linear]="true" #stepper>
            <!-- Address Step -->
            <mat-step [stepControl]="addressForm" label="Shipping Address">
              <form [formGroup]="addressForm" class="mt-6">
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <mat-form-field appearance="outline" class="w-full">
                    <mat-label>First Name</mat-label>
                    <input matInput formControlName="firstName" autocomplete="given-name">
                    @if (addressForm.get('firstName')?.hasError('required') && addressForm.get('firstName')?.touched) {
                      <mat-error>First name is required</mat-error>
                    }
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="w-full">
                    <mat-label>Last Name</mat-label>
                    <input matInput formControlName="lastName" autocomplete="family-name">
                    @if (addressForm.get('lastName')?.hasError('required') && addressForm.get('lastName')?.touched) {
                      <mat-error>Last name is required</mat-error>
                    }
                  </mat-form-field>
                </div>

                <mat-form-field appearance="outline" class="w-full">
                  <mat-label>Street Address</mat-label>
                  <input matInput formControlName="street" autocomplete="street-address">
                  @if (addressForm.get('street')?.hasError('required') && addressForm.get('street')?.touched) {
                    <mat-error>Street address is required</mat-error>
                  }
                </mat-form-field>

                <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
                  <mat-form-field appearance="outline" class="w-full">
                    <mat-label>City</mat-label>
                    <input matInput formControlName="city" autocomplete="address-level2">
                    @if (addressForm.get('city')?.hasError('required') && addressForm.get('city')?.touched) {
                      <mat-error>City is required</mat-error>
                    }
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="w-full">
                    <mat-label>State</mat-label>
                    <input matInput formControlName="state" autocomplete="address-level1">
                    @if (addressForm.get('state')?.hasError('required') && addressForm.get('state')?.touched) {
                      <mat-error>State is required</mat-error>
                    }
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="w-full">
                    <mat-label>Zip Code</mat-label>
                    <input matInput formControlName="zipCode" autocomplete="postal-code">
                    @if (addressForm.get('zipCode')?.hasError('required') && addressForm.get('zipCode')?.touched) {
                      <mat-error>Zip code is required</mat-error>
                    }
                  </mat-form-field>
                </div>

                <div class="mt-6">
                  <button mat-raised-button color="primary" matStepperNext [disabled]="addressForm.invalid">
                    Continue to Delivery
                    <mat-icon>arrow_forward</mat-icon>
                  </button>
                </div>
              </form>
            </mat-step>

            <!-- Delivery Step -->
            <mat-step [stepControl]="deliveryForm" label="Delivery Method">
              <form [formGroup]="deliveryForm" class="mt-6">
                <mat-radio-group formControlName="deliveryMethod" class="flex flex-col gap-4">
                  @for (method of deliveryMethods; track method.id) {
                    <mat-card class="cursor-pointer hover:shadow-md transition-shadow"
                              [class.ring-2]="selectedDeliveryMethod()?.id === method.id"
                              [style.--tw-ring-color]="'var(--color-primary-500)'"
                              (click)="selectDeliveryMethod(method)">
                      <mat-card-content class="flex items-center gap-4 p-4">
                        <mat-radio-button [value]="method.id"></mat-radio-button>
                        <div class="flex-1">
                          <h3 class="font-semibold text-gray-900">{{ method.shortName }}</h3>
                          <p class="text-sm text-gray-500">{{ method.description }}</p>
                          <p class="text-sm text-gray-500">{{ method.deliveryTime }}</p>
                        </div>
                        <span class="font-bold" style="color: var(--color-primary-600)">
                          {{ method.price | currency }}
                        </span>
                      </mat-card-content>
                    </mat-card>
                  }
                </mat-radio-group>

                <div class="mt-6 flex gap-4">
                  <button mat-stroked-button matStepperPrevious>
                    <mat-icon>arrow_back</mat-icon>
                    Back
                  </button>
                  <button mat-raised-button color="primary" matStepperNext [disabled]="deliveryForm.invalid">
                    Continue to Review
                    <mat-icon>arrow_forward</mat-icon>
                  </button>
                </div>
              </form>
            </mat-step>

            <!-- Review Step -->
            <mat-step label="Review Order">
              <div class="mt-6">
                <!-- Address Summary -->
                <mat-card class="mb-4">
                  <mat-card-header>
                    <mat-card-title class="text-lg">Shipping Address</mat-card-title>
                  </mat-card-header>
                  <mat-card-content>
                    <p class="text-gray-700">{{ addressForm.value.firstName }} {{ addressForm.value.lastName }}</p>
                    <p class="text-gray-600">{{ addressForm.value.street }}</p>
                    <p class="text-gray-600">{{ addressForm.value.city }}, {{ addressForm.value.state }} {{ addressForm.value.zipCode }}</p>
                  </mat-card-content>
                </mat-card>

                <!-- Delivery Summary -->
                <mat-card class="mb-4">
                  <mat-card-header>
                    <mat-card-title class="text-lg">Delivery Method</mat-card-title>
                  </mat-card-header>
                  <mat-card-content>
                    @if (selectedDeliveryMethod(); as method) {
                      <p class="text-gray-700 font-medium">{{ method.shortName }}</p>
                      <p class="text-gray-600">{{ method.deliveryTime }}</p>
                    }
                  </mat-card-content>
                </mat-card>

                <!-- Items Summary -->
                <mat-card>
                  <mat-card-header>
                    <mat-card-title class="text-lg">Order Items</mat-card-title>
                  </mat-card-header>
                  <mat-card-content>
                    @for (item of items; track item.id; let last = $last) {
                      <div class="flex items-center gap-4 py-3">
                        <img [src]="item.pictureUrl" [alt]="item.productName" class="w-16 h-16 object-cover rounded">
                        <div class="flex-1">
                          <p class="font-medium text-gray-900">{{ item.productName }}</p>
                          <p class="text-sm text-gray-500">Qty: {{ item.quantity }}</p>
                        </div>
                        <span class="font-medium">{{ item.price * item.quantity | currency }}</span>
                      </div>
                      @if (!last) {
                        <mat-divider></mat-divider>
                      }
                    }
                  </mat-card-content>
                </mat-card>

                <div class="mt-6 flex gap-4">
                  <button mat-stroked-button matStepperPrevious>
                    <mat-icon>arrow_back</mat-icon>
                    Back
                  </button>
                  <button mat-raised-button color="primary" matStepperNext>
                    Continue to Payment
                    <mat-icon>arrow_forward</mat-icon>
                  </button>
                </div>
              </div>
            </mat-step>

            <!-- Payment Step -->
            <mat-step label="Payment">
              <div class="mt-6">
                <mat-card>
                  <mat-card-header>
                    <mat-card-title class="text-lg">Payment Details</mat-card-title>
                  </mat-card-header>
                  <mat-card-content>
                    <!-- Mock payment form - Stripe will be integrated in Phase 2 -->
                    <div class="p-4 bg-gray-100 rounded-lg mb-4">
                      <p class="text-sm text-gray-600 mb-4">
                        <mat-icon class="text-blue-500 align-middle mr-1">info</mat-icon>
                        Payment integration will be connected in Phase 2. This is a mock checkout.
                      </p>

                      <mat-form-field appearance="outline" class="w-full">
                        <mat-label>Card Number</mat-label>
                        <input matInput placeholder="4242 4242 4242 4242" disabled>
                        <mat-icon matSuffix>credit_card</mat-icon>
                      </mat-form-field>

                      <div class="grid grid-cols-2 gap-4">
                        <mat-form-field appearance="outline" class="w-full">
                          <mat-label>Expiry</mat-label>
                          <input matInput placeholder="12/28" disabled>
                        </mat-form-field>
                        <mat-form-field appearance="outline" class="w-full">
                          <mat-label>CVC</mat-label>
                          <input matInput placeholder="123" disabled>
                        </mat-form-field>
                      </div>
                    </div>
                  </mat-card-content>
                </mat-card>

                <div class="mt-6 flex gap-4">
                  <button mat-stroked-button matStepperPrevious>
                    <mat-icon>arrow_back</mat-icon>
                    Back
                  </button>
                  <button mat-raised-button color="primary" (click)="placeOrder()">
                    <mat-icon>lock</mat-icon>
                    Place Order ({{ total() | currency }})
                  </button>
                </div>
              </div>
            </mat-step>
          </mat-stepper>
        </div>

        <!-- Order Summary Sidebar -->
        <div class="lg:col-span-1">
          <mat-card class="sticky top-24">
            <mat-card-header>
              <mat-card-title>Order Summary</mat-card-title>
            </mat-card-header>
            <mat-card-content class="pt-4">
              <div class="space-y-3">
                <div class="flex justify-between">
                  <span class="text-gray-600">Subtotal</span>
                  <span class="font-medium">{{ subtotal() | currency }}</span>
                </div>

                <div class="flex justify-between">
                  <span class="text-gray-600">Shipping</span>
                  <span class="font-medium">{{ shipping() | currency }}</span>
                </div>

                <mat-divider></mat-divider>

                <div class="flex justify-between text-lg">
                  <span class="font-semibold text-gray-900">Total</span>
                  <span class="font-bold" style="color: var(--color-primary-600)">{{ total() | currency }}</span>
                </div>
              </div>
            </mat-card-content>
          </mat-card>
        </div>
      </div>
    </div>
  `
})
export class CheckoutComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);

  // Mock data
  items: BasketItem[] = MOCK_BASKET_ITEMS;
  deliveryMethods: DeliveryMethod[] = MOCK_DELIVERY_METHODS;

  // Forms - pre-populate with mock address
  addressForm = this.fb.group({
    firstName: [MOCK_ADDRESS.firstName, Validators.required],
    lastName: [MOCK_ADDRESS.lastName, Validators.required],
    street: [MOCK_ADDRESS.street, Validators.required],
    city: [MOCK_ADDRESS.city, Validators.required],
    state: [MOCK_ADDRESS.state, Validators.required],
    zipCode: [MOCK_ADDRESS.zipcode, Validators.required]
  });

  deliveryForm = this.fb.group({
    deliveryMethod: [null as number | null, Validators.required]
  });

  // Signals - select first delivery method by default
  selectedDeliveryMethod = signal<DeliveryMethod | null>(
    this.deliveryMethods.length > 0 ? this.deliveryMethods[0] : null
  );

  constructor() {
    // Initialize delivery form with default selection
    if (this.deliveryMethods.length > 0) {
      this.deliveryForm.patchValue({ deliveryMethod: this.deliveryMethods[0].id });
    }
  }

  // Computed values
  subtotal = computed(() =>
    this.items.reduce((sum, item) => sum + (item.price * item.quantity), 0)
  );

  shipping = computed(() =>
    this.selectedDeliveryMethod()?.price || 0
  );

  total = computed(() => this.subtotal() + this.shipping());

  selectDeliveryMethod(method: DeliveryMethod): void {
    this.selectedDeliveryMethod.set(method);
    this.deliveryForm.patchValue({ deliveryMethod: method.id });
  }

  placeOrder(): void {
    // Mock order placement - navigate to success page
    console.log('Order placed!', {
      address: this.addressForm.value,
      deliveryMethod: this.selectedDeliveryMethod(),
      items: this.items,
      total: this.total()
    });

    // Navigate to orders page (in real implementation, this would go to order confirmation)
    this.router.navigateByUrl('/orders');
  }
}
