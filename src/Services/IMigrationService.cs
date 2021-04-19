// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service interface for database migration management
/// </summary>
public interface IMigrationService
{
    Task<Migration?> GetMigrationAsync(string migrationId, CancellationToken cancellationToken = default);
    Task<List<Migration>> GetDatabaseMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<List<Migration>> GetPendingMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<List<Migration>> GetAppliedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<Migration> CreateMigrationAsync(string databaseId, string version, string name, string upScript, string? downScript = null, CancellationToken cancellationToken = default);
    Task ExecuteMigrationAsync(string migrationId, string executedBy, CancellationToken cancellationToken = default);
    Task RollbackMigrationAsync(string migrationId, string executedBy, CancellationToken cancellationToken = default);
    Task MarkMigrationAsCompletedAsync(string migrationId, long executionTimeMs, CancellationToken cancellationToken = default);
    Task MarkMigrationAsFailedAsync(string migrationId, string errorMessage, CancellationToken cancellationToken = default);
    Task<int> GetMigrationCountAsync(string databaseId, CancellationToken cancellationToken = default);
    Task<bool> IsMigrationAppliedAsync(string databaseId, string version, CancellationToken cancellationToken = default);
    Task<List<Migration>> GetFailedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);
}
