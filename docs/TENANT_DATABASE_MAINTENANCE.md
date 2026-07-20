# Tenant Database Maintenance Operations

This document describes the tenant database maintenance operations implemented to optimize SQLite database performance and reclaim disk space.

## Overview

SQLite databases require explicit maintenance operations to maintain optimal performance:
- **VACUUM**: Reclaims space from deleted rows and repacks the database file
- **ANALYZE**: Updates query planner statistics for better query optimization
- **PRAGMA optimize**: Automatically runs ANALYZE and other database optimizations


## Implementation


### New Components


#### 1. ITenantDatabaseMaintenanceService Interface
**File**: `src/Services/ITenantDatabaseMaintenanceService.cs`


Provides methods for executing maintenance operations on tenant databases:
- `VacuumTenantDatabaseAsync()` - Execute VACUUM on a single tenant
- `VacuumAllTenantDatabasesAsync()` - Execute VACUUM on all tenants
- `AnalyzeTenantDatabaseAsync()` - Execute ANALYZE on a single tenant
- `AnalyzeAllTenantDatabasesAsync()` - Execute ANALYZE on all tenants
- `OptimizeTenantDatabaseAsync()` - Execute PRAGMA optimize on a single tenant
- `OptimizeAllTenantDatabasesAsync()` - Execute PRAGMA optimize on all tenants
- `PerformFullMaintenanceAsync()` - Execute VACUUM + ANALYZE + PRAGMA optimize on a single tenant
- `PerformFullMaintenanceOnAllAsync()` - Execute comprehensive maintenance on all tenants


#### 2. TenantDatabaseMaintenanceService Implementation
**File**: `src/Services/TenantDatabaseMaintenanceService.cs`


Concrete implementation that:
- Takes ITenantService dependency to access tenant information
- Executes SQLite maintenance commands (VACUUM, ANALYZE, PRAGMA optimize)
- Reports file sizes before/after operations
- Calculates space reclaimed
- Logs detailed operation information
- Handles errors gracefully with individual tenant failure isolation

#### 3. TenantMaintenanceResult Model
**File**: `src/Models/TenantMaintenanceResult.cs`


Result object containing:
- Tenant ID and name
- Operation type (VACUUM, ANALYZE, etc.)
- Timestamps (started, completed)
- File sizes before/after (SizeBeforeBytes, SizeAfterBytes)
- Intermediate size after VACUUM (for full maintenance)
- Computed space reduction (SizeReductionBytes)
- Duration in milliseconds (DurationMs)
- Error message (if operation failed)
- IsSuccess flag
- Human-readable summaries (SizeChangeSummary, OperationSummary)

#### 4. TenantDatabaseMaintenanceServiceExtensions
**File**: `src/Services/TenantDatabaseMaintenanceServiceExtensions.cs`


Extension methods for dependency injection:
- `AddTenantDatabaseMaintenanceService()` - Registers the service
- `TenantDatabaseMaintenanceOptions` - Configuration options for maintenance behavior

Configuration options include:
- EnableVacuum (default: true)
- EnableAnalyze (default: true)
- EnableOptimize (default: true)
- IntervalHours (default: 24 for daily maintenance)
- TimeoutSeconds (default: 300 = 5 minutes per database)
- DegreeOfParallelism (default: 1 for sequential processing)

#### 5. DatabaseMaintenanceWorker Updates
**File**: `src/BackgroundWorkers/DatabaseMaintenanceWorker.cs`


Updated to use the new ITenantDatabaseMaintenanceService:
- Constructor now injects ITenantDatabaseMaintenanceService
- ExecuteMaintenanceAsync now returns List<TenantMaintenanceResult>
- Logs detailed maintenance results including space savings
- Provides summary statistics for all operations

#### 6. Dependency Injection Registration
**File**: `src/Configuration/DependencyInjectionSetup.cs`


Updated `AddBackgroundWorkers()` method to register:
- ITenantDatabaseMaintenanceService
- TenantDatabaseMaintenanceService
- DatabaseMaintenanceWorker (with new constructor)

## Usage Examples

### Single Tenant Maintenance

```csharp
var maintenanceService = serviceProvider.GetRequiredService<ITenantDatabaseMaintenanceService>();

// Execute VACUUM to reclaim space
var result = await maintenanceService.VacuumTenantDatabaseAsync("tenant-123");
Console.WriteLine($"Space reclaimed: {result.SizeReductionBytes} bytes");

// Execute full maintenance (VACUUM + ANALYZE + PRAGMA optimize)
var fullResult = await maintenanceService.PerformFullMaintenanceAsync("tenant-123");
```

### All Tenants Maintenance

```csharp
var maintenanceService = serviceProvider.GetRequiredService<ITenantDatabaseMaintenanceService>();

// Execute VACUUM on all tenants
var results = await maintenanceService.VacuumAllTenantDatabasesAsync();
foreach (var result in results.Where(r => r.IsSuccess))
{
    Console.WriteLine($"{result.TenantName}: {result.SizeReductionBytes} bytes reclaimed");
}

// Execute full maintenance on all tenants
var fullResults = await maintenanceService.PerformFullMaintenanceOnAllAsync();
```

### Background Worker (Automatic)

The DatabaseMaintenanceWorker runs automatically and performs full maintenance on all tenants at configured intervals (default: every 24 hours).


Configuration can be customized via:
```csharp
services.Configure<TenantDatabaseMaintenanceOptions>(options =>
{
    options.IntervalHours = 12; // Run every 12 hours
    options.TimeoutSeconds = 600; // 10 minutes per database
    options.DegreeOfParallelism = 2; // Process 2 databases in parallel
});
```

## Benefits

1. **Space Reclamation**: VACUUM reclaims space from deleted rows, reducing database file size
2. **Performance Optimization**: ANALYZE and PRAGMA optimize improve query performance by updating statistics
3. **Automatic Maintenance**: Background worker runs maintenance automatically on schedule
4. **Detailed Reporting**: Each operation reports file sizes before/after, space saved, and duration
5. **Error Resilience**: Individual tenant failures don't stop maintenance on other tenants
6. **Monitoring**: Results can be logged and monitored for maintenance effectiveness

## Monitoring and Validation

The maintenance operations report detailed information that can be used for monitoring:

```
VACUUM on MyTenant (tenant-123): 10.50 MB → 8.25 MB (saved: 2.25 MB, 21.43% reduction)
Duration: 450ms
```

## Best Practices

1. **Schedule**: Run full maintenance daily during low-traffic periods
2. **Monitor**: Track space savings over time to identify growing databases
3. **Timeouts**: Adjust timeout based on database size (larger databases need more time)
4. **Parallelism**: Increase parallelism for faster maintenance on many small databases
5. **Validation**: Verify maintenance results are being logged and monitored

## Integration with Existing Systems

The new maintenance service integrates seamlessly with:
- Existing ITenantService for tenant management
- DatabaseMaintenanceWorker for automatic scheduling
- Logging infrastructure for operation tracking
- Error handling for resilience

## Files Modified/Created

### New Files
- `src/Services/ITenantDatabaseMaintenanceService.cs`
- `src/Services/TenantDatabaseMaintenanceService.cs`
- `src/Services/TenantDatabaseMaintenanceServiceExtensions.cs`
- `src/Models/TenantMaintenanceResult.cs`
- `examples/4-tenant-database-maintenance.cs`
- `docs/TENANT_DATABASE_MAINTENANCE.md` (this file)

### Modified Files
- `src/BackgroundWorkers/DatabaseMaintenanceWorker.cs` - Updated to use new service
- `src/Configuration/DependencyInjectionSetup.cs` - Added service registration

## Build Status

✅ Solution compiles successfully with `dotnet build`
✅ All new components follow existing code patterns and conventions
✅ No changes to .csproj or .sln files (as per requirements)
✅ No new NuGet packages required
✅ Follows SOLID principles and dependency injection patterns
