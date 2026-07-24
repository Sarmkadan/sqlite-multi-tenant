#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Thrown when a tenant's storage quota would be exceeded by an operation.
/// </summary>
public sealed class QuotaExceededException : Exception
{
    /// <summary>Gets the tenant identifier.</summary>
    public string? TenantId { get; }

    /// <summary>Gets the quota limit in bytes.</summary>
    public long QuotaBytes { get; }

    /// <summary>Gets the current size in bytes.</summary>
    public long CurrentSizeBytes { get; }

    /// <summary>Gets the size delta that would be added by the operation.</summary>
    public long DeltaBytes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuotaExceededException"/> class.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="quotaBytes">The quota limit in bytes.</param>
    /// <param name="currentSizeBytes">The current database size in bytes.</param>
    /// <param name="deltaBytes">The size delta that would be added by the operation.</param>
    public QuotaExceededException(string tenantId, long quotaBytes, long currentSizeBytes, long deltaBytes)
        : base(FormatMessage(tenantId, quotaBytes, currentSizeBytes, deltaBytes))
    {
        TenantId = tenantId;
        QuotaBytes = quotaBytes;
        CurrentSizeBytes = currentSizeBytes;
        DeltaBytes = deltaBytes;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuotaExceededException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="quotaBytes">The quota limit in bytes.</param>
    /// <param name="currentSizeBytes">The current database size in bytes.</param>
    /// <param name="deltaBytes">The size delta that would be added by the operation.</param>
    public QuotaExceededException(
        string message,
        string tenantId,
        long quotaBytes,
        long currentSizeBytes,
        long deltaBytes)
        : base(message)
    {
        TenantId = tenantId;
        QuotaBytes = quotaBytes;
        CurrentSizeBytes = currentSizeBytes;
        DeltaBytes = deltaBytes;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuotaExceededException"/> class with inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="quotaBytes">The quota limit in bytes.</param>
    /// <param name="currentSizeBytes">The current database size in bytes.</param>
    /// <param name="deltaBytes">The size delta that would be added by the operation.</param>
    /// <param name="innerException">The inner exception.</param>
    public QuotaExceededException(
        string message,
        string tenantId,
        long quotaBytes,
        long currentSizeBytes,
        long deltaBytes,
        Exception? innerException)
        : base(message, innerException)
    {
        TenantId = tenantId;
        QuotaBytes = quotaBytes;
        CurrentSizeBytes = currentSizeBytes;
        DeltaBytes = deltaBytes;
    }

    private static string FormatMessage(string tenantId, long quotaBytes, long currentSizeBytes, long deltaBytes)
    {
        var newSize = currentSizeBytes + deltaBytes;
        var percent = (double)newSize / quotaBytes * 100;
        return $"Tenant '{tenantId}' would exceed storage quota. " +
               $"Quota: {FormatSize(quotaBytes)}, " +
               $"Current: {FormatSize(currentSizeBytes)}, " +
               $"Delta: {FormatSize(deltaBytes)}, " +
               $"Proposed: {FormatSize(newSize)} ({percent:F2}% of quota).";
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}