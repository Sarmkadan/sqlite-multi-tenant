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
    /// Calculates the actual saved space in bytes by this backup
    /// </summary>
    /// <param name="backup">The backup instance</param>
    /// <returns>Number of bytes saved, or 0 if original size is unknown</returns>
    public static long GetSavedSpaceBytes(this Backup backup)
    {
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
    /// <returns>True if backup type is Full, otherwise false</returns>
    public static bool IsFullBackup(this Backup backup)
    {
        return backup.BackupType == BackupType.Full;
    }

    /// <summary>
    /// Determines if this backup is a system backup (full backup with verification)
    /// </summary>
    /// <param name="backup">The backup instance</param>
    /// <returns>True if backup is a verified full backup, otherwise false</returns>
    public static bool IsSystemBackup(this Backup backup)
    {
        return backup.IsFullBackup() && backup.IsVerified && backup.Status == BackupStatus.Verified;
    }

    /// <summary>
    /// Gets the human-readable size of the backup
    /// </summary>
    /// <param name="backup">The backup instance</param>
    /// <returns>Formatted size string (e.g., "2.5 MB", "1.2 GB")</returns>
    public static string GetHumanReadableSize(this Backup backup)
    {
        long bytes = backup.SizeBytes;
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        int order = 0;
        double len = bytes;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Gets the human-readable duration of the backup operation
    /// </summary>
    /// <param name="backup">The backup instance</param>
    /// <returns>Formatted duration string (e.g., "2m 30s", "1h 5m")</returns>
    public static string GetHumanReadableDuration(this Backup backup)
    {
        long totalSeconds = backup.DurationMs / 1000;

        if (totalSeconds < 60)
        {
            return $"{totalSeconds}s";
        }

        long minutes = totalSeconds / 60;
        long seconds = totalSeconds % 60;

        if (minutes < 60)
        {
            return seconds == 0
                ? $"{minutes}m"
                : $"{minutes}m {seconds}s";
        }

        long hours = minutes / 60;
        minutes %= 60;

        return minutes == 0
            ? $"{hours}h"
            : $"{hours}h {minutes}m";
    }
}
