// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Repositories;

/// <summary>
/// Repository interface for backup CRUD and query operations
/// </summary>
public interface IBackupRepository
{
    Task<Backup?> GetByIdAsync(string backupId, CancellationToken cancellationToken = default);
    Task<List<Backup>> GetByDatabaseAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<List<Backup>> GetCompletedBackupsAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<List<Backup>> GetVerifiedBackupsAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<List<Backup>> GetFailedBackupsAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<Backup?> GetLatestBackupAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<Backup> AddAsync(Backup backup, CancellationToken cancellationToken = default);
    Task UpdateAsync(Backup backup, CancellationToken cancellationToken = default);
    Task DeleteAsync(string backupId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string backupId, CancellationToken cancellationToken = default);
    Task<int> GetCountByDatabaseAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<List<Backup>> GetExpiredBackupsAsync(CancellationToken cancellationToken = default);
    Task<List<Backup>> GetPagedAsync(string databaseId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
