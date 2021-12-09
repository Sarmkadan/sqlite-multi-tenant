#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Caching
{
    /// <summary>
    /// Defines a contract for asynchronous cache strategies.
    /// Implementations provide methods to get, set, remove, and clear cached items.
    /// </summary>
    public interface ICacheStrategy
    {
        /// <summary>
        /// Retrieves a cached value associated with the specified <paramref name="key"/>.
        /// Returns <c>default</c> if the key is null, empty, or the entry does not exist.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The unique identifier for the cached entry.</param>
        /// <returns>A <see cref="Task{TResult}"/> that resolves to the cached value, or <c>default</c> if not found.</returns>
        Task<T> GetAsync<T>(string key);

        /// <summary>
        /// Stores a value in the cache under the specified <paramref name="key"/>.
        /// If <paramref name="expiration"/> is provided, the entry will be automatically removed after the given time span.
        /// </summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="key">The unique identifier for the cached entry.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="expiration">An optional time‑to‑live for the entry.</param>
        /// <returns>A <see cref="Task"/> that completes when the operation finishes.</returns>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// Removes the cached entry identified by <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The unique identifier for the cached entry to remove.</param>
        /// <returns>A <see cref="Task"/> that completes when the removal operation finishes.</returns>
        Task RemoveAsync(string key);

        /// <summary>
        /// Clears all entries from the cache.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the cache has been cleared.</returns>
        Task ClearAsync();
    }

    /// <summary>
    /// LRU (Least Recently Used) cache implementation with optional TTL (time‑to‑live) support.
    /// When the cache reaches its maximum size, the least recently accessed entry is evicted.
    /// </summary>
    public sealed class LruCacheStrategy : ICacheStrategy {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly ILogger<LruCacheStrategy> _logger;
        private readonly int _maxSize;
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of <see cref="LruCacheStrategy"/>.
        /// </summary>
        /// <param name="logger">The logger used for diagnostic messages.</param>
        /// <param name="maxSize">The maximum number of entries the cache can hold. Defaults to 1000.</param>
        public LruCacheStrategy(ILogger<LruCacheStrategy> logger, int maxSize = 1000)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxSize = maxSize;
            _cache = new ConcurrentDictionary<string, CacheEntry>();
        }

        /// <inheritdoc/>
        public async Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default;

            try
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    // Check expiration
                    if (entry.ExpiresAt.HasValue && DateTime.UtcNow > entry.ExpiresAt)
                    {
                        _cache.TryRemove(key, out _);
                        return default;
                    }

                    // Update last accessed time
                    entry.LastAccessedAt = DateTime.UtcNow;
                    entry.AccessCount++;

                    return (T)entry.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving from cache: {Key}", key);
            }

            return default;
        }

        /// <inheritdoc/>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key) || value is null)
                return;

            try
            {
                lock (_lock)
                {
                    // Evict if cache is full (using LRU policy)
                    if (_cache.Count >= _maxSize)
                    {
                        EvictLRUEntry();
                    }
                }

                var entry = new CacheEntry
                {
                    Value = value,
                    CreatedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow,
                    ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null,
                    AccessCount = 1
                };

                _cache.AddOrUpdate(key, entry, (_, __) => entry);

                _logger.LogDebug("Cache entry set: {Key} with TTL: {TTL}ms",
                    key, expiration?.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache: {Key}", key);
            }
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            try
            {
                _cache.TryRemove(key, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from cache: {Key}", key);
            }
        }

        /// <inheritdoc/>
        public async Task ClearAsync()
        {
            try
            {
                _cache.Clear();
                _logger.LogInformation("Cache cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
            }
        }

        /// <summary>
        /// Retrieves statistics for all cached entries, including creation time, last access time,
        /// access count, and expiration.
        /// </summary>
        /// <returns>A dictionary keyed by cache entry key containing <see cref="CacheStatistics"/> objects.</returns>
        public Dictionary<string, CacheStatistics> GetStatistics()
        {
            var stats = new Dictionary<string, CacheStatistics>();

            foreach (var kvp in _cache)
            {
                stats[kvp.Key] = new CacheStatistics
                {
                    Key = kvp.Key,
                    CreatedAt = kvp.Value.CreatedAt,
                    LastAccessedAt = kvp.Value.LastAccessedAt,
                    AccessCount = kvp.Value.AccessCount,
                    ExpiresAt = kvp.Value.ExpiresAt
                };
            }

            return stats;
        }

        private void EvictLRUEntry()
        {
            var lruEntry = _cache
                .OrderBy(x => x.Value.LastAccessedAt)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(lruEntry.Key))
            {
                _cache.TryRemove(lruEntry.Key, out _);
                _logger.LogDebug("LRU cache entry evicted: {Key}", lruEntry.Key);
            }
        }

        private class CacheEntry
        {
            public object Value { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastAccessedAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public int AccessCount { get; set; }
        }
    }

    /// <summary>
    /// Time‑based cache strategy that stores entries for a configurable duration.
    /// Intended for scenarios where failed retrievals may be retried with exponential back‑off
    /// (the back‑off logic is external to this class).
    /// </summary>
    public sealed class TimeBasedCacheStrategy : ICacheStrategy {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly ILogger<TimeBasedCacheStrategy> _logger;
        private readonly TimeSpan _defaultExpiration;

        /// <summary>
        /// Initializes a new instance of <see cref="TimeBasedCacheStrategy"/>.
        /// </summary>
        /// <param name="logger">The logger used for diagnostic messages.</param>
        /// <param name="defaultExpiration">
        /// The default time‑to‑live applied when <paramref name="expiration"/> is not supplied to <see cref="SetAsync{T}"/>.
        /// </param>
        public TimeBasedCacheStrategy(ILogger<TimeBasedCacheStrategy> logger,
            TimeSpan? defaultExpiration = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultExpiration = defaultExpiration ?? TimeSpan.FromHours(1);
            _cache = new ConcurrentDictionary<string, CacheEntry>();
        }

        /// <inheritdoc/>
        public async Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default;

            try
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    if (DateTime.UtcNow > entry.ExpiresAt)
                    {
                        _cache.TryRemove(key, out _);
                        return default;
                    }

                    return (T)entry.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving from cache: {Key}", key);
            }

            return default;
        }

        /// <inheritdoc/>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key) || value is null)
                return;

            try
            {
                var ttl = expiration ?? _defaultExpiration;
                var entry = new CacheEntry
                {
                    Value = value,
                    ExpiresAt = DateTime.UtcNow.Add(ttl)
                };

                _cache.AddOrUpdate(key, entry, (_, __) => entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache: {Key}", key);
            }
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            _cache.TryRemove(key, out _);
        }

        /// <inheritdoc/>
        public async Task ClearAsync()
        {
            _cache.Clear();
        }

        private class CacheEntry
        {
            public object Value { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }

    /// <summary>
    /// Represents statistical information for a single cache entry.
    /// </summary>
    public sealed class CacheStatistics {
        /// <summary>
        /// The cache key associated with this entry.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The UTC timestamp when the entry was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The UTC timestamp of the most recent access.
        /// </summary>
        public DateTime LastAccessedAt { get; set; }

        /// <summary>
        /// The total number of times the entry has been accessed.
        /// </summary>
        public int AccessCount { get; set; }

        /// <summary>
        /// The UTC timestamp when the entry expires, or <c>null</c> if it does not expire.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }
}
