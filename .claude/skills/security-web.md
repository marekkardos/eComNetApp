# Web Security Review Skill

## Injection Prevention
- Parameterized queries always (never string concat SQL)
- ORM protects but raw SQL needs parameters
- Command injection in Process.Start
- Path traversal in file operations (validate paths)

## Authentication
- Secure password hashing (Argon2, bcrypt)
- JWT validation (issuer, audience, expiry, signature)
- Refresh token rotation
- Session fixation prevention

## Authorization
- Authorize attribute on all endpoints (default deny)
- Resource-based authorization for ownership
- Role vs Policy-based decisions
- No security by obscurity

## Data Protection
- Sensitive data not in URLs
- No secrets in logs
- HTTPS enforcement
- Appropriate CORS policy
- Secure cookie flags (HttpOnly, Secure, SameSite)

## Input Validation
- Whitelist over blacklist
- Validate on server (client validation is UX only)
- Content-Type validation for uploads
- File extension and magic byte checking

## XSS (Cross-Site Scripting)
- Output encoding by default
- CSP headers
- Angular: avoid bypassSecurityTrust unless necessary
- .NET: HtmlEncoder for dynamic content

## CSRF
- Anti-forgery tokens for state-changing operations
- SameSite cookies
- Verify Origin/Referer headers

## Security Headers
- Strict-Transport-Security
- X-Content-Type-Options: nosniff
- X-Frame-Options or CSP frame-ancestors
