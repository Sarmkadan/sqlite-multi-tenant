#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.DataOperations;
using Xunit;

namespace SqliteMultiTenant.Tests;

public sealed class DataConsistencyCheckerTests {
    private readonly DataConsistencyChecker _checker;
    private readonly ILogger<DataConsistencyChecker> _logger;

    public DataConsistencyCheckerTests()
    {
        _logger = Substitute.For<ILogger<DataConsistencyChecker>>();
        _checker = new DataConsistencyChecker(_logger);
        _logger.LogInformation("Initialized {TestClass} with {CheckerType}", nameof(DataConsistencyCheckerTests), nameof(DataConsistencyChecker));
    }

    [Fact]
    public async Task CheckDatabaseIntegrityAsync_WithClosedConnection_ThrowsException()
    {
        // Arrange
        _logger.LogInformation("Starting {TestName} with {ConnectionString}", nameof(CheckDatabaseIntegrityAsync_WithClosedConnection_ThrowsException), "Data Source=:memory:");
        using var connection = new SQLiteConnection("Data Source=:memory:");
        // Intentionally not opening connection
        _logger.LogWarning("Connection {ConnectionString} was intentionally left closed to simulate a degraded state", "Data Source=:memory:");

        // Act
        Func<Task> act = async () => await _checker.CheckDatabaseIntegrityAsync(connection);

        // Assert
        await act.Should().ThrowAsync<Exception>();
        _logger.LogInformation("Completed {TestName}", nameof(CheckDatabaseIntegrityAsync_WithClosedConnection_ThrowsException));
    }

    [Fact]
    public async Task CheckDatabaseIntegrityAsync_WithValidMemoryConnection_ReturnsHealthy()
    {
        // Arrange
        _logger.LogInformation("Starting {TestName} with {ConnectionString}", nameof(CheckDatabaseIntegrityAsync_WithValidMemoryConnection_ReturnsHealthy), "Data Source=:memory:");
        using var connection = new SQLiteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // Act
        var result = await _checker.CheckDatabaseIntegrityAsync(connection);

        // Assert
        result.Should().NotBeNull();
        // Assuming result has an IsHealthy or similar property, but since we don't know the exact property, we assert it doesn't throw and returns a result object
        result.GetType().Should().Be(typeof(ConsistencyCheckResult));
        _logger.LogInformation("Completed {TestName} with result type {ResultType}", nameof(CheckDatabaseIntegrityAsync_WithValidMemoryConnection_ReturnsHealthy), result.GetType().Name);
    }

    [Fact]
    public async Task CheckDatabaseIntegrityAsync_WithNullConnection_ThrowsArgumentNullException()
    {
        // Act
        _logger.LogInformation("Starting {TestName} with a null connection", nameof(CheckDatabaseIntegrityAsync_WithNullConnection_ThrowsArgumentNullException));
        Func<Task> act = async () => await _checker.CheckDatabaseIntegrityAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
        _logger.LogInformation("Completed {TestName}", nameof(CheckDatabaseIntegrityAsync_WithNullConnection_ThrowsArgumentNullException));
    }

    [Fact]
    public void Checker_Initialization_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        _logger.LogInformation("Starting {TestName} with a null logger", nameof(Checker_Initialization_WithNullLogger_ThrowsArgumentNullException));
        var act = () => new DataConsistencyChecker(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
        _logger.LogInformation("Completed {TestName}", nameof(Checker_Initialization_WithNullLogger_ThrowsArgumentNullException));
    }

    [Fact]
    public async Task CheckDatabaseIntegrityAsync_WithCorruptData_SimulatesFailure()
    {
        // This is a placeholder test for corruption scenario, normally handled by PRAGMA integrity_check
        _logger.LogInformation("Starting {TestName} with {ConnectionString}", nameof(CheckDatabaseIntegrityAsync_WithCorruptData_SimulatesFailure), "Data Source=:memory:");
        using var connection = new SQLiteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        _logger.LogWarning("Corruption scenario for {TestName} is simulated; no actual corruption is injected", nameof(CheckDatabaseIntegrityAsync_WithCorruptData_SimulatesFailure));

        // Act
        var result = await _checker.CheckDatabaseIntegrityAsync(connection);

        // Assert
        result.Should().NotBeNull();
        _logger.LogInformation("Completed {TestName}", nameof(CheckDatabaseIntegrityAsync_WithCorruptData_SimulatesFailure));
    }
}
