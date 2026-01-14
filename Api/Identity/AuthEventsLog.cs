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
    // Critical events - written to audit log (must succeed)

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
                    "SECURITY: Account locked for user {Email} (ID: {UserId}) from IP {IpAddress} due to multiple failed login attempts",
                    email, userId, ipAddress);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CRITICAL: Audit logging failed for account lockout event. User: {Email}", email);
            throw;
        }
    }

    public void FailedLoginThreshold(string userId, string email, string ipAddress, int failedAttempts)
    {
        try
        {
            using (LogContext.PushProperty("AuthEvent", true))
            using (LogContext.PushProperty("EventType", "FailedLoginThreshold"))
            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("Email", email))
            using (LogContext.PushProperty("IpAddress", ipAddress))
            using (LogContext.PushProperty("FailedAttempts", failedAttempts))
            {
                auditLogger.Warning(
                    "SECURITY: User {Email} has {FailedAttempts} failed login attempts from IP {IpAddress}",
                    email, failedAttempts, ipAddress);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CRITICAL: Audit logging failed for failed login threshold. User: {Email}", email);
            throw;
        }
    }

    public void TokenReuseDetected(string userId, string tokenId, DateTime revokedAt)
    {
        try
        {
            using (LogContext.PushProperty("AuthEvent", true))
            using (LogContext.PushProperty("EventType", "TokenReuseDetected"))
            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("TokenId", tokenId))
            using (LogContext.PushProperty("RevokedAt", revokedAt))
            {
                auditLogger.Error(
                    "SECURITY BREACH: Revoked refresh token reused for user {UserId}. " +
                    "Token was revoked at {RevokedAt}. This indicates a stolen token.",
                    userId, revokedAt);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CRITICAL: Audit logging failed for token reuse detection. User: {UserId}", userId);
            throw;
        }
    }

    public void AllTokensRevoked(string userId)
    {
        try
        {
            using (LogContext.PushProperty("AuthEvent", true))
            using (LogContext.PushProperty("EventType", "AllTokensRevoked"))
            using (LogContext.PushProperty("UserId", userId))
            {
                auditLogger.Warning(
                    "SECURITY: All refresh tokens revoked for user {UserId} due to token reuse detection",
                    userId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CRITICAL: Audit logging failed for all tokens revoked. User: {UserId}", userId);
            throw;
        }
    }

    // Monitoring events - written to standard log (Seq)

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

    public void Monitor_UserRegistration(string userId, string email, string ipAddress)
    {
        using (LogContext.PushProperty("AuthEvent", true))
        using (LogContext.PushProperty("EventType", "UserRegistration"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("IpAddress", ipAddress))
        {
            logger.LogInformation("New user registered: {Email} from IP {IpAddress}", email, ipAddress);
        }
    }

    public void Monitor_TokenRefresh(string userId, string email, string ipAddress)
    {
        using (LogContext.PushProperty("AuthEvent", true))
        using (LogContext.PushProperty("EventType", "TokenRefresh"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("IpAddress", ipAddress))
        {
            logger.LogInformation("Token refreshed for user {Email} from IP {IpAddress}", email, ipAddress);
        }
    }

    public void Monitor_UserLogout(string userId, string ipAddress)
    {
        using (LogContext.PushProperty("AuthEvent", true))
        using (LogContext.PushProperty("EventType", "UserLogout"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("IpAddress", ipAddress))
        {
            logger.LogInformation("User {UserId} logged out from IP {IpAddress}", userId, ipAddress);
        }
    }

    public void Monitor_LoginAttemptNonExistent(string email, string ipAddress)
    {
        using (LogContext.PushProperty("AuthEvent", true))
        using (LogContext.PushProperty("EventType", "LoginAttemptNonExistent"))
        using (LogContext.PushProperty("Email", email))
        using (LogContext.PushProperty("IpAddress", ipAddress))
        {
            logger.LogInformation(
                "Login attempt for non-existent email: {Email} from IP: {IpAddress}",
                email, ipAddress);
        }
    }

    public void Monitor_LoginAttemptFailed(string userId, string email, string ipAddress, int failedAttempts)
    {
        using (LogContext.PushProperty("AuthEvent", true))
        using (LogContext.PushProperty("EventType", "LoginAttemptFailed"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Email", email))
        using (LogContext.PushProperty("IpAddress", ipAddress))
        using (LogContext.PushProperty("FailedAttempts", failedAttempts))
        {
            logger.LogInformation(
                "Failed login attempt for {Email} from IP: {IpAddress}. Attempt count: {FailedAttempts}",
                email, ipAddress, failedAttempts);
        }
    }
}
