using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using StackExchange.Redis;

namespace Services
{
    public class ResponseCacheService : IResponseCacheService
    {
        private readonly ILogger<ResponseCacheService> _logger;
        private readonly IDatabase _database;

        public ResponseCacheService(IConnectionMultiplexer redis, ILogger<ResponseCacheService> logger)
        {
            _logger = logger;
            _database = redis.GetDatabase();
        }

        public Task CacheResponseAsync(string cacheKey, object response, TimeSpan timeToLive)
        {
            if (response == null)
            {
                _logger.LogWarning("CacheResponseAsync response is null.");
                return null;
            }

            _logger.LogDebug($"CacheResponseAsync cacheKey:'{cacheKey}' , timeToLive:{timeToLive.TotalMilliseconds} ms");

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var serializedResponse = JsonSerializer.Serialize(response, options);

            return _database.StringSetAsync(cacheKey, serializedResponse, timeToLive);
        }

        public async Task<string> GetCachedResponseAsync(string cacheKey)
        {
            var cachedResponse = await _database.StringGetAsync(cacheKey);

            _logger.LogDebug($"GetCachedResponseAsync cacheKey:'{cacheKey}'");

            if (cachedResponse.IsNullOrEmpty)
            {
                _logger.LogDebug("GetCachedResponseAsync cachedResponse is null.");
                return null;
            }

            return cachedResponse;
        }
    }
}