import { Injectable, signal, inject } from '@angular/core';
import { NgxSpinnerService } from 'ngx-spinner';

@Injectable({ providedIn: 'root' })
export class BusyService {
  private spinnerService = inject(NgxSpinnerService);
  private _busyRequestCount = signal(0);

  readonly busyRequestCount = this._busyRequestCount.asReadonly();

  busy(): void {
    this._busyRequestCount.update(count => count + 1);
    this.spinnerService.show(undefined, {
      type: 'ball-spin-clockwise',
      bdColor: 'rgba(255,255,255,0.7)',
      color: '#333333'
    });
  }

  idle(): void {
    this._busyRequestCount.update(count => {
      const newCount = count - 1;
      if (newCount <= 0) {
        this.spinnerService.hide();
        return 0;
      }
      return newCount;
    });
  }
}
