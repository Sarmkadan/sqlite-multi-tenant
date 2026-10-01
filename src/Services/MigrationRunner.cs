#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;
using System.Diagnostics;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Database;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Executes migration scripts against tenant databases. Handles ordering, transaction
/// wrapping, rollback on failure, and records each migration result via <see cref="IMigrationService"/>.
/// </summary>
public sealed class MigrationRunner {
    private readonly IMigrationService _migrationService;
    private readonly IConnectionPoolManager _connectionPool;
    private readonly ILogger<MigrationRunner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationRunner"/> class.
    /// </summary>
    /// <param name="migrationService">Service for persisting migration records.</param>
    /// <param name="connectionPool">Connection pool for acquiring tenant connections.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public MigrationRunner(
        IMigrationService migrationService,
        IConnectionPoolManager connectionPool,
        ILogger<MigrationRunner> logger)
    {
        _migrationService = migrationService ?? throw new ArgumentNullException(nameof(migrationService));
        _connectionPool = connectionPool ?? throw new ArgumentNullException(nameof(connectionPool));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs a single SQL migration against the specified tenant database within a transaction.
    /// On failure the transaction is rolled back and the migration is marked as <see cref="MigrationStatus.Failed"/>.
    /// </summary>
    /// <param name="tenantId">Target tenant identifier.</param>
    /// <param name="connectionString">SQLite connection string for the tenant database.</param>
    /// <param name="migrationSql">The SQL script to execute.</param>
    /// <param name="migrationName">A human-readable name for the migration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="MigrationResult"/> describing the outcome.</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are null or whitespace.</exception>
    public async Task<MigrationResult> RunMigrationAsync(
        string tenantId,
        string connectionString,
        string migrationSql,
        string migrationName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(migrationSql))
            throw new ArgumentException("Migration SQL is required.", nameof(migrationSql));
        if (string.IsNullOrWhiteSpace(migrationName))
            throw new ArgumentException("Migration name is required.", nameof(migrationName));

        _logger.LogInformation("Running migration '{Name}' for tenant '{TenantId}'", migrationName, tenantId);
        var sw = Stopwatch.StartNew();

        var connection = await _connectionPool.AcquireAsync(tenantId, connectionString, cancellationToken);
        try
        {
            using var transaction = connection.BeginTransaction();
            try
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = migrationSql;
                await command.ExecuteNonQueryAsync(cancellationToken);

                transaction.Commit();
                sw.Stop();

                _logger.LogInformation(
                    "Migration '{Name}' completed for tenant '{TenantId}' in {Elapsed}ms",
                    migrationName, tenantId, sw.ElapsedMilliseconds);

                return MigrationResult.SuccessResult(appliedCount: 1);
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); }
                catch (Exception rbEx)
                {
                    _logger.LogError(rbEx, "Rollback failed for migration '{Name}' on tenant '{TenantId}'",
                        migrationName, tenantId);
                }

                sw.Stop();
                _logger.LogError(ex, "Migration '{Name}' failed for tenant '{TenantId}'", migrationName, tenantId);

                return MigrationResult.FailureResult(ex.Message);
            }
        }
        finally
        {
            await _connectionPool.ReleaseAsync(tenantId, connection);
        }
    }

    /// <summary>
    /// Runs multiple migrations in order against a tenant database. Stops at the first failure.
    /// </summary>
    /// <param name="tenantId">Target tenant identifier.</param>
    /// <param name="connectionString">SQLite connection string for the tenant database.</param>
    /// <param name="migrations">Ordered list of (name, sql) tuples to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="MigrationBatchResult"/> summarizing all applied and skipped migrations.</returns>
    public async Task<MigrationBatchResult> RunMigrationsAsync(
        string tenantId,
        string connectionString,
        IReadOnlyList<(string Name, string Sql)> migrations,
        CancellationToken cancellationToken = default)
    {
        if (migrations == null || migrations.Count == 0)
            throw new ArgumentException("At least one migration is required.", nameof(migrations));

        var results = new List<MigrationResult>(migrations.Count);
        var sw = Stopwatch.StartNew();

        foreach (var (name, sql) in migrations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await RunMigrationAsync(tenantId, connectionString, sql, name, cancellationToken);
            results.Add(result);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Migration batch halted at '{Name}' for tenant '{TenantId}': {Error}",
                    name, tenantId, result.Error);
                break;
            }
        }

        sw.Stop();
        var succeeded = results.Count(r => r.Success);
        var failed = results.Count(r => !r.Success);
        var tenantResult = new TenantMigrationResult
        {
            DatabaseId = tenantId,
            TenantId = tenantId,
            TotalMigrationsAttempted = migrations.Count,
            SuccessfulMigrations = succeeded,
            FailedMigrations = failed
        };

        if (failed == 0)
            return MigrationBatchResult.SuccessResult(migrations.Count, succeeded, new List<TenantMigrationResult> { tenantResult });

        var failureDetails = results.Where(r => !r.Success).Select(r => r.Error ?? "Unknown error").First();
        return MigrationBatchResult.FailureResult(failureDetails!, new List<TenantMigrationResult> { tenantResult });
    }

    /// <summary>
    /// Checks which migrations from a given list have already been applied to a tenant database.
    /// </summary>
    /// <param name="tenantId">Target tenant identifier.</param>
    /// <param name="databaseId">The database identifier within the tenant.</param>
    /// <param name="migrationNames">Names of migrations to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Set of migration names that have already been applied.</returns>
    public async Task<HashSet<string>> GetAppliedMigrationsAsync(
        string tenantId,
        string databaseId,
        IEnumerable<string> migrationNames,
        CancellationToken cancellationToken = default)
    {
        var existing = await _migrationService.GetDatabaseMigrationsAsync(databaseId, cancellationToken);
        var appliedNames = new HashSet<string>(
            existing
                .Where(m => m.Status == MigrationStatus.Completed)
                .Select(m => m.Name),
            StringComparer.OrdinalIgnoreCase);

        return appliedNames;
    }

    /// <summary>
    /// Validates a migration SQL script by parsing it without executing.
    /// Uses SQLite's EXPLAIN to detect syntax errors.
    /// </summary>
    /// <param name="connectionString">SQLite connection string for validation.</param>
    /// <param name="migrationSql">The SQL to validate.</param>
    /// <returns>True if the SQL parses successfully; false otherwise.</returns>
    public bool ValidateMigrationSyntax(string connectionString, string migrationSql)
    {
        if (string.IsNullOrWhiteSpace(migrationSql))
            return false;

        try
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"EXPLAIN {migrationSql}";
            command.ExecuteNonQuery();
            return true;
        }
        catch (SQLiteException ex)
        {
            _logger.LogDebug(ex, "Migration SQL validation failed");
            return false;
        }
    }
}
