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
    private readonly Random _random = new();

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

    public async Task<MigrationResult> ExecuteMigrationAsync(string migrationId, string executedBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(migrationId))
            throw new ArgumentException("Migration ID cannot be empty", nameof(migrationId));

        if (string.IsNullOrWhiteSpace(executedBy))
            throw new ArgumentException("ExecutedBy cannot be empty", nameof(executedBy));

        try
        {
            var migration = await _repository.GetByIdAsync(migrationId, cancellationToken);
            if (migration is null)
                return MigrationResult.FailureResult($"Migration with ID '{migrationId}' was not found");

            migration.MarkAsStarted(executedBy);
            await _repository.UpdateAsync(migration, cancellationToken);
            _logger.LogInformation($"Migration execution started: {migration.GetDisplayName()}");
            return MigrationResult.SuccessResult();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error executing migration {MigrationId}: {Message}", migrationId, ex.Message);
            throw;
        }
    }

    public async Task<MigrationResult> RollbackMigrationAsync(string migrationId, string executedBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(migrationId))
            throw new ArgumentException("Migration ID cannot be empty", nameof(migrationId));

        if (string.IsNullOrWhiteSpace(executedBy))
            throw new ArgumentException("ExecutedBy cannot be empty", nameof(executedBy));

        try
        {
            var migration = await _repository.GetByIdAsync(migrationId, cancellationToken);
            if (migration is null)
                return MigrationResult.FailureResult($"Migration with ID '{migrationId}' was not found");

            if (!migration.CanRollback())
                return MigrationResult.FailureResult($"Migration cannot be rolled back: {migration.GetDisplayName()}");

            migration.MarkAsRolledBack(0);
            await _repository.UpdateAsync(migration, cancellationToken);
            _logger.LogInformation($"Migration rolled back: {migration.GetDisplayName()}");
            return MigrationResult.SuccessResult();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error rolling back migration {MigrationId}: {Message}", migrationId, ex.Message);
            throw;
        }
    }

    public async Task<MigrationResult> MarkMigrationAsCompletedAsync(string migrationId, long executionTimeMs, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(migrationId))
            throw new ArgumentException("Migration ID cannot be empty", nameof(migrationId));

        try
        {
            var migration = await _repository.GetByIdAsync(migrationId, cancellationToken);
            if (migration is null)
                return MigrationResult.FailureResult($"Migration with ID '{migrationId}' was not found");

            migration.MarkAsCompleted(executionTimeMs);
            await _repository.UpdateAsync(migration, cancellationToken);
            _logger.LogInformation($"Migration completed: {migration.GetDisplayName()} ({executionTimeMs}ms)");
            return MigrationResult.SuccessResult();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error marking migration as completed: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<MigrationResult> MarkMigrationAsFailedAsync(string migrationId, string errorMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(migrationId))
            throw new ArgumentException("Migration ID cannot be empty", nameof(migrationId));

        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be empty", nameof(errorMessage));

        try
        {
            var migration = await _repository.GetByIdAsync(migrationId, cancellationToken);
            if (migration is null)
                return MigrationResult.FailureResult($"Migration with ID '{migrationId}' was not found");

            migration.MarkAsFailed(errorMessage);
            await _repository.UpdateAsync(migration, cancellationToken);
            _logger.LogError($"Migration failed: {migration.GetDisplayName()} - {errorMessage}");
            return MigrationResult.FailureResult(errorMessage);
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

    public async Task<Models.MigrationBatchResult> ApplyMigrationsWithFaultIsolationAsync(string databaseId, string executedBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be empty", nameof(databaseId));

        if (string.IsNullOrWhiteSpace(executedBy))
            throw new ArgumentException("ExecutedBy cannot be empty", nameof(executedBy));

        try
        {
            var pendingMigrations = await _repository.GetPendingMigrationsAsync(databaseId, cancellationToken);
            if (pendingMigrations.Count == 0)
            {
                _logger.LogInformation("No pending migrations for database {DatabaseId}", databaseId);
                return Models.MigrationBatchResult.SuccessResult(0, 0, new List<Models.TenantMigrationResult> {
                    Models.TenantMigrationResult.SuccessResult(databaseId, null, null, 0, 0, null)
                });
            }

            var tenantResult = await ApplyMigrationsToDatabaseWithFaultIsolationAsync(databaseId, executedBy, pendingMigrations, cancellationToken);
            var batchResult = Models.MigrationBatchResult.SuccessResult(
                totalMigrationsAttempted: tenantResult.TotalMigrationsAttempted,
                successfulMigrations: tenantResult.SuccessfulMigrations,
                tenantResults: new List<Models.TenantMigrationResult> { tenantResult }
            );

            _logger.LogInformation("Batch migration completed for database {DatabaseId}: {Summary}",
                databaseId, batchResult.ResultSummary);

            return batchResult;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error applying migrations with fault isolation to database {DatabaseId}: {Message}",
                databaseId, ex.Message);
            return Models.MigrationBatchResult.FailureResult(ex.Message, new List<Models.TenantMigrationResult>());
        }
    }

    public async Task<Models.MigrationBatchResult> ApplyMigrationsToMultipleDatabasesAsync(
        List<string> databaseIds,
        string executedBy,
        CancellationToken cancellationToken = default)
    {
        if (databaseIds == null || databaseIds.Count == 0)
            throw new ArgumentException("Database IDs list cannot be null or empty", nameof(databaseIds));

        if (string.IsNullOrWhiteSpace(executedBy))
            throw new ArgumentException("ExecutedBy cannot be empty", nameof(executedBy));

        try
        {
            var allTenantResults = new List<Models.TenantMigrationResult>();
            int totalMigrationsAttempted = 0;
            int successfulMigrations = 0;

            foreach (var databaseId in databaseIds)
            {
                try
                {
                    var pendingMigrations = await _repository.GetPendingMigrationsAsync(databaseId, cancellationToken);
                    if (pendingMigrations.Count == 0)
                    {
                        _logger.LogInformation("No pending migrations for database {DatabaseId}", databaseId);
                        allTenantResults.Add(Models.TenantMigrationResult.SuccessResult(databaseId, null, null, 0, 0, null));
                        continue;
                    }

                    var tenantResult = await ApplyMigrationsToDatabaseWithFaultIsolationAsync(databaseId, executedBy, pendingMigrations, cancellationToken);
                    totalMigrationsAttempted += tenantResult.TotalMigrationsAttempted;
                    successfulMigrations += tenantResult.SuccessfulMigrations;
                    allTenantResults.Add(tenantResult);
                }
                catch (Exception tenantEx)
                {
                    _logger.LogError("Error processing database {DatabaseId}: {Message}", databaseId, tenantEx.Message);
                    allTenantResults.Add(Models.TenantMigrationResult.FailureResult(
                        databaseId: databaseId,
                        tenantId: null,
                        databaseName: null,
                        totalMigrationsAttempted: 0,
                        successfulMigrations: 0,
                        schemaVersionReached: null,
                        failures: new List<Models.MigrationFailure> {
                            Models.MigrationFailure.Create(
                                migrationId: "system",
                                version: "system",
                                name: "Database Processing Error",
                                errorMessage: tenantEx.Message,
                                exception: tenantEx
                            )
                        }
                    ));
                }
            }

            var batchResult = Models.MigrationBatchResult.SuccessResult(
                totalMigrationsAttempted: totalMigrationsAttempted,
                successfulMigrations: successfulMigrations,
                tenantResults: allTenantResults
            );

            if (batchResult.IsSuccess)
            {
                _logger.LogInformation("Batch migration completed for {Count} database(s): {Summary}",
                    databaseIds.Count, batchResult.ResultSummary);
            }
            else
            {
                _logger.LogWarning("Batch migration completed with failures for {Count} database(s): {Summary}",
                    databaseIds.Count, batchResult.ResultSummary);
            }

            return batchResult;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error applying migrations to multiple databases: {Message}", ex.Message);
            return Models.MigrationBatchResult.FailureResult(ex.Message, new List<Models.TenantMigrationResult>());
        }
    }

    private async Task<Models.TenantMigrationResult> ApplyMigrationsToDatabaseWithFaultIsolationAsync(
        string databaseId,
        string executedBy,
        List<Models.Migration> pendingMigrations,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(databaseId);
        ArgumentNullException.ThrowIfNull(pendingMigrations);
        ArgumentException.ThrowIfNullOrEmpty(executedBy);

        var failures = new List<Models.MigrationFailure>();
        int successfulCount = 0;
        string? lastSuccessfulVersion = null;

        _logger.LogInformation("Starting fault-isolated migration for database {DatabaseId} with {Count} pending migrations",
            databaseId, pendingMigrations.Count);

        foreach (var migration in pendingMigrations.OrderBy(m => m.ExecutionOrder))
        {
            try
            {
                // Mark migration as started
                migration.MarkAsStarted(executedBy);
                await _repository.UpdateAsync(migration, cancellationToken);
                _logger.LogInformation("Executing migration {Version} - {Name} for database {DatabaseId}",
                    migration.Version, migration.Name, databaseId);

                // Execute the actual migration script
                var executionTimeMs = await ExecuteMigrationScriptAsync(migration, cancellationToken);

                // Mark as completed
                migration.MarkAsCompleted(executionTimeMs);
                await _repository.UpdateAsync(migration, cancellationToken);

                successfulCount++;
                lastSuccessfulVersion = migration.Version;
                _logger.LogInformation("Successfully completed migration {Version} for database {DatabaseId}",
                    migration.Version, databaseId);
            }
            catch (MigrationException mex)
            {
                _logger.LogError("Migration failed for database {DatabaseId}: {Message}", databaseId, mex.Message);
                failures.Add(mex.ToMigrationFailure());

                // Mark as failed in database
                try
                {
                    migration.MarkAsFailed(mex.Message);
                    await _repository.UpdateAsync(migration, cancellationToken);
                }
                catch (Exception updateEx)
                {
                    _logger.LogError("Failed to mark migration as failed in database {DatabaseId}: {Message}",
                        databaseId, updateEx.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error during migration {Version} for database {DatabaseId}: {Message}",
                    migration.Version, databaseId, ex.Message);

                var migrationException = MigrationException.ExecutionFailed(
                    migration.MigrationId,
                    migration.Version,
                    ex);

                failures.Add(migrationException.ToMigrationFailure());

                // Mark as failed in database
                try
                {
                    migration.MarkAsFailed(ex.Message);
                    await _repository.UpdateAsync(migration, cancellationToken);
                }
                catch (Exception updateEx)
                {
                    _logger.LogError("Failed to mark migration as failed in database {DatabaseId}: {Message}",
                        databaseId, updateEx.Message);
                }
            }
        }

        var tenantResult = Models.TenantMigrationResult.SuccessResult(
            databaseId: databaseId,
            tenantId: null, // Would be populated from tenant service in real implementation
            databaseName: null, // Would be populated from database service in real implementation
            totalMigrationsAttempted: pendingMigrations.Count,
            successfulMigrations: successfulCount,
            schemaVersionReached: lastSuccessfulVersion
        );

        if (failures.Count > 0)
        {
            tenantResult = Models.TenantMigrationResult.FailureResult(
                databaseId: databaseId,
                tenantId: null,
                databaseName: null,
                totalMigrationsAttempted: pendingMigrations.Count,
                successfulMigrations: successfulCount,
                schemaVersionReached: lastSuccessfulVersion,
                failures: failures
            );
        }

        _logger.LogInformation("Migration batch completed for database {DatabaseId}: {Summary}",
            databaseId, tenantResult.IsSuccess
            ? $"Success: {successfulCount}/{pendingMigrations.Count} migrations applied"
            : $"Failed: {failures.Count} migration(s) failed, schema version reached: {lastSuccessfulVersion}");

        return tenantResult;
    }

    /// <summary>
    /// Executes the migration SQL script against the database.
    /// In a real implementation, this would connect to the specific tenant database and execute the script.
    /// For this implementation, we simulate the execution and introduce controlled failure scenarios.
    /// </summary>
    /// <param name="migration">The migration to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution time in milliseconds.</returns>
    private async Task<long> ExecuteMigrationScriptAsync(Models.Migration migration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(migration);

        // Simulate database connection and execution
        await Task.Delay(50, cancellationToken); // Simulate actual execution time

        // Simulate controlled failure scenarios based on migration version
        // This allows testing the fault isolation system
        if (migration.Version.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
            migration.Version.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            throw new MigrationException(
                $"Migration execution failed: {migration.Name} (v{migration.Version})",
                migration.MigrationId,
                migration.Version,
                new Exception("Database constraint violation: column already exists or constraint failed"));
        }

        // Simulate occasional failures for demonstration (5% chance)
        if (_random.Next(0, 100) < 5)
        {
            throw new MigrationException(
                $"Random migration failure during execution of {migration.Name} (v{migration.Version})",
                migration.MigrationId,
                migration.Version,
                new Exception("Simulated database error"));
        }

        return _random.Next(10, 200); // Return random execution time between 10-200ms
    }
}