# DistributedCacheService

`DistributedCacheService` is an in-process, thread-safe cache with time-to-live (TTL) expiration, least-recently-used (LRU) eviction, prefix-based removal, and aggregate statistics. Its entries live in a private `Dictionary<string, CacheEntry>` and are therefore local to the service instance; the implementation does not communicate with an external distributed cache.

## Relation to `IDistributedCache`

The class implements the project-specific `IDistributedCache` interface documented in [`IDistributedCache.md`](IDistributedCache.md). `GetAsync`, `SetAsync`, `RemoveAsync`, `RemoveByPatternAsync`, `ClearAsync`, and `GetStatisticsAsync` implement that contract. `CleanupExpiredAsync` is an additional public method available only on `DistributedCacheService`.

All cache operations are guarded by a `SemaphoreSlim`, so access to the underlying dictionary and statistics counters is serialized. This interface is `SqliteMultiTenant.Caching.IDistributedCache`, not `Microsoft.Extensions.Caching.Distributed.IDistributedCache`.

## Constructor and Dependencies

```csharp
public DistributedCacheService(
    ILogger<DistributedCacheService> logger,
    int maxItems = 1000)
```

- `logger` receives diagnostic messages for cache hits, writes, removals, eviction, cleanup, and clearing.
- `maxItems` is the maximum number of distinct keys retained before inserting a new key evicts the entry with the oldest `LastAccessedAt` value. It defaults to `1000`.

The service also uses a fixed default TTL of one hour when `SetAsync` is called without a `ttl` value.

## Public Methods

### Get a value

```csharp
public async Task<T?> GetAsync<T>(string key) where T : class
```

Returns the cached value as `T`, or `null` when the key is absent, expired, or the stored object is not compatible with `T`. Reading an expired entry removes it and records a miss. A successful read updates the entry's last-access timestamp and access count and records a hit.

`key` must be non-null and non-empty.

### Set a value

```csharp
public async Task SetAsync<T>(
    string key,
    T value,
    TimeSpan? ttl = null) where T : class
```

Adds or replaces an entry. The expiration time is the current UTC time plus `ttl`, or plus the one-hour default when `ttl` is `null`. If the cache is already at `maxItems` and `key` is new, the least recently accessed entry is evicted first.

`key` must be non-null and non-empty, and `value` must be non-null.

### Remove a value

```csharp
public async Task<bool> RemoveAsync(string key)
```

Removes `key` and returns `true` when an entry existed; otherwise returns `false`. `key` must be non-null and non-empty.

### Remove values by prefix

```csharp
public async Task RemoveByPatternAsync(string pattern)
```

Removes every key that starts with `pattern`, using an ordinal, case-insensitive comparison. Despite the method name, `pattern` is treated as a literal prefix; wildcard syntax and regular expressions are not interpreted. `pattern` must be non-null and non-empty.

### Clear the cache

```csharp
public async Task ClearAsync()
```

Removes all entries and resets the hit and miss counters to zero.

### Get statistics

```csharp
public async Task<DistributedCacheStatistics> GetStatisticsAsync()
```

Returns the current item count, estimated total size, hits, misses, hit rate, and estimated average item size. Size values are estimates based on each cached object's `ToString()` length.

### Clean up expired entries

```csharp
public async Task CleanupExpiredAsync()
```

Removes entries whose expiration timestamp is earlier than the current UTC time. This method is public on the concrete class but is not part of `IDistributedCache`. Expired entries are also removed lazily when requested through `GetAsync`.

## Serialization and Value Storage

The implementation performs no serialization. `SetAsync` stores the supplied object reference in `CacheEntry.Value`, and `GetAsync<T>` retrieves it with `entry.Value as T`. Consequently:

- cached values must be reference types because both generic methods use `where T : class`;
- callers receive the same object instance that was stored, so mutations to that object remain visible in the cache;
- requesting a value with an incompatible reference type returns `null` while still counting as a cache hit because the key exists;
- the reported size is not a serialized byte count: `EstimateSize` uses `value.ToString()?.Length`.

## Usage Example

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Caching;

using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole());

var logger = loggerFactory.CreateLogger<DistributedCacheService>();
IDistributedCache cache = new DistributedCacheService(logger, maxItems: 500);

var profile = new UserProfile("tenant-42", "Ada");
await cache.SetAsync("tenant-42:user:7", profile, TimeSpan.FromMinutes(15));

UserProfile? cachedProfile =
    await cache.GetAsync<UserProfile>("tenant-42:user:7");

if (cachedProfile is not null)
{
    Console.WriteLine(cachedProfile.DisplayName);
}

// Removes every key beginning with this tenant-specific prefix.
await cache.RemoveByPatternAsync("tenant-42:");

DistributedCacheStatistics statistics = await cache.GetStatisticsAsync();
Console.WriteLine($"Hits: {statistics.Hits}; misses: {statistics.Misses}");

// CleanupExpiredAsync is available through the concrete service.
var concreteCache = (DistributedCacheService)cache;
await concreteCache.CleanupExpiredAsync();

public sealed record UserProfile(string TenantId, string DisplayName);
```

Tenant separation is established by the caller's key convention in this example. The service itself does not automatically add or enforce tenant identifiers.
