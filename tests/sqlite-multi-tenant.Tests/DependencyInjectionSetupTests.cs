using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SqliteMultiTenant.Configuration;
using System;

namespace SqliteMultiTenant.Tests
{
    public class DependencyInjectionSetupTests
    {
        [Fact]
        public void AddApiControllers_ValidInput_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = DependencyInjectionSetup.AddApiControllers(services);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void AddMiddlewareServices_ValidInput_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = DependencyInjectionSetup.AddMiddlewareServices(services);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void AddCachingServices_ValidInput_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = DependencyInjectionSetup.AddCachingServices(services);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void AddEventServices_ValidInput_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = DependencyInjectionSetup.AddEventServices(services);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void AddFormatterServices_ValidInput_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = DependencyInjectionSetup.AddFormatterServices(services);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void AddValidationServices_ValidInput_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = DependencyInjectionSetup.AddValidationServices(services);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void AddHealthCheckServices_ValidInput_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = DependencyInjectionSetup.AddHealthCheckServices(services, "databasePath");

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void AddBackgroundWorkers_ValidInput_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = DependencyInjectionSetup.AddBackgroundWorkers(services);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void AddIntegrationServices_ValidInput_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = DependencyInjectionSetup.AddIntegrationServices(services);

            // Assert
            Assert.NotNull(result);
        }
    }
}