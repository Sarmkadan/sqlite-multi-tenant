using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SqliteMultiTenant.Services;

namespace SqliteMultiTenant.Tests
{
    public sealed class TenantDatabaseMaintenanceServiceExtensionsTests
    {
        [Fact]
        public void AddTenantDatabaseMaintenanceService_NullServices_ThrowsArgumentNullException()
        {
            // Arrange
            IServiceCollection services = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.AddTenantDatabaseMaintenanceService());
        }

        [Fact]
        public void AddTenantDatabaseMaintenanceService_WithServices_RegistersServiceAndReturnsSameCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddTenantDatabaseMaintenanceService();

            // Assert
            Assert.Same(services, result);
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(ITenantDatabaseMaintenanceService) &&
                descriptor.ImplementationType == typeof(TenantDatabaseMaintenanceService) &&
                descriptor.Lifetime == ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddTenantDatabaseMaintenanceService_WithConfigure_NullServices_ThrowsArgumentNullException()
        {
            // Arrange
            IServiceCollection services = null;
            Action<TenantDatabaseMaintenanceOptions> configure = options => { };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.AddTenantDatabaseMaintenanceService(configure));
        }

        [Fact]
        public void AddTenantDatabaseMaintenanceService_WithConfigure_NullConfigure_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            Action<TenantDatabaseMaintenanceOptions> configure = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.AddTenantDatabaseMaintenanceService(configure));
        }

        [Fact]
        public void AddTenantDatabaseMaintenanceService_WithConfigure_RegistersServiceAndAppliesConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            bool vacuumEnabled = false;
            bool analyzeEnabled = false;
            bool optimizeEnabled = false;

            // Act
            var result = services.AddTenantDatabaseMaintenanceService(options =>
            {
                options.EnableVacuum = vacuumEnabled;
                options.EnableAnalyze = analyzeEnabled;
                options.EnableOptimize = optimizeEnabled;
            });

            // Assert
            Assert.Same(services, result);
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(ITenantDatabaseMaintenanceService) &&
                descriptor.ImplementationType == typeof(TenantDatabaseMaintenanceService) &&
                descriptor.Lifetime == ServiceLifetime.Scoped);

            // Verify configuration was applied by building service provider and resolving options
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<TenantDatabaseMaintenanceOptions>();
            Assert.NotNull(options);
            Assert.Equal(vacuumEnabled, options.EnableVacuum);
            Assert.Equal(analyzeEnabled, options.EnableAnalyze);
            Assert.Equal(optimizeEnabled, options.EnableOptimize);
        }

        [Fact]
        public void TenantDatabaseMaintenanceOptions_EnableVacuum_DefaultIsTrue()
        {
            // Arrange & Act
            var options = new TenantDatabaseMaintenanceOptions();

            // Assert
            Assert.True(options.EnableVacuum);
        }

        [Fact]
        public void TenantDatabaseMaintenanceOptions_EnableAnalyze_DefaultIsTrue()
        {
            // Arrange & Act
            var options = new TenantDatabaseMaintenanceOptions();

            // Assert
            Assert.True(options.EnableAnalyze);
        }

        [Fact]
        public void TenantDatabaseMaintenanceOptions_EnableOptimize_DefaultIsTrue()
        {
            // Arrange & Act
            var options = new TenantDatabaseMaintenanceOptions();

            // Assert
            Assert.True(options.EnableOptimize);
        }
    }
}