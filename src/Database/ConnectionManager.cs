#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Database
{
    // Manages connection pooling and lifecycle for per-tenant SQLite databases
    // Implements connection reuse to minimize resource overhead and improve performance
    public sealed class ConnectionManager : IDisposable {
        private readonly ConcurrentDictionary<string, ConnectionPool> _pools;
        private readonly ILogger<ConnectionManager> _logger;
        private readonly int _maxConnectionsPerPool;
        private bool _disposed;

        public ConnectionManager(ILogger<ConnectionManager> logger, int maxConnectionsPerPool = 10)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxConnectionsPerPool = maxConnectionsPerPool;
            _pools = new ConcurrentDictionary<string, ConnectionPool>();
        }

        /// <summary>
        /// Acquires a connection for the given tenant with timeout protection.
        /// Returns an open <see cref="SQLiteConnection"/> from the pool or creates a new one.
        /// </summary>
        /// <param name="tenantId">The unique identifier of the tenant.</param>
        /// <param name="connectionString">SQLite connection string for the tenant database.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>An open SQLite connection bound to the tenant database.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the connection manager has been disposed.</exception>
        /// <exception cref="ArgumentNullException">Thrown when tenantId or connectionString is null or empty.</exception>
        public async Task<SQLiteConnection> GetConnectionAsync(string tenantId, string connectionString,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            var pool = _pools.GetOrAdd(tenantId,
                _ => new ConnectionPool(connectionString, _maxConnectionsPerPool, _logger));

            return await pool.GetConnectionAsync(cancellationToken);
        }

        /// <summary>
        /// Releases a connection back to the pool for reuse.
        /// Broken connections are disposed rather than returned to the pool.
        /// </summary>
        /// <param name="tenantId">The tenant whose pool should receive the connection.</param>
        /// <param name="connection">The connection to release.</param>
        public async Task ReleaseConnectionAsync(string tenantId, SQLiteConnection connection)
        {
            if (_pools.TryGetValue(tenantId, out var pool))
            {
                await pool.ReleaseConnectionAsync(connection);
            }
        }

        /// <summary>
        /// Clears all connections for a specific tenant (useful for tenant deletion or suspension).
        /// </summary>
        /// <param name="tenantId">The tenant whose connection pool should be removed.</param>
        public async Task ClearTenantPoolAsync(string tenantId)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_pools.TryRemove(tenantId, out var pool))
            {
                await pool.DisposeAsync();
                _logger.LogInformation("Connection pool cleared for tenant: {TenantId}", tenantId);
            }
        }

        // Gets current pool statistics for monitoring
        public Dictionary<string, PoolStatistics> GetPoolStatistics()
        {
            var stats = new Dictionary<string, PoolStatistics>();

            foreach (var kvp in _pools)
            {
                stats[kvp.Key] = new PoolStatistics
                {
                    TenantId = kvp.Key,
                    AvailableConnections = kvp.Value.AvailableCount,
                    TotalConnections = kvp.Value.TotalCount,
                    WaitingRequests = kvp.Value.WaitingCount
                };
            }

            return stats;
        }

        public void Dispose()
        {
            if (_disposed) return;

            foreach (var pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
            _disposed = true;
        }

        private class ConnectionPool : IAsyncDisposable
        {
            private readonly string _connectionString;
            private readonly int _maxConnections;
            private readonly ILogger<ConnectionManager> _logger;
            private readonly SemaphoreSlim _semaphore;
            private readonly ConcurrentBag<SQLiteConnection> _availableConnections;
            private int _totalConnections;

            public int AvailableCount => _availableConnections.Count;
            public int TotalCount => _totalConnections;
            public int WaitingCount => _semaphore.CurrentCount == 0 ? 1 : 0;

            public ConnectionPool(string connectionString, int maxConnections, ILogger<ConnectionManager> logger)
            {
                _connectionString = connectionString;
                _maxConnections = maxConnections;
                _logger = logger;
                _semaphore = new SemaphoreSlim(maxConnections, maxConnections);
                _availableConnections = new ConcurrentBag<SQLiteConnection>();
                _totalConnections = 0;
            }

            public async Task<SQLiteConnection> GetConnectionAsync(CancellationToken cancellationToken)
            {
                await _semaphore.WaitAsync(cancellationToken);

                SQLiteConnection connection;

                if (_availableConnections.TryTake(out connection))
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    return connection;
                }

                connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();
                Interlocked.Increment(ref _totalConnections);

                return connection;
            }

            public async Task ReleaseConnectionAsync(SQLiteConnection connection)
            {
                if (connection?.State == System.Data.ConnectionState.Open)
                {
                    _availableConnections.Add(connection);
                }
                else
                {
                    connection?.Dispose();
                    Interlocked.Decrement(ref _totalConnections);
                }

                _semaphore.Release();
            }

            public async ValueTask DisposeAsync()
            {
                while (_availableConnections.TryTake(out var connection))
                {
                    connection?.Dispose();
                }

                _semaphore?.Dispose();
            }

            public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
        }
    }

    public sealed class PoolStatistics {
        public string TenantId { get; set; }
        public int AvailableConnections { get; set; }
        public int TotalConnections { get; set; }
        public int WaitingRequests { get; set; }
    }
}
