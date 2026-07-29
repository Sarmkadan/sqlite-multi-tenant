using System;
using SqliteMultiTenant.Configuration;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class MultiTenantOptionsTests
    {
        [Fact]
        public void DefaultValues_ShouldMatchExpected()
        {
            // Arrange & Act
            var options = new MultiTenantOptions();

            // Assert
            Assert.Equal("./databases", options.BasePath);
            Assert.Equal(10, options.MaxConnectionsPerTenant);
            Assert.Equal(10, options.DefaultMaxConnections);
            Assert.Equal(20, options.MaxBackupCount);
            Assert.Equal(TimeSpan.FromDays(30), options.BackupRetention);
            Assert.True(options.EnableBackupScheduling);
            Assert.Equal(TimeSpan.FromHours(1), options.BackupInterval);
            Assert.True(options.EnableAuditLogging);
            Assert.True(options.EnablePerformanceMonitoring);
        }

        [Fact]
        public void PropertySetters_ShouldPersistValues()
        {
            // Arrange
            var options = new MultiTenantOptions
            {
                BasePath = "/var/data",
                MaxConnectionsPerTenant = 5,
                DefaultMaxConnections = 3,
                MaxBackupCount = 7,
                BackupRetention = TimeSpan.FromDays(10),
                EnableBackupScheduling = false,
                BackupInterval = TimeSpan.FromMinutes(30),
                EnableAuditLogging = false,
                EnablePerformanceMonitoring = false
            };

            // Assert
            Assert.Equal("/var/data", options.BasePath);
            Assert.Equal(5, options.MaxConnectionsPerTenant);
            Assert.Equal(3, options.DefaultMaxConnections);
            Assert.Equal(7, options.MaxBackupCount);
            Assert.Equal(TimeSpan.FromDays(10), options.BackupRetention);
            Assert.False(options.EnableBackupScheduling);
            Assert.Equal(TimeSpan.FromMinutes(30), options.BackupInterval);
            Assert.False(options.EnableAuditLogging);
            Assert.False(options.EnablePerformanceMonitoring);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(int.MaxValue)]
        public void MaxConnectionsPerTenant_BoundaryValues_ShouldBeSet(int value)
        {
            // Arrange
            var options = new MultiTenantOptions { MaxConnectionsPerTenant = value };

            // Assert
            Assert.Equal(value, options.MaxConnectionsPerTenant);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(8640000000000000)] // TimeSpan.MaxValue ticks (≈106751 days)
        public void BackupRetention_BoundaryValues_ShouldBeSet(long ticks)
        {
            // Arrange
            var ts = new TimeSpan(ticks);
            var options = new MultiTenantOptions { BackupRetention = ts };

            // Assert
            Assert.Equal(ts, options.BackupRetention);
        }

        [Fact]
        public void BasePath_NullAssignment_ShouldAllowNull()
        {
            // Arrange
            var options = new MultiTenantOptions { BasePath = null! };

            // Assert
            Assert.Null(options.BasePath);
        }

        [Fact]
        public void InvalidNumericValues_ShouldNotThrow()
        {
            // Arrange
            var options = new MultiTenantOptions();

            // Act & Assert - no exception expected when assigning negative numbers
            var exception = Record.Exception(() =>
            {
                options.MaxConnectionsPerTenant = -1;
                options.DefaultMaxConnections = -5;
                options.MaxBackupCount = -10;
            });

            Assert.Null(exception);
            Assert.Equal(-1, options.MaxConnectionsPerTenant);
            Assert.Equal(-5, options.DefaultMaxConnections);
            Assert.Equal(-10, options.MaxBackupCount);
        }
    }
}
