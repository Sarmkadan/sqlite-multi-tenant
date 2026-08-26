#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace SqliteMultiTenant.Models;

/// <summary>
/// Represents the result of a tenant database maintenance operation.
/// Contains timing information, file sizes (before/after), and operation details.
/// </summary>
public sealed class TenantMaintenanceResult
{
    /// <summary>
    /// The tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// The tenant name.
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// The maintenance operation performed (VACUUM, ANALYZE, etc.).
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// When the maintenance operation started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the maintenance operation completed (null if not completed).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// File size before maintenance (in bytes).
    /// </summary>
    public long SizeBeforeBytes { get; set; }

    /// <summary>
    /// File size after maintenance (in bytes).
    /// </summary>
    public long SizeAfterBytes { get; set; }

    /// <summary>
    /// File size after VACUUM but before other operations (in bytes).
    /// </summary>
    public long? IntermediateSizeBytes { get; set; }

    /// <summary>
    /// Amount of space reclaimed (SizeBeforeBytes - SizeAfterBytes).
    /// </summary>
    public long SizeReductionBytes => SizeBeforeBytes - SizeAfterBytes;

    /// <summary>
    /// Duration of the maintenance operation in milliseconds.
    /// </summary>
    public long DurationMs
    {
        get
        {
            if (CompletedAt.HasValue)
            {
                return (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
            }
            return 0;
        }
    }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess => string.IsNullOrEmpty(Error) && CompletedAt.HasValue;

    /// <summary>
    /// Gets a human-readable summary of the size change.
    /// </summary>
    public string SizeChangeSummary
    {
        get
        {
            if (SizeBeforeBytes == 0)
                return "N/A";

            var reductionPercent = (double)SizeReductionBytes / SizeBeforeBytes * 100;
            var before = FormatFileSize(SizeBeforeBytes);
            var after = FormatFileSize(SizeAfterBytes);
            var saved = FormatFileSize(SizeReductionBytes);

            if (IsSuccess)
            {
                return $"{before} → {after} (saved: {saved}, {reductionPercent:F2}% reduction)";
            }

            return $"{before} → {after} (operation failed: {Error}) ";
        }
    }

    /// <summary>
    /// Gets a human-readable summary of the operation.
    /// </summary>
    public string OperationSummary => $"{Operation} on {TenantName} ({TenantId}): {SizeChangeSummary}";

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
    public override string ToString() => $"TenantMaintenanceResult {{ TenantId = {TenantId}, TenantName = {TenantName}, Operation = {Operation}, StartedAt = {StartedAt}, CompletedAt = {CompletedAt}, SizeBeforeBytes = {SizeBeforeBytes} }}";
}
