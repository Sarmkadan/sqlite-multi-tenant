namespace SqliteMultiTenant.Tests
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using SqliteMultiTenant.BackgroundWorkers;
    using SqliteMultiTenant.Models;
    using SqliteMultiTenant.Services;
    using Xunit;
    using NSubstitute;

    public sealed class DatabaseMaintenanceWorkerTests
    {
        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var maintenanceService = Substitute.For<ITenantDatabaseMaintenanceService>();

            // Act
            Action act = () => new DatabaseMaintenanceWorker(null, maintenanceService);

            // Assert
            Assert.Throws<ArgumentNullException>(act);
        }

        [Fact]
        public void Constructor_NullMaintenanceService_ThrowsArgumentNullException()
        {
            // Arrange
            var logger = Substitute.For<ILogger<DatabaseMaintenanceWorker>>();

            // Act
            Action act = () => new DatabaseMaintenanceWorker(logger, null);

            // Assert
            Assert.Throws<ArgumentNullException>(act);
        }

        [Fact]
        public void Constructor_NullInterval_UsesDefaultIntervalOf24Hours()
        {
            // Arrange
            var logger = Substitute.For<ILogger<DatabaseMaintenanceWorker>>();
            var maintenanceService = Substitute.For<ITenantDatabaseMaintenanceService>();

            // Act
            var worker = new DatabaseMaintenanceWorker(logger, maintenanceService, null);

            // Assert
            var intervalField = typeof(DatabaseMaintenanceWorker)
                .GetField("_interval", BindingFlags.NonPublic | BindingFlags.Instance);
            var interval = (TimeSpan)intervalField.GetValue(worker);
            Assert.Equal(TimeSpan.FromHours(24), interval);
        }

        [Fact]
        public void Constructor_WithInterval_UsesProvidedInterval()
        {
            // Arrange
            var logger = Substitute.For<ILogger<DatabaseMaintenanceWorker>>();
            var maintenanceService = Substitute.For<ITenantDatabaseMaintenanceService>();
            var interval = TimeSpan.FromHours(6);

            // Act
            var worker = new DatabaseMaintenanceWorker(logger, maintenanceService, interval);

            // Assert
            var intervalField = typeof(DatabaseMaintenanceWorker)
                .GetField("_interval", BindingFlags.NonPublic | BindingFlags.Instance);
            var actual = (TimeSpan)intervalField.GetValue(worker);
            Assert.Equal(interval, actual);
        }
    }

    public sealed class DatabaseMaintenanceOptionsTests
    {
        [Fact]
        public void Options_DefaultValues_AreAsExpected()
        {
            // Arrange & Act
            var options = new DatabaseMaintenanceOptions();

            // Assert
            Assert.True(options.EnableVacuum);
            Assert.True(options.EnableAnalyze);
            Assert.False(options.EnableReindex);
            Assert.Equal(24, options.IntervalHours);
            Assert.Equal(300, options.TimeoutSeconds);
            Assert.Equal(1, options.DegreeOfParallelism);
        }

        [Fact]
        public void Options_SetAllProperties_UpdatesValues()
        {
            // Arrange
            var options = new DatabaseMaintenanceOptions();

            // Act
            options.EnableVacuum = false;
            options.EnableAnalyze = false;
            options.EnableReindex = true;
            options.IntervalHours = 12;
            options.TimeoutSeconds = 600;
            options.DegreeOfParallelism = 4;

            // Assert
            Assert.False(options.EnableVacuum);
            Assert.False(options.EnableAnalyze);
            Assert.True(options.EnableReindex);
            Assert.Equal(12, options.IntervalHours);
            Assert.Equal(600, options.TimeoutSeconds);
            Assert.Equal(4, options.DegreeOfParallelism);
        }
    }
}