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
using SqliteMultiTenant.Exceptions;

namespace SqliteMultiTenant.Database
{
    /// <summary>
    /// Manages connection pooling and lifecycle for per-tenant SQLite databases.
    /// Implements connection reuse to minimize resource overhead and improve performance.
    /// </summary>
    public sealed class ConnectionManager : IDisposable {
        private readonly ConcurrentDictionary<string, ConnectionPool> _pools;
        private readonly ILogger<ConnectionManager> _logger;
        private readonly int _maxConnectionsPerPool;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager"/> class.
        /// </summary>
        /// <param name="logger">The logger used for connection manager operations.</param>
        /// <param name="maxConnectionsPerPool">The maximum number of connections allowed per tenant pool.</param>
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
            _logger.LogInformation("Getting connection for tenant {TenantId}", tenantId);
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            var pool = _pools.GetOrAdd(tenantId,
                _ => new ConnectionPool(connectionString, _maxConnectionsPerPool, _logger));

            var connection = await pool.GetConnectionAsync(cancellationToken);
            _logger.LogInformation("Acquired connection for tenant {TenantId}", tenantId);
            return connection;
        }

        /// <summary>
        /// Acquires an encrypted connection for the given tenant.
        /// After the connection is opened the SQLCipher <c>PRAGMA key</c> is applied so
        /// the database is transparently decrypted for the duration of the connection.
        /// </summary>
        /// <param name="tenantId">The unique identifier of the tenant.</param>
        /// <param name="connectionString">SQLite connection string for the tenant database.</param>
        /// <param name="encryptionKey">
        /// The per-tenant SQLCipher passphrase or raw hex key.
        /// Requires the <c>SQLitePCLRaw.bundle_sqlcipher</c> package.
        /// </param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>An open, decrypted SQLite connection bound to the tenant database.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the connection manager has been disposed.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> or <paramref name="connectionString"/> is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="encryptionKey"/> is empty or whitespace.</exception>
        public async Task<SQLiteConnection> GetEncryptedConnectionAsync(
            string tenantId,
            string connectionString,
            string encryptionKey,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            if (string.IsNullOrWhiteSpace(encryptionKey))
                throw new ArgumentException("Encryption key cannot be empty.", nameof(encryptionKey));

            var pool = _pools.GetOrAdd(tenantId,
                _ => new ConnectionPool(connectionString, _maxConnectionsPerPool, _logger));

            var connection = await pool.GetConnectionAsync(cancellationToken);
            await SqliteMultiTenant.Security.SqlCipherConnectionBuilder.ApplyEncryptionKeyAsync(
                connection, encryptionKey, cancellationToken);
            return connection;
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

        /// <summary>
        /// Gets current pool statistics for monitoring.
        /// </summary>
        /// <returns>A dictionary mapping tenant IDs to their pool statistics.</returns>
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

        /// <summary>
        /// Releases all resources used by the <see cref="ConnectionManager"/>.
        /// </summary>
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

        /// <summary>
        /// Returns a string representation of the connection manager showing pooled connection statistics.
        /// </summary>
        /// <returns>A formatted string with tenant count and connection pool totals.</returns>
        public override string ToString()
        {
            int tenantCount = _pools.Count;
            int totalAvailable = 0;
            int totalTotal = 0;
            int totalWaiting = 0;

            foreach (var pool in _pools.Values)
            {
                totalAvailable += pool.AvailableCount;
                totalTotal += pool.TotalCount;
                totalWaiting += pool.WaitingCount;
            }

            return $"ConnectionManager {{ TenantCount = {tenantCount}, AvailableConnections = {totalAvailable}, TotalConnections = {totalTotal}, WaitingRequests = {totalWaiting} }}";
        }

        private class ConnectionPool : IAsyncDisposable
        {
            private readonly string _connectionString;
            private readonly int _maxConnections;
            private readonly ILogger<ConnectionManager> _logger;
            private readonly SemaphoreSlim _semaphore;
            private readonly ConcurrentBag<SQLiteConnection> _availableConnections;
            private int _totalConnections;

            /// <summary>
            /// Gets the number of connections currently available for reuse in the pool.
            /// </summary>
            public int AvailableCount => _availableConnections.Count;

            /// <summary>
            /// Gets the total number of connections currently tracked by the pool.
            /// </summary>
            public int TotalCount => _totalConnections;

            /// <summary>
            /// Gets a value indicating whether the pool is at capacity and new requests may be waiting.
            /// </summary>
            public int WaitingCount => _semaphore.CurrentCount == 0 ? 1 : 0;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConnectionPool"/> class.
            /// </summary>
            /// <param name="connectionString">The connection string for the SQLite database.</param>
            /// <param name="maxConnections">The maximum number of connections allowed in the pool.</param>
            /// <param name="logger">The logger used for pool operations.</param>
            public ConnectionPool(string connectionString, int maxConnections, ILogger<ConnectionManager> logger)
            {
                _connectionString = connectionString;
                _maxConnections = maxConnections;
                _logger = logger;
                _semaphore = new SemaphoreSlim(maxConnections, maxConnections);
                _availableConnections = new ConcurrentBag<SQLiteConnection>();
                _totalConnections = 0;
            }

            /// <summary>
            /// Retrieves an open connection from the pool or creates a new one if the pool is exhausted.
            /// Blocks until a connection is available or the cancellation token is triggered.
            /// </summary>
            /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
            /// <returns>An open <see cref="SQLiteConnection"/>.</returns>
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

            /// <summary>
            /// Returns a connection to the pool for reuse. Disposes the connection if it is not in an open state.
            /// </summary>
            /// <param name="connection">The connection to release.</param>
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

            /// <summary>
            /// Disposes all available connections in the pool and releases the semaphore.
            /// </summary>
            public async ValueTask DisposeAsync()
            {
                while (_availableConnections.TryTake(out var connection))
                {
                    connection?.Dispose();
                }

                _semaphore?.Dispose();
            }

            /// <summary>
            /// Synchronously disposes the pool by awaiting <see cref="DisposeAsync"/>.
            /// </summary>
            public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
        }
    }

    public sealed class PoolStatistics {
        /// <summary>
        /// The unique identifier of the tenant.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// The number of available connections in the pool for the tenant.
        /// </summary>
        public int AvailableConnections { get; set; }

        /// <summary>
        /// The total number of connections created in the pool for the tenant.
        /// </summary>
        public int TotalConnections { get; set; }

        /// <summary>
        /// Returns 1 if there are no available connections in the pool (indicating that requests may be waiting), otherwise 0.
        /// </summary>
        public int WaitingRequests { get; set; }
    }
}
