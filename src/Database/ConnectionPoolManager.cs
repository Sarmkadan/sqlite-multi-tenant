// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Database;

/// <summary>
/// Manages per-tenant SQLite connection pools, enforcing configurable pool-size limits
/// and periodically pruning connections that have exceeded their idle or maximum lifetime.
/// </summary>
public interface IConnectionPoolManager : IAsyncDisposable
{
    /// <summary>
    /// Acquires a healthy connection for <paramref name="tenantId"/>, creating the tenant pool
    /// on first use. Waits up to <see cref="ConnectionPoolOptions.AcquireTimeout"/> when the
    /// pool is at capacity before raising a <see cref="TimeoutException"/>.
    /// </summary>
    /// <param name="tenantId">Unique identifier of the tenant.</param>
    /// <param name="connectionString">SQLite connection string used to open new connections.</param>
    /// <param name="cancellationToken">Token to cancel the wait for an available slot.</param>
    Task<SQLiteConnection> AcquireAsync(string tenantId, string connectionString,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <paramref name="connection"/> to its tenant pool so it can be reused by the
    /// next caller. If the connection is in a broken state it is disposed instead.
    /// </summary>
    /// <param name="tenantId">Tenant to which the connection belongs.</param>
    /// <param name="connection">The connection being returned.</param>
    Task ReleaseAsync(string tenantId, SQLiteConnection connection);

    /// <summary>
    /// Closes and disposes every connection belonging to <paramref name="tenantId"/>.
    /// Intended for use during tenant deletion or deprovisioning flows.
    /// </summary>
    Task EvictTenantAsync(string tenantId);

    /// <summary>Returns a snapshot of current pool statistics for all active tenant pools.</summary>
    IReadOnlyDictionary<string, PoolStatisticsSnapshot> GetStatistics();
}

/// <summary>
/// Default <see cref="IConnectionPoolManager"/> implementation.
/// A <see cref="PeriodicTimer"/> background loop runs at <see cref="ConnectionPoolOptions.PruneInterval"/>
/// to close idle and long-lived connections, keeping per-tenant pool footprints lean.
/// </summary>
public sealed class ConnectionPoolManager : IConnectionPoolManager
{
    private readonly ConnectionPoolOptions _options;
    private readonly ILogger<ConnectionPoolManager> _logger;
    private readonly ConcurrentDictionary<string, TenantPool> _pools = new();
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly Task _pruneTask;

    /// <summary>
    /// Creates a <see cref="ConnectionPoolManager"/> and immediately starts the background
    /// idle-connection pruning loop.
    /// </summary>
    /// <param name="options">
    /// Pool sizing and timeout settings. <see cref="ConnectionPoolOptions.Validate"/> is called
    /// on construction; invalid settings throw before any pools are created.
    /// </param>
    /// <param name="logger">Logger used for prune events and pool eviction notices.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public ConnectionPoolManager(ConnectionPoolOptions options, ILogger<ConnectionPoolManager> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options.Validate();
        _pruneTask = RunPruneLoopAsync(_shutdownCts.Token);
    }

    /// <inheritdoc/>
    public async Task<SQLiteConnection> AcquireAsync(
        string tenantId,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        var pool = _pools.GetOrAdd(tenantId, _ => new TenantPool(connectionString, _options, _logger));
        return await pool.AcquireAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ReleaseAsync(string tenantId, SQLiteConnection connection)
    {
        if (connection is null) return;

        if (_pools.TryGetValue(tenantId, out var pool))
            await pool.ReleaseAsync(connection).ConfigureAwait(false);
        else
            connection.Dispose();
    }

    /// <inheritdoc/>
    public async Task EvictTenantAsync(string tenantId)
    {
        if (_pools.TryRemove(tenantId, out var pool))
        {
            await pool.DisposeAsync().ConfigureAwait(false);
            _logger.LogInformation("Connection pool evicted for tenant {TenantId}", tenantId);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, PoolStatisticsSnapshot> GetStatistics()
    {
        var result = new Dictionary<string, PoolStatisticsSnapshot>(_pools.Count);
        foreach (var (id, pool) in _pools)
            result[id] = pool.GetSnapshot(id);
        return result;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _shutdownCts.CancelAsync().ConfigureAwait(false);

        try { await _pruneTask.ConfigureAwait(false); }
        catch (OperationCanceledException) { /* expected on shutdown */ }

        foreach (var (_, pool) in _pools)
            await pool.DisposeAsync().ConfigureAwait(false);

        _pools.Clear();
        _shutdownCts.Dispose();
    }

    private async Task RunPruneLoopAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(_options.PruneInterval);
        while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var (_, pool) in _pools)
                pool.PruneIdle(now);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Per-tenant pool
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class TenantPool : IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly ConnectionPoolOptions _opts;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _slots;

        // Metadata keyed by connection identity; ConditionalWeakTable entries are automatically
        // reclaimed by the GC once the connection object is no longer referenced anywhere.
        private readonly ConditionalWeakTable<SQLiteConnection, ConnectionMeta> _meta = new();

        private readonly Queue<SQLiteConnection> _idle = new();
        private readonly object _lock = new();
        private int _total;
        private long _pruned;
        private DateTimeOffset _lastPruneAt;

        public TenantPool(string connectionString, ConnectionPoolOptions opts, ILogger logger)
        {
            _connectionString = connectionString;
            _opts = opts;
            _logger = logger;
            _slots = new SemaphoreSlim(opts.MaxPoolSize, opts.MaxPoolSize);
        }

        /// <summary>
        /// Checks out a healthy connection. Stale or broken idle connections encountered during
        /// the search are disposed eagerly so they do not accumulate in the queue.
        /// </summary>
        public async Task<SQLiteConnection> AcquireAsync(CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_opts.AcquireTimeout);

            try
            {
                await _slots.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"No connection became available within {_opts.AcquireTimeout.TotalSeconds:0}s. " +
                    $"Consider increasing MaxPoolSize (current: {_opts.MaxPoolSize}).");
            }

            var now = DateTimeOffset.UtcNow;
            SQLiteConnection? reused = null;

            lock (_lock)
            {
                while (_idle.TryDequeue(out var candidate))
                {
                    if (IsHealthy(candidate, now))
                    {
                        reused = candidate;
                        break;
                    }

                    // Dispose unhealthy idle connections eagerly; no semaphore slot to return
                    // because idle connections do not hold a slot.
                    candidate.Dispose();
                    Interlocked.Decrement(ref _total);
                }
            }

            if (reused is not null)
                return reused;

            var conn = new SQLiteConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            _meta.Add(conn, new ConnectionMeta(now));
            Interlocked.Increment(ref _total);
            return conn;
        }

        /// <summary>Returns a connection to the idle queue, or disposes it if it is no longer usable.</summary>
        public Task ReleaseAsync(SQLiteConnection connection)
        {
            if (connection.State == System.Data.ConnectionState.Open)
            {
                if (_meta.TryGetValue(connection, out var m))
                    m.LastReturnedAt = DateTimeOffset.UtcNow;

                lock (_lock)
                    _idle.Enqueue(connection);
            }
            else
            {
                connection.Dispose();
                Interlocked.Decrement(ref _total);
            }

            _slots.Release();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Scans the idle queue and disposes connections that have exceeded
        /// <see cref="ConnectionPoolOptions.IdleTimeout"/> or <see cref="ConnectionPoolOptions.MaxConnectionLifetime"/>,
        /// while always retaining at least <see cref="ConnectionPoolOptions.MinPoolSize"/> connections.
        /// </summary>
        public void PruneIdle(DateTimeOffset now)
        {
            int pruned = 0;

            lock (_lock)
            {
                int minKeep = Math.Min(_opts.MinPoolSize, _idle.Count);
                int canPrune = _idle.Count - minKeep;

                if (canPrune <= 0)
                {
                    _lastPruneAt = now;
                    return;
                }

                var snapshot = _idle.ToArray();
                _idle.Clear();

                foreach (var conn in snapshot)
                {
                    bool eligible = canPrune > 0
                        && _meta.TryGetValue(conn, out var m)
                        && ((now - m.CreatedAt) >= _opts.MaxConnectionLifetime
                            || (now - m.LastReturnedAt) >= _opts.IdleTimeout);

                    if (eligible)
                    {
                        conn.Dispose();
                        Interlocked.Decrement(ref _total);
                        pruned++;
                        canPrune--;
                    }
                    else
                    {
                        _idle.Enqueue(conn);
                    }
                }

                _lastPruneAt = now;
            }

            if (pruned > 0)
            {
                Interlocked.Add(ref _pruned, pruned);
                _logger.LogDebug("Pruned {Count} idle connection(s) from tenant pool", pruned);
            }
        }

        /// <summary>Returns a statistics snapshot for this pool under the current lock state.</summary>
        public PoolStatisticsSnapshot GetSnapshot(string tenantId)
        {
            lock (_lock)
            {
                return new PoolStatisticsSnapshot
                {
                    TenantId = tenantId,
                    Available = _idle.Count,
                    Total = _total,
                    Waiting = _slots.CurrentCount == 0 ? 1 : 0,
                    PrunedTotal = Interlocked.Read(ref _pruned),
                    LastPruneAt = _lastPruneAt,
                };
            }
        }

        public ValueTask DisposeAsync()
        {
            lock (_lock)
            {
                while (_idle.TryDequeue(out var conn))
                    conn.Dispose();
            }

            _slots.Dispose();
            return ValueTask.CompletedTask;
        }

        private bool IsHealthy(SQLiteConnection conn, DateTimeOffset now) =>
            conn.State == System.Data.ConnectionState.Open
            && _meta.TryGetValue(conn, out var m)
            && (now - m.CreatedAt) < _opts.MaxConnectionLifetime
            && (now - m.LastReturnedAt) < _opts.IdleTimeout;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Per-connection metadata
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class ConnectionMeta(DateTimeOffset createdAt)
    {
        /// <summary>UTC instant when this connection was first opened.</summary>
        public DateTimeOffset CreatedAt { get; } = createdAt;

        /// <summary>UTC instant when this connection was most recently returned to the idle pool.</summary>
        public DateTimeOffset LastReturnedAt { get; set; } = createdAt;
    }
}
