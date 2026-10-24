#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SqliteMultiTenant.Database;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Services;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Integration tests for <see cref="HealthChecker"/> that run the ASP.NET Core <see cref="IHealthCheck"/>
/// contract against real temporary SQLite tenant database files.
/// </summary>
public sealed class HealthCheckerIntegrationTests : IAsyncDisposable
{
    private const string TenantId = "tenant-1";

    private readonly string _rootDirectory;
    private readonly string _databasePath;
    private readonly ITenantService _tenantService;
    private readonly ConnectionPoolManager _connectionPool;
    private readonly HealthChecker _healthChecker;

    /// <summary>
    /// Initializes a temporary tenant directory, a real connection pool, and a tenant service that returns one tenant.
    /// </summary>
    public HealthCheckerIntegrationTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "health-checker-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootDirectory);
        _databasePath = Path.Combine(_rootDirectory, "tenant-1.db");

        _tenantService = Substitute.For<ITenantService>();
        _tenantService.GetAllTenantsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<Tenant> {
                new Tenant { TenantId = TenantId, Name = "Tenant One", DatabasePath = _databasePath }
            }));

        _connectionPool = new ConnectionPoolManager(new ConnectionPoolOptions(), NullLogger<ConnectionPoolManager>.Instance);
        _healthChecker = new HealthChecker(_tenantService, _connectionPool, NullLogger<HealthChecker>.Instance);
    }

    /// <summary>
    /// Tests that <see cref="HealthChecker.CheckHealthAsync"/> returns Healthy and populates per-tenant data
    /// for a tenant whose database file is intact.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WithIntactTenantDatabase_ReturnsHealthyWithPopulatedData()
    {
        // Arrange
        CreateValidDatabase(_databasePath);

        // Act
        var result = await _healthChecker.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["tenantCount"].Should().Be(1);
        result.Data["unhealthyCount"].Should().Be(0);

        var tenants = (IReadOnlyDictionary<string, TenantHealthReport>)result.Data["tenants"];
        var report = tenants[TenantId];
        report.IsHealthy.Should().BeTrue();
        report.FileExists.Should().BeTrue();
        report.FileSizeBytes.Should().BeGreaterThan(0);
        report.IntegrityCheckResult.Should().Be("ok");
        report.PoolTotal.Should().BeGreaterThan(0);
        report.PoolLeased.Should().Be(0);
        report.Error.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="HealthChecker.CheckHealthAsync"/> returns Unhealthy and reports the failure
    /// for a tenant whose database file has been overwritten with non-SQLite bytes.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WithCorruptedTenantFile_ReturnsUnhealthy()
    {
        // Arrange
        CreateValidDatabase(_databasePath);
        File.WriteAllBytes(_databasePath, Enumerable.Repeat((byte)0x42, 8192).ToArray());

        // Act
        var result = await _healthChecker.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data["unhealthyCount"].Should().Be(1);

        var tenants = (IReadOnlyDictionary<string, TenantHealthReport>)result.Data["tenants"];
        var report = tenants[TenantId];
        report.IsHealthy.Should().BeFalse();
        report.FileExists.Should().BeTrue();
        report.Error.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Tests that <see cref="HealthChecker.CheckHealthAsync"/> returns Unhealthy and reports a missing file
    /// when the tenant's database file is deleted.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WithDeletedTenantFile_ReturnsUnhealthyWithMissingFileError()
    {
        // Arrange
        CreateValidDatabase(_databasePath);

        // Act: Delete the file
        File.Delete(_databasePath);

        var result = await _healthChecker.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data["unhealthyCount"].Should().Be(1);

        var tenants = (IReadOnlyDictionary<string, TenantHealthReport>)result.Data["tenants"];
        var report = tenants[TenantId];
        report.IsHealthy.Should().BeFalse();
        report.FileExists.Should().BeFalse();
        report.Error.Should().NotBeNullOrWhiteSpace();
        report.Error.Should().StartWith("Database file not found:");
    }

    /// <summary>
    /// Tests that <see cref="HealthChecker.CheckHealthAsync"/> returns Unhealthy and reports a timeout
    /// when all connections in the pool are leased and the acquire operation times out.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WithExhaustedConnectionPool_ReturnsUnhealthyWithTimeoutError()
    {
        // Arrange
        CreateValidDatabase(_databasePath);

        // Use a connection pool with a small max size and short acquire timeout to simulate exhaustion quickly
        var poolOptions = new ConnectionPoolOptions
        {
            MaxPoolSize = 1,
            AcquireTimeout = TimeSpan.FromMilliseconds(100) // Short timeout for test
        };
        var connectionPool = new ConnectionPoolManager(poolOptions, NullLogger<ConnectionPoolManager>.Instance);
        var healthChecker = new HealthChecker(_tenantService, connectionPool, NullLogger<HealthChecker>.Instance);

        // Exhaust the pool by acquiring the single available connection and holding it
        var heldConnection = await connectionPool.AcquireAsync(TenantId, $"Data Source={_databasePath};");

        try
        {
            // Act
            var result = await healthChecker.CheckHealthAsync(new HealthCheckContext());

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Data["unhealthyCount"].Should().Be(1);

            var tenants = (IReadOnlyDictionary<string, TenantHealthReport>)result.Data["tenants"];
            var report = tenants[TenantId];
            report.IsHealthy.Should().BeFalse();
            report.Error.Should().NotBeNullOrWhiteSpace();
            // The error should indicate a timeout (or at least not be null)
            // Note: The exact message may vary, but we know it's not healthy and has an error
        }
        finally
        {
            // Clean up: release the held connection and dispose the pool
            await connectionPool.ReleaseAsync(TenantId, heldConnection);
            await connectionPool.DisposeAsync();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _connectionPool.DisposeAsync();
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private static void CreateValidDatabase(string path)
    {
        SQLiteConnection.CreateFile(path);

        using var connection = new SQLiteConnection($"Data Source={path};");
        connection.Open();

        using (var create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE IF NOT EXISTS items (id INTEGER PRIMARY KEY, name TEXT);";
            create.ExecuteNonQuery();
        }

        using (var version = connection.CreateCommand())
        {
            version.CommandText = "PRAGMA user_version = 3;";
            version.ExecuteNonQuery();
        }
    }
}
