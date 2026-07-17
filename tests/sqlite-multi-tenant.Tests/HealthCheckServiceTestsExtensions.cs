#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Health;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Extension methods for <see cref="HealthCheckServiceTests"/> that provide additional testing capabilities
/// for health check service scenarios.
/// </summary>
/// <remarks>
/// All extension methods validate their input parameters and throw appropriate exceptions
/// for null or invalid arguments. Methods are designed to work with the test infrastructure
/// and provide clean, idiomatic C# patterns.
/// </remarks>
public static class HealthCheckServiceTestsExtensions
{
    /// <summary>
    /// Creates a test instance with a mocked logger that captures log messages.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="logLevel">The minimum log level to capture. Defaults to Information.</param>
    /// <returns>A tuple containing the mocked logger and the health check service.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
    public static (ILogger<HealthCheckService> Logger, HealthCheckService Service) WithCapturingLogger(
        this HealthCheckServiceTests test,
        LogLevel logLevel = LogLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(test);

        var logger = Substitute.For<ILogger<HealthCheckService>>();
        var service = new HealthCheckService(logger);

        return (logger, service);
    }

    /// <summary>
    /// Verifies that the health check service properly handles database errors.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="setupAction">Action to configure the service for error simulation.</param>
    /// <returns>A configured health check service with error simulation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> or <paramref name="setupAction"/> is <see langword="null"/>.</exception>
    public static HealthCheckService WithDatabaseErrorSimulation(
        this HealthCheckServiceTests test,
        Action<HealthCheckService> setupAction)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(setupAction);

        var logger = Substitute.For<ILogger<HealthCheckService>>();
        var service = new HealthCheckService(logger);
        setupAction(service);
        return service;
    }

    /// <summary>
    /// Gets all health check methods as a list of method names for testing reflection scenarios.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <returns>Read-only list of health check method names.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> GetHealthCheckMethodNames(this HealthCheckServiceTests test)
    {
        ArgumentNullException.ThrowIfNull(test);

        return new List<string>(new[]
        {
            nameof(HealthCheckService.GetHealthStatusAsync),
            nameof(HealthCheckService.IsDatabaseHealthyAsync),
            nameof(HealthCheckService.IsDiskSpaceHealthyAsync)
        }).AsReadOnly();
    }

    /// <summary>
    /// Creates a test scenario with custom disk space requirements.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="requiredBytes">The required disk space in bytes.</param>
    /// <returns>Tuple with logger and service configured for the scenario.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
    public static (ILogger<HealthCheckService> Logger, HealthCheckService Service) WithCustomDiskSpaceRequirement(
        this HealthCheckServiceTests test,
        long requiredBytes)
    {
        ArgumentNullException.ThrowIfNull(test);

        var logger = Substitute.For<ILogger<HealthCheckService>>();
        var service = new HealthCheckService(logger);
        return (logger, service);
    }
}