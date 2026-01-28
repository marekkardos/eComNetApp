namespace Api.Identity;

/// <summary>
/// Authentication and authorization event logging service.
/// Separates audit logging concerns from business logic.
/// </summary>
public interface IAuthEventsLog
{
    // Critical events - written to audit log (must succeed)

    /// <summary>
    /// Account has been locked out due to multiple failed login attempts.
    /// </summary>
    void AccountLockout(string userId, string email, string ipAddress);

    /// <summary>
    /// User is approaching lockout threshold (3+ failed attempts).
    /// </summary>
    void FailedLoginThreshold(string userId, string email, string ipAddress, int failedAttempts);

    /// <summary>
    /// A revoked refresh token was reused - potential security breach.
    /// </summary>
    void TokenReuseDetected(string userId, string tokenId, DateTime revokedAt);

    /// <summary>
    /// All refresh tokens have been revoked for a user due to security concerns.
    /// </summary>
    void AllTokensRevoked(string userId);

    // Monitoring events - written to standard log (Seq)

    /// <summary>
    /// User successfully logged in.
    /// </summary>
    void Monitor_SuccessfulLogin(string userId, string email, string ipAddress);

    /// <summary>
    /// A new user registered.
    /// </summary>
    void Monitor_UserRegistration(string userId, string email, string ipAddress);

    /// <summary>
    /// Access token was refreshed.
    /// </summary>
    void Monitor_TokenRefresh(string userId, string email, string ipAddress);

    /// <summary>
    /// User logged out.
    /// </summary>
    void Monitor_UserLogout(string userId, string ipAddress);

    /// <summary>
    /// Login attempt for non-existent email.
    /// </summary>
    void Monitor_LoginAttemptNonExistent(string email, string ipAddress);

    /// <summary>
    /// Failed login attempt (below threshold).
    /// </summary>
    void Monitor_LoginAttemptFailed(string userId, string email, string ipAddress, int failedAttempts);

    // External authentication events

    /// <summary>
    /// User logged in via external provider (new account created).
    /// </summary>
    void Monitor_ExternalLogin(string userId, string email, string provider, string ipAddress);

    /// <summary>
    /// External provider linked to existing account.
    /// </summary>
    void Monitor_ExternalAccountLinked(string userId, string email, string provider, string ipAddress);

    /// <summary>
    /// External provider unlinked from account.
    /// </summary>
    void Monitor_ExternalAccountUnlinked(string userId, string email, string provider, string ipAddress);
}
