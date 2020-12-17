using Core.Entities.Identity;

namespace Api.Identity
{
    public interface ITokenService
    {
         string CreateToken(AppUser user);
    }
}