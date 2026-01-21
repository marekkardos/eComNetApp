# Docker Compose Local Development Guide

This guide explains how to use Docker Compose for local development across three different developer roles: Frontend, Backend, and Full-Stack.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Initial Setup](#initial-setup)
- [Developer Workflows](#developer-workflows)
  - [Frontend Developer](#frontend-developer)
  - [Backend Developer](#backend-developer)
  - [Full-Stack Developer](#full-stack-developer)
- [Architecture](#architecture)
- [Common Commands](#common-commands)
- [Troubleshooting](#troubleshooting)
- [FAQ](#faq)

---

## Overview

Our local development environment uses Docker Compose to orchestrate multiple services:

- **API** (.NET 8/9) - REST API backend
- **Angular** (v9) - Frontend application
- **SQL Server** - Database
- **Redis** - Caching
- **Aspire Dashboard** - OpenTelemetry observability (traces, logs, metrics)
- **Seq** (optional) - Structured log viewer for local development

Different developer roles need different setups. Docker Compose profiles allow each developer to run only what they need.

---

## Prerequisites

### Required Software

| Software | Version | Notes |
|----------|---------|-------|
| Docker Desktop | Latest | Ensure it's running before starting |
| .NET SDK | 8 or 9 | For local API development |
| Node.js | 12.22.12 | Angular 9 requires Node 12.x |
| Visual Studio | 2026 | For API development and debugging |

### Install Node Version Manager (nvm)

We use Node 12.x for Angular 9 compatibility. Use nvm to manage Node versions:

**Windows:** Download [nvm-windows](https://github.com/coreybutler/nvm-windows/releases)

**Mac/Linux:** 
```bash
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash
```

### Verify Installation

```bash
# Check Docker
docker --version
docker-compose --version

# Check Node (should be 12.x)
node --version

# Check .NET
dotnet --version
```

---

## Initial Setup

Follow these steps once before starting development:

### 1. Clone Repository

```bash
git clone <your-repository-url>
cd <your-solution>
```

### 2. Set Up User Secrets (API Keys)

The API uses User Secrets for sensitive data like Stripe keys. These are NOT stored in git.

```bash
cd Api
dotnet user-secrets init
dotnet user-secrets set "StripeSettings:SecretKey" "sk_test_YOUR_SECRET_KEY"
dotnet user-secrets set "StripeSettings:WebHookSecret" "whsec__WebHookSecret"
cd ..
```

**Where to get keys:**
- Stripe: https://dashboard.stripe.com/test/apikeys

**For complete Stripe local development setup (webhooks, testing, troubleshooting):**
- See [STRIPE_DEVELOPMENT.md](STRIPE_DEVELOPMENT.md) for detailed guide

### 3. Set Up Node.js for Angular

**Note:** Backend Developers can skip this step - Node.js is already installed in the Angular container.

```bash
cd client

# Install and use Node 12
nvm install 12.22.12
nvm use 12.22.12

# Verify
node --version  # Should show v12.22.12

# Install dependencies
npm install

cd ..
```

### 4. Set Up Angular Environment Files

Angular uses environment-specific configuration files. Template files are provided - copy them and add your Stripe publishable key:

```bash
cd client/src/environments

# For full-stack local development
cp environment.local.template.ts environment.local.ts

# For backend dev (containerized Angular)
cp environment.container.template.ts environment.container.ts

cd ../../..
```

Edit each file and replace `REPLACE_WITH_YOUR_KEY` with your Stripe publishable key:
- Get your key from: https://dashboard.stripe.com/test/apikeys (use the **Publishable key**, starts with `pk_test_`)

**Note:** The `.ts` files are gitignored to prevent committing secrets. Only the `.template.ts` files are tracked.

You're ready to start development! Choose your workflow below.

---

## Developer Workflows

Choose the workflow that matches your role:

---

## Frontend Developer

You work primarily on the Angular application and need the backend API running but don't need to debug it.

### What Runs Where

| Service | Location | Why |
|---------|----------|-----|
| Angular | **Host** (your machine) | Hot reload, Chrome DevTools |
| API | Docker container | Just needs to work |
| Database | Docker container | Infrastructure |
| Redis | Docker container | Infrastructure |

### Environment Configuration

Angular uses `environment.ts` which points to:
- API URL: `http://localhost:44369/api/`

### How to Start

```bash
# Terminal 1: Start backend services
docker-compose up api dbserver redis

# Wait for "Application started" message

# Terminal 2: Start Angular with hot reload
cd client
nvm use 12.22.12  # Switches to Node 12
ng serve

# Open browser: http://localhost:4200
```

### What You Get

- ✅ Angular runs on http://localhost:4200
- ✅ Hot reload - save file, browser refreshes automatically
- ✅ Chrome DevTools for debugging
- ✅ API available at http://localhost:44369/swagger
- ✅ Aspire Dashboard at http://localhost:18888 (traces, logs, metrics)
- ✅ All API calls work

### Making Changes

1. Edit Angular files (components, services, etc.)
2. Save
3. Browser auto-refreshes with your changes
4. No need to restart anything

### Stopping Services

```bash
# Ctrl+C in both terminals

# Or stop containers
docker-compose down
```

### Tips

- Keep `docker-compose up api dbserver redis` running in the background
- Only restart containers if you change docker-compose.yml
- If API is misbehaving, check logs: `docker-compose logs api`

---

## Backend Developer

You work primarily on the API and need Angular running for testing, but don't need to modify Angular code.

### What Runs Where

| Service | Location | Why |
|---------|----------|-----|
| Angular | Docker container | Just needs to work for testing |
| API | Docker container | Can be debugged in Visual Studio |
| Database | Docker container | Infrastructure |
| Redis | Docker container | Infrastructure |

### Environment Configuration

Angular uses `environment.container.ts` which points to:
- API URL: `http://localhost:44369/api/`

### Two Options

#### Option A: Everything in Containers (No Debugging)

**When to use:** Quick testing, don't need to debug API

```bash
# Start everything
docker-compose --profile backend-dev up

# Open browser: http://localhost:4200
# Open Swagger: http://localhost:44369/swagger
```

**What you get:**
- ✅ Full stack running
- ✅ Can test Angular → API integration
- ✅ Angular has hot reload (slower than local)
- ✅ Aspire Dashboard at http://localhost:18888
- ❌ Can't debug API with breakpoints

**Stopping:**
```bash
# Ctrl+C
docker-compose down
```

#### Option B: Debug API in Visual Studio (Recommended)

**When to use:** Need to debug API code, set breakpoints, step through code

**In Visual Studio:**
1. Set **docker-compose** as startup project
   - Right-click `docker-compose` in Solution Explorer
   - Select "Set as Startup Project"
2. Press **F5** to start debugging
3. VS automatically uses the `backend-dev` profile and starts all services:
   - API (with debugging attached)
   - Angular (without debugging)
   - Database (without debugging)
   - Redis (without debugging)
4. Browser opens to Swagger at http://localhost:44369/swagger

**What you get:**
- ✅ Full stack running (all services started automatically by VS)
- ✅ Set breakpoints in API code
- ✅ Step through code, inspect variables
- ✅ Angular available at http://localhost:4200 for testing
- ✅ Aspire Dashboard at http://localhost:18888
- ✅ No need to manually run docker-compose commands

### Making API Changes

When you modify API code:

1. Stop debugging in Visual Studio (Shift+F5)
2. Rebuild: Right-click docker-compose project → Rebuild
3. Press F5 again

Visual Studio will rebuild the API image and restart all services automatically.

### Making Angular Changes

If you need to modify Angular:

1. Edit Angular files in the `client` directory
2. Angular hot reload will detect changes automatically (may take a few seconds)
3. If you need to rebuild the image: Stop debugging (Shift+F5), rebuild docker-compose project, press F5 again

**Note:** Angular hot reload works in container but is slower than running locally. For heavy Angular work, switch to Frontend Developer workflow.

### Tips

- Keep infrastructure running, only rebuild API when needed
- Check API logs: `docker-compose logs api`
- Check Angular logs: `docker-compose logs angular`
- If Angular can't reach API, check CORS in API logs

---

## Full-Stack Developer

You work on both Angular and API and need maximum debugging capability for both.

### What Runs Where

| Service | Location | Why |
|---------|----------|-----|
| Angular | **Host** | Hot reload, Chrome DevTools |
| API | **Host** (Visual Studio) | Full debugging |
| Database | Docker container | Infrastructure |
| Redis | Docker container | Infrastructure |

### Environment Configuration

Angular uses `environment.local.ts` which points to:
- API URL: `https://localhost:5001/api/`

### How to Start

```bash
# Terminal 1: Start infrastructure only
docker-compose up dbserver redis

# Or include Aspire Dashboard for observability
docker-compose up dbserver redis aspire-dashboard

# Wait for services to start
```

**In Visual Studio:**
1. Select **"Local Development"** profile (not docker-compose!)
2. Press **F5**
3. API starts at https://localhost:5001
4. Browser opens to Swagger

```bash
# Terminal 2: Start Angular
cd client
nvm use 12.22.12
npm start

# Browser auto-opens: http://localhost:4200
```

### What You Get

- ✅ API at https://localhost:5001 with full VS debugging
- ✅ Angular at http://localhost:4200 with instant hot reload
- ✅ Set breakpoints in API → they hit when Angular calls endpoints
- ✅ Chrome DevTools for Angular debugging
- ✅ Aspire Dashboard at http://localhost:18888 (run `docker-compose up aspire-dashboard` separately)
- ✅ Maximum development speed

### Making Changes

**API Changes:**
1. Edit C# code in Visual Studio
2. Hot Reload kicks in (or restart with Ctrl+Shift+F5)
3. Changes active immediately

**Angular Changes:**
1. Edit TypeScript/HTML/CSS files
2. Save
3. Browser auto-refreshes (1-2 seconds)

### Debugging Both Simultaneously

1. Set breakpoint in API controller method
2. Set breakpoint in Angular service (Chrome DevTools)
3. Trigger action in Angular UI
4. Angular breakpoint hits first (in browser)
5. Step through, API call is made
6. API breakpoint hits (in Visual Studio)
7. Step through API code
8. Response returns to Angular

This is the most powerful setup for full-stack debugging!

### Stopping Services

```bash
# Stop API: Shift+F5 in Visual Studio
# Stop Angular: Ctrl+C in terminal
# Stop infrastructure: docker-compose down
```

### Tips

- This is the fastest workflow for rapid development
- Use this for daily work on features touching both frontend and backend
- Only downside: requires Node.js and .NET SDK installed locally
- If you want to test containerized behavior, switch to Backend Developer workflow

---

## Architecture

### Docker Network

All containers run on the same Docker network: `api-network`

```
api-network (bridge)
├── api container (hostname: "api")
├── angular container (hostname: "angular")
├── dbserver container (hostname: "dbserver")
├── redis container (hostname: "redis")
└── aspire-dashboard container (hostname: "aspire-dashboard")
```

**Inside containers:** Services can reach each other by name (e.g., `http://api:80`)

**From host machine:** Must use `localhost` with published ports (e.g., `http://localhost:44369`)

### Port Mappings

| Service | Container Port | Host Port | URL from Host |
|---------|----------------|-----------|---------------|
| API | 80 | 44369 | http://localhost:44369 |
| Angular | 4200 | 4200 | http://localhost:4200 |
| SQL Server | 1433 | 1433 | localhost,1433 |
| Redis | 6379 | 6379 | localhost:6379 |
| Aspire Dashboard | 18888 | 18888 | http://localhost:18888 |
| Seq (optional) | 80 | 8081 | http://localhost:8081 |

### Environment Files

Angular uses different environment files for different scenarios:

| File | Used By | API URL |
|------|---------|---------|
| `environment.ts` | Frontend dev (ng serve) | `http://localhost:44369/api/` |
| `environment.local.ts` | Full-stack local (npm start) | `https://localhost:5001/api/` |
| `environment.container.ts` | Backend dev (containerized) | `http://localhost:44369/api/` |

**Template Pattern:** Files containing secrets use a template pattern:
- `*.template.ts` - Tracked in git, contains placeholder `REPLACE_WITH_YOUR_KEY`
- `*.ts` - Gitignored, created locally by copying template and adding real keys

The correct file is loaded based on Angular configuration:
- `ng serve` → uses `environment.ts`
- `npm start` → uses `environment.local.ts` (via `--configuration=local`)
- Container → uses `environment.container.ts` (via `--configuration=container`)

### User Secrets

Sensitive data (API keys) are stored in .NET User Secrets:
- **Location:**
  - Windows: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
  - macOS/Linux: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`
- **UserSecretsId:** `56ee6ee3-b7d8-4759-ab17-87297accef46` (defined in `Api/Api.csproj` and `docker-compose.dcproj`)
- **Mounted in containers:** Via volume in `docker-compose.override.yml` (copy from `docker-compose.override.example.yml`)
- **NOT in git:** Never committed to repository

### Docker Compose Profiles

Profiles control which services start:

| Profile | Services Started | Command |
|---------|------------------|---------|
| (none) | api, dbserver, redis, aspire-dashboard | `docker-compose up` |
| `backend-dev` | api, angular, dbserver, redis, aspire-dashboard | `docker-compose --profile backend-dev up` |

### Observability with Aspire Dashboard

The Aspire Dashboard provides a unified view of telemetry data from the API:

- **Traces**: View distributed traces for API requests
- **Logs**: Structured logs from the application
- **Metrics**: Runtime, process, and ASP.NET Core metrics

**Access the dashboard:** http://localhost:18888

The API automatically sends OpenTelemetry data to the Aspire Dashboard via OTLP (gRPC) on port 18889. No additional configuration is needed when running with Docker Compose.

**For local development (Full-Stack workflow):** When running the API locally in Visual Studio, ensure `OPEN_TELEMETRY:ENDPOINT` is set to `http://localhost:18889` in `appsettings.Development.json` and start the Aspire Dashboard container:

```bash
docker-compose up aspire-dashboard
```

### Local Logging with Seq (Optional)

[Seq](https://datalust.co/seq) provides a powerful structured log viewer for local development. It's configured via `docker-compose.override.yml`.

**Setup:**

Create a `docker-compose.override.yml` file in the repository root with the following content:

```yaml
version: '3.4'

services:
  api:
    environment:
      - SEQ__ENDPOINT=http://seq:5341

  seq:
    image: datalust/seq:latest
    ports:
      - "5341:5341"  # Ingestion API
      - "8081:80"    # Web UI
    environment:
      - ACCEPT_EULA=Y
    volumes:
      - .data/seq-data:/data
    networks:
      - api-network
```

**Access Seq:** http://localhost:8081

**How it works:**
- Seq is automatically started when you run `docker-compose up` (override file is merged automatically)
- The API sends Serilog logs to Seq when `SEQ:ENDPOINT` is configured
- Only enabled in Development environment for safety
- Log data is persisted in `.data/seq-data/`

**For local development (Full-Stack workflow):** When running the API locally in Visual Studio, add to `appsettings.Development.json`:

```json
{
  "SEQ": {
    "ENDPOINT": "http://localhost:5341"
  }
}
```

Then start the Seq container:

```bash
docker-compose up seq
```

**Note:** `docker-compose.override.yml` is gitignored, so this setup remains personal and won't affect other developers.

---

## Common Commands

### Starting Services

```bash
# Frontend dev: API + infrastructure
docker-compose up api dbserver redis

# Backend dev: Everything
docker-compose --profile backend-dev up

# Full-stack local: Infrastructure + observability
docker-compose up dbserver redis aspire-dashboard

# Start in background (detached)
docker-compose up -d dbserver redis
```

### Stopping Services

```bash
# Stop all containers (Ctrl+C if in foreground)
docker-compose down

# Stop and remove all data (clean slate)
docker-compose down -v

# Stop specific service
docker-compose stop api
```

### Building Images

```bash
# Build all images
docker-compose build

# Build specific service
docker-compose build api
docker-compose build angular

# Build and start
docker-compose up --build
```

### Viewing Logs

```bash
# All services
docker-compose logs

# Specific service
docker-compose logs api
docker-compose logs angular
docker-compose logs dbserver
docker-compose logs aspire-dashboard

# Follow logs (real-time)
docker-compose logs -f api

# Last 100 lines
docker-compose logs --tail=100 api
```

### Inspecting Containers

```bash
# List running containers
docker-compose ps

# Get into a container shell
docker exec -it <container-name> sh

# Example: Get into API container
docker ps  # Find container name
docker exec -it <api-container-name> sh

# Inside container, you can:
ls -la
env  # View environment variables
exit  # Leave container
```

### Database

```bash
# Start only database
docker-compose up -d dbserver

# Check if ready
docker-compose logs dbserver | grep "ready for client connections"

# Connect with SQL tools
# Server: localhost,1433
# User: sa
# Password: Strong!Passw0rd
```

### Rebuilding Everything

```bash
# Nuclear option - clean slate
docker-compose down -v
docker-compose build --no-cache
docker-compose up
```

---

## Quick Start Cheat Sheet

```bash
# First time setup
cd Api && dotnet user-secrets init && cd ..
cd client && nvm use 12.22.12 && npm install && cd ..
cd client/src/environments && cp environment.local.template.ts environment.local.ts && cp environment.container.template.ts environment.container.ts && cd ../../..
# Edit the .ts files and add your Stripe publishable key

# Frontend Developer
docker-compose up api dbserver redis
cd client && ng serve

# Backend Developer (with debugging)
docker-compose up angular dbserver redis
# Then: F5 in Visual Studio (docker-compose project)

# Full-Stack Developer
docker-compose up dbserver redis aspire-dashboard
# Then: F5 in VS (Local Development profile)
cd client && npm start

# View traces/logs/metrics
# Open http://localhost:18888

# Stop everything
docker-compose down
```
