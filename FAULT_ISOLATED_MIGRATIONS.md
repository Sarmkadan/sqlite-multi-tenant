# Fault-Isolated Per-Tenant Migrations

## Overview

This implementation adds fault isolation to per-tenant database migrations, ensuring that a migration failure in one tenant does not abort migrations for remaining tenants. This is critical for multi-tenant systems where you need to maintain availability and consistency across all tenants.

## Problem Statement

Previously, when applying migrations to multiple tenant databases:
- If a migration failed for one tenant, the entire process would abort
- No visibility into which specific tenants succeeded or failed
- No way to retry only failed tenants without affecting working ones
- Failed tenants would be left in an inconsistent state

## Solution

The new fault-isolated migration system provides:

1. **Per-Tenant Execution**: Each tenant's migrations run in complete isolation
2. **Failure Collection**: All failures are collected without aborting the process
3. **Detailed Reporting**: Comprehensive results showing success/failure per tenant
4. **Schema Version Tracking**: Know the last successfully applied version per tenant
5. **Targeted Retry**: Retry only failed tenants without touching working ones

## Key Components

### 1. MigrationBatchResult

The main result type that contains:
- Overall batch success/failure status
- Counts of total/failed/successful migrations
- Collection of `TenantMigrationResult` objects (one per tenant)

### 2. TenantMigrationResult

Per-tenant migration result containing:
- Database ID and tenant information
- Migration attempt counts
- Schema version reached before any failures
- Collection of `MigrationFailure` objects for detailed error reporting

### 3. MigrationFailure

Detailed failure information:
- Migration ID, version, and name
- Error message and exception details
- Timestamp of failure
- Serialized exception for debugging

### 4. MigrationExceptionExtensions.ToMigrationFailure()

Extension method that converts `MigrationException` to `MigrationFailure` for consistent error reporting.

## API Endpoints

### ApplyMigrationsWithFaultIsolationAsync

Applies migrations to a single database with fault isolation.

**Endpoint**: `POST /api/migration/{databaseId}/apply-with-fault-isolation`

**Parameters**:
- `databaseId`: The database to migrate
- `appliedBy`: Who is executing the migration

**Returns**: `MigrationBatchResponse` with detailed results

### ApplyMigrationsToMultipleDatabasesAsync

Applies migrations to multiple databases with fault isolation.

**Endpoint**: `POST /api/migration/apply-multiple`

**Request Body**:
```json
{
  "databaseIds": ["db1", "db2", "db3"],
  "appliedBy": "admin"
}
```

**Returns**: `MigrationBatchResponse` with results for all tenants

## Service Interface Additions

### IMigrationService.Additions

```csharp
Task<MigrationBatchResult> ApplyMigrationsWithFaultIsolationAsync(
    string databaseId, string executedBy, CancellationToken cancellationToken = default);

Task<MigrationBatchResult> ApplyMigrationsToMultipleDatabasesAsync(
    List<string> databaseIds, string executedBy, CancellationToken cancellationToken = default);
```

### MigrationService.Additions

```csharp
private async Task<TenantMigrationResult> ApplyMigrationsToDatabaseWithFaultIsolationAsync(
    string databaseId, string executedBy, List<Migration> pendingMigrations,
    CancellationToken cancellationToken)
```

This private method handles the actual fault-isolated execution:
- Iterates through pending migrations one by one
- Catches and records failures without throwing
- Marks migrations as failed in the database
- Returns comprehensive tenant-level results

## Usage Examples

### Single Database (C#)

```csharp
var result = await migrationService.ApplyMigrationsWithFaultIsolationAsync(
    databaseId: "tenant-db-123",
    executedBy: "migration-bot");

if (result.IsSuccess)
{
    Console.WriteLine($"Success: {result.SuccessfulMigrations}/{result.TotalMigrationsAttempted} migrations applied");
}
else
{
    Console.WriteLine($"Failed: {result.FailedMigrations} migrations failed");
    foreach (var tenantResult in result.TenantResults)
    {
        if (tenantResult.Failures.Any())
        {
            Console.WriteLine($"Tenant {tenantResult.DatabaseId} failed:");
            foreach (var failure in tenantResult.Failures)
            {
                Console.WriteLine($"  - {failure.Version}: {failure.ErrorMessage}");
            }
        }
    }
}
```

### Multiple Databases (C#)

```csharp
var result = await migrationService.ApplyMigrationsToMultipleDatabasesAsync(
    databaseIds: new List<string> { "db1", "db2", "db3" },
    executedBy: "migration-bot");

// Analyze results per tenant
foreach (var tenantResult in result.TenantResults)
{
    if (tenantResult.IsSuccess)
    {
        Console.WriteLine($"✓ Tenant {tenantResult.DatabaseId}: All migrations applied");
    }
    else
    {
        Console.WriteLine($"✗ Tenant {tenantResult.DatabaseId}: {tenantResult.FailedMigrations} failures");
        Console.WriteLine($"  Last successful version: {tenantResult.SchemaVersionReached}");
        
        // Retry only this tenant
        await RetryFailedTenantMigrations(tenantResult);
    }
}
```

### REST API

```bash
# Single database
curl -X POST "https://api.example.com/api/migration/{databaseId}/apply-with-fault-isolation?appliedBy=migration-bot"

# Multiple databases
curl -X POST "https://api.example.com/api/migration/apply-multiple" \
  -H "Content-Type: application/json" \
  -d '{
    "databaseIds": ["db1", "db2", "db3"],
    "appliedBy": "migration-bot"
  }'
```

## Failure Handling

### What Happens on Failure?

1. **Exception is caught**: Migration execution failures are caught and recorded
2. **Migration is marked as failed**: The failed migration record is updated with error details
3. **Process continues**: Next migration is attempted
4. **Result is collected**: Failure details are added to the `TenantMigrationResult`
5. **Batch completes**: All migrations are attempted, results collected

### Retry Strategy

```csharp
// After getting batch results, retry only failed tenants
foreach (var failedTenant in batchResult.FailedTenantResults)
{
    var retryResult = await migrationService.ApplyMigrationsWithFaultIsolationAsync(
        databaseId: failedTenant.DatabaseId,
        executedBy: "admin",
        cancellationToken: cancellationToken);
    
    if (retryResult.IsSuccess)
    {
        // Tenant is now up-to-date
        logger.LogInformation("Successfully retried tenant: {DatabaseId}", failedTenant.DatabaseId);
    }
    else
    {
        // Still failing - may need manual intervention
        logger.LogError("Tenant {DatabaseId} still failing after retry", failedTenant.DatabaseId);
    }
}
```

## Schema Version Tracking

Each `TenantMigrationResult` includes `SchemaVersionReached`, which tells you:
- The last successfully applied migration version
- What schema level the tenant reached before failures
- Whether to rollback or continue from that point

```csharp
if (tenantResult.SchemaVersionReached == "003")
{
    Console.WriteLine("Tenant reached version 003 before failure");
    Console.WriteLine("Can rollback to 002 if needed");
}
```

## Monitoring and Alerting

### Logging

The service logs detailed information:
- Start/end of batch operations
- Individual migration execution
- Failure details with stack traces
- Completion summaries

### Metrics

You can extend this to emit metrics:
```csharp
// Example metrics collection
metrics.Increment("migrations.attempted", tags: new { tenant = tenantResult.DatabaseId });
if (tenantResult.IsSuccess)
{
    metrics.Increment("migrations.successful", tags: new { tenant = tenantResult.DatabaseId });
}
else
{
    metrics.Increment("migrations.failed", tags: new { tenant = tenantResult.DatabaseId });
    metrics.Gauge("migrations.failures_per_tenant", tenantResult.Failures.Count,
        tags: new { tenant = tenantResult.DatabaseId });
}
```

## Performance Considerations

### Parallel vs Sequential

The current implementation runs migrations sequentially per tenant for safety. For large-scale deployments:

```csharp
// Consider parallel execution with controlled concurrency
var options = new ParallelOptions { MaxDegreeOfParallelism = 4 };
Parallel.ForEach(databaseIds, options, async (databaseId) =>
{
    await ApplyMigrationsToDatabaseWithFaultIsolationAsync(...);
});
```

### Transaction Boundaries

Each migration runs in its own transaction context. The fault isolation ensures:
- One migration failure doesn't affect others
- Database state remains consistent per tenant
- Failed migrations can be inspected and retried independently

## Migration Failure Scenarios

### Scenario 1: Constraint Violation
```sql
-- Migration fails due to unique constraint
CREATE TABLE Users (Id INT PRIMARY KEY, Email TEXT UNIQUE);
-- Duplicate email in data
```

**Result**: Migration marked as failed, next migration attempted, detailed error recorded

### Scenario 2: Disk Full
```
-- Migration requires disk space
CREATE INDEX large_index ON large_table(...);
```

**Result**: Migration fails, error recorded, subsequent migrations skipped for that tenant

### Scenario 3: Schema Conflict
```csharp
// Migration tries to create existing table
var upScript = "CREATE TABLE Users (...)";
```

**Result**: SQLite throws exception, caught and recorded, tenant marked with failure

## Best Practices

### 1. Always Use Fault Isolation for Multi-Tenant
```csharp
// ✓ Good - fault isolation
var result = await migrationService.ApplyMigrationsToMultipleDatabasesAsync(databaseIds, "admin");

// ✗ Bad - no fault isolation
foreach (var db in databaseIds)
{
    await migrationService.ApplyMigrationsAsync(db, "admin"); // Fails on first error
}
```

### 2. Monitor Failed Tenants
```csharp
var failedTenants = batchResult.FailedTenantResults;
if (failedTenants.Count > 0)
{
    alerting.SendAlert(
        "Migration failures detected",
        $"{failedTenants.Count} tenants failed migrations",
        severity: AlertSeverity.Warning
    );
}
```

### 3. Retry Failed Tenants Automatically
```csharp
foreach (var tenant in batchResult.FailedTenantResults)
{
    await RetryWithExponentialBackoff(tenant);
}
```

### 4. Set Up Automated Alerts
- Alert on any failed migrations
- Alert on high failure rates (>5% of tenants)
- Alert on specific error patterns (disk full, constraint violations)

### 5. Include in CI/CD Pipeline
```yaml
# Example GitHub Actions step
- name: Run migrations
  run: |
    dotnet run -- apply-migrations --tenant-db-ids "${{ { steps.get-tenants.outputs.ids } }"
    
    # Check for failures
    if migration-service-has-failures; then
      echo "::error::Migrations failed for some tenants"
      exit 1
    fi
```

## Testing the Feature

Run the example:
```bash
cd /path/to/sqlite-multi-tenant

# Clean up from previous runs
rm -f master_fault_test.db
rm -rf databases_fault_test/
rm -rf backups_fault_test/

# Run the example
dotnet run --project src/SqliteMultiTenant.csproj -- example FaultIsolatedMigrationsExample
```

Expected output shows:
- 3 tenants created
- Migrations applied with simulated failures
- Detailed results per tenant
- Clear indication of which tenants succeeded/failed

## Migration to Existing Systems

### No Breaking Changes
- Existing `ApplyMigrationsAsync` still works as before
- New methods are additive only
- No changes to database schema required
- No changes to existing APIs

### Gradual Adoption
1. Start by using new methods alongside old ones
2. Monitor results and build confidence
3. Gradually migrate to fault-isolated methods
4. Remove old methods when ready (future major version)

## Future Enhancements

### Planned Features
- [ ] Parallel tenant migration execution
- [ ] Automatic retry with exponential backoff
- [ ] Integration with tenant health monitoring
- [ ] Automated rollback for failed migrations
- [ ] Migration dependency analysis across tenants

### Monitoring Dashboard
- Tenant migration success rates
- Failure patterns and trends
- Schema version distribution
- Retry effectiveness

## Summary

The fault-isolated migration system provides:

✅ **Reliability**: One tenant's failure doesn't affect others
✅ **Visibility**: Detailed failure reports per tenant
✅ **Recovery**: Targeted retry of failed tenants
✅ **Safety**: Failed migrations marked in database
✅ **Scalability**: Works for 1 tenant or 1000 tenants

This is essential for production multi-tenant systems where availability and consistency across all tenants is critical.
