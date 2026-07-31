using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Monitoring;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public sealed class PerformanceMonitorTests
    {
        private readonly ILogger<PerformanceMonitor> _logger;
        private readonly PerformanceMonitor _monitor;

        public PerformanceMonitorTests()
        {
            _logger = Substitute.For<ILogger<PerformanceMonitor>>();
            _monitor = new PerformanceMonitor(_logger);
        }

        [Fact]
        public void RecordMetric_AddsMetricSuccessfully()
        {
            _monitor.RecordMetric("test-op", 100, "tenant1");

            var stats = _monitor.GetOperationStats("test-op");
            Assert.NotNull(stats);
            Assert.Equal(1, stats.TotalExecutions);
        }

        [Fact]
        public void StartOperation_RecordsMetricOnDispose()
        {
            using (var tracker = _monitor.StartOperation("tracked-op", "tenant1"))
            {
                // Simulate work
            }

            var stats = _monitor.GetOperationStats("tracked-op");
            Assert.NotNull(stats);
            Assert.Equal(1, stats.TotalExecutions);
        }

        [Fact]
        public void GetOperationStats_ReturnsNullForNonExistentOperation()
        {
            var stats = _monitor.GetOperationStats("unknown");
            Assert.Null(stats);
        }

        [Fact]
        public void GetTenantMetrics_FiltersByTenant()
        {
            _monitor.RecordMetric("op1", 10, "tenant1");
            _monitor.RecordMetric("op2", 20, "tenant2");

            var tenant1Metrics = _monitor.GetTenantMetrics("tenant1");
            Assert.Single(tenant1Metrics);
            Assert.True(tenant1Metrics.ContainsKey("op1"));
        }

        [Fact]
        public void GetSlowOperations_ReturnsOnlySlowOperations()
        {
            _monitor.RecordMetric("fast", 10);
            _monitor.RecordMetric("slow", 2000);

            var slowOps = _monitor.GetSlowOperations(thresholdMs: 1000);
            Assert.Single(slowOps);
            Assert.Equal("slow", slowOps[0].OperationName);
        }

        [Fact]
        public void ClearMetrics_RemovesAllMetrics()
        {
            _monitor.RecordMetric("op1", 10);
            _monitor.ClearMetrics();

            var stats = _monitor.GetAllStatistics();
            Assert.Empty(stats);
        }

        [Fact]
        public void PerformanceTracker_RecordException_MarksAsFailure()
        {
            using (var tracker = _monitor.StartOperation("failing-op"))
            {
                tracker.RecordException(new InvalidOperationException());
            }

            var stats = _monitor.GetOperationStats("failing-op");
            Assert.NotNull(stats);
            // It records both a failure (RecordException) and a success (Dispose)
            Assert.Equal(1, stats.SuccessfulExecutions);
            Assert.Equal(1, stats.FailedExecutions);
            Assert.Equal(2, stats.TotalExecutions);
        }
    }
}
