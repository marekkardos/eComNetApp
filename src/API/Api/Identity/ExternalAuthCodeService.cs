using System.Security.Cryptography;
using System.Text.Json;
using Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Api.Identity;

/// <summary>
/// Redis-backed service for managing short-lived authorization codes.
/// </summary>
public class ExternalAuthCodeService(
    IDistributedCache cache,
    UserManager<AppUser> userManager,
    IOptions<GoogleAuthSettings> googleAuthSettings,
    ILogger<ExternalAuthCodeService> logger) : IExternalAuthCodeService
{
    private readonly GoogleAuthSettings _settings = googleAuthSettings.Value;
    private const string CacheKeyPrefix = "external_auth_code:";

    public async Task<string> GenerateCodeAsync(AppUser user)
    {
        var code = GenerateSecureCode();
        var cacheKey = GetCacheKey(code);

        var codeData = new AuthCodeData
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_settings.AuthCodeExpirationSeconds)
        };

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(codeData), options);

        logger.LogInformation("Generated external auth code for user {UserId}", user.Id);

        return code;
    }

    public async Task<AppUser> ValidateAndConsumeCodeAsync(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }

        var cacheKey = GetCacheKey(code);

        // Get and immediately delete the code (single-use)
        var cachedValue = await cache.GetStringAsync(cacheKey);

        if (string.IsNullOrEmpty(cachedValue))
        {
            logger.LogWarning("Invalid or expired external auth code attempted");
            return null;
        }

        // Delete immediately to ensure single-use
        await cache.RemoveAsync(cacheKey);

        try
        {
            var codeData = JsonSerializer.Deserialize<AuthCodeData>(cachedValue);

            if (codeData == null || string.IsNullOrEmpty(codeData.UserId))
            {
                logger.LogWarning("Malformed external auth code data");
                return null;
            }

            var user = await userManager.FindByIdAsync(codeData.UserId);

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
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string GetCacheKey(string code) => $"{CacheKeyPrefix}{code}";

    private class AuthCodeData
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
