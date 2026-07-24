#nullable enable

using System.Text;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Represents the result of a batch migration operation across multiple tenants/databases.
/// Contains success status, count of successful migrations, and detailed tenant-level results.
/// </summary>
public sealed record MigrationBatchResult
{
    /// <summary>
    /// Indicates whether the batch migration operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The total number of migrations that were attempted.
    /// </summary>
    public int TotalMigrationsAttempted { get; init; }

    /// <summary>
    /// The number of migrations that were successfully applied.
    /// </summary>
    public int SuccessfulMigrations { get; init; }

    /// <summary>
    /// The number of migrations that failed.
    /// </summary>
    public int FailedMigrations { get; init; }

    /// <summary>
    /// Error message if the batch migration failed, otherwise null.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Indicates whether the migration operation completed successfully.
    /// </summary>
    public bool IsSuccess => Success && string.IsNullOrEmpty(Error) && FailedMigrations == 0;

    /// <summary>
    /// Collection of tenant-specific migration results.
    /// </summary>
    public List<TenantMigrationResult> TenantResults { get; init; } = new();

    /// <summary>
    /// Collection of failed tenant migration results for easy access.
    /// </summary>
    public List<TenantMigrationResult> FailedTenantResults => TenantResults.Where(t => t.FailedMigrations > 0).ToList();

    /// <summary>
    /// Gets a human-readable summary of the migration result.
    /// </summary>
    public string ResultSummary
    {
        get
        {
            if (IsSuccess)
            {
                return Success
                    ? $"Success: {SuccessfulMigrations}/{TotalMigrationsAttempted} migrations applied across {TenantResults.Count} tenant(s)"
                    : "Success";
            }

            var sb = new StringBuilder();
            sb.Append("Failed");

            if (!string.IsNullOrEmpty(Error))
            {
                sb.Append($": {Error}");
            }
            else if (FailedMigrations > 0)
            {
                sb.Append($": {FailedMigrations} migration(s) failed across {FailedTenantResults.Count} tenant(s)");
            }
            else
            {
                sb.Append(": Unknown error occurred");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Creates a successful migration batch result with the specified counts.
    /// </summary>
    /// <param name="totalMigrationsAttempted">Total migrations attempted.</param>
    /// <param name="successfulMigrations">Number of successful migrations.</param>
    /// <param name="tenantResults">Tenant-level results.</param>
    public static MigrationBatchResult SuccessResult(
        int totalMigrationsAttempted,
        int successfulMigrations,
        List<TenantMigrationResult> tenantResults)
    {
        ArgumentNullException.ThrowIfNull(tenantResults);

        if (successfulMigrations < 0)
            throw new ArgumentOutOfRangeException(nameof(successfulMigrations), "Value cannot be negative");
        if (totalMigrationsAttempted < successfulMigrations)
            throw new ArgumentOutOfRangeException(nameof(totalMigrationsAttempted), "Total cannot be less than successful");

        return new MigrationBatchResult
        {
            Success = true,
            TotalMigrationsAttempted = totalMigrationsAttempted,
            SuccessfulMigrations = successfulMigrations,
            FailedMigrations = totalMigrationsAttempted - successfulMigrations,
            Error = null,
            TenantResults = tenantResults
        };
    }

    /// <summary>
    /// Creates a failed migration batch result with the specified error message.
    /// </summary>
    /// <param name="error">Error message.</param>
    /// <param name="tenantResults">Tenant-level results (may be partial).</param>
    public static MigrationBatchResult FailureResult(string error, List<TenantMigrationResult>? tenantResults = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(error);

        return new MigrationBatchResult
        {
            Success = false,
            TotalMigrationsAttempted = 0,
            SuccessfulMigrations = 0,
            FailedMigrations = 0,
            Error = error,
            TenantResults = tenantResults ?? new List<TenantMigrationResult>()
        };
    }
}

/// <summary>
/// Represents the migration result for a specific tenant/database.
/// </summary>
public sealed record TenantMigrationResult
{
    /// <summary>
    /// The tenant/database identifier.
    /// </summary>
    public string DatabaseId { get; init; } = string.Empty;

    /// <summary>
    /// The tenant identifier (if available).
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// The database name.
    /// </summary>
    public string? DatabaseName { get; init; }

    /// <summary>
    /// Total migrations attempted for this tenant.
    /// </summary>
    public int TotalMigrationsAttempted { get; init; }

    /// <summary>
    /// Number of migrations successfully applied.
    /// </summary>
    public int SuccessfulMigrations { get; init; }

    /// <summary>
    /// Number of migrations that failed.
    /// </summary>
    public int FailedMigrations { get; init; }

    /// <summary>
    /// Schema version reached before failures (last successfully applied version).
    /// </summary>
    public string? SchemaVersionReached { get; init; }

    /// <summary>
    /// Collection of individual migration failures.
    /// </summary>
    public List<MigrationFailure> Failures { get; init; } = new();

    /// <summary>
    /// Indicates whether this tenant's migrations were all successful.
    /// </summary>
    public bool IsSuccess => FailedMigrations == 0;

    /// <summary>
    /// Creates a tenant migration result with successful migrations.
    /// </summary>
    public static TenantMigrationResult SuccessResult(
        string databaseId,
        string? tenantId,
        string? databaseName,
        int totalMigrationsAttempted,
        int successfulMigrations,
        string? schemaVersionReached)
    {
        ArgumentException.ThrowIfNullOrEmpty(databaseId);

        if (successfulMigrations < 0)
            throw new ArgumentOutOfRangeException(nameof(successfulMigrations), "Value cannot be negative");
        if (totalMigrationsAttempted < successfulMigrations)
            throw new ArgumentOutOfRangeException(nameof(totalMigrationsAttempted), "Total cannot be less than successful");

        return new TenantMigrationResult
        {
            DatabaseId = databaseId,
            TenantId = tenantId,
            DatabaseName = databaseName,
            TotalMigrationsAttempted = totalMigrationsAttempted,
            SuccessfulMigrations = successfulMigrations,
            FailedMigrations = totalMigrationsAttempted - successfulMigrations,
            SchemaVersionReached = schemaVersionReached,
            Failures = new List<MigrationFailure>()
        };
    }

    /// <summary>
    /// Creates a tenant migration result with failed migrations.
    /// </summary>
    public static TenantMigrationResult FailureResult(
        string databaseId,
        string? tenantId,
        string? databaseName,
        int totalMigrationsAttempted,
        int successfulMigrations,
        string? schemaVersionReached,
        List<MigrationFailure> failures)
    {
        ArgumentException.ThrowIfNullOrEmpty(databaseId);
        ArgumentNullException.ThrowIfNull(failures);

        if (successfulMigrations < 0)
            throw new ArgumentOutOfRangeException(nameof(successfulMigrations), "Value cannot be negative");
        if (totalMigrationsAttempted < successfulMigrations)
            throw new ArgumentOutOfRangeException(nameof(totalMigrationsAttempted), "Total cannot be less than successful");

        return new TenantMigrationResult
        {
            DatabaseId = databaseId,
            TenantId = tenantId,
            DatabaseName = databaseName,
            TotalMigrationsAttempted = totalMigrationsAttempted,
            SuccessfulMigrations = successfulMigrations,
            FailedMigrations = failures.Count,
            SchemaVersionReached = schemaVersionReached,
            Failures = failures
        };
    }
}

/// <summary>
/// Represents a single migration failure with details.
/// </summary>
public sealed record MigrationFailure
{
    /// <summary>
    /// The migration ID that failed.
    /// </summary>
    public string MigrationId { get; init; } = string.Empty;

    /// <summary>
    /// The migration version.
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// The migration name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The error message.
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>
    /// The exception that occurred (serialized if needed).
    /// </summary>
    public string? ExceptionDetails { get; init; }

    /// <summary>
    /// The timestamp when the failure occurred.
    /// </summary>
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a migration failure record.
    /// </summary>
    public static MigrationFailure Create(
        string migrationId,
        string version,
        string name,
        string errorMessage,
        Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(migrationId);
        ArgumentException.ThrowIfNullOrEmpty(version);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(errorMessage);

        return new MigrationFailure
        {
            MigrationId = migrationId,
            Version = version,
            Name = name,
            ErrorMessage = errorMessage,
            ExceptionDetails = exception?.ToString(),
            FailedAt = DateTime.UtcNow
        };
    }
}