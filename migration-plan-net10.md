# .NET 10 Migration Plan — eComNetApp API

## Current State Summary

| Project | Current TFM | Notes |
|---------|------------|-------|
| Api | `net8.0` | ASP.NET Core Web API entry point |
| Core | `netstandard2.0` | Domain layer, C# 12 via LangVersion |
| Data | `net8.0` | EF Core 8 persistence layer |
| Services | `net8.0` | Application services |
| SeedData | `net8.0` | Console app for DB seeding |
| Api.IntegrationTests | `net8.0` | NUnit test project |
| Core.UnitTests | `net8.0` | NUnit test project |

**Key Observations:**
- Solution is on .NET 8 (November 2023 LTS) — skipping .NET 9 entirely
- Core project targets `netstandard2.0` — must be migrated to `net10.0`
- Several deprecated/archived packages are in use
- Docker images use `mcr.microsoft.com/dotnet/aspnet:8.0` and `sdk:8.0`
- Central Package Management (CPM) via `Directory.Packages.props` — simplifies version bumps
- No `global.json` — SDK version is implicit
- docker-compose uses format version `3.4`

---

## Migration Phases

### Phase 1: Target Framework Updates

Update all `.csproj` files from their current TFM to `net10.0`:

| File | Change |
|------|--------|
| `Api/Api.csproj` | `net8.0` → `net10.0` |
| `Core/Core.csproj` | `netstandard2.0` → `net10.0`, remove explicit `LangVersion` (C# 14 is default) |
| `Data/Data.csproj` | `net8.0` → `net10.0` |
| `Services/Services.csproj` | `net8.0` → `net10.0` |
| `SeedData/SeedData.csproj` | `net8.0` → `net10.0` |
| `Api.IntegrationTests/Api.IntegrationTests.csproj` | `net8.0` → `net10.0` |
| `Core.UnitTests/Core.UnitTests.csproj` | `net8.0` → `net10.0` |

**Core project impact:** Moving from `netstandard2.0` to `net10.0` means this library is no longer consumable by .NET Framework callers. Since all consumers are already `net8.0+`, this is safe.

### Phase 2: Deprecated Package Replacements

#### 2a. MediatR (Critical — breaking API change)
- **Remove:** `MediatR` 9.0.0 + `MediatR.Extensions.Microsoft.DependencyInjection` 9.0.0
- **Add:** `MediatR` 12.x (latest stable)
- **Code change:** `services.AddMediatR(typeof(BaseEntity))` → `services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<BaseEntity>())`

#### 2b. AutoMapper (Package consolidation)
- **Remove:** `AutoMapper.Extensions.Microsoft.DependencyInjection` 8.1.0
- **Add:** `AutoMapper` 13.x (latest stable, includes DI registration natively)
- **Code change:** Minimal — `services.AddAutoMapper(...)` stays the same, just comes from the core package now

#### 2c. API Versioning (Namespace/package rename)
- **Remove:** `Microsoft.AspNetCore.Mvc.Versioning` 4.2.0 + `Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer` 4.2.0
- **Add:** `Asp.Versioning.Mvc` 8.x + `Asp.Versioning.Mvc.ApiExplorer` 8.x
- **Code change:** Update `using` statements and registration in `ApiVersioningExtensions.cs`

#### 2d. Swashbuckle → Microsoft.AspNetCore.OpenApi + Scalar
- **Remove:** `Swashbuckle.AspNetCore` 5.6.3
- **Add:** `Microsoft.AspNetCore.OpenApi` (ships with .NET 10) + `Scalar.AspNetCore` for UI
- **Code change:** Replace `AddSwaggerServicesExt()` / `UseSwaggerExt()` with `AddOpenApi()` / `MapOpenApi()` + `MapScalarApiReference()`
- **Note:** This is the most involved change — need to update `SwaggerExtensions.cs` and `Startup.cs`

### Phase 3: Microsoft Package Version Bumps (via CPM)

Update `Directory.Packages.props` versions:

| Package | From | To |
|---------|------|----|
| `Microsoft.AspNetCore.Authentication.Google` | 8.0.22 | 10.0.x |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.22 | 10.0.x |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | 8.0.22 | 10.0.x |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 8.0.0 | 10.0.x |
| `Microsoft.EntityFrameworkCore` | 8.0.0 | 10.0.x |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.0 | 10.0.x |
| `Microsoft.EntityFrameworkCore.Relational` | 8.0.0 | 10.0.x |
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.0 | 10.0.x |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | 8.0.0 | 10.0.x |
| `Microsoft.Extensions.Diagnostics.HealthChecks` | 8.0.0 | 10.0.x |
| `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | 8.0.0 | 10.0.x |
| `Microsoft.Extensions.Hosting` | 8.0.0 | 10.0.x |
| `Microsoft.Extensions.Identity.Stores` | 8.0.22 | 10.0.x |
| `AspNetCore.HealthChecks.Redis` | 8.0.1 | 10.0.x (if available) |
| `Microsoft.Extensions.Configuration.Abstractions` | 10.0.1 | 10.0.x (already .NET 10!) |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.1 | 10.0.x (already .NET 10!) |

Also bump test-specific packages in the per-project `Directory.Packages.props`:
| Package | From | To |
|---------|------|----|
| `Microsoft.NET.Test.Sdk` | 17.8.0 | 17.12.x+ |
| `NUnit` | 3.14.0 | 4.x or keep 3.14 |
| `coverlet.collector` | 6.0.0 | 6.x latest |

### Phase 4: Docker Updates

#### Api/Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
```

#### SeedData/Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
```

### Phase 5: docker-compose.yml Updates

- Update SQL Server image from `2019-latest` to `2022-latest` (SQL Server 2019 goes EOL soon; 2022 is current)
- Consider removing deprecated `version: '3.4'` field (modern Docker Compose ignores it)

### Phase 6: Add global.json (optional but recommended)

Pin the SDK version to prevent accidental use of a different SDK:
```json
{
  "sdk": {
    "version": "10.0.103",
    "rollForward": "latestPatch"
  }
}
```

### Phase 7: Code-Level Breaking Changes

1. **EF Core 10 breaking changes** — review for any removed APIs or behavioral changes in EF Core migrations
2. **ASP.NET Core 10 breaking changes** — review for any middleware, authentication, or hosting changes
3. **C# 14 opportunities** — Core project can now use latest language features without explicit `LangVersion`
4. **Nullable reference types** — Api.csproj currently has `<Nullable>disable</Nullable>` — consider enabling (separate effort)

### Phase 8: Validation & Testing

1. `dotnet restore` — verify all packages resolve
2. `dotnet build` — fix any compilation errors
3. `dotnet test` — run unit and integration tests
4. Docker build — verify containers build correctly
5. Update CLAUDE.md to reflect .NET 10

---

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| Core `netstandard2.0` → `net10.0` breaks downstream consumers | Low | All consumers are in-solution and already net8+ |
| Swashbuckle removal breaks API documentation workflow | Medium | Replace with OpenAPI + Scalar, test thoroughly |
| MediatR 9→12 has breaking API changes | Medium | Well-documented migration, isolated registration change |
| EF Core migrations incompatibility | Low | Existing migrations should work; new ones will use EF 10 |
| Third-party packages not yet supporting net10.0 | Low | .NET 10 has been GA since Nov 2025; most packages support it |
| SQL Server 2019 image compatibility | Low | Upgrade to 2022 in docker-compose |

## Out of Scope

- Angular client migration (separate project)
- Enabling nullable reference types across the solution
- Upgrading NUnit 3 → NUnit 4 (can be done separately)
- C# 14 syntax modernization (can be done in follow-up)
