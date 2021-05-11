#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Caching;

/// <summary>
/// Advanced distributed caching service with LRU eviction and TTL support.
/// Provides high-performance caching with thread-safe operations.
/// Supports cache invalidation, warming, and statistics.
/// </summary>
public interface IDistributedCache
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class;
    Task<bool> RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task ClearAsync();
    Task<CacheStatistics> GetStatisticsAsync();
}

public sealed class DistributedCacheService : IDistributedCache {
    private readonly Dictionary<string, CacheEntry> _cache;
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly int _maxItems;
    private readonly TimeSpan _defaultTtl;
    private long _hits;
    private long _misses;

    public DistributedCacheService(ILogger<DistributedCacheService> logger, int maxItems = 1000)
    {
        _logger = logger;
        _cache = new Dictionary<string, CacheEntry>();
        _semaphore = new SemaphoreSlim(1);
        _maxItems = maxItems;
        _defaultTtl = TimeSpan.FromHours(1);
        _hits = 0;
        _misses = 0;
    }

    /// <summary>
    /// Gets a cached value by key.
    /// Returns null if not found or expired.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            await _semaphore.WaitAsync();

            if (_cache.TryGetValue(key, out var entry))
            {
                // Check if expired
                if (entry.ExpiresAt.HasValue && DateTime.UtcNow > entry.ExpiresAt)
                {
                    _cache.Remove(key);
                    _misses++;
                    return null;
                }

                // Update access time for LRU
                entry.LastAccessedAt = DateTime.UtcNow;
                entry.AccessCount++;
                _hits++;

                _logger.LogDebug("Cache hit: {Key}", key);
                return entry.Value as T;
            }

            _misses++;
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Sets a cache value with optional TTL.
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class
    {
        try
        {
            await _semaphore.WaitAsync();

            var entry = new CacheEntry
            {
                Value = value,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                ExpiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : DateTime.UtcNow.Add(_defaultTtl),
                AccessCount = 0,
                Size = EstimateSize(value)
            };

            // Check if we need to evict
            if (_cache.Count >= _maxItems && !_cache.ContainsKey(key))
            {
                EvictLruEntry();
            }

            _cache[key] = entry;
            _logger.LogDebug($"Cache set: {key}, TTL: {ttl?.TotalSeconds ?? _defaultTtl.TotalSeconds}s");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Removes a cache entry by key.
    /// </summary>
    public async Task<bool> RemoveAsync(string key)
    {
        try
        {
            await _semaphore.WaitAsync();

            if (_cache.Remove(key))
            {
                _logger.LogDebug("Cache removed: {Key}", key);
                return true;
            }

            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Removes cache entries matching a pattern.
    /// </summary>
    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            await _semaphore.WaitAsync();

            var keysToRemove = _cache.Keys
                .Where(k => k.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
                _cache.Remove(key);

            _logger.LogDebug("Cache pattern removal: {Pattern}, Removed: {Count}", pattern, keysToRemove.Count);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Clears all cache entries.
    /// </summary>
    public async Task ClearAsync()
    {
        try
        {
            await _semaphore.WaitAsync();

            _cache.Clear();
            _hits = 0;
            _misses = 0;
            _logger.LogInformation("Cache cleared");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        try
        {
            await _semaphore.WaitAsync();

            var totalSize = _cache.Values.Sum(e => e.Size);
            var hitRate = (_hits + _misses) > 0 ? (double)_hits / (_hits + _misses) : 0;

            return new CacheStatistics
            {
                ItemCount = _cache.Count,
                TotalSizeBytes = totalSize,
                Hits = _hits,
                Misses = _misses,
                HitRate = hitRate,
                AverageItemSize = _cache.Count > 0 ? totalSize / _cache.Count : 0
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Cleans up expired entries.
    /// </summary>
    public async Task CleanupExpiredAsync()
    {
        try
        {
            await _semaphore.WaitAsync();

            var now = DateTime.UtcNow;
            var keysToRemove = _cache
                .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
                _cache.Remove(key);

            if (keysToRemove.Count > 0)
                _logger.LogInformation("Cleaned up {Count} expired cache entries", keysToRemove.Count);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void EvictLruEntry()
    {
        // Find least recently used entry
        var lruEntry = _cache
            .OrderBy(e => e.Value.LastAccessedAt)
            .First();

        _cache.Remove(lruEntry.Key);
        _logger.LogDebug("Cache evicted (LRU): {Key}", lruEntry.Key);
    }

    private static long EstimateSize(object value)
    {
        // Rough estimation
        return value?.ToString()?.Length ?? 0;
    }
}

public sealed class CacheEntry {
    public object? Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public long AccessCount { get; set; }
    public long Size { get; set; }
}

public sealed class CacheStatistics {
    public int ItemCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public long Hits { get; set; }
    public long Misses { get; set; }
    public double HitRate { get; set; }
    public long AverageItemSize { get; set; }
}
