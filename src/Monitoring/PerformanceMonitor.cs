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
    public sealed class PerformanceMonitor
    {
        /// <summary>
        /// The logger instance used for recording diagnostic information.
        /// </summary>
        private readonly ILogger<PerformanceMonitor> _logger;

        /// <summary>
        /// A concurrent dictionary that stores lists of performance metrics organized by operation name.
        /// Each operation name maps to a list of <see cref="PerformanceMetric"/> objects, with automatic cleanup
        /// to retain only the most recent 1000 metrics per operation.
        /// </summary>
        private readonly ConcurrentDictionary<string, List<PerformanceMetric>> _metrics;

        /// <summary>
        /// A stopwatch that tracks the uptime of the PerformanceMonitor instance since its creation.
        /// </summary>
        private readonly Stopwatch _uptime;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceMonitor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance used for recording diagnostic information. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when the logger parameter is null.</exception>
        public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = new ConcurrentDictionary<string, List<PerformanceMetric>>();
            _uptime = Stopwatch.StartNew();
            _logger.LogInformation("PerformanceMonitor initialized");
        }

        /// <summary>
        /// Starts timing an operation and returns a tracker that automatically records the metric when disposed.
        /// </summary>
        /// <param name="operationName">The name of the operation being tracked. Cannot be null or whitespace.</param>
        /// <param name="tenantId">The optional tenant identifier for multi-tenant scenarios. Can be null.</param>
        /// <returns>A <see cref="PerformanceTracker"/> instance that records the operation duration when disposed.</returns>
        /// <exception cref="ArgumentException">Thrown when operationName is null or whitespace.</exception>
        public PerformanceTracker StartOperation(string operationName, string tenantId = null)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                throw new ArgumentException("Operation name cannot be empty", nameof(operationName));

            _logger.LogInformation("Starting operation {OperationName} for tenant {TenantId}", operationName, tenantId ?? "null");
            return new PerformanceTracker(this, operationName, tenantId);
        }

        /// <summary>
        /// Records a performance metric for a completed operation.
        /// </summary>
        /// <param name="operationName">The name of the operation being recorded. If null or whitespace, the method returns without recording.</param>
        /// <param name="elapsedMilliseconds">The elapsed time in milliseconds for the operation.</param>
        /// <param name="tenantId">The optional tenant identifier for multi-tenant scenarios. Can be null.</param>
        /// <param name="isSuccess">Whether the operation completed successfully. Defaults to true.</param>
        /// <param name="exception">Optional exception that occurred during the operation. Can be null.</param>
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
        /// <param name="tenantId">The tenant identifier to filter metrics by. If null or whitespace, returns an empty dictionary.</param>
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
        /// <param name="thresholdMs">The minimum elapsed time in milliseconds to consider an operation "slow". Defaults to 1000ms.</param>
        /// <param name="limit">The maximum number of slow operations to return. Defaults to 20.</param>
        /// <returns>A list of <see cref="PerformanceMetric"/> objects for operations exceeding the threshold, ordered by duration (descending).</returns>
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
        {
            _metrics.Clear();
            _logger.LogInformation("Performance metrics cleared");
        }

        /// <summary>
        /// Calculates the specified percentile value from a list of performance metrics.
        /// </summary>
        /// <param name="metrics">The list of performance metrics to calculate the percentile from.</param>
        /// <param name="percentile">The percentile to calculate (e.g., 95 for P95).</param>
        /// <returns>The elapsed milliseconds value at the specified percentile, or 0 if no metrics are available.</returns>
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
    public sealed class PerformanceTracker : IDisposable
    {
        private readonly PerformanceMonitor _monitor;
        private readonly string _operationName;
        private readonly string _tenantId;
        private readonly Stopwatch _stopwatch;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceTracker"/> class.
        /// </summary>
        /// <param name="monitor">The <see cref="PerformanceMonitor"/> instance to record metrics with.</param>
        /// <param name="operationName">The name of the operation being tracked.</param>
        /// <param name="tenantId">The optional tenant identifier for multi-tenant scenarios. Can be null.</param>
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
    public sealed class PerformanceMetric
    {
        /// <summary>
        /// Gets or sets the name of the operation being tracked.
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// Gets or sets the elapsed time in milliseconds for the operation.
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier associated with the operation. Can be null.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the metric was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the operation completed successfully.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the type name of the exception that occurred during the operation, if any. Can be null.
        /// </summary>
        public string ExceptionType { get; set; }

        /// <summary>
        /// Returns a string representation of the performance metric.
        /// </summary>
        /// <returns>A string representation of the performance metric.</returns>
        public override string ToString()
        {
            return $"PerformanceMetric {{ OperationName = {OperationName}, ElapsedMilliseconds = {ElapsedMilliseconds}, TenantId = {TenantId}, Timestamp = {Timestamp}, IsSuccess = {IsSuccess}, ExceptionType = {ExceptionType} }}";
        }
    }

    /// <summary>
    /// Represents aggregated statistics for a specific operation.
    /// </summary>
    public sealed class OperationStatistics
    {
        /// <summary>
        /// Gets or sets the name of the operation being tracked.
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// Gets or sets the total number of executions for the operation.
        /// </summary>
        public long TotalExecutions { get; set; }

        /// <summary>
        /// Gets or sets the number of successful executions for the operation.
        /// </summary>
        public long SuccessfulExecutions { get; set; }

        /// <summary>
        /// Gets or sets the number of failed executions for the operation.
        /// </summary>
        public long FailedExecutions { get; set; }

        /// <summary>
        /// Gets or sets the average elapsed time in milliseconds for successful executions.
        /// </summary>
        public double AverageElapsedMs { get; set; }

        /// <summary>
        /// Gets or sets the minimum elapsed time in milliseconds for successful executions.
        /// </summary>
        public long MinElapsedMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum elapsed time in milliseconds for successful executions.
        /// </summary>
        public long MaxElapsedMs { get; set; }

        /// <summary>
        /// Gets or sets the success rate as a percentage of total executions.
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp of the last execution of the operation. Can be null.
        /// </summary>
        public DateTime? LastExecutedAt { get; set; }
    }

    /// <summary>
    /// Represents a comprehensive summary of system health and performance metrics.
    /// </summary>
    public sealed class SystemHealthSummary
    {
        /// <summary>
        /// Gets or sets the total uptime of the PerformanceMonitor in seconds.
        /// </summary>
        public double UptimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the total number of recorded operations.
        /// </summary>
        public long TotalOperations { get; set; }

        /// <summary>
        /// Gets or sets the overall success rate as a percentage of all operations.
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Gets or sets the number of unique operation types tracked.
        /// </summary>
        public int OperationTypes { get; set; }

        /// <summary>
        /// Gets or sets the average latency in milliseconds across all operations.
        /// </summary>
        public double AverageLatencyMs { get; set; }

        /// <summary>
        /// Gets or sets the 95th percentile latency in milliseconds across all operations.
        /// </summary>
        public long P95LatencyMs { get; set; }

        /// <summary>
        /// Gets or sets the 99th percentile latency in milliseconds across all operations.
        /// </summary>
        public long P99LatencyMs { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the health summary was last updated.
        /// </summary>
        public DateTime LastUpdatedAt { get; set; }
    }
}
