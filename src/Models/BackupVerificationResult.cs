#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;
using System.Diagnostics;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Result of a backup file integrity verification
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BackupVerificationResult
{
    /// <summary>
    /// Returns a diagnostic summary of the verification outcome, including the integrity check output and any error.
    /// </summary>
    public override string ToString() =>
        $"BackupVerificationResult {{ IsValid = {IsValid}, IntegrityCheckResult = {IntegrityCheckResult}, FileSizeBytes = {FileSizeBytes}, " +
        $"PageCount = {PageCount}, PageSizeBytes = {PageSizeBytes}, VerifiedAt = {VerifiedAt:O}, Error = {ErrorMessage ?? "none"} }}";

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
    /// <param name="integrityResult">SQLite integrity check result message</param>
    /// <param name="fileSize">File size in bytes</param>
    /// <param name="pageCount">Database page count</param>
    /// <param name="pageSize">Database page size in bytes</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="integrityResult"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="integrityResult"/> is empty or white space.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="fileSize"/>, <paramref name="pageCount"/> or <paramref name="pageSize"/> is negative.</exception>
    public static BackupVerificationResult Success(string integrityResult, long fileSize, int pageCount, int pageSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(integrityResult);
        ArgumentOutOfRangeException.ThrowIfNegative(fileSize);
        ArgumentOutOfRangeException.ThrowIfNegative(pageCount);
        ArgumentOutOfRangeException.ThrowIfNegative(pageSize);

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
    /// <param name="errorMessage">Error message describing why verification failed</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorMessage"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errorMessage"/> is empty or white space.</exception>
    public static BackupVerificationResult Failed(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new BackupVerificationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}
