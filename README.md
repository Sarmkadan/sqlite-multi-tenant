Console.WriteLine($"Uptime: {diagnostics?.Uptime.TotalHours:F2} hours");
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

        // 9. Get creation time of the copied file
        DateTime createdUtc = copyDest.GetFileCreationTimeUtc();
        Console.WriteLine($"Copy created (UTC): {createdUtc:u}");
    }
}
```
```