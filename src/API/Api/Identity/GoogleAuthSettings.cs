namespace Api.Identity;

/// <summary>
/// Configuration settings for Google OAuth authentication.
/// </summary>
public class GoogleAuthSettings
{
    /// <summary>
    /// Google OAuth Client ID from Google Cloud Console.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Google OAuth Client Secret from Google Cloud Console.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Time in seconds before the authorization code expires. Default is 60 seconds.
    /// </summary>
    public int AuthCodeExpirationSeconds { get; set; } = 60;

    /// <summary>
    /// List of allowed hosts for the returnUrl parameter to prevent open redirect attacks.
    /// </summary>
    public string[] AllowedReturnUrlHosts { get; set; } = ["localhost"];
}
