#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using SqliteMultiTenant.Models;

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
public sealed class MetricsSnapshot {
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
public sealed class RequestMetrics {
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
public sealed class MetricsService : IMetricsService {
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

    /// <summary>
    /// Exports metrics in Prometheus text exposition format.
    /// Includes HELP and TYPE lines for each metric, with tenant_id labels where applicable.
    /// </summary>
    /// <param name="tenantContext">Optional tenant context to filter metrics by tenant</param>
    /// <returns>Prometheus exposition format metrics</returns>
    public string ToPrometheusExpositionFormat(TenantContext? tenantContext = null)
    {
        var snapshot = GetSnapshot();
        var builder = new System.Text.StringBuilder();

        // Global counters (no tenant label)
        builder.AppendLine("# HELP sqlite_multi_tenant_requests_total Total number of HTTP requests processed");
        builder.AppendLine("# TYPE sqlite_multi_tenant_requests_total counter");
        builder.AppendLine($"sqlite_multi_tenant_requests_total {snapshot.TotalRequests}");
        builder.AppendLine();

        builder.AppendLine("# HELP sqlite_multi_tenant_errors_total Total number of errors encountered");
        builder.AppendLine("# TYPE sqlite_multi_tenant_errors_total counter");
        builder.AppendLine($"sqlite_multi_tenant_errors_total {snapshot.TotalErrors}");
        builder.AppendLine();

        builder.AppendLine("# HELP sqlite_multi_tenant_error_rate Error rate as a percentage (0-100)");
        builder.AppendLine("# TYPE sqlite_multi_tenant_error_rate gauge");
        builder.AppendLine($"sqlite_multi_tenant_error_rate {(snapshot.TotalRequests > 0 ? (double)snapshot.TotalErrors / snapshot.TotalRequests * 100 : 0)}");
        builder.AppendLine();

        builder.AppendLine("# HELP sqlite_multi_tenant_request_duration_seconds Average request duration in seconds");
        builder.AppendLine("# TYPE sqlite_multi_tenant_request_duration_seconds gauge");
        builder.AppendLine($"sqlite_multi_tenant_request_duration_seconds {snapshot.AverageResponseTimeMs / 1000.0}");
        builder.AppendLine();

        builder.AppendLine("# HELP sqlite_multi_tenant_backups_total Total number of backup operations");
        builder.AppendLine("# TYPE sqlite_multi_tenant_backups_total counter");
        builder.AppendLine($"sqlite_multi_tenant_backups_total {snapshot.TotalBackups}");
        builder.AppendLine();

        builder.AppendLine("# HELP sqlite_multi_tenant_backups_failed_total Total number of failed backup operations");
        builder.AppendLine("# TYPE sqlite_multi_tenant_backups_failed_total counter");
        builder.AppendLine($"sqlite_multi_tenant_backups_failed_total {snapshot.FailedBackups}");
        builder.AppendLine();

        builder.AppendLine("# HELP sqlite_multi_tenant_backup_size_bytes Total bytes backed up across all operations");
        builder.AppendLine("# TYPE sqlite_multi_tenant_backup_size_bytes counter");
        builder.AppendLine($"sqlite_multi_tenant_backup_size_bytes {snapshot.TotalBackupBytes}");
        builder.AppendLine();

        builder.AppendLine("# HELP sqlite_multi_tenant_migrations_total Total number of database migrations performed");
        builder.AppendLine("# TYPE sqlite_multi_tenant_migrations_total counter");
        builder.AppendLine($"sqlite_multi_tenant_migrations_total {snapshot.TotalMigrations}");
        builder.AppendLine();

        builder.AppendLine("# HELP sqlite_multi_tenant_migrations_failed_total Total number of failed database migrations");
        builder.AppendLine("# TYPE sqlite_multi_tenant_migrations_failed_total counter");
        builder.AppendLine($"sqlite_multi_tenant_migrations_failed_total {snapshot.FailedMigrations}");
        builder.AppendLine();

        // Per-tenant metrics (with tenant_id label)
        if (tenantContext != null && !string.IsNullOrEmpty(tenantContext.TenantId))
        {
            var tenantId = tenantContext.TenantId;

            builder.AppendLine("# HELP sqlite_multi_tenant_tenant_requests_total Total requests for tenant");
            builder.AppendLine("# TYPE sqlite_multi_tenant_tenant_requests_total counter");
            builder.AppendLine($"sqlite_multi_tenant_tenant_requests_total{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\"}} {snapshot.TotalRequests}");
            builder.AppendLine();

            builder.AppendLine("# HELP sqlite_multi_tenant_tenant_errors_total Total errors for tenant");
            builder.AppendLine("# TYPE sqlite_multi_tenant_tenant_errors_total counter");
            builder.AppendLine($"sqlite_multi_tenant_tenant_errors_total{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\"}} {snapshot.TotalErrors}");
            builder.AppendLine();

            builder.AppendLine("# HELP sqlite_multi_tenant_tenant_error_rate Error rate for tenant as a percentage (0-100)");
            builder.AppendLine("# TYPE sqlite_multi_tenant_tenant_error_rate gauge");
            builder.AppendLine($"sqlite_multi_tenant_tenant_error_rate{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\"}} {(snapshot.TotalRequests > 0 ? (double)snapshot.TotalErrors / snapshot.TotalRequests * 100 : 0)}");
            builder.AppendLine();

            builder.AppendLine("# HELP sqlite_multi_tenant_tenant_request_duration_seconds Average request duration for tenant in seconds");
            builder.AppendLine("# TYPE sqlite_multi_tenant_tenant_request_duration_seconds gauge");
            builder.AppendLine($"sqlite_multi_tenant_tenant_request_duration_seconds{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\"}} {snapshot.AverageResponseTimeMs / 1000.0}");
            builder.AppendLine();
        }

        // Endpoint-specific metrics (with tenant_id and endpoint labels)
        foreach (var endpointMetric in snapshot.EndpointMetrics)
        {
            var endpoint = endpointMetric.Key;
            var metrics = endpointMetric.Value;
            var tenantId = tenantContext?.TenantId ?? "global";

            builder.AppendLine("# HELP sqlite_multi_tenant_endpoint_requests_total Total requests for endpoint");
            builder.AppendLine("# TYPE sqlite_multi_tenant_endpoint_requests_total counter");
            builder.AppendLine($"sqlite_multi_tenant_endpoint_requests_total{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\",endpoint=\"{EscapePrometheusLabel(endpoint)}\"}} {metrics.RequestCount}");
            builder.AppendLine();

            builder.AppendLine("# HELP sqlite_multi_tenant_endpoint_errors_total Total errors for endpoint");
            builder.AppendLine("# TYPE sqlite_multi_tenant_endpoint_errors_total counter");
            builder.AppendLine($"sqlite_multi_tenant_endpoint_errors_total{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\",endpoint=\"{EscapePrometheusLabel(endpoint)}\"}} {metrics.ErrorCount}");
            builder.AppendLine();

            builder.AppendLine("# HELP sqlite_multi_tenant_endpoint_request_duration_seconds Average request duration for endpoint in seconds");
            builder.AppendLine("# TYPE sqlite_multi_tenant_endpoint_request_duration_seconds gauge");
            builder.AppendLine($"sqlite_multi_tenant_endpoint_request_duration_seconds{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\",endpoint=\"{EscapePrometheusLabel(endpoint)}\"}} {metrics.AverageResponseTimeMs / 1000.0}");
            builder.AppendLine();
        }

        // Error type metrics (with error_type label)
        foreach (var errorCount in snapshot.ErrorCounts)
        {
            var errorType = errorCount.Key;
            var count = errorCount.Value;
            var tenantId = tenantContext?.TenantId ?? "global";

            builder.AppendLine("# HELP sqlite_multi_tenant_errors_by_type_total Total errors by error type");
            builder.AppendLine("# TYPE sqlite_multi_tenant_errors_by_type_total counter");
            builder.AppendLine($"sqlite_multi_tenant_errors_by_type_total{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\",error_type=\"{EscapePrometheusLabel(errorType)}\"}} {count}");
            builder.AppendLine();
        }

        // Build info metric
        builder.AppendLine("# HELP sqlite_multi_tenant_build_info Build information and version");
        builder.AppendLine("# TYPE sqlite_multi_tenant_build_info gauge");
        var version = System.Reflection.Assembly.GetExecutingAssembly()?.GetName().Version?.ToString() ?? "unknown";
        builder.AppendLine($"sqlite_multi_tenant_build_info{{version=\"{EscapePrometheusLabel(version)}\",captured_at=\"{DateTime.UtcNow:O}\"}} 1");
        builder.AppendLine();

        return builder.ToString();
    }

    /// <summary>
    /// Escapes a string for use as a Prometheus label value.
    /// Replaces backslashes, quotes, and newlines with escaped versions.
    /// </summary>
    private static string EscapePrometheusLabel(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Prometheus requires: backslash, double quote, and newline to be escaped
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }
}
