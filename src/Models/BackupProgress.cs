#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Models;

/// <summary>
/// Progress snapshot reported during a streaming database backup operation.
/// </summary>
public sealed record BackupProgress
{
    /// <summary>Number of pages copied so far.</summary>
    public int PagesCopied { get; init; }

    /// <summary>Number of pages still remaining to be copied.</summary>
    public int PagesRemaining { get; init; }

    /// <summary>Total number of pages in the source database.</summary>
    public int TotalPages { get; init; }

    /// <summary>Completion percentage in the range [0, 100].</summary>
    public double PercentComplete =>
        TotalPages > 0 ? Math.Round((double)PagesCopied / TotalPages * 100.0, 1) : 0;
}
