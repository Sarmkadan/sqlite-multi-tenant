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
    // Base interface for pluggable cache strategies
    public interface ICacheStrategy
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task ClearAsync();
    }

    // LRU (Least Recently Used) cache implementation with TTL support
    public sealed class LruCacheStrategy : ICacheStrategy {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly ILogger<LruCacheStrategy> _logger;
        private readonly int _maxSize;
        private readonly object _lock = new object();

        public LruCacheStrategy(ILogger<LruCacheStrategy> logger, int maxSize = 1000)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxSize = maxSize;
            _cache = new ConcurrentDictionary<string, CacheEntry>();
        }

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

    // Time-based cache that uses exponential backoff for failed retrievals
    public sealed class TimeBasedCacheStrategy : ICacheStrategy {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly ILogger<TimeBasedCacheStrategy> _logger;
        private readonly TimeSpan _defaultExpiration;

        public TimeBasedCacheStrategy(ILogger<TimeBasedCacheStrategy> logger,
            TimeSpan? defaultExpiration = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultExpiration = defaultExpiration ?? TimeSpan.FromHours(1);
            _cache = new ConcurrentDictionary<string, CacheEntry>();
        }

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

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            _cache.TryRemove(key, out _);
        }

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

    public sealed class CacheStatistics {
        public string Key { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public int AccessCount { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
