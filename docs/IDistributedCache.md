# IDistributedCache

A thread-safe, multi-tenant-aware cache abstraction for distributed scenarios, implemented via `DistributedCacheService` in the `sqlite-multi-tenant` project. It provides asynchronous key-value storage with optional expiration, statistics tracking, and pattern-based cleanup, while isolating tenant data through the underlying service implementation.

## API

### `DistributedCacheService`

#### `public DistributedCacheService`
Initializes a new instance of the distributed cache service.
No parameters are required; the service is configured via dependency injection or manual setup.

#### `public async Task<T?> GetAsync<T>(string key)`
Retrieves a cached value by key and deserializes it to type `T`.
- **Parameters**:
  - `key`: The cache key to retrieve.
- **Returns**: The deserialized value of type `T`, or `null` if the key does not exist or the value is invalid.
- **Exceptions**: Throws `ArgumentNullException` if `key` is `null`.

#### `public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null)`
Stores a value in the cache with an optional absolute expiration.
- **Parameters**:
  - `key`: The cache key to store under.
  - `value`: The value to cache, which must be serializable.
  - `absoluteExpiration`: Optional expiration time from the moment of insertion.
- **Exceptions**: Throws `ArgumentNullException` if `key` or `value` is `null`.
- **Behavior**: If `absoluteExpiration` is `null`, the entry persists until explicitly removed or expired by cleanup.

#### `public async Task<bool> RemoveAsync(string key)`
Removes a single cache entry by key.
- **Parameters**:
  - `key`: The cache key to remove.
- **Returns**: `true` if the key existed and was removed; `false` otherwise.
- **Exceptions**: Throws `ArgumentNullException` if `key` is `null`.

#### `public async Task RemoveByPatternAsync(string pattern)`
Removes all cache entries whose keys match the given pattern using a simple wildcard (`*`) syntax.
- **Parameters**:
  - `pattern`: A wildcard pattern (e.g., `"user:*"`) to match keys.
- **Exceptions**: Throws `ArgumentNullException` if `pattern` is `null`.
- **Behavior**: Wildcards are expanded client-side; performance depends on key distribution and pattern specificity.

#### `public async Task ClearAsync()`
Removes all cache entries across all tenants.
- **Exceptions**: None expected under normal operation.
- **Note**: Use with caution; this operation is non-tenant-scoped and affects all tenants.

#### `public async Task<DistributedCacheStatistics> GetStatisticsAsync()`
Retrieves aggregate statistics about the cache.
- **Returns**: A `DistributedCacheStatistics` object containing item count, total size in bytes, and hit count.
- **Exceptions**: None expected.

#### `public async Task CleanupExpiredAsync()`
Removes all cache entries that have passed their expiration time.
- **Exceptions**: None expected.
- **Behavior**: Runs asynchronously and does not block; expired entries are removed in batches.

### `CacheEntry`

#### `public object? Value`
The cached value. May be `null` if the entry was stored with a `null` value.

#### `public DateTime CreatedAt`
The UTC timestamp when the entry was created.

#### `public DateTime LastAccessedAt`
The UTC timestamp when the entry was last accessed via `GetAsync`.

#### `public DateTime? ExpiresAt`
The UTC timestamp when the entry expires, or `null` if it does not expire.

#### `public long AccessCount`
The number of times the entry has been accessed via `GetAsync`.

#### `public long Size`
The estimated size of the serialized value in bytes.

### `DistributedCacheStatistics`

#### `public int ItemCount`
The total number of active cache entries.

#### `public long TotalSizeBytes`
The sum of `Size` for all active cache entries.

#### `public long Hits`
The total number of successful `GetAsync` operations (cache hits).

## Usage
