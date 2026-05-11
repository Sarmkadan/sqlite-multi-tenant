#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Models;

/// <summary>
/// Storage usage statistics for a single tenant database.
/// </summary>
public sealed record TenantStorageInfo
{
    /// <summary>Tenant identifier.</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>Total database size in bytes (page_count × page_size).</summary>
    public long SizeBytes { get; init; }

    /// <summary>Number of allocated pages reported by <c>PRAGMA page_count</c>.</summary>
    public long PageCount { get; init; }

    /// <summary>Page size in bytes reported by <c>PRAGMA page_size</c>.</summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Size of the WAL file in bytes, or 0 when WAL mode is not active or the
    /// WAL file does not exist.
    /// </summary>
    public long WalSizeBytes { get; init; }

    /// <summary>Combined size of the database and its WAL file in bytes.</summary>
    public long TotalSizeBytes => SizeBytes + WalSizeBytes;
}
