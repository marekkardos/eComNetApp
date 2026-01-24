using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using Api.ApiResponses;
using Api.Controllers;
using Api.Dtos;
using Api.Extensions;
using Api.Identity;
using AutoMapper;
using Core.Entities.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiExplorerSettings(GroupName = "Account")]
public class AccountController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IAuthenticationServices authServices,
    IMapper mapper,
    ILoggerFactory loggerFactory,
    IWebHostEnvironment environment) : BaseApiController
{
    private readonly ILogger<AccountController> _logger = loggerFactory.CreateLogger<AccountController>();
    private readonly TokenSettings _tokenSettings = authServices.TokenSettings;
    private readonly LockoutSettings _lockoutSettings = authServices.LockoutSettings;
    private readonly ITokenService tokenService = authServices.TokenService;
    private readonly IRefreshTokenService refreshTokenService = authServices.RefreshTokenService;
    private readonly IAuthEventsLog authEventsLog =  authServices.AuthEventsLog;

    [HttpGet("emailexists")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> CheckEmailExistsAsync([FromQuery] [Required] [EmailAddress] string email)
    {
        return await userManager.FindByEmailAsync(email) != null;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        var dbUser = await userManager.FindByEmailAsync(registerDto.Email);

        if (dbUser != null)
        {
            return new BadRequestObjectResult(new ApiValidationErrorResponse
                {Errors = ["Email address already exists."]});
        }

        var user = new AppUser
        {
            DisplayName = registerDto.DisplayName,
            Email = registerDto.Email,
            UserName = registerDto.Email
        };

        var result = await userManager.CreateAsync(user, registerDto.Password);

        if (result.Succeeded)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            authEventsLog.Monitor_UserRegistration(user.Id, user.Email, clientIp);

            var response = await GenerateAuthResponseAsync(user);
            return Created("account", response.Value);
        }

        _logger.LogWarning("Problem creating the user: _userManager.CreateAsync failed:{IdentityResult}", result);

        return new BadRequestObjectResult(new ApiValidationErrorResponse
            {Errors = result.Errors.Select(x => x.Description)});
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
            authEventsLog.AccountLockout(user.Id, user.Email, clientIp);
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized,
                "Account locked due to multiple failed attempts. Try again later."));
        }

        if (!result.Succeeded)
        {
            var failedCount = await userManager.GetAccessFailedCountAsync(user);

            if (failedCount >= _lockoutSettings.MaxFailedAccessAttempts-1)
            {
                authEventsLog.FailedLoginThreshold(user.Id, user.Email, clientIp, failedCount);
            }
            else
            {
                authEventsLog.Monitor_LoginAttemptFailed(user.Id, user.Email, clientIp, failedCount);
            }

            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized));
        }

        authEventsLog.Monitor_SuccessfulLogin(user.Id, user.Email, clientIp);
        return await GenerateAuthResponseAsync(user);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var user = await userManager.FindByEmailFromClaimsPrinciple(HttpContext.User);

        if (user == null)
        {
            return null;
        }

        var (accessToken, _) = tokenService.CreateToken(user);

        return new UserDto
        {
            Email = user.Email,
            Token = accessToken,
            DisplayName = user.DisplayName
        };
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> RefreshToken()
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var refreshTokenValue = Request.Cookies[_tokenSettings.CookieName];

        if (string.IsNullOrEmpty(refreshTokenValue))
        {
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized, "No refresh token"));
        }

        var refreshToken = await refreshTokenService.ValidateRefreshTokenAsync(refreshTokenValue);

        if (refreshToken == null)
        {
            Response.ClearRefreshTokenCookie(_tokenSettings, !environment.IsDevelopment());
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized, "Invalid refresh token"));
        }

        var user = refreshToken.AppUser;
        var (accessToken, jwtId) = tokenService.CreateToken(user);

        var newRefreshToken = await refreshTokenService.RotateRefreshTokenAsync(refreshToken, user, jwtId);
        Response.SetRefreshTokenCookie(newRefreshToken, _tokenSettings, !environment.IsDevelopment());

        authEventsLog.Monitor_TokenRefresh(user.Id, user.Email, clientIp);

        return new UserDto
        {
            Email = user.Email,
            Token = accessToken,
            DisplayName = user.DisplayName
        };
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var refreshTokenValue = Request.Cookies[_tokenSettings.CookieName];

        if (!string.IsNullOrEmpty(refreshTokenValue))
        {
            var refreshToken = await refreshTokenService.ValidateRefreshTokenAsync(refreshTokenValue);
            if (refreshToken != null)
            {
                await refreshTokenService.RevokeTokenAsync(refreshToken);
                authEventsLog.Monitor_UserLogout(refreshToken.AppUserId, clientIp);
            }
        }

        Response.ClearRefreshTokenCookie(_tokenSettings, !environment.IsDevelopment());
        return NoContent();
    }

    [HttpGet("address")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<AddressDto>> GetUserAddress()
    {
        var user = await userManager.FindByUserByClaimsPrincipleWithAddressAsync(HttpContext.User);

        return mapper.Map<Address, AddressDto>(user.Address);
    }

    [HttpPut("address")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddressDto>> UpdateUserAddress(AddressDto address)
    {
        var user = await userManager.FindByUserByClaimsPrincipleWithAddressAsync(HttpContext.User);

        var isInsert = user.Address == null;

        user.Address = mapper.Map<AddressDto, Address>(address);

        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            return isInsert ? (ActionResult<AddressDto>) Created("address", address) : Ok();
        }

        _logger.LogWarning("_userManager.UpdateAsync failed:{IdentityResult}", result);
        return BadRequest(new ApiResponse(HttpStatusCode.BadRequest, "Problem updating the user"));
    }

    [HttpGet("external-login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] string returnUrl = "/")
    {
        if (string.IsNullOrEmpty(provider))
        {
            return BadRequest(new ApiResponse(HttpStatusCode.BadRequest, "Provider is required"));
        }

        // Validate provider
        if (!provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ApiResponse(HttpStatusCode.BadRequest, "Invalid provider. Supported providers: Google"));
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

        return Challenge(properties, provider);
    }

    [HttpGet("external-login-callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExternalLoginCallback([FromQuery] string returnUrl = "/", [FromQuery] string remoteError = null)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (!string.IsNullOrEmpty(remoteError))
        {
            _logger.LogWarning("External login failed with remote error: {RemoteError}", remoteError);
            return Redirect($"{returnUrl}?error=external_login_failed&message={Uri.EscapeDataString(remoteError)}");
        }

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning("External login info is null");
            return Redirect($"{returnUrl}?error=external_login_failed&message=Unable to get external login information");
        }

        // Try to sign in with the external login
        var signInResult = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            // User exists and is linked - generate tokens
            var existingUser = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existingUser != null)
            {
                authEventsLog.Monitor_SuccessfulLogin(existingUser.Id, existingUser.Email, clientIp);
                var response = await GenerateExternalLoginResponseAsync(existingUser, returnUrl);
                return response;
            }
        }

        if (signInResult.IsLockedOut)
        {
            return Redirect($"{returnUrl}?error=account_locked&message=Account is locked out");
        }

        // User doesn't exist or isn't linked - create or link account
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? info.Principal.FindFirstValue(ClaimTypes.GivenName);

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("External login did not provide email");
            return Redirect($"{returnUrl}?error=external_login_failed&message=Email is required from external provider");
        }

        var user = await userManager.FindByEmailAsync(email);

        if (user != null)
        {
            // User exists with this email - link the external login
            var addLoginResult = await userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                _logger.LogWarning("Failed to link external login for user {Email}: {Errors}", email, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                return Redirect($"{returnUrl}?error=link_failed&message=Failed to link external account");
            }

            authEventsLog.Monitor_SuccessfulLogin(user.Id, user.Email, clientIp);
            return await GenerateExternalLoginResponseAsync(user, returnUrl);
        }

        // Create new user
        user = new AppUser
        {
            UserName = email,
            Email = email,
            DisplayName = name ?? email.Split('@')[0]
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            _logger.LogWarning("Failed to create user for external login {Email}: {Errors}", email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return Redirect($"{returnUrl}?error=registration_failed&message=Failed to create user account");
        }

        var linkResult = await userManager.AddLoginAsync(user, info);
        if (!linkResult.Succeeded)
        {
            _logger.LogWarning("Failed to link external login for new user {Email}: {Errors}", email, string.Join(", ", linkResult.Errors.Select(e => e.Description)));
            // User was created but linking failed - still allow login
        }

        authEventsLog.Monitor_UserRegistration(user.Id, user.Email, clientIp);
        return await GenerateExternalLoginResponseAsync(user, returnUrl);
    }

    [HttpGet("external-logins")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ExternalLoginInfoDto>>> GetExternalLogins()
    {
        var user = await userManager.FindByEmailFromClaimsPrinciple(HttpContext.User);

        if (user == null)
        {
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized));
        }

        var logins = await userManager.GetLoginsAsync(user);

        return Ok(logins.Select(l => new ExternalLoginInfoDto
        {
            LoginProvider = l.LoginProvider,
            ProviderKey = l.ProviderKey,
            ProviderDisplayName = l.ProviderDisplayName ?? l.LoginProvider
        }));
    }

    [HttpGet("link-external-login")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LinkExternalLogin([FromQuery] string provider, [FromQuery] string returnUrl = "/")
    {
        if (string.IsNullOrEmpty(provider))
        {
            return BadRequest(new ApiResponse(HttpStatusCode.BadRequest, "Provider is required"));
        }

        // Validate provider
        if (!provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ApiResponse(HttpStatusCode.BadRequest, "Invalid provider. Supported providers: Google"));
        }

        var user = await userManager.FindByEmailFromClaimsPrinciple(HttpContext.User);
        if (user == null)
        {
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized));
        }

        // Check if this provider is already linked
        var existingLogins = await userManager.GetLoginsAsync(user);
        if (existingLogins.Any(l => l.LoginProvider.Equals(provider, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new ApiResponse(HttpStatusCode.BadRequest, $"{provider} is already linked to your account"));
        }

        var redirectUrl = Url.Action(nameof(LinkExternalLoginCallback), "Account", new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

        // Store user ID in authentication properties to retrieve in callback
        properties.Items["LinkUserId"] = user.Id;

        return Challenge(properties, provider);
    }

    [HttpGet("link-external-login-callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LinkExternalLoginCallback([FromQuery] string returnUrl = "/", [FromQuery] string remoteError = null)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (!string.IsNullOrEmpty(remoteError))
        {
            _logger.LogWarning("External login linking failed with remote error: {RemoteError}", remoteError);
            return Redirect($"{returnUrl}?error=link_failed&message={Uri.EscapeDataString(remoteError)}");
        }

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning("External login info is null during account linking");
            return Redirect($"{returnUrl}?error=link_failed&message=Unable to get external login information");
        }

        // Get the user ID from the authentication properties
        var userId = info.AuthenticationProperties?.Items.ContainsKey("LinkUserId") == true
            ? info.AuthenticationProperties.Items["LinkUserId"]
            : null;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("LinkUserId not found in authentication properties");
            return Redirect($"{returnUrl}?error=link_failed&message=Invalid linking request");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found for linking: {UserId}", userId);
            return Redirect($"{returnUrl}?error=link_failed&message=User not found");
        }

        // Check if this external account is already linked to another user
        var existingUser = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (existingUser != null)
        {
            _logger.LogWarning("External account {Provider} is already linked to another user", info.LoginProvider);
            return Redirect($"{returnUrl}?error=link_failed&message=This {info.LoginProvider} account is already linked to another user");
        }

        var result = await userManager.AddLoginAsync(user, info);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to link external login for user {Email}: {Errors}",
                user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Redirect($"{returnUrl}?error=link_failed&message=Failed to link external account");
        }

        _logger.LogInformation("Successfully linked {Provider} to user {Email}", info.LoginProvider, user.Email);
        return Redirect($"{returnUrl}?success=true&provider={info.LoginProvider}");
    }

    [HttpDelete("external-logins/{provider}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkExternalLogin(string provider)
    {
        if (string.IsNullOrEmpty(provider))
        {
            return BadRequest(new ApiResponse(HttpStatusCode.BadRequest, "Provider is required"));
        }

        var user = await userManager.FindByEmailFromClaimsPrinciple(HttpContext.User);
        if (user == null)
        {
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized));
        }

        var logins = await userManager.GetLoginsAsync(user);
        var loginToRemove = logins.FirstOrDefault(l => l.LoginProvider.Equals(provider, StringComparison.OrdinalIgnoreCase));

        if (loginToRemove == null)
        {
            return NotFound(new ApiResponse(HttpStatusCode.NotFound, $"{provider} is not linked to your account"));
        }

        // Ensure user has another way to sign in
        var hasPassword = await userManager.HasPasswordAsync(user);
        var remainingLoginsCount = logins.Count - 1;

        if (!hasPassword && remainingLoginsCount == 0)
        {
            return BadRequest(new ApiResponse(HttpStatusCode.BadRequest,
                "Cannot unlink the last external login. Please set a password first or link another provider."));
        }

        var result = await userManager.RemoveLoginAsync(user, loginToRemove.LoginProvider, loginToRemove.ProviderKey);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to unlink external login {Provider} for user {Email}: {Errors}",
                provider, user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new ApiResponse(HttpStatusCode.BadRequest, "Failed to unlink external account"));
        }

        _logger.LogInformation("Successfully unlinked {Provider} from user {Email}", provider, user.Email);
        return Ok(new { message = $"{provider} has been unlinked from your account" });
    }

    private async Task<IActionResult> GenerateExternalLoginResponseAsync(AppUser user, string returnUrl)
    {
        var (accessToken, jwtId) = tokenService.CreateToken(user);
        var refreshToken = await refreshTokenService.GenerateRefreshTokenAsync(user, jwtId);

        Response.SetRefreshTokenCookie(refreshToken, _tokenSettings, !environment.IsDevelopment());

        // Redirect back to frontend with token
        var separator = returnUrl.Contains('?') ? '&' : '?';
        return Redirect($"{returnUrl}{separator}token={accessToken}&email={Uri.EscapeDataString(user.Email)}&displayName={Uri.EscapeDataString(user.DisplayName)}");
    }

    private async Task<ActionResult<UserDto>> GenerateAuthResponseAsync(AppUser user)
    {
        var (accessToken, jwtId) = tokenService.CreateToken(user);
        var refreshToken = await refreshTokenService.GenerateRefreshTokenAsync(user, jwtId);

        Response.SetRefreshTokenCookie(refreshToken, _tokenSettings, !environment.IsDevelopment());

        return new UserDto
        {
            Email = user.Email,
            Token = accessToken,
            DisplayName = user.DisplayName
        };
    }
}
