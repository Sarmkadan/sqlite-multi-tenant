# CacheService

`CacheService` is the in-memory implementation of `ICacheService` in the `SqliteMultiTenant.Caching` namespace. It stores values in an injected `IMemoryCache` and keeps a concurrent list of the keys it has written so that entries can be removed by prefix or cleared as a group.

The service does not add tenant identifiers to keys. Callers should use a tenant-aware key convention, or the `CacheKeys` helpers declared in the same source file, when cached data must be isolated by tenant.

## Relation to `ICacheService`

`CacheService` is a sealed class that implements the [`ICacheService`](ICacheService.md) contract. Its public `Get`, `Set`, `Remove`, `RemoveByPattern`, and `Clear` methods correspond directly to the interface members. Consumers can therefore depend on `ICacheService` while using `CacheService` as the in-memory implementation.

This page describes the concrete implementation in `src/Caching/CacheService.cs`. In particular, key scoping and validation are performed as described below; `CacheService` does not automatically prefix keys and does not throw for blank keys in `Get`, `Set`, or `Remove`.

## Constructor

```csharp
public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
```

- `cache` is the backing memory cache. Passing `null` throws `ArgumentNullException`.
- `logger` records cache hits, misses, writes, removals, pattern invalidations, and clears. Passing `null` throws `ArgumentNullException`.

The constructor also creates an empty, thread-safe key-tracking dictionary. Only keys written through this `CacheService` instance are tracked for `RemoveByPattern` and `Clear`.

## Public Methods

### Get

```csharp
public T Get<T>(string key)
```

Returns the cached value when `key` exists and the stored object is compatible with `T`. It returns `default(T)` when the key is null, empty, or whitespace, when no entry exists, when the entry has expired, or when its value is not compatible with `T`.

An unsuccessful lookup for a nonblank key is logged as a cache miss. A successful lookup is logged as a cache hit.

### Set

```csharp
public void Set<T>(string key, T value, TimeSpan? expiration = null)
```

Adds or replaces `key` with `value` and records the key for later invalidation. If `key` is null, empty, or whitespace, or if `value` is null, the method returns without changing the cache.

`expiration` is configured as a sliding expiration:

- When supplied, the provided `TimeSpan` is used.
- When omitted or `null`, the default sliding expiration is **one hour**.

Because the policy is sliding, a successful cache access refreshes the interval. There are no other default expiration values in `CacheService`.

### Remove

```csharp
public void Remove(string key)
```

Removes `key` from both the backing cache and the tracked-key dictionary. A null, empty, or whitespace key is ignored. Removing a key that does not exist is safe.

### RemoveByPattern

```csharp
public void RemoveByPattern(string pattern)
```

Removes tracked keys whose beginning matches the supplied pattern after every `*` character has been removed. For example, `"sqlmt:tenant:*"` becomes the prefix `"sqlmt:tenant:"`. This is prefix matching, not general wildcard or regular-expression matching, and it uses the runtime's default case-sensitive `string.StartsWith(string)` behavior.

Only keys tracked by this service instance are considered. A null pattern causes `NullReferenceException` when the implementation calls `Replace`; an empty pattern matches every tracked key.

### Clear

```csharp
public void Clear()
```

Removes every tracked key from the backing cache, then empties the tracked-key dictionary. It does not dispose the injected `IMemoryCache`, and it does not remove entries that were placed in that cache by another component without going through this service instance.

## Usage Example

```csharp
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Caching;

using var memoryCache = new MemoryCache(new MemoryCacheOptions());
using var loggerFactory = LoggerFactory.Create(builder => { });

ICacheService cache = new CacheService(
    memoryCache,
    loggerFactory.CreateLogger<CacheService>());

string tenantId = "tenant-42";
string key = CacheKeys.TenantKey(tenantId);

// Uses the default one-hour sliding expiration.
cache.Set(key, new TenantSummary(tenantId, "Acme"));

TenantSummary? tenant = cache.Get<TenantSummary>(key);
if (tenant is not null)
{
    Console.WriteLine(tenant.Name);
}

// A custom sliding expiration can be supplied when writing.
cache.Set(
    CacheKeys.HealthCheckKey(),
    "healthy",
    TimeSpan.FromMinutes(5));

cache.Remove(key);
cache.RemoveByPattern(CacheKeys.TenantPattern());
cache.Clear();

public sealed record TenantSummary(string Id, string Name);
```
