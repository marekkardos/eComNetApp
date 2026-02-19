using System.Net;
using Api.ApiResponses;
using Api.Dtos;
using Api.Extensions;
using Api.Identity;
using Core.Entities.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Controllers;

/// <summary>
/// Controller for external authentication (social login) endpoints.
/// Implements the BFF (Backend-for-Frontend) pattern with code exchange.
/// </summary>
[ApiExplorerSettings(GroupName = "ExternalAuth")]
public class ExternalAuthController(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    IExternalAuthService externalAuthService,
    IExternalAuthCodeService externalAuthCodeService,
    IAuthenticationServices authServices,
    IOptions<GoogleAuthSettings> googleAuthSettings,
    ILogger<ExternalAuthController> logger) : BaseApiController
{
    private readonly GoogleAuthSettings _googleSettings = googleAuthSettings.Value;
    private readonly TokenSettings _tokenSettings = authServices.TokenSettings;
    private readonly ITokenService _tokenService = authServices.TokenService;
    private readonly IRefreshTokenService _refreshTokenService = authServices.RefreshTokenService;
    private readonly IAuthEventsLog _authEventsLog = authServices.AuthEventsLog;

    /// <summary>
    /// Initiates Google OAuth login flow. Redirects to Google for authentication.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful authentication.</param>
    [HttpGet("google")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GoogleLogin([FromQuery] string returnUrl = "/")
    {
        if (!IsValidReturnUrl(returnUrl))
        {
            logger.LogWarning("Invalid returnUrl attempted: {ReturnUrl}", returnUrl);
            returnUrl = "/";
        }

        var properties = signInManager.ConfigureExternalAuthenticationProperties(
            GoogleDefaults.AuthenticationScheme,
            Url.Action(nameof(GoogleCallback), new { returnUrl }));

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Callback endpoint for Google OAuth. Creates/links user and redirects with auth code.
    /// </summary>
    [HttpGet("google/callback")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GoogleCallback([FromQuery] string returnUrl = "/")
    {
        var info = await signInManager.GetExternalLoginInfoAsync();

        if (info == null)
        {
            logger.LogWarning("External login info not available in callback");
            return RedirectWithError(returnUrl, "external_login_failed", "Could not retrieve login information from Google.");
        }

        // Find or create user
        var result = await externalAuthService.FindOrCreateUserAsync(info);

        if (!result.Succeeded)
        {
            logger.LogWarning("External auth failed: {Error}", result.Error);
            return RedirectWithError(returnUrl, "external_auth_failed", result.Error ?? "Authentication failed.");
        }

        var user = result.User!;

        // Generate short-lived code for code exchange
        var code = await externalAuthCodeService.GenerateCodeAsync(user);

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        _authEventsLog.Monitor_SuccessfulLogin(user.Id, user.Email, clientIp);

        // Clean up the temporary cookie
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        // Redirect to frontend with code
        var redirectUrl = AppendQueryParam(returnUrl, "code", code);
        return Redirect(redirectUrl);
    }

    /// <summary>
    /// Exchanges a short-lived authorization code for an access token.
    /// </summary>
    [HttpPost("exchange")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> ExchangeCode([FromBody] ExternalAuthCodeExchangeDto dto)
    {
        AppUser user = await externalAuthCodeService.ValidateAndConsumeCodeAsync(dto.Code);

        if (user == null)
        {
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized, "Invalid or expired code"));
        }

        // Generate access token and refresh token together — both issued atomically here
        (string accessToken, string jwtId) = _tokenService.CreateToken(user);
        RefreshToken refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user, jwtId);
        Response.SetRefreshTokenCookie(refreshToken, _tokenSettings);

        return new UserDto
        {
            Email = user.Email,
            DisplayName = user.DisplayName,
            Token = accessToken
        };
    }

    /// <summary>
    /// Initiates Google OAuth linking flow for authenticated users.
    /// </summary>
    [HttpGet("google/link")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GoogleLink([FromQuery] string returnUrl = "/")
    {
        if (!IsValidReturnUrl(returnUrl))
        {
            returnUrl = "/";
        }

        var properties = signInManager.ConfigureExternalAuthenticationProperties(
            GoogleDefaults.AuthenticationScheme,
            Url.Action(nameof(GoogleLinkCallback), new { returnUrl }));

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Callback endpoint for Google OAuth linking.
    /// </summary>
    [HttpGet("google/link/callback")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GoogleLinkCallback([FromQuery] string returnUrl = "/")
    {
        var user = await userManager.FindByEmailFromClaimsPrinciple(HttpContext.User);

        if (user == null)
        {
            return RedirectWithError(returnUrl, "user_not_found", "User not found.");
        }

        var info = await signInManager.GetExternalLoginInfoAsync();

        if (info == null)
        {
            return RedirectWithError(returnUrl, "external_login_failed", "Could not retrieve login information from Google.");
        }

        var result = await externalAuthService.LinkExternalLoginAsync(user, info);

        if (!result.Succeeded)
        {
            return RedirectWithError(returnUrl, "link_failed", result.Error ?? "Failed to link account.");
        }

        // Clean up the temporary cookie
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        
        var redirectUrl = AppendQueryParam(returnUrl, "linked", "google");
        return Redirect(redirectUrl);
    }

    /// <summary>
    /// Unlinks Google from the user's account.
    /// </summary>
    [HttpDelete("google/unlink")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GoogleUnlink()
    {
        var user = await userManager.FindByEmailFromClaimsPrinciple(HttpContext.User);

        if (user == null)
        {
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized));
        }

        var logins = await externalAuthService.GetExternalLoginsAsync(user);
        var googleLogin = logins.FirstOrDefault(l => l.LoginProvider == GoogleDefaults.AuthenticationScheme);

        if (googleLogin == null)
        {
            return BadRequest(new ApiResponse(HttpStatusCode.BadRequest, "Google account is not linked."));
        }

        var result = await externalAuthService.UnlinkExternalLoginAsync(
            user, GoogleDefaults.AuthenticationScheme, googleLogin.ProviderKey);

        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponse(HttpStatusCode.BadRequest, result.Error));
        }

        return NoContent();
    }

    /// <summary>
    /// Gets the list of available external login providers and their link status for the current user.
    /// </summary>
    [HttpGet("providers")]
    [Authorize]
    [ProducesResponseType(typeof(UserWithExternalLoginsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserWithExternalLoginsDto>> GetProviders()
    {
        var user = await userManager.FindByEmailFromClaimsPrinciple(HttpContext.User);

        if (user == null)
        {
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized));
        }

        var logins = await externalAuthService.GetExternalLoginsAsync(user);
        var hasPassword = await externalAuthService.HasPasswordAsync(user);

        return new UserWithExternalLoginsDto
        {
            Email = user.Email,
            DisplayName = user.DisplayName,
            HasPassword = hasPassword,
            ExternalLogins =
            [
                new ExternalLoginInfoDto
                {
                    Provider = GoogleDefaults.AuthenticationScheme,
                    ProviderDisplayName = "Google",
                    IsLinked = logins.Any(l => l.LoginProvider == GoogleDefaults.AuthenticationScheme)
                }
            ]
        };
    }

    private bool IsValidReturnUrl(string returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return false;
        }

        // Allow relative URLs
        if (returnUrl.StartsWith('/') && !returnUrl.StartsWith("//"))
        {
            return true;
        }

        // Validate absolute URLs against allowed hosts
        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out Uri uri))
        {
            return _googleSettings.AllowedReturnUrlHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
        }

        return false;
    }

    private IActionResult RedirectWithError(string returnUrl, string error, string errorDescription)
    {
        var url = AppendQueryParam(returnUrl, "error", error);
        url = AppendQueryParam(url, "error_description", errorDescription);
        return Redirect(url);
    }

    private static string AppendQueryParam(string url, string key, string value)
    {
        var separator = url.Contains('?') ? "&" : "?";
        return $"{url}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
    }
}
