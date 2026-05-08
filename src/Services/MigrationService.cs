#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service implementation for database migration management
/// </summary>
public sealed class MigrationService : IMigrationService {
    private readonly IMigrationRepository _repository;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(IMigrationRepository repository, ILogger<MigrationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Migration?> GetMigrationAsync(string migrationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(migrationId))
            throw new ArgumentException("Migration ID cannot be empty", nameof(migrationId));

        try
        {
            return await _repository.GetByIdAsync(migrationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving migration {MigrationId}: {Message}", migrationId, ex.Message);
            throw;
        }
    }

    public async Task<List<Migration>> GetDatabaseMigrationsAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be empty", nameof(databaseId));

        try
        {
            return await _repository.GetOrderedMigrationsAsync(databaseId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving migrations for database {DatabaseId}: {Message}", databaseId, ex.Message);
            throw;
        }
    }

    public async Task<List<Migration>> GetPendingMigrationsAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be empty", nameof(databaseId));

        try
        {
            return await _repository.GetPendingMigrationsAsync(databaseId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving pending migrations for database {DatabaseId}: {Message}", databaseId, ex.Message);
            throw;
        }
    }

    public async Task<List<Migration>> GetAppliedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be empty", nameof(databaseId));

        try
        {
            return await _repository.GetAppliedMigrationsAsync(databaseId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving applied migrations for database {DatabaseId}: {Message}", databaseId, ex.Message);
            throw;
        }
    }

    public async Task<Migration> CreateMigrationAsync(string databaseId, string version, string name, string upScript, string? downScript = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be empty", nameof(databaseId));

        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Migration version cannot be empty", nameof(version));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Migration name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(upScript))
            throw new ArgumentException("UpScript cannot be empty", nameof(upScript));

        try
        {
            var existingMigration = await _repository.GetByVersionAsync(databaseId, version, cancellationToken);
            if (existingMigration is not null)
                throw MigrationException.AlreadyApplied(version);

            var migrations = await _repository.GetByDatabaseAsync(databaseId, cancellationToken);
            int executionOrder = migrations.Count + 1;

            var migration = new Migration
            {
                MigrationId = Guid.NewGuid().ToString(),
                DatabaseId = databaseId,
                Version = version,
                Name = name,
                UpScript = upScript,
                DownScript = downScript,
                Status = MigrationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExecutionOrder = executionOrder,
                IsRollbackable = !string.IsNullOrEmpty(downScript)
            };

            if (!migration.Validate(out var errors))
                throw new ArgumentException($"Migration validation failed: {string.Join(", ", errors)}");

            var createdMigration = await _repository.AddAsync(migration, cancellationToken);
            _logger.LogInformation($"Migration created: {migration.GetDisplayName()}");
            return createdMigration;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating migration: {Message}", ex.Message);
            throw;
        }
    }

    public async Task ExecuteMigrationAsync(string migrationId, string executedBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(migrationId))
            throw new ArgumentException("Migration ID cannot be empty", nameof(migrationId));

        if (string.IsNullOrWhiteSpace(executedBy))
            throw new ArgumentException("ExecutedBy cannot be empty", nameof(executedBy));

        try
        {
            var migration = await _repository.GetByIdAsync(migrationId, cancellationToken);
            if (migration is null)
                throw new MigrationException.NotFound(migrationId);

            migration.MarkAsStarted(executedBy);
            await _repository.UpdateAsync(migration, cancellationToken);
            _logger.LogInformation($"Migration execution started: {migration.GetDisplayName()}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error executing migration {MigrationId}: {Message}", migrationId, ex.Message);
            throw;
        }
    }

    public async Task RollbackMigrationAsync(string migrationId, string executedBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(migrationId))
            throw new ArgumentException("Migration ID cannot be empty", nameof(migrationId));

        try
        {
            var migration = await _repository.GetByIdAsync(migrationId, cancellationToken);
            if (migration is null)
                throw new MigrationException.NotFound(migrationId);

            if (!migration.CanRollback())
                throw new MigrationException($"Migration cannot be rolled back: {migration.GetDisplayName()}");

            migration.MarkAsRolledBack(0);
            await _repository.UpdateAsync(migration, cancellationToken);
            _logger.LogInformation($"Migration rolled back: {migration.GetDisplayName()}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error rolling back migration {MigrationId}: {Message}", migrationId, ex.Message);
            throw;
        }
    }

    public async Task MarkMigrationAsCompletedAsync(string migrationId, long executionTimeMs, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(migrationId))
            throw new ArgumentException("Migration ID cannot be empty", nameof(migrationId));

        try
        {
            var migration = await _repository.GetByIdAsync(migrationId, cancellationToken);
            if (migration is null)
                throw new MigrationException.NotFound(migrationId);

            migration.MarkAsCompleted(executionTimeMs);
            await _repository.UpdateAsync(migration, cancellationToken);
            _logger.LogInformation($"Migration completed: {migration.GetDisplayName()} ({executionTimeMs}ms)");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error marking migration as completed: {Message}", ex.Message);
            throw;
        }
    }

    public async Task MarkMigrationAsFailedAsync(string migrationId, string errorMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(migrationId))
            throw new ArgumentException("Migration ID cannot be empty", nameof(migrationId));

        try
        {
            var migration = await _repository.GetByIdAsync(migrationId, cancellationToken);
            if (migration is null)
                throw new MigrationException.NotFound(migrationId);

            migration.MarkAsFailed(errorMessage);
            await _repository.UpdateAsync(migration, cancellationToken);
            _logger.LogError($"Migration failed: {migration.GetDisplayName()} - {errorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error marking migration as failed: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<int> GetMigrationCountAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be empty", nameof(databaseId));

        try
        {
            return await _repository.GetCountByDatabaseAsync(databaseId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting migration count: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<bool> IsMigrationAppliedAsync(string databaseId, string version, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be empty", nameof(databaseId));

        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be empty", nameof(version));

        try
        {
            var migration = await _repository.GetByVersionAsync(databaseId, version, cancellationToken);
            return migration is not null && migration.Status == MigrationStatus.Completed;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error checking if migration is applied: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<List<Migration>> GetFailedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be empty", nameof(databaseId));

        try
        {
            return await _repository.GetFailedMigrationsAsync(databaseId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving failed migrations: {Message}", ex.Message);
            throw;
        }
    }
}
