#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Monitoring
{
    /// <summary>
/// Tracks application performance metrics including query execution times, memory usage,
/// and operation latencies across all tenants in a multi-tenant SQLite environment.
/// </summary>
/// <remarks>
/// This class maintains performance data for operations including timing metrics, success/failure rates,
/// and tenant-specific performance breakdowns. It supports tracking up to 1000 metrics per operation
/// and provides various statistical analyses including percentiles and health summaries.
/// </remarks>
    public sealed class PerformanceMonitor {
        /// <summary>
/// Initializes a new instance of the <see cref="PerformanceMonitor"/> class.
/// </summary>
/// <param name="logger">The logger instance used for recording diagnostic information.</param>
private readonly ILogger<PerformanceMonitor> _logger;
        /// <summary>
/// Gets the collection of performance metrics organized by operation name.
/// </summary>
/// <remarks>
/// This dictionary maintains lists of <see cref="PerformanceMetric"/> objects for each operation,
/// with automatic cleanup to maintain a maximum of 1000 metrics per operation.
/// </remarks>
private readonly ConcurrentDictionary<string, List<PerformanceMetric>> _metrics;
        /// <summary>
/// Gets the stopwatch tracking application uptime since the PerformanceMonitor instance was created.
/// </summary>
private readonly Stopwatch _uptime;

        /// <summary>
/// Initializes a new instance of the <see cref="PerformanceMonitor"/> class.
/// </summary>
/// <param name="logger">The logger instance used for recording diagnostic information.</param>
/// <exception cref="ArgumentNullException">Thrown when the logger parameter is null.</exception>
public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = new ConcurrentDictionary<string, List<PerformanceMetric>>();
            _uptime = Stopwatch.StartNew();
        }

        /// <summary>
/// Starts timing an operation and returns a tracker that automatically records the metric when disposed.
/// </summary>
/// <param name="operationName">The name of the operation being tracked.</param>
/// <param name="tenantId">The optional tenant identifier for multi-tenant scenarios.</param>
/// <returns>A <see cref="PerformanceTracker"/> instance that records the operation duration when disposed.</returns>
/// <exception cref="ArgumentException">Thrown when operationName is null or whitespace.</exception>
        public PerformanceTracker StartOperation(string operationName, string tenantId = null)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                throw new ArgumentException("Operation name cannot be empty", nameof(operationName));

            return new PerformanceTracker(this, operationName, tenantId);
        }

        /// <summary>
/// Records a performance metric for a completed operation.
/// </summary>
/// <param name="operationName">The name of the operation being recorded.</param>
/// <param name="elapsedMilliseconds">The elapsed time in milliseconds.</param>
/// <param name="tenantId">The optional tenant identifier for multi-tenant scenarios.</param>
/// <param name="isSuccess">Whether the operation completed successfully (default: true).</param>
/// <param name="exception">Optional exception that occurred during the operation.</param>
        public void RecordMetric(string operationName, long elapsedMilliseconds, string tenantId = null,
            bool isSuccess = true, Exception exception = null)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                return;

            var metric = new PerformanceMetric
            {
                OperationName = operationName,
                ElapsedMilliseconds = elapsedMilliseconds,
                TenantId = tenantId,
                Timestamp = DateTime.UtcNow,
                IsSuccess = isSuccess,
                ExceptionType = exception?.GetType().Name
            };

            _metrics.AddOrUpdate(operationName,
                new List<PerformanceMetric> { metric },
                (_, list) =>
                {
                    list.Add(metric);
                    // Keep only last 1000 metrics per operation
                    if (list.Count > 1000)
                    {
                        list.RemoveRange(0, list.Count - 1000);
                    }
                    return list;
                });
        }

        /// <summary>
/// Gets aggregated statistics for a specific operation across all recorded executions.
/// </summary>
/// <param name="operationName">The name of the operation to get statistics for.</param>
/// <returns>An <see cref="OperationStatistics"/> object containing performance metrics, or null if no metrics exist for the operation.</returns>
        public OperationStatistics GetOperationStats(string operationName)
        {
            if (!_metrics.TryGetValue(operationName, out var metrics) || !metrics.Any())
            {
                return null;
            }

            var successMetrics = metrics.Where(m => m.IsSuccess).ToList();
            var failureMetrics = metrics.Where(m => !m.IsSuccess).ToList();

            return new OperationStatistics
            {
                OperationName = operationName,
                TotalExecutions = metrics.Count,
                SuccessfulExecutions = successMetrics.Count,
                FailedExecutions = failureMetrics.Count,
                AverageElapsedMs = successMetrics.Any()
                    ? successMetrics.Average(m => m.ElapsedMilliseconds)
                    : 0,
                MinElapsedMs = successMetrics.Any()
                    ? successMetrics.Min(m => m.ElapsedMilliseconds)
                    : 0,
                MaxElapsedMs = successMetrics.Any()
                    ? successMetrics.Max(m => m.ElapsedMilliseconds)
                    : 0,
                SuccessRate = metrics.Count > 0
                    ? (successMetrics.Count / (double)metrics.Count) * 100
                    : 0,
                LastExecutedAt = metrics.OrderByDescending(m => m.Timestamp).FirstOrDefault()?.Timestamp
            };
        }

        /// <summary>
/// Gets aggregated statistics for all operations that have been recorded.
/// </summary>
/// <returns>A dictionary mapping operation names to their <see cref="OperationStatistics"/> objects.</returns>
        public Dictionary<string, OperationStatistics> GetAllStatistics()
        {
            var stats = new Dictionary<string, OperationStatistics>();

            foreach (var kvp in _metrics)
            {
                var opStats = GetOperationStats(kvp.Key);
                if (opStats is not null)
                {
                    stats[kvp.Key] = opStats;
                }
            }

            return stats;
        }

        /// <summary>
/// Gets performance metrics filtered by a specific tenant identifier.
/// </summary>
/// <param name="tenantId">The tenant identifier to filter metrics by.</param>
/// <returns>A dictionary mapping operation names to lists of <see cref="PerformanceMetric"/> objects for the specified tenant.</returns>
        public Dictionary<string, List<PerformanceMetric>> GetTenantMetrics(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return new Dictionary<string, List<PerformanceMetric>>();

            var tenantMetrics = new Dictionary<string, List<PerformanceMetric>>();

            foreach (var kvp in _metrics)
            {
                var filtered = kvp.Value.Where(m => m.TenantId == tenantId).ToList();
                if (filtered.Any())
                {
                    tenantMetrics[kvp.Key] = filtered;
                }
            }

            return tenantMetrics;
        }

        /// <summary>
/// Gets recently recorded slow operations that exceeded the specified performance threshold.
/// </summary>
/// <param name="thresholdMs">The minimum elapsed time in milliseconds to consider an operation "slow" (default: 1000ms).</param>
/// <param name="limit">The maximum number of slow operations to return (default: 20).</param>
/// <returns>A list of <see cref="PerformanceMetric"/> objects for operations exceeding the threshold, ordered by duration (descending).</returns>
public List<PerformanceMetric> GetSlowOperations(long thresholdMs = 1000, int limit = 20)
        public List<PerformanceMetric> GetSlowOperations(long thresholdMs = 1000, int limit = 20)
        {
            var slowOps = _metrics
                .SelectMany(kvp => kvp.Value)
                .Where(m => m.ElapsedMilliseconds > thresholdMs)
                .OrderByDescending(m => m.ElapsedMilliseconds)
                .Take(limit)
                .ToList();

            return slowOps;
        }

        /// <summary>
/// Gets a comprehensive summary of system health including uptime, success rates, and latency percentiles.
/// </summary>
/// <returns>A <see cref="SystemHealthSummary"/> object containing overall system performance metrics.</returns>
public SystemHealthSummary GetHealthSummary()
        public SystemHealthSummary GetHealthSummary()
        {
            var allMetrics = _metrics.SelectMany(kvp => kvp.Value).ToList();
            var successCount = allMetrics.Count(m => m.IsSuccess);

            return new SystemHealthSummary
            {
                UptimeSeconds = _uptime.Elapsed.TotalSeconds,
                TotalOperations = allMetrics.Count,
                SuccessRate = allMetrics.Count > 0
                    ? (successCount / (double)allMetrics.Count) * 100
                    : 0,
                OperationTypes = _metrics.Count,
                AverageLatencyMs = allMetrics.Any()
                    ? allMetrics.Average(m => m.ElapsedMilliseconds)
                    : 0,
                P95LatencyMs = CalculatePercentile(allMetrics, 95),
                P99LatencyMs = CalculatePercentile(allMetrics, 99),
                LastUpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
/// Clears all recorded performance metrics from the monitor.
/// </summary>
/// <remarks>
/// This method is useful for resetting baseline metrics, typically used when starting a new measurement
/// period or when troubleshooting performance issues to eliminate historical data.
/// </remarks>
public void ClearMetrics()
        public void ClearMetrics()
        {
            _metrics.Clear();
            _logger.LogInformation("Performance metrics cleared");
        }

        private long CalculatePercentile(List<PerformanceMetric> metrics, int percentile)
        {
            if (!metrics.Any())
                return 0;

            var sorted = metrics
                .OrderBy(m => m.ElapsedMilliseconds)
                .ToList();

            var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
            return sorted[Math.Max(0, index)].ElapsedMilliseconds;
        }
    }

    /// <summary>
/// A disposable tracker that measures and records the duration of an operation when disposed.
/// </summary>
/// <remarks>
/// This class implements <see cref="IDisposable"/> to provide a convenient way to track operation duration
/// using a using statement pattern. The elapsed time is automatically recorded when the tracker is disposed.
/// </remarks>
public sealed class PerformanceTracker : IDisposable {
    public sealed class PerformanceTracker : IDisposable {
        private readonly PerformanceMonitor _monitor;
        private readonly string _operationName;
        private readonly string _tenantId;
        private readonly Stopwatch _stopwatch;

        /// <summary>
/// Initializes a new instance of the <see cref="PerformanceTracker"/> class.
/// </summary>
/// <param name="monitor">The <see cref="PerformanceMonitor"/> instance to record metrics with.</param>
/// <param name="operationName">The name of the operation being tracked.</param>
/// <param name="tenantId">The optional tenant identifier for multi-tenant scenarios.</param>
public PerformanceTracker(PerformanceMonitor monitor, string operationName, string tenantId)
        {
            _monitor = monitor;
            _operationName = operationName;
            _tenantId = tenantId;
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
/// Records the operation metric with the elapsed time and disposes of the tracker.
/// </summary>
/// <remarks>
/// This method stops the internal stopwatch and records the metric with the <see cref="PerformanceMonitor"/>.
/// </remarks>
public void Dispose()
        {
            _stopwatch.Stop();
            _monitor.RecordMetric(_operationName, _stopwatch.ElapsedMilliseconds, _tenantId);
        }

        /// <summary>
/// Records an operation metric with failure status when an exception occurs.
/// </summary>
/// <param name="ex">The exception that occurred during the operation.</param>
/// <remarks>
/// This method stops the internal stopwatch, records the metric as a failure, and includes the exception type
/// in the recorded metric for diagnostic purposes.
/// </remarks>
public void RecordException(Exception ex)
        {
            _stopwatch.Stop();
            _monitor.RecordMetric(_operationName, _stopwatch.ElapsedMilliseconds, _tenantId, false, ex);
        }
    }

    /// <summary>
/// Represents a single performance measurement for an operation.
/// </summary>
/// <remarks>
/// This class stores timing data, success/failure status, and contextual information about a specific
/// operation execution, including tenant association for multi-tenant scenarios.
/// </remarks>
public sealed class PerformanceMetric {
        /// <summary>
/// Gets or sets the name of the operation being tracked.
/// </summary>
public string OperationName { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public string TenantId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsSuccess { get; set; }
        public string ExceptionType { get; set; }
    }

    public sealed class OperationStatistics {
        /// <summary>
/// Gets or sets the name of the operation being tracked.
/// </summary>
public string OperationName { get; set; }
        public long TotalExecutions { get; set; }
        public long SuccessfulExecutions { get; set; }
        public long FailedExecutions { get; set; }
        public double AverageElapsedMs { get; set; }
        public long MinElapsedMs { get; set; }
        public long MaxElapsedMs { get; set; }
        public double SuccessRate { get; set; }
        public DateTime? LastExecutedAt { get; set; }
    }

    public sealed class SystemHealthSummary {
        public double UptimeSeconds { get; set; }
        public long TotalOperations { get; set; }
        public double SuccessRate { get; set; }
        public int OperationTypes { get; set; }
        public double AverageLatencyMs { get; set; }
        public long P95LatencyMs { get; set; }
        public long P99LatencyMs { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }
}
