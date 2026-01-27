# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Angular 21 e-commerce client (Skishop) - a complete rewrite of the Angular 9 client using modern Angular patterns.

**Migration Status**: Phase 0 and 1 complete, Phase 2 (Core Infrastructure) is next.

## Tech Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| Angular | 21.1.0 | Framework |
| Angular Material | 21.1.1 | UI components (forms, dialogs, stepper) |
| Tailwind CSS | 4.x | Utility-first styling |
| TypeScript | 5.9.x | Language |
| RxJS | 7.8.x | Reactive programming |
| ngx-toastr | 20.x | Toast notifications |
| ngx-spinner | 21.x | Loading spinners |
| xng-breadcrumb | 14.x | Breadcrumb navigation |
| Stripe.js | 8.x | Payment processing |

## Development Commands

```bash
# Development server (uses environment.local.ts)
npm start

# Development server with docker environment (uses environment.docker.ts)
npm run start:docker

# Development server with generic development config
npm run start:dev

# Production build
npm run build:prod

# Development build with watch mode
npm run watch

# Run tests
npm test

# Lint code
npm run lint
```

**Important**: The app runs on **port 4201** (not the default 4200) to avoid conflicts with the Angular 9 client.

## Project Structure

```
src/
├── app/
│   ├── app.ts                 # Root component
│   ├── app.config.ts          # Providers (router, http, animations)
│   ├── app.routes.ts          # Lazy-loaded routes
│   ├── core/
│   │   ├── components/        # not-found, server-error
│   │   ├── layouts/           # auth-layout, main-layout
│   │   ├── guards/            # (Phase 2 - not yet implemented)
│   │   ├── interceptors/      # (Phase 2 - not yet implemented)
│   │   └── services/          # (Phase 2 - not yet implemented)
│   ├── features/
│   │   ├── account/           # Login, register with auth-layout
│   │   ├── basket/            # Shopping cart
│   │   ├── checkout/          # Checkout flow with stepper
│   │   ├── home/              # Landing page
│   │   ├── orders/            # Order history
│   │   └── shop/              # Product listing & details
│   └── shared/
│       ├── models/            # TypeScript interfaces
│       └── mock-data/         # Mock data (Phase 1)
└── environments/              # local, docker, production configs
```

## Key Angular 21 Patterns

### 1. Standalone Components (Default)
All components use `standalone: true` and import dependencies directly:

```typescript
@Component({
  selector: 'app-example',
  standalone: true,
  imports: [CommonModule, MatButtonModule],
  template: `...`
})
export class ExampleComponent {}
```

### 2. Signals for Reactive State
Use signals instead of BehaviorSubject for state management:

```typescript
// Define signals
private dataSignal = signal<Data | null>(null);
readonly data = this.dataSignal.asReadonly();

// Computed signals
readonly isLoaded = computed(() => !!this.dataSignal());
readonly itemCount = computed(() => this.data()?.items.length ?? 0);

// Update signals
this.dataSignal.set(newValue);
this.dataSignal.update(prev => ({ ...prev, updated: true }));
```

### 3. Dependency Injection with inject()
Use `inject()` instead of constructor injection:

```typescript
export class ExampleComponent {
  private http = inject(HttpClient);
  private router = inject(Router);

  // Services can be injected anywhere in the class body
}
```

### 4. Modern Control Flow
Use `@if`, `@for`, `@switch` instead of structural directives:

```html
@if (condition) {
  <div>Content</div>
}

@for (item of items(); track item.id) {
  <div>{{ item.name }}</div>
}

@switch (status) {
  @case ('loading') { <spinner /> }
  @case ('error') { <error-message /> }
  @default { <content /> }
}
```

### 5. Lazy Loading with Route Arrays
Feature routes are exported as constants and lazy-loaded:

```typescript
// shop.routes.ts
export const SHOP_ROUTES: Routes = [
  { path: '', component: ShopComponent },
  { path: ':id', component: ProductDetailsComponent }
];

// app.routes.ts
{
  path: 'shop',
  loadChildren: () => import('./features/shop/shop.routes').then(m => m.SHOP_ROUTES)
}
```

### 6. Inline Templates
Components with modest templates keep template inline rather than in separate .html files:

```typescript
@Component({
  selector: 'app-example',
  template: `
    <div class="container">
      <!-- Template content here -->
    </div>
  `
})
```

## Styling Architecture

### Tailwind CSS
Used for layout, spacing, and custom UI:

```html
<div class="max-w-7xl mx-auto px-4 py-8">
  <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-6">
    <!-- Grid items -->
  </div>
</div>
```

### Angular Material
Used for form controls, cards, buttons, and dialogs:

```typescript
imports: [
  MatFormFieldModule,
  MatInputModule,
  MatButtonModule,
  MatCardModule,
  MatPaginatorModule
]
```

### Custom Theme Colors
Primary color (teal) is defined in Tailwind config and accessed via CSS variables:

```html
<p class="font-bold text-lg" style="color: var(--color-primary-600)">
```

## Environment Configuration

Three environment files are used:

| File | Purpose | API URL | Usage |
|------|---------|---------|-------|
| `environment.local.ts` | Local development | `https://localhost:5001/api/` | Default (`npm start`) |
| `environment.docker.ts` | Docker containers | `http://localhost:44369/api/` | `npm run start:docker` |
| `environment.production.ts` | Production build | TBD | `npm run build:prod` |

**Note**: `environment.ts` is a placeholder; actual configs are loaded via file replacement in `angular.json`.

## Current Implementation State (Phase 1 Complete)

### ✅ Implemented
- All UI components with static layouts
- Mock data for products, basket, orders, users
- Routing with lazy-loaded feature modules
- Two layouts: `main-layout` (with navbar) and `auth-layout` (centered card)
- Responsive design (mobile, tablet, desktop)
- Angular Material components for forms and controls
- Tailwind CSS for layout and styling

### ⏳ Not Yet Implemented (Phase 2)
- Core services (`AccountService`, `BasketService`, `ShopService`)
- HTTP interceptors (JWT, error handling, loading)
- Route guards (`authGuard`)
- Real API integration (currently using mock data)
- Token-based authentication flow
- Stripe payment integration

## Migration Documentation

All migration docs are in `../../doc/client-v21/`:

- **[ANGULAR-21-MIGRATION-PLAN.md](../../doc/client-v21/ANGULAR-21-MIGRATION-PLAN.md)** - Main migration plan with all phases
- **[PHASE-0-DOCUMENTATION.md](../../doc/client-v21/PHASE-0-DOCUMENTATION.md)** - Angular 9 analysis, API contracts
- **[federated-tickling-dijkstra.md](../../doc/client-v21/federated-tickling-dijkstra.md)** - Phase 1 static UI implementation details
- **[Phase-4-Feature-Modules.md](../../doc/client-v21/Phase-4-Feature-Modules.md)** - Feature module implementation plan

## Coding Conventions

### Component Structure
```typescript
@Component({
  selector: 'app-example',
  imports: [/* dependencies */],
  changeDetection: ChangeDetectionStrategy.OnPush,  // Use OnPush
  template: `...`
})
export class ExampleComponent {
  // 1. Injected services
  private http = inject(HttpClient);

  // 2. Signal state
  private dataSignal = signal<Data | null>(null);

  // 3. Public computed signals
  readonly data = this.dataSignal.asReadonly();
  readonly count = computed(() => this.data()?.length ?? 0);

  // 4. Methods
  loadData(): void {
    // Implementation
  }
}
```

### Change Detection
Always use `OnPush` change detection strategy:

```typescript
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  // ...
})
```

### Service Pattern
```typescript
@Injectable({ providedIn: 'root' })
export class ExampleService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  // Private writable signals
  private dataSignal = signal<Data | null>(null);

  // Public readonly signals
  readonly data = this.dataSignal.asReadonly();

  // Computed signals
  readonly isLoaded = computed(() => !!this.dataSignal());

  // Async methods
  async loadData(): Promise<void> {
    const result = await firstValueFrom(
      this.http.get<Data>(`${this.baseUrl}endpoint`)
    );
    this.dataSignal.set(result);
  }
}
```

### Type Safety
- All models are defined in `src/app/shared/models/`
- Use TypeScript interfaces for all data structures
- Export models from a barrel file: `src/app/shared/models/index.ts`
- Strict TypeScript mode is enabled in `tsconfig.json`

### Testing Configuration
- Test framework: **Vitest** (as per package.json and README)
- Tests are disabled by default in schematics (`skipTests: true`)
- Test files use `.spec.ts` extension

## Common Tasks

### Generate a New Component
```bash
# Generate in feature module
ng generate component features/example/my-component

# Generate in shared
ng generate component shared/components/my-component
```

Components are generated as standalone by default with SCSS styles.

### Generate a New Service
```bash
# Generate in core services
ng generate service core/services/example

# Generate in feature
ng generate service features/shop/services/example
```

### Add a New Feature Module
1. Create feature directory under `src/app/features/`
2. Create `feature-name.routes.ts` with route array
3. Add lazy-loaded route in `app.routes.ts`
4. Create components and services as needed

## Important Notes

- **No NgModules**: This is a fully standalone application
- **Port 4201**: Development server uses port 4201 (configured in angular.json)
- **Mock Data**: All data is currently mocked in `shared/mock-data/`
- **Zoneless**: App uses `provideZonelessChangeDetection()` for better performance
- **Phase-based Development**: Follow the migration plan phases; don't skip ahead
- **API Parity**: API contracts from Angular 9 app must be preserved (see Phase 0 docs)
