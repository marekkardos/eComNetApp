# .NET Aspire Integration — Implementation Summary

## Phase 1: ServiceDefaults + Docs

**Fixed:** `ServiceDefaults/ServiceDefaults.csproj` — changed `<TargetFramework>net9.0</TargetFramework>` to `<TargetFrameworks>net8.0;net9.0</TargetFrameworks>` so the net8.0 Api project can reference it.

**Already in place** (from prior work):
- `ServiceDefaults/Extensions.cs` — `AddServiceDefaults()` (service discovery, resilient HTTP, "self" liveness check) and `MapDefaultEndpoints()` (`/alive` endpoint). No OTel to avoid double instrumentation.
- `Api/Api.csproj` — ProjectReference to ServiceDefaults
- `Api/Program.cs` — `builder.AddServiceDefaults()` + `app.MapDefaultEndpoints()`
- `Directory.Packages.props` — `Microsoft.Extensions.Http.Resilience` 9.3.0, `Microsoft.Extensions.ServiceDiscovery` 9.0.0
- `doc/ASPIRE.md` — comprehensive docs (what/why, mutual exclusivity, prerequisites, quick start, troubleshooting)
- Solution includes ServiceDefaults under "Infrastructure" folder

## Phase 2: AppHost Project

**New files:**
- `AppHost/AppHost.csproj` — net9.0, `Aspire.AppHost.Sdk` 9.0.0, `IsAspireHost=true`, `VersionOverride` for `Microsoft.Extensions.Diagnostics.HealthChecks` and `Microsoft.Extensions.Hosting` (9.0.0) to resolve CPM conflicts with Aspire 9.x transitive deps
- `AppHost/Program.cs` — orchestrates:
  - SQL Server (2019-latest, persistent, bind mount to `.data/MSSQL2019-DATA`, two databases: `DefaultConnectionMssql`->`eCommNetDb`, `IdentityConnectionMssql`->`eCommNet_IdentityDb`)
  - Redis (ephemeral)
  - Seq (generic container, persistent, bind mount to `.data/seq-data`, ports 5341 ingest + 8081 UI)
  - Api (project reference, `WaitFor` SQL/Redis/Seq, Seq URL + AllowedOrigins injected, HTTPS on port 5001)
- `AppHost/Properties/launchSettings.json` — "Backend Only" (default) and "Full Stack" profiles

**Modified:**
- `Directory.Packages.props` — added `Aspire.Hosting.AppHost`, `Aspire.Hosting.SqlServer`, `Aspire.Hosting.Redis`, `Aspire.Hosting.NodeJs` (all 9.0.0)
- `eComNetApp.sln` — AppHost under new "Aspire" solution folder, `ASPIRE.md` added to docs folder

## Phase 3: SeedData + Angular (opt-in)

**AppHost/Program.cs additions:**
- SeedData — gated by `ASPIRE_INCLUDE_SEEDDATA` env var, references both databases, `WaitFor(sqlServer)`, `ExcludeFromManifest()`
- Angular — gated by `ASPIRE_INCLUDE_ANGULAR` env var, `AddNpmApp` with `start:aspire` script, port 4200, `ExcludeFromManifest()`

**New files:**
- `client/src/environments/environment.aspire.ts` — targets `https://localhost:5001/api/`

**Modified:**
- `AppHost/AppHost.csproj` — added ProjectReference to SeedData, added `Aspire.Hosting.NodeJs` package
- `client/angular.json` — added `aspire` build and serve configurations
- `client/package.json` — added `"start:aspire"` npm script

## Phase 4a: OTel Endpoint Fallback

**Modified:** `Api/StartupConfigurations/OpenTelemetryExtensions.cs` — now falls back to `OTEL_EXPORTER_OTLP_ENDPOINT` (standard OTel env var set automatically by Aspire) when `OPEN_TELEMETRY:ENDPOINT` is missing.

## Roadmap Update

**Modified:** `doc/ROADMAP.md` — added section 6 ".NET Aspire Integration" with scope description, added to status table as "In Progress".

## Build Verification

`dotnet build eComNetApp.sln` — **0 warnings, 0 errors** across all 9 projects (mixed TFMs: netstandard2.0, net8.0, net9.0).

## Key Technical Decisions Made During Implementation

1. **CPM + VersionOverride** (not opt-out) — AppHost stays in CPM but overrides `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.Diagnostics.HealthChecks` to 9.0.0 to satisfy Aspire transitive deps
2. **`IsAspireHost=true`** explicitly set — required because the NuGet SDK approach (`Aspire.AppHost.Sdk`) doesn't set this property (the Aspire workload normally does)
3. **`WithBindMount` for Seq** — generic containers use `WithBindMount(source, target)` not `WithDataBindMount` (which is resource-type-specific)
4. **Seq URL as lambda** — `WithEnvironment("Seq__ServerUrl", () => seq.GetEndpoint("ingest").Url)` to resolve the endpoint at runtime
