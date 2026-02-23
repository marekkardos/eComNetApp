#pragma warning disable S2092 // Secure is intentionally false in development to allow HTTP localhost
using Api.Identity;
using Core.Entities.Identity;

namespace Api.Extensions;

public static class CookieExtensions
{
    public static void SetRefreshTokenCookie(
        this HttpResponse response,
        RefreshToken refreshToken,
        TokenSettings tokenSettings)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshToken.ExpiresAt,
            Path = "/api"
        };

        response.Cookies.Append(tokenSettings.CookieName, refreshToken.Token, cookieOptions);
    }

    public static void ClearRefreshTokenCookie(
        this HttpResponse response,
        TokenSettings tokenSettings)
    {
        response.Cookies.Delete(tokenSettings.CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api"
        });
    }
}
