#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Progress snapshot reported during a streaming database backup operation.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BackupProgress
{
    /// <summary>
    /// Returns a diagnostic summary of the backup progress: pages copied, pages remaining, and percent complete.
    /// </summary>
    public override string ToString() =>
        $"BackupProgress {{ PagesCopied = {PagesCopied}, PagesRemaining = {PagesRemaining}, TotalPages = {TotalPages}, PercentComplete = {PercentComplete:F1}% }}";

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
