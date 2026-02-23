using Serilog.Context;

namespace Api.Identity;

/// <summary>
/// Authentication and authorization event logging implementation.
/// Uses audit logger for critical security events and regular logger for monitoring.
/// All events are tagged with AuthEvent=true for easy filtering in Seq.
/// </summary>
public class AuthEventsLog(
    Serilog.ILogger auditLogger,
    ILogger<AuthEventsLog> logger) : IAuthEventsLog
{
    private const string AuthEventProperty = "AuthEvent";
    private const string EventTypeProperty = "EventType";
    private const string UserIdProperty = "UserId";
    private const string EmailProperty = "Email";
    private const string IpAddressProperty = "IpAddress";
    private const string FailedAttemptsProperty = "FailedAttempts";
    private const string TokenIdProperty = "TokenId";
    private const string RevokedAtProperty = "RevokedAt";
    private const string ProviderProperty = "Provider";

    // Critical events - written to audit log (must succeed)

    public void AccountLockout(string userId, string email, string ipAddress)
    {
        try
        {
            using (LogContext.PushProperty(AuthEventProperty, true))
            using (LogContext.PushProperty(EventTypeProperty, "AccountLockout"))
            using (LogContext.PushProperty(UserIdProperty, userId))
            using (LogContext.PushProperty(EmailProperty, email))
            using (LogContext.PushProperty(IpAddressProperty, ipAddress))
            {
                auditLogger.Warning(
                    "SECURITY: Account locked for user {Email} (ID: {UserId}) from IP {IpAddress} due to multiple failed login attempts",
                    email, userId, ipAddress);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"CRITICAL: Audit logging failed for account lockout event. User: {email}", ex);
        }
    }

    public void FailedLoginThreshold(string userId, string email, string ipAddress, int failedAttempts)
    {
        try
        {
            using (LogContext.PushProperty(AuthEventProperty, true))
            using (LogContext.PushProperty(EventTypeProperty, "FailedLoginThreshold"))
            using (LogContext.PushProperty(UserIdProperty, userId))
            using (LogContext.PushProperty(EmailProperty, email))
            using (LogContext.PushProperty(IpAddressProperty, ipAddress))
            using (LogContext.PushProperty(FailedAttemptsProperty, failedAttempts))
            {
                auditLogger.Warning(
                    "SECURITY: User {Email} has {FailedAttempts} failed login attempts from IP {IpAddress}",
                    email, failedAttempts, ipAddress);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"CRITICAL: Audit logging failed for failed login threshold. User: {email}", ex);
        }
    }

    public void TokenReuseDetected(string userId, string tokenId, DateTime revokedAt)
    {
        try
        {
            using (LogContext.PushProperty(AuthEventProperty, true))
            using (LogContext.PushProperty(EventTypeProperty, "TokenReuseDetected"))
            using (LogContext.PushProperty(UserIdProperty, userId))
            using (LogContext.PushProperty(TokenIdProperty, tokenId))
            using (LogContext.PushProperty(RevokedAtProperty, revokedAt))
            {
                auditLogger.Error(
                    "SECURITY BREACH: Revoked refresh token reused for user {UserId}. " +
                    "Token was revoked at {RevokedAt}. This indicates a stolen token.",
                    userId, revokedAt);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"CRITICAL: Audit logging failed for token reuse detection. User: {userId}", ex);
        }
    }

    public void AllTokensRevoked(string userId)
    {
        try
        {
            using (LogContext.PushProperty(AuthEventProperty, true))
            using (LogContext.PushProperty(EventTypeProperty, "AllTokensRevoked"))
            using (LogContext.PushProperty(UserIdProperty, userId))
            {
                auditLogger.Warning(
                    "SECURITY: All refresh tokens revoked for user {UserId} due to token reuse detection",
                    userId);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"CRITICAL: Audit logging failed for all tokens revoked. User: {userId}", ex);
        }
    }

    // Monitoring events - written to standard log (Seq)

    public void Monitor_SuccessfulLogin(string userId, string email, string ipAddress)
    {
        using (LogContext.PushProperty(AuthEventProperty, true))
        using (LogContext.PushProperty(EventTypeProperty, "SuccessfulLogin"))
        using (LogContext.PushProperty(UserIdProperty, userId))
        using (LogContext.PushProperty(IpAddressProperty, ipAddress))
        {
            logger.LogInformation("User {Email} logged in successfully from IP {IpAddress}", email, ipAddress);
        }
    }

    public void Monitor_UserRegistration(string userId, string email, string ipAddress)
    {
        using (LogContext.PushProperty(AuthEventProperty, true))
        using (LogContext.PushProperty(EventTypeProperty, "UserRegistration"))
        using (LogContext.PushProperty(UserIdProperty, userId))
        using (LogContext.PushProperty(IpAddressProperty, ipAddress))
        {
            logger.LogInformation("New user registered: {Email} from IP {IpAddress}", email, ipAddress);
        }
    }

    public void Monitor_TokenRefresh(string userId, string email, string ipAddress)
    {
        using (LogContext.PushProperty(AuthEventProperty, true))
        using (LogContext.PushProperty(EventTypeProperty, "TokenRefresh"))
        using (LogContext.PushProperty(UserIdProperty, userId))
        using (LogContext.PushProperty(IpAddressProperty, ipAddress))
        {
            logger.LogInformation("Token refreshed for user {Email} from IP {IpAddress}", email, ipAddress);
        }
    }

    public void Monitor_UserLogout(string userId, string ipAddress)
    {
        using (LogContext.PushProperty(AuthEventProperty, true))
        using (LogContext.PushProperty(EventTypeProperty, "UserLogout"))
        using (LogContext.PushProperty(UserIdProperty, userId))
        using (LogContext.PushProperty(IpAddressProperty, ipAddress))
        {
            logger.LogInformation("User {UserId} logged out from IP {IpAddress}", userId, ipAddress);
        }
    }

    public void Monitor_LoginAttemptNonExistent(string email, string ipAddress)
    {
        using (LogContext.PushProperty(AuthEventProperty, true))
        using (LogContext.PushProperty(EventTypeProperty, "LoginAttemptNonExistent"))
        using (LogContext.PushProperty(EmailProperty, email))
        using (LogContext.PushProperty(IpAddressProperty, ipAddress))
        {
            logger.LogInformation(
                "Login attempt for non-existent email: {Email} from IP: {IpAddress}",
                email, ipAddress);
        }
    }

    public void Monitor_LoginAttemptFailed(string userId, string email, string ipAddress, int failedAttempts)
    {
        using (LogContext.PushProperty(AuthEventProperty, true))
        using (LogContext.PushProperty(EventTypeProperty, "LoginAttemptFailed"))
        using (LogContext.PushProperty(UserIdProperty, userId))
        using (LogContext.PushProperty(EmailProperty, email))
        using (LogContext.PushProperty(IpAddressProperty, ipAddress))
        using (LogContext.PushProperty(FailedAttemptsProperty, failedAttempts))
        {
            logger.LogInformation(
                "Failed login attempt for {Email} from IP: {IpAddress}. Attempt count: {FailedAttempts}",
                email, ipAddress, failedAttempts);
        }
    }

    // External authentication events

    public void Monitor_ExternalLogin(string userId, string email, string provider, string ipAddress)
    {
        using (LogContext.PushProperty(AuthEventProperty, true))
        using (LogContext.PushProperty(EventTypeProperty, "ExternalLogin"))
        using (LogContext.PushProperty(UserIdProperty, userId))
        using (LogContext.PushProperty(EmailProperty, email))
        using (LogContext.PushProperty(ProviderProperty, provider))
        using (LogContext.PushProperty(IpAddressProperty, ipAddress))
        {
            logger.LogInformation(
                "New user {Email} registered via {Provider} from IP {IpAddress}",
                email, provider, ipAddress);
        }
    }

    public void Monitor_ExternalAccountLinked(string userId, string email, string provider, string ipAddress)
    {
        using (LogContext.PushProperty(AuthEventProperty, true))
        using (LogContext.PushProperty(EventTypeProperty, "ExternalAccountLinked"))
        using (LogContext.PushProperty(UserIdProperty, userId))
        using (LogContext.PushProperty(EmailProperty, email))
        using (LogContext.PushProperty(ProviderProperty, provider))
        using (LogContext.PushProperty(IpAddressProperty, ipAddress))
        {
            logger.LogInformation(
                "User {Email} linked {Provider} account from IP {IpAddress}",
                email, provider, ipAddress);
        }
    }

    public void Monitor_ExternalAccountUnlinked(string userId, string email, string provider, string ipAddress)
    {
        using (LogContext.PushProperty(AuthEventProperty, true))
        using (LogContext.PushProperty(EventTypeProperty, "ExternalAccountUnlinked"))
        using (LogContext.PushProperty(UserIdProperty, userId))
        using (LogContext.PushProperty(EmailProperty, email))
        using (LogContext.PushProperty(ProviderProperty, provider))
        using (LogContext.PushProperty(IpAddressProperty, ipAddress))
        {
            logger.LogInformation(
                "User {Email} unlinked {Provider} account from IP {IpAddress}",
                email, provider, ipAddress);
        }
    }
}
