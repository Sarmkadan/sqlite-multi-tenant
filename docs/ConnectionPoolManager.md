# ConnectionPoolManager

`ConnectionPoolManager` is the default implementation of [`IConnectionPoolManager`](IConnectionPoolManager.md). It maintains an independent SQLite connection pool for each tenant, limits concurrent checkouts, reuses healthy connections, and runs a background task that periodically removes expired idle connections.

The class implements `IAsyncDisposable`. Applications should normally create one long-lived instance and dispose it during shutdown.

## Constructor

```csharp
public ConnectionPoolManager(
    ConnectionPoolOptions options,
    ILogger<ConnectionPoolManager> logger)
```

Creates the manager, validates `options`, and immediately starts the background pruning loop.

- `options`: Pool limits, timeouts, and pruning settings. The instance is retained by the manager and shared by all tenant pools.
- `logger`: Receives tenant-eviction information and connection-pruning debug messages.
- Throws `ArgumentNullException` when `options` or `logger` is `null`.
- Throws an exception from `ConnectionPoolOptions.Validate()` when an option value is invalid.

## Public Methods

### `AcquireAsync`

```csharp
public async Task<SQLiteConnection> AcquireAsync(
    string tenantId,
    string connectionString,
    CancellationToken cancellationToken = default)
```

Acquires an open connection for `tenantId`. The tenant pool is created lazily on its first acquisition. Healthy idle connections are reused; otherwise, a new `SQLiteConnection` is opened.

The connection string supplied when a tenant pool is first created is retained for that pool. Later calls for the same tenant do not replace it, even if they pass a different string.

- `tenantId`: Non-null, non-empty tenant identifier.
- `connectionString`: Non-null, non-empty SQLite connection string used when opening new connections.
- `cancellationToken`: Cancels waiting for capacity or opening a new connection.
- Returns an open `SQLiteConnection` that must be returned with `ReleaseAsync`.
- Throws `ArgumentNullException` or `ArgumentException` when `tenantId` or `connectionString` is null or empty.
- Throws `TimeoutException` if no pool slot becomes available within `ConnectionPoolOptions.AcquireTimeout`.
- Propagates caller cancellation and errors raised while opening the connection.

### `ReleaseAsync`

```csharp
public async Task ReleaseAsync(string tenantId, SQLiteConnection connection)
```

Returns an open connection to the matching tenant's idle queue. A non-open connection is disposed instead. If the tenant pool does not exist, including after eviction, the connection is disposed. Passing `null` for `connection` is a no-op despite the non-nullable signature.

Call this exactly once for each successfully acquired connection and use the same tenant ID. The implementation does not validate that a connection belongs to the specified pool.

### `EvictTenantAsync`

```csharp
public async Task EvictTenantAsync(string tenantId)
```

Removes the tenant pool, disposes all connections currently in its idle queue, disposes the pool's capacity semaphore, and logs the eviction. If no pool exists for `tenantId`, the method completes without action.

Callers should ensure all checked-out connections have been released before eviction. Checked-out connections are not held in the idle queue and therefore are not closed by eviction; releasing one after its pool has been removed disposes the connection.

### `GetStatistics`

```csharp
public IReadOnlyDictionary<string, PoolStatisticsSnapshot> GetStatistics()
```

Returns a new dictionary containing a point-in-time snapshot for every tenant pool currently registered. Each `PoolStatisticsSnapshot` includes the tenant ID, idle and total connection counts, a saturation indicator, cumulative pruned count, and last prune timestamp. The returned dictionary is a snapshot and is not updated as pool state changes.

### `DisposeAsync`

```csharp
public async ValueTask DisposeAsync()
```

Cancels and awaits the background pruning task, disposes every registered tenant pool and its idle connections, clears the pool registry, and disposes the shutdown token source. Cancellation of the pruning task during normal shutdown is suppressed.

Dispose the manager only after application code has stopped acquiring connections and has released all checked-out connections. Disposal closes idle connections, but it does not track and close connections that remain checked out. The implementation does not make repeated disposal or use after disposal safe.

## How ConnectionPoolOptions Is Consumed

See [`ConnectionPoolOptions`](ConnectionPoolOptions.md) for the options API and validation rules. The manager consumes the values as follows:

| Option | Use |
| --- | --- |
| `MinPoolSize` | During pruning, preserves at least this many currently idle connections in each tenant pool. It does not eagerly create a minimum number of connections. |
| `MaxPoolSize` | Initializes a per-tenant semaphore and therefore limits simultaneous checked-out connections for that tenant. |
| `IdleTimeout` | Rejects an idle connection during acquisition and makes it eligible for periodic pruning once enough idle connections remain to satisfy `MinPoolSize`. |
| `AcquireTimeout` | Limits how long `AcquireAsync` waits for a tenant's semaphore slot. Expiration is translated to `TimeoutException`; caller cancellation remains cancellation. |
| `MaxConnectionLifetime` | Rejects an over-age idle connection during acquisition and makes it eligible for periodic pruning, subject to `MinPoolSize`. Checked-out connections are not forcibly closed when they reach this age. |
| `PruneInterval` | Sets the interval of the `PeriodicTimer` used by the background pruning loop. |

`Validate()` is called once by the constructor before the pruning task starts. The manager retains the supplied options object rather than copying it. Tenant semaphores and the pruning timer are initialized from option values when they are created, while timeout and health checks read retained option values during operation; treat the options as startup configuration rather than mutating them after construction.

## Usage Example

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Database;
using System.Data.SQLite;

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

var options = new ConnectionPoolOptions
{
    MinPoolSize = 1,
    MaxPoolSize = 10,
    AcquireTimeout = TimeSpan.FromSeconds(15),
    IdleTimeout = TimeSpan.FromMinutes(5),
    MaxConnectionLifetime = TimeSpan.FromHours(1),
    PruneInterval = TimeSpan.FromMinutes(1)
};

await using var poolManager = new ConnectionPoolManager(
    options,
    loggerFactory.CreateLogger<ConnectionPoolManager>());

const string tenantId = "tenant-123";
const string connectionString = "Data Source=tenant-123.db;Version=3;";

SQLiteConnection? connection = null;
try
{
    connection = await poolManager.AcquireAsync(tenantId, connectionString);

    using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM Orders";
    var orderCount = Convert.ToInt64(await command.ExecuteScalarAsync());
    Console.WriteLine($"Orders: {orderCount}");
}
finally
{
    if (connection is not null)
        await poolManager.ReleaseAsync(tenantId, connection);
}

var statistics = poolManager.GetStatistics();
if (statistics.TryGetValue(tenantId, out var tenantStatistics))
    Console.WriteLine($"Open: {tenantStatistics.Total}; idle: {tenantStatistics.Available}");

// Before deprovisioning a tenant, stop its database work and release its connections.
await poolManager.EvictTenantAsync(tenantId);
```
