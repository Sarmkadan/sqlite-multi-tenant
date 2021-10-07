# ConnectionManager

The `ConnectionManager` class manages a pool of SQLite connections for a multi-tenant environment. It provides methods to obtain, release, and monitor connections, supporting both standard and encrypted connections. Each tenant’s connections are isolated in a dedicated pool, and the class implements `IDisposable` to allow deterministic cleanup of resources.

## API

### Constructor

```csharp
public ConnectionManager()
```

Initializes a new instance of the `ConnectionManager` class.

### Methods

#### `GetConnectionAsync`

```csharp
public async Task<SQLiteConnection> GetConnectionAsync()
```

Retrieves a `SQLiteConnection` from the appropriate tenant pool asynchronously.  
**Returns:** A task that represents the asynchronous operation. The task result contains the `SQLiteConnection`.  
**Throws:** `ObjectDisposedException` if the manager has been disposed.

#### `GetEncryptedConnectionAsync`

```csharp
public async Task<SQLiteConnection> GetEncryptedConnectionAsync()
```

Retrieves an encrypted `SQLiteConnection` from the appropriate tenant pool asynchronously.  
**Returns:** A task that represents the asynchronous operation. The task result contains the encrypted `SQLiteConnection`.  
**Throws:** `ObjectDisposedException` if the manager has been disposed.

#### `ReleaseConnectionAsync`

```csharp
public async Task ReleaseConnectionAsync()
```

Releases a previously obtained connection back to its tenant pool asynchronously.  
**Returns:** A task that represents the asynchronous release operation.  
**Throws:** `ObjectDisposedException` if the manager has been disposed.

#### `ClearTenantPoolAsync`

```csharp
public async Task ClearTenantPoolAsync()
```

Clears the connection pool for a specific tenant asynchronously, closing all idle connections.  
**Returns:** A task that represents the asynchronous clear operation.  
**Throws:** `ObjectDisposedException` if the manager has been disposed.

#### `GetPoolStatistics`

```csharp
public Dictionary<string, PoolStatistics> GetPoolStatistics()
```

Returns a snapshot of statistics for all tenant pools.  
**Returns:** A dictionary mapping tenant identifiers to their corresponding `PoolStatistics` objects.

#### `Dispose`

```csharp
public void Dispose()
```

Releases all resources used by the `ConnectionManager`. After calling this method, the instance cannot be used for further connection operations.

### Nested Types

#### `ConnectionPool`

```csharp
public sealed class ConnectionPool : IDisposable
```

Represents a pool of connections for a single tenant. This class is used internally by `ConnectionManager` but is exposed for advanced scenarios.

**Members:**

- `public async Task<SQLiteConnection> GetConnectionAsync()`  
  Retrieves a connection from the pool asynchronously.

- `public async Task ReleaseConnectionAsync()`  
  Releases a connection back to the pool asynchronously.

- `public async ValueTask DisposeAsync()`  
  Performs asynchronous cleanup of the pool.

- `public void Dispose()`  
  Performs synchronous cleanup of the pool.

#### `PoolStatistics`

```csharp
public sealed class PoolStatistics
```

Provides statistics about a single tenant’s connection pool.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `TenantId` | `string` | The identifier of the tenant. |
| `AvailableConnections` | `int` | The number of connections currently idle in the pool. |
| `TotalConnections` | `int` | The total number of connections managed by the pool (idle + in use). |
| `WaitingRequests` | `int` | The number of requests currently waiting for a connection to become available. |

## Usage

### Basic Connection Acquisition and Release

```csharp
using var manager = new ConnectionManager();

// Obtain a connection for the default tenant
var connection = await manager.GetConnectionAsync();

try
{
    // Use the connection for database operations
    await connection.ExecuteAsync("SELECT 1");
}
finally
{
    // Always release the connection back to the pool
    await manager.ReleaseConnectionAsync();
}
```

### Monitoring Pool Statistics

```csharp
var manager = new ConnectionManager();

// Perform some operations...
var conn1 = await manager.GetConnectionAsync();
var conn2 = await manager.GetEncryptedConnectionAsync();

// Inspect pool health
var stats = manager.GetPoolStatistics();
foreach (var kvp in stats)
{
    Console.WriteLine($"Tenant: {kvp.Key}");
    Console.WriteLine($"  Available: {kvp.Value.AvailableConnections}");
    Console.WriteLine($"  Total: {kvp.Value.TotalConnections}");
    Console.WriteLine($"  Waiting: {kvp.Value.WaitingRequests}");
}

// Clean up
await manager.ReleaseConnectionAsync();
await manager.ReleaseConnectionAsync();
manager.Dispose();
```

## Notes

- **Thread Safety:** `ConnectionManager` is designed for concurrent access from multiple threads. All public instance methods are thread-safe. However, individual `SQLiteConnection` objects obtained from the manager are **not** thread-safe and should not be shared across threads without synchronization.
- **Disposal:** Always call `Dispose` (or use `using` / `await using`) when the manager is no longer needed. After disposal, any attempt to call `GetConnectionAsync`, `GetEncryptedConnectionAsync`, `ReleaseConnectionAsync`, or `ClearTenantPoolAsync` will throw an `ObjectDisposedException`.
- **Connection Leaks:** Failing to call `ReleaseConnectionAsync` after obtaining a connection will cause the pool to exhaust its available connections, leading to deadlocks or timeouts. Use `try`/`finally` or `using` patterns to ensure release.
- **Encrypted Connections:** `GetEncryptedConnectionAsync` returns a connection that uses SQLite’s encryption extension (e.g., SQLCipher). The underlying pool is separate from the standard connection pool.
- **Pool Statistics:** The dictionary returned by `GetPoolStatistics` is a snapshot and may become stale immediately after retrieval. It is intended for monitoring and diagnostics, not for synchronization.
- **Nested Types:** `ConnectionPool` is exposed for advanced use cases (e.g., custom pooling logic). Direct manipulation of a pool should be done with care to avoid interfering with the manager’s internal state.
