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
/// <summary>
/// Contains unit tests for the <see cref="ConfigurationManager"/> class.
/// Tests various constructor validation scenarios, configuration section retrieval,
/// tenant-specific settings resolution, and options management functionality.
/// </summary>
public sealed class ConfigurationManagerTests : IDisposable
{
private readonly IConfiguration _mockConfiguration;
private readonly ILogger<ConfigurationManager> _mockLogger;
private readonly string _tempBasePath;

/// <summary>
/// Initializes a new instance of the <see cref="ConfigurationManagerTests"/> class.
/// Sets up mock dependencies using NSubstitute and creates a temporary directory for testing.
/// </summary>
public ConfigurationManagerTests()
{
_mockConfiguration = Substitute.For<IConfiguration>();
_mockLogger = Substitute.For<ILogger<ConfigurationManager>>();

// Create a temporary directory for BasePath testing
_tempBasePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
Directory.CreateDirectory(_tempBasePath);
}

/// <summary>
/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
/// Cleans up the temporary directory created during test initialization.
/// </summary>
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
/// <summary>
/// Tests that the <see cref="ConfigurationManager"/> constructor throws <see cref="ArgumentNullException"/>
/// when the configuration parameter is null.
/// </summary>
public void ConfigurationManager_Constructor_ThrowsArgumentNullException_WhenConfigurationIsNull()
{
_mockLogger.LogInformation("ConfigurationManager_Constructor_ThrowsArgumentNullException_WhenConfigurationIsNull called");
// Arrange
var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = 10 };

// Act & Assert
this.Invoking(_ => new ConfigurationManager(null, _mockLogger, Options.Create(options)))
.Should().Throw<ArgumentNullException>()
.WithParameterName("configuration");
}

[Fact]
/// <summary>
/// Tests that the <see cref="ConfigurationManager"/> constructor throws <see cref="ArgumentNullException"/>
/// when the logger parameter is null.
/// </summary>
public void ConfigurationManager_Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
{
_mockLogger.LogInformation("ConfigurationManager_Constructor_ThrowsArgumentNullException_WhenLoggerIsNull called");
// Arrange
var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = 10 };

// Act & Assert
this.Invoking(_ => new ConfigurationManager(_mockConfiguration, null, Options.Create(options)))
.Should().Throw<ArgumentNullException>()
.WithParameterName("logger");
}

[Fact]
/// <summary>
/// Tests that the <see cref="ConfigurationManager"/> constructor throws <see cref="ArgumentNullException"/>
/// when the options parameter is null.
/// </summary>
public void ConfigurationManager_Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
{
_mockLogger.LogInformation("ConfigurationManager_Constructor_ThrowsArgumentNullException_WhenOptionsIsNull called");
// Act & Assert
this.Invoking(_ => new ConfigurationManager(_mockConfiguration, _mockLogger, null))
.Should().Throw<ArgumentNullException>()
.WithParameterName("multiTenantOptions");
}

[Fact]
/// <summary>
/// Tests that the <see cref="ConfigurationManager"/> constructor throws <see cref="ArgumentOutOfRangeException"/>
/// when DefaultMaxConnections is set to 0.
/// </summary>
public void ConfigurationManager_Constructor_ThrowsArgumentOutOfRangeException_WhenDefaultMaxConnectionsIsZero()
{
_mockLogger.LogInformation("ConfigurationManager_Constructor_ThrowsArgumentOutOfRangeException_WhenDefaultMaxConnectionsIsZero called");
// Arrange
var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = 0 };

// Act & Assert
this.Invoking(_ => CreateManager(options))
.Should().Throw<ArgumentOutOfRangeException>()
.WithParameterName("DefaultMaxConnections")
.WithMessage("DefaultMaxConnections must be greater than 0. (Parameter 'DefaultMaxConnections')");
}

[Fact]
/// <summary>
/// Tests that the <see cref="ConfigurationManager"/> constructor throws <see cref="ArgumentOutOfRangeException"/>
/// when DefaultMaxConnections is set to a negative value.
/// </summary>
public void ConfigurationManager_Constructor_ThrowsArgumentOutOfRangeException_WhenDefaultMaxConnectionsIsNegative()
{
_mockLogger.LogInformation("ConfigurationManager_Constructor_ThrowsArgumentOutOfRangeException_WhenDefaultMaxConnectionsIsNegative called");
// Arrange
var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = -5 };

// Act & Assert
this.Invoking(_ => CreateManager(options))
.Should().Throw<ArgumentOutOfRangeException>()
.WithParameterName("DefaultMaxConnections")
.WithMessage("DefaultMaxConnections must be greater than 0. (Parameter 'DefaultMaxConnections')");
}

[Fact]
/// <summary>
/// Tests that the <see cref="ConfigurationManager"/> constructor throws <see cref="ArgumentException"/>
/// when BasePath is null.
/// </summary>
public void ConfigurationManager_Constructor_ThrowsArgumentException_WhenBasePathIsNull()
{
_mockLogger.LogInformation("ConfigurationManager_Constructor_ThrowsArgumentException_WhenBasePathIsNull called");
// Arrange
var options = new MultiTenantOptions { BasePath = null, DefaultMaxConnections = 10 };

// Act & Assert
this.Invoking(_ => CreateManager(options))
.Should().Throw<ArgumentException>()
.WithParameterName("BasePath")
.WithMessage("BasePath cannot be null or empty. (Parameter 'BasePath')");
}

[Fact]
/// <summary>
/// Tests that the <see cref="ConfigurationManager"/> constructor throws <see cref="ArgumentException"/>
/// when BasePath is empty.
/// </summary>
public void ConfigurationManager_Constructor_ThrowsArgumentException_WhenBasePathIsEmpty()
{
_mockLogger.LogInformation("ConfigurationManager_Constructor_ThrowsArgumentException_WhenBasePathIsEmpty called");
// Arrange
var options = new MultiTenantOptions { BasePath = "", DefaultMaxConnections = 10 };

// Act & Assert
this.Invoking(_ => CreateManager(options))
.Should().Throw<ArgumentException>()
.WithParameterName("BasePath")
.WithMessage("BasePath cannot be null or empty. (Parameter 'BasePath')");
}

[Fact]
/// <summary>
/// Tests that the <see cref="ConfigurationManager"/> constructor throws <see cref="DirectoryNotFoundException"/>
/// when BasePath does not exist.
/// </summary>
public void ConfigurationManager_Constructor_ThrowsDirectoryNotFoundException_WhenBasePathDoesNotExist()
{
_mockLogger.LogInformation("ConfigurationManager_Constructor_ThrowsDirectoryNotFoundException_WhenBasePathDoesNotExist called");
// Arrange
var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent");
var options = new MultiTenantOptions { BasePath = nonExistentPath, DefaultMaxConnections = 10 };

// Act & Assert
this.Invoking(_ => CreateManager(options))
.Should().Throw<DirectoryNotFoundException>()
.WithMessage($"BasePath '{nonExistentPath}' does not exist.");
}

[Fact]
/// <summary>
/// Tests that the <see cref="ConfigurationManager"/> constructor successfully validates options
/// and logs a success message when valid options are provided.
/// </summary>
public void ConfigurationManager_Constructor_LogsSuccess_WhenOptionsAreValid()
{
_mockLogger.LogInformation("ConfigurationManager_Constructor_LogsSuccess_WhenOptionsAreValid called");
// Arrange
var options = new MultiTenantOptions { BasePath = _tempBasePath, DefaultMaxConnections = 10 };

// Act
var manager = CreateManager(options);

// Assert
_mockLogger.Received(1).LogInformation("Multi-tenant options validated successfully.");
manager.Should().NotBeNull();
}

[Fact]
/// <summary>
/// Tests that the <see cref="ConfigurationManager.GetSection"/> method returns the correct
/// configuration section for the specified key.
/// </summary>
public void GetSection_ReturnsCorrectConfigurationSection()
{
_mockLogger.LogInformation("GetSection_ReturnsCorrectConfigurationSection called");
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
/// <summary>
/// Tests that the <see cref="ConfigurationManager.GetTenantSetting"/> method returns the tenant-specific
/// setting when it is available.
/// </summary>
public void GetTenantSetting_ReturnsTenantSpecificSetting_WhenAvailable()
{
_mockLogger.LogInformation("GetTenantSetting_ReturnsTenantSpecificSetting_WhenAvailable called");
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
/// <summary>
/// Tests that the <see cref="ConfigurationManager.GetTenantSetting"/> method returns the global setting
/// when the tenant-specific setting is not available.
/// </summary>
public void GetTenantSetting_ReturnsGlobalSetting_WhenTenantSpecificNotAvailable()
{
_mockLogger.LogInformation("GetTenantSetting_ReturnsGlobalSetting_WhenTenantSpecificNotAvailable called");
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
/// <summary>
/// Tests that the <see cref="ConfigurationManager.GetTenantSetting"/> method returns null when neither
/// tenant-specific nor global setting is available.
/// </summary>
public void GetTenantSetting_ReturnsNull_WhenNeitherTenantSpecificNorGlobalSettingAvailable()
{
_mockLogger.LogInformation("GetTenantSetting_ReturnsNull_WhenNeitherTenantSpecificNorGlobalSettingAvailable called");
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
/// <summary>
/// Tests that the <see cref="ConfigurationManager.GetMultiTenantOptions"/> method returns the configured
/// <see cref="MultiTenantOptions"/> instance.
/// </summary>
public void GetMultiTenantOptions_ReturnsConfiguredOptions()
{
_mockLogger.LogInformation("GetMultiTenantOptions_ReturnsConfiguredOptions called");
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