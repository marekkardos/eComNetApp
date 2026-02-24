using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using StackExchange.Redis;

namespace Services;

public class ResponseCacheService(IConnectionMultiplexer redis, ILogger<ResponseCacheService> logger) : IResponseCacheService
{
    private readonly ILogger<ResponseCacheService> _logger = logger;
    private readonly IDatabase _database = redis.GetDatabase();

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Task CacheResponseAsync(string cacheKey, object response, TimeSpan timeToLive)
    {
        if (response == null)
        {
            _logger.LogWarning("CacheResponseAsync response is null.");
            return Task.CompletedTask;
        }

        _logger.LogDebug("CacheResponseAsync cacheKey:'{CacheKey}' , timeToLive:{TimeToLive} ms", cacheKey, timeToLive.TotalMilliseconds);


        var serializedResponse = JsonSerializer.Serialize(response, _jsonOptions);

        return _database.StringSetAsync(cacheKey, serializedResponse, timeToLive);
    }

    public async Task<string> GetCachedResponseAsync(string cacheKey)
    {
        var cachedResponse = await _database.StringGetAsync(cacheKey);

        _logger.LogDebug("GetCachedResponseAsync cacheKey:'{CacheKey}'", cacheKey);

        if (cachedResponse.IsNullOrEmpty)
        {
            _logger.LogDebug("GetCachedResponseAsync cachedResponse is null.");
            return null;
        }

        return cachedResponse;
    }
}