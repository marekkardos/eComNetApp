---
name: code-review
description: Perform comprehensive code review with security and performance checks
user-invocable: true
---

# Code Review
When invoked, analyze the provided code for:
1. **Security vulnerabilities**
   - SQL injection risks
   - XSS attack vectors
   - Authentication issues
   - Sensitive data exposure
2. **Performance issues**
   - N+1 queries
   - Unnecessary loops
   - Memory leaks
   - Blocking operations
3. **Code quality**
   - Naming conventions
   - Function complexity
   - Error handling
   - Test coverage
4. **Best practices**
   - SOLID principles
   - DRY violations
   - Proper documentation
Provide:
- Priority level for each issue (Critical/High/Medium/Low)
- Specific line numbers
- Suggested fixes with code examples