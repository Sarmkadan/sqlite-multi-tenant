namespace SqliteMultiTenant.Models;

/// <summary>
/// Provides extension methods for <see cref="TenantDatabase"/>.
/// </summary>
public static class TenantDatabaseExtensions
{
    /// <summary>
    /// Determines whether the tenant database requires attention.
    /// A database requires attention if it has no backups or if its last backup is older than 7 days.
    /// </summary>
    /// <param name="database">The tenant database.</param>
    /// <returns><c>true</c> if the database requires attention; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="database"/> is <c>null</c>.</exception>
    public static bool RequiresAttention(this TenantDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        if (database.LastBackupAt == null) return true;

        var sevenDaysAgo = DateTime.UtcNow - TimeSpan.FromDays(7);
        return database.LastBackupAt.Value < sevenDaysAgo;
    }

    /// <summary>
    /// Gets the database size in a human-readable format.
    /// </summary>
    /// <param name="database">The tenant database.</param>
    /// <returns>The database size as a string (e.g., "1 KB", "2 MB", "3 GB").</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="database"/> is <c>null</c>.</exception>
    public static string GetSizeString(this TenantDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        var sizeBytes = database.SizeBytes;
        if (sizeBytes < 1024) return $"{sizeBytes} B";
        if (sizeBytes < 1024 * 1024) return $"{sizeBytes / 1024.0:F2} KB";
        if (sizeBytes < 1024 * 1024 * 1024) return $"{sizeBytes / (1024.0 * 1024):F2} MB";
        return $"{sizeBytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
