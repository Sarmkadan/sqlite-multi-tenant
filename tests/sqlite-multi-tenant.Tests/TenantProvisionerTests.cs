// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Tenants;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class TenantProvisionerTests
{
    private readonly TenantProvisioner _provisioner;
    private readonly ILogger<TenantProvisioner> _mockLogger;

    public TenantProvisionerTests()
    {
        _mockLogger = Substitute.For<ILogger<TenantProvisioner>>();
        // Instantiating with mock logger
        _provisioner = new TenantProvisioner(_mockLogger);
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