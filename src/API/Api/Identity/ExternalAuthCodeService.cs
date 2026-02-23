using System.Security.Cryptography;
using System.Text.Json;
using Core.Entities.Identity;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Api.Identity;

/// <summary>
/// Redis-backed service for managing short-lived authorization codes.
/// </summary>
public class ExternalAuthCodeService(
    IConnectionMultiplexer redis,
    UserManager<AppUser> userManager,
    IOptions<GoogleAuthSettings> googleAuthSettings,
    ILogger<ExternalAuthCodeService> logger) : IExternalAuthCodeService
{
    private readonly GoogleAuthSettings _settings = googleAuthSettings.Value;
    private const string CacheKeyPrefix = "external_auth_code:";

    public async Task<string> GenerateCodeAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        string code = GenerateSecureCode();
        string cacheKey = GetCacheKey(code);

        var codeData = new AuthCodeData
        {
            UserId = user.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        IDatabase db = redis.GetDatabase();
        await db.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(codeData),
            TimeSpan.FromSeconds(_settings.AuthCodeExpirationSeconds));

        logger.LogInformation("Generated external auth code for user {UserId}", user.Id);

        return code;
    }

    public async Task<AppUser> ValidateAndConsumeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }

        string cacheKey = GetCacheKey(code);
        IDatabase db = redis.GetDatabase();

        // Atomic read-and-delete — eliminates the TOCTOU race from the prior IDistributedCache approach
        RedisValue cachedValue = await db.StringGetDeleteAsync(cacheKey);

        if (cachedValue.IsNullOrEmpty)
        {
            logger.LogWarning("Invalid or expired external auth code attempted");
            return null;
        }

        try
        {
            AuthCodeData codeData = JsonSerializer.Deserialize<AuthCodeData>((string)cachedValue);

            if (codeData == null || string.IsNullOrEmpty(codeData.UserId))
            {
                logger.LogWarning("Malformed external auth code data");
                return null;
            }

            AppUser user = await userManager.FindByIdAsync(codeData.UserId);

            if (user == null)
            {
                logger.LogWarning("User {UserId} not found for external auth code", codeData.UserId);
                return null;
            }

            logger.LogInformation("Successfully validated and consumed external auth code for user {UserId}", user.Id);
            return user;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize external auth code data");
            return null;
        }
    }

    private static string GenerateSecureCode()
    {
        // Generate a 32-byte (256-bit) cryptographically secure random code
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string GetCacheKey(string code) => $"{CacheKeyPrefix}{code}";
}
