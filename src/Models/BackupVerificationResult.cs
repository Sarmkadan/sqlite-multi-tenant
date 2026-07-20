#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Result of a backup file integrity verification
/// </summary>
public sealed record BackupVerificationResult
{
    /// <summary>
    /// Indicates whether the backup file passed integrity check
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// SQLite integrity check result message
    /// </summary>
    public string IntegrityCheckResult { get; init; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Database page count
    /// </summary>
    public int PageCount { get; init; }

    /// <summary>
    /// Database page size in bytes
    /// </summary>
    public int PageSizeBytes { get; init; }

    /// <summary>
    /// Verification timestamp
    /// </summary>
    public DateTime VerifiedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Any error message if verification failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful verification result
    /// </summary>
    public static BackupVerificationResult Success(string integrityResult, long fileSize, int pageCount, int pageSize)
    {
        return new BackupVerificationResult
        {
            IsValid = true,
            IntegrityCheckResult = integrityResult,
            FileSizeBytes = fileSize,
            PageCount = pageCount,
            PageSizeBytes = pageSize
        };
    }

    /// <summary>
    /// Creates a failed verification result
    /// </summary>
    public static BackupVerificationResult Failed(string errorMessage)
    {
        return new BackupVerificationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}