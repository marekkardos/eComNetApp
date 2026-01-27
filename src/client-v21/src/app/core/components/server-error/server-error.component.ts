import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-server-error',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-50">
      <div class="text-center max-w-2xl px-4">
        <h1 class="text-6xl font-bold text-red-300 mb-4">500</h1>
        <h2 class="text-2xl font-semibold text-gray-700 mb-4">Server Error</h2>
        <p class="text-gray-600 mb-8">Something went wrong on our end. Please try again later.</p>
        @if (error) {
          <div class="bg-red-50 border border-red-200 rounded-lg p-4 text-left">
            <h3 class="font-semibold text-red-800 mb-2">{{ error.message }}</h3>
            @if (error.details) {
              <pre class="text-sm text-red-700 overflow-auto">{{ error.details }}</pre>
            }
          </div>
        }
      </div>
    </div>
  `
})
export class ServerErrorComponent {
  private router = inject(Router);
  error: { message: string; details?: string } | null = null;

  constructor() {
    const navigation = this.router.getCurrentNavigation();
    if (navigation?.extras?.state) {
      this.error = navigation.extras.state as { message: string; details?: string };
    }
  }
}
