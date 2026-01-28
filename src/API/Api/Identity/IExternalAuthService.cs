using Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Api.Identity;

/// <summary>
/// Result of an external authentication operation.
/// </summary>
public class ExternalAuthResult
{
    public bool Succeeded { get; set; }
    public AppUser User { get; set; }
    public string Error { get; set; }
    public bool IsNewUser { get; set; }
    public bool WasLinked { get; set; }

    public static ExternalAuthResult Success(AppUser user, bool isNewUser = false, bool wasLinked = false)
        => new() { Succeeded = true, User = user, IsNewUser = isNewUser, WasLinked = wasLinked };

    public static ExternalAuthResult Failure(string error)
        => new() { Succeeded = false, Error = error };
}

/// <summary>
/// Service for handling external authentication operations (social login).
/// </summary>
public interface IExternalAuthService
{
    /// <summary>
    /// Finds an existing user by external login or creates a new user.
    /// If a user with the same verified email exists, auto-links the external login.
    /// </summary>
    /// <param name="info">The external login information from the OAuth provider.</param>
    /// <returns>The result containing the user or error information.</returns>
    Task<ExternalAuthResult> FindOrCreateUserAsync(ExternalLoginInfo info);

    /// <summary>
    /// Links an external login to an existing authenticated user.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="info">The external login information to link.</param>
    /// <returns>The result of the linking operation.</returns>
    Task<ExternalAuthResult> LinkExternalLoginAsync(AppUser user, ExternalLoginInfo info);

    /// <summary>
    /// Unlinks an external login from a user's account.
    /// Fails if this is the user's only authentication method.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="provider">The login provider name (e.g., "Google").</param>
    /// <param name="providerKey">The unique key for this user from the provider.</param>
    /// <returns>The result of the unlinking operation.</returns>
    Task<ExternalAuthResult> UnlinkExternalLoginAsync(AppUser user, string provider, string providerKey);

    /// <summary>
    /// Gets all external logins linked to a user's account.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>List of external login information.</returns>
    Task<IList<UserLoginInfo>> GetExternalLoginsAsync(AppUser user);

    /// <summary>
    /// Checks if a user has a password set (for determining if they can unlink external logins).
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>True if the user has a password.</returns>
    Task<bool> HasPasswordAsync(AppUser user);
}
