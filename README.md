Console.WriteLine($"Uptime: {diagnostics?.Uptime.TotalHours:F2} hours");
}
 
## IDataMapper
 
The `IDataMapper` interface defines a generic, reusable approach for transforming objects, particularly useful for mapping between domain entities and API contracts or DTOs. It provides capabilities for both simple property-based object mapping and bulk list transformations, with an emphasis on type safety and robustness during conversion.
 
### Public Members
 
```csharp
public sealed class DataMapper : IDataMapper
public DataMapper
public TTarget Map<TSource, TTarget>(TSource source) where TTarget : class, new
public List<TTarget> MapList<TSource, TTarget>(List<TSource> sources) where TTarget : class, new
public sealed class MappingProfile
public MappingProfile
public void AddCustomMapping<TSource, TTarget>
public bool TryGetCustomMapping
```
 
### Usage Example
 
```csharp
using SqliteMultiTenant.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
 
// Setup dependency injection
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<DataMapper>();
 
// Register mapper
var mapper = new DataMapper(logger);
 
// Example 1: Map an entity to a DTO
var tenant = new TenantEntity { Id = "acme-corp", Name = "Acme Corporation", CreatedDate = DateTime.UtcNow };
var dto = mapper.Map<TenantEntity, TenantDto>(tenant);
 
Console.WriteLine($"Mapped Tenant: {dto.Name} (ID: {dto.Id})");
 
// Example 2: Map a list of entities to a list of DTOs
var tenants = new List<TenantEntity>
{
    new TenantEntity { Id = "globex", Name = "Globex" },
    new TenantEntity { Id = "initech", Name = "Initech" }
};
var dtos = mapper.MapList<TenantEntity, TenantDto>(tenants);
 
Console.WriteLine($"Mapped {dtos.Count} tenants.");
 
public class TenantEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
 
public class TenantDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

## IStatisticsService
 
The `IStatisticsService` interface provides a standardized contract for collecting and analyzing system statistics and usage metrics. It records system events with contextual data, calculates aggregated statistics over time periods, and performs trend analysis on key metrics. This service is essential for monitoring system health, performance optimization, and capacity planning.
 
### Public Members
 
```csharp
public interface IStatisticsService
public Task RecordEventAsync(SystemEvent @event)
public Task<SystemStatistics> GetStatisticsAsync(TimeSpan period)
public Task<List<AggregatedMetric>> GetMetricsAsync(string metricName, TimeSpan period)
public Task<TrendAnalysis> AnalyzeTrendAsync(string metricName, TimeSpan period)
 
public sealed class SystemEvent
public string Id { get; set; }
public string EventType { get; set; }
public double Value { get; set; }
public TimeSpan? Duration { get; set; }
public DateTime Timestamp { get; set; }
public Dictionary<string, string> Tags { get; set; }
 
public sealed class SystemStatistics
public TimeSpan Period { get; set; }
public DateTime StartTime { get; set; }
public DateTime EndTime { get; set; }
public int TotalEvents { get; set; }
public Dictionary<string, int> EventTypeBreakdown { get; set; }
public double AverageResponseTime { get; set; }
public int PeakEventCount { get; set; }
 
public sealed class AggregatedMetric
public DateTime Timestamp { get; set; }
public double Value { get; set; }
public int Count { get; set; }
public double Min { get; set; }
public double Max { get; set; }
 
public sealed class TrendAnalysis
public string MetricName { get; set; }
public TimeSpan Period { get; set; }
public int DataPoints { get; set; }
public double AverageValue { get; set; }
public double MinValue { get; set; }
public double MaxValue { get; set; }
public string TrendDirection { get; set; }
public double TrendStrength { get; set; }
public double Volatility { get; set; }
public DateTime Timestamp { get; set; }
```
 
### Usage Example
 
```csharp
using SqliteMultiTenant.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
 
// Setup dependency injection
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<StatisticsService>();
 
// Register statistics service
services.AddSingleton<IStatisticsService, StatisticsService>();
var serviceProvider = services.BuildServiceProvider();
var statisticsService = serviceProvider.GetRequiredService<IStatisticsService>();
 
// Example 1: Record system events
var databaseEvent = new SystemEvent
{
    EventType = "DatabaseQuery",
    Value = 125.5, // Response time in milliseconds
    Duration = TimeSpan.FromMilliseconds(125),
    Tags = new Dictionary<string, string>
    {
        { "tenant", "acme-corp" },
        { "operation", "GetTenant" },
        { "status", "success" }
    }
};
 
await statisticsService.RecordEventAsync(databaseEvent);
 
var backupEvent = new SystemEvent
{
    EventType = "BackupOperation",
    Value = 15.7, // Size in MB
    Duration = TimeSpan.FromSeconds(12.5),
    Tags = new Dictionary<string, string>
    {
        { "tenant", "acme-corp" },
        { "type", "full" },
        { "status", "completed" }
    }
};
 
await statisticsService.RecordEventAsync(backupEvent);
 
// Example 2: Get statistics for the last hour
var hourlyStats = await statisticsService.GetStatisticsAsync(TimeSpan.FromHours(1));
Console.WriteLine($"Period: {hourlyStats.Period}");
Console.WriteLine($"Total events: {hourlyStats.TotalEvents}");
Console.WriteLine($"Peak event count: {hourlyStats.PeakEventCount}");
Console.WriteLine($"Average response time: {hourlyStats.AverageResponseTime:F2}ms");
Console.WriteLine("Event type breakdown:");
foreach (var kvp in hourlyStats.EventTypeBreakdown)
{
    Console.WriteLine($"  {kvp.Key}: {kvp.Value} events");
}
 
// Example 3: Get aggregated metrics for database queries over the last 24 hours
var queryMetrics = await statisticsService.GetMetricsAsync(
    "DatabaseQuery",
    TimeSpan.FromHours(24)
);
 
Console.WriteLine($"\nDatabase query metrics (last 24 hours):");
foreach (var metric in queryMetrics)
{
    Console.WriteLine($"{metric.Timestamp:yyyy-MM-dd HH:mm:ss} - " +
                     $"Avg: {metric.Value:F2}ms, " +
                     $"Count: {metric.Count}, " +
                     $"Min: {metric.Min:F2}ms, " +
                     $"Max: {metric.Max:F2}ms");
}
 
// Example 4: Analyze trend for backup operations
var backupTrend = await statisticsService.AnalyzeTrendAsync(
    "BackupOperation",
    TimeSpan.FromDays(7)
);
 
Console.WriteLine($"\nBackup operation trend analysis (last 7 days):");
Console.WriteLine($"Metric: {backupTrend.MetricName}");
Console.WriteLine($"Data points: {backupTrend.DataPoints}");
Console.WriteLine($"Average size: {backupTrend.AverageValue:F2} MB");
Console.WriteLine($"Trend: {backupTrend.TrendDirection} (strength: {backupTrend.TrendStrength:F4})");
Console.WriteLine($"Volatility: {backupTrend.Volatility:F4}");
Console.WriteLine($"Value range: {backupTrend.MinValue:F2} - {backupTrend.MaxValue:F2} MB");
 
// Example 5: Monitor system health with multiple metrics
var healthMetrics = await statisticsService.GetStatisticsAsync(TimeSpan.FromMinutes(30));
var errorRateMetrics = await statisticsService.GetMetricsAsync("ErrorRate", TimeSpan.FromHours(1));
var trendAnalysis = await statisticsService.AnalyzeTrendAsync("DatabaseQuery", TimeSpan.FromHours(6));
 
Console.WriteLine($"\nSystem Health Report:");
Console.WriteLine($"Events in last 30 minutes: {healthMetrics.TotalEvents}");
Console.WriteLine($"Peak activity: {healthMetrics.PeakEventCount} events in one second");
Console.WriteLine($"Query performance trend: {trendAnalysis.TrendDirection}");
Console.WriteLine($"Current volatility: {trendAnalysis.Volatility:F4}");
```
 
## PerformanceMonitor
 
The `PerformanceMonitor` class provides comprehensive performance tracking and monitoring capabilities for multi-tenant SQLite systems. It tracks operation execution times, records metrics by tenant, identifies slow operations, and provides health summaries. This is essential for performance optimization, capacity planning, and troubleshooting performance issues in multi-tenant environments.
 
### Public Members
 
```csharp
public sealed class PerformanceMonitor
public PerformanceMonitor
public PerformanceTracker StartOperation
public void RecordMetric
public OperationStatistics GetOperationStats
public Dictionary<string, OperationStatistics> GetAllStatistics
public Dictionary<string, List<PerformanceMetric>> GetTenantMetrics
public List<PerformanceMetric> GetSlowOperations
public SystemHealthSummary GetHealthSummary
public void ClearMetrics
 
public sealed class PerformanceTracker : IDisposable
public PerformanceTracker
public void Dispose
public void RecordException
 
public sealed class PerformanceMetric
public string OperationName
public long ElapsedMilliseconds
public string TenantId
public DateTime Timestamp
public bool IsSuccess
```
 
### Usage Example
 
```csharp
using SqliteMultiTenant.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
 
// Setup dependency injection
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<PerformanceMonitor>();
 
// Register performance monitor
services.AddSingleton<PerformanceMonitor>();
var serviceProvider = services.BuildServiceProvider();
var performanceMonitor = serviceProvider.GetRequiredService<PerformanceMonitor>();
 
// Example 1: Track database operation performance
var tracker = performanceMonitor.StartOperation("DatabaseQuery", "acme-corp");
try
{
    // Simulate database operation
    await Task.Delay(150);
    tracker.RecordMetric(150, true);
}
catch (Exception ex)
{
    tracker.RecordException(ex);
    throw;
}
 
// Example 2: Get statistics for a specific operation
var operationStats = performanceMonitor.GetOperationStats("DatabaseQuery");
Console.WriteLine($"DatabaseQuery - Total: {operationStats.TotalCalls}, " +
                $"Average: {operationStats.AverageDuration}ms, " +
                $"Slowest: {operationStats.MaxDuration}ms, " +
                $"Success rate: {operationStats.SuccessRate:P}");
 
// Example 3: Get all operation statistics
var allStats = performanceMonitor.GetAllStatistics();
foreach (var kvp in allStats)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value.TotalCalls} calls, " +
                     $"Avg: {kvp.Value.AverageDuration}ms");
}
 
// Example 4: Get tenant-specific metrics
var tenantMetrics = performanceMonitor.GetTenantMetrics("acme-corp");
foreach (var metric in tenantMetrics)
{
    Console.WriteLine($"{metric.Timestamp:yyyy-MM-dd HH:mm:ss} - " +
                     $"{metric.OperationName}: {metric.ElapsedMilliseconds}ms " +
                     $"(Success: {metric.IsSuccess})");
}
 
// Example 5: Identify slow operations
var slowOperations = performanceMonitor.GetSlowOperations();
Console.WriteLine($"Found {slowOperations.Count} slow operations:");
foreach (var slowOp in slowOperations.Take(5))
{
    Console.WriteLine($"  {slowOp.OperationName} took {slowOp.ElapsedMilliseconds}ms " +
                     $"for tenant {slowOp.TenantId}");
}
 
// Example 6: Get system health summary
var healthSummary = performanceMonitor.GetHealthSummary();
Console.WriteLine($"System Health: {healthSummary.Status}");
Console.WriteLine($"Total operations: {healthSummary.TotalOperations}");
Console.WriteLine($"Average response time: {healthSummary.AverageResponseTime}ms");
Console.WriteLine($"Success rate: {healthSummary.SuccessRate:P}");
Console.WriteLine($"Slow operations (>500ms): {healthSummary.SlowOperationCount}");
 
// Example 7: Clear metrics for a fresh start
performanceMonitor.ClearMetrics();
```
 
## ReportGenerator
 
The `ReportGenerator` class provides a set of methods for generating comprehensive monitoring and diagnostic reports for multi-tenant SQLite systems. It creates health, performance, tenant usage, error, and capacity reports by aggregating data from system statistics and diagnostics, making it ideal for operational dashboards and troubleshooting scenarios.
 
### Public Members
 
```csharp
public sealed class ReportGenerator
public ReportGenerator(IStatisticsService statisticsService, IDiagnosticsService diagnosticsService)
public string GenerateHealthReport()
public string GeneratePerformanceReport()
public string GenerateTenantUsageReport()
public string GenerateErrorReport()
public string GenerateCapacityReport()
```
 
### Usage Example
 
```csharp
using SqliteMultiTenant.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
 
// Setup dependency injection
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ReportGenerator>();
 
// Register required services
services.AddSingleton<IStatisticsService, StatisticsService>();
services.AddSingleton<IDiagnosticsService, DiagnosticsService>();
services.AddSingleton<ReportGenerator>();
 
var serviceProvider = services.BuildServiceProvider();
var reportGenerator = serviceProvider.GetRequiredService<ReportGenerator>();
 
// Example 1: Generate health report
var healthReport = reportGenerator.GenerateHealthReport();
Console.WriteLine("=== System Health Report ===");
Console.WriteLine(healthReport);
 
// Example 2: Generate performance report
var performanceReport = reportGenerator.GeneratePerformanceReport();
Console.WriteLine("\n=== Performance Report ===");
Console.WriteLine(performanceReport);
 
// Example 3: Generate tenant usage report
var tenantUsageReport = reportGenerator.GenerateTenantUsageReport();
Console.WriteLine("\n=== Tenant Usage Report ===");
Console.WriteLine(tenantUsageReport);
 
// Example 4: Generate error report
var errorReport = reportGenerator.GenerateErrorReport();
Console.WriteLine("\n=== Error Report ===");
Console.WriteLine(errorReport);
 
// Example 5: Generate capacity report
var capacityReport = reportGenerator.GenerateCapacityReport();
Console.WriteLine("\n=== Capacity Report ===");
Console.WriteLine(capacityReport);
 
// Example 6: Generate all reports in sequence
Console.WriteLine("=== System Monitoring Dashboard ===");
Console.WriteLine(reportGenerator.GenerateHealthReport());
Console.WriteLine(reportGenerator.GeneratePerformanceReport());
Console.WriteLine(reportGenerator.GenerateTenantUsageReport());
Console.WriteLine(reportGenerator.GenerateErrorReport());
Console.WriteLine(reportGenerator.GenerateCapacityReport());
```
 
## TenantNameValidator
 
The `TenantNameValidator` class provides static methods for validating tenant IDs and names, ensuring they comply with system naming conventions, length restrictions, and security policies to prevent issues like SQL injection. It also includes utility methods for generating valid tenant IDs from tenant names and validating database identifiers, offering a robust way to enforce tenant naming standards across the application.
 
### Public Members
 
```csharp
public static ValidationResult ValidateTenantId(string tenantId)
public static ValidationResult ValidateTenantName(string tenantName)
public static string GenerateTenantId(string tenantName)
public static bool IsValidDatabaseIdentifier(string identifier)
 
public sealed class ValidationResult
{
    public bool IsValid { get; set; }
    public string Error { get; set; }
}
```
 
### Usage Example
 
```csharp
using SqliteMultiTenant.Utilities;
using System;
 
// Example 1: Validate a tenant ID
var idResult = TenantNameValidator.ValidateTenantId("acme-corp");
if (idResult.IsValid)
{
    Console.WriteLine("Tenant ID is valid.");
}
else
{
    Console.WriteLine($"Invalid Tenant ID: {idResult.Error}");
}
 
// Example 2: Validate a tenant name
var nameResult = TenantNameValidator.ValidateTenantName("Acme Corporation");
if (nameResult.IsValid)
{
    Console.WriteLine("Tenant name is valid.");
}
else
{
    Console.WriteLine($"Invalid Tenant name: {nameResult.Error}");
}
 
// Example 3: Generate a tenant ID
string tenantName = "Acme Corp";
string generatedId = TenantNameValidator.GenerateTenantId(tenantName);
Console.WriteLine($"Generated ID for '{tenantName}': {generatedId}");
 
// Example 4: Check database identifier validity
string identifier = "acme_corp_db";
bool isValidDbId = TenantNameValidator.IsValidDatabaseIdentifier(identifier);
Console.WriteLine($"Is '{identifier}' a valid DB identifier? {isValidDbId}");
```
 
## IAuditLogger 

`IAuditLogger` provides a centralized, asynchronous audit logging facility for recording system‑wide actions such as user operations, configuration changes, and other critical events. It stores entries in memory, supports filtered queries, entry counting, retention‑based purging, and exposes basic statistics about the logged data.

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Monitoring;

// Create a logger (e.g., console logger)
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<AuditLogger>();

// Instantiate the audit logger
var auditLogger = new AuditLogger(logger);

// Log an audit entry
await auditLogger.LogAsync(new AuditLogEntry
{
    EventType = "UserLogin",
    Actor = "john.doe",
    ResourceId = "user-123",
    ResourceType = "User",
    Description = "User logged in successfully",
    Action = AuditAction.Read,
    IpAddress = "192.168.1.10"
});

// Retrieve recent login entries (limit to 5)
var recentLogins = await auditLogger.GetEntriesAsync(new AuditLogFilter
{
    EventType = "UserLogin",
    Limit = 5
});

// Count entries for a specific actor
int loginCount = await auditLogger.GetEntryCountAsync(new AuditLogFilter
{
    Actor = "john.doe"
});
Console.WriteLine($"Login events for john.doe: {loginCount}");

// Purge entries older than 30 days
await auditLogger.PurgeOldEntriesAsync(TimeSpan.FromDays(30));

// Get audit log statistics
var stats = await auditLogger.GetStatisticsAsync();
Console.WriteLine($"Total audit entries: {stats.TotalEntries}");
Console.WriteLine($"Unique actors: {stats.UniqueActors}");
Console.WriteLine($"Unique event types: {stats.UniqueEventTypes}");
```

## PathUtilities

The `PathUtilities` class provides robust, cross-platform file system and path manipulation utilities. It includes methods for safely combining and normalizing paths, managing directories (creating, deleting, checking size), performing recursive file operations, and handling file system cleanup tasks securely.

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using System;
using System.IO;

// 1. Safely combine paths (prevents traversal)
string basePath = "/app/data";
string relativePath = "tenants/acme-corp/db.sqlite";
string safePath = PathUtilities.SafeCombinePath(basePath, relativePath);
Console.WriteLine($"Safe path: {safePath}");

// 2. Create directory securely
string tenantDir = Path.Combine(basePath, "tenants/acme-corp");
bool created = PathUtilities.SafeCreateDirectory(tenantDir);

// 3. Get and format directory size
long size = PathUtilities.GetDirectorySizeBytes(tenantDir);
Console.WriteLine($"Directory size: {PathUtilities.FormatBytes(size)}");

// 4. Recursive file discovery
var files = PathUtilities.GetFilesRecursive(tenantDir, "*.sqlite");

// 5. Cleanup old files
int deletedCount = PathUtilities.CleanupOldFiles(tenantDir, TimeSpan.FromDays(30));
Console.WriteLine($"Cleaned up {deletedCount} old files.");

// 6. Path manipulation
string normalized = PathUtilities.NormalizePath("some\\path/to/file.db");
string ext = PathUtilities.GetExtensionWithoutDot("file.db");
bool isEmpty = PathUtilities.IsDirectoryEmpty(tenantDir);
```

## StringExtensions

`StringExtensions` provides a set of extension methods for string manipulation, focused on validation, transformation, and sanitization for database and tenant-specific contexts. These methods are designed to handle null or empty inputs gracefully, ensuring safe operations for identifiers, file paths, and JSON content.
### Public Members
 
```csharp
public static string ToSafeDatabaseIdentifier
public static string SafeTruncate
public static bool IsValidTenantIdentifier
public static T ToEnum<T>
public static string EscapeForJson
public static bool ContainsForbiddenCharacters
public static string NormalizeWhitespace
public static bool IsValidFilePath
public static string Reverse
```
 
### Usage Example
 
```csharp
using SqliteMultiTenant.Utilities;
using System;
 
// Example 1: Sanitize strings for database identifiers
string tenantName = "Acme Corp!";
string dbId = tenantName.ToSafeDatabaseIdentifier();
Console.WriteLine(dbId); // Outputs: acme_corp
 
// Example 2: Safely truncate strings for UI
string description = "This is a very long tenant description that needs truncation.";
string truncated = description.SafeTruncate(20);
Console.WriteLine(truncated); // Outputs: This is a very l...
 
// Example 3: Validate tenant identifiers
string tenantId = "acme-corp-123";
bool isValid = tenantId.IsValidTenantIdentifier();
Console.WriteLine(isValid); // Outputs: True
 
// Example 4: Convert to enum with safe fallback
string status = "Active";
var tenantStatus = status.ToEnum(TenantStatus.Inactive);
Console.WriteLine(tenantStatus); // Outputs: Active
 
// Example 5: Escape strings for JSON serialization
string jsonContent = "Line 1\nLine 2";
string escaped = jsonContent.EscapeForJson();
Console.WriteLine(escaped); // Outputs: Line 1\nLine 2
 
// Example 6: Check for forbidden characters in SQL scripts
string sqlScript = "DROP TABLE users;";
bool hasForbidden = sqlScript.ContainsForbiddenCharacters(new[] { "DROP", "DELETE" });
Console.WriteLine(hasForbidden); // Outputs: True
 
// Example 7: Normalize whitespace
string messy = "  too    much   whitespace  ";
string normalized = messy.NormalizeWhitespace();
Console.WriteLine(normalized); // Outputs: too much whitespace
 
// Example 8: Validate file paths
string path = "data/tenants/acme.db";
bool isPathValid = path.IsValidFilePath();
Console.WriteLine(isPathValid); // Outputs: True
 
// Example 9: Reverse a string
string original = "hello";
string reversed = original.Reverse();
Console.WriteLine(reversed); // Outputs: olleh

public enum TenantStatus { Inactive, Active, Suspended }
```
 
## CollectionExtensions

`CollectionExtensions` provides a collection of extension methods for common collection operations such as safe element access, filtering, batching, and functional-style transformations. These methods avoid LINQ performance pitfalls while providing more readable alternatives for common scenarios.

### Public Members

```csharp
public static T SafeGet<T>
public static T SafeFirst<T>
public static T SafeLast<T>
public static List<List<T>> ChunkBy<T>
public static IEnumerable<T> WhereNotNull<T>
public static void ForEach<T>
public static void ForEachWithIndex<T>
public static IEnumerable<T> DistinctBy<T, TKey>
public static bool HasElements<T>
public static List<T> Shuffle<T>
public static Dictionary<DateTime, List<T>> GroupByDate<T>
public static List<T> ToListSafe<T>
public static bool HasDuplicates<T>
public static IEnumerable<T> IntersectBy<T, TKey>
```

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        // Sample data
        var tenants = new List<Tenant>
        {
            new Tenant("acme-corp", "Acme Corp", DateTime.Now.AddDays(-5)),
            new Tenant("globex", "Globex Inc", DateTime.Now.AddDays(-3)),
            new Tenant("initech", "Initech", DateTime.Now.AddDays(-10)),
            new Tenant("umbrella", "Umbrella Corp", DateTime.Now.AddDays(-1))
        };

        // 1. Safe element access with default value
        var firstTenant = tenants.SafeGet(0, new Tenant("default", "Default Tenant", DateTime.MinValue));
        Console.WriteLine($"First tenant: {firstTenant.Name}");

        // 2. Safely get first/last elements
        var firstSafe = tenants.SafeFirst();
        var lastSafe = tenants.SafeLast();
        Console.WriteLine($"First: {firstSafe?.Name}, Last: {lastSafe?.Name}");

        // 3. Chunk collection for batch processing
        var tenantChunks = tenants.ChunkBy(2);
        Console.WriteLine($"Created {tenantChunks.Count} chunks");
        foreach (var chunk in tenantChunks)
        {
            Console.WriteLine($"Chunk has {chunk.Count} tenants");
        }

        // 4. Filter out null elements
        var tenantsWithNulls = new List<Tenant?> { tenants[0], null, tenants[1], null, tenants[2] };
        var validTenants = tenantsWithNulls.WhereNotNull();
        Console.WriteLine($"Filtered out {tenantsWithNulls.Count - validTenants.Count()} nulls");

        // 5. Execute action for each element
        tenants.ForEach(t => Console.WriteLine($"Processing: {t.Name}"));

        // 6. Execute action with index
        tenants.ForEachWithIndex((tenant, index) => 
            Console.WriteLine($"Tenant {index}: {tenant.Name}"));

        // 7. Get distinct elements by key
        var tenantsWithDuplicates = new List<Tenant>
        {
            new Tenant("acme-corp", "Acme Corp", DateTime.Now),
            new Tenant("acme-corp", "Acme Corp Duplicate", DateTime.Now),
            new Tenant("globex", "Globex Inc", DateTime.Now)
        };
        var uniqueTenants = tenantsWithDuplicates.DistinctBy(t => t.Id);
        Console.WriteLine($"Unique tenants: {uniqueTenants.Count()}");

        // 8. Check if collection has elements
        bool hasTenants = tenants.HasElements();
        Console.WriteLine($"Has tenants: {hasTenants}");

        // 9. Shuffle collection randomly
        var shuffledTenants = tenants.Shuffle();
        Console.WriteLine($"Shuffled first tenant: {shuffledTenants[0].Name}");

        // 10. Group items by date
        var backupDates = new List<Backup>
        {
            new Backup("acme-corp", DateTime.Now.Date),
            new Backup("globex", DateTime.Now.Date.AddDays(-1)),
            new Backup("acme-corp", DateTime.Now.Date.AddDays(-1))
        };
        var backupsByDate = backupDates.GroupByDate(b => b.Date);
        foreach (var kvp in backupsByDate)
        {
            Console.WriteLine($"Date {kvp.Key:yyyy-MM-dd}: {kvp.Value.Count} backups");
        }

        // 11. Safely convert to list
        IEnumerable<Tenant>? nullTenants = null;
        var safeList = nullTenants.ToListSafe();
        Console.WriteLine($"Safe list count: {safeList.Count}");

        // 12. Check for duplicates
        bool hasDuplicates = tenantsWithDuplicates.HasDuplicates();
        Console.WriteLine($"Has duplicates: {hasDuplicates}");

        // 13. Intersect collections by key
        var activeTenantIds = new List<string> { "acme-corp", "globex" };
        var activeTenants = tenants.IntersectBy(activeTenantIds, t => t.Id);
        Console.WriteLine($"Active tenants: {activeTenants.Count()}");
    }
}

class Tenant
{
    public string Id { get; }
    public string Name { get; }
    public DateTime CreatedDate { get; }

    public Tenant(string id, string name, DateTime createdDate)
    {
        Id = id;
        Name = name;
        CreatedDate = createdDate;
    }
}

class Backup
{
    public string TenantId { get; }
    public DateTime Date { get; }

    public Backup(string tenantId, DateTime date)
    {
        TenantId = tenantId;
        Date = date;
    }
}

## StringUtilities

The `StringUtilities` class provides a collection of extension methods for common string operations such as hashing, truncation, case conversion, sanitization, and validation.

### Public Members

```csharp
public static string ComputeSha256Hash
public static string ComputeMd5Hash
public static string TruncateWithEllipsis
public static string ToTitleCase
public static string ToSnakeCase
public static string ToCamelCase
public static string RemoveWhitespace
public static string SanitizeForFilePath
public static string SanitizeForHtml
public static bool IsValidEmail
public static bool IsValidUrl
public static bool IsValidGuid
public static string GenerateRandomString
public static string Repeat
public static IEnumerable<string> SplitPreservingQuotes
public static double GetStringSimilarity
```

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;

// Example 1: Compute SHA256 hash of a string
var hash = StringUtilities.ComputeSha256Hash("Hello, World!");
Console.WriteLine(hash);

// Example 2: Truncate a string with ellipsis
var truncated = StringUtilities.TruncateWithEllipsis("This is a very long string", 10);
Console.WriteLine(truncated);

// Example 3: Convert to title case
var titleCase = StringUtilities.ToTitleCase("hello world");
Console.WriteLine(titleCase);

// Example 4: Convert to snake_case
var snakeCase = StringUtilities.ToSnakeCase("HelloWorld");
Console.WriteLine(snakeCase);

// Example 5: Convert to camelCase
var camelCase = StringUtilities.ToCamelCase("hello_world");
Console.WriteLine(camelCase);

// Example 6: Remove whitespace from a string
var noWhitespace = StringUtilities.RemoveWhitespace("   Hello   World  ");
Console.WriteLine(noWhitespace);

// Example 7: Sanitize a string for file path
var sanitizedPath = StringUtilities.SanitizeForFilePath("Hello World!");
Console.WriteLine(sanitizedPath);

// Example 8: Sanitize a string for HTML output
var sanitizedHtml = StringUtilities.SanitizeForHtml("<script>alert('XSS')</script>");
Console.WriteLine(sanitizedHtml);

// Example 9: Validate an email address
var isValidEmail = StringUtilities.IsValidEmail("user@example.com");
Console.WriteLine(isValidEmail);

// Example 10: Validate a URL
var isValidUrl = StringUtilities.IsValidUrl("https://example.com");
Console.WriteLine(isValidUrl);

// Example 11: Validate a GUID
var isValidGuid = StringUtilities.IsValidGuid("01234567-89ab-cdef-0123-456789abcdef");
Console.WriteLine(isValidGuid);

// Example 12: Generate a random string
var randomString = StringUtilities.GenerateRandomString(10);
Console.WriteLine(randomString);

// Example 13: Repeat a string
var repeatedString = StringUtilities.Repeat("Hello", 3);
Console.WriteLine(repeatedString);

// Example 14: Split a string while preserving quotes
var splitString = StringUtilities.SplitPreservingQuotes("Hello, 'World'!");
Console.WriteLine(string.Join(" ", splitString));

// Example 15: Get the similarity ratio between two strings
var similarity = StringUtilities.GetStringSimilarity("Hello", "World");
Console.WriteLine(similarity);
```

## AsyncResourcePool

The `AsyncResourcePool<T>` class provides a generic, asynchronous resource pooling mechanism for managing expensive resource creation and reuse. It's particularly useful for pooling database connections, HTTP clients, file handles, and other disposable resources that benefit from reuse rather than frequent creation and disposal. The pool maintains a configurable maximum size, reuses available resources when possible, and creates new ones only when necessary, with automatic cleanup and statistics tracking.

### Public Members

```csharp
public sealed class AsyncResourcePool<T> : IDisposable where T : class
public AsyncResourcePool(Func<Task<T>> resourceFactory, Func<T, Task> resourceDisposer, ILogger<AsyncResourcePool<T>> logger, int maxPoolSize = 10)
public async Task<PooledResource<T>> AcquireAsync(CancellationToken cancellationToken = default)
public PoolStatistics GetStatistics()
public async Task ClearAsync()
public void Dispose()

public sealed class PooledResource<T> : IAsyncDisposable, IDisposable where T : class
public PooledResource(T resource, Func<T, Task> onDispose)
public T Resource { get; }
public async ValueTask DisposeAsync()
public void Dispose()

public sealed class PoolStatistics
public int AvailableResources { get; }
public int TotalCreated { get; }
public int WaitingRequests { get; }
public int MaxPoolSize { get; }
```

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

// Setup logging
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<AsyncResourcePool<DatabaseConnection>>();

// Create a resource pool for database connections
var pool = new AsyncResourcePool<DatabaseConnection>(
    resourceFactory: async () => await DatabaseConnection.CreateAsync("server=localhost;database=test"),
    resourceDisposer: async conn => await conn.DisposeAsync(),
    logger: logger,
    maxPoolSize: 5
);

// Example 1: Acquire and use a pooled resource
var resource1 = await pool.AcquireAsync();
try
{
    // Use the resource
    var data = await resource1.Resource.QueryAsync("SELECT * FROM users");
    Console.WriteLine($"Retrieved {data.Count} users");
}
finally
{
    // Return the resource to the pool
    await resource1.DisposeAsync();
}

// Example 2: Use using statement for automatic disposal
await using (var resource2 = await pool.AcquireAsync())
{
    var result = await resource2.Resource.ExecuteAsync("UPDATE users SET last_login = @date", new { date = DateTime.UtcNow });
    Console.WriteLine($"Updated {result} rows");
}

// Example 3: Get pool statistics
var stats = pool.GetStatistics();
Console.WriteLine($"Pool Stats - Available: {stats.AvailableResources}, " +
                $"Total Created: {stats.TotalCreated}, " +
                $"Waiting: {stats.WaitingRequests}, " +
                $"Max Size: {stats.MaxPoolSize}");

// Example 4: Clear the pool (dispose all resources)
await pool.ClearAsync();

// Example 5: Multiple concurrent operations with resource pooling
var tasks = new List<Task>();
for (int i = 0; i < 10; i++)
{
    tasks.Add(Task.Run(async () =>
    {
        await using var resource = await pool.AcquireAsync();
        await resource.Resource.QueryAsync("SELECT 1");
        await Task.Delay(100); // Simulate work
    }));
}
await Task.WhenAll(tasks);

// Dispose the pool when done
pool.Dispose();

// Example classes for demonstration
public class DatabaseConnection : IAsyncDisposable
{
    private readonly string _connectionString;
    
    public static async Task<DatabaseConnection> CreateAsync(string connectionString)
    {
        await Task.Delay(50); // Simulate connection delay
        return new DatabaseConnection(connectionString);
    }
    
    public DatabaseConnection(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<int> QueryAsync(string sql, object? parameters = null)
    {
        await Task.Delay(25); // Simulate query
        return 42; // Mock result
    }
    
    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        await Task.Delay(30); // Simulate execution
        return 1; // Mock result
    }
    
    public async ValueTask DisposeAsync()
    {
        await Task.Delay(10); // Simulate cleanup
    }
}
```

## TimeUtilities

The `TimeUtilities` class provides a robust set of static methods for advanced datetime manipulation, date arithmetic, and time span formatting. It simplifies common operations such as rounding dates, calculating period ranges, handling business days, and converting between UTC and Unix timestamps.

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using System;

// 1. Formatting
string span = TimeUtilities.FormatTimeSpan(TimeSpan.FromMinutes(90)); // "01:30:00"
string relative = TimeUtilities.FormatRelativeTime(DateTime.UtcNow.AddMinutes(-5)); // e.g., "5 minutes ago"

// 2. Date Arithmetic
DateTime today = DateTime.UtcNow;
DateTime startOfToday = TimeUtilities.GetStartOfDay(today);
DateTime endOfMonth = TimeUtilities.GetEndOfMonth(today);
DateTime rounded = TimeUtilities.RoundToNearest(today, TimeSpan.FromMinutes(15));

// 3. Business Logic
bool isLeap = TimeUtilities.IsLeapYear(2024); // True
int daysInFeb = TimeUtilities.GetDaysInMonth(2024, 2); // 29
DateTime nextBusinessDay = TimeUtilities.AddBusinessDays(today, 1);
bool isWorking = TimeUtilities.IsBusinessHours(today);

// 4. Period Ranges
var (start, end) = TimeUtilities.GetPeriodRange(today, "month");

// 5. Unix Timestamps
long unix = TimeUtilities.ToUnixTimestamp(today);
DateTime fromUnix = TimeUtilities.FromUnixTimestamp(unix);
```

## DateTimeExtensions
 
`DateTimeExtensions` provides specialized extension methods for `DateTime` to handle common operations within backup, retention, and scheduling workflows. These methods ensure consistent UTC-based calculations and include utilities for formatting, expiration checks, and range normalizations.

### Public Members

```csharp
public static bool IsExpired
public static int GetAgeDays
public static string ToIso8601String
public static bool IsWithinRetentionWindow
public static DateTime GetNextScheduledTime
public static string ToHumanReadableDuration
public static bool IsCreatedToday
public static DateTime StartOfDayUtc
public static DateTime EndOfDayUtc
public static DateTime RoundDownToMinute
```

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using System;

var now = DateTime.UtcNow;

// Example 1: Expiration and retention checks
var backupDate = now.AddDays(-35);
bool isExpired = backupDate.IsExpired(); // Throws if in future
int ageDays = backupDate.GetAgeDays();
bool withinWindow = backupDate.IsWithinRetentionWindow(30);

// Example 2: Scheduling
DateTime baseTime = now.AddHours(-1);
DateTime nextRun = baseTime.GetNextScheduledTime(15); // Next 15m interval

// Example 3: Formatting and Range Normalization
string isoString = now.ToIso8601String();
DateTime start = now.StartOfDayUtc();
DateTime end = now.EndOfDayUtc();
DateTime rounded = now.RoundDownToMinute();

// Example 4: Duration formatting (extension on TimeSpan)
TimeSpan duration = TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(30));
string humanDuration = duration.ToHumanReadableDuration(); // "2h 30m"

Console.WriteLine($"Is expired: {isExpired}");
Console.WriteLine($"Age in days: {ageDays}");
Console.WriteLine($"Next scheduled: {nextRun}");
Console.WriteLine($"Start of day: {start}");
Console.WriteLine($"Rounded: {rounded}");
Console.WriteLine($"Human duration: {humanDuration}");
```

## OperationRetryPolicy

`OperationRetryPolicy` provides a robust mechanism for executing operations with automatic retry logic. It supports configurable retry attempts, exponential backoff with jitter, and customizable logging. This is particularly useful for transient operations such as database connections, network calls, or file operations where temporary failures may resolve on subsequent attempts.

### Public Members

```csharp
public sealed class OperationRetryPolicy
public OperationRetryPolicy
public async Task<T> ExecuteAsync<T>
public async Task ExecuteAsync

public sealed class RetryPolicyBuilder
public RetryPolicyBuilder WithMaxRetries
public RetryPolicyBuilder WithInitialDelay
public RetryPolicyBuilder WithBackoffMultiplier
public RetryPolicyBuilder WithLogger
public OperationRetryPolicy Build
```

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Create a logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<OperationRetryPolicy>();

        // Example 1: Basic retry with default settings (3 retries, 100ms initial delay)
        var retryPolicy = new OperationRetryPolicy();
        
        int attemptCount = 0;
        var result = await retryPolicy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new InvalidOperationException("Temporary failure");
            }
            return "Success!";
        });
        
        Console.WriteLine($"Operation succeeded after {attemptCount} attempts: {result}");

        // Example 2: Configure retry policy with builder
        var customPolicy = new RetryPolicyBuilder()
            .WithMaxRetries(5)
            .WithInitialDelay(TimeSpan.FromMilliseconds(200))
            .WithBackoffMultiplier(2.0)
            .WithLogger(logger)
            .Build();
        
        // Execute a database operation with retry
        var dbResult = await customPolicy.ExecuteAsync(async () =>
        {
            // Simulate a transient database error
            if (DateTime.Now.Second % 3 == 0)
            {
                throw new TimeoutException("Database connection timeout");
            }
            return "Database operation completed";
        });
        
        Console.WriteLine(dbResult);

        // Example 3: Retry with specific return type
        var intResult = await customPolicy.ExecuteAsync<int>(async () =>
        {
            // Simulate a transient failure
            if (DateTime.Now.Millisecond % 5 == 0)
            {
                throw new TemporaryException("Network unavailable");
            }
            return 42;
        });
        
        Console.WriteLine($"Result: {intResult}");

        // Example 4: Retry with async operation
        var fileResult = await customPolicy.ExecuteAsync(async () =>
        {
            // Simulate file operation that might fail temporarily
            await Task.Delay(50);
            if (DateTime.Now.Second % 2 == 0)
            {
                throw new IOException("File lock detected");
            }
            return "File processed successfully";
        });
        
        Console.WriteLine(fileResult);
    }
}

public class TemporaryException : Exception
{
    public TemporaryException(string message) : base(message) { }
}
```

## FileSystemExtensions
 
`FileSystemExtensions` provides a collection of safe, utility‑style extension methods for common file‑system operations such as path validation, directory creation, size calculation, safe deletion, backup‑file naming, recursive file discovery, copying, and retrieving creation timestamps. All methods are designed to handle errors gracefully and return status values instead of throwing exceptions.
 
### Usage Example
 
```csharp
using System;
using System.Collections.Generic;
using SqliteMultiTenant.Utilities;

class Program
{
    static void Main()
    {
        string basePath = "/var/data/tenants";
        string tenantId = "acme-corp";

        // 1. Validate a file path
        string candidatePath = $"{basePath}/{tenantId}/db.sqlite";
        bool isSafe = candidatePath.IsSafeFilePath(basePath);
        Console.WriteLine($"Path safe: {isSafe}");

        // 2. Ensure the tenant directory exists
        string tenantDir = $"{basePath}/{tenantId}";
        tenantDir.EnsureDirectoryExists();

        // 3. Get file size (0 if missing)
        long size = candidatePath.GetFileSizeBytes();
        Console.WriteLine($"File size: {size} bytes");

        // 4. Safely delete a temporary file
        string tempFile = $"{tenantDir}/temp.tmp";
        tempFile.SafeDelete();

        // 5. Generate a backup file name
        string backupName = tenantId.GenerateBackupFileName();
        Console.WriteLine($"Backup file: {backupName}");

        // 6. List all .db files under the base path
        List<string> dbFiles = basePath.GetFilesWithExtension(".db");
        Console.WriteLine($".db files found: {dbFiles.Count}");

        // 7. Calculate total size of the tenant directory
        long dirSize = tenantDir.GetDirectorySizeBytes();
        Console.WriteLine($"Directory size: {dirSize} bytes");

        // 8. Copy a file safely
        string copyDest = $"{tenantDir}/copy.sqlite";
        bool copied = candidatePath.SafeCopyFile(copyDest, overwrite: true);
        Console.WriteLine($"File copied: {copied}");

        
## JsonHelper

The `JsonHelper` static class provides a centralized, robust set of methods for JSON serialization, deserialization, and manipulation within multi-tenant SQLite systems. It ensures consistent JSON handling with camelCase naming policies, enum support, and error handling, making it ideal for processing configurations, API payloads, and tenant-specific data structures.

### Public Members

```csharp
public static string Serialize<T>(T obj, bool indented = true)
public static T Deserialize<T>(string json)
public static dynamic DeserializeDynamic(string json)
public static string MergeJson(string json1, string json2)
public static T GetProperty<T>(string json, string propertyPath)
public static bool IsValidJson(string json)
public static T DeepClone<T>(T obj)
public static string PrettyPrint(string json)
public static string Minify(string json)
```

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using System;

// Example 1: Serialize and Deserialize
var config = new TenantConfig { Name = "Acme Corp", MaxDatabaseSize = 100 };
string json = JsonHelper.Serialize(config);
var deserialized = JsonHelper.Deserialize<TenantConfig>(json);
Console.WriteLine($"Deserialized Name: {deserialized.Name}");

// Example 2: Accessing JSON properties and dynamic data
string rawJson = "{\"settings\": {\"theme\": \"dark\"}}";
string theme = JsonHelper.GetProperty<string>(rawJson, "settings.theme");
Console.WriteLine($"Theme: {theme}");

// Example 3: Merging and formatting
string json1 = "{\"name\": \"Acme\"}";
string json2 = "{\"version\": \"1.0\"}";
string merged = JsonHelper.MergeJson(json1, json2);
string minified = JsonHelper.Minify(merged);
Console.WriteLine($"Minified JSON: {minified}");

public class TenantConfig
{
    public string Name { get; set; } = string.Empty;
    public int MaxDatabaseSize { get; set; }
}
```

```
## TenantContextHelper

The `TenantContextHelper` class provides a centralized mechanism for managing tenant-specific context across asynchronous operations in a multi-tenant environment. It allows setting, retrieving, and validating the current tenant context, and facilitates scoping operations to a specific tenant using `AsyncLocal` storage. Additionally, it helps in enriching diagnostic information and metadata with tenant-specific identifiers, ensuring consistent traceability.

### Public Members

```csharp
public sealed class TenantContextHelper
public TenantContextHelper(ILogger<TenantContextHelper> logger)
public void SetTenantContext(TenantContext context)
public TenantContext GetTenantContext()
public bool HasTenantContext()
public string GetCurrentTenantId()
public void ClearTenantContext()
public bool ValidateTenantContext(string expectedTenantId = null)
public IDisposable CreateScope(string tenantId, string userId = null)
public Dictionary<string, object> GetContextMetadata()
public string EnrichErrorWithContext(string errorMessage)
```

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using SqliteMultiTenant.Models;
using Microsoft.Extensions.Logging;
using System;

// Setup logger and helper
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<TenantContextHelper>();
var tenantHelper = new TenantContextHelper(logger);

// Example 1: Creating a scoped context for tenant-specific operations
using (tenantHelper.CreateScope("acme-corp", "user-123"))
{
    if (tenantHelper.HasTenantContext())
    {
        Console.WriteLine($"Current Tenant: {tenantHelper.GetCurrentTenantId()}");
        
        // Validate context
        if (tenantHelper.ValidateTenantContext("acme-corp"))
        {
            // Operation logic here...
        }
    }
} // Scope automatically clears when disposed

## RequestCorrelationIdGenerator
 
The `RequestCorrelationIdGenerator` class is a utility for managing correlation IDs in asynchronous operations, enabling end-to-end request tracing. It provides static methods to generate, set, retrieve, and scope correlation IDs within the current execution context, ensuring that operations spanning across multiple services or asynchronous boundaries remain identifiable and traceable.
 
### Public Members
 
```csharp
public sealed class RequestCorrelationIdGenerator
public static string GenerateCorrelationId
public static void SetCorrelationId
public static string GetCorrelationId
public static bool HasCorrelationId
public static List<string> GetCorrelationChain
public static void ClearCorrelationId
public static IDisposable CreateScope
public CorrelationIdScope
public void Dispose
```
 
### Usage Example
 
```csharp
using SqliteMultiTenant.Utilities;
using System;
 
// Example 1: Generate and set a correlation ID
string id = RequestCorrelationIdGenerator.GenerateCorrelationId();
RequestCorrelationIdGenerator.SetCorrelationId(id);
Console.WriteLine($"Current Correlation ID: {RequestCorrelationIdGenerator.GetCorrelationId()}");
 
// Example 2: Use in a scoped operation
using (RequestCorrelationIdGenerator.CreateScope("tenant123"))
{
    Console.WriteLine($"Scoped Correlation ID: {RequestCorrelationIdGenerator.GetCorrelationId()}");
    // Perform traced operations...
}
 
// Example 3: Verify state and chain
if (RequestCorrelationIdGenerator.HasCorrelationId())
{
    var chain = RequestCorrelationIdGenerator.GetCorrelationChain();
    Console.WriteLine($"Correlation chain length: {chain.Count}");
}
 
// Clear the correlation ID
RequestCorrelationIdGenerator.ClearCorrelationId();
```

