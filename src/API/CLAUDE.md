# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the .NET 8 REST API backend.

## Project Overview

This is the .NET 8 REST API backend for eComNetApp, an e-commerce application. The API serves an Angular 9 frontend located in `../client/`. The solution follows Clean Architecture principles with clear separation between domain logic, data access, and presentation layers.

**For local development setup and workflows, see the main [README.md](../../README.md)** in the project root.

## Architecture

### Backend (.NET 8)

The backend is organized into distinct projects with specific responsibilities:

- **Api** - ASP.NET Core Web API (entry point)
  - Controllers in `Api/Controllers/`
  - Startup configuration in `Api/Startup.cs` and `Api/StartupConfigurations/`
  - DTOs in `Api/Dtos/`
  - API responses in `Api/ApiResponses/`
  - Static files served from `Api/Content/`

- **Core** - Domain layer (.NET Standard 2.0)
  - Entities in `Core/Entities/` (domain models)
  - Interfaces in `Core/Interfaces/` (repository and service contracts)
  - Specifications pattern in `Core/Specifications/`
  - MediatR command handlers in `Core/CommandHandlers/`
  - MediatR query handlers in `Core/QueryHandlers/`

- **Data** - Infrastructure/persistence layer (.NET 8)
  - `StoreContext` - Main EF Core DbContext for products, orders, delivery methods (database: `eCommNetDb`)
  - Identity context - Separate DbContext for user authentication (database: `eCommNet_IdentityDb`)
  - Repository implementations in `Data/Repositories/`
  - Entity configurations in `Data/Config/`
  - EF Core migrations in `Data/Migrations/`
  - Unit of Work pattern in `Data/UnitOfWork.cs`

- **Services** - Application services (.NET 8)
  - `StripePaymentService` - Payment processing
  - `ResponseCacheService` - Redis caching
  - Service interfaces in `Services/Interfaces/`
  - Dependency registration in `Services/ConfigureDependencies.cs`

- **SeedData** - Console app for seeding database
  - `StoreContextSeed.cs` - Seeds products, brands, types, delivery methods
  - `AppIdentityDbContextSeed.cs` - Seeds identity data

- **Api.IntegrationTests** - NUnit integration tests

### Key Patterns

- **CQRS with MediatR**: Command and query handlers are separated in Core project
- **Repository Pattern**: Data access abstracted through IGenericRepository and specific repositories
- **Specification Pattern**: Query logic encapsulated in reusable specifications
- **Unit of Work**: Transaction management across multiple repositories

### Technology Stack

- **Framework**: ASP.NET Core 8, Entity Framework Core 8
- **Database**: SQL Server 2019 (via EF Core migrations)
- **Caching**: Redis (StackExchange.Redis)
- **Authentication**: JWT tokens (custom implementation in `Api/Identity/`)
- **Payments**: Stripe.net
- **Patterns**: MediatR, AutoMapper
- **Observability**: OpenTelemetry, Serilog (with Seq and console sinks)
- **API Documentation**: Swagger/Swashbuckle
- **Health Checks**: AspNetCore.HealthChecks (SQL Server, Redis)

## Common Commands

### Development

Build the solution:
```bash
dotnet build
```

Build specific project:
```bash
dotnet build Api/Api.csproj
```

Run the API locally (uses appsettings.Development.json):
```bash
dotnet run --project Api/Api.csproj
```

Watch mode for development:
```bash
dotnet watch --project Api/Api.csproj
```

Run integration tests:
```bash
dotnet test Api.IntegrationTests/Api.IntegrationTests.csproj
```

### Database Migrations

Add a new migration:
```bash
dotnet ef migrations add <MigrationName> --project Data/Data.csproj --startup-project Api/Api.csproj
```

Update database:
```bash
dotnet ef database update --project Data/Data.csproj --startup-project Api/Api.csproj
```

Seed database (run SeedData console app):
```bash
dotnet run --project SeedData/SeedData.csproj
```

### Docker Development

The API can run in Docker containers. See main [README.md](../../README.md) for detailed workflows.

Quick start:
```bash
# From project root
# Run API + infrastructure
docker-compose up api dbserver redis
```

The API is accessible at `http://localhost:44369` when running in containers.

## Environment Configuration

### Configuration Files

- Main config: `Api/appsettings.Development.json`

### Connection Strings

| Key | Database | Description |
|-----|----------|-------------|
| `ConnectionStrings:DefaultConnectionMssql` | eCommNetDb | Main store database |
| `ConnectionStrings:IdentityConnectionMssql` | eCommNet_IdentityDb | Identity/auth database |
| `ConnectionStrings:Redis` | - | Redis cache server |

### Authentication Settings

| Section | Keys | Description |
|---------|------|-------------|
| `Token` | `Key`, `Issuer` | JWT signing key and issuer claim |
| `TokenSettings` | `AccessTokenExpirationMinutes` (15), `RefreshTokenExpirationDays` (7), `CookieName` | Token lifecycle configuration |
| `LockoutSettings` | `DefaultLockoutTimeSpanMinutes` (15), `MaxFailedAccessAttempts` (5), `AllowedForNewUsers` | Account lockout policy |

### Observability Settings

| Section | Keys | Description |
|---------|------|-------------|
| `OPEN_TELEMETRY` | `ENDPOINT` | OpenTelemetry collector endpoint (required) |
| `Seq` | `ServerUrl`, `ApiKey` | Seq logging server (ServerUrl required for audit logging) |

### Other Settings

| Key | Description |
|-----|-------------|
| `StripeSettings:SecretKey` | Stripe API secret (required, use user-secrets) |
| `StripeSettings:WebHookSecret` | Webhook signature verification (required, use user-secrets) |
| `HealthCheckApiKey` | Remote health check authentication header value |
| `ApiUrlContent` | Static files URL path (default: "/Content/") |
| `AllowedOrigins` | CORS origins, comma-separated (env var) |

### Required Configuration (throws exception if missing)

- `Token:Key` - JWT signing
- `OPEN_TELEMETRY:ENDPOINT` - Telemetry export
- `Seq:ServerUrl` - Audit logging
- `StripeSettings:SecretKey` - Payment processing
- `ConnectionStrings:Redis` - Caching

## Important Implementation Details

### C# Language Version

The solution uses C# 12 features. When adding new functionality or refactoring existing code, prefer modern C# 12 syntax:
- Primary constructors for classes
- Collection expressions
- Using directives for aliases
- Raw string literals

### Central Package Management

The solution uses Central Package Management (CPM) with `Directory.Packages.props` at the root. Package versions are centrally managed, and individual projects reference packages without version attributes.

### Logging and Observability

- Serilog configured in `Api/Program.cs` with `UseSerilog()`
- OpenTelemetry setup in `Api/StartupConfigurations/OpenTelemetryExtensions.cs`
- Tracing: ASP.NET Core, HTTP client, EF Core instrumentation
- Metrics: Runtime, process, ASP.NET Core metrics
- Log sinks: Console, Seq, OpenTelemetry

### Authentication Flow (Server-Side)

The API uses a secure refresh token pattern with HttpOnly cookies to protect against XSS attacks:

**Flow:**
1. User logs in → server sets a refresh token in HttpOnly cookie
2. Server returns a short-lived access token in the response body
3. Angular stores the access token in memory only (not localStorage)
4. When access token expires → Angular calls `/api/account/refresh` → cookie is sent automatically → server returns a new access token

**Key Components:**
- `TokenService.cs` (`Api/Identity/`) - JWT access token generation with JTI claim
- `RefreshTokenService.cs` (`Api/Identity/`) - Refresh token generation, validation, rotation, and revocation
- `TokenSettings` configuration in appsettings - Configurable token lifetimes (AccessTokenExpirationMinutes, RefreshTokenExpirationDays)
- `CookieExtensions.cs` (`Api/Extensions/`) - Secure cookie helpers (HttpOnly, Secure, SameSite=Strict)
- `AccountController.cs` - `/login`, `/register`, `/refresh`, `/logout` endpoints

**Security Features:**
- Short-lived access tokens with configurable expiration (reduced attack window)
- Refresh tokens stored in HttpOnly cookies (protected from JavaScript access)
- Server-side token revocation and rotation on each refresh
- Stolen token detection: if a revoked refresh token is reused, all user's refresh tokens are automatically revoked (forces re-login)
- Account lockout after 5 failed login attempts (15 minute duration)
- Separate identity context from main StoreContext

**Database:**
- `RefreshToken` entity in `Core/Entities/Identity/` with SHA256 hashing
- Stored in `eCommNet_IdentityDb` database via `AppIdentityDbContext`

See [docs/AUTH_REFACTORING_PLAN.md](docs/AUTH_REFACTORING_PLAN.md) for complete implementation details.

### Stripe Payment Integration

Payment processing uses Stripe Payment Intents with webhook-based order status updates.

**Key Components:**
- `StripePaymentService.cs` (Services) - Payment Intent creation and order status updates
- `PaymentsController.cs` (Api/Controllers) - Payment endpoints and webhook handler
- Webhook endpoint: `POST /api/payments/webhook`

**Payment Flow:**
1. Client calls `POST /api/payments/{basketId}` to create/update Payment Intent
2. API validates basket, creates Stripe Payment Intent, returns `ClientSecret`
3. Angular frontend completes payment using Stripe.js
4. Stripe sends webhook to API with payment result
5. API updates order status: `PaymentReceived` or `PaymentFailed`

**Configuration:**
- `StripeSettings:SecretKey` - Stripe API secret key (backend only)
- `StripeSettings:WebHookSecret` - Webhook signature verification secret
- Frontend publishable key configured in Angular environment files

**Important:**
- Order status updates happen via webhooks, not client-side confirmation
- Webhook signature verification ensures requests come from Stripe (PaymentsController.cs:46)
- PaymentIntentId is the critical link: baskets (Redis) store it, orders (SQL) must copy it during creation, webhooks (Stripe) use it to find and update the correct order. If an order lacks the PaymentIntentId, webhook updates will silently fail

**Local Development:**
- Requires Stripe CLI for webhook forwarding: `stripe listen --forward-to http://localhost:44369/api/payments/webhook`
- See [STRIPE_DEVELOPMENT.md](STRIPE_DEVELOPMENT.md) for complete setup guide

### CORS Configuration

CORS origins configured via `AllowedOrigins` environment variable (comma-separated):
```
AllowedOrigins=http://localhost:4200,http://angular:4200
```

### API Versioning

API versioning configured in `Startup.cs` with `AddCustomApiVersioning()` extension.

### Health Checks

Health checks are exposed at `/healthz` endpoint with security-aware access control:

**Registered Checks:**
- `StoreDbContext` - Main EF Core database (eCommNetDb)
- `AppIdentityDbContext` - Identity database (eCommNet_IdentityDb)
- `redis cache` - Redis connectivity and availability
- `Stripe` - Stripe API connectivity and key validation (uses Balance API)

**Security Model:**
- **Local access** (127.0.0.1, ::1, localhost): No authentication required
- **Remote access**: Requires `X-Health-Check-Key` header matching `HealthCheckApiKey` configuration
- Unauthorized requests are logged with IP address

### Static Files

Static files served from `Api/Content/` directory, accessible at `/content` URL path.

## Development Workflow Notes

### Port Mappings

| Service | Port | URL |
|---------|------|-----|
| API (Docker) | 44369 | http://localhost:44369 |
| API (Local VS) | 5001 | https://localhost:5001 |
| Angular | 4200 | http://localhost:4200 |
| SQL Server | 1433 | localhost,1433 |
| Redis | 6379 | localhost:6379 |

### Important Notes

- The solution uses **separate databases**: `eCommNetDb` (store context) and `eCommNet_IdentityDb` (identity context)
- AutoMapper configuration is validated in Development environment (see `Startup.ConfigureApp()`)
- Error handling uses custom error pages: `/error` for exceptions, `/errors/{code}` for status codes