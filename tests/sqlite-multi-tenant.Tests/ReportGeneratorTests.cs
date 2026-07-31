using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Monitoring;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class ReportGeneratorTests
    {
        private readonly ReportGenerator _generator;

        public ReportGeneratorTests()
        {
            _generator = new ReportGenerator(NullLogger<ReportGenerator>.Instance);
        }

        [Fact]
        public void GenerateHealthReport_ValidInputs_ReturnsFormattedReport()
        {
            var health = new SystemHealthSummary
            {
                UptimeSeconds = 100,
                TotalOperations = 10,
                SuccessRate = 95.0,
                OperationTypes = 2,
                AverageLatencyMs = 50,
                P95LatencyMs = 100,
                P99LatencyMs = 200
            };
            var stats = new Dictionary<string, OperationStatistics>
            {
                { "Op1", new OperationStatistics { OperationName = "Op1", TotalExecutions = 5, SuccessRate = 100, AverageElapsedMs = 50, MinElapsedMs = 10, MaxElapsedMs = 100 } }
            };

            var report = _generator.GenerateHealthReport(health, stats);

            Assert.Contains("=== SYSTEM HEALTH REPORT ===", report);
            Assert.Contains("Total Operations: 10", report);
            Assert.Contains("Op1:", report);
        }

        [Fact]
        public void GeneratePerformanceReport_EmptyList_ReturnsReportWithZeroMetrics()
        {
            var metrics = new List<PerformanceMetric>();
            var report = _generator.GeneratePerformanceReport(metrics);

            Assert.Contains("Total Metrics: 0", report);
        }

        [Fact]
        public void GenerateTenantUsageReport_ValidInputs_ReturnsFormattedReport()
        {
            var metrics = new Dictionary<string, List<PerformanceMetric>>
            {
                { "tenant1", new List<PerformanceMetric> { new PerformanceMetric { ElapsedMilliseconds = 100, IsSuccess = true } } }
            };

            var report = _generator.GenerateTenantUsageReport(metrics);

            Assert.Contains("Tenant: tenant1", report);
            Assert.Contains("Operations: 1", report);
        }

        [Fact]
        public void GenerateErrorReport_ValidInputs_ReturnsFormattedReport()
        {
            var metrics = new List<PerformanceMetric>
            {
                new PerformanceMetric { OperationName = "Op1", ExceptionType = "Exception" }
            };

            var report = _generator.GenerateErrorReport(metrics);

            Assert.Contains("Total Failures: 1", report);
            Assert.Contains("Operation: Op1", report);
            Assert.Contains("Exception: 1", report);
        }

        [Fact]
        public void GenerateCapacityReport_ValidInputs_ReturnsFormattedReport()
        {
            var health = new SystemHealthSummary();
            var report = _generator.GenerateCapacityReport(health, 1024 * 1024, 2);

            Assert.Contains("Tenant Count: 2", report);
            Assert.Contains("Total Disk Usage: 1 MB", report);
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ReportGenerator(null!));
        }
    }
}
