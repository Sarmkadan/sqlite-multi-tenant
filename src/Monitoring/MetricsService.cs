#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Monitoring;

/// <summary>
/// Metrics collection service for monitoring system performance and behavior.
/// Tracks request counts, response times, error rates, and resource usage.
/// Useful for alerting and performance optimization.
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Records an HTTP request with its duration and status code.
    /// </summary>
    /// <param name="path">Request path used as the endpoint key.</param>
    /// <param name="durationMs">Request duration in milliseconds.</param>
    /// <param name="statusCode">HTTP status code of the response.</param>
    void RecordRequest(string path, long durationMs, int statusCode);

    /// <summary>
    /// Records a backup operation.
    /// </summary>
    /// <param name="sizeBytes">Size of the backup in bytes.</param>
    /// <param name="durationMs">Backup duration in milliseconds.</param>
    /// <param name="success">Whether the backup succeeded.</param>
    void RecordBackup(long sizeBytes, long durationMs, bool success);

    /// <summary>
    /// Records a database migration.
    /// </summary>
    /// <param name="version">Migration version.</param>
    /// <param name="durationMs">Migration duration in milliseconds.</param>
    /// <param name="success">Whether the migration succeeded.</param>
    void RecordMigration(string version, long durationMs, bool success);

    /// <summary>
    /// Records an application error by type.
    /// </summary>
    /// <param name="errorType">Error category used as the counter key.</param>
    /// <param name="message">Error message, written to the log.</param>
    void RecordError(string errorType, string message);

    /// <summary>
    /// Gets a snapshot of the current metrics.
    /// </summary>
    /// <returns>A point-in-time copy of the collected metrics.</returns>
    MetricsSnapshot GetSnapshot();
}

/// <summary>
/// Snapshot of current system metrics.
/// </summary>
public sealed class MetricsSnapshot
{
    /// <summary>
    /// Gets or sets the UTC time at which the snapshot was captured.
    /// </summary>
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the total number of HTTP requests recorded.
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Gets or sets the total number of HTTP requests with a status code of 400 or higher.
    /// </summary>
    public long TotalErrors { get; set; }

    /// <summary>
    /// Gets or sets the average HTTP response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the total number of bytes backed up across all backup operations.
    /// </summary>
    public long TotalBackupBytes { get; set; }

    /// <summary>
    /// Gets or sets the total number of backup operations.
    /// </summary>
    public int TotalBackups { get; set; }

    /// <summary>
    /// Gets or sets the number of failed backup operations.
    /// </summary>
    public int FailedBackups { get; set; }

    /// <summary>
    /// Gets or sets the total number of database migrations.
    /// </summary>
    public int TotalMigrations { get; set; }

    /// <summary>
    /// Gets or sets the number of failed database migrations.
    /// </summary>
    public int FailedMigrations { get; set; }

    /// <summary>
    /// Gets or sets the error counts keyed by error type.
    /// </summary>
    public Dictionary<string, int> ErrorCounts { get; set; } = new();

    /// <summary>
    /// Gets or sets the per-endpoint request metrics keyed by endpoint path.
    /// </summary>
    public Dictionary<string, RequestMetrics> EndpointMetrics { get; set; } = new();

    /// <summary>
    /// Returns a one-line summary of the snapshot's totals.
    /// </summary>
    /// <returns>A summary of requests, errors, backups, migrations and endpoints.</returns>
    public override string ToString()
    {
        return $"MetricsSnapshot: {TotalRequests} requests, {TotalErrors} errors, avg {AverageResponseTimeMs:F2}ms, " +
            $"{TotalBackups} backups ({FailedBackups} failed), {TotalMigrations} migrations ({FailedMigrations} failed), " +
            $"{ErrorCounts.Count} error types, {EndpointMetrics.Count} endpoints, captured {CapturedAt:O}";
    }
}

/// <summary>
/// Metrics for individual endpoint.
/// </summary>
public sealed class RequestMetrics
{
    /// <summary>
    /// Gets or sets the endpoint path.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the total number of requests to the endpoint.
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Gets or sets the number of requests with a status code below 400.
    /// </summary>
    public long SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of requests with a status code of 400 or higher.
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the average response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum response time in milliseconds.
    /// </summary>
    public long MaxResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the minimum response time in milliseconds.
    /// </summary>
    public long MinResponseTimeMs { get; set; }

    /// <summary>
    /// Returns a one-line summary of the endpoint's request metrics.
    /// </summary>
    /// <returns>A summary of request, success and error counts and response times.</returns>
    public override string ToString()
    {
        return $"RequestMetrics: {Endpoint ?? "unknown"}, {RequestCount} requests ({SuccessCount} succeeded, {ErrorCount} failed), " +
            $"avg {AverageResponseTimeMs:F2}ms, min {MinResponseTimeMs}ms, max {MaxResponseTimeMs}ms";
    }
}

/// <summary>
/// In-memory metrics collection with thread-safe aggregation.
/// </summary>
public sealed class MetricsService : IMetricsService
{
    private const double PercentFactor = 100;
    private const double MillisecondsPerSecond = 1000.0;
    private const string MetricPrefix = "sqlite_multi_tenant_";

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

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsService"/> class.
    /// </summary>
    /// <param name="logger">Logger used to record metric events.</param>
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
        {
            Interlocked.Increment(ref _totalErrors);
        }

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
                {
                    existing.SuccessCount++;
                }
                else
                {
                    existing.ErrorCount++;
                }

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
        {
            Interlocked.Increment(ref _failedBackups);
        }

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
        {
            Interlocked.Increment(ref _failedMigrations);
        }

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
            {
                avgResponseTime = _responseTimes.Average();
            }
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
        report.AppendLine($"  Error Rate: {(snapshot.TotalRequests > 0 ? (double)snapshot.TotalErrors / snapshot.TotalRequests * PercentFactor : 0):F2}%");
        report.AppendLine($"  Avg Response Time: {snapshot.AverageResponseTimeMs:F2}ms");
        report.AppendLine();

        report.AppendLine("Backups:");
        report.AppendLine($"  Total: {snapshot.TotalBackups}");
        report.AppendLine($"  Failed: {snapshot.FailedBackups}");
        report.AppendLine($"  Success Rate: {(snapshot.TotalBackups > 0 ? (double)(snapshot.TotalBackups - snapshot.FailedBackups) / snapshot.TotalBackups * PercentFactor : 0):F2}%");
        report.AppendLine($"  Total Data Backed Up: {snapshot.TotalBackupBytes / 1_000_000_000:F2}GB");
        report.AppendLine();

        report.AppendLine("Migrations:");
        report.AppendLine($"  Total: {snapshot.TotalMigrations}");
        report.AppendLine($"  Failed: {snapshot.FailedMigrations}");
        report.AppendLine($"  Success Rate: {(snapshot.TotalMigrations > 0 ? (double)(snapshot.TotalMigrations - snapshot.FailedMigrations) / snapshot.TotalMigrations * PercentFactor : 0):F2}%");

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
        builder.AppendLine($"# HELP {MetricPrefix}requests_total Total number of HTTP requests processed");
        builder.AppendLine($"# TYPE {MetricPrefix}requests_total counter");
        builder.AppendLine($"{MetricPrefix}requests_total {snapshot.TotalRequests}");
        builder.AppendLine();

        builder.AppendLine($"# HELP {MetricPrefix}errors_total Total number of errors encountered");
        builder.AppendLine($"# TYPE {MetricPrefix}errors_total counter");
        builder.AppendLine($"{MetricPrefix}errors_total {snapshot.TotalErrors}");
        builder.AppendLine();

        builder.AppendLine($"# HELP {MetricPrefix}error_rate Error rate as a percentage (0-100)");
        builder.AppendLine($"# TYPE {MetricPrefix}error_rate gauge");
        builder.AppendLine($"{MetricPrefix}error_rate {(snapshot.TotalRequests > 0 ? (double)snapshot.TotalErrors / snapshot.TotalRequests * PercentFactor : 0)}");
        builder.AppendLine();

        builder.AppendLine($"# HELP {MetricPrefix}request_duration_seconds Average request duration in seconds");
        builder.AppendLine($"# TYPE {MetricPrefix}request_duration_seconds gauge");
        builder.AppendLine($"{MetricPrefix}request_duration_seconds {snapshot.AverageResponseTimeMs / MillisecondsPerSecond}");
        builder.AppendLine();

        builder.AppendLine($"# HELP {MetricPrefix}backups_total Total number of backup operations");
        builder.AppendLine($"# TYPE {MetricPrefix}backups_total counter");
        builder.AppendLine($"{MetricPrefix}backups_total {snapshot.TotalBackups}");
        builder.AppendLine();

        builder.AppendLine($"# HELP {MetricPrefix}backups_failed_total Total number of failed backup operations");
        builder.AppendLine($"# TYPE {MetricPrefix}backups_failed_total counter");
        builder.AppendLine($"{MetricPrefix}backups_failed_total {snapshot.FailedBackups}");
        builder.AppendLine();

        builder.AppendLine($"# HELP {MetricPrefix}backup_size_bytes Total bytes backed up across all operations");
        builder.AppendLine($"# TYPE {MetricPrefix}backup_size_bytes counter");
        builder.AppendLine($"{MetricPrefix}backup_size_bytes {snapshot.TotalBackupBytes}");
        builder.AppendLine();

        builder.AppendLine($"# HELP {MetricPrefix}migrations_total Total number of database migrations performed");
        builder.AppendLine($"# TYPE {MetricPrefix}migrations_total counter");
        builder.AppendLine($"{MetricPrefix}migrations_total {snapshot.TotalMigrations}");
        builder.AppendLine();

        builder.AppendLine($"# HELP {MetricPrefix}migrations_failed_total Total number of failed database migrations");
        builder.AppendLine($"# TYPE {MetricPrefix}migrations_failed_total counter");
        builder.AppendLine($"{MetricPrefix}migrations_failed_total {snapshot.FailedMigrations}");
        builder.AppendLine();

        // Per-tenant metrics (with tenant_id label)
        if (tenantContext != null && !string.IsNullOrEmpty(tenantContext.TenantId))
        {
            var tenantId = tenantContext.TenantId;

            builder.AppendLine($"# HELP {MetricPrefix}tenant_requests_total Total requests for tenant");
            builder.AppendLine($"# TYPE {MetricPrefix}tenant_requests_total counter");
            builder.AppendLine($"{MetricPrefix}tenant_requests_total{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\"}} {snapshot.TotalRequests}");
            builder.AppendLine();

            builder.AppendLine($"# HELP {MetricPrefix}tenant_errors_total Total errors for tenant");
            builder.AppendLine($"# TYPE {MetricPrefix}tenant_errors_total counter");
            builder.AppendLine($"{MetricPrefix}tenant_errors_total{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\"}} {snapshot.TotalErrors}");
            builder.AppendLine();

            builder.AppendLine($"# HELP {MetricPrefix}tenant_error_rate Error rate for tenant as a percentage (0-100)");
            builder.AppendLine($"# TYPE {MetricPrefix}tenant_error_rate gauge");
            builder.AppendLine($"{MetricPrefix}tenant_error_rate{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\"}} {(snapshot.TotalRequests > 0 ? (double)snapshot.TotalErrors / snapshot.TotalRequests * PercentFactor : 0)}");
            builder.AppendLine();

            builder.AppendLine($"# HELP {MetricPrefix}tenant_request_duration_seconds Average request duration for tenant in seconds");
            builder.AppendLine($"# TYPE {MetricPrefix}tenant_request_duration_seconds gauge");
            builder.AppendLine($"{MetricPrefix}tenant_request_duration_seconds{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\"}} {snapshot.AverageResponseTimeMs / MillisecondsPerSecond}");
            builder.AppendLine();
        }

        // Endpoint-specific metrics (with tenant_id and endpoint labels)
        foreach (var endpointMetric in snapshot.EndpointMetrics)
        {
            var endpoint = endpointMetric.Key;
            var metrics = endpointMetric.Value;
            var tenantId = tenantContext?.TenantId ?? "global";

            builder.AppendLine($"# HELP {MetricPrefix}endpoint_requests_total Total requests for endpoint");
            builder.AppendLine($"# TYPE {MetricPrefix}endpoint_requests_total counter");
            builder.AppendLine($"{MetricPrefix}endpoint_requests_total{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\",endpoint=\"{EscapePrometheusLabel(endpoint)}\"}} {metrics.RequestCount}");
            builder.AppendLine();

            builder.AppendLine($"# HELP {MetricPrefix}endpoint_errors_total Total errors for endpoint");
            builder.AppendLine($"# TYPE {MetricPrefix}endpoint_errors_total counter");
            builder.AppendLine($"{MetricPrefix}endpoint_errors_total{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\",endpoint=\"{EscapePrometheusLabel(endpoint)}\"}} {metrics.ErrorCount}");
            builder.AppendLine();

            builder.AppendLine($"# HELP {MetricPrefix}endpoint_request_duration_seconds Average request duration for endpoint in seconds");
            builder.AppendLine($"# TYPE {MetricPrefix}endpoint_request_duration_seconds gauge");
            builder.AppendLine($"{MetricPrefix}endpoint_request_duration_seconds{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\",endpoint=\"{EscapePrometheusLabel(endpoint)}\"}} {metrics.AverageResponseTimeMs / MillisecondsPerSecond}");
            builder.AppendLine();
        }

        // Error type metrics (with error_type label)
        foreach (var errorCount in snapshot.ErrorCounts)
        {
            var errorType = errorCount.Key;
            var count = errorCount.Value;
            var tenantId = tenantContext?.TenantId ?? "global";

            builder.AppendLine($"# HELP {MetricPrefix}errors_by_type_total Total errors by error type");
            builder.AppendLine($"# TYPE {MetricPrefix}errors_by_type_total counter");
            builder.AppendLine($"{MetricPrefix}errors_by_type_total{{tenant_id=\"{EscapePrometheusLabel(tenantId)}\",error_type=\"{EscapePrometheusLabel(errorType)}\"}} {count}");
            builder.AppendLine();
        }

        // Build info metric
        builder.AppendLine($"# HELP {MetricPrefix}build_info Build information and version");
        builder.AppendLine($"# TYPE {MetricPrefix}build_info gauge");
        var version = System.Reflection.Assembly.GetExecutingAssembly()?.GetName().Version?.ToString() ?? "unknown";
        builder.AppendLine($"{MetricPrefix}build_info{{version=\"{EscapePrometheusLabel(version)}\",captured_at=\"{DateTime.UtcNow:O}\"}} 1");
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
        {
            return input;
        }

        // Prometheus requires: backslash, double quote, and newline to be escaped
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }
}
