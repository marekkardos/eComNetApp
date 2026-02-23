using Api.Dtos;
using Api.Extensions;
using Core.Entities.Identity;
using Microsoft.Extensions.Options;

namespace Api.Identity;

public class AuthenticationServices(
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService,
    IAuthEventsLog authEventsLog,
    IOptions<TokenSettings> tokenSettings,
    IOptions<LockoutSettings> lockoutSettings) : IAuthenticationServices
{
    public ITokenService TokenService => tokenService;
    public IRefreshTokenService RefreshTokenService => refreshTokenService;
    public IAuthEventsLog AuthEventsLog => authEventsLog;
    public TokenSettings TokenSettings => tokenSettings.Value;
    public LockoutSettings LockoutSettings => lockoutSettings.Value;

    public async Task<UserDto> GenerateLoginResponseAsync(AppUser user, HttpResponse response)
    {
        (string accessToken, string jwtId) = tokenService.CreateToken(user);
        RefreshToken refreshToken = await refreshTokenService.GenerateRefreshTokenAsync(user, jwtId);
        response.SetRefreshTokenCookie(refreshToken, tokenSettings.Value);

        return new UserDto
        {
            Email = user.Email,
            Token = accessToken,
            DisplayName = user.DisplayName
        };
    }
}
