#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service interface for database backup management
/// </summary>
public interface IBackupService
{
    Task<Backup?> GetBackupAsync(string backupId, CancellationToken cancellationToken = default);
    Task<List<Backup>> GetDatabaseBackupsAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<List<Backup>> GetCompletedBackupsAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<Backup?> GetLatestBackupAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<Backup> CreateBackupAsync(string databaseId, BackupType backupType, string createdBy, string? backupPath = null, CancellationToken cancellationToken = default);
    Task MarkBackupAsCompletedAsync(string backupId, long sizeBytes, long durationMs, CancellationToken cancellationToken = default);
    Task MarkBackupAsFailedAsync(string backupId, string errorMessage, CancellationToken cancellationToken = default);
    Task VerifyBackupAsync(string backupId, string verifiedBy, CancellationToken cancellationToken = default);
    Task SetBackupExpirationAsync(string backupId, DateTime expirationDate, CancellationToken cancellationToken = default);
    Task<List<Backup>> GetExpiredBackupsAsync(CancellationToken cancellationToken = default);
    Task<int> GetBackupCountAsync(string databaseId, CancellationToken cancellationToken = default);
    Task DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default);
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
