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
}
