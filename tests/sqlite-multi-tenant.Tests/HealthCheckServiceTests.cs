// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Health;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class HealthCheckServiceTests
{
    private readonly ILogger<HealthCheckService> _mockLogger;
    private readonly HealthCheckService _healthCheckService;

    public HealthCheckServiceTests()
    {
        _mockLogger = Substitute.For<ILogger<HealthCheckService>>();
        _healthCheckService = new HealthCheckService(_mockLogger);
    }

    [Fact]
    public async Task GetHealthStatusAsync_ShouldReturnResponse()
    {
        // Act
        var response = await _healthCheckService.GetHealthStatusAsync();

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task IsDatabaseHealthyAsync_ShouldReturnBoolean()
    {
        // Act
        var isHealthy = await _healthCheckService.IsDatabaseHealthyAsync();

        // Assert
        isHealthy.Should().BeTrue(); // Assuming mock or default returns true
    }

    [Fact]
    public async Task IsDiskSpaceHealthyAsync_WithDefaultRequirement_ShouldReturnBoolean()
    {
        // Act
        var isHealthy = await _healthCheckService.IsDiskSpaceHealthyAsync();

        // Assert
        isHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task IsDiskSpaceHealthyAsync_WithHighRequirement_ShouldHandleProperly()
    {
        // Act
        var isHealthy = await _healthCheckService.IsDiskSpaceHealthyAsync(long.MaxValue);

        // Assert
        isHealthy.Should().BeFalse(); // Disk doesn't have MaxValue space
    }

    [Fact]
    public void Service_Initialization_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new HealthCheckService(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }
}