#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Data.SQLite;

namespace SqliteMultiTenant.Database;

/// <summary>
/// A lightweight, single-tenant SQLite connection pool. Maintains a bounded set
/// of open <see cref="SQLiteConnection"/> instances and reuses them across callers,
/// tracking idle time and enforcing a maximum pool size. For per-tenant pooling
/// across multiple tenants, see <see cref="ConnectionPoolManager"/>.
/// </summary>
public sealed class ConnectionPool : IAsyncDisposable, IDisposable {
    private readonly string _connectionString;
    private readonly int _maxSize;
    private readonly TimeSpan _idleTimeout;
    private readonly ILogger<ConnectionPool> _logger;
    private readonly ConcurrentBag<PooledConnection> _available = new();
    private readonly SemaphoreSlim _semaphore;
    private int _totalCreated;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionPool"/> class.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="maxSize">Maximum number of concurrent connections. Default: 10.</param>
    /// <param name="idleTimeout">How long an idle connection stays in the pool before disposal. Default: 5 minutes.</param>
    /// <exception cref="ArgumentException">Thrown when connection string is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxSize is less than 1.</exception>
    public ConnectionPool(
        string connectionString,
        ILogger<ConnectionPool> logger,
        int maxSize = 10,
        TimeSpan? idleTimeout = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        if (maxSize < 1)
            throw new ArgumentOutOfRangeException(nameof(maxSize), "Max pool size must be at least 1.");

        _connectionString = connectionString;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxSize = maxSize;
        _idleTimeout = idleTimeout ?? TimeSpan.FromMinutes(5);
        _semaphore = new SemaphoreSlim(maxSize, maxSize);
    }

    /// <summary>
    /// Acquires a connection from the pool. Reuses an idle connection if one is available
    /// and still healthy; otherwise creates a new one. Blocks if the pool is at capacity.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for a slot. Default: 30 seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open <see cref="SQLiteConnection"/>.</returns>
    /// <exception cref="TimeoutException">Thrown when no slot becomes available within the timeout.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
    public async Task<SQLiteConnection> AcquireAsync(
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var acquired = await _semaphore.WaitAsync(timeout ?? TimeSpan.FromSeconds(30), cancellationToken);
        if (!acquired)
            throw new TimeoutException("Timed out waiting for a connection pool slot.");

        while (_available.TryTake(out var pooled))
        {
            if (DateTimeOffset.UtcNow - pooled.ReturnedAt > _idleTimeout)
            {
                pooled.Connection.Dispose();
                Interlocked.Decrement(ref _totalCreated);
                continue;
            }

            if (pooled.Connection.State == System.Data.ConnectionState.Open)
            {
                // Additional health check: try to create a command to verify connection is usable
                try
                {
                    using var cmd = pooled.Connection.CreateCommand();
                    cmd.CommandText = "SELECT 1";
                    // We don't await this as it's a sync call on SQLiteConnection
                    // but we can execute it synchronously for health check
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        return pooled.Connection;
                    }
                }
                catch
                {
                    // If health check fails, dispose the connection and continue
                    pooled.Connection.Dispose();
                    Interlocked.Decrement(ref _totalCreated);
                    continue;
                }
            }

            pooled.Connection.Dispose();
            Interlocked.Decrement(ref _totalCreated);
        }

        var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        Interlocked.Increment(ref _totalCreated);

        _logger.LogDebug("Created new pooled connection. Total open: {Total}", _totalCreated);
        return connection;
    }

    /// <summary>
    /// Returns a connection to the pool for reuse. If the connection is broken,
    /// it is disposed instead.
    /// </summary>
    /// <param name="connection">The connection to return.</param>
    public void Release(SQLiteConnection connection)
    {
        if (_disposed)
        {
            // If the pool is disposed, we don't interfere with the connection.
            // Leased connections are managed by their owners and will be disposed by them.
            // We also don't touch the semaphore as it has already been disposed.
            return;
        }

        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Dispose();
                Interlocked.Decrement(ref _totalCreated);
                _semaphore.Release();
                return;
            }
        }
        catch (ObjectDisposedException)
        {
            // Connection is already disposed, just decrement counter and release semaphore
            Interlocked.Decrement(ref _totalCreated);
            _semaphore.Release();
            return;
        }

        _available.Add(new PooledConnection(connection, DateTimeOffset.UtcNow));
        _semaphore.Release();
    }

    /// <summary>
    /// Returns the current number of connections (both idle and checked-out) in this pool.
    /// </summary>
    public int TotalConnections => _totalCreated;

    /// <summary>
    /// Returns the number of idle connections available for immediate checkout.
    /// </summary>
    public int AvailableConnections => _available.Count;

    /// <summary>
    /// Removes and disposes all idle connections that have exceeded the idle timeout.
    /// </summary>
    /// <returns>The number of connections pruned.</returns>
    public int PruneIdle()
    {
        var pruned = 0;
        var keep = new List<PooledConnection>();

        while (_available.TryTake(out var pooled))
        {
            if (DateTimeOffset.UtcNow - pooled.ReturnedAt > _idleTimeout)
            {
                pooled.Connection.Dispose();
                Interlocked.Decrement(ref _totalCreated);
                pruned++;
            }
            else
            {
                keep.Add(pooled);
            }
        }

        foreach (var item in keep)
            _available.Add(item);

        if (pruned > 0)
            _logger.LogDebug("Pruned {Count} idle connections", pruned);

        return pruned;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        while (_available.TryTake(out var pooled))
        {
            pooled.Connection.Dispose();
        }

        _semaphore.Dispose();
        _totalCreated = 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        while (_available.TryTake(out var pooled))
        {
            pooled.Connection.Dispose();
        }

        _semaphore.Dispose();
        _totalCreated = 0;
    }

    private sealed record PooledConnection(SQLiteConnection Connection, DateTimeOffset ReturnedAt);
}
