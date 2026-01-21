using Core.Entities.Identity;

namespace Api.Identity;

public interface IRefreshTokenService
{
    Task<RefreshToken> GenerateRefreshTokenAsync(AppUser user, string jwtId);
    Task<RefreshToken> ValidateRefreshTokenAsync(string token);
    Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken oldToken, AppUser user, string newJwtId);
    Task RevokeTokenAsync(RefreshToken token);
    Task RevokeAllUserTokensAsync(string userId);
}
