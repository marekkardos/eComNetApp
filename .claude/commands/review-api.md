# .NET API Review Agent

Execute this as an independent subagent with fresh context.

## Input
$ARGUMENTS

## Task

```task
You are a .NET 8 API code review agent.

## Context
- Working directory: src/API/
- Project: .NET 8 REST API with Clean Architecture
- Skills: ../.claude/skills/

## Steps

1. **SonarQube** (only if "$ARGUMENTS" contains "sonar"):
   - Run: `node ../sonar-fetch.mjs`
   - Wait for completion
   - Read `sonar-issues.md`

2. **Scope**:
   - "changes" → `git diff .`
   - "staged" → `git diff --staged .`
   - else → scan Domain/, Application/, Infrastructure/, API/

3. **Load skills ON DEMAND** (read only when relevant code found):
   | Code pattern | Load skill |
   |--------------|------------|
   | async/await, Task | `../.claude/skills/dotnet.md` |
   | DbContext, Include, queries | `../.claude/skills/ef-core.md` |
   | Layer references, DI | `../.claude/skills/clean-architecture.md` |
   | Controllers, HTTP | `../.claude/skills/api-design.md` |
   | Auth, validation | `../.claude/skills/security-web.md` |
   | Loops, caching | `../.claude/skills/performance.md` |

4. **Review** code applying loaded skill criteria

5. **Output** to `code-review-report.md`:
   # API Code Review
   ## Summary
   ## Critical Issues
   ## Improvements  
   ## Architecture Notes
   ## Skills Applied

6. **Return only**: "Review complete: X critical, Y improvements. See code-review-report.md"
```
