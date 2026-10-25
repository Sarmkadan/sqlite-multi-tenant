#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;
using SqliteMultiTenant.Exceptions;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Extension methods for the <see cref="BackupVerificationResult"/> record providing helpers for
/// turning a failed verification into an exception, describing the result for display, and
/// checking whether a batch of verification results is entirely valid.
/// </summary>
public static class BackupVerificationResultExtensions
{
    /// <summary>
    /// Throws a <see cref="BackupException"/> when the verification did not pass.
    /// The exception message is taken from <see cref="BackupVerificationResult.ErrorMessage"/>, falling back to
    /// <see cref="BackupVerificationResult.IntegrityCheckResult"/> and then to a generic message.
    /// </summary>
    /// <param name="result">The verification result to check.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    /// <exception cref="BackupException">Thrown when <see cref="BackupVerificationResult.IsValid"/> is <c>false</c>.</exception>
    public static void ThrowIfFailed(this BackupVerificationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsValid)
        {
            return;
        }

        var reason = !string.IsNullOrWhiteSpace(result.ErrorMessage)
            ? result.ErrorMessage
            : !string.IsNullOrWhiteSpace(result.IntegrityCheckResult)
                ? result.IntegrityCheckResult
                : "unknown error";

        throw new BackupException($"Backup verification failed: {reason}");
    }

    /// <summary>
    /// Returns a human-readable, single-line description of the verification result.
    /// Valid results show the file size, page count and page size; failed results show the error message when one is present.
    /// </summary>
    /// <param name="result">The verification result to describe.</param>
    /// <returns>A display string such as <c>"VALID - 1.5 MB, 384 pages x 4096 B"</c> or
    /// <c>"INVALID - file is not a database"</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    public static string ToDisplayString(this BackupVerificationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var size = FormatFileSize(result.FileSizeBytes);

        if (result.IsValid)
        {
            return $"VALID - {size}, {result.PageCount} pages x {result.PageSizeBytes} B";
        }

        return string.IsNullOrEmpty(result.ErrorMessage)
            ? $"INVALID - {size}"
            : $"INVALID - {size} - {result.ErrorMessage}";
    }

    /// <summary>
    /// Determines whether every verification result in the sequence passed.
    /// An empty sequence returns <c>true</c>, following the semantics of <see cref="Enumerable.All{TSource}"/>.
    /// </summary>
    /// <param name="results">The verification results to inspect.</param>
    /// <returns><c>true</c> when all results are valid or the sequence is empty; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> is <c>null</c>.</exception>
    public static bool AllVerified(this IEnumerable<BackupVerificationResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.All(result => result.IsValid);
    }

    /// <summary>
    /// Formats a byte count as a human-readable size using binary (1024-based) units.
    /// </summary>
    /// <param name="bytes">The size in bytes.</param>
    /// <returns>A string such as <c>"512 B"</c>, <c>"1.5 KB"</c> or <c>"2.25 GB"</c>.</returns>
    private static string FormatFileSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        var order = 0;
        double value = bytes;

        while (value >= 1024 && order < units.Length - 1)
        {
            order++;
            value /= 1024;
        }

        return $"{value.ToString("0.##", CultureInfo.InvariantCulture)} {units[order]}";
    }
}
