#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Extension methods for the Backup class providing useful operations
/// </summary>
public static class BackupExtensions
{
    /// <summary>
    /// Number of milliseconds in one second
    /// </summary>
    private const long MillisecondsPerSecond = 1000;

    /// <summary>
    /// Number of seconds in one minute
    /// </summary>
    private const long SecondsPerMinute = 60;

    /// <summary>
    /// Number of minutes in one hour
    /// </summary>
    private const long MinutesPerHour = 60;

    /// <summary>
    /// Number of bytes in one kilobyte (binary divisor used for size formatting)
    /// </summary>
    private const double BytesPerKilobyte = 1024;

    /// <summary>
    /// Calculates the actual saved space in bytes by this backup
    /// </summary>
    /// <param name="backup">The backup instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="backup"/> is <see langword="null"/></exception>
    /// <returns>Number of bytes saved, or 0 if original size is unknown or negative</returns>
    public static long GetSavedSpaceBytes(this Backup backup)
    {
        ArgumentNullException.ThrowIfNull(backup);

        if (backup.OriginalSizeBytes <= 0)
        {
            return 0;
        }

        return backup.OriginalSizeBytes - backup.SizeBytes;
    }

    /// <summary>
    /// Determines if this backup is a full backup
    /// </summary>
    /// <param name="backup">The backup instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="backup"/> is <see langword="null"/></exception>
    /// <returns>True if backup type is Full, otherwise false</returns>
    public static bool IsFullBackup(this Backup backup)
    {
        ArgumentNullException.ThrowIfNull(backup);

        return backup.BackupType == BackupType.Full;
    }

    /// <summary>
    /// Determines if this backup is a system backup (full backup with verification)
    /// </summary>
    /// <param name="backup">The backup instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="backup"/> is <see langword="null"/></exception>
    /// <returns>True if backup is a verified full backup, otherwise false</returns>
    public static bool IsSystemBackup(this Backup backup)
    {
        ArgumentNullException.ThrowIfNull(backup);

        return backup.IsFullBackup() && backup.IsVerified && backup.Status == BackupStatus.Verified;
    }

    /// <summary>
    /// Determines whether the backup was created more than the specified age ago
    /// </summary>
    /// <param name="backup">The backup instance</param>
    /// <param name="age">The age threshold to compare against</param>
    /// <exception cref="ArgumentNullException"><paramref name="backup"/> is <see langword="null"/></exception>
    /// <returns>True if the backup creation timestamp is earlier than the age threshold, otherwise false</returns>
    public static bool IsOlderThan(this Backup backup, TimeSpan age)
    {
        ArgumentNullException.ThrowIfNull(backup);

        return backup.CreatedAt < DateTime.UtcNow - age;
    }

    /// <summary>
    /// Gets the human-readable size of the backup
    /// </summary>
    /// <param name="backup">The backup instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="backup"/> is <see langword="null"/></exception>
    /// <returns>Formatted size string (e.g., "2.5 MB", "1.2 GB")</returns>
    public static string GetHumanReadableSize(this Backup backup)
    {
        ArgumentNullException.ThrowIfNull(backup);

        long bytes = backup.SizeBytes;
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        int order = 0;
        double len = bytes;

        while (len >= BytesPerKilobyte && order < sizes.Length - 1)
        {
            order++;
            len /= BytesPerKilobyte;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Gets the human-readable duration of the backup operation
    /// </summary>
    /// <param name="backup">The backup instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="backup"/> is <see langword="null"/></exception>
    /// <returns>Formatted duration string (e.g., "2m 30s", "1h 5m")</returns>
    public static string GetHumanReadableDuration(this Backup backup)
    {
        ArgumentNullException.ThrowIfNull(backup);

        long totalSeconds = backup.DurationMs / MillisecondsPerSecond;

        if (totalSeconds < SecondsPerMinute)
        {
            return $"{totalSeconds}s";
        }

        long minutes = totalSeconds / SecondsPerMinute;
        long seconds = totalSeconds % SecondsPerMinute;

        if (minutes < MinutesPerHour)
        {
            return seconds == 0
                ? $"{minutes}m"
                : $"{minutes}m {seconds}s";
        }

        long hours = minutes / MinutesPerHour;
        minutes %= MinutesPerHour;

        return minutes == 0
            ? $"{hours}h"
            : $"{hours}h {minutes}m";
    }
}
