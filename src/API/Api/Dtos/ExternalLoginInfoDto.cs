namespace Api.Dtos;

/// <summary>
/// DTO representing an external login provider linked to a user account.
/// </summary>
public class ExternalLoginInfoDto
{
    /// <summary>
    /// The login provider name (e.g., "Google").
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the provider (e.g., "Google").
    /// </summary>
    public string ProviderDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this provider is currently linked to the user's account.
    /// </summary>
    public bool IsLinked { get; set; }
}

/// <summary>
/// DTO representing a user with their linked external logins.
/// </summary>
public class UserWithExternalLoginsDto
{
    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// JWT access token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user has a password set (for local authentication).
    /// </summary>
    public bool HasPassword { get; set; }

    /// <summary>
    /// List of external login providers linked to this account.
    /// </summary>
    public List<ExternalLoginInfoDto> ExternalLogins { get; set; } = [];
}
