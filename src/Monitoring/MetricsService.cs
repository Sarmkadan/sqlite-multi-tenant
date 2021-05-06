// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace SqliteMultiTenant.Monitoring;

/// <summary>
/// Metrics collection service for monitoring system performance and behavior.
/// Tracks request counts, response times, error rates, and resource usage.
/// Useful for alerting and performance optimization.
/// </summary>
public interface IMetricsService
{
    void RecordRequest(string path, long durationMs, int statusCode);
    void RecordBackup(long sizeBytes, long durationMs, bool success);
    void RecordMigration(string version, long durationMs, bool success);
    void RecordError(string errorType, string message);
    MetricsSnapshot GetSnapshot();
}

/// <summary>
/// Snapshot of current system metrics.
/// </summary>
public class MetricsSnapshot
{
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public long TotalRequests { get; set; }
    public long TotalErrors { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public long TotalBackupBytes { get; set; }
    public int TotalBackups { get; set; }
    public int FailedBackups { get; set; }
    public int TotalMigrations { get; set; }
    public int FailedMigrations { get; set; }
    public Dictionary<string, int> ErrorCounts { get; set; } = new();
    public Dictionary<string, RequestMetrics> EndpointMetrics { get; set; } = new();
}

/// <summary>
/// Metrics for individual endpoint.
/// </summary>
public class RequestMetrics
{
    public string Endpoint { get; set; }
    public long RequestCount { get; set; }
    public long SuccessCount { get; set; }
    public long ErrorCount { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public long MaxResponseTimeMs { get; set; }
    public long MinResponseTimeMs { get; set; }
}

/// <summary>
/// In-memory metrics collection with thread-safe aggregation.
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private long _totalRequests;
    private long _totalErrors;
    private readonly List<long> _responseTimes = new();
    private long _totalBackupBytes;
    private int _totalBackups;
    private int _failedBackups;
    private int _totalMigrations;
    private int _failedMigrations;
    private readonly ConcurrentDictionary<string, int> _errorCounts = new();
    private readonly ConcurrentDictionary<string, RequestMetrics> _endpointMetrics = new();
    private readonly object _timeLock = new object();

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Records HTTP request metric.
    /// </summary>
    public void RecordRequest(string path, long durationMs, int statusCode)
    {
        Interlocked.Increment(ref _totalRequests);

        if (statusCode >= 400)
            Interlocked.Increment(ref _totalErrors);

        lock (_timeLock)
        {
            _responseTimes.Add(durationMs);
        }

        // Update endpoint-specific metrics
        _endpointMetrics.AddOrUpdate(path,
            new RequestMetrics
            {
                Endpoint = path,
                RequestCount = 1,
                SuccessCount = statusCode < 400 ? 1 : 0,
                ErrorCount = statusCode >= 400 ? 1 : 0,
                AverageResponseTimeMs = durationMs,
                MaxResponseTimeMs = durationMs,
                MinResponseTimeMs = durationMs
            },
            (key, existing) =>
            {
                existing.RequestCount++;
                if (statusCode < 400)
                    existing.SuccessCount++;
                else
                    existing.ErrorCount++;

                existing.AverageResponseTimeMs = (existing.AverageResponseTimeMs * (existing.RequestCount - 1) + durationMs) / existing.RequestCount;
                existing.MaxResponseTimeMs = Math.Max(existing.MaxResponseTimeMs, durationMs);
                existing.MinResponseTimeMs = Math.Min(existing.MinResponseTimeMs, durationMs);

                return existing;
            });
    }

    /// <summary>
    /// Records backup operation metric.
    /// </summary>
    public void RecordBackup(long sizeBytes, long durationMs, bool success)
    {
        Interlocked.Add(ref _totalBackupBytes, sizeBytes);
        Interlocked.Increment(ref _totalBackups);

        if (!success)
            Interlocked.Increment(ref _failedBackups);

        _logger.LogDebug("Backup recorded: {size}MB, {duration}ms, success: {success}",
            sizeBytes / 1_000_000, durationMs, success);
    }

    /// <summary>
    /// Records migration operation metric.
    /// </summary>
    public void RecordMigration(string version, long durationMs, bool success)
    {
        Interlocked.Increment(ref _totalMigrations);

        if (!success)
            Interlocked.Increment(ref _failedMigrations);

        _logger.LogDebug("Migration recorded: v{version}, {duration}ms, success: {success}",
            version, durationMs, success);
    }

    /// <summary>
    /// Records application error.
    /// </summary>
    public void RecordError(string errorType, string message)
    {
        _errorCounts.AddOrUpdate(errorType, 1, (key, count) => count + 1);

        _logger.LogWarning("Error recorded: {type} - {message}", errorType, message);
    }

    /// <summary>
    /// Gets current metrics snapshot.
    /// </summary>
    public MetricsSnapshot GetSnapshot()
    {
        double avgResponseTime = 0;
        lock (_timeLock)
        {
            if (_responseTimes.Count > 0)
                avgResponseTime = _responseTimes.Average();
        }

        return new MetricsSnapshot
        {
            TotalRequests = _totalRequests,
            TotalErrors = _totalErrors,
            AverageResponseTimeMs = avgResponseTime,
            TotalBackupBytes = _totalBackupBytes,
            TotalBackups = _totalBackups,
            FailedBackups = _failedBackups,
            TotalMigrations = _totalMigrations,
            FailedMigrations = _failedMigrations,
            ErrorCounts = new Dictionary<string, int>(_errorCounts),
            EndpointMetrics = new Dictionary<string, RequestMetrics>(_endpointMetrics)
        };
    }

    /// <summary>
    /// Resets all metrics (useful for testing or daily rollover).
    /// </summary>
    public void Reset()
    {
        _totalRequests = 0;
        _totalErrors = 0;
        _totalBackupBytes = 0;
        _totalBackups = 0;
        _failedBackups = 0;
        _totalMigrations = 0;
        _failedMigrations = 0;

        lock (_timeLock)
        {
            _responseTimes.Clear();
        }

        _errorCounts.Clear();
        _endpointMetrics.Clear();

        _logger.LogInformation("Metrics reset");
    }

    /// <summary>
    /// Exports metrics as formatted report.
    /// </summary>
    public string GetReport()
    {
        var snapshot = GetSnapshot();

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== System Metrics Report ===");
        report.AppendLine($"Captured At: {snapshot.CapturedAt:O}");
        report.AppendLine();

        report.AppendLine("HTTP Requests:");
        report.AppendLine($"  Total: {snapshot.TotalRequests}");
        report.AppendLine($"  Errors: {snapshot.TotalErrors}");
        report.AppendLine($"  Error Rate: {(snapshot.TotalRequests > 0 ? (double)snapshot.TotalErrors / snapshot.TotalRequests * 100 : 0):F2}%");
        report.AppendLine($"  Avg Response Time: {snapshot.AverageResponseTimeMs:F2}ms");
        report.AppendLine();

        report.AppendLine("Backups:");
        report.AppendLine($"  Total: {snapshot.TotalBackups}");
        report.AppendLine($"  Failed: {snapshot.FailedBackups}");
        report.AppendLine($"  Success Rate: {(snapshot.TotalBackups > 0 ? (double)(snapshot.TotalBackups - snapshot.FailedBackups) / snapshot.TotalBackups * 100 : 0):F2}%");
        report.AppendLine($"  Total Data Backed Up: {snapshot.TotalBackupBytes / 1_000_000_000:F2}GB");
        report.AppendLine();

        report.AppendLine("Migrations:");
        report.AppendLine($"  Total: {snapshot.TotalMigrations}");
        report.AppendLine($"  Failed: {snapshot.FailedMigrations}");
        report.AppendLine($"  Success Rate: {(snapshot.TotalMigrations > 0 ? (double)(snapshot.TotalMigrations - snapshot.FailedMigrations) / snapshot.TotalMigrations * 100 : 0):F2}%");

        return report.ToString();
    }
}
