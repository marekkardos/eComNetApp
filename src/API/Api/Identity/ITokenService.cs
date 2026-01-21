using Core.Entities.Identity;

namespace Api.Identity;

public interface ITokenService
{
    (string Token, string JwtId) CreateToken(AppUser user);
}