using System;
using SqliteMultiTenant.Models;
using Xunit;

namespace sqlite_multi_tenant.Tests
{
    public class TenantMaintenanceResultTests
    {
        [Fact]
        public void DurationMs_ReturnsCorrectMilliseconds_WhenCompleted()
        {
            // Arrange
            var result = new TenantMaintenanceResult
            {
                StartedAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                CompletedAt = new DateTime(2023, 1, 1, 12, 0, 5, DateTimeKind.Utc) // 5 seconds later
            };

            // Act
            var duration = result.DurationMs;

            // Assert
            Assert.Equal(5_000, duration);
        }

        [Fact]
        public void DurationMs_ReturnsZero_WhenNotCompleted()
        {
            var result = new TenantMaintenanceResult
            {
                StartedAt = DateTime.UtcNow,
                CompletedAt = null
            };

            Assert.Equal(0, result.DurationMs);
        }

        [Fact]
        public void SizeReductionBytes_IsCalculatedCorrectly()
        {
            var result = new TenantMaintenanceResult
            {
                SizeBeforeBytes = 10_000,
                SizeAfterBytes = 7_500
            };

            Assert.Equal(2_500, result.SizeReductionBytes);
        }

        [Fact]
        public void IsSuccess_IsTrue_WhenNoErrorAndCompleted()
        {
            var result = new TenantMaintenanceResult
            {
                CompletedAt = DateTime.UtcNow,
                Error = null
            };

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void IsSuccess_IsFalse_WhenErrorIsSet()
        {
            var result = new TenantMaintenanceResult
            {
                CompletedAt = DateTime.UtcNow,
                Error = "boom"
            };

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void SizeChangeSummary_ReturnsNA_WhenSizeBeforeIsZero()
        {
            var result = new TenantMaintenanceResult
            {
                SizeBeforeBytes = 0,
                SizeAfterBytes = 0,
                CompletedAt = DateTime.UtcNow
            };

            Assert.Equal("N/A", result.SizeChangeSummary);
        }

        [Fact]
        public void SizeChangeSummary_ReturnsFormattedString_OnSuccess()
        {
            var result = new TenantMaintenanceResult
            {
                TenantId = "t1",
                TenantName = "Tenant One",
                Operation = "VACUUM",
                StartedAt = DateTime.UtcNow.AddSeconds(-10),
                CompletedAt = DateTime.UtcNow,
                SizeBeforeBytes = 10_485_760, // 10 MB
                SizeAfterBytes = 5_242_880   // 5 MB
            };

            var summary = result.SizeChangeSummary;

            // Expect reduction of 50%
            Assert.Contains("10.00 MB → 5.00 MB", summary);
            Assert.Contains("saved: 5.00 MB", summary);
            Assert.Contains("50.00% reduction", summary);
        }

        [Fact]
        public void SizeChangeSummary_IncludesError_WhenOperationFailed()
        {
            var result = new TenantMaintenanceResult
            {
                TenantId = "t2",
                TenantName = "Tenant Two",
                Operation = "ANALYZE",
                StartedAt = DateTime.UtcNow,
                CompletedAt = null,
                SizeBeforeBytes = 1_024,
                SizeAfterBytes = 1_024,
                Error = "analysis failed"
            };

            var summary = result.SizeChangeSummary;

            Assert.Contains("1.00 KB → 1.00 KB", summary);
            Assert.Contains("operation failed: analysis failed", summary);
        }

        [Fact]
        public void OperationSummary_ComposesCorrectly()
        {
            var result = new TenantMaintenanceResult
            {
                TenantId = "t3",
                TenantName = "Tenant Three",
                Operation = "VACUUM",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                SizeBeforeBytes = 2_048,
                SizeAfterBytes = 1_024
            };

            var opSummary = result.OperationSummary;

            Assert.StartsWith("VACUUM on Tenant Three (t3): ", opSummary);
            Assert.Contains("2.00 KB → 1.00 KB", opSummary);
        }

        [Fact]
        public void IntermediateSizeBytes_CanBeSetAndRead()
        {
            var result = new TenantMaintenanceResult
            {
                IntermediateSizeBytes = 123_456
            };

            Assert.Equal(123_456, result.IntermediateSizeBytes);
        }
    }
}
