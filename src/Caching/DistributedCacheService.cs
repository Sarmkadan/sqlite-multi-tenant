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
    /// <summary>
    /// Gets a cached value by key, or null if it is missing or expired.
    /// </summary>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Stores a value under the given key with an optional time-to-live.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class;

    /// <summary>
    /// Removes the entry for the given key and reports whether one existed.
    /// </summary>
    Task<bool> RemoveAsync(string key);

    /// <summary>
    /// Removes all entries whose keys start with the given pattern.
    /// </summary>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Removes all entries and resets the hit and miss counters.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Gets a snapshot of the cache item count, size, hit and miss counts.
    /// </summary>
    Task<DistributedCacheStatistics> GetStatisticsAsync();
}

/// <summary>
/// In-memory implementation of <see cref="IDistributedCache"/> backed by a dictionary guarded by a semaphore.
/// </summary>
public sealed class DistributedCacheService : IDistributedCache
{
    private readonly Dictionary<string, CacheEntry> _cache;
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly int _maxItems;
    private readonly TimeSpan _defaultTtl;
    private long _hits;
    private long _misses;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedCacheService"/> class.
    /// </summary>
    /// <param name="logger">Logger used for cache diagnostics.</param>
    /// <param name="maxItems">Maximum number of entries before LRU eviction starts. Defaults to 1000.</param>
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
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (key == string.Empty)
        {
            throw new ArgumentException("Key cannot be empty.", nameof(key));
        }

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
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (key == string.Empty)
        {
            throw new ArgumentException("Key cannot be empty.", nameof(key));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        try
        {
            await _semaphore.WaitAsync();

            var entry = new CacheEntry
            {
                Key = key,
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
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (key == string.Empty)
        {
            throw new ArgumentException("Key cannot be empty.", nameof(key));
        }

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
        if (pattern == null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        if (pattern == string.Empty)
        {
            throw new ArgumentException("Pattern cannot be empty.", nameof(pattern));
        }

        try
        {
            await _semaphore.WaitAsync();

            var keysToRemove = _cache.Keys
                .Where(k => k.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }

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
    public async Task<DistributedCacheStatistics> GetStatisticsAsync()
    {
        try
        {
            await _semaphore.WaitAsync();

            var totalSize = _cache.Values.Sum(e => e.Size);
            var hitRate = (_hits + _misses) > 0 ? (double)_hits / (_hits + _misses) : 0;

            return new DistributedCacheStatistics
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
            {
                _cache.Remove(key);
            }

            if (keysToRemove.Count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired cache entries", keysToRemove.Count);
            }
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

/// <summary>
/// A single item stored in <see cref="DistributedCacheService"/>, with its expiry and access metadata.
/// </summary>
public sealed class CacheEntry
{
    /// <summary>
    /// Gets or sets the cache key this entry is stored under.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cached value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the entry was last read or written.
    /// </summary>
    public DateTime LastAccessedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC expiry time, or null if the entry never expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the number of cache hits for this entry.
    /// </summary>
    public long AccessCount { get; set; }

    /// <summary>
    /// Gets or sets the estimated size of the entry.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Returns a one-line summary of the key, expiry and size. The cached value is not included.
    /// </summary>
    /// <returns>A string with the entry key, expiry time and size.</returns>
    public override string ToString()
    {
        return $"Key: {Key}, expires: {(ExpiresAt.HasValue ? ExpiresAt.Value.ToString("O") : "never")}, size: {Size}";
    }
}

/// <summary>
/// Snapshot of cache usage returned by <see cref="IDistributedCache.GetStatisticsAsync"/>.
/// </summary>
public sealed class DistributedCacheStatistics
{
    /// <summary>
    /// Gets or sets the number of entries currently in the cache.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the total estimated size of all entries in bytes.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the number of cache hits since the last clear.
    /// </summary>
    public long Hits { get; set; }

    /// <summary>
    /// Gets or sets the number of cache misses since the last clear.
    /// </summary>
    public long Misses { get; set; }

    /// <summary>
    /// Gets or sets the ratio of hits to total lookups, from 0 to 1.
    /// </summary>
    public double HitRate { get; set; }

    /// <summary>
    /// Gets or sets the average estimated size of an entry in bytes.
    /// </summary>
    public long AverageItemSize { get; set; }

    /// <summary>
    /// Returns a one-line summary of the item count, total size and hit rate.
    /// </summary>
    /// <returns>A string with the item count, total size in bytes and hit rate.</returns>
    public override string ToString()
    {
        return $"Items: {ItemCount}, total size: {TotalSizeBytes} bytes, hit rate: {HitRate:P1}";
    }
}
