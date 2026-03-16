using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Accounting.Identity.Infrastructure.Caching;

/// <summary>
/// Redis-based caching service for high-performance data caching.
/// Provides string and object caching with TTL support.
/// </summary>
public class RedisCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly string _instanceName;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger,
        string instanceName = "identity:")
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _instanceName = instanceName;
    }

    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var fullKey = GetFullKey(key);

        try
        {
            var value = await db.StringGetAsync(fullKey);

            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", fullKey);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {Key}", fullKey);
            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from cache for key: {Key}", fullKey);
            return default;
        }
    }

    /// <summary>
    /// Sets a cached value with optional TTL.
    /// </summary>
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var fullKey = GetFullKey(key);

        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            await db.StringSetAsync(fullKey, serializedValue, expiry, StackExchange.Redis.When.Always, CommandFlags.None);

            _logger.LogDebug(
                "Cached value for key: {Key} with expiry: {Expiry}",
                fullKey,
                expiry?.TotalSeconds ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing to cache for key: {Key}", fullKey);
        }
    }

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var fullKey = GetFullKey(key);

        try
        {
            await db.KeyDeleteAsync(fullKey);
            _logger.LogDebug("Removed cache key: {Key}", fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", fullKey);
        }
    }

    /// <summary>
    /// Checks if a key exists in cache.
    /// </summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var fullKey = GetFullKey(key);

        try
        {
            return await db.KeyExistsAsync(fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {Key}", fullKey);
            return false;
        }
    }

    /// <summary>
    /// Gets or creates a cached value using the provided factory function.
    /// </summary>
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var cachedValue = await GetAsync<T>(key, cancellationToken);

        if (cachedValue != null)
        {
            return cachedValue;
        }

        var value = await factory();
        await SetAsync(key, value, expiry, cancellationToken);

        return value;
    }

    private string GetFullKey(string key)
    {
        return $"{_instanceName}{key}";
    }
}

/// <summary>
/// Extension methods for registering Redis caching services.
/// </summary>
public static class RedisCacheServiceExtensions
{
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        string connectionString,
        string instanceName = "identity:")
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(connectionString));

        services.AddSingleton(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var logger = sp.GetRequiredService<ILogger<RedisCacheService>>();
            return new RedisCacheService(redis, logger, instanceName);
        });

        return services;
    }
}
