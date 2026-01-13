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

namespace API.Controllers
{
    [Authorize]
    [ApiExplorerSettings(GroupName = "Account")]
    public class AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
        ITokenService tokenService, IMapper mapper, ILoggerFactory loggerFactory) : BaseApiController
    {
        private readonly ILogger<AccountController> _logger = loggerFactory.CreateLogger<AccountController>();

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
                return Created("account", new UserDto
                {
                    DisplayName = user.DisplayName,
                    Token = tokenService.CreateToken(user),
                    Email = user.Email
                });
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

            var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded)
            {
                return Unauthorized(new ApiResponse(HttpStatusCode.Unauthorized));
            }

            var token = tokenService.CreateToken(user);

            return new UserDto
            {
                Email = user.Email,
                Token = token,
                DisplayName = user.DisplayName
            };
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var user = await userManager.FindByEmailFromClaimsPrinciple(HttpContext.User);

            if (user == null)
            {
                // email claim from token doesn't exist in the db.
                return null;
            }

            return new UserDto
            {
                Email = user.Email,
                Token = tokenService.CreateToken(user),
                DisplayName = user.DisplayName
            };
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
    }
}