#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqliteMultiTenant.Configuration;
using System;
using System.IO;
using Xunit;
using ConfigurationManager = SqliteMultiTenant.Configuration.ConfigurationManager;

namespace SqliteMultiTenant.Tests
{
    public sealed class ConfigurationManagerTests : IDisposable {
        private readonly IConfiguration _mockConfiguration;
        private readonly ILogger<ConfigurationManager> _mockLogger;
        private readonly string _tempBasePath;

        public ConfigurationManagerTests()
        {
            _mockConfiguration = Substitute.For<IConfiguration>();
            _mockLogger = Substitute.For<ILogger<ConfigurationManager>>();

            // Create a temporary directory for BasePath testing
            _tempBasePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempBasePath);
        }

        public void Dispose()
        {
            // Clean up the temporary directory
            if (Directory.Exists(_tempBasePath))
            {
                Directory.Delete(_tempBasePath, true);
            }
        }

        private ConfigurationManager CreateManager(MultiTenantOptions options)
        {
            return new ConfigurationManager(_mockConfiguration, _mockLogger, Options.Create(options));
        }

        [Fact]
        public void ConfigurationManager_Constructor_ThrowsArgumentNullException_WhenConfigurationIsNull()
        {
            // Arrange
            var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = 10 };

            // Act & Assert
            this.Invoking(_ => new ConfigurationManager(null, _mockLogger, Options.Create(options)))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("configuration");
        }

        [Fact]
        public void ConfigurationManager_Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            // Arrange
            var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = 10 };

            // Act & Assert
            this.Invoking(_ => new ConfigurationManager(_mockConfiguration, null, Options.Create(options)))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void ConfigurationManager_Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
        {
            // Act & Assert
            this.Invoking(_ => new ConfigurationManager(_mockConfiguration, _mockLogger, null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("multiTenantOptions");
        }
        
        [Fact]
        public void ConfigurationManager_Constructor_ThrowsArgumentOutOfRangeException_WhenDefaultMaxConnectionsIsZero()
        {
            // Arrange
            var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = 0 };

            // Act & Assert
            this.Invoking(_ => CreateManager(options))
                .Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("DefaultMaxConnections")
                .WithMessage("DefaultMaxConnections must be greater than 0. (Parameter 'DefaultMaxConnections')");
        }

        [Fact]
        public void ConfigurationManager_Constructor_ThrowsArgumentOutOfRangeException_WhenDefaultMaxConnectionsIsNegative()
        {
            // Arrange
            var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = -5 };

            // Act & Assert
            this.Invoking(_ => CreateManager(options))
                .Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("DefaultMaxConnections")
                .WithMessage("DefaultMaxConnections must be greater than 0. (Parameter 'DefaultMaxConnections')");
        }

        [Fact]
        public void ConfigurationManager_Constructor_ThrowsArgumentException_WhenBasePathIsNull()
        {
            // Arrange
            var options = new MultiTenantOptions { BasePath = null, DefaultMaxConnections = 10 };

            // Act & Assert
            this.Invoking(_ => CreateManager(options))
                .Should().Throw<ArgumentException>()
                .WithParameterName("BasePath")
                .WithMessage("BasePath cannot be null or empty. (Parameter 'BasePath')");
        }

        [Fact]
        public void ConfigurationManager_Constructor_ThrowsArgumentException_WhenBasePathIsEmpty()
        {
            // Arrange
            var options = new MultiTenantOptions { BasePath = "", DefaultMaxConnections = 10 };

            // Act & Assert
            this.Invoking(_ => CreateManager(options))
                .Should().Throw<ArgumentException>()
                .WithParameterName("BasePath")
                .WithMessage("BasePath cannot be null or empty. (Parameter 'BasePath')");
        }

        [Fact]
        public void ConfigurationManager_Constructor_ThrowsDirectoryNotFoundException_WhenBasePathDoesNotExist()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent");
            var options = new MultiTenantOptions { BasePath = nonExistentPath, DefaultMaxConnections = 10 };

            // Act & Assert
            this.Invoking(_ => CreateManager(options))
                .Should().Throw<DirectoryNotFoundException>()
                .WithMessage($"BasePath '{nonExistentPath}' does not exist.");
        }

        [Fact]
        public void ConfigurationManager_Constructor_LogsSuccess_WhenOptionsAreValid()
        {
            // Arrange
            var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = 10 };

            // Act
            var manager = CreateManager(options);

            // Assert
            _mockLogger.Received(1).LogInformation("Multi-tenant options validated successfully.");
            manager.Should().NotBeNull();
        }

        [Fact]
        public void GetSection_ReturnsCorrectConfigurationSection()
        {
            // Arrange
            var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = 10 };
            var manager = CreateManager(options);
            var expectedSection = Substitute.For<IConfigurationSection>();
            _mockConfiguration.GetSection("TestSection").Returns(expectedSection);

            // Act
            var result = manager.GetSection("TestSection");

            // Assert
            result.Should().Be(expectedSection);
        }

        [Fact]
        public void GetTenantSetting_ReturnsTenantSpecificSetting_WhenAvailable()
        {
            // Arrange
            var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = 10 };
            var manager = CreateManager(options);
            _mockConfiguration["Tenants:tenant123:Settings:MyKey"].Returns("TenantValue");
            _mockConfiguration["GlobalSettings:MyKey"].Returns("GlobalValue");

            // Act
            var result = manager.GetTenantSetting("tenant123", "MyKey");

            // Assert
            result.Should().Be("TenantValue");
        }

        [Fact]
        public void GetTenantSetting_ReturnsGlobalSetting_WhenTenantSpecificNotAvailable()
        {
            // Arrange
            var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = 10 };
            var manager = CreateManager(options);
            _mockConfiguration["Tenants:tenant123:Settings:MyKey"].Returns(null as string); // Explicitly null
            _mockConfiguration["GlobalSettings:MyKey"].Returns("GlobalValue");

            // Act
            var result = manager.GetTenantSetting("tenant123", "MyKey");

            // Assert
            result.Should().Be("GlobalValue");
        }

        [Fact]
        public void GetTenantSetting_ReturnsNull_WhenNeitherTenantSpecificNorGlobalSettingAvailable()
        {
            // Arrange
            var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = 10 };
            var manager = CreateManager(options);
            _mockConfiguration["Tenants:tenant123:Settings:MyKey"].Returns(null as string);
            _mockConfiguration["GlobalSettings:MyKey"].Returns(null as string);

            // Act
            var result = manager.GetTenantSetting("tenant123", "MyKey");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetMultiTenantOptions_ReturnsConfiguredOptions()
        {
            // Arrange
            var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = 15 };
            var manager = CreateManager(options);

            // Act
            var result = manager.GetMultiTenantOptions();

            // Assert
            result.Should().Be(options);
            result.DefaultMaxConnections.Should().Be(15);
        }
    }
}
