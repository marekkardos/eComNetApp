using System.ComponentModel.DataAnnotations;

namespace Api.Dtos;

/// <summary>
/// DTO for exchanging an authorization code for an access token.
/// </summary>
public class ExternalAuthCodeExchangeDto
{
    /// <summary>
    /// The short-lived authorization code received from the OAuth callback.
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;
}
