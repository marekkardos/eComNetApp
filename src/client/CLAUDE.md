# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the Angular frontend application.

## Project Overview

This is the Angular 9 frontend for eComNetApp, an e-commerce application. The frontend communicates with a .NET 8 REST API backend located in `../API/`.

**For local development setup and workflows, see the main [README.md](../API/README.md)** in the project root.

## Architecture

### Frontend Structure (Angular 9)

Feature-based module structure:

- `src/app/account/` - User authentication and account management
- `src/app/basket/` - Shopping basket/cart
- `src/app/checkout/` - Checkout flow
- `src/app/orders/` - Order history and details
- `src/app/shop/` - Product catalog and browsing
- `src/app/core/` - Singleton services, guards, interceptors, nav components
- `src/app/shared/` - Reusable components and utilities

### Technology Stack

- **Framework**: Angular 9, rxjs 6.5.4
- **UI**: Bootstrap 4, ngx-bootstrap, ngx-toastr
- **Build**: Angular CLI with webpack
- **Node**: Requires Node.js 12.x (use nvm to manage versions)

## Common Commands

### Development

Install dependencies:
```bash
npm install
```

Run Angular dev server (local configuration - API at https://localhost:5001):
```bash
npm start
# or: ng serve --configuration=local
```

Run in container mode (API at https://localhost:44370):
```bash
npm run start:container
```

Build for production:
```bash
npm run build:prod
```

Build for test environment:
```bash
npm run build:test
```

Run tests:
```bash
npm test
```

Lint:
```bash
npm run lint
```

## Environment Configuration

Multiple environment configurations in `src/environments/` for different development scenarios:

- `environment.ts` - Default (API at https://localhost:44370)
- `environment.local.ts` - Local full-stack development (API at https://localhost:5001)
- `environment.container.ts` - Running in Docker container
- `environment.stage.ts` - Staging environment (used for production builds)

**Key Configuration Values:**
- `apiUrl` - Backend API base URL
- `stripePublishableKey` - Stripe frontend publishable key for payment processing

See main [README.md](../API/README.md) for detailed setup and usage of each environment.

## Important Implementation Details

### Authentication Flow (Client-Side)

The Angular app implements a secure authentication pattern using in-memory token storage:

**Components:**
- `account.service.ts` - In-memory access token storage, login/register/logout methods
- `jwt.interceptor.ts` - Automatic token refresh on 401 errors with request retry
- Refresh tokens stored in HttpOnly cookies (set by API, sent automatically)

More details in the [AuthFlow.md](../../doc/Authentication/AuthFlow.md)

Google social login was added. 
More details in the [GoogleAuthFlow.md](../../doc/Social%20Login%20Integration/GoogleAuthFlow.md)

### Stripe Payment Integration

Payment processing uses Stripe.js library with Payment Intents:

**Flow:**
1. Get Payment Intent client secret from API: `POST /api/payments/{basketId}`
2. Use Stripe.js to confirm card payment with the client secret
3. Handle payment confirmation in UI
4. Order status updates happen server-side via webhooks (not in Angular)

**Configuration:**
- Frontend publishable key configured in environment files (`stripePublishableKey`)
- Stripe.js loaded from CDN

**Important:**
- Payment confirmation happens client-side with Stripe.js
- Order status updates are webhook-based (backend responsibility)
- Never send sensitive card data directly to the API

More details in the [STRIPE_DEVELOPMENT.md](../../doc/Stripe/STRIPE_DEVELOPMENT.md)

## Development Workflow Notes

### Port Mappings

| Service | Port | URL |
|---------|------|-----|
| Angular | 4200 | http://localhost:4200 |
| API (Docker) | 44370 | https://localhost:44370 |
| API (Local) | 5001 | https://localhost:5001 |

### Docker Development

When running Angular in Docker:
```bash
# From project root
docker-compose --profile backend-dev up
```

When running Angular locally (API in Docker):
```bash
# From project root - start API and infrastructure
docker-compose up api dbserver redis

# From client directory - start Angular
npm start
```

### Important Notes

- Angular 9 requires Node.js 12.x - use nvm to manage versions
- Use `npm run start:container` when API runs in Docker on port 44370
- Use `npm start` when API runs locally on port 5001
- Environment files control which API URL is used