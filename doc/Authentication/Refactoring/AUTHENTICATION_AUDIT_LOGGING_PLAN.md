# Authentication Audit Logging Implementation Plan

## Summary

Implement comprehensive authentication audit logging using Serilog with a dual approach:
- **Critical audit events**: Use `AuditTo.Seq()` for events that must succeed (throws on failure, synchronous writes)
- **Monitoring events**: Use regular Serilog with `WriteTo.Seq()` for operational monitoring (batched, async)

Both critical and standard events flow to Seq, but critical events use the audit sink which guarantees delivery by throwing exceptions on failure and using synchronous network calls.

All authentication events are tagged with `AuthEvent=true` for easy filtering in Seq, regardless of whether they are critical or standard events.

This follows the principle that certain authentication events (lockouts, token reuse detection, failed logins) are so critical that logging failures must be immediately visible rather than silent.

---

## Architecture Overview

```
Authentication Event
    |
    v
IAuthEventsLog service (adds AuthEvent=true property)
    |
    v
Is it critical? (lockout, token reuse, brute force)
    |
    +--Yes--> AuditTo.Seq() [Program.cs - must succeed, throws on failure, synchronous]
    |
    +--No --> WriteTo.Seq() [LoggingExtensions.cs - standard logging, batched, async]
```

**Logging Configuration:**
- `Program.cs` - Configures audit logger only (`AuditTo.Seq`)
- `LoggingExtensions.cs` - Configures standard logging (`WriteTo.Seq`, `WriteTo.Console`, OpenTelemetry)

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
- Login attempts for non-existent emails

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

Configure **only** the audit logger in Program.cs. Standard logging is handled by `LoggingExtensions.AddLogging()`.

```csharp
// Create dedicated audit logger for critical security events
// Uses AuditTo.Seq() which:
// - Throws exceptions on write failure (guaranteed delivery)
// - Sends events synchronously (blocking network calls)
// - Should only be used for critical security events due to performance impact
var auditLogger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "eComNetApp_API")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .Enrich.WithProperty("LogType", "SecurityAudit")
    .AuditTo.Seq(
        serverUrl: builder.Configuration["Seq:ServerUrl"]
                    ?? throw new InvalidOperationException("Seq:ServerUrl configuration is missing."),
        apiKey: builder.Configuration["Seq:ApiKey"]
    )
    .CreateLogger();

// Register audit logger in DI container for injection into IAuthEventsLog
builder.Services.AddSingleton(auditLogger);

// Standard logging is configured in LoggingExtensions.AddLogging()
Startup.ConfigureServices(builder.Services, builder.Configuration, builder.Environment);
```

### 2. Update `Api/StartupConfigurations/LoggingExtensions.cs`

Standard logging configuration with Seq always enabled:

```csharp
public static void AddLogging(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
{
    services.AddSerilog(log =>
    {
        log.Filter.ByExcluding("RequestPath like '%/health%'")
           .Filter.ByExcluding("RequestPath like '%/swagger%'");

        log.Enrich.WithSpan()
           .Enrich.FromLogContext()
           .Enrich.WithClientIp()
           .WriteTo.Console();

        // OpenTelemetry configuration...

        // Always sink to Seq for centralized logging and monitoring
        string seqServerUrl = configuration["Seq:ServerUrl"] ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(seqServerUrl))
        {
            log.WriteTo.Seq(
                serverUrl: seqServerUrl,
                apiKey: configuration["Seq:ApiKey"],
                restrictedToMinimumLevel: environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information
            );
        }
    });
}
```

### 3. Update `Api/appsettings.Development.json`

Seq configuration section (required):

```json
{
  "Seq": {
    "ServerUrl": "http://localhost:5341",
    "ApiKey": null
  }
}
```

### 4. Update `docker-compose.yml`

Seq is defined in the main docker-compose.yml (always available):

```yaml
services:
  api:
    environment:
      - Seq__ServerUrl=http://seq:5341
    depends_on:
      - seq

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

---

## Code Changes

### Architecture: IAuthEventsLog Service

The implementation uses a dedicated `IAuthEventsLog` service to separate audit logging concerns from business logic. This provides:
- Clean separation of concerns
- Easy testability (mock the interface)
- Centralized logging logic
- Consistent event formatting
- `AuthEvent=true` property on all events for easy filtering

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

All methods include `AuthEvent=true` property for easy filtering:

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
            using (LogContext.PushProperty("AuthEvent", true))
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

    // ... other critical events follow same pattern with AuthEvent=true ...

    // Monitoring events - use standard logger (async, batched)
    public void Monitor_SuccessfulLogin(string userId, string email, string ipAddress)
    {
        using (LogContext.PushProperty("AuthEvent", true))
        using (LogContext.PushProperty("EventType", "SuccessfulLogin"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("IpAddress", ipAddress))
        {
            logger.LogInformation("User {Email} logged in successfully from IP {IpAddress}", email, ipAddress);
        }
    }

    // ... other monitoring events follow same pattern with AuthEvent=true ...
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

### Query 1: All Authentication Events
```
AuthEvent = true
```

### Query 2: All Critical Security Audit Events
```
LogType = "SecurityAudit"
```

### Query 3: Recent Account Lockouts
```
AuthEvent = true and EventType = "AccountLockout" and @Timestamp > Now() - 1h
```

### Query 4: Brute Force Patterns (Multiple Failed Logins)
```
AuthEvent = true and EventType = "FailedLoginThreshold"
| group by IpAddress
| where count(*) > 5
```

### Query 5: Token Reuse Detection (Critical!)
```
AuthEvent = true and EventType = "TokenReuseDetected"
```

### Query 6: Successful Logins by User
```
AuthEvent = true and EventType = "SuccessfulLogin"
| group by Email
| where @Timestamp > Now() - 24h
```

### Query 7: All Failed Login Attempts
```
AuthEvent = true and (EventType = "LoginAttemptFailed" or EventType = "FailedLoginThreshold" or EventType = "LoginAttemptNonExistent")
```

### Query 8: Authentication Events by User
```
AuthEvent = true and UserId = "specific-user-id"
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

### Common Properties (All Events)
- `AuthEvent: true` - Identifies all authentication-related events
- `EventType: string` - Specific event type for filtering
- `UserId: string` - User identifier (when available)
- `Email: string` - User email (when available)
- `IpAddress: string` - Client IP address

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
- Event with `AuthEvent = true`
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
- Both have `AuthEvent = true`

### 3. Test Audit Failure Handling

Stop the Seq server and trigger a lockout event:
- The API should throw an exception (audit logging must succeed)
- The exception should be visible in console logs
- Application will fail to start if `Seq:ServerUrl` is not configured

### 4. Test AuthEvent Filtering

In Seq, run query `AuthEvent = true` to verify all auth events are captured.

---

## Deployment Considerations

### Seq Server Requirements

1. **Required**: Seq must be running and accessible - application will fail to start without it
2. **High Availability**: Consider Seq clustering for production
3. **Retention Policies**: Configure Seq retention for audit events (recommend 90+ days)
4. **Alerts**: Set up Seq alerts for critical events (TokenReuseDetected, AccountLockout)
5. **Backup**: Ensure Seq data is included in backup strategy

### Docker Deployment

Seq is defined in `docker-compose.yml` (always available):

```yaml
services:
  api:
    environment:
      - Seq__ServerUrl=http://seq:5341
    depends_on:
      - seq

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

## Implementation Status

### Completed
- [x] NuGet packages configured (Serilog.Sinks.Seq, Serilog.Enrichers.ClientInfo)
- [x] Seq configuration in appsettings.Development.json
- [x] Audit logger configured with AuditTo.Seq in Program.cs
- [x] Standard logging configured in LoggingExtensions.cs with WriteTo.Seq
- [x] IAuthEventsLog interface defined
- [x] AuthEventsLog implementation with AuthEvent=true property
- [x] IAuthEventsLog registered in DI
- [x] AccountController using IAuthEventsLog for all auth events
- [x] RefreshTokenService using IAuthEventsLog for token reuse detection
- [x] Seq container in docker-compose.yml
- [x] WithClientIp enricher added

### Pending
- [ ] Create Seq alerts for critical events
- [ ] Test locally with Seq running
- [ ] Deploy to staging
- [ ] Deploy to production

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
