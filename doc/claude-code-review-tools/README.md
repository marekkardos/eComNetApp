# Claude Code Review Tools

Modular code review system with lazy-loaded skills for token efficiency.

## Target Stack

| Project | Technology |
|---------|------------|
| `src/client` | **Angular 9** (Ivy) + RxJS 6.x |
| `src/API` | .NET 8 + Clean Architecture |

*Note: Angular skills are for Angular 9. A future Angular 19/20 app will need separate skills.*

## Architecture

```
your-repo/
├── src/
│   ├── sonar-fetch.mjs            # Shared analyzer script
│   ├── client/                    # Angular 9 app
│   │   └── .sonarqube.json        # Client SonarQube config
│   └── API/                       # .NET API
│       └── .sonarqube.json        # API SonarQube config
└── .claude/
    ├── commands/                  # Agents (Task-based, isolated)
    │   ├── review-client.md       # → spawns Angular review agent
    │   ├── review-api.md          # → spawns .NET review agent
    │   ├── review-solution.md     # → spawns full-stack agent
    │   ├── sonar-analyze.md       # → spawns remediation agent
    │   └── diff-review.md         # → spawns quick diff agent
    └── skills/                    # Knowledge (lazy-loaded by agents)
        ├── angular.md             # Angular 9 patterns
        ├── rxjs.md                # RxJS 6.x patterns
        ├── dotnet.md              # .NET 8 / C# patterns
        ├── clean-architecture.md  # Layer rules
        ├── ef-core.md             # EF Core queries
        ├── security-web.md        # Web security
        ├── api-design.md          # REST API design
        └── performance.md         # Performance patterns
```

## How It Works

**Agents** = independent subagents spawned via Task tool with fresh context
**Skills** = focused knowledge loaded only when needed

```
/review-api changes
    │
    ├─▶ Spawns fresh subagent (isolated context)
    ├─▶ Scans src/API/ for changes
    ├─▶ Finds async code → loads dotnet.md
    ├─▶ Finds DbContext → loads ef-core.md
    ├─▶ Skips angular.md, rxjs.md (not relevant)
    ├─▶ Outputs report
    └─▶ Returns only: "Review complete: 2 critical, 5 improvements"
```

Benefits of Task-based agents:
- **Fresh context**: No pollution from main conversation
- **Token efficient**: Skills loaded only when needed
- **Parallel ready**: Multiple agents could run simultaneously
- **Clean returns**: Only summary comes back to main session

## Setup

1. Extract to your repo root

2. Configure SonarQube for each project:
   ```bash
   # API
   cp src/API/.sonarqube.json.example src/API/.sonarqube.json
   # Edit with your token and project key
   
   # Client  
   cp src/client/.sonarqube.json.example src/client/.sonarqube.json
   # Edit with your token and project key
   ```

3. Add to `.gitignore`:
   ```
   src/**/.sonarqube.json
   **/sonar-issues.md
   **/sonar-remediation.md
   **/code-review-report.md
   ```

## Commands

| From directory | Command | What it does |
|----------------|---------|--------------|
| `src/client/` | `/review-client changes` | Review Angular changes |
| `src/client/` | `/review-client with sonar` | Run SonarQube scan → review with findings |
| `src/API/` | `/review-api changes` | Review .NET changes |
| `src/API/` | `/review-api with sonar` | Run SonarQube scan → review with findings |
| repo root | `/review-solution` | Review entire monorepo |
| `src/API/` or `src/client/` | `/sonar-analyze` | Run scan → generate fix guide |
| `src/API/` or `src/client/` | `/diff-review staged` | Quick staged changes check |

**Note:** "with sonar" runs full analysis (build + scan + fetch), not just fetch existing issues.

## Skills Reference

| Skill | Version | Loaded when... |
|-------|---------|----------------|
| `angular.md` | Angular 9 | Components, modules, DI patterns |
| `rxjs.md` | RxJS 6.x | Observables, subscribe, pipe operators |
| `dotnet.md` | .NET 8 | Async/await, C# patterns, null safety |
| `clean-architecture.md` | - | Layer violations, dependency direction |
| `ef-core.md` | EF Core 8 | DbContext, queries, Include patterns |
| `security-web.md` | - | Auth, input validation, XSS/CSRF |
| `api-design.md` | - | Controllers, HTTP methods, status codes |
| `performance.md` | - | Allocations, caching, N+1 queries |

## SonarQube CLI

The script automatically detects project type and runs the appropriate scanner:

| Project Type | Detection | Scanner Used |
|--------------|-----------|--------------|
| .NET | `*.csproj` or `*.sln` | `dotnet sonarscanner` |
| Node/Angular | `package.json` | `sonar-scanner` |
| Generic | `sonar-project.properties` | `sonar-scanner` |

```powershell
# Full analysis + fetch (default)
cd src/API
node ../sonar-fetch.mjs

# Skip build (if already built)
node ../sonar-fetch.mjs --skip-build

# Just fetch existing issues (no new analysis)
node ../sonar-fetch.mjs --fetch-only
```

### Prerequisites

**DOCKER:**

Create the volumes with the following commands:
```
docker volume create --name sonarqube_data
docker volume create --name sonarqube_logs
docker volume create --name sonarqube_extensions
```

Starting the container by using docker run:
```
docker run -d --name sonarqube \
    -p 9000:9000 \
    -v sonarqube_data:/opt/sonarqube/data \
    -v sonarqube_extensions:/opt/sonarqube/extensions \
    -v sonarqube_logs:/opt/sonarqube/logs \
    sonarqube:latest
```
Once your server is installed and running, you can access SonarQube Server UI in your 
web browser (the default system administrator credentials are admin/admin) and you’re 
ready to begin Project analysis setup.

**Sonarqube - Project analysis setup:**

Create a local projects 'ecomnetapp-client', 'ecomnetapp-api' and generate project tokens.

src/API/.sonarqube.json
src/client/.sonarqube.json
   # Edit with your token and project keys

**.NET projects:**
```powershell
dotnet tool install --global dotnet-sonarscanner
```

**Node/Angular projects:**
```powershell
npm install -g sonarqube-scanner
# or download from https://docs.sonarqube.org/latest/analysis/scan/sonarscanner/
```

## Troubleshooting

**"Project key required"**
- Set in `src/.sonarqube.json` or use `--project` flag

**Skills not loading**
- Verify `.claude/skills/` directory exists
- Check skill filename matches exactly

**SonarQube connection failed**
- Verify Docker container running: `docker ps | grep sonar`
- Test API: `curl http://localhost:9000/api/system/status`
