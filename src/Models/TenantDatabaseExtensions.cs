namespace SqliteMultiTenant.Models;

/// <summary>
/// Provides extension methods for <see cref="TenantDatabase"/>.
/// </summary>
public static class TenantDatabaseExtensions
{
    private const long BytesPerKilobyte = 1024;
    private const long BytesPerMegabyte = BytesPerKilobyte * 1024;
    private const long BytesPerGigabyte = BytesPerMegabyte * 1024;
    private static readonly TimeSpan BackupThreshold = TimeSpan.FromDays(7);

    /// <summary>
    /// Determines whether the tenant database requires attention.
    /// A database requires attention if it has no backups or if its last backup is older than 7 days.
    /// </summary>
    /// <param name="database">The tenant database.</param>
    /// <returns><see langword="true"/> if the database requires attention; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="database"/> is <c>null</c>.</exception>
    public static bool RequiresAttention(this TenantDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        return database.LastBackupAt is null
            || database.LastBackupAt.Value < DateTime.UtcNow - BackupThreshold;
    }

    /// <summary>
    /// Gets the database size in a human-readable format.
    /// </summary>
    /// <param name="database">The tenant database.</param>
    /// <returns>The database size as a string (e.g., "1 KB", "2 MB", "3 GB").</returns>
    /// <exception cref="ArgumentNullException"><paramref name="database"/> is <c>null</c>.</exception>
    public static string GetSizeString(this TenantDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        return database.SizeBytes switch
        {
            < BytesPerKilobyte => $"{database.SizeBytes} B",
            < BytesPerMegabyte => $"{database.SizeBytes / (double)BytesPerKilobyte:F2} KB",
            < BytesPerGigabyte => $"{database.SizeBytes / (double)BytesPerMegabyte:F2} MB",
            _ => $"{database.SizeBytes / (double)BytesPerGigabyte:F2} GB"
        };
    }
}
