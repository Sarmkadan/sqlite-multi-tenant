namespace SqliteMultiTenant.BackgroundWorkers;

/// <summary>
/// Extension methods for <see cref="BackupSchedulerService"/>.
/// Provides API for scheduling, querying, and canceling database backups.
/// </summary>
public static class BackupSchedulerServiceExtensions
{
    /// <summary>
    /// Dictionary to track scheduled backups by database path.
    /// Since BackupSchedulerService is a singleton, this maintains state across the application.
    /// </summary>
    private static readonly Dictionary<string, DateTime> _scheduledBackups = new();

    /// <summary>
    /// Schedules a backup for the specified database path.
    /// The backup will be executed at the next scheduled backup interval.
    /// </summary>
    /// <param name="service">The <see cref="BackupSchedulerService"/> instance.</param>
    /// <param name="databasePath">The path to the database.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> or <paramref name="databasePath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databasePath"/> is empty or whitespace.</exception>
    public static void ScheduleBackup(this BackupSchedulerService service, string databasePath)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(databasePath);

        lock (_scheduledBackups)
        {
            _scheduledBackups[databasePath] = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Gets the next scheduled backup time for the specified database path.
    /// </summary>
    /// <param name="service">The <see cref="BackupSchedulerService"/> instance.</param>
    /// <param name="databasePath">The path to the database.</param>
    /// <returns>The next scheduled backup time, or null if no backup is scheduled.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> or <paramref name="databasePath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databasePath"/> is empty or whitespace.</exception>
    public static DateTime? GetNextScheduledBackupTime(this BackupSchedulerService service, string databasePath)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(databasePath);

        lock (_scheduledBackups)
        {
            return _scheduledBackups.TryGetValue(databasePath, out var scheduledTime) ? scheduledTime : null;
        }
    }

    /// <summary>
    /// Cancels any scheduled backups for the specified database path.
    /// </summary>
    /// <param name="service">The <see cref="BackupSchedulerService"/> instance.</param>
    /// <param name="databasePath">The path to the database.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> or <paramref name="databasePath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databasePath"/> is empty or whitespace.</exception>
    public static void CancelScheduledBackup(this BackupSchedulerService service, string databasePath)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(databasePath);

        lock (_scheduledBackups)
        {
            _scheduledBackups.Remove(databasePath);
        }
    }
}