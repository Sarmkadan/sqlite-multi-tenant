#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Models;
using SqliteMultiTenant.Services;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Extension methods for <see cref="BackupService"/> providing additional utility functionality
/// </summary>
public static class BackupServiceExtensions
{
    /// <summary>
    /// Checks if a backup exists with the specified ID
    /// </summary>
    /// <param name="service">The <see cref="BackupService"/> instance</param>
    /// <param name="backupId">The backup ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if backup exists, false otherwise</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="backupId"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="backupId"/> is empty or whitespace</exception>
    public static async Task<bool> ExistsAsync(this BackupService service, string backupId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(backupId);

        return await service.GetBackupAsync(backupId, cancellationToken) is not null;
    }

    /// <summary>
    /// Gets the latest successful (completed) backup for a database
    /// </summary>
    /// <param name="service">The <see cref="BackupService"/> instance</param>
    /// <param name="databaseId">The database ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The latest completed backup or null if none exists</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="databaseId"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="databaseId"/> is empty or whitespace</exception>
    public static async Task<Backup?> GetLatestCompletedBackupAsync(this BackupService service, string databaseId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(databaseId);

        var allBackups = await service.GetCompletedBackupsAsync(databaseId, cancellationToken);
        return allBackups.OrderByDescending(b => b.CreatedAt).FirstOrDefault();
    }

    /// <summary>
    /// Gets the backup count for completed backups only
    /// </summary>
    /// <param name="service">The <see cref="BackupService"/> instance</param>
    /// <param name="databaseId">The database ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of completed backups</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="databaseId"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="databaseId"/> is empty or whitespace</exception>
    public static async Task<int> GetCompletedBackupCountAsync(this BackupService service, string databaseId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(databaseId);

        var completedBackups = await service.GetCompletedBackupsAsync(databaseId, cancellationToken);
        return completedBackups.Count;
    }

    /// <summary>
    /// Checks if a database has any backups
    /// </summary>
    /// <param name="service">The <see cref="BackupService"/> instance</param>
    /// <param name="databaseId">The database ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if database has backups, false otherwise</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="databaseId"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="databaseId"/> is empty or whitespace</exception>
    public static async Task<bool> HasBackupsAsync(this BackupService service, string databaseId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(databaseId);

        var count = await service.GetBackupCountAsync(databaseId, cancellationToken);
        return count > 0;
    }
}
