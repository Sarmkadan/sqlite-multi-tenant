# MigrationBatchResult

Documentation for `src/Models/MigrationBatchResult.cs`

## Overview

The `MigrationBatchResult` represents the outcome of a batch migration operation across multiple tenants/databases. It aggregates success status, counts of migrations, and detailed tenant-level results.

## Types

### MigrationBatchResult

```csharp
public sealed record MigrationBatchResult
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Indicates whether the batch migration operation was successful. |
| `TotalMigrationsAttempted` | `int` | The total number of migrations that were attempted. |
| `SuccessfulMigrations` | `int` | The number of migrations that were successfully applied. |
| `FailedMigrations` | `int` | The number of migrations that failed. |
| `Error` | `string?` | Error message if the batch migration failed, otherwise null. |
| `IsSuccess` | `bool` | Computed property indicating whether the migration operation completed successfully (Success == true, Error == null/empty, and FailedMigrations == 0). |
| `TenantResults` | `List<TenantMigrationResult>` | Collection of tenant-specific migration results. |
| `FailedTenantResults` | `List<TenantMigrationResult>` | Collection of failed tenant migration results for easy access (where `FailedMigrations > 0`). |
| `ResultSummary` | `string` | Gets a human-readable summary of the migration result. |

#### Methods

| Method | Description |
|--------|-------------|
| `static MigrationBatchResult SuccessResult(int totalMigrationsAttempted, int successfulMigrations, List<TenantMigrationResult> tenantResults)` | Creates a successful migration batch result with the specified counts. |
| `static MigrationBatchResult FailureResult(string error, List<TenantMigrationResult>? tenantResults = null)` | Creates a failed migration batch result with the specified error message. |

### TenantMigrationResult

```csharp
public sealed record TenantMigrationResult
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `DatabaseId` | `string` | The tenant/database identifier. |
| `TenantId` | `string?` | The tenant identifier (if available). |
| `DatabaseName` | `string?` | The database name. |
| `TotalMigrationsAttempted` | `int` | Total migrations attempted for this tenant. |
| `SuccessfulMigrations` | `int` | Number of migrations successfully applied. |
| `FailedMigrations` | `int` | Number of migrations that failed. |
| `SchemaVersionReached` | `string?` | Schema version reached before failures (last successfully applied version). |
| `Failures` | `List<MigrationFailure>` | Collection of individual migration failures. |
| `IsSuccess` | `bool` | Indicates whether this tenant's migrations were all successful (FailedMigrations == 0). |

#### Methods

| Method | Description |
|--------|-------------|
| `static TenantMigrationResult SuccessResult(string databaseId, string? tenantId, string? databaseName, int totalMigrationsAttempted, int successfulMigrations, string? schemaVersionReached)` | Creates a tenant migration result with successful migrations. |
| `static TenantMigrationResult FailureResult(string databaseId, string? tenantId, string? databaseName, int totalMigrationsAttempted, int successfulMigrations, string? schemaVersionReached, List<MigrationFailure> failures)` | Creates a tenant migration result with failed migrations. |

### MigrationFailure

```csharp
public sealed record MigrationFailure
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `MigrationId` | `string` | The migration ID that failed. |
| `Version` | `string` | The migration version. |
| `Name` | `string` | The migration name. |
| `ErrorMessage` | `string` | The error message. |
| `ExceptionDetails` | `string?` | The exception that occurred (serialized if needed). |
| `FailedAt` | `DateTime` | The timestamp when the failure occurred. |

#### Methods

| Method | Description |
|--------|-------------|
| `static MigrationFailure Create(string migrationId, string version, string name, string errorMessage, Exception? exception = null)` | Creates a migration failure record. |

## How Per-Tenant Results Are Aggregated

In the `MigrationService`, batch migration results are aggregated through the `ApplyMigrationsToMultipleDatabasesAsync` method:

1. **Iterate Over Databases**: For each database ID in the input list:
   - Retrieve pending migrations for that database
   - If no pending migrations, create a successful `TenantMigrationResult` with zero counts
   - Otherwise, apply migrations with fault isolation via `ApplyMigrationsToDatabaseWithFaultIsolationAsync`

2. **Fault-Isolated Application**: For each database:
   - Process migrations in order of execution
   - Track successful migrations and failures separately
   - On failure, record the failure but continue processing remaining migrations (fault isolation)
   - Create a `TenantMigrationResult` for the database (success or failure based on whether any migrations failed)

3. **Aggregate Results**:
   - Sum `TotalMigrationsAttempted` across all tenants
   - Sum `SuccessfulMigrations` across all tenants
   - Collect all `TenantMigrationResult` objects
   - Create a `MigrationBatchResult.SuccessResult` with the aggregated totals and tenant results

4. **Failure Handling**:
   - If an exception occurs during database processing, create a failed `TenantMigrationResult` for that database
   - The overall batch result will reflect failures through the `IsSuccess` property and `ResultSummary`

## Usage Example

```csharp
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;
using Microsoft.Extensions.Logging;

// Assume we have an IMigrationRepository and ILogger<MigrationService>
var repository = /* obtain IMigrationRepository instance */;
var logger = /* obtain ILogger<MigrationService> instance */;
var migrationService = new MigrationService(repository, logger);

// List of database IDs to migrate
var databaseIds = new List<string> { "db1", "db2", "db3" };
var executedBy = "migration-service";

// Apply migrations to multiple databases
MigrationBatchResult batchResult = await migrationService.ApplyMigrationsToMultipleDatabasesAsync(
    databaseIds, 
    executedBy
);

// Check overall success
if (batchResult.IsSuccess)
{
    Console.WriteLine($"✅ {batchResult.ResultSummary}");
}
else
{
    Console.WriteLine($"❌ {batchResult.ResultSummary}");
    
    // Inspect failed tenants
    foreach (var failedTenant in batchResult.FailedTenantResults)
    {
        Console.WriteLine($"  Failed database: {failedTenant.DatabaseId}");
        Console.WriteLine($"    Failures: {failedTenant.FailedMigrations}/{failedTenant.TotalMigrationsAttempted}");
        
        // Inspect individual migration failures
        foreach (var failure in failedTenant.Failures)
        {
            Console.WriteLine($"      Migration {failure.Version} ({failure.Name}): {failure.ErrorMessage}");
        }
    }
}

// Access aggregated counts
Console.WriteLine($"Total migrations attempted: {batchResult.TotalMigrationsAttempted}");
Console.WriteLine($"Successful migrations: {batchResult.SuccessfulMigrations}");
Console.WriteLine($"Failed migrations: {batchResult.FailedMigrations}");
```

## Notes

- The `IsSuccess` property provides a quick way to check if the entire batch operation succeeded without any errors or failures.
- The `ResultSummary` property generates a human-readable description suitable for logging or display.
- Tenant results are stored in `TenantResults` for detailed inspection, while `FailedTenantResults` provides convenient access to only the failed tenants.
- The aggregation pattern ensures that partial successes are preserved - even if some databases fail, successful migrations on other databases are still counted and reported.