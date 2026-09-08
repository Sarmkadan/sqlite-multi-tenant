# Cache strategies

`src/Caching/CacheStrategy.cs` contains the cache contract, two in-memory cache strategies, and the statistics type returned by the LRU implementation. For the cache contract itself, see [`ICacheStrategy.md`](ICacheStrategy.md).

All cache operations use string keys. Both implementations store entries in a `ConcurrentDictionary<string, CacheEntry>`. Their methods return `Task` or `Task<T>`, although the work shown in the current implementations completes synchronously.

## Declared types

### `ICacheStrategy`

The public interface implemented by both strategies. Its public methods are:

```csharp
Task<T> GetAsync<T>(string key);
Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
Task RemoveAsync(string key);
Task ClearAsync();
```

See [`ICacheStrategy.md`](ICacheStrategy.md) for the interface documentation.

### `LruCacheStrategy`

A sealed, capacity-limited cache with optional per-entry expiration and access statistics. It implements `ICacheStrategy`.

Constructor:

```csharp
LruCacheStrategy(ILogger<LruCacheStrategy> logger, int maxSize = 1000)
```

Public methods:

```csharp
Task<T> GetAsync<T>(string key);
Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
Task RemoveAsync(string key);
Task ClearAsync();
Dictionary<string, CacheStatistics> GetStatistics();
```

Behavior visible in the implementation:

- `GetAsync<T>` returns `default(T)` for a null or empty key, a missing key, an expired entry, or an error. An expired entry is removed when it is read. A successful read updates `LastAccessedAt` and increments `AccessCount` before casting the stored value to `T`.
- `SetAsync<T>` ignores null or empty keys and null values. It records UTC creation and last-access timestamps, starts `AccessCount` at `1`, and calculates `ExpiresAt` from the optional `TimeSpan`; without an expiration, the entry does not expire.
- Before every valid set, if the current count is at least `maxSize`, the entry with the oldest `LastAccessedAt` is removed. This check also occurs when the set will replace an existing key.
- Expired entries are not swept proactively and are not preferred during capacity eviction; expiration removal happens only when that key is read.
- `RemoveAsync` removes one key if present. `ClearAsync` removes every entry.
- `GetStatistics` returns a new dictionary containing a snapshot of each current entry's metadata. It does not remove expired entries.
- The implementation catches and logs errors in get, set, remove, and clear operations. A failed get returns `default(T)`.

### `TimeBasedCacheStrategy`

A sealed cache in which every stored entry has a time-to-live. It implements `ICacheStrategy`.

Constructor:

```csharp
TimeBasedCacheStrategy(
    ILogger<TimeBasedCacheStrategy> logger,
    TimeSpan? defaultExpiration = null)
```

The default expiration is one hour when the constructor argument is omitted or null.

Public methods:

```csharp
Task<T> GetAsync<T>(string key);
Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
Task RemoveAsync(string key);
Task ClearAsync();
```

Behavior visible in the implementation:

- `SetAsync<T>` ignores null or empty keys and null values. A supplied expiration overrides the strategy's default for that entry, and setting an existing key replaces its value and expiration timestamp.
- `GetAsync<T>` returns `default(T)` for a null or empty key, a missing key, an expired entry, or an error. An expired entry is removed when read.
- Expiration is lazy: there is no background sweep, size limit, or capacity-based eviction, so an expired entry remains stored until its key is read, explicitly removed, replaced, or the cache is cleared.
- `RemoveAsync` removes one key if present. `ClearAsync` removes every entry.
- Get and set errors are caught and logged. Remove and clear call the dictionary directly.

### `CacheStatistics`

A sealed data-transfer type used by `LruCacheStrategy.GetStatistics()`. It exposes no public methods. Its public properties are:

```csharp
string Key { get; set; }
DateTime CreatedAt { get; set; }
DateTime LastAccessedAt { get; set; }
int AccessCount { get; set; }
DateTime? ExpiresAt { get; set; }
```

There are no enums declared in `CacheStrategy.cs`.

## Usage example

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Caching;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole());

var lru = new LruCacheStrategy(
    loggerFactory.CreateLogger<LruCacheStrategy>(),
    maxSize: 100);

await lru.SetAsync("tenant:42:profile", "cached profile", TimeSpan.FromMinutes(10));
string profile = await lru.GetAsync<string>("tenant:42:profile");
Dictionary<string, CacheStatistics> statistics = lru.GetStatistics();
await lru.RemoveAsync("tenant:42:profile");

ICacheStrategy timeBased = new TimeBasedCacheStrategy(
    loggerFactory.CreateLogger<TimeBasedCacheStrategy>(),
    defaultExpiration: TimeSpan.FromMinutes(5));

await timeBased.SetAsync("tenant:42:settings", new { Theme = "dark" });
await timeBased.ClearAsync();
```
