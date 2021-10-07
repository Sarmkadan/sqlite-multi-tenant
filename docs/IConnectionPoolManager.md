# IConnectionPoolManager

Provides a managed pool of `SQLiteConnection` instances for multi-tenant applications, enabling efficient reuse of database connections while maintaining isolation between tenants.

## API

### `ConnectionPoolManager`
Initializes a new connection pool manager with default configuration.

### `AcquireAsync(string tenantId)`
Acquires a `SQLiteConnection` for the specified tenant.

- **tenantId**: Identifier of the tenant for which the connection is requested.
- **Returns**: A `Task<SQLiteConnection>` representing the asynchronous operation. The connection is ready for use.
- **Throws**: `ArgumentNullException` if `tenantId` is null.
- **Throws**: `InvalidOperationException` if the pool for the tenant is not initialized or has been evicted.

### `ReleaseAsync(string tenantId, SQLiteConnection connection)`
Releases a `SQLiteConnection` back to the pool for the specified tenant.

- **tenantId**: Identifier of the tenant associated with the connection.
- **connection**: The `SQLiteConnection` to return to the pool.
- **Throws**: `ArgumentNullException` if `tenantId` or `connection` is null.
- **Throws**: `InvalidOperationException` if the tenant's pool does not exist or the connection is not tracked.

### `EvictTenantAsync(string tenantId)`
Removes all connections associated with the specified tenant from the pool, closing and disposing them.

- **tenantId**: Identifier of the tenant to evict.
- **Returns**: A `Task` representing the asynchronous operation.
- **Throws**: `ArgumentNullException` if `tenantId` is null.

### `GetStatistics()`
Gathers and returns statistics for all tenant pools.

- **Returns**: An `IReadOnlyDictionary<string, PoolStatisticsSnapshot>` mapping tenant identifiers to their respective pool statistics.

### `DisposeAsync()`
Asynchronously releases all resources held by the connection pool manager.

- **Returns**: A `ValueTask` representing the asynchronous disposal operation.

### `TenantPool`
Gets the dictionary of tenant-specific connection pools.

- **Returns**: An `IReadOnlyDictionary<string, TenantConnectionPool>` representing the current tenant pools.

### `AcquireAsync(SQLiteConnection connection)`
Acquires a `SQLiteConnection` from the pool (used internally by the manager).

- **connection**: The `SQLiteConnection` to acquire.
- **Returns**: A `Task<SQLiteConnection>` representing the asynchronous operation.

### `ReleaseAsync(SQLiteConnection connection)`
Releases a `SQLiteConnection` back to the pool (used internally by the manager).

- **connection**: The `SQLiteConnection` to release.
- **Returns**: A `Task` representing the asynchronous operation.

### `PruneIdle()`
Removes idle connections from all tenant pools, closing and disposing them.

### `GetSnapshot()`
Gets a snapshot of the current pool statistics for the associated tenant pool.

- **Returns**: A `PoolStatisticsSnapshot` representing the current state of the pool.

### `DisposeAsync()`
Asynchronously releases resources used by the tenant connection pool.

- **Returns**: A `ValueTask` representing the asynchronous disposal operation.

### `CreatedAt`
Gets the timestamp when the tenant pool was created.

- **Returns**: A `DateTimeOffset` indicating when the pool was initialized.

### `LastReturnedAt`
Gets the timestamp when a connection was last returned to the pool.

- **Returns**: A `DateTimeOffset` indicating the last return time.
