namespace Api.Identity;

public interface IAuthenticationServices
{
    ITokenService TokenService
    {
        get;
    }
    IRefreshTokenService RefreshTokenService
    {
        get;
    }
    IAuthEventsLog AuthEventsLog
    {
        get;
    }
    TokenSettings TokenSettings
    {
        get;
    }
    LockoutSettings LockoutSettings
    {
        get;
    }
}
