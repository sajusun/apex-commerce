using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ApexCommerce.Application.Common.Interfaces;

namespace ApexCommerce.Infrastructure.Caching;

public sealed class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DistributedCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(key, cancellationToken);
        if (bytes == null || bytes.Length == 0) return default;

        return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
        };

        await _cache.SetAsync(key, bytes, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null) return cached;

        var fresh = await factory();
        if (fresh != null)
        {
            await SetAsync(key, fresh, expiration, cancellationToken);
        }

        return fresh;
    }
}

