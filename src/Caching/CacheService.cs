#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Caching;

/// <summary>
/// In-memory cache service for frequently accessed data (tenants, migrations, backups).
/// Reduces database queries and improves API response times.
/// Implements expiration policies and safe null handling.
/// </summary>
public interface ICacheService
{
    T Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan? expiration = null);
    void Remove(string key);
    void RemoveByPattern(string pattern);
    void Clear();
}

/// <summary>
/// Memory cache implementation using IMemoryCache.
/// Thread-safe via underlying MemoryCache synchronization.
/// </summary>
public sealed class CacheService : ICacheService {
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _keyTimestamps;

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // Fix: Use ConcurrentDictionary to prevent thread-safety issues during concurrent cache operations
        _keyTimestamps = new ConcurrentDictionary<string, DateTime>();
    }

    /// <summary>
    /// Retrieves value from cache if it exists and hasn't expired.
    /// Returns default(T) if not found or expired (no exception thrown).
    /// </summary>
    public T Get<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return default;

        if (_cache.TryGetValue(key, out T value))
        {
            _logger.LogDebug("Cache hit: {Key}", key);
            return value;
        }

        _logger.LogDebug("Cache miss: {Key}", key);
        return default;
    }

    /// <summary>
    /// Stores value in cache with optional expiration time.
    /// Default expiration is 1 hour to prevent stale data.
    /// </summary>
    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key) || value is null)
            return;

        var cacheOptions = new MemoryCacheEntryOptions();

        if (expiration.HasValue)
        {
            cacheOptions.SlidingExpiration = expiration;
        }
        else
        {
            // Default 1-hour expiration
            cacheOptions.SlidingExpiration = TimeSpan.FromHours(1);
        }

        _cache.Set(key, value, cacheOptions);
        _keyTimestamps[key] = DateTime.UtcNow;

        _logger.LogDebug($"Cache set: {key} (expires in {cacheOptions.SlidingExpiration?.TotalSeconds}s)");
    }

    /// <summary>
    /// Removes a specific key from cache.
    /// Safe to call even if key doesn't exist.
    /// </summary>
    public void Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        _cache.Remove(key);
        _keyTimestamps.TryRemove(key, out _);

        _logger.LogDebug("Cache removed: {Key}", key);
    }

    /// <summary>
    /// Removes all keys matching a pattern (e.g., "tenant:*").
    /// Useful for invalidating related cache entries at once.
    /// Note: IMemoryCache doesn't support pattern matching natively,
    /// so we track keys and filter by prefix.
    /// </summary>
    public void RemoveByPattern(string pattern)
    {
        var keysToRemove = _keyTimestamps.Keys
            .Where(k => k.StartsWith(pattern.Replace("*", string.Empty)))
            .ToList();

        foreach (var key in keysToRemove)
        {
            Remove(key);
        }

        _logger.LogInformation("Cache cleared for pattern: {Pattern} ({Count} keys)", pattern, keysToRemove.Count);
    }

    /// <summary>
    /// Clears all entries from cache.
    /// Called during application shutdown or maintenance.
    /// </summary>
    public void Clear()
    {
        // Fix: Dispose breaks the injected IMemoryCache singleton. Instead, we manually remove tracked keys.
        foreach (var key in _keyTimestamps.Keys)
        {
            _cache.Remove(key);
        }
        _keyTimestamps.Clear();

        _logger.LogWarning("Cache cleared (all entries removed)");
    }
}

/// <summary>
/// Cache key manager for consistent key generation across the application.
/// Prevents key collision and improves maintainability.
/// </summary>
public static class CacheKeys
{
    private const string Prefix = "sqlmt";

    public static string TenantKey(string tenantId) => $"{Prefix}:tenant:{tenantId}";
    public static string AllTenantsKey() => $"{Prefix}:tenants:all";
    public static string TenantPattern() => $"{Prefix}:tenant:*";

    public static string BackupKey(string backupId) => $"{Prefix}:backup:{backupId}";
    public static string BackupsForDatabase(string databaseId) => $"{Prefix}:backups:{databaseId}";
    public static string BackupPattern() => $"{Prefix}:backup:*";

    public static string MigrationKey(string migrationId) => $"{Prefix}:migration:{migrationId}";
    public static string PendingMigrationsKey(string databaseId) => $"{Prefix}:migrations:pending:{databaseId}";
    public static string AppliedMigrationsKey(string databaseId) => $"{Prefix}:migrations:applied:{databaseId}";
    public static string MigrationPattern() => $"{Prefix}:migration:*";

    public static string HealthCheckKey() => $"{Prefix}:health";
    public static string ConfigurationKey(string section) => $"{Prefix}:config:{section}";
}

/// <summary>
/// Cache invalidation handler for updating cache when data changes.
/// Implements event-driven cache invalidation for consistency.
/// </summary>
public sealed class CacheInvalidationService {
    private readonly ICacheService _cache;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(ICacheService cache, ILogger<CacheInvalidationService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invalidates all cache entries related to a tenant.
    /// Called when tenant is updated, suspended, or deleted.
    /// </summary>
    public void InvalidateTenant(string tenantId)
    {
        _cache.Remove(CacheKeys.TenantKey(tenantId));
        _cache.Remove(CacheKeys.AllTenantsKey());

        _logger.LogInformation("Cache invalidated for tenant: {TenantId}", tenantId);
    }

    /// <summary>
    /// Invalidates backups related to a database.
    /// Called after backup creation or completion.
    /// </summary>
    public void InvalidateBackups(string databaseId)
    {
        _cache.Remove(CacheKeys.BackupsForDatabase(databaseId));

        _logger.LogInformation("Cache invalidated for backups in database: {DatabaseId}", databaseId);
    }

    /// <summary>
    /// Invalidates migrations related to a database.
    /// Called after migration creation or application.
    /// </summary>
    public void InvalidateMigrations(string databaseId)
    {
        _cache.Remove(CacheKeys.PendingMigrationsKey(databaseId));
        _cache.Remove(CacheKeys.AppliedMigrationsKey(databaseId));

        _logger.LogInformation("Cache invalidated for migrations in database: {DatabaseId}", databaseId);
    }

    /// <summary>
    /// Clears health check cache to force fresh evaluation.
    /// Called when infrastructure state changes.
    /// </summary>
    public void InvalidateHealthCheck()
    {
        _cache.Remove(CacheKeys.HealthCheckKey());

        _logger.LogInformation("Health check cache invalidated");
    }
}
