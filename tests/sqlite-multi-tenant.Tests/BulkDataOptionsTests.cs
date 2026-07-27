using System;
using SqliteMultiTenant.BulkOperations;
using Xunit;

namespace sqlite_multi_tenant.Tests
{
    public class BulkDataOptionsTests
    {
        [Fact]
        public void Constructor_InitializesWithExpectedDefaults()
        {
            // Arrange & Act
            var options = new BulkDataOptions();

            // Assert
            Assert.Equal(1_000, options.DefaultBatchSize);
            Assert.Equal(3, options.MaxConcurrentTables);
            Assert.Equal(10_000_000, options.MaxBufferSizeBytes);
            Assert.Equal(TimeSpan.FromHours(1), options.OperationTimeout);
            Assert.True(options.PublishDomainEvents);
            Assert.True(options.EnableProgressReporting);
            Assert.Equal("./exports", options.DefaultExportDirectory);
            Assert.Equal("./databases", options.BaseDatabasePath);
        }

        [Fact]
        public void CanUpdateIntegerProperties()
        {
            // Arrange
            var options = new BulkDataOptions();

            // Act
            options.DefaultBatchSize = 500;
            options.MaxConcurrentTables = 10;
            options.MaxBufferSizeBytes = 50_000;

            // Assert
            Assert.Equal(500, options.DefaultBatchSize);
            Assert.Equal(10, options.MaxConcurrentTables);
            Assert.Equal(50_000, options.MaxBufferSizeBytes);
        }

        [Fact]
        public void CanUpdateBooleanProperties()
        {
            // Arrange
            var options = new BulkDataOptions();

            // Act
            options.PublishDomainEvents = false;
            options.EnableProgressReporting = false;

            // Assert
            Assert.False(options.PublishDomainEvents);
            Assert.False(options.EnableProgressReporting);
        }

        [Fact]
        public void CanUpdateStringProperties()
        {
            // Arrange
            var options = new BulkDataOptions();

            // Act
            options.DefaultExportDirectory = "/tmp/exports";
            options.BaseDatabasePath = "/data/db";

            // Assert
            Assert.Equal("/tmp/exports", options.DefaultExportDirectory);
            Assert.Equal("/data/db", options.BaseDatabasePath);
        }

        [Fact]
        public void CanUpdateTimeSpanProperty()
        {
            // Arrange
            var options = new BulkDataOptions();
            var newTimeout = TimeSpan.FromMinutes(30);

            // Act
            options.OperationTimeout = newTimeout;

            // Assert
            Assert.Equal(newTimeout, options.OperationTimeout);
        }
    }
}
