#nullable enable
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

/// <summary>
/// Contains unit tests for the <see cref="HealthCheckService"/> class.
/// </summary>
public sealed class HealthCheckServiceTests {
	/// <summary>
	/// Mock logger instance for testing.
	/// </summary>
	private readonly ILogger<HealthCheckService> _mockLogger;

	/// <summary>
	/// Instance of the service under test.
	/// </summary>
	private readonly HealthCheckService _healthCheckService;

	/// <summary>
	/// Initializes a new instance of the <see cref="HealthCheckServiceTests"/> class.
	/// </summary>
	public HealthCheckServiceTests()
	{
		_mockLogger = Substitute.For<ILogger<HealthCheckService>>();
		_healthCheckService = new HealthCheckService(_mockLogger);
	}

	/// <summary>
	/// Tests that <see cref="HealthCheckService.GetHealthStatusAsync"/> returns a valid response object.
	/// </summary>
	[Fact]
	public async Task GetHealthStatusAsync_ShouldReturnResponse()
	{
		// Act
		var response = await _healthCheckService.GetHealthStatusAsync();

		// Assert
		response.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that <see cref="HealthCheckService.IsDatabaseHealthyAsync"/> returns a boolean indicating database health.
	/// </summary>
	[Fact]
	public async Task IsDatabaseHealthyAsync_ShouldReturnBoolean()
	{
		// Act
		var isHealthy = await _healthCheckService.IsDatabaseHealthyAsync();

		// Assert
		isHealthy.Should().BeTrue(); // Assuming mock or default returns true
	}

	/// <summary>
	/// Tests that <see cref="HealthCheckService.IsDiskSpaceHealthyAsync"/> with default disk space requirement returns true.
	/// </summary>
	[Fact]
	public async Task IsDiskSpaceHealthyAsync_WithDefaultRequirement_ShouldReturnBoolean()
	{
		// Act
		var isHealthy = await _healthCheckService.IsDiskSpaceHealthyAsync();

		// Assert
		isHealthy.Should().BeTrue();
	}

	/// <summary>
	/// Tests that <see cref="HealthCheckService.IsDiskSpaceHealthyAsync"/> with maximum disk space requirement returns false.
	/// </summary>
	[Fact]
	public async Task IsDiskSpaceHealthyAsync_WithHighRequirement_ShouldHandleProperly()
	{
		// Act
		var isHealthy = await _healthCheckService.IsDiskSpaceHealthyAsync(long.MaxValue);

		// Assert
		isHealthy.Should().BeFalse(); // Disk doesn't have MaxValue space
	}

	/// <summary>
	/// Tests that <see cref="HealthCheckService"/> constructor throws <see cref="ArgumentNullException"/> when null logger is provided.
	/// </summary>
	[Fact]
	public void Service_Initialization_WithNullLogger_ShouldThrowArgumentNullException()
	{
		// Act
		var action = () => new HealthCheckService(null!);

		// Assert
		action.Should().Throw<ArgumentNullException>();
	}
}
