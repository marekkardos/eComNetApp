import { Component, inject, NgZone, forwardRef } from '@angular/core';
import { NgControl, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-text-input',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TextInputComponent),
      multi: true
    }
  ],
  templateUrl: './text-input.component.html',
  styleUrl: './text-input.component.scss'
})
export class TextInputComponent {
  ngZone = inject(NgZone);

  type = 'text';
  label = '';

  // eslint-disable-next-line @typescript-eslint/no-unused-vars, @typescript-eslint/no-empty-function
  private _onChangeValue = (_value: string): void => {};
  // eslint-disable-next-line @typescript-eslint/no-empty-function
  private _onTouchedValue = (): void => {};

  ngControl = inject(NgControl, { self: true });

  constructor() {
    this.ngControl.valueAccessor = this;
  }

  get control() {
    return this.ngControl.control;
  }

  get isValid(): boolean {
    return !this.control || !this.control.touched || this.control.valid === true;
  }

  get isInvalid(): boolean {
    return !!(this.control && this.control.touched && !this.control.valid);
  }

  get isPending(): boolean {
    return this.control?.status === 'PENDING';
  }

  get errors(): Record<string, unknown> | null {
    return this.control ? this.control.errors : null;
  }

  onInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this._onChangeValue(target.value);
  }

  onBlur(): void {
    this._onTouchedValue();
  }

  writeValue(value: string): void {
    const inputElement = document.getElementById(this.label) as HTMLInputElement;
    if (inputElement) {
      this.ngZone.runOutsideAngular(() => {
        inputElement.value = value || '';
      });
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this._onChangeValue = fn;
  }

  registerOnTouched(fn: () => void): void {
    this._onTouchedValue = fn;
  }
}
