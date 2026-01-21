using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Entities.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Identity;

public class TokenService(IConfiguration config, IOptions<TokenSettings> tokenSettings) : ITokenService
{
    private readonly SymmetricSecurityKey _key = new(Encoding.UTF8.GetBytes(
        config["Token:Key"] ?? throw new InvalidOperationException("Token:Key configuration is missing")));
    private readonly TokenSettings _tokenSettings = tokenSettings.Value;

    public (string Token, string JwtId) CreateToken(AppUser user)
    {
        var jwtId = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, jwtId)
        };

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_tokenSettings.AccessTokenExpirationMinutes),
            SigningCredentials = creds,
            Issuer = config["Token:Issuer"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return (tokenHandler.WriteToken(token), jwtId);
    }
}