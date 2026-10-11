#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Structured health probe result for a single tenant database.
/// Produced by <see cref="SqliteMultiTenant.Services.HealthChecker"/>.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record TenantHealthReport
{
    /// <summary>Tenant identifier.</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>Tenant display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>True when the file exists, <c>PRAGMA integrity_check</c> returned <c>ok</c>, and no error occurred.</summary>
    public bool IsHealthy { get; init; }

    /// <summary>Whether the tenant database file exists on disk.</summary>
    public bool FileExists { get; init; }

    /// <summary>Size of the tenant database file in bytes; 0 when the file is missing.</summary>
    public long FileSizeBytes { get; init; }

    /// <summary>First row returned by <c>PRAGMA integrity_check</c>, or null when the probe did not reach that point.</summary>
    public string? IntegrityCheckResult { get; init; }

    /// <summary>Schema version read from <c>PRAGMA user_version</c>, or null when the probe did not reach that point.</summary>
    public long? SchemaVersion { get; init; }

    /// <summary>Total open connections in the tenant's pool after the probe.</summary>
    public int PoolTotal { get; init; }

    /// <summary>Idle connections in the tenant's pool.</summary>
    public int PoolIdle { get; init; }

    /// <summary>Connections currently checked out of the tenant's pool (<see cref="PoolTotal"/> minus <see cref="PoolIdle"/>).</summary>
    public int PoolLeased { get; init; }

    /// <summary>Callers currently waiting for a pool slot.</summary>
    public int PoolWaiting { get; init; }

    /// <summary>Fraction of open connections that are checked out (<see cref="PoolLeased"/> / <see cref="PoolTotal"/>), 0 when no connections are open.</summary>
    public double PoolUtilization => PoolTotal > 0 ? (double)PoolLeased / PoolTotal : 0d;

    /// <summary>Most recent backup time across the tenant's databases, or null when none has been recorded.</summary>
    public DateTime? LastBackupAt { get; init; }

    /// <summary>Error message when the probe failed or the file is missing; null otherwise.</summary>
    public string? Error { get; init; }

    /// <summary>UTC timestamp when the probe ran.</summary>
    public DateTimeOffset CheckedAt { get; init; }

    /// <inheritdoc />
    public override string ToString()
    {
        var status = IsHealthy ? "Healthy" : "Unhealthy";
        return $"TenantHealthReport {{ TenantId = {TenantId}, Status = {status}, FileExists = {FileExists}, FileSizeBytes = {FileSizeBytes}, " +
               $"IntegrityCheckResult = {IntegrityCheckResult ?? "n/a"}, SchemaVersion = {SchemaVersion?.ToString() ?? "n/a"}, " +
               $"Pool = {PoolLeased} leased / {PoolIdle} idle / {PoolTotal} total, LastBackupAt = {LastBackupAt?.ToString("O") ?? "never"}, " +
               $"Error = {Error ?? "none"} }}";
    }
}
