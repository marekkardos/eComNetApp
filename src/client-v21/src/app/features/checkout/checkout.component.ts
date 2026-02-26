import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  afterNextRender,
  ViewChild,
  ElementRef,
  OnInit
} from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { MatStepper, MatStepperModule } from '@angular/material/stepper';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatRadioModule } from '@angular/material/radio';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { firstValueFrom } from 'rxjs';
import { loadStripe, Stripe, StripeCardElement } from '@stripe/stripe-js';
import { ToastrService } from 'ngx-toastr';
import { CheckoutService } from '../../core/services/checkout.service';
import { BasketService } from '../../core/services/basket.service';
import { AccountService } from '../../core/services/account.service';
import { environment } from '../../../environments/environment';
import { DeliveryMethod } from '../../shared/models';

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
    MatDividerModule,
    MatProgressSpinnerModule
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
                <div class="flex justify-between items-center mb-4">
                  <h2 class="text-lg font-semibold text-gray-900">Shipping Address</h2>
                  <button mat-stroked-button
                          (click)="saveUserAddress()"
                          [disabled]="addressForm.invalid || !addressForm.dirty || savingAddress()">
                    <mat-icon>save</mat-icon>
                    Save as default address
                  </button>
                </div>
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
              <div class="mt-6">
                @if (loading()) {
                  <div class="flex justify-center py-8">
                    <mat-spinner diameter="40"></mat-spinner>
                  </div>
                } @else {
                  <form [formGroup]="deliveryForm">
                    <mat-radio-group formControlName="deliveryMethod" class="flex flex-col gap-4">
                      @for (method of deliveryMethods(); track method.id) {
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
                }
              </div>
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
                    @for (item of items(); track item.id; let last = $last) {
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
                  <button mat-raised-button color="primary"
                          (click)="continueToPayment()"
                          [disabled]="preparingPayment()">
                    @if (preparingPayment()) {
                      <mat-spinner diameter="20"></mat-spinner>
                      Preparing Payment...
                    } @else {
                      <ng-container>
                        Continue to Payment
                        <mat-icon>arrow_forward</mat-icon>
                      </ng-container>
                    }
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
                    <div #cardElement class="p-3 border border-gray-300 rounded-lg min-h-[40px]"></div>
                    @if (cardErrors()) {
                      <p class="text-red-600 text-sm mt-2">{{ cardErrors() }}</p>
                    }
                    @if (!stripeReady()) {
                      <p class="text-gray-400 text-sm mt-2">Loading payment form...</p>
                    }
                  </mat-card-content>
                </mat-card>

                <div class="mt-6 flex gap-4">
                  <button mat-stroked-button matStepperPrevious [disabled]="submitting()">
                    <mat-icon>arrow_back</mat-icon>
                    Back
                  </button>
                  <button mat-raised-button color="primary"
                          (click)="placeOrder()"
                          [disabled]="submitting() || !stripeReady()">
                    @if (submitting()) {
                      <mat-spinner diameter="20"></mat-spinner>
                    } @else {
                      <mat-icon>lock</mat-icon>
                    }
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
export class CheckoutComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private checkoutService = inject(CheckoutService);
  private basketService = inject(BasketService);
  private accountService = inject(AccountService);
  private toastr = inject(ToastrService);

  @ViewChild('stepper') stepper!: MatStepper;
  @ViewChild('cardElement') cardElementRef!: ElementRef;

  // Stripe instances (raw refs, not signals)
  private stripe: Stripe | null = null;
  private cardElement: StripeCardElement | null = null;

  // State signals
  deliveryMethods = signal<DeliveryMethod[]>([]);
  loading = signal(true);
  submitting = signal(false);
  stripeReady = signal(false);
  cardErrors = signal('');
  selectedDeliveryMethod = signal<DeliveryMethod | null>(null);
  preparingPayment = signal(false);
  savingAddress = signal(false);

  // Forms
  addressForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    street: ['', Validators.required],
    city: ['', Validators.required],
    state: ['', Validators.required],
    zipCode: ['', Validators.required]
  });

  deliveryForm = this.fb.group({
    deliveryMethod: [null as number | null, Validators.required]
  });

  // Computed from basket service
  readonly items = computed(() => this.basketService.basket()?.items ?? []);
  readonly subtotal = computed(() => this.basketService.totals()?.subtotal ?? 0);
  readonly shipping = computed(() => this.basketService.totals()?.shipping ?? 0);
  readonly total = computed(() => this.basketService.totals()?.total ?? 0);

  constructor() {
    afterNextRender(() => { void this.initStripe(); });
  }

  async ngOnInit(): Promise<void> {
    await Promise.all([this.loadDeliveryMethods(), this.loadUserAddress()]);
    this.loading.set(false);
  }

  private async loadDeliveryMethods(): Promise<void> {
    try {
      const methods = await firstValueFrom(this.checkoutService.getDeliveryMethods());
      this.deliveryMethods.set(methods);
      if (methods.length > 0) {
        this.selectedDeliveryMethod.set(methods[0]);
        this.deliveryForm.patchValue({ deliveryMethod: methods[0].id });
      }
    } catch {
      this.toastr.error('Failed to load delivery methods');
    }
  }

  async saveUserAddress(): Promise<void> {
    this.savingAddress.set(true);
    try {
      const addr = this.addressForm.value;
      await firstValueFrom(this.accountService.updateUserAddress({
        firstName: addr.firstName!,
        lastName: addr.lastName!,
        street: addr.street!,
        city: addr.city!,
        state: addr.state!,
        zipcode: addr.zipCode!
      }));
      this.toastr.success('Address saved');
      this.addressForm.markAsPristine();
      this.addressForm.markAsUntouched();
    } catch {
      this.toastr.error('Failed to save address');
    } finally {
      this.savingAddress.set(false);
    }
  }

  private async loadUserAddress(): Promise<void> {
    try {
      const address = await firstValueFrom(this.accountService.getUserAddress());
      this.addressForm.patchValue({
        firstName: address.firstName,
        lastName: address.lastName,
        street: address.street,
        city: address.city,
        state: address.state,
        zipCode: address.zipcode
      });
    } catch {
      // silent — user may not have a saved address
    }
  }

  private async initStripe(): Promise<void> {
    const stripe = await loadStripe(environment.stripe.publishableKey);
    if (!stripe || !this.cardElementRef?.nativeElement) return;
    this.stripe = stripe;
    const elements = stripe.elements();
    this.cardElement = elements.create('card');
    this.cardElement.mount(this.cardElementRef.nativeElement);
    this.cardElement.on('change', e => this.cardErrors.set(e.error?.message ?? ''));
    this.stripeReady.set(true);
  }

  selectDeliveryMethod(method: DeliveryMethod): void {
    this.selectedDeliveryMethod.set(method);
    this.deliveryForm.patchValue({ deliveryMethod: method.id });
    this.basketService.setShippingPrice(method);
  }

  async continueToPayment(): Promise<void> {
    this.preparingPayment.set(true);
    try {
      await firstValueFrom(this.basketService.createPaymentIntent());
      this.stepper.next();
    } catch {
      this.toastr.error('Could not create payment intent. Please try again.');
    } finally {
      this.preparingPayment.set(false);
    }
  }

  async placeOrder(): Promise<void> {
    if (this.submitting()) return;
    this.submitting.set(true);
    const basket = this.basketService.basket();
    if (!this.stripe || !this.cardElement || !basket?.clientSecret) {
      this.submitting.set(false);
      return;
    }
    try {
      const { error, paymentIntent } = await this.stripe.confirmCardPayment(
        basket.clientSecret,
        { payment_method: { card: this.cardElement } }
      );
      if (error) {
        this.cardErrors.set(error.message ?? 'Payment failed');
        return;
      }
      if (paymentIntent?.status === 'succeeded') {
        const addr = this.addressForm.value;
        const order = await firstValueFrom(this.checkoutService.createOrder({
          basketId: basket.id,
          deliveryMethodId: this.selectedDeliveryMethod()!.id,
          shipToAddress: {
            firstName: addr.firstName!,
            lastName: addr.lastName!,
            street: addr.street!,
            city: addr.city!,
            state: addr.state!,
            zipcode: addr.zipCode!
          }
        }));
        this.basketService.deleteLocalBasket();
        this.router.navigate(['/checkout/success'], { state: { order } });
      }
    } catch {
      this.cardErrors.set('An unexpected error occurred. Please try again.');
    } finally {
      this.submitting.set(false);
    }
  }
}
