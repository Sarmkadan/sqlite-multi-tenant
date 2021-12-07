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
    public sealed class ConnectionManagerIntegrationTests : IDisposable {
        private readonly ILogger<ConnectionManager> _mockLogger;
        private readonly ConnectionManager _connectionManager;
        private const string TenantId1 = "tenant1";
        private const string TenantId2 = "tenant2";
        private const string ConnectionString1 = "Data Source=:memory:;Mode=Memory;Cache=Shared";
        private const string ConnectionString2 = "Data Source=:memory:;Mode=Memory;Cache=Shared";

        public ConnectionManagerIntegrationTests()
        {
            _mockLogger = Substitute.For<ILogger<ConnectionManager>>();
            _connectionManager = new ConnectionManager(_mockLogger, maxConnectionsPerPool: 2);
        }

        [Fact]
        public async Task GetConnectionAsync_ShouldReturnOpenConnection()
        {
            // Arrange
            // Act
            var connection = await _connectionManager.GetConnectionAsync(TenantId1, ConnectionString1);

            // Assert
            connection.Should().NotBeNull();
            connection.State.Should().Be(System.Data.ConnectionState.Open);

            // Cleanup
            await _connectionManager.ReleaseConnectionAsync(TenantId1, connection);
        }

        [Fact]
        public async Task GetConnectionAsync_ShouldReuseConnection_WhenAvailable()
        {
            // Arrange
            var connection1 = await _connectionManager.GetConnectionAsync(TenantId1, ConnectionString1);
            await _connectionManager.ReleaseConnectionAsync(TenantId1, connection1);

            // Act
            var connection2 = await _connectionManager.GetConnectionAsync(TenantId1, ConnectionString1);

            // Assert
            connection2.Should().Be(connection1); // Should be the same instance from the pool
            connection2.State.Should().Be(System.Data.ConnectionState.Open);

            // Cleanup
            await _connectionManager.ReleaseConnectionAsync(TenantId1, connection2);
        }

        [Fact]
        public async Task GetConnectionAsync_ShouldCreateNewConnection_WhenPoolNotFull()
        {
            // Arrange
            var connection1 = await _connectionManager.GetConnectionAsync(TenantId1, ConnectionString1); // Uses 1st slot
            var initialStats = _connectionManager.GetPoolStatistics();
            initialStats[TenantId1].TotalConnections.Should().Be(1);
            initialStats[TenantId1].AvailableConnections.Should().Be(0);

            // Act
            var connection2 = await _connectionManager.GetConnectionAsync(TenantId1, ConnectionString1); // Uses 2nd slot

            // Assert
            connection2.Should().NotBeNull();
            connection2.Should().NotBe(connection1); // Should be a new connection
            connection2.State.Should().Be(System.Data.ConnectionState.Open);

            var finalStats = _connectionManager.GetPoolStatistics();
            finalStats[TenantId1].TotalConnections.Should().Be(2);
            finalStats[TenantId1].AvailableConnections.Should().Be(0);

            // Cleanup
            await _connectionManager.ReleaseConnectionAsync(TenantId1, connection1);
            await _connectionManager.ReleaseConnectionAsync(TenantId1, connection2);
        }

        [Fact]
        public async Task ReleaseConnectionAsync_ShouldReturnConnectionToPool()
        {
            // Arrange
            var connection = await _connectionManager.GetConnectionAsync(TenantId1, ConnectionString1);
            var initialStats = _connectionManager.GetPoolStatistics();
            initialStats[TenantId1].AvailableConnections.Should().Be(0);

            // Act
            await _connectionManager.ReleaseConnectionAsync(TenantId1, connection);

            // Assert
            var finalStats = _connectionManager.GetPoolStatistics();
            finalStats[TenantId1].AvailableConnections.Should().Be(1);
        }

        [Fact]
        public async Task ClearTenantPoolAsync_ShouldRemoveTenantPoolAndDisposeConnections()
        {
            // Arrange
            var connection = await _connectionManager.GetConnectionAsync(TenantId1, ConnectionString1);
            await _connectionManager.ReleaseConnectionAsync(TenantId1, connection);
            _connectionManager.GetPoolStatistics().Should().ContainKey(TenantId1);

            // Act
            await _connectionManager.ClearTenantPoolAsync(TenantId1);

            // Assert
            _connectionManager.GetPoolStatistics().Should().NotContainKey(TenantId1);
            // System.Data.SQLite fully disposes the native handle on Dispose(), so the
            // connection object itself becomes unusable afterwards (State access throws)
            // rather than reporting ConnectionState.Closed like some other ADO.NET providers.
            this.Invoking(_ => connection.State)
                .Should().Throw<ObjectDisposedException>();
            _mockLogger.AssertLogged(LogLevel.Information, 1, "Connection pool cleared for tenant: {TenantId}", TenantId1);
        }

        [Fact]
        public async Task GetPoolStatistics_ShouldReturnCorrectStats()
        {
            // Arrange
            var connection1 = await _connectionManager.GetConnectionAsync(TenantId1, ConnectionString1);
            await _connectionManager.GetConnectionAsync(TenantId2, ConnectionString2);
            await _connectionManager.ReleaseConnectionAsync(TenantId1, connection1); // 1 available, 1 total for tenant1
                                                                                  // 0 available, 1 total for tenant2 (not released yet)

            // Act
            var stats = _connectionManager.GetPoolStatistics();

            // Assert
            stats.Should().ContainKey(TenantId1);
            stats[TenantId1].AvailableConnections.Should().Be(1);
            stats[TenantId1].TotalConnections.Should().Be(1);
            stats[TenantId1].WaitingRequests.Should().Be(0);

            stats.Should().ContainKey(TenantId2);
            stats[TenantId2].AvailableConnections.Should().Be(0);
            stats[TenantId2].TotalConnections.Should().Be(1);
            stats[TenantId2].WaitingRequests.Should().Be(0);
        }

        [Fact]
        public async Task GetConnectionAsync_ShouldThrowArgumentNullException_WhenTenantIdIsNull()
        {
            // Arrange
            string tenantId = null;

            // Act
            Func<Task> act = async () => await _connectionManager.GetConnectionAsync(tenantId, ConnectionString1);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'tenantId')");
        }

        [Fact]
        public async Task GetConnectionAsync_ShouldThrowArgumentNullException_WhenConnectionStringIsNull()
        {
            // Arrange
            string connectionString = null;

            // Act
            Func<Task> act = async () => await _connectionManager.GetConnectionAsync(TenantId1, connectionString);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'connectionString')");
        }

        [Fact]
        public async Task GetConnectionAsync_ShouldRespectMaxConnectionsPerPool()
        {
            // Arrange - Max connections per pool is 2
            var connections = new List<SQLiteConnection>();
            connections.Add(await _connectionManager.GetConnectionAsync(TenantId1, ConnectionString1)); // Connection 1
            connections.Add(await _connectionManager.GetConnectionAsync(TenantId1, ConnectionString1)); // Connection 2

            // Act - Try to get a third connection, which should block
            var task = _connectionManager.GetConnectionAsync(TenantId1, ConnectionString1);

            // Assert that it does not complete immediately
            task.IsCompleted.Should().BeFalse();

            // Release one connection
            await _connectionManager.ReleaseConnectionAsync(TenantId1, connections[0]);

            // Now the task should complete
            var connection3 = await task;
            connection3.Should().NotBeNull();
            connection3.State.Should().Be(System.Data.ConnectionState.Open);

            var stats = _connectionManager.GetPoolStatistics();
            stats[TenantId1].TotalConnections.Should().Be(2); // Still 2 total, one was reused
            stats[TenantId1].AvailableConnections.Should().Be(0);

            // Cleanup
            await _connectionManager.ReleaseConnectionAsync(TenantId1, connections[1]);
            await _connectionManager.ReleaseConnectionAsync(TenantId1, connection3);
        }

        public void Dispose()
        {
            _connectionManager.Dispose();
        }
    }
}
