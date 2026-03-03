# .NET Aspire Integration

## What is .NET Aspire?

[.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) is a code-first orchestration framework for cloud-native .NET applications. It provides:

- **Service orchestration** — define your entire dev stack (databases, caches, APIs, frontends) in C# code
- **Built-in observability** — automatic OpenTelemetry integration with a dashboard
- **Service discovery** — services find each other by name, not hardcoded URLs
- **Resilient HTTP** — automatic retry, circuit breaker, and timeout policies

## Why Aspire alongside Docker Compose?

Aspire is introduced **alongside** docker-compose — both remain fully functional. This allows incremental adoption while keeping the existing workflow available.

| Feature | Docker Compose | Aspire |
|---------|---------------|--------|
| Start command | `docker-compose up` | `dotnet run --project AppHost` |
| Dashboard | Aspire Dashboard container | Built-in Aspire Dashboard |
| Service discovery | Docker networking | Aspire service discovery |
| Config injection | Environment variables in YAML | Code-first with `WithReference()` |

## Mutual Exclusivity

Docker Compose and Aspire **cannot run simultaneously** — they share ports (1433, 6379, 5341, etc.) and data directories (`.data/`).

**Always stop one before starting the other:**

```bash
# Stop docker-compose before starting Aspire
docker-compose down

# Stop Aspire (Ctrl+C) before starting docker-compose
```

## Prerequisites

- **.NET 9 SDK** — required for AppHost and ServiceDefaults projects
- **.NET 8 SDK** — required for Api, Data, Services, SeedData projects
- **Docker Desktop** — required for containerized resources (SQL Server, Redis, Seq)

## Quick Start

### 1. Set up SA password (one-time)

```bash
dotnet user-secrets set "Parameters:sql-password" "Strong!Passw0rd" --project src/API/AppHost
```

This matches the docker-compose hardcoded password, so the shared `.data/` directory works when switching between orchestrators.

### 2. Run with Aspire

```bash
dotnet run --project src/API/AppHost
```

The Aspire Dashboard opens automatically at `https://localhost:15888`.

### 3. Available Services

| Service | URL |
|---------|-----|
| Aspire Dashboard | https://localhost:15888 |
| API (HTTPS) | https://localhost:5001 |
| API (Swagger) | https://localhost:5001/swagger |
| Seq UI | http://localhost:8081 |
| SQL Server | localhost:1433 |
| Redis | localhost:6379 |

## Launch Profiles

| Profile | What starts | Use case |
|---------|------------|----------|
| Backend Only | Api + SQL + Redis + Seq | Day-to-day backend development |
| Full Stack | Above + Angular + SeedData | Full integration testing |

To select a profile from the CLI use `--launch-profile`:

```bash
# Backend only (default)
dotnet run --project src/API/AppHost --launch-profile "Backend Only"

# Full stack (Angular + SeedData included)
dotnet run --project src/API/AppHost --launch-profile "Full Stack"
```

You can also point to the directory instead of the `.csproj` file — both work:

```bash
dotnet run --project src/API/AppHost/AppHost.csproj --launch-profile "Full Stack"
```

`dotnet run` reads `Properties/launchSettings.json` automatically and applies all environment variables defined in the named profile. Alternatively, set the variables manually:

```bash
ASPIRE_INCLUDE_ANGULAR=true ASPIRE_INCLUDE_SEEDDATA=true dotnet run --project src/API/AppHost
```

### Option 2: Aspire CLI

Requires the [Aspire CLI](https://aspire.dev/get-started/install-cli/) (installed at `~/.aspire/bin/aspire`). Run from the repo root — it auto-discovers the AppHost and runs the Full Stack profile by default:

```bash
# Full stack (default)
aspire run

# Backend only
ASPIRE_INCLUDE_ANGULAR=false ASPIRE_INCLUDE_SEEDDATA=false aspire run
```

The CLI adds certificate checking, a richer startup display, and a clickable dashboard URL in the terminal.

## Architecture Decisions

### No OpenTelemetry in ServiceDefaults

The standard Aspire ServiceDefaults template includes OpenTelemetry configuration. We deliberately omit it because `OpenTelemetryExtensions.cs` already configures the full OTel pipeline (tracing, metrics, exporters). Including it in ServiceDefaults would cause double instrumentation.

### Connection String Names

Aspire database resources are named to match existing connection string keys:
- `DefaultConnectionMssql` -> database `eCommNetDb`
- `IdentityConnectionMssql` -> database `eCommNet_IdentityDb`

This ensures `PersistanceDependencies.cs` and `SeedData/Program.cs` work without changes.

### Shared Data Directory

Both orchestrators use `.data/` for persistent storage (SQL Server data, Seq logs). Using the same SA password ensures database files are compatible when switching between docker-compose and Aspire.

## Stopping Aspire

Unlike `docker-compose down`, there is no single "Aspire down" command. The way you stop depends on what you want to clean up:

### Stop the orchestrator only (keep containers running)

Press **Ctrl+C** in the terminal where `dotnet run` is running. This stops the AppHost process and the Aspire Dashboard, but leaves containers (SQL Server, Seq, Redis, Angular) running in Docker. Persistent containers (`sql`, `seq`) are always kept across restarts by design.

### Stop all containers as well

After Ctrl+C, stop all running containers:

```bash
# Stop all running Docker containers (equivalent to docker-compose down for services)
docker stop $(docker ps -q)

# Or stop specific Aspire containers by name pattern
docker ps --filter name=aspire --format "{{.Names}}" | xargs docker stop
```

### Kill a zombie AppHost (port already in use)

If `dotnet run` fails with `address already in use` on ports 19900, 15888, or 19888 it means a previous AppHost process is still alive.

**macOS / Linux:**
```bash
lsof -ti:19900 | xargs -r kill -9
lsof -ti:15888 | xargs -r kill -9
lsof -ti:19888 | xargs -r kill -9
```

**Windows (PowerShell):**
```powershell
@(19900, 15888, 19888) | ForEach-Object {
    Get-NetTCPConnection -LocalPort $_ -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty OwningProcess |
    ForEach-Object { Stop-Process -Id $_ -Force }
}
```

## Troubleshooting

### Port conflicts

If you see port binding errors, ensure docker-compose is fully stopped:

```bash
docker-compose down
docker ps  # verify no containers are running on conflicting ports
```

### SQL Server won't start

Ensure the SA password is set in user secrets:

```bash
dotnet user-secrets list --project src/API/AppHost
```

### API crashes on startup

The API requires Seq to be running (audit logger throws on write failure). Aspire's `WaitFor(seq)` handles this, but if running the API standalone, ensure Seq is accessible.
