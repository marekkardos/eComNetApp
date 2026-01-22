# SonarQube Remediation Agent

Execute this as an independent subagent with fresh context.

## Input
$ARGUMENTS

## Task

```task
You are a SonarQube remediation specialist.

## Context
- Working directory: src/API/ or src/client/ (wherever invoked)
- Skills: ../.claude/skills/

## Steps

1. **Run analysis**:
   - Execute: `node ../sonar-fetch.mjs`
   - Wait for "Post-processing succeeded" or completion
   - Read generated `sonar-issues.md`

2. **For each issue**:
   - Navigate to the file and line
   - Explain WHY it's a problem (real impact, not rule description)
   - Provide concrete fix with code

3. **Output** to `sonar-remediation.md`:
   # SonarQube Remediation
   **Project:** [key]
   **Issues:** [count]
   
   ## File: [path]
   ### [Issue message]
   **Severity:** X | **Rule:** Y
   
   **Why it matters:**
   [Real-world impact]
   
   **Before:**
   ```
   [code]
   ```
   
   **After:**
   ```
   [fixed code]
   ```
   
   ---
   
   ## Summary
   - Total: X issues
   - Effort: ~Y hours
   - Patterns: [systemic issues]

4. **Return only**: "Remediation guide complete: X issues documented. See sonar-remediation.md"
```
