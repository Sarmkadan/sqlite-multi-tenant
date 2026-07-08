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

namespace SqliteMultiTenant.Tests;

public sealed class TenantProvisionerTests : IDisposable {
    private readonly TenantProvisioner _provisioner;
    private readonly ILogger<TenantProvisioner> _mockLogger;
    private readonly string _basePath;

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

    public void Dispose()
    {
        if (Directory.Exists(_basePath))
        {
            Directory.Delete(_basePath, true);
        }
    }

    [Fact]
    public async Task ValidateTenantDatabaseAsync_WithNullTenantId_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _provisioner.ValidateTenantDatabaseAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ValidateTenantDatabaseAsync_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _provisioner.ValidateTenantDatabaseAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CloneTenantAsync_WithInvalidSource_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _provisioner.CloneTenantAsync(string.Empty, "target1");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CloneTenantAsync_WithInvalidTarget_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _provisioner.CloneTenantAsync("source1", string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeprovisionTenantAsync_WithInvalidId_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _provisioner.DeprovisionTenantAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}