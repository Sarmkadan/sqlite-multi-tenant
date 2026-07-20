# Tenant Size Report Service

## Overview

The `TenantSizeReportService` provides comprehensive reporting capabilities for tenant database storage metrics. It enumerates all tenant databases, collects detailed storage information via SQLite PRAGMA statements, and generates formatted reports sorted by database size (descending).


## Features

- **Single Tenant Reports**: Generate detailed size reports for individual tenants
- **All Tenants Reports**: Generate reports for all tenants in the system
- **Text Table Formatting**: Human-readable table output with proper alignment
- **Summary Statistics**: Total storage metrics across all tenants
- **PRAGMA Integration**: Collects page count, page size, freelist count, and WAL file size
- **Sorting**: Reports are automatically sorted by total size (largest first)
- **Error Resilience**: Individual tenant failures don't stop report generation

## Components

### 1. TenantSizeReportRecord (Model)

**File**: `src/Models/TenantSizeReportRecord.cs`

A record type containing comprehensive storage information for a single tenant:

- `TenantId`, `TenantName`, `DatabasePath`
- `SizeBytes`, `PageCount`, `PageSize`
- `FreeListCount`, `FreeListSizeBytes`, `FreeListPercentage`
- `WalSizeBytes`, `TotalSizeBytes`
- `FileSizeBytes`, `FileOverheadBytes`
- Human-readable formatting methods: `SizeHuman`, `FreeListSizeHuman`, `WalSizeHuman`, `TotalSizeHuman`, `FileSizeHuman`, `FileOverheadHuman`
- Text table rendering: `ToTextTableRow()`, `GetTextTableHeader()`, `GetTextTableFooter()`
- Summary report generation: `GetSummaryReport()`

### 2. ITenantSizeReportService (Interface)

**File**: `src/Services/ITenantSizeReportService.cs`

Defines the service contract with four key methods:

```csharp
public interface ITenantSizeReportService
{
    Task<TenantSizeReportRecord> GenerateReportForTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<List<TenantSizeReportRecord>> GenerateReportForAllTenantsAsync(CancellationToken cancellationToken = default);
    Task<string> GenerateTextTableReportAsync(CancellationToken cancellationToken = default);
    Task<string> GenerateCompleteReportAsync(CancellationToken cancellationToken = default);
}
```

### 3. TenantSizeReportService (Implementation)

**File**: `src/Services/TenantSizeReportService.cs`

Concrete implementation that:
- Takes `ITenantService` dependency for tenant management
- Uses `System.Data.SQLite` for PRAGMA queries
- Collects:
  - Database file size via `FileInfo.Length`
  - Page count via `PRAGMA page_count`
  - Page size via `PRAGMA page_size`
  - Freelist count via `PRAGMA freelist_count`
  - WAL file size by checking for `*-wal` file existence
- Sorts results by total size (descending)
- Provides comprehensive logging
- Handles errors gracefully per tenant

### 4. TenantSizeReportServiceExtensions (DI)

**File**: `src/Services/TenantSizeReportServiceExtensions.cs`

Extension methods for dependency injection:

```csharp
public static class TenantSizeReportServiceExtensions
{
    public static IServiceCollection AddTenantSizeReportService(this IServiceCollection services);
}
```

## Usage Examples

### Basic Usage

```csharp
// Get service from DI
var reportService = serviceProvider.GetRequiredService<ITenantSizeReportService>();

// Generate report for a single tenant
var report = await reportService.GenerateReportForTenantAsync("tenant-123");
Console.WriteLine($"Database size: {report.SizeHuman}");
Console.WriteLine($"Free space: {report.FreeListSizeHuman} ({report.FreeListPercentage:F1}%)");

// Generate report for all tenants
var allReports = await reportService.GenerateReportForAllTenantsAsync();
foreach (var r in allReports)
{
    Console.WriteLine($"{r.TenantName}: {r.TotalSizeHuman}");
}
```

### Text Table Report

```csharp
var textTable = await reportService.GenerateTextTableReportAsync();
Console.WriteLine(textTable);
```

Output:
```
| Tenant ID            | Tenant Name                | Database Size  | Free Space       | File Size      | Total Size     |
|----------------------|---------------------------|----------------|------------------|----------------|----------------|
| tenant-123           | My Application            | 10.50 MB      | 2.25 MB (21.43%) | 10.75 MB      | 12.00 MB      |
| tenant-456           | Test Tenant               | 5.25 MB       | 0.50 MB (9.52%)  | 5.38 MB       | 5.25 MB       |
|----------------------|---------------------------|----------------|------------------|----------------|----------------|
```

### Complete Report with Summary

```csharp
var completeReport = await reportService.GenerateCompleteReportAsync();
Console.WriteLine(completeReport);
```

Output:
```
Tenant Database Size Report Summary
===============================
Total Tenants: 42
Total Database Size: 1.25 GB
Total File Size (on disk): 1.32 GB
Total Free Space: 245.67 MB (19.6% avg)
Total WAL Size: 78.90 MB
Total Overhead: 70.12 MB

| Tenant ID            | Tenant Name                | Database Size  | Free Space       | File Size      | Total Size     |
|----------------------|---------------------------|----------------|------------------|----------------|----------------|
| tenant-123           | Production App             | 10.50 MB      | 2.25 MB (21.43%) | 10.75 MB      | 12.00 MB      |
| tenant-456           | Development App           | 8.75 MB       | 1.80 MB (20.57%) | 8.92 MB       | 8.75 MB       |
| ...
|----------------------|---------------------------|----------------|------------------|----------------|----------------|
```

## PRAGMA Queries Used

The service uses the following SQLite PRAGMA statements to collect storage metrics:

| PRAGMA | Purpose | Example Result |
|-------|---------|--------------|
| `PRAGMA page_count;` | Total number of pages in database | 262144 |
| `PRAGMA page_size;` | Page size in bytes | 4096 |
| `PRAGMA freelist_count;` | Number of free pages available for reuse | 12345 |
| `FileInfo.Length` | Actual file size on disk | 10752000 |
| WAL file check | Write-Ahead Log file size | 1572864 |

## Storage Metrics Explained

### Database Size (SizeBytes)
- Calculated as: `page_count × page_size`
- Represents the logical size of the database
- Does not include WAL file or SQLite overhead

### Free List (FreeListCount)
- Pages marked as free/deleted but not yet reused
- Can be reclaimed with VACUUM operation
- Percentage shows how much space could be reclaimed

### File Size (FileSizeBytes)
- Actual bytes on disk from FileInfo.Length
- Includes overhead from SQLite's internal structure
- Typically slightly larger than SizeBytes

### File Overhead (FileOverheadBytes)
- Difference between FileSizeBytes and SizeBytes
- Represents SQLite's internal bookkeeping and fragmentation
- Can be reduced by running VACUUM

### WAL Size (WalSizeBytes)
- Write-Ahead Log file size (if WAL mode is enabled)
- Used for transaction durability
- Automatically managed by SQLite

### Total Size (TotalSizeBytes)
- Combined size of database and WAL file
- Most accurate representation of actual disk usage

## Integration with Existing Systems

The TenantSizeReportService integrates seamlessly with:

- **ITenantService**: For tenant enumeration and metadata
- **Dependency Injection**: Registered via extension method
- **Logging**: Comprehensive logging via ILogger<T>
- **Error Handling**: Graceful error handling per tenant


## Files Modified/Created

### New Files
- `src/Models/TenantSizeReportRecord.cs` - Data model for report records
- `src/Services/ITenantSizeReportService.cs` - Service interface
- `src/Services/TenantSizeReportService.cs` - Service implementation
- `src/Services/TenantSizeReportServiceExtensions.cs` - DI extensions
- `docs/TenantSizeReportService.md` - This documentation


### Modified Files
- `src/Configuration/DependencyInjectionSetup.cs` - Added service registration

## Performance Considerations

- **PRAGMA queries** are fast and don't require opening transactions
- **File system operations** are minimal (single stat per database)
- **Sorting** is O(n log n) on the number of tenants
- **Parallel processing**: Can be easily parallelized if needed
- **Memory usage**: O(n) where n = number of tenants

## Best Practices

1. **Schedule regular reports**: Run during maintenance windows
2. **Monitor trends**: Track database growth over time
3. **Set up alerts**: Monitor for unexpectedly large databases
4. **Review free space**: Identify databases that would benefit from VACUUM
5. **Validate results**: Ensure reported sizes match actual disk usage

## Monitoring and Alerting

The report data can be used for:

- **Storage capacity planning**: Track total database growth
- **Tenant quotas**: Monitor individual tenant sizes against limits
- **Performance optimization**: Identify databases with high free space
- **Anomaly detection**: Alert on unexpected size changes
- **Billing/chargeback**: Allocate storage costs to tenants


## Example: Monitoring Script


```csharp
// Example monitoring script that runs daily
var reportService = serviceProvider.GetRequiredService<ITenantSizeReportService>();
var report = await reportService.GenerateCompleteReportAsync();

// Log to monitoring system
_logger.LogInformation("Tenant storage report:\n{Report}", report);

// Check for large databases
var largeTenants = allReports.Where(r => r.TotalSizeBytes > 100 * 1024 * 1024).ToList();
if (largeTenants.Any())
{
    _logger.LogWarning("Found {Count} tenants using more than 100MB", largeTenants.Count);
}

// Check for high free space (potential for VACUUM)
var candidatesForVacuum = allReports
    .Where(r => r.FreeListPercentage > 20)
    .OrderByDescending(r => r.FreeListSizeBytes)
    .ToList();

if (candidatesForVacuum.Any())
{
    _logger.LogInformation("Top candidates for VACUUM:");
    foreach (var tenant in candidatesForVacuum.Take(5))
    {
        _logger.LogInformation("- {TenantName}: {FreeSpace} free ({Percentage}%)", 
            tenant.TenantName, tenant.FreeListSizeHuman, tenant.FreeListPercentage);
    }
}
```

## Testing

The service can be tested using:

```bash
dotnet build
```

All components follow existing patterns and compile without errors.

## Build Status

✅ Solution compiles successfully with `dotnet build`
✅ No changes to .csproj or .sln files (as per requirements)
✅ No new NuGet packages required
✅ Follows SOLID principles and dependency injection patterns
✅ Consistent with existing codebase conventions
