// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Monitoring
{
    // Generates comprehensive reports for system monitoring and analysis
    public class ReportGenerator
    {
        private readonly ILogger<ReportGenerator> _logger;

        public ReportGenerator(ILogger<ReportGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Generates a system health report
        public string GenerateHealthReport(SystemHealthSummary health,
            Dictionary<string, OperationStatistics> operationStats)
        {
            var report = new StringBuilder();

            report.AppendLine("=== SYSTEM HEALTH REPORT ===");
            report.AppendLine($"Generated: {DateTime.UtcNow:O}");
            report.AppendLine();

            report.AppendLine("=== UPTIME & OPERATIONS ===");
            report.AppendLine($"System Uptime: {TimeSpan.FromSeconds(health.UptimeSeconds):hh\\:mm\\:ss}");
            report.AppendLine($"Total Operations: {health.TotalOperations}");
            report.AppendLine($"Success Rate: {health.SuccessRate:P2}");
            report.AppendLine($"Operation Types: {health.OperationTypes}");
            report.AppendLine();

            report.AppendLine("=== LATENCY METRICS ===");
            report.AppendLine($"Average Latency: {health.AverageLatencyMs}ms");
            report.AppendLine($"P95 Latency: {health.P95LatencyMs}ms");
            report.AppendLine($"P99 Latency: {health.P99LatencyMs}ms");
            report.AppendLine();

            report.AppendLine("=== TOP OPERATIONS ===");
            var topOps = operationStats.Values
                .OrderByDescending(o => o.TotalExecutions)
                .Take(10)
                .ToList();

            foreach (var op in topOps)
            {
                report.AppendLine($"  {op.OperationName}:");
                report.AppendLine($"    Executions: {op.TotalExecutions}");
                report.AppendLine($"    Success Rate: {op.SuccessRate:P2}");
                report.AppendLine($"    Avg Time: {op.AverageElapsedMs}ms");
                report.AppendLine($"    Min/Max: {op.MinElapsedMs}ms / {op.MaxElapsedMs}ms");
            }

            report.AppendLine();
            report.AppendLine("=== SLOWEST OPERATIONS ===");
            var slowOps = operationStats.Values
                .OrderByDescending(o => o.AverageElapsedMs)
                .Take(5)
                .ToList();

            foreach (var op in slowOps)
            {
                report.AppendLine($"  {op.OperationName}: {op.AverageElapsedMs}ms avg");
            }

            return report.ToString();
        }

        // Generates a performance report
        public string GeneratePerformanceReport(List<PerformanceMetric> metrics)
        {
            var report = new StringBuilder();

            report.AppendLine("=== PERFORMANCE REPORT ===");
            report.AppendLine($"Generated: {DateTime.UtcNow:O}");
            report.AppendLine($"Total Metrics: {metrics.Count}");
            report.AppendLine();

            // Group by operation
            var grouped = metrics.GroupBy(m => m.OperationName);

            foreach (var group in grouped.OrderByDescending(g => g.Count()))
            {
                report.AppendLine($"Operation: {group.Key}");
                report.AppendLine($"  Count: {group.Count()}");
                report.AppendLine($"  Avg Time: {group.Average(m => m.ElapsedMilliseconds):F2}ms");
                report.AppendLine($"  Min: {group.Min(m => m.ElapsedMilliseconds)}ms");
                report.AppendLine($"  Max: {group.Max(m => m.ElapsedMilliseconds)}ms");

                var failures = group.Count(m => !m.IsSuccess);
                if (failures > 0)
                {
                    report.AppendLine($"  Failures: {failures}");
                }
            }

            return report.ToString();
        }

        // Generates a tenant usage report
        public string GenerateTenantUsageReport(Dictionary<string, List<PerformanceMetric>> tenantMetrics)
        {
            var report = new StringBuilder();

            report.AppendLine("=== TENANT USAGE REPORT ===");
            report.AppendLine($"Generated: {DateTime.UtcNow:O}");
            report.AppendLine($"Active Tenants: {tenantMetrics.Count}");
            report.AppendLine();

            foreach (var tenant in tenantMetrics.OrderByDescending(t => t.Value.Count))
            {
                report.AppendLine($"Tenant: {tenant.Key}");
                report.AppendLine($"  Operations: {tenant.Value.Count}");
                report.AppendLine($"  Avg Latency: {tenant.Value.Average(m => m.ElapsedMilliseconds):F2}ms");

                var failures = tenant.Value.Count(m => !m.IsSuccess);
                var successRate = ((tenant.Value.Count - failures) / (double)tenant.Value.Count) * 100;
                report.AppendLine($"  Success Rate: {successRate:P2}");
            }

            return report.ToString();
        }

        // Generates an error report
        public string GenerateErrorReport(List<PerformanceMetric> failedMetrics)
        {
            var report = new StringBuilder();

            report.AppendLine("=== ERROR REPORT ===");
            report.AppendLine($"Generated: {DateTime.UtcNow:O}");
            report.AppendLine($"Total Failures: {failedMetrics.Count}");
            report.AppendLine();

            var byOperation = failedMetrics.GroupBy(m => m.OperationName);

            foreach (var group in byOperation.OrderByDescending(g => g.Count()))
            {
                report.AppendLine($"Operation: {group.Key}");
                report.AppendLine($"  Failure Count: {group.Count()}");

                var byException = group.GroupBy(m => m.ExceptionType);
                foreach (var exGroup in byException)
                {
                    report.AppendLine($"    {exGroup.Key}: {exGroup.Count()}");
                }
            }

            return report.ToString();
        }

        // Generates a capacity planning report
        public string GenerateCapacityReport(SystemHealthSummary health, long totalDiskUsage,
            int tenantCount)
        {
            var report = new StringBuilder();

            report.AppendLine("=== CAPACITY PLANNING REPORT ===");
            report.AppendLine($"Generated: {DateTime.UtcNow:O}");
            report.AppendLine();

            report.AppendLine("=== CURRENT STATE ===");
            report.AppendLine($"Tenant Count: {tenantCount}");
            report.AppendLine($"Total Disk Usage: {FormatBytes(totalDiskUsage)}");
            report.AppendLine($"Avg per Tenant: {FormatBytes(tenantCount > 0 ? totalDiskUsage / tenantCount : 0)}");
            report.AppendLine();

            report.AppendLine("=== PROJECTIONS (12 months) ===");
            var projectedTenants = (int)(tenantCount * 1.3); // 30% growth
            var projectedDiskUsage = (long)(totalDiskUsage * 1.5); // 50% data growth

            report.AppendLine($"Projected Tenants: {projectedTenants}");
            report.AppendLine($"Projected Disk Usage: {FormatBytes(projectedDiskUsage)}");
            report.AppendLine($"Required Capacity: {FormatBytes((long)(projectedDiskUsage * 1.2))}");

            return report.ToString();
        }

        private string FormatBytes(long bytes)
        {
            var sizes = new[] { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
