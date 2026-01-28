using Core.Entities.Identity;

namespace Api.Identity;

/// <summary>
/// Service for managing short-lived authorization codes used in the OAuth code exchange flow.
/// These codes are single-use and expire quickly to prevent replay attacks.
/// </summary>
public interface IExternalAuthCodeService
{
    /// <summary>
    /// Generates a cryptographically secure, short-lived authorization code for a user.
    /// The code is stored in Redis and expires after the configured timeout.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>The generated authorization code.</returns>
    Task<string> GenerateCodeAsync(AppUser user);

    /// <summary>
    /// Validates and consumes an authorization code, returning the associated user if valid.
    /// The code is deleted after use to prevent replay attacks.
    /// </summary>
    /// <param name="code">The authorization code to validate.</param>
    /// <returns>The user associated with the code, or null if the code is invalid or expired.</returns>
    Task<AppUser> ValidateAndConsumeCodeAsync(string code);
}
