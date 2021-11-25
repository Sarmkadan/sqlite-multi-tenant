#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Extension methods for DateTime operations specific to backup and retention policies.
/// All methods use UTC internally to prevent timezone-related bugs in distributed systems.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Calculates if a backup has expired based on retention policy.
    /// Compares against UTC now to ensure consistency across time zones.
    /// </summary>
    /// <param name="expiryDate">The expiration date to check.</param>
    /// <returns><see langword="true"/> if the date has passed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="expiryDate"/> is in the future relative to <see cref="DateTime.UtcNow"/>.</exception>
    public static bool IsExpired(this DateTime expiryDate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(expiryDate, DateTime.UtcNow, nameof(expiryDate));
        return expiryDate < DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates backup age in days since creation.
    /// Used for retention policy calculations and audit reports.
    /// </summary>
    /// <param name="createdDate">The creation date of the backup.</param>
    /// <returns>The age in days, rounded down.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="createdDate"/> is in the future.</exception>
    public static int GetAgeDays(this DateTime createdDate)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(createdDate, DateTime.UtcNow, nameof(createdDate));
        var age = DateTime.UtcNow - createdDate;
        return (int)age.TotalDays;
    }

    /// <summary>
    /// Formats DateTime as ISO 8601 string for API responses and logs.
    /// Ensures consistent formatting across different systems.
    /// </summary>
    /// <param name="dateTime">The date and time to format.</param>
    /// <returns>An ISO 8601 formatted string in universal time.</returns>
    public static string ToIso8601String(this DateTime dateTime) =>
        dateTime.ToUniversalTime().ToString("O");

    /// <summary>
    /// Checks if a date falls within a retention window.
    /// Example: IsWithinRetentionWindow(backup.CreatedAt, 30) checks if backup is < 30 days old.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <param name="retentionDays">The retention period in days.</param>
    /// <returns><see langword="true"/> if the date is within the retention window; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="retentionDays"/> is negative.</exception>
    public static bool IsWithinRetentionWindow(this DateTime date, int retentionDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(retentionDays, nameof(retentionDays));
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        return date >= cutoff;
    }

    /// <summary>
    /// Calculates the next occurrence of a scheduled backup time.
    /// Used for backup scheduler to determine when next backup should run.
    /// </summary>
    /// <param name="baseTime">The base time from which to calculate the next occurrence.</param>
    /// <param name="intervalMinutes">The interval in minutes between scheduled runs.</param>
    /// <returns>The next scheduled DateTime that is in the future.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="intervalMinutes"/> is not positive.</exception>
    public static DateTime GetNextScheduledTime(this DateTime baseTime, int intervalMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(intervalMinutes, 0, nameof(intervalMinutes));

        var now = DateTime.UtcNow;
        var nextRun = baseTime;

        while (nextRun < now)
        {
            nextRun = nextRun.AddMinutes(intervalMinutes);
        }

        return nextRun;
    }

    /// <summary>
    /// Converts a duration to human-readable format (e.g., "2h 30m").
    /// Useful for API responses showing backup duration or uptime.
    /// </summary>
    public static string ToHumanReadableDuration(this TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return "< 1s";

        if (duration.TotalMinutes < 1)
            return $"{(int)duration.TotalSeconds}s";

        if (duration.TotalHours < 1)
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";

        var hours = (int)duration.TotalHours;
        var minutes = duration.Minutes;
        return $"{hours}h {minutes}m";
    }

    /// <summary>
    /// Checks if a backup was created on the current day (UTC).
    /// Used for daily backup tracking and monitoring dashboards.
    /// </summary>
    /// <param name="createdDate">The creation date to check.</param>
    /// <returns><see langword="true"/> if the date is today in UTC; otherwise, <see langword="false"/>.</returns>
    public static bool IsCreatedToday(this DateTime createdDate)
    {
        var today = DateTime.UtcNow.Date;
        return createdDate.Date == today;
    }

    /// <summary>
    /// Calculates the start of day in UTC for range queries.
    /// Ensures consistent results across different time zones.
    /// </summary>
    /// <param name="dateTime">The date and time to normalize.</param>
    /// <returns>The start of the day in UTC.</returns>
    public static DateTime StartOfDayUtc(this DateTime dateTime) =>
        dateTime.ToUniversalTime().Date;

    /// <summary>
    /// Calculates the end of day in UTC (23:59:59.999).
    /// Used for inclusive date range queries.
    /// </summary>
    /// <param name="dateTime">The date and time to normalize.</param>
    /// <returns>The end of the day in UTC.</returns>
    public static DateTime EndOfDayUtc(this DateTime dateTime) =>
        dateTime.ToUniversalTime().Date.AddDays(1).AddTicks(-1);

    /// <summary>
    /// Rounds DateTime down to nearest minute (truncates seconds).
    /// Useful for backup scheduling to align times to minute boundaries.
    /// </summary>
    /// <param name="dateTime">The date and time to round down.</param>
    /// <returns>A new DateTime rounded down to the nearest minute.</returns>
    public static DateTime RoundDownToMinute(this DateTime dateTime) =>
        dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerMinute));
}
