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
    // Tracks application performance metrics including query times, memory usage, and operation latencies
    public class PerformanceMonitor
    {
        private readonly ILogger<PerformanceMonitor> _logger;
        private readonly ConcurrentDictionary<string, List<PerformanceMetric>> _metrics;
        private readonly Stopwatch _uptime;

        public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = new ConcurrentDictionary<string, List<PerformanceMetric>>();
            _uptime = Stopwatch.StartNew();
        }

        // Starts timing an operation
        public PerformanceTracker StartOperation(string operationName, string tenantId = null)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                throw new ArgumentException("Operation name cannot be empty", nameof(operationName));

            return new PerformanceTracker(this, operationName, tenantId);
        }

        // Records a performance metric
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

        // Gets aggregated statistics for an operation
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

        // Gets statistics for all operations
        public Dictionary<string, OperationStatistics> GetAllStatistics()
        {
            var stats = new Dictionary<string, OperationStatistics>();

            foreach (var kvp in _metrics)
            {
                var opStats = GetOperationStats(kvp.Key);
                if (opStats != null)
                {
                    stats[kvp.Key] = opStats;
                }
            }

            return stats;
        }

        // Gets per-tenant performance breakdown
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

        // Gets recent slow operations (above threshold)
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

        // Gets system health summary
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

        // Clears all metrics (useful for resetting baseline)
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

    // Disposable tracker for measuring operation duration
    public class PerformanceTracker : IDisposable
    {
        private readonly PerformanceMonitor _monitor;
        private readonly string _operationName;
        private readonly string _tenantId;
        private readonly Stopwatch _stopwatch;

        public PerformanceTracker(PerformanceMonitor monitor, string operationName, string tenantId)
        {
            _monitor = monitor;
            _operationName = operationName;
            _tenantId = tenantId;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _monitor.RecordMetric(_operationName, _stopwatch.ElapsedMilliseconds, _tenantId);
        }

        public void RecordException(Exception ex)
        {
            _stopwatch.Stop();
            _monitor.RecordMetric(_operationName, _stopwatch.ElapsedMilliseconds, _tenantId, false, ex);
        }
    }

    public class PerformanceMetric
    {
        public string OperationName { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public string TenantId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsSuccess { get; set; }
        public string ExceptionType { get; set; }
    }

    public class OperationStatistics
    {
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

    public class SystemHealthSummary
    {
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
