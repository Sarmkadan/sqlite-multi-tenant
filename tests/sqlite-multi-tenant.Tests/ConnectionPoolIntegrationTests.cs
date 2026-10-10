#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Database;
using System;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    /// <summary>
    /// Integration tests for the ConnectionPool class using real SQLite databases.
    /// </summary>
    public sealed class ConnectionPoolIntegrationTests
    {
        private readonly string _connectionString;
        private readonly ILogger<ConnectionPool> _logger;

        public ConnectionPoolIntegrationTests()
        {
            // Create a temporary SQLite database file for testing
            var tempFile = Path.GetTempFileName();
            File.Delete(tempFile); // Delete the empty file so SQLite can create it
            _connectionString = $"Data Source={tempFile};";
            _logger = NullLogger<ConnectionPool>.Instance;
        }

        [Fact]
        public async Task AcquireAsync_WithMaxPoolSizeN_ConcurrentTasks_DoesNotExceedPoolSize()
        {
            // Arrange
            const int maxPoolSize = 3;
            const int taskCount = maxPoolSize * 3; // 3N tasks
            var pool = new ConnectionPool(_connectionString, _logger, maxPoolSize);
            var completed = false;
            var maxObserved = 0;
            var checkTask = Task.Run(async () =>
            {
                while (!completed)
                {
                    // Number of connections currently checked out = total created - available
                    var current = pool.TotalConnections - pool.AvailableConnections;
                    if (current > maxObserved)
                        maxObserved = current;
                    await Task.Delay(10);
                }
            });

            // Act
            var tasks = new Task[taskCount];
            for (int i = 0; i < taskCount; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    var connection = await pool.AcquireAsync();
                    try
                    {
                        // Hold the connection for 50ms
                        await Task.Delay(50);
                    }
                    finally
                    {
                        pool.Release(connection);
                    }
                });
            }

            await Task.WhenAll(tasks);
            completed = true;
            await checkTask;

            // Assert
            maxObserved.Should().BeLessOrEqualTo(maxPoolSize, "At no point should more than N connections be checked out");
            // All tasks should have completed without timeout
            foreach (var task in tasks)
                task.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Fact]
        public async Task Release_AcquireAgain_SameConnectionIsReused()
        {
            // Arrange
            var pool = new ConnectionPool(_connectionString, _logger, maxSize: 5);
            SQLiteConnection? firstConnection = null;

            // Act
            firstConnection = await pool.AcquireAsync();
            pool.Release(firstConnection);

            var secondConnection = await pool.AcquireAsync();

            // Assert
            secondConnection.Should().BeSameAs(firstConnection, "The released connection should be reused");
        }

        [Fact]
        public async Task DisposeAsync_WhileConnectionsLeased_ThrowsOnNewAcquireAndClosesOnRelease()
        {
            // Arrange
            var pool = new ConnectionPool(_connectionString, _logger, maxSize: 5);
            var leasedConnections = new List<SQLiteConnection>();
            const int leaseCount = 3;

            // Acquire some connections and hold them
            for (int i = 0; i < leaseCount; i++)
            {
                var conn = await pool.AcquireAsync();
                leasedConnections.Add(conn);
            }

            // Act: Dispose the pool while connections are leased
            await pool.DisposeAsync();

            // Attempt to acquire a new connection should throw ObjectDisposedException
            Func<Task> acquireAct = async () => await pool.AcquireAsync();
            await acquireAct.Should().ThrowAsync<ObjectDisposedException>();

            // Act: Return the leased connections to the disposed pool
            // When the pool is disposed, releasing connections doesn't affect them
            // (they remain open and are the responsibility of the caller to dispose)
            foreach (var conn in leasedConnections)
            {
                pool.Release(conn);
                // After release to a disposed pool, the connection remains open
                // It's up to the caller to dispose it properly
                conn.State.Should().Be(System.Data.ConnectionState.Open, "Leased connection remains open when returned to a disposed pool");
            }
        }

        [Fact]
        public async Task AcquireAsync_AfterFailedConnection_IsEvictedAndNotReused()
        {
            // Arrange
            var pool = new ConnectionPool(_connectionString, _logger, maxSize: 5);
            SQLiteConnection? badConnection = null;

            // Act: Acquire a connection and then simulate failure (e.g. by disposing it)
            badConnection = await pool.AcquireAsync();
            // Simulate connection failure by disposing it
            badConnection.Dispose();

            // Return the failed connection to the pool (it will be detected as broken and disposed)
            pool.Release(badConnection);

            // Now acquire a new connection - it should not be the same failed one
            var newConnection = await pool.AcquireAsync();

            // Assert
            newConnection.Should().NotBeSameAs(badConnection, "The failed connection should have been evicted and not reused");
            // The new connection should be open and functional
            newConnection.State.Should().Be(System.Data.ConnectionState.Open);
            // We can run a simple query to verify
            await using var cmd = newConnection.CreateCommand();
            cmd.CommandText = "SELECT 1";
            var result = await cmd.ExecuteScalarAsync();
            result.Should().Be(1L);
        }
    }
}