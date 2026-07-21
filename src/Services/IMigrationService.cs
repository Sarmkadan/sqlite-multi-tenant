#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service interface for database migration management.
/// </summary>
public interface IMigrationService
{
    /// <summary>
    /// Retrieves a migration by its ID.
    /// </summary>
    /// <param name="migrationId">The unique ID of the migration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The migration if found; otherwise, null.</returns>
    Task<Migration?> GetMigrationAsync(string migrationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all migrations for a specific database.
    /// </summary>
    /// <param name="databaseId">The database ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of migrations.</returns>
    Task<List<Migration>> GetDatabaseMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves pending migrations for a database.
    /// </summary>
    /// <param name="databaseId">The database ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of pending migrations.</returns>
    Task<List<Migration>> GetPendingMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves applied migrations for a database.
    /// </summary>
    /// <param name="databaseId">The database ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of applied migrations.</returns>
    Task<List<Migration>> GetAppliedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new migration definition.
    /// </summary>
    /// <param name="databaseId">The database ID.</param>
    /// <param name="version">The migration version.</param>
    /// <param name="name">The migration name.</param>
    /// <param name="upScript">The SQL script to apply the migration.</param>
    /// <param name="downScript">Optional SQL script to rollback the migration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created migration.</returns>
    Task<Migration> CreateMigrationAsync(string databaseId, string version, string name, string upScript, string? downScript = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a migration.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="executedBy">The user or process executing the migration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Migration result indicating success/failure.</returns>
    Task<MigrationResult> ExecuteMigrationAsync(string migrationId, string executedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a migration.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="executedBy">The user or process rolling back the migration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Migration result indicating success/failure.</returns>
    Task<MigrationResult> RollbackMigrationAsync(string migrationId, string executedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a migration as completed.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="executionTimeMs">The time taken to execute in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Migration result indicating success/failure.</returns>
    Task<MigrationResult> MarkMigrationAsCompletedAsync(string migrationId, long executionTimeMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a migration as failed.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Migration result indicating success/failure.</returns>
    Task<MigrationResult> MarkMigrationAsFailedAsync(string migrationId, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of migrations for a database.
    /// </summary>
    /// <param name="databaseId">The database ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The migration count.</returns>
    Task<int> GetMigrationCountAsync(string databaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a migration version is applied for a database.
    /// </summary>
    /// <param name="databaseId">The database ID.</param>
    /// <param name="version">The migration version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if applied; otherwise, false.</returns>
    Task<bool> IsMigrationAppliedAsync(string databaseId, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves failed migrations for a database.
    /// </summary>
    /// <param name="databaseId">The database ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of failed migrations.</returns>
    Task<List<Migration>> GetFailedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);
}