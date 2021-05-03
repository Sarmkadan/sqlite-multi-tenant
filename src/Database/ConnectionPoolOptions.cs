// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace SqliteMultiTenant.Database;

/// <summary>
/// Configuration options governing per-tenant connection pool size and idle-connection behaviour.
/// Pass an instance to <see cref="ConnectionPoolManager"/> directly or via
/// <c>AddConnectionPooling(options => { … })</c> at startup.
/// </summary>
public sealed class ConnectionPoolOptions
{
    /// <summary>
    /// Minimum number of connections to keep alive per tenant pool even after
    /// <see cref="IdleTimeout"/> has elapsed. Default: <c>1</c>.
    /// </summary>
    public int MinPoolSize { get; set; } = 1;

    /// <summary>
    /// Maximum number of concurrent connections allowed per tenant pool.
    /// Callers block for up to <see cref="AcquireTimeout"/> when this ceiling is reached.
    /// Default: <c>10</c>.
    /// </summary>
    public int MaxPoolSize { get; set; } = 10;

    /// <summary>
    /// How long an idle connection may sit in the pool before becoming eligible for pruning.
    /// Connections kept by <see cref="MinPoolSize"/> are exempt. Default: <c>5 minutes</c>.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum time to wait for a pool slot when the pool is at capacity.
    /// A <see cref="TimeoutException"/> is raised on expiry. Default: <c>30 seconds</c>.
    /// </summary>
    public TimeSpan AcquireTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Absolute maximum lifetime of a single connection regardless of activity.
    /// Stale connections are disposed and replaced transparently on the next acquire.
    /// Default: <c>1 hour</c>.
    /// </summary>
    public TimeSpan MaxConnectionLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// How often the background pruning loop wakes to close idle and expired connections.
    /// Default: <c>60 seconds</c>.
    /// </summary>
    public TimeSpan PruneInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Validates all option values and throws when any value is outside an acceptable range.
    /// Called automatically by <see cref="ConnectionPoolManager"/>'s constructor.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a numeric option is out of range or a <see cref="TimeSpan"/> is non-positive.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="MinPoolSize"/> exceeds <see cref="MaxPoolSize"/>.
    /// </exception>
    public void Validate()
    {
        if (MinPoolSize < 0)
            throw new ArgumentOutOfRangeException(nameof(MinPoolSize), "MinPoolSize must be non-negative.");
        if (MaxPoolSize < 1)
            throw new ArgumentOutOfRangeException(nameof(MaxPoolSize), "MaxPoolSize must be at least 1.");
        if (MinPoolSize > MaxPoolSize)
            throw new ArgumentException($"{nameof(MinPoolSize)} cannot exceed {nameof(MaxPoolSize)}.");
        if (IdleTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(IdleTimeout), "IdleTimeout must be positive.");
        if (AcquireTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(AcquireTimeout), "AcquireTimeout must be positive.");
        if (MaxConnectionLifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(MaxConnectionLifetime), "MaxConnectionLifetime must be positive.");
        if (PruneInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(PruneInterval), "PruneInterval must be positive.");
    }
}

/// <summary>
/// Point-in-time statistics snapshot for a single tenant's connection pool,
/// intended for monitoring dashboards and diagnostic endpoints.
/// </summary>
public sealed class PoolStatisticsSnapshot
{
    /// <summary>Tenant identifier this snapshot belongs to.</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>Connections currently in the idle pool, available for immediate checkout.</summary>
    public int Available { get; init; }

    /// <summary>
    /// Total open connections for this tenant (checked-out connections plus idle ones).
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// Indicates whether any callers are currently waiting for a slot. Non-zero means the pool
    /// is fully saturated and back-pressure is being applied.
    /// </summary>
    public int Waiting { get; init; }

    /// <summary>Cumulative number of connections pruned since this pool was first created.</summary>
    public long PrunedTotal { get; init; }

    /// <summary>UTC timestamp of the most recently completed idle-prune pass.</summary>
    public DateTimeOffset LastPruneAt { get; init; }
}
