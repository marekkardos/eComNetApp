# Client-v21 Project Instructions

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
│   │   ├── guards/            # (Phase 2)
│   │   ├── interceptors/      # (Phase 2)
│   │   └── services/          # (Phase 2)
│   ├── features/
│   │   ├── account/           # Login, register
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

## Key Patterns

### Standalone Components (Angular 21 default)
```typescript
@Component({
  selector: 'app-example',
  standalone: true,
  imports: [CommonModule, MatButtonModule],
  template: `...`
})
export class ExampleComponent {}
```

### Signals for State
```typescript
// Use signals instead of BehaviorSubject
private dataSignal = signal<Data | null>(null);
readonly data = this.dataSignal.asReadonly();
readonly isLoaded = computed(() => !!this.dataSignal());
```

### Dependency Injection
```typescript
// Use inject() instead of constructor injection
private http = inject(HttpClient);
private router = inject(Router);
```

### Control Flow (Angular 17+)
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

### Lazy Loading Routes
```typescript
{
  path: 'shop',
  loadChildren: () => import('./features/shop/shop.routes').then(m => m.SHOP_ROUTES)
}
```

## Development Commands

```bash
npm start           # Dev server (local config) - http://localhost:4201
npm run start:docker # Docker environment
npm run build:prod  # Production build
npm run test        # Run tests
npm run lint        # Lint code
```

## API Configuration

| Environment | API URL |
|-------------|---------|
| Local | `https://localhost:5001/api/` |
| Docker | `http://localhost:44369/api/` |
| Production | Configured in environment.production.ts |

## Migration Documentation

All migration docs are in `doc/client-v21/`:

- **[ANGULAR-21-MIGRATION-PLAN.md](../../doc/client-v21/ANGULAR-21-MIGRATION-PLAN.md)** - Main migration plan (entry point)
- **[PHASE-0-DOCUMENTATION.md](../../doc/client-v21/PHASE-0-DOCUMENTATION.md)** - Angular 9 analysis, API contracts
- **[federated-tickling-dijkstra.md](../../doc/client-v21/federated-tickling-dijkstra.md)** - Static UI implementation details
- **[Phase-4-Feature-Modules.md](../../doc/client-v21/Phase-4-Feature-Modules.md)** - Feature module implementation

## Current Phase: Phase 2 - Core Infrastructure

### What's Next

1. **Core Services with Signals**
   - `AccountService` - Auth, user state, token management
   - `BasketService` - Cart state, totals computation
   - `ShopService` - Products, filtering, pagination

2. **HTTP Interceptors** (functional pattern)
   - `jwtInterceptor` - Attach Bearer token
   - `errorInterceptor` - Handle API errors, redirects
   - `loadingInterceptor` - Show/hide spinner

3. **Guards**
   - `authGuard` - Protect checkout/orders routes

4. **Wire Up Components** - Replace mock data with real API calls

## Coding Conventions

- Use `signal()` and `computed()` for reactive state
- Use `inject()` for dependency injection
- Use `@if`/`@for`/`@switch` control flow (not `*ngIf`/`*ngFor`)
- Standalone components only (no NgModules)
- Lazy load all feature routes
- Keep components in single files when template is small (inline template)
- Use Angular Material for form controls, dialogs, steppers
- Use Tailwind CSS for layout and custom styling
