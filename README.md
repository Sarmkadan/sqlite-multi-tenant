Console.WriteLine($"Uptime: {diagnostics?.Uptime.TotalHours:F2} hours");
}
```

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
