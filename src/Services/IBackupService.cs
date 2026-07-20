#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service interface for database backup management.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Retrieves a backup by its ID.
    /// </summary>
    /// <param name="backupId">The unique ID of the backup.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The backup if found; otherwise, null.</returns>
    Task<Backup?> GetBackupAsync(string backupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all backups for a specific database.
    /// </summary>
    /// <param name="databaseId">The database ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of backups.</returns>
    Task<List<Backup>> GetDatabaseBackupsAsync(string databaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves completed backups for a specific database.
    /// </summary>
    /// <param name="databaseId">The database ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of completed backups.</returns>
    Task<List<Backup>> GetCompletedBackupsAsync(string databaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the latest backup for a specific database.
    /// </summary>
    /// <param name="databaseId">The database ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest backup if found; otherwise, null.</returns>
    Task<Backup?> GetLatestBackupAsync(string databaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new backup record.
    /// </summary>
    /// <param name="databaseId">The database ID.</param>
    /// <param name="backupType">The type of backup.</param>
    /// <param name="createdBy">The user or process creating the backup.</param>
    /// <param name="backupPath">Optional path for the backup file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created backup record.</returns>
    Task<Backup> CreateBackupAsync(string databaseId, BackupType backupType, string createdBy, string? backupPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a backup as completed.
    /// </summary>
    /// <param name="backupId">The backup ID.</param>
    /// <param name="sizeBytes">The size of the backup in bytes.</param>
    /// <param name="durationMs">The duration of the backup in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkBackupAsCompletedAsync(string backupId, long sizeBytes, long durationMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a backup as failed.
    /// </summary>
    /// <param name="backupId">The backup ID.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkBackupAsFailedAsync(string backupId, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the integrity of a backup by opening the backup file and running PRAGMA integrity_check.
    /// </summary>
    /// <param name="backupId">The backup ID.</param>
    /// <param name="verifiedBy">The user or process verifying the backup.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification result containing integrity check status and details.</returns>
    Task<BackupVerificationResult> VerifyBackupAsync(string backupId, string verifiedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets an expiration date for a backup.
    /// </summary>
    /// <param name="backupId">The backup ID.</param>
    /// <param name="expirationDate">The expiration date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetBackupExpirationAsync(string backupId, DateTime expirationDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all expired backups.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of expired backups.</returns>
    Task<List<Backup>> GetExpiredBackupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of backups for a database.
    /// </summary>
    /// <param name="databaseId">The database ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The backup count.</returns>
    Task<int> GetBackupCountAsync(string databaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a backup.
    /// </summary>
    /// <param name="backupId">The backup ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a tag to a backup.
    /// </summary>
    /// <param name="backupId">The backup ID.</param>
    /// <param name="tag">The tag to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddBackupTagAsync(string backupId, string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a physical SQLite online backup of <paramref name="sourceDatabasePath"/> to
    /// <paramref name="destinationPath"/>, reporting page-level progress via
    /// <paramref name="progress"/> as the copy proceeds.
    /// </summary>
    /// <param name="sourceDatabasePath">Absolute path to the source SQLite database file.</param>
    /// <param name="destinationPath">Absolute path where the backup file should be written.</param>
    /// <param name="progress">
    /// Optional receiver for <see cref="BackupProgress"/> updates. Each callback fires
    /// after a batch of pages has been copied. Pass <c>null</c> to skip reporting.
    /// </param>
    /// <param name="pagesPerStep">
    /// Number of pages to copy per step. Smaller values yield more granular progress
    /// updates; larger values reduce callback overhead. Default: <c>-1</c> (copy all
    /// pages in one step, equivalent to a single-shot backup).
    /// </param>
    /// <param name="cancellationToken">Token used to abort the backup mid-flight.</param>
    Task BackupWithProgressAsync(
        string sourceDatabasePath,
        string destinationPath,
        IProgress<BackupProgress>? progress = null,
        int pagesPerStep = -1,
        CancellationToken cancellationToken = default);
}
