# eComNetApp — Feature Roadmap

This document tracks planned and upcoming features for the eComNetApp platform.

---

## Planned Features

### 1. Two-Factor Authentication (2FA)

Add an optional second authentication factor for registered users.

**Scope**
- TOTP-based 2FA (e.g. Google Authenticator, Authy)
- Enable/disable 2FA from user account settings
- Recovery codes generation and management
- API: new endpoints for 2FA setup, verification, and recovery
- Client: setup wizard, verification step during login

**Notes**
- Should integrate with the existing JWT + refresh-token auth flow
- See [AuthFlow.md](Authentication/AuthFlow.md) for current auth details

---

### 2. Password Management for Social-Login Accounts

Allow users who registered via Google (or another social provider) to set or change a local password, enabling hybrid login.

**Scope**
- Detect accounts that have no local password (social-only accounts)
- "Set password" flow in account settings for social-only users
- "Change password" flow for accounts that already have a password
- API: endpoint to add/update password for an external-auth account
- Client: account settings section for password management

**Notes**
- Must not break existing email/password login for users who have both
- See [GoogleAuthFlow.md](Social%20Login%20Integration/GoogleAuthFlow.md) for current Google login details

---

### 3. Email Confirmation for New Accounts

Require new users to confirm their email address before the account is fully activated.

**Scope**
- Send a confirmation email with a unique token on registration
- API: confirmation endpoint that validates the token and activates the account
- Block login (or restrict access) for unconfirmed accounts, with a clear error message
- Resend confirmation email option in the UI
- Expiry and regeneration of confirmation tokens

**Notes**
- Choose an email provider/service (e.g. SendGrid, SMTP relay) during implementation
- Social-login accounts can be treated as pre-confirmed (email verified by provider)

---

### 4. Link/Unlink Management for Social-Login Accounts

Allow users to link additional social providers to their existing account and unlink providers they no longer want to use.

**Scope**
- Account settings page listing all currently linked providers (e.g. Google, Facebook)
- "Link" action: initiate OAuth flow and attach the provider to the current account
- "Unlink" action: remove a social provider from the account
- Guard: prevent unlinking the last login method when no local password is set (would lock the user out)
- API: endpoints to list linked providers, link a new provider, and unlink an existing one
- Client: connected accounts section in user profile/settings

**Notes**
- Depends on Password Management feature (#2) — unlinking should be blocked unless the user has a local password or another provider linked
- Reuses the external-auth pipeline from the Google integration
- See [GoogleAuthFlow.md](Social%20Login%20Integration/GoogleAuthFlow.md) for the current provider-linking approach

---

### 5. Facebook Social Login

Add Facebook as a supported OAuth 2.0 / OpenID Connect social login provider, following the same pattern as the existing Google integration.

**Scope**
- Register a Facebook App and obtain App ID / App Secret
- API: extend `ExternalAuthController` to handle Facebook tokens
- Client: "Continue with Facebook" button on login/register pages
- Link Facebook account to an existing account (same email)
- Account settings: connect / disconnect Facebook

**Notes**
- Reuse the generic external-auth pipeline already in place for Google
- See [GoogleAuthFlow.md](Social%20Login%20Integration/GoogleAuthFlow.md) for the pattern to follow
- Facebook SDK or OAuth redirect flow — to be decided during implementation

---

## Status Legend

| Status | Meaning |
|--------|---------|
| Planned | Defined, not yet started |
| In Progress | Actively being developed |
| Done | Merged to main branch |

| Feature | Status |
|---------|--------|
| Two-Factor Authentication | Planned |
| Password management for social accounts | Planned |
| Email confirmation for new accounts | Planned |
| Link/Unlink social accounts | Planned |
| Facebook Login | Planned |
