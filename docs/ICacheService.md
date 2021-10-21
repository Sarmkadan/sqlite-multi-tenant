# ICacheService

`ICacheService` is a contract for a cache layer that supports multi-tenancy in the `sqlite-multi-tenant` project. It provides methods to store, retrieve, and invalidate cached data while isolating cache entries by tenant. The interface is implemented by `CacheService`, which uses a backing store (e.g., in-memory or distributed cache) to manage tenant-scoped data.

## API

### `T Get<T>(string key)`
Retrieves a cached value of type `T` associated with the specified `key`. The key is automatically prefixed with the current tenant identifier to ensure isolation.

- **Parameters**:
  - `key` – The cache key without tenant prefix. Must not be null or empty.
- **Return value**:
  - The deserialized value of type `T` if found; otherwise, `default(T)`.
- **Exceptions**:
  - Throws `ArgumentNullException` if `key` is null.
  - Throws `ArgumentException` if `key` is empty.

---

### `void Set<T>(string key, T value, TimeSpan? expiration = null)`
Stores a value of type `T` in the cache under the specified `key`, scoped to the current tenant. Optionally sets an expiration time.

- **Parameters**:
  - `key` – The cache key without tenant prefix. Must not be null or empty.
  - `value` – The value to cache. Can be null.
  - `expiration` – Optional time span after which the entry expires. If null, uses default cache policy.
- **Exceptions**:
  - Throws `ArgumentNullException` if `key` is null.
  - Throws `ArgumentException` if `key` is empty.

---

### `void Remove(string key)`
Removes a cache entry associated with the specified `key` from the current tenant’s scope.

- **Parameters**:
  - `key` – The cache key without tenant prefix. Must not be null or empty.
- **Exceptions**:
  - Throws `ArgumentNullException` if `key` is null.
  - Throws `ArgumentException` if `key` is empty.

---
### `void RemoveByPattern(string pattern)`
Removes all cache entries in the current tenant’s scope whose keys match the specified `pattern`. Pattern matching is implementation-defined (e.g., supports wildcards like `*`).

- **Parameters**:
  - `pattern` – A key pattern to match. Must not be null or empty.
- **Exceptions**:
  - Throws `ArgumentNullException` if `pattern` is null.
  - Throws `ArgumentException` if `pattern` is empty.

---
### `void Clear()`
Removes all cache entries associated with the current tenant.

---
### `string TenantKey`
Static property providing the cache key prefix used to isolate tenant-specific entries. Format: `"tenant:{TenantId}"`.

---
### `string AllTenantsKey`
Static property providing the cache key used to store or enumerate data across all tenants. Format: `"tenants"`.

---
### `string TenantPattern`
Static property providing a pattern to match all tenant-scoped keys. Format: `"tenant:*"`.

---
### `string BackupKey`
Static property providing the cache key prefix for backup-related entries. Format: `"backup:{BackupId}"`.

---
### `string BackupsForDatabase`
Static property providing the cache key for listing backups associated with a database. Format: `"backups:{DatabaseId}"`.

---
### `string BackupPattern`
Static property providing a pattern to match all backup-related keys. Format: `"backup:*"`.

---
### `string MigrationKey`
Static property providing the cache key prefix for migration-related entries. Format: `"migration:{MigrationId}"`.

---
### `string PendingMigrationsKey`
Static property providing the cache key for pending migrations. Format: `"migrations:pending"`.

---
### `string AppliedMigrationsKey`
Static property providing the cache key for applied migrations. Format: `"migrations:applied"`.

---
### `string MigrationPattern`
Static property providing a pattern to match all migration-related keys. Format: `"migration:*"`.

---
### `string HealthCheckKey`
Static property providing the cache key for health check status. Format: `"health"`.

---
### `string ConfigurationKey`
Static property providing the cache key for configuration data. Format: `"config:{ConfigKey}"`.

---
### `CacheInvalidationService`
A sealed class responsible for triggering cache invalidation events across tenants. Not part of `ICacheService` but commonly used alongside it.

## Usage

### Example 1: Storing and retrieving tenant-scoped data
