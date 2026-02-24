using Api.Dtos;
using Core.Entities.Identity;

namespace Api.Identity;

public interface IAuthenticationServices
{
    ITokenService TokenService { get; }
    IRefreshTokenService RefreshTokenService { get; }
    IAuthEventsLog AuthEventsLog { get; }
    TokenSettings TokenSettings { get; }
    LockoutSettings LockoutSettings { get; }

    /// <summary>
    /// Creates an access token and refresh token for the given user, sets the refresh token
    /// cookie on the response, and returns a populated UserDto. Use this as the single
    /// place for login/register/exchange-code response generation.
    /// </summary>
    Task<UserDto> GenerateLoginResponseAsync(AppUser user, HttpResponse response);
}
