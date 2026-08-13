#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Database;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Tenants;
using Xunit;

/// <summary>
/// Tests for the TenantProvisioner class.
/// </summary>
public sealed class TenantProvisionerTests : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantProvisionerTests"/> class.
    /// </summary>
    public TenantProvisionerTests()
    {
        _mockLogger = Substitute.For<ILogger<TenantProvisioner>>();
        _basePath = Path.Combine(Path.GetTempPath(), $"tenant_provisioner_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_basePath);

        var tenantRepository = Substitute.For<ITenantRepository>();
        var schemaManager = new SchemaManager(Substitute.For<ILogger<SchemaManager>>(), "Data Source=:memory:;Version=3;");

        // Instantiating with mock logger
        _provisioner = new TenantProvisioner(tenantRepository, schemaManager, _mockLogger, _basePath);
    }

    /// <summary>
    /// Gets the mock logger instance used in the tests.
    /// </summary>
    private readonly ILogger<TenantProvisioner> _mockLogger;

    /// <summary>
    /// Gets the base path used for temporary files.
    /// </summary>
    private readonly string _basePath;

    /// <summary>
    /// Gets the tenant provisioner instance used in the tests.
    /// </summary>
    private readonly TenantProvisioner _provisioner;

    /// <summary>
    /// Disposes of the temporary files created during the tests.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_basePath))
        {
            Directory.Delete(_basePath, true);
        }
    }

    /// <summary>
    /// Tests that validating a tenant database with a null tenant ID throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public async Task ValidateTenantDatabaseAsync_WithNullTenantId_ThrowsArgumentNullException()
    {
        // Arrange
        string? tenantId = null;
        _mockLogger.LogInformation("ValidateTenantDatabaseAsync called with tenantId {TenantId}", tenantId);

        // Act
        var act = async () => await _provisioner.ValidateTenantDatabaseAsync(tenantId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();

        _mockLogger.LogInformation("ValidateTenantDatabaseAsync_WithNullTenantId completed");
    }

    /// <summary>
    /// Tests that validating a tenant database with an empty tenant ID throws an <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public async Task ValidateTenantDatabaseAsync_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Arrange
        var tenantId = string.Empty;
        _mockLogger.LogInformation("ValidateTenantDatabaseAsync called with tenantId {TenantId}", tenantId);

        // Act
        var act = async () => await _provisioner.ValidateTenantDatabaseAsync(tenantId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();

        _mockLogger.LogInformation("ValidateTenantDatabaseAsync_WithEmptyTenantId completed");
    }

    /// <summary>
    /// Tests that cloning a tenant with an invalid source tenant ID throws an <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public async Task CloneTenantAsync_WithInvalidSource_ThrowsArgumentException()
    {
        // Arrange
        var sourceTenantId = string.Empty;
        var targetTenantId = "target1";
        _mockLogger.LogInformation(
            "CloneTenantAsync called with sourceTenantId {SourceTenantId}, targetTenantId {TargetTenantId}",
            sourceTenantId,
            targetTenantId);

        // Act
        var act = async () => await _provisioner.CloneTenantAsync(sourceTenantId, targetTenantId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();

        _mockLogger.LogInformation("CloneTenantAsync_WithInvalidSource completed");
    }

    /// <summary>
    /// Tests that cloning a tenant with an invalid target tenant ID throws an <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public async Task CloneTenantAsync_WithInvalidTarget_ThrowsArgumentException()
    {
        // Arrange
        var sourceTenantId = "source1";
        var targetTenantId = string.Empty;
        _mockLogger.LogInformation(
            "CloneTenantAsync called with sourceTenantId {SourceTenantId}, targetTenantId {TargetTenantId}",
            sourceTenantId,
            targetTenantId);

        // Act
        var act = async () => await _provisioner.CloneTenantAsync(sourceTenantId, targetTenantId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();

        _mockLogger.LogInformation("CloneTenantAsync_WithInvalidTarget completed");
    }

    /// <summary>
    /// Tests that deprovisioning a tenant with an invalid tenant ID throws an <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public async Task DeprovisionTenantAsync_WithInvalidId_ThrowsArgumentException()
    {
        // Arrange
        var tenantId = string.Empty;
        _mockLogger.LogInformation("DeprovisionTenantAsync called with tenantId {TenantId}", tenantId);

        // Act
        var act = async () => await _provisioner.DeprovisionTenantAsync(tenantId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();

        _mockLogger.LogInformation("DeprovisionTenantAsync_WithInvalidId completed");
    }
}
