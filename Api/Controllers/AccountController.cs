using System.ComponentModel.DataAnnotations;
using System.Net;
using Api.ApiResponses;
using Api.Controllers;
using Api.Dtos;
using Api.Extensions;
using Api.Identity;
using AutoMapper;
using Core.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[Authorize]
[ApiExplorerSettings(GroupName = "Account")]
public class AccountController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService,
    IOptions<TokenSettings> tokenSettings,
    IMapper mapper,
    ILoggerFactory loggerFactory,
    IWebHostEnvironment environment) : BaseApiController
{
    private readonly ILogger<AccountController> _logger = loggerFactory.CreateLogger<AccountController>();
    private readonly TokenSettings _tokenSettings = tokenSettings.Value;

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

        if (user == null)
        {
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized));
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized,
                "Account locked due to multiple failed attempts. Try again later."));
        }

        if (!result.Succeeded)
        {
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized));
        }

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
        var refreshTokenValue = Request.Cookies[_tokenSettings.CookieName];

        if (string.IsNullOrEmpty(refreshTokenValue))
        {
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized, "No refresh token"));
        }

        var refreshToken = await refreshTokenService.ValidateRefreshTokenAsync(refreshTokenValue);

        if (refreshToken == null)
        {
            Response.ClearRefreshTokenCookie(_tokenSettings);
            return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized, "Invalid refresh token"));
        }

        var user = refreshToken.AppUser;
        var (accessToken, jwtId) = tokenService.CreateToken(user);

        var newRefreshToken = await refreshTokenService.RotateRefreshTokenAsync(refreshToken, user, jwtId);
        Response.SetRefreshTokenCookie(newRefreshToken, _tokenSettings, !environment.IsDevelopment());

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
        var refreshTokenValue = Request.Cookies[_tokenSettings.CookieName];

        if (!string.IsNullOrEmpty(refreshTokenValue))
        {
            var refreshToken = await refreshTokenService.ValidateRefreshTokenAsync(refreshTokenValue);
            if (refreshToken != null)
            {
                await refreshTokenService.RevokeTokenAsync(refreshToken);
            }
        }

        Response.ClearRefreshTokenCookie(_tokenSettings);
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