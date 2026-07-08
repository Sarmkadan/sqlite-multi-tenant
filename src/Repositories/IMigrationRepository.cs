#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Repositories;

/// <summary>
/// Repository interface for migration CRUD and query operations
/// </summary>
public interface IMigrationRepository
{
    Task<List<Migration>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Migration?> GetByIdAsync(string migrationId, CancellationToken cancellationToken = default);
    Task<List<Migration>> GetByDatabaseAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<List<Migration>> GetPendingMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<List<Migration>> GetAppliedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<List<Migration>> GetFailedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<Migration?> GetByVersionAsync(string databaseId, string version, CancellationToken cancellationToken = default);
    Task<Migration> AddAsync(Migration migration, CancellationToken cancellationToken = default);
    Task UpdateAsync(Migration migration, CancellationToken cancellationToken = default);
    Task DeleteAsync(string migrationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string migrationId, CancellationToken cancellationToken = default);
    Task<int> GetCountByDatabaseAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<List<Migration>> GetOrderedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);
}
