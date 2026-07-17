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
 
## TenantValidator

The `TenantValidator` class provides comprehensive validation for tenant-related operations in multi-tenant SQLite systems. It includes validation methods for tenant creation, updates, name uniqueness checks, connection strings, backup tags, migrations, and retention policies. This validator ensures data integrity and prevents common issues like SQL injection, invalid naming conventions, and configuration errors.

### Public Members

```csharp
public sealed class TenantValidator
public Dictionary<string, string> ValidateCreateRequest(TenantCreateRequest request)
public Dictionary<string, string> ValidateUpdateRequest(TenantUpdateRequest request)
public Dictionary<string, string> ValidateNameUniqueness(string tenantName, string? existingTenantId = null)

public sealed class MigrationValidator
public Dictionary<string, string> ValidateMigrationRequest(MigrationRequest request)
public bool IsValidMigrationNaming(string migrationName)
public bool ContainsDangerousPatterns(string input)

public sealed class ConnectionStringValidator
public Dictionary<string, string> ValidateSqliteConnectionString(string connectionString)

public sealed class BackupValidator
public Dictionary<string, string> ValidateBackupTag(string tag)
public Dictionary<string, string> ValidateRetentionDays(int days)
```

### Usage Example


```csharp
using SqliteMultiTenant.Validation;
using System;
using System.Collections.Generic;

// Example 1: Validate tenant creation request
var validator = new TenantValidator();

var createRequest = new TenantCreateRequest
{
    Name = "Acme Corporation",
    ConnectionString = "Data Source=tenants/acme-corp/tenant.db;Version=3;Pooling=True;"
};

var createErrors = validator.ValidateCreateRequest(createRequest);
if (createErrors.Count == 0)
{
    Console.WriteLine("Tenant creation request is valid!");
}
else
{
    Console.WriteLine("Validation errors:");
    foreach (var error in createErrors)
    {
        Console.WriteLine($" - {error.Key}: {error.Value}");
    }
}

// Example 2: Validate tenant update request
var updateRequest = new TenantUpdateRequest
{
    Name = "Acme Corp Updated",
    MaxDatabaseSize = 1024
};

var updateErrors = validator.ValidateUpdateRequest(updateRequest);
Console.WriteLine(updateErrors.Count == 0 ? "Update is valid!" : "Update has errors");

// Example 3: Validate tenant name uniqueness
var nameErrors = validator.ValidateNameUniqueness("acme-corp", existingTenantId: null);
if (nameErrors.Count == 0)
{
    Console.WriteLine("Tenant name is unique!");
}

// Example 4: Validate database migration
var migrationValidator = new TenantValidator.MigrationValidator();
var migrationRequest = new MigrationRequest
{
    Name = "AddUserTable",
    Description = "Add users table for tenant management",
    Script = "CREATE TABLE Users (Id TEXT PRIMARY KEY, Name TEXT)"
};

var migrationErrors = migrationValidator.ValidateMigrationRequest(migrationRequest);
bool isNamingValid = migrationValidator.IsValidMigrationNaming("20240101_AddUserTable");
bool hasDangerousPatterns = migrationValidator.ContainsDangerousPatterns("DROP TABLE Users");

Console.WriteLine($"Migration valid: {migrationErrors.Count == 0}");
Console.WriteLine($"Naming valid: {isNamingValid}");
Console.WriteLine($"Has dangerous patterns: {hasDangerousPatterns}");

// Example 5: Validate SQLite connection string
var connectionStringValidator = new TenantValidator.ConnectionStringValidator();
var connStringErrors = connectionStringValidator.ValidateSqliteConnectionString(
    "Data Source=tenants/acme-corp/tenant.db;Version=3;Pooling=True;"
);
Console.WriteLine(connStringErrors.Count == 0 ? "Connection string is valid!" : "Invalid connection string");

// Example 6: Validate backup tag and retention days
var backupValidator = new TenantValidator.BackupValidator();
var backupErrors = backupValidator.ValidateBackupTag("daily-backup-2024-01-01");
var retentionErrors = backupValidator.ValidateRetentionDays(30);

Console.WriteLine(backupErrors.Count == 0 ? "Backup tag is valid!" : "Invalid backup tag");
Console.WriteLine(retentionErrors.Count == 0 ? "Retention days are valid!" : "Invalid retention days");

public class TenantCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public int MaxDatabaseSize { get; set; } = 512;
}

public class TenantUpdateRequest
{
    public string? Name { get; set; }
    public int? MaxDatabaseSize { get; set; }
}

public class MigrationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
}
```

## ValidationRuleBuilder

The `ValidationRuleBuilder<T>` class provides a fluent interface for constructing validation rules for tenant and entity properties. It allows chaining multiple validation rules (required, string length, email format, range checks, regex patterns, custom validators) and produces a comprehensive validation result with all errors. This builder pattern is particularly useful for validating tenant configurations, user inputs, and entity properties in a type-safe and readable way.

### Public Members

```csharp
public sealed class ValidationRuleBuilder<T>
public ValidationRuleBuilder
public ValidationRuleBuilder<T> Required
public ValidationRuleBuilder<T> StringLength
public ValidationRuleBuilder<T> Email
public ValidationRuleBuilder<T> Range
public ValidationRuleBuilder<T> Pattern
public ValidationRuleBuilder<T> Custom
public ValidationRuleBuilder<T> MustMatch
public RuleValidationResult Validate
public string FieldName
public Func<object, bool> Predicate
public string ErrorMessage

public sealed class RuleValidationResult
public bool IsValid
public List<RuleValidationError> Errors

public sealed class RuleValidationError
public string FieldName
public string Message
```

### Usage Example

```csharp
using SqliteMultiTenant.Validation;
using System;
using System.Collections.Generic;

// Example 1: Validate a tenant configuration
var validator = new ValidationRuleBuilder<TenantConfig>();

var config = new TenantConfig
{
    Name = "Acme Corporation",
    MaxConnections = 100,
    ConnectionString = "Data Source=tenants/acme-corp.db;Version=3;"
};

// Build validation rules
var result = validator
    .Field(c => c.Name)
    .Required("Tenant name is required")
    .StringLength(2, 50, "Name must be between 2 and 50 characters")
    .Email("Invalid email format")
    .Validate(config.Name);

if (!result.IsValid)
{
    Console.WriteLine("Name validation errors:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($" - {error.FieldName}: {error.Message}");
    }
}

// Example 2: Validate multiple fields with a single builder
var tenantValidator = new ValidationRuleBuilder<TenantConfig>()
    .Field(c => c.Name)
    .Required("Name is required")
    .StringLength(2, 50, "Name must be 2-50 chars")
    
    .Field(c => c.MaxConnections)
    .Required("Max connections is required")
    .Range(1, 1000, "Max connections must be between 1 and 1000");

var configResult = tenantValidator.Validate(config);
Console.WriteLine(configResult.IsValid ? "Configuration is valid!" : "Configuration has errors");

// Example 3: Validate connection string format
var connectionStringValidator = new ValidationRuleBuilder<TenantConfig>()
    .Field(c => c.ConnectionString)
    .Required("Connection string is required")
    .Pattern("^Data Source=.+\\.db;Version=\\[23\];", "Invalid SQLite connection string format");

var connResult = connectionStringValidator.Validate(config);
Console.WriteLine(connResult.IsValid ? "Connection string format is valid!" : "Invalid connection string");

// Example 4: Custom validation with predicate
var customValidator = new ValidationRuleBuilder<TenantConfig>()
    .Field(c => c.Name)
    .Required("Name is required")
    .Custom("Name cannot contain special characters", 
        name => !name.Any(ch => !char.IsLetterOrDigit(ch) && ch != ' ' && ch != '-'),
        "Name contains invalid characters");

var customResult = customValidator.Validate(config);
Console.WriteLine(customResult.IsValid ? "Name validation passed!" : "Name validation failed");

public class TenantConfig
{
    public string Name { get; set; } = string.Empty;
    public int MaxConnections { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
}
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

## IOutputFormatter

The `IOutputFormatter` interface defines a standardized contract for formatting objects into different output formats such as JSON, CSV, and XML. It enables consistent serialization for API responses, file exports, and CLI output, supporting multiple content types and providing a pluggable architecture for adding new formatters. This is particularly useful for multi-tenant systems where different output formats may be required for different clients or integration scenarios.

### Public Members

```csharp
public interface IOutputFormatter
public string Format<T>(T data)
public string ContentType { get; }

public sealed class JsonFormatter : IOutputFormatter
public string Format<T>(T data)
public string ContentType => "application/json"

public sealed class CsvFormatter : IOutputFormatter
public string Format<T>(T data)
public string ContentType => "text/csv"

public sealed class XmlFormatter : IOutputFormatter
public string Format<T>(T data)
public string ContentType => "application/xml"

public sealed class FormatterFactory
public FormatterFactory()
public IOutputFormatter GetFormatter(string type)
public IOutputFormatter GetFormatterByContentType(string contentType)

public sealed class OutputFormatter
public OutputFormatter()
public OutputFormatter(FormatterFactory formatterFactory)
public string FormatObject(object data, string format)
```

### Usage Example

```csharp
using SqliteMultiTenant.Formatters;
using System;
using System.Collections.Generic;

// Example 1: Use JSON formatter directly
var jsonFormatter = new JsonFormatter();
var tenant = new Tenant { Id = "acme-corp", Name = "Acme Corporation", CreatedDate = DateTime.UtcNow };
string jsonOutput = jsonFormatter.Format(tenant);
Console.WriteLine(jsonOutput);

// Example 2: Use CSV formatter for tabular data
var csvFormatter = new CsvFormatter();
var tenants = new List<Tenant>
{
    new Tenant { Id = "acme-corp", Name = "Acme Corporation", CreatedDate = DateTime.UtcNow },
    new Tenant { Id = "globex", Name = "Globex Inc", CreatedDate = DateTime.UtcNow.AddDays(-1) },
    new Tenant { Id = "initech", Name = "Initech", CreatedDate = DateTime.UtcNow.AddDays(-2) }
};
string csvOutput = csvFormatter.Format(tenants);
Console.WriteLine(csvOutput);

// Example 3: Use XML formatter for structured data
var xmlFormatter = new XmlFormatter();
string xmlOutput = xmlFormatter.Format(tenant);
Console.WriteLine(xmlOutput);

// Example 4: Use FormatterFactory to get appropriate formatter
var factory = new FormatterFactory();
var jsonFormatterFromFactory = factory.GetFormatter("json");
var csvFormatterFromFactory = factory.GetFormatter("csv");
var xmlFormatterFromFactory = factory.GetFormatter("xml");

// Example 5: Use OutputFormatter for flexible formatting
var outputFormatter = new OutputFormatter();
string textOutput = outputFormatter.FormatObject(tenant, "text");
string jsonOutput2 = outputFormatter.FormatObject(tenant, "json");
string csvOutput2 = outputFormatter.FormatObject(tenants, "csv");
string xmlOutput2 = outputFormatter.FormatObject(tenant, "xml");

Console.WriteLine("Text format:");
Console.WriteLine(textOutput);
Console.WriteLine("\nJSON format:");
Console.WriteLine(jsonOutput2);
Console.WriteLine("\nCSV format:");
Console.WriteLine(csvOutput2);
Console.WriteLine("\nXML format:");
Console.WriteLine(xmlOutput2);

public class Tenant
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
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


## DatabaseUtilities

The `DatabaseUtilities` class provides a comprehensive set of static methods for database administration, maintenance, and introspection in SQLite multi-tenant systems. It includes utilities for database sizing and formatting, configuration optimization, database compaction, query performance analysis, and schema introspection (checking for table/column existence and retrieving column metadata). These methods are essential for database maintenance tasks, monitoring, and automated administration workflows.

### Public Members

```csharp
public static async Task ConfigureOptimalSettingsAsync()
public static long GetDatabaseSize(string databasePath)
public static string GetDatabaseSizeFormatted(string databasePath)
public static async Task CompactDatabaseAsync(string databasePath)
public static async Task AnalyzeQueryPerformanceAsync(string databasePath)
public static async Task<DatabaseStatistics> GetDatabaseStatisticsAsync(string databasePath)
public static async Task<bool> TableExistsAsync(string databasePath, string tableName)
public static async Task<bool> ColumnExistsAsync(string databasePath, string tableName, string columnName)
public static async Task<List<ColumnInfo>> GetTableColumnsAsync(string databasePath, string tableName)

public sealed class DatabaseStatistics
{
    public long TableCount { get; set; }
    public long IndexCount { get; set; }
    public long PageCount { get; set; }
    public long PageSize { get; set; }
    public long EstimatedSize { get; set; }
}

public sealed class ColumnInfo
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool NotNull { get; set; }
    public string DefaultValue { get; set; }
}
```

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        string databasePath = "/var/data/tenants/acme-corp/tenant.db";
        
        // Example 1: Get database size and statistics
        long sizeBytes = DatabaseUtilities.GetDatabaseSize(databasePath);
        string formattedSize = DatabaseUtilities.GetDatabaseSizeFormatted(databasePath);
        
        Console.WriteLine($"Database size: {formattedSize}");
        
        var stats = await DatabaseUtilities.GetDatabaseStatisticsAsync(databasePath);
        Console.WriteLine($"Tables: {stats.TableCount}, Indexes: {stats.IndexCount}");
        Console.WriteLine($"Estimated size: {stats.EstimatedSize:N0} bytes");
        
        // Example 2: Configure optimal database settings
        await DatabaseUtilities.ConfigureOptimalSettingsAsync();
        Console.WriteLine("Database settings optimized");
        
        // Example 3: Compact database to reclaim space
        await DatabaseUtilities.CompactDatabaseAsync(databasePath);
        Console.WriteLine("Database compacted successfully");
        
        // Example 4: Analyze query performance
        await DatabaseUtilities.AnalyzeQueryPerformanceAsync(databasePath);
        Console.WriteLine("Query performance analysis completed");
        
        // Example 5: Check if table exists
        bool usersTableExists = await DatabaseUtilities.TableExistsAsync(databasePath, "Users");
        Console.WriteLine($"Users table exists: {usersTableExists}");
        
        // Example 6: Check if column exists
        bool emailColumnExists = await DatabaseUtilities.ColumnExistsAsync(databasePath, "Users", "Email");
        Console.WriteLine($"Email column exists: {emailColumnExists}");
        
        // Example 7: Get table column information
        var columns = await DatabaseUtilities.GetTableColumnsAsync(databasePath, "Users");
        Console.WriteLine($"Columns in Users table:");
        foreach (var column in columns)
        {
            Console.WriteLine($"  - {column.Name} ({column.Type})" + 
                           $"{(column.NotNull ? " NOT NULL" : "")}" +
                           $"{(column.DefaultValue != null ? $" DEFAULT {column.DefaultValue}" : "")}");
        }
    }
}

        
## ReflectionExtensions

The `ReflectionExtensions` class provides a comprehensive set of static utility methods for working with .NET reflection. It simplifies common reflection operations such as inspecting and manipulating object properties, checking type characteristics, creating instances, copying properties between objects, and working with collections. These methods are particularly useful for data mapping, serialization, dynamic object manipulation, and framework-level utilities where reflection is commonly used.

### Public Members

```csharp
public static PropertyInfo[] GetPublicProperties<T>()
public static object GetPropertyValue<T>(T obj, string propertyName)
public static bool SetPropertyValue<T>(T obj, string propertyName, object value)
public static bool IsCollection<T>(T obj)
public static Type GetCollectionElementType<T>(T obj)
public static bool IsNullable<T>(T obj)
public static Type GetUnderlyingType<T>(T obj)
public static bool IsScalarType<T>(T obj)
public static MethodInfo[] GetMethodsByName<T>(string methodName)
public static object CreateInstance<T>()
public static bool HasAttribute<T>(this MemberInfo member)
public static T GetAttribute<T>(this MemberInfo member) where T : Attribute
public static void CopyPropertiesTo<TSource, TTarget>(TSource source, TTarget target) where TTarget : class, new()
```

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;

// Example 1: Get public properties of a type
var properties = ReflectionExtensions.GetPublicProperties<Tenant>();
Console.WriteLine($"Tenant has {properties.Length} public properties:");
foreach (var prop in properties)
{
    Console.WriteLine($" - {prop.Name} ({prop.PropertyType.Name})");
}

// Example 2: Get and set property values dynamically
var tenant = new Tenant { Id = "acme-corp", Name = "Acme Corporation" };
var idValue = ReflectionExtensions.GetPropertyValue(tenant, "Id");
Console.WriteLine($"Tenant ID: {idValue}");

bool setSuccess = ReflectionExtensions.SetPropertyValue(tenant, "Name", "Acme Corp Updated");
Console.WriteLine($"Set property success: {setSuccess}");
Console.WriteLine($"Updated tenant name: {tenant.Name}");

// Example 3: Check type characteristics
var tenantList = new List<Tenant>();
bool isCollection = ReflectionExtensions.IsCollection(tenantList);
Console.WriteLine($"Is tenantList a collection? {isCollection}");

Type elementType = ReflectionExtensions.GetCollectionElementType(tenantList);
Console.WriteLine($"Collection element type: {elementType.Name}");

// Example 4: Type checking utilities
bool isNullable = ReflectionExtensions.IsNullable(tenant.Id);
Console.WriteLine($"Is string nullable? {isNullable}");

bool isScalar = ReflectionExtensions.IsScalarType(tenant.Id);
Console.WriteLine($"Is string a scalar type? {isScalar}");

// Example 5: Find methods by name
var methods = ReflectionExtensions.GetMethodsByName<Tenant>("ToString");
Console.WriteLine($"Found {methods.Length} ToString methods");

// Example 6: Create instances dynamically
var newTenant = (Tenant)ReflectionExtensions.CreateInstance<Tenant>();
Console.WriteLine($"Created new tenant with ID: {newTenant.Id}");

// Example 7: Check for attributes
var hasSerializable = typeof(Tenant).HasAttribute<SerializableAttribute>();
Console.WriteLine($"Tenant has Serializable attribute: {hasSerializable}");

// Example 8: Get attributes
var obsoleteAttr = typeof(Tenant).GetAttribute<ObsoleteAttribute>();
Console.WriteLine($"Tenant has Obsolete attribute: {obsoleteAttr != null}");

// Example 9: Copy properties between objects
var sourceTenant = new Tenant { Id = "globex", Name = "Globex Inc", CreatedDate = DateTime.UtcNow };
var targetTenant = new Tenant();
ReflectionExtensions.CopyPropertiesTo(sourceTenant, targetTenant);
Console.WriteLine($"Copied properties: {targetTenant.Name} (ID: {targetTenant.Id})");

public class Tenant
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
```

## EnumExtensions

The `EnumExtensions` class provides a comprehensive set of static utility methods for working with .NET enums. It simplifies common enum operations such as parsing enum values with safe fallback, retrieving enum attributes, checking for valid enum values, getting all enum values, and converting between enum values and their display names or descriptions. These methods are particularly useful for configuration parsing, data binding, and working with attribute-based metadata on enum values.

### Public Members

```csharp
public static string GetDisplayName<T>(T value) where T : Enum
public static T ParseSafe<T>(string value, T defaultValue = default) where T : Enum
public static bool HasAttribute<T, TAttribute>(T value) where TAttribute : Attribute
public static TAttribute GetAttribute<T, TAttribute>(T value) where TAttribute : Attribute
public static IEnumerable<T> GetAllValues<T>() where T : Enum
public static bool IsValidEnumValue<T>(T value) where T : Enum
public static string GetDescription<T>(T value) where T : Enum
```

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using System;
using System.ComponentModel;

// Example 1: Parse enum values with safe fallback
var logLevel = EnumExtensions.ParseSafe<LogLevel>("Warning", LogLevel.Info);
Console.WriteLine($"Parsed log level: {logLevel}");

// Example 2: Get all enum values
var allLevels = EnumExtensions.GetAllValues<LogLevel>();
Console.WriteLine("All log levels:");
foreach (var level in allLevels)
{
    Console.WriteLine($" - {level}");
}

// Example 3: Check if a value is valid for an enum
bool isValid = EnumExtensions.IsValidEnumValue(LogLevel.Debug);
Console.WriteLine($"Is Debug valid? {isValid}");

// Example 4: Get display name for an enum value
string displayName = EnumExtensions.GetDisplayName(LogLevel.Error);
Console.WriteLine($"Display name for Error: {displayName}");

// Example 5: Work with enum attributes
bool hasDescription = EnumExtensions.HasAttribute<LogLevel, DescriptionAttribute>(LogLevel.Warning);
Console.WriteLine($"Has Description attribute: {hasDescription}");

if (hasDescription)
{
    var descriptionAttr = EnumExtensions.GetAttribute<LogLevel, DescriptionAttribute>(LogLevel.Warning);
    Console.WriteLine($"Warning description: {descriptionAttr?.Description}");
}

// Example 6: Get description from enum value
string description = EnumExtensions.GetDescription(LogLevel.Info);
Console.WriteLine($"Info description: {description}");

public enum LogLevel
{
    [Description("Debug information")]
    Debug,
    
    [Description("Informational messages")]
    Info,
    
    [Description("Warning messages")]
    Warning,
    
    [Description("Error messages")]
    Error,
    
    [Description("Critical errors")]
    Critical
}
```

## ValidationExtensions

The `ValidationExtensions` class provides a set of extension methods for validating various types of user input and system configuration settings, such as emails, UUIDs, semantic versions, database names, and connection strings. These methods are designed to be used at system boundaries (e.g., in controllers or services) to enforce data integrity and prevent common security issues like SQL injection by validating input format and constraints before processing.

### Public Members

```csharp
public static bool IsValidEmail(this string email)
public static bool IsValidUuid(this string uuid)
public static bool IsValidSemanticVersion(this string version)
public static bool IsValidDatabaseName(this string name)
public static bool IsValidTenantName(this string name)
public static bool IsValidRelativePath(this string path)
public static bool IsValidSqlScript(this string script)
public static bool IsValidPort(this int port)
public static bool IsValidConnectionString(this string connectionString)
public static bool IsValidBackupTag(this string tag)
public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
public static bool IsValidRetentionDays(this int days)
public static bool IsValidConnectionTimeout(this int timeoutSeconds)
```

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using System;

// Example 1: Validating strings
string email = "test@example.com";
bool isEmailValid = email.IsValidEmail();
Console.WriteLine($"Email valid: {isEmailValid}");

string dbName = "tenant_db";
bool isDbNameValid = dbName.IsValidDatabaseName();
Console.WriteLine($"Database name valid: {isDbNameValid}");

// Example 2: Validating integers
int port = 5432;
bool isPortValid = port.IsValidPort();
Console.WriteLine($"Port valid: {isPortValid}");

int retentionDays = 30;
bool isRetentionValid = retentionDays.IsValidRetentionDays();
Console.WriteLine($"Retention days valid: {isRetentionValid}");

// Example 3: Validating collections
var tenants = new List<string> { "acme-corp", "globex" };
bool isNullOrEmpty = tenants.IsNullOrEmpty();
Console.WriteLine($"Collection null or empty: {isNullOrEmpty}");
```

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
## JsonExportFormatter

The `JsonExportFormatter` class provides a robust utility for serializing objects to JSON and deserializing JSON back into objects with configurable serialization options. It supports camelCase naming policies, null value handling, enum conversion, and custom JSON formatting through different serialization profiles. This formatter is particularly useful for API responses, configuration serialization, and data export scenarios in multi-tenant systems.

### Public Members

```csharp
public sealed class JsonExportFormatter
public JsonExportFormatter(ILogger<JsonExportFormatter> logger, bool prettyPrint = true)
public string Format<T>(T? data) where T : class
public T? Parse<T>(string json) where T : class
public string FormatWithOptions<T>(T? data, JsonSerializerOptions options) where T : class
public static JsonSerializerOptions GetMinimalOptions()
public static JsonSerializerOptions GetVerboseOptions()
```

### Usage Example

```csharp
using SqliteMultiTenant.Formatters;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

// Setup dependency injection
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<JsonExportFormatter>();

// Create formatter with default settings (pretty-printed JSON)
var formatter = new JsonExportFormatter(logger);

// Example 1: Format an object to JSON
var tenant = new Tenant
{
    Id = "acme-corp",
    Name = "Acme Corporation",
    CreatedDate = DateTime.UtcNow,
    IsActive = true,
    MaxConnections = 100
};

string jsonOutput = formatter.Format(tenant);
Console.WriteLine(jsonOutput);

// Example 2: Parse JSON back to an object
string jsonInput = "{\"id\":\"globex\",\"name\":\"Globex Inc\",\"createdDate\":\"2024-01-01T00:00:00\",\"isActive\":true,\"maxConnections\":50}";
var parsedTenant = formatter.Parse<Tenant>(jsonInput);
Console.WriteLine($"Parsed tenant: {parsedTenant?.Name} (ID: {parsedTenant?.Id})");

// Example 3: Format with minimal options (compact JSON)
var minimalOptions = JsonExportFormatter.GetMinimalOptions();
string compactJson = formatter.FormatWithOptions(tenant, minimalOptions);
Console.WriteLine(compactJson);

// Example 4: Format with verbose options (include all properties including nulls)
var verboseOptions = JsonExportFormatter.GetVerboseOptions();
string verboseJson = formatter.FormatWithOptions(tenant, verboseOptions);
Console.WriteLine(verboseJson);

// Example 5: Handle null values gracefully
string nullJson = formatter.Format<Tenant>(null);
Console.WriteLine(nullJson); // Outputs: null

// Example 6: Format collections
var tenants = new List<Tenant>
{
    new Tenant { Id = "acme-corp", Name = "Acme Corporation", CreatedDate = DateTime.UtcNow, IsActive = true },
    new Tenant { Id = "globex", Name = "Globex Inc", CreatedDate = DateTime.UtcNow.AddDays(-1), IsActive = false },
    new Tenant { Id = "initech", Name = "Initech", CreatedDate = DateTime.UtcNow.AddDays(-2), IsActive = true }
};

string collectionJson = formatter.Format(tenants);
Console.WriteLine(collectionJson);

public class Tenant
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
    public int? MaxConnections { get; set; }
}
```

## IHealthCheckService

The `IHealthCheckService` interface defines a standardized contract for performing health checks on multi-tenant SQLite systems. It provides methods to check system health, database connectivity, disk space availability, and retrieve detailed diagnostic information. This service is essential for monitoring system reliability, implementing health-based routing, and triggering automated recovery procedures when issues are detected.

### Public Members

```csharp
public interface IHealthCheckService
public Task<HealthCheckResponse> GetHealthStatusAsync()
public Task<bool> IsDatabaseHealthyAsync()
public Task<bool> IsDiskSpaceHealthyAsync()
public Task<bool> IsSystemHealthyAsync()
public async Task<string> GetDetailedStatusAsync()

public sealed class HealthCheckService : IHealthCheckService
public HealthCheckService(ILogger<HealthCheckService> logger, ISystemMonitor systemMonitor)
public async Task<HealthCheckResponse> GetHealthStatusAsync()
public Task<bool> IsDatabaseHealthyAsync()
public Task<bool> IsDiskSpaceHealthyAsync()
public Task<bool> IsSystemHealthyAsync()
public async Task<string> GetDetailedStatusAsync()

public sealed class DetailedHealthCheckService : HealthCheckService
public DetailedHealthCheckService(ILogger<DetailedHealthCheckService> logger, ISystemMonitor systemMonitor)
public Dictionary<string, object> GetDetailedDiagnostics()

public sealed class HealthCheckResponse
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
    public TimeSpan Uptime { get; set; }
    public long DatabaseSizeBytes { get; set; }
    public double FreeDiskSpacePercent { get; set; }
    public List<HealthCheckIssue> Issues { get; set; } = new List<HealthCheckIssue>();
}

public sealed class HealthCheckIssue
{
    public string Component { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}
```

### Usage Example

```csharp
using SqliteMultiTenant.Health;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

// Setup dependency injection
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<HealthCheckService>();

// Register health check service
services.AddSingleton<IHealthCheckService, HealthCheckService>();
var serviceProvider = services.BuildServiceProvider();
var healthCheckService = serviceProvider.GetRequiredService<IHealthCheckService>();

// Example 1: Check basic health status
var healthStatus = await healthCheckService.GetHealthStatusAsync();
Console.WriteLine($"System health: {(healthStatus.IsHealthy ? "HEALTHY" : "UNHEALTHY")}");
Console.WriteLine($"Status: {healthStatus.Status}");
Console.WriteLine($"Uptime: {healthStatus.Uptime.TotalHours:F2} hours");

// Example 2: Check specific components
bool isDatabaseHealthy = await healthCheckService.IsDatabaseHealthyAsync();
bool isDiskSpaceHealthy = await healthCheckService.IsDiskSpaceHealthyAsync();
bool isSystemHealthy = await healthCheckService.IsSystemHealthyAsync();

Console.WriteLine($"Database healthy: {isDatabaseHealthy}");
Console.WriteLine($"Disk space healthy: {isDiskSpaceHealthy}");
Console.WriteLine($"System healthy: {isSystemHealthy}");

// Example 3: Get detailed status information
string detailedStatus = await healthCheckService.GetDetailedStatusAsync();
Console.WriteLine("\nDetailed Status:");
Console.WriteLine(detailedStatus);

// Example 4: Use DetailedHealthCheckService for comprehensive diagnostics
services.AddSingleton<IHealthCheckService, DetailedHealthCheckService>();
var detailedHealthService = serviceProvider.GetRequiredService<IHealthCheckService>() as DetailedHealthCheckService;

if (detailedHealthService != null)
{
    var diagnostics = detailedHealthService.GetDetailedDiagnostics();
    Console.WriteLine("\nDetailed Diagnostics:");
    foreach (var kvp in diagnostics)
    {
        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
    }
}

// Example 5: Monitor health in a loop with alerts
while (true)
{
    var response = await healthCheckService.GetHealthStatusAsync();
    
    if (!response.IsHealthy)
    {
        Console.WriteLine($"[ALERT] Health check failed at {response.CheckedAt:yyyy-MM-dd HH:mm:ss}");
        foreach (var issue in response.Issues)
        {
            Console.WriteLine($"  [{issue.Severity}] {issue.Component}: {issue.Description}");
        }
    }
    
    await Task.Delay(TimeSpan.FromMinutes(5));
}
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

