#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Represents a single record in the tenant database size report.
/// Contains comprehensive storage information for a tenant database.
/// </summary>
public sealed record TenantSizeReportRecord : IComparable<TenantSizeReportRecord>
{
    /// <summary>Tenant identifier.</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>Tenant name.</summary>
    public string TenantName { get; init; } = string.Empty;

    /// <summary>Tenant database file path.</summary>
    public string DatabasePath { get; init; } = string.Empty;

    /// <summary>Total database size in bytes (page_count × page_size).</summary>
    public long SizeBytes { get; init; }

    /// <summary>Human-readable size string (e.g., "10.50 MB").</summary>
    public string SizeHuman => FormatFileSize(SizeBytes);

    /// <summary>Number of allocated pages reported by <c>PRAGMA page_count</c>.</summary>
    public long PageCount { get; init; }

    /// <summary>Page size in bytes reported by <c>PRAGMA page_size</c>.</summary>
    public int PageSize { get; init; }

    /// <summary>Number of free pages (freelist_count).</summary>
    public long FreeListCount { get; init; }

    /// <summary>Size of free space in bytes.</summary>
    public long FreeListSizeBytes => FreeListCount * PageSize;

    /// <summary>Human-readable free space string (e.g., "2.25 MB").</summary>
    public string FreeListSizeHuman => FormatFileSize(FreeListSizeBytes);

    /// <summary>
    /// Size of the WAL file in bytes, or 0 when WAL mode is not active or the
    /// WAL file does not exist.
    /// </summary>
    public long WalSizeBytes { get; init; }

    /// <summary>Human-readable WAL size string (e.g., "1.50 MB").</summary>
    public string WalSizeHuman => FormatFileSize(WalSizeBytes);

    /// <summary>Combined size of the database and its WAL file in bytes.</summary>
    public long TotalSizeBytes => SizeBytes + WalSizeBytes;

    /// <summary>Human-readable total size string (e.g., "12.00 MB").</summary>
    public string TotalSizeHuman => FormatFileSize(TotalSizeBytes);

    /// <summary>Percentage of space that is free list.</summary>
    public double FreeListPercentage => SizeBytes > 0 ? (double)FreeListSizeBytes / SizeBytes * 100 : 0;

    /// <summary>File size on disk in bytes (from FileInfo.Length).</summary>
    public long FileSizeBytes { get; init; }

    /// <summary>Human-readable file size on disk (e.g., "10.50 MB").</summary>
    public string FileSizeHuman => FormatFileSize(FileSizeBytes);

    /// <summary>
    /// Difference between file size on disk and database size (SizeBytes).
    /// This indicates overhead from SQLite's internal structure.
    /// </summary>
    public long FileOverheadBytes => FileSizeBytes - SizeBytes;

    /// <summary>Human-readable overhead string (e.g., "0.25 MB").</summary>
    public string FileOverheadHuman => FormatFileSize(FileOverheadBytes);

    /// <summary>
    /// Creates a text table representation of this record.
    /// </summary>
    public string ToTextTableRow()
    {
        return $"| {TenantId,-20} | {TenantName,-25} | {SizeHuman,-12} | {FreeListSizeHuman,-12} ({FreeListPercentage,5:F1}%) | {FileSizeHuman,-12} | {TotalSizeHuman,-12} |";
    }

    /// <summary>
    /// Creates a text table header.
    /// </summary>
    public static string GetTextTableHeader()
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Tenant ID            | Tenant Name                | Database Size  | Free Space       | File Size      | Total Size     |");
        sb.AppendLine("|----------------------|---------------------------|----------------|------------------|----------------|----------------|");
        return sb.ToString();
    }

    /// <summary>
    /// Creates a text table footer.
    /// </summary>
    public static string GetTextTableFooter()
    {
        return "|----------------------|---------------------------|----------------|------------------|----------------|----------------|";
    }

    /// <summary>
    /// Creates a summary report of all records.
    /// </summary>
    public static string GetSummaryReport(IReadOnlyList<TenantSizeReportRecord> records)
    {
        if (records == null || records.Count == 0)
        {
            return "No tenant databases found.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Tenant Database Size Report Summary");
        sb.AppendLine("===============================");
        sb.AppendLine($"Total Tenants: {records.Count}");
        sb.AppendLine($"Total Database Size: {FormatFileSize(records.Sum(r => r.SizeBytes))}");
        sb.AppendLine($"Total File Size (on disk): {FormatFileSize(records.Sum(r => r.FileSizeBytes))}");
        sb.AppendLine($"Total Free Space: {FormatFileSize(records.Sum(r => r.FreeListSizeBytes))} ({records.Average(r => r.FreeListPercentage):F1}% avg)");
        sb.AppendLine($"Total WAL Size: {FormatFileSize(records.Sum(r => r.WalSizeBytes))}");
        sb.AppendLine($"Total Overhead: {FormatFileSize(records.Sum(r => r.FileOverheadBytes))}");
        sb.AppendLine();
        return sb.ToString();
    }

    /// <summary>
    /// Sorts records by total size descending (largest first).
    /// </summary>
    public int CompareTo(TenantSizeReportRecord? other)
    {
        if (other == null) return 1;
        return other.TotalSizeBytes.CompareTo(TotalSizeBytes);
    }

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        double size = bytes;

        while (size >= 1024 && counter < suffixes.Length - 1)
        {
            size /= 1024;
            counter++;
        }

        return $"{size:F2} {suffixes[counter]}";
    }
}
