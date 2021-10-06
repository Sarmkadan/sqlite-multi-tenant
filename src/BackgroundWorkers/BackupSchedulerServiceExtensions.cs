namespace SqliteMultiTenant.BackgroundWorkers;

/// <summary>
/// Extension methods for <see cref="BackupSchedulerService"/>.
/// </summary>
public static class BackupSchedulerServiceExtensions
{
    /// <summary>
    /// Schedules a backup for the specified database path.
    /// </summary>
    /// <param name="service">The <see cref="BackupSchedulerService"/> instance.</param>
    /// <param name="databasePath">The path to the database.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> or <paramref name="databasePath"/> is null.</exception>
    public static void ScheduleBackup(this BackupSchedulerService service, string databasePath)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(databasePath);

        // Implementation to schedule a backup for the specified database path
        // For example:
        // service.Backup(databasePath);
    }

    /// <summary>
    /// Gets the next scheduled backup time for the specified database path.
    /// </summary>
    /// <param name="service">The <see cref="BackupSchedulerService"/> instance.</param>
    /// <param name="databasePath">The path to the database.</param>
    /// <returns>The next scheduled backup time, or null if no backup is scheduled.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> or <paramref name="databasePath"/> is null.</exception>
    public static DateTime? GetNextScheduledBackupTime(this BackupSchedulerService service, string databasePath)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(databasePath);

        // Implementation to get the next scheduled backup time for the specified database path
        // For example:
        // return service.GetNextBackupTime(databasePath);
        return null;
    }

    /// <summary>
    /// Cancels any scheduled backups for the specified database path.
    /// </summary>
    /// <param name="service">The <see cref="BackupSchedulerService"/> instance.</param>
    /// <param name="databasePath">The path to the database.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> or <paramref name="databasePath"/> is null.</exception>
    public static void CancelScheduledBackup(this BackupSchedulerService service, string databasePath)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(databasePath);

        // Implementation to cancel any scheduled backups for the specified database path
        // For example:
        // service.CancelBackup(databasePath);
    }
}
