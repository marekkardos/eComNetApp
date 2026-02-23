# Claude Code Review Tools

Modular code review system with lazy-loaded skills for token efficiency.

## Table of Contents

- [Target Stack](#target-stack)
- [Architecture](#architecture)
- [How It Works](#how-it-works)
- [Setup](#setup)
- [Commands — quick reference](#commands)
- [Practical Examples](#practical-examples)
  - [diff-review — what exactly gets reviewed?](#diff-review--what-exactly-gets-reviewed)
  - [Reviewing changes between two branches](#reviewing-changes-between-two-branches)
  - [review-client / review-api — scope reference](#review-client--review-api--scope-reference)
  - [sonar-analyze — when to use it](#sonar-analyze--when-to-use-it)
  - [review-solution — full-stack integration review](#review-solution--full-stack-integration-review)
- [Skills Reference](#skills-reference)
- [SonarQube CLI](#sonarqube-cli)
  - [Prerequisites](#prerequisites)
- [Troubleshooting](#troubleshooting)

---

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

---

## Practical Examples

### diff-review — what exactly gets reviewed?

`/diff-review` is the **fastest, lightest check** — no file output, no skills loaded, answer straight in chat.

It runs one of two `git diff` commands depending on your argument:

| Argument | Git command executed | What you see |
|----------|----------------------|--------------|
| `staged` | `git diff --staged .` | Only files you have already `git add`-ed |
| *(anything else)* | `git diff .` | All unstaged edits in the working tree |

It checks **only** these five things in the changed lines:
1. Obvious bugs introduced by new/changed code
2. Security issues (hardcoded secrets, missing auth checks, injection risks)
3. Null / undefined safety problems
4. Resource leaks — unsubscribed Observables, undisposed `IDisposable` objects
5. Breaking changes to public APIs or contracts

Output is a compact inline report:
```
## Diff Review
Files: 3 | +lines: 47 | -lines: 12

### Issues
- [src/app/account/account.service.ts:82] CRITICAL - subscription not unsubscribed
- [src/API/Controllers/ExternalAuthController.cs:34] WARN - token not validated before use

### Suggestions
- [src/app/account/login/login.component.ts:19] extract magic string to constant

### Verdict: Needs fixes ⚠
```

Use it as a **pre-commit sanity check**, not a full review.

---

### Reviewing changes between two branches

The built-in commands (`changes`, `staged`) always diff against your **working tree or index**.
To compare two branches, stage the diff first or pass the branch range directly as free-text:

#### Option A — quick read-only comparison (recommended)

```bash
# From src/client/ — review everything that feature branch adds over master
/diff-review feature/social-login-google vs master
```
The agent receives the full argument text and will run:
```bash
git diff master...feature/social-login-google -- .
```

```bash
# From src/API/
/diff-review feature/social-login-google vs master
```

#### Option B — full review of a feature branch vs master

```bash
# From src/client/
/review-client changes on feature/social-login-google vs master

# From src/API/
/review-api changes on feature/social-login-google vs master

# From repo root (both projects at once)
/review-solution changes on feature/social-login-google vs master
```

The agent interprets the extra text and runs `git diff master...feature/social-login-google -- <project path>`.

#### Option C — manual git diff piped to a review

If you need full control over the diff range, produce the diff yourself, then ask Claude to review it:

```bash
# In terminal
git diff master...feature/social-login-google -- src/client/ > /tmp/client.diff
git diff master...feature/social-login-google -- src/API/    > /tmp/api.diff
```
Then in Claude Code chat:
```
Review the diff in /tmp/client.diff for Angular 9 issues.
Review the diff in /tmp/api.diff for .NET 8 / Clean Architecture issues.
```

---

### review-client / review-api — scope reference

| Argument | Scope |
|----------|-------|
| `changes` | `git diff .` — unstaged edits in working tree |
| `staged` | `git diff --staged .` — staged edits only |
| `with sonar` | same as `changes` **plus** full SonarQube scan |
| *(no argument)* | scans entire `src/app/` (client) or all layers (API) |

Skills are loaded **on demand** — only when matching code patterns are found:

**Client skills triggered by:**
- Component / NgModule / DI code → `angular.md`
- Observable / subscribe / pipe → `rxjs.md`
- HTTP calls / auth / forms → `security-web.md`
- Large lists / loops → `performance.md`

**API skills triggered by:**
- async/await / Task → `dotnet.md`
- DbContext / Include / LINQ → `ef-core.md`
- Layer references / DI → `clean-architecture.md`
- Controllers / HTTP verbs → `api-design.md`
- Auth / input validation → `security-web.md`
- Loops / caching → `performance.md`

Output is written to `code-review-report.md` in the project directory.

---

### sonar-analyze — when to use it

Use `/sonar-analyze` when you want **code-level fix guidance** for every SonarQube issue, not just a list.

```bash
# From src/client/ — scan Angular app and generate fix guide
/sonar-analyze

# From src/API/ — scan .NET API and generate fix guide
/sonar-analyze
```

What it does:
1. Runs `node ../sonar-fetch.mjs` (builds project, submits to SonarQube, fetches issues)
2. For every issue: opens the exact file/line, explains the real-world impact, shows before/after code
3. Writes `sonar-remediation.md` with effort estimate and systemic pattern summary

Typical output summary:
```
Remediation guide complete: 12 issues documented. See sonar-remediation.md
```

To skip a fresh build and only fetch already-existing issues:
```bash
node ../sonar-fetch.mjs --fetch-only
# then ask: "Review sonar-issues.md and suggest fixes"
```

---

### review-solution — full-stack integration review

Run from the **repo root** when you want Angular + API reviewed together, including:
- DTO / model contract mismatches between client and API
- HTTP endpoint calls that don't match controller routes
- Inconsistent error handling across the stack
- End-to-end auth flow correctness

```bash
# Repo root — review all uncommitted changes across both projects
/review-solution changes

# Repo root — full scan of both projects + SonarQube for both
/review-solution with sonar
```

Output goes to `code-review-report.md` in the repo root.

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
