#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Models;

/// <summary>
/// Extension methods for the <see cref="TenantIntegrityCheckResult"/> class providing helpers for
/// detecting problems, displaying results, and filtering or counting failed checks in collections.
/// </summary>
public static class TenantIntegrityCheckResultExtensions
{
    /// <summary>
    /// Determines whether the integrity check reported any problem.
    /// This is the inverse of <see cref="TenantIntegrityCheckResult.IsSuccess"/>.
    /// </summary>
    /// <param name="result">The integrity check result to inspect.</param>
    /// <returns><c>true</c> when the check failed or carries an error message; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    public static bool HasIssues(this TenantIntegrityCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return !result.IsSuccess;
    }

    /// <summary>
    /// Returns a human-readable, single-line description of the integrity check result.
    /// Passing checks are shown as <c>"PASS"</c>; failing checks also include the error message
    /// when one is present.
    /// </summary>
    /// <param name="result">The integrity check result to describe.</param>
    /// <returns>A display string such as <c>"Tenant 'Acme' (acme): PASS"</c> or
    /// <c>"Tenant 'Acme' (acme): FAIL - database disk image is malformed"</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    public static string ToDisplayString(this TenantIntegrityCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var subject = $"Tenant '{result.TenantName}' ({result.TenantId})";

        if (result.IsSuccess)
        {
            return $"{subject}: PASS";
        }

        return string.IsNullOrEmpty(result.Error)
            ? $"{subject}: FAIL"
            : $"{subject}: FAIL - {result.Error}";
    }

    /// <summary>
    /// Filters a sequence of integrity check results, yielding only the results that reported a problem.
    /// The filter is applied lazily as the sequence is enumerated.
    /// </summary>
    /// <param name="results">The integrity check results to filter.</param>
    /// <returns>An enumerable containing only the failed results, in their original order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> is <c>null</c>.
    /// Thrown during enumeration when an element of <paramref name="results"/> is <c>null</c>.</exception>
    public static IEnumerable<TenantIntegrityCheckResult> WhereFailed(this IEnumerable<TenantIntegrityCheckResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.Where(result =>
        {
            ArgumentNullException.ThrowIfNull(result, nameof(results));

            return result.HasIssues();
        });
    }

    /// <summary>
    /// Counts the integrity check results in a sequence that reported a problem.
    /// </summary>
    /// <param name="results">The integrity check results to count.</param>
    /// <returns>The number of results for which <see cref="HasIssues"/> returns <c>true</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> or any of its elements is <c>null</c>.</exception>
    public static int CountFailed(this IEnumerable<TenantIntegrityCheckResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var count = 0;

        foreach (var result in results)
        {
            ArgumentNullException.ThrowIfNull(result, nameof(results));

            if (result.HasIssues())
            {
                count++;
            }
        }

        return count;
    }
}
