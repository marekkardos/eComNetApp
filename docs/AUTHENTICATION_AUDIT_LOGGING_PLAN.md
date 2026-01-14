# Authentication Audit Logging Implementation Plan

## Summary

Implement comprehensive authentication audit logging using Serilog with a dual approach:
- **Critical audit events**: Use `AuditTo.Seq()` for events that must succeed (throws on failure, synchronous writes)
- **Monitoring events**: Use regular Serilog with `WriteTo.Seq()` for operational monitoring (batched, async)

Both critical and standard events flow to Seq, but critical events use the audit sink which guarantees delivery by throwing exceptions on failure and using synchronous network calls.

This follows the principle that certain authentication events (lockouts, token reuse detection, failed logins) are so critical that logging failures must be immediately visible rather than silent.

---

## Architecture Overview

```
Authentication Event
    |
    v
IAuthEventsLog service
    |
    v
Is it critical? (lockout, token reuse, brute force)
    |
    +--Yes--> AuditTo.Seq() [must succeed, throws on failure, synchronous]
    |
    +--No --> WriteTo.Seq() [standard logging, batched, async]
```

**Critical Events** (use audit logger via `AuditTo.Seq`):
- Account lockout triggered
- Token reuse detected (security breach)
- Multiple failed login attempts (brute force pattern)
- All tokens revoked (security response)

**Standard Events** (use regular logger via `WriteTo.Seq`):
- Successful login/logout
- Token refresh
- Registration
- Failed login attempts (below threshold)
- General authentication flow

---

## Package Requirements

### NuGet Packages

The `Serilog.Sinks.Seq` package supports both `WriteTo.Seq()` and `AuditTo.Seq()` - no additional packages needed for audit functionality.

**Already in `Directory.Packages.props`:**
```xml
<PackageVersion Include="Serilog.Sinks.Seq" Version="9.0.0" />
<PackageVersion Include="Serilog.Enrichers.ClientInfo" Version="2.1.2" />
```

**Already in `Api/Api.csproj`:**
```xml
<PackageReference Include="Serilog.Sinks.Seq" />
<PackageReference Include="Serilog.Enrichers.ClientInfo" />
```

---

## Configuration Changes

### 1. Update `Api/Program.cs`

Configure separate audit logger using `AuditTo.Seq()` alongside existing Serilog setup:

```csharp
// Create dedicated audit logger for critical security events
// Uses AuditTo.Seq() which:
// - Throws exceptions on write failure (guaranteed delivery)
// - Sends events synchronously (blocking network calls)
// - Should only be used for critical security events due to performance impact
var auditLogger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "eComNetApp")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .Enrich.WithProperty("LogType", "SecurityAudit")
    .AuditTo.Seq(
        serverUrl: builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341",
        apiKey: builder.Configuration["Seq:ApiKey"]
    )
    .CreateLogger();

// Register audit logger in DI container for injection into IAuthEventsLog
builder.Services.AddSingleton(auditLogger);

// Keep existing Serilog configuration for standard logging
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithClientIp()
    .WriteTo.Console()
    .WriteTo.Seq(
        serverUrl: ctx.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341",
        apiKey: ctx.Configuration["Seq:ApiKey"]
    )
);
```

### 2. Update `Api/appsettings.Development.json`

Add Seq configuration section:

```json
{
  "Seq": {
    "ServerUrl": "http://localhost:5341",
    "ApiKey": null
  }
}
```

---

## Code Changes

### Architecture: IAuthEventsLog Service

The implementation uses a dedicated `IAuthEventsLog` service to separate audit logging concerns from business logic. This provides:
- Clean separation of concerns
- Easy testability (mock the interface)
- Centralized logging logic
- Consistent event formatting

### 1. Interface `Api/Identity/IAuthEventsLog.cs`

```csharp
public interface IAuthEventsLog
{
    // Critical events - written to audit logger (AuditTo.Seq - must succeed)
    void AccountLockout(string userId, string email, string ipAddress);
    void FailedLoginThreshold(string userId, string email, string ipAddress, int failedAttempts);
    void TokenReuseDetected(string userId, string tokenId, DateTime revokedAt);
    void AllTokensRevoked(string userId);

    // Monitoring events - written to standard logger (WriteTo.Seq)
    void Monitor_SuccessfulLogin(string userId, string email, string ipAddress);
    void Monitor_UserRegistration(string userId, string email, string ipAddress);
    void Monitor_TokenRefresh(string userId, string email, string ipAddress);
    void Monitor_UserLogout(string userId, string ipAddress);
    void Monitor_LoginAttemptNonExistent(string email, string ipAddress);
    void Monitor_LoginAttemptFailed(string userId, string email, string ipAddress, int failedAttempts);
}
```

### 2. Implementation `Api/Identity/AuthEventsLog.cs`

```csharp
public class AuthEventsLog(
    Serilog.ILogger auditLogger,
    ILogger<AuthEventsLog> logger) : IAuthEventsLog
{
    // Critical events - use audit logger (throws on failure)
    public void AccountLockout(string userId, string email, string ipAddress)
    {
        try
        {
            using (LogContext.PushProperty("EventType", "AccountLockout"))
            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("Email", email))
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                auditLogger.Warning(
                    "SECURITY: Account locked for user {Email} (ID: {UserId}) from IP {IpAddress}",
                    email, userId, ipAddress);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CRITICAL: Audit logging failed for account lockout. User: {Email}", email);
            throw;
        }
    }

    // ... other critical events follow same pattern ...

    // Monitoring events - use standard logger (async, batched)
    public void Monitor_SuccessfulLogin(string userId, string email, string ipAddress)
    {
        using (LogContext.PushProperty("EventType", "SuccessfulLogin"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("IpAddress", ipAddress))
        {
            logger.LogInformation("User {Email} logged in successfully from IP {IpAddress}", email, ipAddress);
        }
    }

    // ... other monitoring events follow same pattern ...
}
```

### 3. Usage in `AccountController.cs`

```csharp
public class AccountController(
    // ... other dependencies ...
    IAuthEventsLog authEventsLog,
    IOptions<LockoutSettings> lockoutSettings) : BaseApiController
{
    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await userManager.FindByEmailAsync(loginDto.Email);
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (user == null)
        {
            authEventsLog.Monitor_LoginAttemptNonExistent(loginDto.Email, clientIp);
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized));
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            authEventsLog.AccountLockout(user.Id, user.Email, clientIp);  // Critical - audit logger
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized, "Account locked..."));
        }

        if (!result.Succeeded)
        {
            var failedCount = await userManager.GetAccessFailedCountAsync(user);
            if (failedCount >= _lockoutSettings.MaxFailedAccessAttempts - 1)
            {
                authEventsLog.FailedLoginThreshold(user.Id, user.Email, clientIp, failedCount);  // Critical
            }
            else
            {
                authEventsLog.Monitor_LoginAttemptFailed(user.Id, user.Email, clientIp, failedCount);  // Standard
            }
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized));
        }

        authEventsLog.Monitor_SuccessfulLogin(user.Id, user.Email, clientIp);  // Standard
        return await GenerateAuthResponseAsync(user);
    }
}
```

### 4. Usage in `RefreshTokenService.cs`

```csharp
public class RefreshTokenService(
    AppIdentityDbContext context,
    IOptions<TokenSettings> tokenSettings,
    ILogger<RefreshTokenService> logger,
    IAuthEventsLog authEventsLog) : IRefreshTokenService
{
    public async Task<RefreshToken> ValidateRefreshTokenAsync(string token)
    {
        // ... token lookup ...

        if (refreshToken.IsRevoked)
        {
            // CRITICAL: Token reuse detected - security breach!
            authEventsLog.TokenReuseDetected(refreshToken.AppUserId, refreshToken.Id.ToString(), refreshToken.RevokedAt ?? DateTime.UtcNow);
            await RevokeAllUserTokensAsync(refreshToken.AppUserId);
            authEventsLog.AllTokensRevoked(refreshToken.AppUserId);
            return null;
        }

        // ... rest of validation ...
    }
}
```

---

## Seq Queries for Monitoring

### Query 1: Recent Account Lockouts
```
EventType = "AccountLockout" AND @Timestamp > Now() - 1h
```

### Query 2: Brute Force Patterns (Multiple Failed Logins)
```
EventType = "FailedLoginThreshold"
| group by IpAddress
| where count(*) > 5
```

### Query 3: Token Reuse Detection (Critical!)
```
EventType = "TokenReuseDetected"
```

### Query 4: Successful Logins by User
```
EventType = "SuccessfulLogin"
| group by Email
| where @Timestamp > Now() - 24h
```

### Query 5: All Security Audit Events
```
LogType = "SecurityAudit"
```

### Query 6: Geographic Anomalies (if IP enrichment added)
```
EventType = "SuccessfulLogin"
| group by UserId, IpAddress
| having distinct(Country) > 1
```

---

## Security Event Hierarchy

### Critical (Audit Logger - AuditTo.Seq)
- `EventType: "AccountLockout"` - User locked out due to failed attempts
- `EventType: "TokenReuseDetected"` - Revoked token reused (security breach)
- `EventType: "AllTokensRevoked"` - All user tokens revoked due to security issue
- `EventType: "FailedLoginThreshold"` - User approaching lockout threshold

### Standard (Regular Logger - WriteTo.Seq)
- `EventType: "SuccessfulLogin"` - User authenticated successfully
- `EventType: "UserRegistration"` - New account created
- `EventType: "TokenRefresh"` - Access token refreshed
- `EventType: "UserLogout"` - User logged out explicitly
- `EventType: "LoginAttemptFailed"` - Failed login (below threshold)
- `EventType: "LoginAttemptNonExistent"` - Login attempt for unknown email

---

## Testing the Implementation

### 1. Test Audit Logging Works

Trigger an account lockout:
```bash
# Attempt login with wrong password 5 times
curl -X POST http://localhost:44369/api/account/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"wrongpassword"}'
```

**Expected Result in Seq:**
- Event with `EventType = "AccountLockout"`
- `LogType = "SecurityAudit"`
- Contains UserId, Email, IpAddress properties

### 2. Test Token Reuse Detection

1. Login and capture refresh token cookie
2. Logout (revokes token)
3. Attempt to use revoked token to call /refresh

**Expected Result in Seq:**
- Event with `EventType = "TokenReuseDetected"` (Error level)
- Event with `EventType = "AllTokensRevoked"` (Warning level)

### 3. Test Audit Failure Handling

Stop the Seq server and trigger a lockout event:
- The API should throw an exception (audit logging must succeed)
- The exception should be visible in console logs

---

## Deployment Considerations

### Seq Server Requirements

1. **High Availability**: Consider Seq clustering for production
2. **Retention Policies**: Configure Seq retention for audit events (recommend 90+ days)
3. **Alerts**: Set up Seq alerts for critical events (TokenReuseDetected, AccountLockout)
4. **Backup**: Ensure Seq data is included in backup strategy

### Docker Deployment

Ensure Seq is accessible from the API container:
```yaml
services:
  api:
    environment:
      - Seq__ServerUrl=http://seq:5341
  seq:
    image: datalust/seq:latest
    ports:
      - "5341:80"
    volumes:
      - seq-data:/data
```

### Performance Considerations

`AuditTo.Seq()` uses synchronous, blocking network calls. This is intentional for critical events but has performance implications:
- Only use for truly critical security events
- Standard monitoring events should use `WriteTo.Seq()` (async, batched)
- The IAuthEventsLog abstraction ensures proper routing

### Compliance

Audit logs in Seq provide evidence for:
- GDPR Article 32 (Security of Processing) - logging security events
- PCI DSS Requirement 10 - track and monitor all access
- SOC 2 - logging and monitoring controls

---

## Implementation Order

1. **Verify NuGet packages** - Serilog.Sinks.Seq, Serilog.Enrichers.ClientInfo (already present)
2. **Update appsettings** - Add Seq configuration section
3. **Update Program.cs** - Configure audit logger with AuditTo.Seq
4. **Create IAuthEventsLog interface** - Define event methods
5. **Create AuthEventsLog implementation** - Implement with dual loggers
6. **Register in DI** - Add IAuthEventsLog to services
7. **Update AccountController** - Use IAuthEventsLog for all auth events
8. **Update RefreshTokenService** - Use IAuthEventsLog for token reuse detection
9. **Create Seq queries** - Set up monitoring dashboards and alerts
10. **Test locally** - Verify events appear in Seq with correct properties
11. **Deploy to staging** - Validate Seq connectivity and event flow
12. **Deploy to production** - With alerting enabled

---

## Success Criteria

- [ ] Audit logger configured with AuditTo.Seq in Program.cs
- [ ] IAuthEventsLog service implemented and registered
- [ ] Account lockout events visible in Seq with LogType="SecurityAudit"
- [ ] Token reuse detection logged with automatic revocation
- [ ] Failed login threshold warnings captured as audit events
- [ ] Audit logging failures throw exceptions (not silent)
- [ ] Standard authentication events visible in Seq
- [ ] Seq queries return expected results
- [ ] No significant performance degradation
- [ ] Alerting configured for critical events in Seq

---

## Future Enhancements

### Phase 2 (Post-Implementation)
- Add IP geolocation enrichment for anomaly detection
- Implement SIEM integration (forward from Seq to Splunk/ELK)
- Add alerting webhooks for critical events
- Add audit metrics dashboard in Seq
- Create automated response playbooks (e.g., auto-ban IP after threshold)

### Phase 3 (Advanced)
- Machine learning for anomaly detection
- Behavioral analytics (detect account takeover patterns)
- Integration with fraud detection systems
