namespace Api.Identity;

public class TokenSettings
{
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public string CookieName { get; set; } = "refreshToken";
}
