#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using SqliteMultiTenant.Exceptions;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Extension methods for the <see cref="MigrationResult"/> record providing helpers for
/// converting results into exceptions, displaying them, and aggregating several results.
/// </summary>
public static class MigrationResultExtensions
{
    /// <summary>
    /// Throws a <see cref="MigrationException"/> when the migration result is not successful.
    /// Does nothing when <see cref="MigrationResult.IsSuccess"/> is <c>true</c>.
    /// </summary>
    /// <param name="result">The migration result to check.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    /// <exception cref="MigrationException">Thrown when the migration result represents a failure.</exception>
    public static void ThrowIfFailed(this MigrationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.IsSuccess)
        {
            throw new MigrationException(result.ToDisplayString());
        }
    }

    /// <summary>
    /// Returns a human-readable, single-line description of the migration result.
    /// Successful results are described by their applied count; failed results also report
    /// how many migrations were applied before the failure and the error message.
    /// </summary>
    /// <param name="result">The migration result to describe.</param>
    /// <returns>A display string such as <c>"Success: 3 migration(s) applied"</c> or
    /// <c>"Failed after 2 migration(s): connection lost"</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    public static string ToDisplayString(this MigrationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return result.ResultSummary;
        }

        var error = string.IsNullOrEmpty(result.Error) ? "Unknown error occurred" : result.Error;
        return $"Failed after {result.AppliedCount} migration(s): {error}";
    }

    /// <summary>
    /// Aggregates a sequence of migration results into a single result.
    /// The combined result is successful only when every input result is successful,
    /// its applied count is the sum of all applied counts, and its error message joins all
    /// non-empty error messages with <c>"; "</c>.
    /// An empty sequence produces a successful result with an applied count of zero.
    /// </summary>
    /// <param name="results">The migration results to combine.</param>
    /// <returns>A single <see cref="MigrationResult"/> summarizing all inputs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> or any of its elements is <c>null</c>.</exception>
    public static MigrationResult Combine(this IEnumerable<MigrationResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var success = true;
        var appliedCount = 0;
        var errors = new StringBuilder();

        foreach (var result in results)
        {
            ArgumentNullException.ThrowIfNull(result, nameof(results));

            success &= result.IsSuccess;
            appliedCount += result.AppliedCount;

            if (string.IsNullOrEmpty(result.Error))
            {
                continue;
            }

            if (errors.Length > 0)
            {
                errors.Append("; ");
            }

            errors.Append(result.Error);
        }

        if (success)
        {
            return MigrationResult.SuccessResult(appliedCount);
        }

        return new MigrationResult
        {
            Success = false,
            AppliedCount = appliedCount,
            Error = errors.Length > 0 ? errors.ToString() : "Unknown error occurred"
        };
    }
}
