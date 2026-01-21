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
}
