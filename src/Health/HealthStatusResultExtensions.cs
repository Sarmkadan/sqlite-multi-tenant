#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;
using SqliteMultiTenant.Api.Responses;

namespace SqliteMultiTenant.Health;

/// <summary>
/// Provides helper methods for interpreting <see cref="HealthStatusResult"/> instances,
/// such as mapping them to HTTP status codes and summarizing failed checks.
/// </summary>
public static class HealthStatusResultExtensions
{
    private const string HealthyStatus = "healthy";
    private const int HealthyHttpStatusCode = 200;
    private const int UnhealthyHttpStatusCode = 503;

    /// <summary>
    /// Determines whether the health check result reports a healthy state.
    /// The result is healthy only when its <see cref="HealthStatusResult.IsHealthy"/> flag is set
    /// and its <see cref="HealthStatusResult.Status"/> is "healthy" (case-insensitive).
    /// </summary>
    /// <remarks>
    /// Named differently from the <c>IsHealthy</c> property on purpose: a method with the same name
    /// as a property cannot be invoked through instance syntax.
    /// </remarks>
    /// <param name="result">The health check result to evaluate.</param>
    /// <returns><c>true</c> if the result is healthy; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    public static bool HasHealthyStatus(this HealthStatusResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsHealthy &&
               string.Equals(result.Status, HealthyStatus, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Maps the health check result to an HTTP status code suitable for probe endpoints.
    /// Returns 200 when the result is healthy and 503 (Service Unavailable) otherwise.
    /// </summary>
    /// <param name="result">The health check result to map.</param>
    /// <returns>200 for a healthy result; 503 for any other result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    public static int ToHttpStatusCode(this HealthStatusResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.HasHealthyStatus() ? HealthyHttpStatusCode : UnhealthyHttpStatusCode;
    }

    /// <summary>
    /// Returns the names of the individual checks that did not report "healthy".
    /// Check names are returned in ordinal sort order so the output is stable across calls.
    /// </summary>
    /// <param name="result">The health check result to inspect.</param>
    /// <returns>
    /// A read-only list of failed check names. The list is empty when every check is healthy
    /// or when the result contains no checks.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    public static IReadOnlyList<string> FailedChecks(this HealthStatusResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Checks
            .Where(pair => !IsCheckHealthy(pair.Value))
            .Select(pair => pair.Key)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Produces a single-line, human-readable summary of the health check result,
    /// suitable for logs and console output.
    /// </summary>
    /// <remarks>
    /// Example output: <c>"healthy (2/2 checks passed, 15 ms, checked at 2026-10-25T10:00:00.0000000Z)"</c>.
    /// When checks fail, the failed check names are appended as <c>"failed: disk, memory"</c>.
    /// </remarks>
    /// <param name="result">The health check result to describe.</param>
    /// <returns>A one-line summary of the overall status, check counts, duration and timestamp.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    public static string ToDisplayString(this HealthStatusResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var totalChecks = result.Checks.Count;
        var failed = result.FailedChecks();
        var passedChecks = totalChecks - failed.Count;

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"{result.Status} ({passedChecks}/{totalChecks} checks passed, {result.DurationMs} ms, checked at {result.CheckedAt:O})");

        return failed.Count == 0
            ? summary
            : $"{summary}, failed: {string.Join(", ", failed)}";
    }

    /// <summary>
    /// Determines whether a single component health entry reports "healthy".
    /// A missing entry is treated as not healthy.
    /// </summary>
    /// <param name="component">The component health entry to evaluate.</param>
    /// <returns><c>true</c> if the entry exists and its status is "healthy" (case-insensitive); otherwise <c>false</c>.</returns>
    private static bool IsCheckHealthy(ComponentHealth? component)
    {
        return component is not null &&
               string.Equals(component.Status, HealthyStatus, StringComparison.OrdinalIgnoreCase);
    }
}
