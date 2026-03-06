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
- Solution is on .NET 8 (November 2023 LTS) — skipping .NET 9 entirely to .NET 10 (November 2025 LTS)
- Core project targets `netstandard2.0` — must be migrated to `net10.0`
- Several deprecated/archived packages are in use
- Docker images use `mcr.microsoft.com/dotnet/aspnet:8.0` and `sdk:8.0`
- Central Package Management (CPM) via `Directory.Packages.props` — simplifies version bumps
- No `global.json` — SDK version is implicit
- docker-compose uses format version `3.4`
- `BuildServiceProvider()` anti-pattern used in 3 places (creates service locator issues)
- Dead packages in CPM: `NLog.Web.AspNetCore` (unused, project uses Serilog)

---

## Migration Phases

### Phase 0: Prerequisites & Tooling

1. **Install .NET 10 SDK** (10.0.103 or latest)
2. **Add `global.json`** at solution root to pin SDK version:
   ```json
   {
     "sdk": {
       "version": "10.0.103",
       "rollForward": "latestPatch"
     }
   }
   ```
3. **Identify code-level breaking changes** before touching anything — see Phase 5 for the full list found during analysis

### Phase 1: Package Replacements & Version Bumps (atomic with TFM change)

> **Critical ordering note:** TFM changes and package updates must happen together in a single commit. Changing TFMs first will break `dotnet restore` because old packages (MediatR 9, etc.) may not resolve for `net10.0`.

#### 1a. Deprecated Package Swaps

**MediatR (Critical — breaking API change)**
- **Remove:** `MediatR` 9.0.0 + `MediatR.Extensions.Microsoft.DependencyInjection` 9.0.0
- **Add:** `MediatR` 12.x (latest stable)
- **Code change in `Startup.cs:82`:** `services.AddMediatR(typeof(BaseEntity))` → `services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<BaseEntity>())`

**AutoMapper (Package consolidation)**
- **Remove:** `AutoMapper.Extensions.Microsoft.DependencyInjection` 8.1.0
- **Add:** `AutoMapper` 13.x (latest stable, includes DI registration natively)
- **Code change:** `services.AddAutoMapper(typeof(MappingProfiles))` API stays the same but comes from core package
- **Verify:** `AssertConfigurationIsValid()` in `AutoMapperService.cs:21` — AutoMapper 13 changed profile validation semantics

**API Versioning (Namespace/package rename)**
- **Remove:** `Microsoft.AspNetCore.Mvc.Versioning` 4.2.0 + `Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer` 4.2.0
- **Add:** `Asp.Versioning.Mvc` 8.x + `Asp.Versioning.Mvc.ApiExplorer` 8.x
- **Code change in `ApiVersioningExtensions.cs`:** Update `using` from `Microsoft.AspNetCore.Mvc.Versioning` → `Asp.Versioning`, update registration to chain `.AddMvc()`

**Swashbuckle → Update to .NET 10 Compatible Version**
- **Update:** `Swashbuckle.AspNetCore` 5.6.3 → latest stable (6.x+) that supports .NET 10
- **Code change:** Minimal — existing 7-document grouping, JWT security scheme, and XML comment configuration in `SwaggerServiceExtensions.cs` should work with updated package
- **Decision rationale:** Lower migration risk; Swashbuckle still works and the OpenAPI + Scalar migration can be done in a follow-up

#### 1b. Dead Package Removal

- **Remove** `NLog.Web.AspNetCore` 4.9.3 from `Directory.Packages.props` — unused, project uses Serilog exclusively
- **Remove** `Microsoft.ApplicationInsights.AspNetCore` 2.16.0 — redundant with the full OpenTelemetry + Serilog + Seq observability stack. Remove from `Directory.Packages.props` and `Api.csproj`

#### 1c. Target Framework Updates (same commit as package updates)

| File | Change |
|------|--------|
| `Api/Api.csproj` | `net8.0` → `net10.0` |
| `Core/Core.csproj` | `netstandard2.0` → `net10.0`, remove explicit `LangVersion` |
| `Data/Data.csproj` | `net8.0` → `net10.0` |
| `Services/Services.csproj` | `net8.0` → `net10.0` |
| `SeedData/SeedData.csproj` | `net8.0` → `net10.0` |
| `Api.IntegrationTests/Api.IntegrationTests.csproj` | `net8.0` → `net10.0` |
| `Core.UnitTests/Core.UnitTests.csproj` | `net8.0` → `net10.0` |

**Core project considerations:**
- Moving from `netstandard2.0` to `net10.0` means this library is no longer consumable by .NET Framework callers. All consumers are in-solution and already `net8.0+` — this is safe.
- Core does not have `<ImplicitUsings>enable</ImplicitUsings>` (unsupported on netstandard2.0). Moving to net10.0 enables implicit usings by default, which may cause ambiguous reference errors with existing explicit `using` directives. Need to audit and remove duplicate usings.

#### 1d. Microsoft Package Version Bumps (via CPM)

Update `Directory.Packages.props`:

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
| `Microsoft.IdentityModel.Tokens` | 8.15.0 | latest stable |
| `System.IdentityModel.Tokens.Jwt` | 8.15.0 | latest stable |
| `AspNetCore.HealthChecks.Redis` | 8.0.1 | latest compatible (Xabaril doesn't follow MS versioning) |

**Already at .NET 10 — no change needed:**
- `Microsoft.Extensions.Configuration.Abstractions` 10.0.1
- `Microsoft.Extensions.Logging.Abstractions` 10.0.1

**Third-party packages to verify compatibility:**
- `StackExchange.Redis` 2.10.1 — verify net10.0 TFM support
- `Stripe.net` 50.0.0 — verify net10.0 TFM support (core business dependency)
- `Ardalis.GuardClauses` 3.0.1 — may ship different API surface per TFM
- `Serilog.*` packages (8 total) — verify compatibility, especially `Serilog.Sinks.MSSqlServer` 8.0.0
- `OpenTelemetry.*` packages at 1.14.0 — two are beta (`EFCore` and `Process` at 1.14.0-beta.2), check for stable releases

**Test packages (per-project `Directory.Packages.props`):**
| Package | From | To |
|---------|------|----|
| `Microsoft.NET.Test.Sdk` | 17.8.0 | 17.12.x+ |
| `coverlet.collector` | 6.0.0 | 6.x latest |
| `NUnit` | 3.14.0 | keep 3.14 (NUnit 4 migration is out of scope) |
| `NUnit3TestAdapter` | 4.5.0 | latest 4.x |
| `NUnit.Analyzers` | 3.9.0 | latest |
| `Moq` | 4.20.72 | verify compatibility |

### Phase 2: Docker Updates

#### Api/Dockerfile
- `mcr.microsoft.com/dotnet/aspnet:8.0` → `aspnet:10.0`
- `mcr.microsoft.com/dotnet/sdk:8.0` → `sdk:10.0`
- **Port change:** .NET 10 images default to non-root user on port 8080. Current Dockerfile exposes 80/443 which are privileged ports. Must update `EXPOSE` directives and add `USER` directive (SeedData/Dockerfile already has `USER $APP_UID`, Api/Dockerfile does not)

#### SeedData/Dockerfile
- `mcr.microsoft.com/dotnet/runtime:8.0` → `runtime:10.0`
- `mcr.microsoft.com/dotnet/sdk:8.0` → `sdk:10.0`

#### docker-compose.yml
- Update `ASPNETCORE_URLS` from `https://+:443;http://+:80` to match new port scheme (e.g., `http://+:8080`)
- Update port mappings: `44369:80` → `44369:8080`, `44370:443` → adjust accordingly
- Update SQL Server image from `2019-latest` to `2022-latest`
- **Data volume risk:** `.data/MSSQL2019-DATA` mounted volume — SQL Server 2022 can read 2019 databases but verify upgrade path. Consider renaming volume.
- Remove deprecated `version: '3.4'` field
- Update `docker-compose.override.example.yml` if it has port/image references

### Phase 3: Code-Level Breaking Changes

These were identified during analysis and must be fixed:

1. **`IdentityBuilder` constructor deprecated** — `IdentityServiceExtensions.cs:20` uses `new IdentityBuilder(builder.UserType, builder.Services)`. Chain directly off the return value of `AddIdentityCore<AppUser>()` instead:
   ```csharp
   services.AddIdentityCore<AppUser>()
       .AddEntityFrameworkStores<AppIdentityDbContext>()
       .AddSignInManager<SignInManager<AppUser>>();
   ```

2. **`BuildServiceProvider()` anti-pattern (3 occurrences)** — OUT OF SCOPE for this migration. Will be addressed in a separate cleanup PR.

3. **`ConfigureApiBehaviorOptions` obsolescence** — `Startup.cs:59` — review if this is obsolete in .NET 10 in favor of `IProblemDetailsService`

4. **Core project implicit usings** — Audit all `.cs` files in Core for `using` directives that will collide with implicit usings after moving to `net10.0`

### Phase 4: Validation & Testing

1. `dotnet restore` — verify all packages resolve
2. `dotnet build` — fix any compilation errors
3. `dotnet test` — run unit and integration tests
4. **EF Core migration test** — verify existing migrations still apply correctly with EF Core 10 (snapshot format may differ)
5. Docker build — verify containers build and ports work correctly
6. Update `CLAUDE.md` to reflect .NET 10

---

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| Core `netstandard2.0` → `net10.0` breaks downstream consumers | Low | All consumers are in-solution and already net8+ |
| Swashbuckle update to 6.x may have breaking config changes | Low | Keeping Swashbuckle reduces risk; update incrementally |
| MediatR 9→12 has breaking API changes | Medium | Well-documented migration, isolated registration change |
| `IdentityBuilder` constructor removed in .NET 10 | **High** | Compile-time break — fix identified in Phase 3 |
| `BuildServiceProvider()` behavior changes in .NET 10 | Medium | Out of scope — separate cleanup PR |
| Docker port 80/443 → non-root user model | Medium | Update Dockerfile EXPOSE + docker-compose port mappings |
| EF Core migrations snapshot incompatibility | Medium | Test migration idempotency, regenerate snapshot if needed |
| SQL Server 2019→2022 data volume upgrade | Low | SQL Server 2022 reads 2019 data, but test in staging first |
| Third-party packages (Stripe, Redis, Serilog) compatibility | Low | .NET 10 GA since Nov 2025; verify before starting |
| OpenTelemetry beta packages may not have stable .NET 10 versions | Medium | Check for stable releases; may need to swap to alternatives |

## Rollback Strategy

- All changes on feature branch `claude/migrate-dotnet-10-N5Vfb`
- If migration fails partway, `git reset` to last working commit
- Keep existing Docker images tagged for rollback
- No database schema changes in this migration — rollback is clean

## Out of Scope

- Angular client migration (separate project)
- Enabling nullable reference types across the solution
- Upgrading NUnit 3 → NUnit 4 (can be done separately)
- C# 14 syntax modernization (can be done in follow-up)
- `Moq` → alternative library migration (SponsorLink concern — separate decision)
- `BuildServiceProvider()` anti-pattern fix (separate cleanup PR)
- Swashbuckle → OpenAPI + Scalar migration (follow-up after .NET 10 migration stabilizes)
