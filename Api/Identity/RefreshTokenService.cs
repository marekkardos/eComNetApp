using System.Security.Cryptography;
using System.Text;
using Core.Entities.Identity;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Identity;

public class RefreshTokenService(
    AppIdentityDbContext context,
    IOptions<TokenSettings> tokenSettings,
    ILogger<RefreshTokenService> logger) : IRefreshTokenService
{
    private readonly TokenSettings _tokenSettings = tokenSettings.Value;

    public async Task<RefreshToken> GenerateRefreshTokenAsync(AppUser user, string jwtId)
    {
        var tokenValue = GenerateSecureToken();
        var hashedToken = HashToken(tokenValue);

        var refreshToken = new RefreshToken
        {
            Token = hashedToken,
            JwtId = jwtId,
            AppUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_tokenSettings.RefreshTokenExpirationDays),
            IsRevoked = false
        };

        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        // Return token with plain text value for cookie (will be hashed on validation)
        refreshToken.Token = tokenValue;
        return refreshToken;
    }

    public async Task<RefreshToken> ValidateRefreshTokenAsync(string token)
    {
        var hashedToken = HashToken(token);

        var refreshToken = await context.RefreshTokens
            .Include(rt => rt.AppUser)
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken);

        if (refreshToken == null)
        {
            logger.LogWarning("Refresh token not found");
            return null;
        }

        if (refreshToken.IsRevoked)
        {
            // Potential token reuse attack - revoke all tokens for this user
            logger.LogWarning("Attempted use of revoked token for user {UserId}", refreshToken.AppUserId);
            await RevokeAllUserTokensAsync(refreshToken.AppUserId);
            return null;
        }

        if (refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            logger.LogInformation("Refresh token expired for user {UserId}", refreshToken.AppUserId);
            return null;
        }

        return refreshToken;
    }

    public async Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken oldToken, AppUser user, string newJwtId)
    {
        // Revoke old token
        oldToken.IsRevoked = true;
        oldToken.RevokedAt = DateTime.UtcNow;

        // Generate new token
        var newTokenValue = GenerateSecureToken();
        var hashedNewToken = HashToken(newTokenValue);

        // Link old token to new one
        oldToken.ReplacedByToken = hashedNewToken;

        var newRefreshToken = new RefreshToken
        {
            Token = hashedNewToken,
            JwtId = newJwtId,
            AppUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_tokenSettings.RefreshTokenExpirationDays),
            IsRevoked = false
        };

        context.RefreshTokens.Add(newRefreshToken);
        await context.SaveChangesAsync();

        // Return token with plain text value for cookie
        newRefreshToken.Token = newTokenValue;
        return newRefreshToken;
    }

    public async Task RevokeTokenAsync(RefreshToken token)
    {
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task RevokeAllUserTokensAsync(string userId)
    {
        var tokens = await context.RefreshTokens
            .Where(rt => rt.AppUserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Revoked all refresh tokens for user {UserId}", userId);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
