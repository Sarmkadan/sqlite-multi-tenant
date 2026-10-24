using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SqliteMultiTenant.Cli;
using SqliteMultiTenant.Database;
using SqliteMultiTenant.Formatters;
using SqliteMultiTenant.Health;
using Xunit;

namespace SqliteMultiTenant.Tests;

public sealed class CommandExecutorExtensionsTests
{
    private readonly CommandExecutor _executor;

    public CommandExecutorExtensionsTests()
    {
        var tenantService = Substitute.For<SqliteMultiTenant.Services.ITenantService>();
        var backupService = Substitute.For<SqliteMultiTenant.Services.IBackupService>();
        var migrationService = Substitute.For<SqliteMultiTenant.Services.IMigrationService>();
        var healthService = new HealthCheckService(
            new NullLogger<HealthCheckService>(), "./databases");
        var connectionManager = new ConnectionManager(
            new NullLogger<ConnectionManager>());
        var logger = new NullLogger<CommandExecutor>();
        var formatter = new OutputFormatter();

        _executor = new CommandExecutor(
            tenantService, backupService, migrationService,
            healthService, connectionManager, logger, formatter);
    }

    [Fact]
    public async Task ExecuteWithSuccessMessageAsync_NullExecutor_ThrowsArgumentNullException()
    {
        // Arrange
        CommandExecutor executor = null!;
        var command = new ParsedCommand { Success = true, MainCommand = "health", Subcommand = "check" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.ExecuteWithSuccessMessageAsync(command, "done"));
    }

    [Fact]
    public async Task ExecuteWithSuccessMessageAsync_NullCommand_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecuteWithSuccessMessageAsync(null!));
    }

    [Fact]
    public async Task CreateTenantAsync_NullExecutor_ThrowsArgumentNullException()
    {
        // Arrange
        CommandExecutor executor = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.CreateTenantAsync("test-tenant"));
    }

    [Fact]
    public async Task CreateTenantAsync_EmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _executor.CreateTenantAsync(""));
    }

    [Fact]
    public async Task CreateTenantAsync_NullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.CreateTenantAsync(null!));
    }

    [Fact]
    public async Task ListTenantsAsync_NullExecutor_ThrowsArgumentNullException()
    {
        // Arrange
        CommandExecutor executor = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.ListTenantsAsync());
    }

    [Fact]
    public async Task CreateBackupAsync_EmptyDatabaseId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _executor.CreateBackupAsync(""));
    }

    [Fact]
    public async Task CheckPendingMigrationsAsync_NullExecutor_ThrowsArgumentNullException()
    {
        // Arrange
        CommandExecutor executor = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.CheckPendingMigrationsAsync("db-1"));
    }

    [Fact]
    public async Task ExecuteOrThrowAsync_NullCommand_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecuteOrThrowAsync(null!));
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_ZeroTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new ParsedCommand { Success = true, MainCommand = "health", Subcommand = "check" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _executor.ExecuteWithTimeoutAsync(command, 0));
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_NegativeTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new ParsedCommand { Success = true, MainCommand = "health", Subcommand = "check" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _executor.ExecuteWithTimeoutAsync(command, -5));
    }

    [Fact]
    public async Task CheckHealthAsync_NullExecutor_ThrowsArgumentNullException()
    {
        // Arrange
        CommandExecutor executor = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.CheckHealthAsync());
    }
}
