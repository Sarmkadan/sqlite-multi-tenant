#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Database;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public sealed class ConnectionPoolManagerIntegrationTests : IAsyncDisposable {
        private readonly ILogger<ConnectionPoolManager> _mockLogger;
        private ConnectionPoolOptions _options;
        private IConnectionPoolManager _connectionPoolManager;
        private const string TenantId1 = "tenant1";
        private const string ConnectionString1 = "Data Source=:memory:connectionpoolmanager1;Mode=Memory;Cache=Shared";
        private const string TenantId2 = "tenant2";
        private const string ConnectionString2 = "Data Source=:memory:connectionpoolmanager2;Mode=Memory;Cache=Shared";

        public ConnectionPoolManagerIntegrationTests()
        {
            _mockLogger = Substitute.For<ILogger<ConnectionPoolManager>>();
            _options = new ConnectionPoolOptions
            {
                MaxPoolSize = 2,
                MinPoolSize = 0,
                AcquireTimeout = TimeSpan.FromSeconds(1),
                IdleTimeout = TimeSpan.FromMilliseconds(100), // Short idle timeout for testing pruning
                MaxConnectionLifetime = TimeSpan.FromSeconds(10), // Short lifetime for testing pruning
                PruneInterval = TimeSpan.FromMilliseconds(50) // Short prune interval for testing
            };
            _connectionPoolManager = new ConnectionPoolManager(_options, _mockLogger);
        }

        [Fact]
        public async Task AcquireAsync_ShouldReturnOpenConnection()
        {
            // Act
            var connection = await _connectionPoolManager.AcquireAsync(TenantId1, ConnectionString1);

            // Assert
            connection.Should().NotBeNull();
            connection.State.Should().Be(System.Data.ConnectionState.Open);

            // Cleanup
            await _connectionPoolManager.ReleaseAsync(TenantId1, connection);
        }

        [Fact]
        public async Task AcquireAsync_ShouldReuseConnection_WhenAvailable()
        {
            // Arrange
            var connection1 = await _connectionPoolManager.AcquireAsync(TenantId1, ConnectionString1);
            await _connectionPoolManager.ReleaseAsync(TenantId1, connection1);

            // Act
            var connection2 = await _connectionPoolManager.AcquireAsync(TenantId1, ConnectionString1);

            // Assert
            connection2.Should().Be(connection1);
            connection2.State.Should().Be(System.Data.ConnectionState.Open);

            // Cleanup
            await _connectionPoolManager.ReleaseAsync(TenantId1, connection2);
        }

        [Fact]
        public async Task AcquireAsync_ShouldCreateNewConnection_WhenPoolNotFull()
        {
            // Arrange
            var connection1 = await _connectionPoolManager.AcquireAsync(TenantId1, ConnectionString1);
            
            // Act
            var connection2 = await _connectionPoolManager.AcquireAsync(TenantId1, ConnectionString1);

            // Assert
            connection2.Should().NotBeNull();
            connection2.Should().NotBe(connection1); // Should be a new connection
            connection2.State.Should().Be(System.Data.ConnectionState.Open);

            // Cleanup
            await _connectionPoolManager.ReleaseAsync(TenantId1, connection1);
            await _connectionPoolManager.ReleaseAsync(TenantId1, connection2);
        }

        [Fact]
        public async Task ReleaseAsync_ShouldReturnConnectionToPool()
        {
            // Arrange
            var connection = await _connectionPoolManager.AcquireAsync(TenantId1, ConnectionString1);
            var statsBefore = _connectionPoolManager.GetStatistics();
            statsBefore[TenantId1].Available.Should().Be(0);

            // Act
            await _connectionPoolManager.ReleaseAsync(TenantId1, connection);

            // Assert
            var statsAfter = _connectionPoolManager.GetStatistics();
            statsAfter[TenantId1].Available.Should().Be(1);
        }

        [Fact]
        public async Task EvictTenantAsync_ShouldRemoveTenantPoolAndDisposeConnections()
        {
            // Arrange
            var connection = await _connectionPoolManager.AcquireAsync(TenantId1, ConnectionString1);
            await _connectionPoolManager.ReleaseAsync(TenantId1, connection);
            _connectionPoolManager.GetStatistics().Should().ContainKey(TenantId1);

            // Act
            await _connectionPoolManager.EvictTenantAsync(TenantId1);

            // Assert
            _connectionPoolManager.GetStatistics().Should().NotContainKey(TenantId1);
            // System.Data.SQLite fully disposes the native handle on Dispose(), so the
            // connection object itself becomes unusable afterwards (State access throws)
            // rather than reporting ConnectionState.Closed like some other ADO.NET providers.
            this.Invoking(_ => connection.State)
                .Should().Throw<ObjectDisposedException>();
            _mockLogger.AssertLogged(LogLevel.Information, 1, "Connection pool evicted for tenant {TenantId}", TenantId1);
        }

        [Fact]
        public async Task GetStatistics_ShouldReturnCorrectStats()
        {
            // Arrange
            var conn1 = await _connectionPoolManager.AcquireAsync(TenantId1, ConnectionString1);
            var conn2 = await _connectionPoolManager.AcquireAsync(TenantId2, ConnectionString2);
            await _connectionPoolManager.ReleaseAsync(TenantId1, conn1);

            // Act
            var stats = _connectionPoolManager.GetStatistics();

            // Assert
            stats.Should().ContainKey(TenantId1);
            stats[TenantId1].Available.Should().Be(1);
            stats[TenantId1].Total.Should().Be(1);
            stats[TenantId1].Waiting.Should().Be(0);

            stats.Should().ContainKey(TenantId2);
            stats[TenantId2].Available.Should().Be(0);
            stats[TenantId2].Total.Should().Be(1);
            stats[TenantId2].Waiting.Should().Be(0);

            // Cleanup
            await _connectionPoolManager.ReleaseAsync(TenantId2, conn2);
        }

        [Fact]
        public async Task AcquireAsync_ShouldThrowTimeoutException_WhenPoolIsExhausted()
        {
            // Arrange - MaxPoolSize is 2
            var conn1 = await _connectionPoolManager.AcquireAsync(TenantId1, ConnectionString1);
            var conn2 = await _connectionPoolManager.AcquireAsync(TenantId1, ConnectionString1);

            // Act
            Func<Task> act = async () => await _connectionPoolManager.AcquireAsync(TenantId1, ConnectionString1);

            // Assert
            await act.Should().ThrowAsync<TimeoutException>()
                .WithMessage("No connection became available within 1s. Consider increasing MaxPoolSize (current: 2).");

            // Cleanup
            await _connectionPoolManager.ReleaseAsync(TenantId1, conn1);
            await _connectionPoolManager.ReleaseAsync(TenantId1, conn2);
        }

        [Fact]
        public async Task AcquireAsync_ShouldThrowArgumentException_WhenTenantIdIsEmpty()
        {
            // Arrange
            string tenantId = "";

            // Act
            Func<Task> act = async () => await _connectionPoolManager.AcquireAsync(tenantId, ConnectionString1);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("The value cannot be an empty string. (Parameter 'tenantId')");
        }

        [Fact]
        public async Task AcquireAsync_ShouldThrowArgumentException_WhenConnectionStringIsEmpty()
        {
            // Arrange
            string connectionString = "";

            // Act
            Func<Task> act = async () => await _connectionPoolManager.AcquireAsync(TenantId1, connectionString);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("The value cannot be an empty string. (Parameter 'connectionString')");
        }
        
        [Fact]
        public async Task PruneIdle_ShouldDisposeIdleConnections()
        {
            // Arrange
            _options.IdleTimeout = TimeSpan.FromMilliseconds(50); // Set a short idle timeout
            _connectionPoolManager = new ConnectionPoolManager(_options, _mockLogger); // Re-initialize with new options

            var conn1 = await _connectionPoolManager.AcquireAsync(TenantId1, ConnectionString1);
            var conn2 = await _connectionPoolManager.AcquireAsync(TenantId1, ConnectionString1);

            await _connectionPoolManager.ReleaseAsync(TenantId1, conn1);
            await _connectionPoolManager.ReleaseAsync(TenantId1, conn2);

            // Connections are now idle. Wait for more than IdleTimeout + PruneInterval
            await Task.Delay(TimeSpan.FromMilliseconds(_options.IdleTimeout.TotalMilliseconds + _options.PruneInterval.TotalMilliseconds + 50));

            // Act: Pruning happens in background, so just check state after delay
            var stats = _connectionPoolManager.GetStatistics();

            // Assert
            stats[TenantId1].Total.Should().Be(0);
            stats[TenantId1].Available.Should().Be(0);
            _mockLogger.AssertLoggedContains(LogLevel.Debug, 1, "Pruned 2 idle connection(s) from tenant pool");
        }

        public async ValueTask DisposeAsync()
        {
            if (_connectionPoolManager is not null)
            {
                await _connectionPoolManager.DisposeAsync();
            }
        }
    }
}
