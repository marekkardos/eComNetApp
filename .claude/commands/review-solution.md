# Full Solution Review Agent

Execute this as an independent subagent with fresh context.

## Input
$ARGUMENTS

## Task

```task
You are a full-stack code review agent for a monorepo.

## Context
- Working directory: repo root
- Projects: src/client/ (Angular 9), src/API/ (.NET 8)
- Skills: .claude/skills/

## Steps

1. **SonarQube** (only if "$ARGUMENTS" contains "sonar"):
   - Run: `cd src/client && node ../sonar-fetch.mjs && cd ../..`
   - Run: `cd src/API && node ../sonar-fetch.mjs && cd ../..`
   - Read both `src/client/sonar-issues.md` and `src/API/sonar-issues.md`

2. **Scope**:
   - "changes" → `git diff src/`
   - else → key files in both projects

3. **Phase 1: Client** (src/client/)
   Load on demand:
   - `.claude/skills/angular.md`
   - `.claude/skills/rxjs.md`
   Remember: Angular 9 only, no modern features

4. **Phase 2: API** (src/API/)
   Load on demand:
   - `.claude/skills/dotnet.md`
   - `.claude/skills/clean-architecture.md`
   - `.claude/skills/ef-core.md`
   - `.claude/skills/api-design.md`

5. **Phase 3: Cross-cutting**
   Load for both:
   - `.claude/skills/security-web.md`
   - `.claude/skills/performance.md`

6. **Phase 4: Integration** (no skill needed)
   - API DTOs match client models?
   - Endpoints match HTTP service calls?
   - Error handling consistent?
   - Auth flow end-to-end?

7. **Output** to `code-review-report.md`:
   # Solution Review
   ## Executive Summary
   ## Client (Angular 9)
   ### Critical
   ### Improvements
   ## API (.NET 8)
   ### Critical
   ### Improvements
   ## Integration Concerns
   ## Skills Applied

8. **Return only**: "Review complete: X critical (client), Y critical (API), Z integration. See code-review-report.md"
```
