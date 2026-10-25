#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Extension methods for the <see cref="BackupProgress"/> record providing completion checks,
/// time estimates and display formatting for streaming backup operations.
/// </summary>
public static class BackupProgressExtensions
{
    /// <summary>
    /// Maximum percentage value returned by completion calculations
    /// </summary>
    private const double MaxPercent = 100.0;

    /// <summary>
    /// Determines whether the backup has copied every page of the source database
    /// </summary>
    /// <param name="progress">The progress snapshot</param>
    /// <exception cref="ArgumentNullException"><paramref name="progress"/> is <see langword="null"/></exception>
    /// <returns>True if the total page count is known and all pages have been copied, otherwise false</returns>
    public static bool IsComplete(this BackupProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        return progress.TotalPages > 0 && progress.PagesCopied >= progress.TotalPages;
    }

    /// <summary>
    /// Calculates the completion percentage of the backup, clamped to the range [0, 100]
    /// </summary>
    /// <remarks>
    /// Returns 0 when <see cref="BackupProgress.TotalPages"/> is zero or negative, so it is safe for
    /// empty or not-yet-measured databases. Note that <see cref="BackupProgress.PercentComplete"/> is an
    /// instance property with the same name, so call this method as
    /// <c>BackupProgressExtensions.PercentComplete(progress)</c>.
    /// </remarks>
    /// <param name="progress">The progress snapshot</param>
    /// <exception cref="ArgumentNullException"><paramref name="progress"/> is <see langword="null"/></exception>
    /// <returns>Completion percentage in the range [0, 100], rounded to one decimal place</returns>
    public static double PercentComplete(this BackupProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        if (progress.TotalPages <= 0)
        {
            return 0;
        }

        double percent = (double)progress.PagesCopied / progress.TotalPages * MaxPercent;

        return Math.Round(Math.Clamp(percent, 0, MaxPercent), 1);
    }

    /// <summary>
    /// Estimates the time left until the backup completes, based on the average copy rate observed so far
    /// </summary>
    /// <remarks>
    /// Returns <see cref="TimeSpan.Zero"/> when no pages remain, and <see cref="TimeSpan.MaxValue"/> when
    /// no pages have been copied yet so no rate can be measured.
    /// </remarks>
    /// <param name="progress">The progress snapshot</param>
    /// <param name="elapsed">Time elapsed since the backup started</param>
    /// <exception cref="ArgumentNullException"><paramref name="progress"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="elapsed"/> is negative</exception>
    /// <returns>The estimated remaining duration</returns>
    public static TimeSpan EstimatedRemaining(this BackupProgress progress, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(progress);

        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), elapsed, "Elapsed time cannot be negative.");
        }

        int remainingPages = Math.Max(0, progress.PagesRemaining);

        if (remainingPages == 0)
        {
            return TimeSpan.Zero;
        }

        if (progress.PagesCopied <= 0)
        {
            return TimeSpan.MaxValue;
        }

        double remainingTicks = (double)elapsed.Ticks * remainingPages / progress.PagesCopied;

        return remainingTicks >= long.MaxValue
            ? TimeSpan.MaxValue
            : TimeSpan.FromTicks((long)remainingTicks);
    }

    /// <summary>
    /// Formats the backup progress as a short human-readable string
    /// </summary>
    /// <example>
    /// <c>"1234/5000 pages (24.7%), 3766 remaining"</c>
    /// </example>
    /// <param name="progress">The progress snapshot</param>
    /// <exception cref="ArgumentNullException"><paramref name="progress"/> is <see langword="null"/></exception>
    /// <returns>Formatted progress string using invariant culture number formatting</returns>
    public static string ToDisplayString(this BackupProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        string percent = PercentComplete(progress).ToString("F1", CultureInfo.InvariantCulture);

        return FormattableString.Invariant(
            $"{progress.PagesCopied}/{progress.TotalPages} pages ({percent}%), {progress.PagesRemaining} remaining");
    }
}
