#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Extension methods for <see cref="TenantMaintenanceResult"/> sequences providing helpers for
/// aggregating reclaimed space, filtering failed operations, computing success rates, and
/// producing a multi-line summary report.
/// </summary>
public static class TenantMaintenanceResultExtensions
{
    /// <summary>
    /// Number of bytes in one kilobyte, used when formatting byte counts in the summary report.
    /// </summary>
    private const double BytesPerKilobyte = 1024;

    /// <summary>
    /// Multiplier used to express a fraction as a percentage.
    /// </summary>
    private const double PercentScale = 100;

    /// <summary>
    /// Sums the space reclaimed by every maintenance result in a sequence.
    /// Each result contributes its <see cref="TenantMaintenanceResult.SizeReductionBytes"/>, so a result
    /// whose file grew contributes a negative value.
    /// </summary>
    /// <param name="results">The maintenance results to aggregate.</param>
    /// <returns>The total number of bytes reclaimed across all results; <c>0</c> for an empty sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> or any of its elements is <c>null</c>.</exception>
    public static long TotalBytesReclaimed(this IEnumerable<TenantMaintenanceResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        long total = 0;

        foreach (var result in results)
        {
            ArgumentNullException.ThrowIfNull(result, nameof(results));

            total += result.SizeReductionBytes;
        }

        return total;
    }

    /// <summary>
    /// Filters a sequence of maintenance results, yielding only the results that did not succeed.
    /// The filter is applied lazily as the sequence is enumerated.
    /// </summary>
    /// <param name="results">The maintenance results to filter.</param>
    /// <returns>An enumerable containing only the failed results, in their original order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> is <c>null</c>.
    /// Thrown during enumeration when an element of <paramref name="results"/> is <c>null</c>.</exception>
    public static IEnumerable<TenantMaintenanceResult> WhereFailed(this IEnumerable<TenantMaintenanceResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.Where(result =>
        {
            ArgumentNullException.ThrowIfNull(result, nameof(results));

            return !result.IsSuccess;
        });
    }

    /// <summary>
    /// Calculates the fraction of maintenance results that succeeded.
    /// </summary>
    /// <param name="results">The maintenance results to evaluate.</param>
    /// <returns>A value between <c>0</c> and <c>1</c> representing the share of successful results;
    /// <c>0</c> for an empty sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> or any of its elements is <c>null</c>.</exception>
    public static double SuccessRate(this IEnumerable<TenantMaintenanceResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var total = 0;
        var succeeded = 0;

        foreach (var result in results)
        {
            ArgumentNullException.ThrowIfNull(result, nameof(results));

            total++;

            if (result.IsSuccess)
            {
                succeeded++;
            }
        }

        return total == 0 ? 0 : (double)succeeded / total;
    }

    /// <summary>
    /// Builds a multi-line, human-readable report summarizing a sequence of maintenance results.
    /// The report starts with totals (operations, successes, failures, success rate, bytes reclaimed)
    /// followed by one line per result taken from <see cref="TenantMaintenanceResult.OperationSummary"/>.
    /// </summary>
    /// <param name="results">The maintenance results to report on.</param>
    /// <returns>A multi-line string terminated by a newline. An empty sequence yields the header with zero totals.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> or any of its elements is <c>null</c>.</exception>
    public static string ToSummaryReport(this IEnumerable<TenantMaintenanceResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var materialized = results.ToList();

        foreach (var result in materialized)
        {
            ArgumentNullException.ThrowIfNull(result, nameof(results));
        }

        var failed = materialized.Count(result => !result.IsSuccess);
        var succeeded = materialized.Count - failed;
        var reclaimed = materialized.TotalBytesReclaimed();
        var rate = materialized.SuccessRate() * PercentScale;

        var builder = new StringBuilder();

        builder.AppendLine("Tenant maintenance report");
        builder.AppendLine($"Operations:      {materialized.Count}");
        builder.AppendLine($"Succeeded:       {succeeded}");
        builder.AppendLine($"Failed:          {failed}");
        builder.AppendLine($"Success rate:    {rate:F2}%");
        builder.AppendLine($"Bytes reclaimed: {FormatBytes(reclaimed)}");

        if (materialized.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Details:");

            foreach (var result in materialized)
            {
                builder.AppendLine($"  - {result.OperationSummary}");
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Formats a signed byte count using binary units (B, KB, MB, GB, TB).
    /// </summary>
    /// <param name="bytes">The byte count to format; may be negative.</param>
    /// <returns>A string such as <c>"1.50 MB"</c> or <c>"-2.00 KB"</c>.</returns>
    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        var counter = 0;
        var size = (double)bytes;

        while (Math.Abs(size) >= BytesPerKilobyte && counter < suffixes.Length - 1)
        {
            size /= BytesPerKilobyte;
            counter++;
        }

        return $"{size:F2} {suffixes[counter]}";
    }
}
