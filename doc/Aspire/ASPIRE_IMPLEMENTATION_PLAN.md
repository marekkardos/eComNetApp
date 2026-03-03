# Plan: Add .NET Aspire as Alternative to Docker Compose

## Context

The project currently uses docker-compose to orchestrate local development (SQL Server, Redis, Seq, Aspire Dashboard, Angular). .NET Aspire offers a code-first orchestration experience with built-in observability, service discovery, and a path to cloud deployment. The goal is to introduce Aspire **alongside** docker-compose — both remain fully functional — and incrementally migrate once Aspire covers all developer workflows.

**Mutual exclusivity**: docker-compose and Aspire cannot run simultaneously (they share ports and data directories). Developers use one OR the other per session.

---

## Key Design Decisions

### TFM for Aspire projects
Aspire 9.x requires `net9.0` for AppHost and ServiceDefaults. The hosted projects (Api, SeedData) stay on `net8.0`. This means:
- Developers need both .NET 8 and .NET 9 SDKs installed
- CI pipelines need updating to install .NET 9 SDK
- `global.json` may need adjustment to allow both SDKs

### OTel: no duplication
ServiceDefaults will **not** include any OpenTelemetry configuration. The existing `OpenTelemetryExtensions.cs` remains the sole OTel pipeline owner. This is deliberately non-standard — the standard Aspire ServiceDefaults template includes OTel, but we strip it to avoid double instrumentation with the existing pipeline.

### Connection strings
Both `PersistanceDependencies.cs` and `SeedData/Program.cs` use `"name=ConnectionStrings:DefaultConnectionMssql"` / `"name=ConnectionStrings:IdentityConnectionMssql"`. Aspire database resources must be named to match these keys, with explicit `databaseName` parameters for the actual catalog names (`eCommNetDb`, `eCommNet_IdentityDb`).

---

## What Does NOT Change

- `docker-compose.yml`, `docker-compose.override.yml`, `docker-compose.dcproj` — untouched
- `Api/Dockerfile`, `SeedData/Dockerfile`, `client/Dockerfile` — preserved
- `Api/StartupConfigurations/OpenTelemetryExtensions.cs` — sole OTel pipeline owner
- `Api/StartupConfigurations/HealthCheckExtensions.cs` — existing `/healthz` with security model unchanged
- All test projects

---

## Phase 1: ServiceDefaults + Skeleton Docs

### Goal
Create `ServiceDefaults` library with service discovery, resilient HTTP, and `/alive` liveness endpoint. Start developer docs immediately.

### New files

**`ServiceDefaults/ServiceDefaults.csproj`** (`net9.0`)
- Packages: `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.ServiceDiscovery`
- `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
- `<IsAspireSharedProject>true</IsAspireSharedProject>`
- No OTel packages (existing pipeline in Api handles this)

**`ServiceDefaults/Extensions.cs`**
- `AddServiceDefaults()`: registers service discovery + resilient HTTP defaults + `"self"` liveness health check tagged `"live"`
- `MapDefaultEndpoints()`: maps `/alive` endpoint (liveness, unauthenticated) — does NOT touch `/healthz`
- **No OpenTelemetry configuration** — deliberately omitted to avoid conflict with `OpenTelemetryExtensions.cs`

**`docs/ASPIRE.md`** — skeleton doc explaining:
- What Aspire is and why it's being added
- Mutual exclusivity with docker-compose (stop one before starting the other)
- Prerequisites (.NET 9 SDK, Docker Desktop)
- Will be expanded in later phases

### Modified files

| File | Change |
|------|--------|
| `Directory.Packages.props` | Add `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.ServiceDiscovery` |
| `Api/Api.csproj` | Add `<ProjectReference>` to ServiceDefaults |
| `Api/Program.cs` | Add `builder.AddServiceDefaults();` after builder creation, `app.MapDefaultEndpoints();` before `Startup.ConfigureApp` |
| `eComNetApp.sln` | Add ServiceDefaults under "Infrastructure" solution folder |

### Verify
- `dotnet build` succeeds (mixed TFMs: `net8.0` for Api, `net9.0` for ServiceDefaults)
- `dotnet run --project Api/Api.csproj` works identically to before
- `GET /alive` returns `Healthy`
- `GET /healthz` unchanged (requires localhost or API key)
- docker-compose unaffected

---

## Phase 2: AppHost Project

### Goal
`dotnet run --project AppHost` becomes the Aspire alternative to `docker-compose up api dbserver redis aspire-dashboard seq`.

### New files

**`AppHost/AppHost.csproj`** (`net9.0`)
```
Sdk: Aspire.AppHost.Sdk (version 9.0.0)
Packages: Aspire.Hosting.AppHost, Aspire.Hosting.SqlServer, Aspire.Hosting.Redis
ProjectReference: Api.csproj
UserSecretsId: 56ee6ee3-b7d8-4759-ab17-87297accef46
```

**`AppHost/Program.cs`** — orchestration:

```
SQL Server
├── WithImage("mcr.microsoft.com/mssql/server", "2019-latest")
├── WithLifetime(ContainerLifetime.Persistent)
├── WithDataBindMount("../.data/MSSQL2019-DATA")
├── AddParameter("sql-password", secret: true)  ← from user secrets
├── AddDatabase("DefaultConnectionMssql", databaseName: "eCommNetDb")
└── AddDatabase("IdentityConnectionMssql", databaseName: "eCommNet_IdentityDb")

Redis (ephemeral, no volume)

Seq (generic container)
├── WithLifetime(ContainerLifetime.Persistent)
├── WithDataBindMount("../.data/seq-data")
├── Ports: 5341 (SeqIngest), 8081 (SeqUi)
├── ExcludeFromManifest()
└── Env: ACCEPT_EULA=Y

Api (project reference)
├── WithReference(storeDb, identityDb, redis)
├── WaitFor(sqlServer, redis, seq)        ← seq included to prevent audit logger crash
├── WithEnvironment("Seq__ServerUrl", seq.GetEndpoint("SeqIngest"))
├── WithEnvironment("OPEN_TELEMETRY__ENDPOINT", ...)
├── WithEnvironment("AllowedOrigins", "http://localhost:4200")
└── WithHttpsEndpoint(port: 5001)
```

**`AppHost/Properties/launchSettings.json`** — with dashboard URL

### Important: `WaitFor(seq)`
The audit logger in `Program.cs` uses `AuditTo.Seq()` which throws on write failure. Api must wait for Seq to be healthy before starting, otherwise the first security event (login attempt) will crash the application.

### SA password setup
Developers run once:
```bash
dotnet user-secrets set "Parameters:sql-password" "Strong!Passw0rd" --project AppHost
```
This matches the docker-compose hardcoded password, so shared `.data/` directory works when switching between orchestrators.

### Modified files

| File | Change |
|------|--------|
| `Directory.Packages.props` | Add `Aspire.Hosting.AppHost`, `Aspire.Hosting.SqlServer`, `Aspire.Hosting.Redis` |
| `eComNetApp.sln` | Add AppHost under new "Aspire" solution folder |
| `docs/ASPIRE.md` | Add quick start section |

### Verify
- `dotnet run --project AppHost/AppHost.csproj` starts all services
- Aspire Dashboard at `http://localhost:15888` shows all resources
- API at `https://localhost:5001/swagger` works
- Databases created with correct names (`eCommNetDb`, `eCommNet_IdentityDb`)
- docker-compose still works independently (after stopping Aspire)

---

## Phase 3: SeedData + Angular in AppHost

### 3a: SeedData (opt-in)
- Add `<ProjectReference>` to SeedData in AppHost.csproj
- Register as `AddProject<Projects.SeedData>("seeddata")` with database references
- **Guarded by `ASPIRE_INCLUDE_SEEDDATA` env var** — does NOT run by default (avoids latency on every start; existing `MigrateAsync` + seed calls add 10-30s)
- `ExcludeFromManifest()` — dev-only

SeedData uses `Host.CreateDefaultBuilder` which loads env vars, so Aspire-injected `ConnectionStrings__DefaultConnectionMssql` will override `SeedData/appsettings.json` values. No code changes to SeedData needed.

### 3b: Angular (opt-in)
- New `client/src/environments/environment.aspire.ts` targeting `https://localhost:5001/api/`
- New `"start:aspire"` npm script in `client/package.json`
- New `aspire` build/serve configuration in `client/angular.json`
- `AddNpmApp("angular", ...)` guarded by `ASPIRE_INCLUDE_ANGULAR` env var
- `ExcludeFromManifest()`

### AppHost launch profiles

```
"Backend Only"  → default (just Api + infrastructure)
"Full Stack"    → ASPIRE_INCLUDE_ANGULAR=true, ASPIRE_INCLUDE_SEEDDATA=true
```

### Modified files

| File | Change |
|------|--------|
| `AppHost/AppHost.csproj` | Add ProjectReference to SeedData, add `Aspire.Hosting.NodeJs` |
| `AppHost/Program.cs` | Add conditional SeedData and Angular resources |
| `Directory.Packages.props` | Add `Aspire.Hosting.NodeJs` |
| `client/package.json` | Add `start:aspire` script |
| `client/angular.json` | Add `aspire` configuration |
| `docs/ASPIRE.md` | Document launch profiles |

### Verify
- Default: Angular and SeedData do NOT start
- `ASPIRE_INCLUDE_ANGULAR=true ASPIRE_INCLUDE_SEEDDATA=true dotnet run --project AppHost` — both visible in dashboard

---

## Phase 4: Developer Experience Polish

### 4a: OTel endpoint fallback (optional)
Modify `OpenTelemetryExtensions.cs` to accept `OTEL_EXPORTER_OTLP_ENDPOINT` as fallback when `OPEN_TELEMETRY:ENDPOINT` is missing. This allows AppHost to skip the explicit `.WithEnvironment("OPEN_TELEMETRY__ENDPOINT", ...)` since Aspire sets the standard env var automatically.

### 4b: Stripe CLI (optional)
`AddExecutable("stripe-cli", "stripe", ...)` guarded by `ASPIRE_INCLUDE_STRIPE_CLI` env var.

### 4c: Finalize documentation
Complete `docs/ASPIRE.md` with all workflows, troubleshooting, and decision rationale.

---

## Phase 5: Deployment Readiness (Separate Initiative)

**Note**: This phase is deliberately scoped out of the initial implementation. It involves production concerns (secrets management, networking, TLS, migration strategy) that warrant their own planning cycle. Documented here for roadmap visibility only.

- Docker Compose publisher (`Aspire.Hosting.Docker`) for non-Azure deployments
- Azure Container Apps via `azd` with `IsPublishMode` guards
- Kubernetes publisher for hybrid scenarios
- Dev-only vs production resource separation

---

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Port conflicts when both orchestrators run | Document mutual exclusivity in `docs/ASPIRE.md`; add note in README |
| Shared `.data/` directory corruption | Same SA password in both; document "stop one before starting the other" |
| Double OTel instrumentation | ServiceDefaults deliberately omits all OTel config |
| OTel package version conflicts (CPM transitive pinning) | Verify Aspire 9.x minimum OTel versions; update `Directory.Packages.props` pins |
| Audit logger crash if Seq not ready | `WaitFor(seq)` on Api resource in AppHost |
| SeedData latency on every start | Opt-in via env var guard |
| `AddDatabase` resource name vs catalog name | Use explicit `databaseName` parameter: `.AddDatabase("DefaultConnectionMssql", databaseName: "eCommNetDb")` |
| Mixed TFMs (net8.0 + net9.0) | Document .NET 9 SDK requirement; update CI |

---

## ROADMAP.md Update

Add section **6. .NET Aspire Integration** to `doc/ROADMAP.md`:

**Scope**
- ServiceDefaults project for service discovery and health checks
- AppHost project orchestrating SQL Server, Redis, Seq, API, Angular
- Coexistence with docker-compose (both fully functional, mutually exclusive per session)
- Future: deployment publishers (Azure Container Apps, Docker Compose, Kubernetes)

Add to status table: `| .NET Aspire Integration | Planned |`

---

## Implementation Order

| Phase | Effort | What it delivers |
|-------|--------|------------------|
| 1: ServiceDefaults + Docs | Small | Aspire awareness in Api, `/alive` endpoint, initial docs |
| 2: AppHost | Medium | Working `dotnet run --project AppHost` alternative to docker-compose |
| 3: SeedData + Angular | Small | Full dev workflow parity with docker-compose |
| 4: DX polish | Small | OTel fallback, Stripe CLI, final docs |
| 5: Deployment (separate) | Large | Production deployment paths — planned separately |

### Critical files to modify
- `Api/Program.cs` — two new lines (`AddServiceDefaults`, `MapDefaultEndpoints`)
- `Api/Api.csproj` — ProjectReference to ServiceDefaults
- `Directory.Packages.props` — new package versions
- `eComNetApp.sln` — new projects
- `doc/ROADMAP.md` — add Aspire entry
