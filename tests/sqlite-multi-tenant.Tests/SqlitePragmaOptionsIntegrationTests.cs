#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Database;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Integration tests verifying that pragmas from <see cref="SqlitePragmaOptions"/> reach real
/// connections opened by <see cref="ConnectionPoolManager"/> and <see cref="ConnectionPool"/>.
/// Each test uses its own temporary database file, because WAL does not apply to in-memory databases.
/// </summary>
public sealed class SqlitePragmaOptionsIntegrationTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"pragma-{Guid.NewGuid():N}.db");
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlitePragmaOptionsIntegrationTests"/> class
    /// and derives the connection string from a fresh temporary database path.
    /// </summary>
    public SqlitePragmaOptionsIntegrationTests()
    {
        _connectionString = $"Data Source={_databasePath}";
    }

    [Fact]
    public async Task ConnectionPoolManager_AcquireAsync_PooledConnectionReportsWalJournalMode()
    {
        // Arrange
        await using IConnectionPoolManager manager = new ConnectionPoolManager(
            new ConnectionPoolOptions(), NullLogger<ConnectionPoolManager>.Instance);

        // Act
        var connection = await manager.AcquireAsync("tenant-wal", _connectionString);
        var journalMode = ReadPragma(connection, "journal_mode");
        await manager.ReleaseAsync("tenant-wal", connection);

        // Assert
        journalMode.Should().Be("wal");
    }

    [Fact]
    public async Task ConnectionPoolManager_GetStatistics_ExposesEffectivePragmas()
    {
        // Arrange
        await using IConnectionPoolManager manager = new ConnectionPoolManager(
            new ConnectionPoolOptions(), NullLogger<ConnectionPoolManager>.Instance);

        // Act
        var connection = await manager.AcquireAsync("tenant-stats", _connectionString);
        await manager.ReleaseAsync("tenant-stats", connection);
        var snapshot = manager.GetStatistics()["tenant-stats"];

        // Assert
        snapshot.Pragmas.Should().ContainKey("journal_mode").WhoseValue.Should().Be("wal");
        snapshot.Pragmas.Should().ContainKey("busy_timeout").WhoseValue.Should().Be("5000");
        snapshot.Pragmas.Should().ContainKey("foreign_keys").WhoseValue.Should().Be("1");
    }

    [Fact]
    public async Task ConnectionPool_AcquireAsync_DefaultPragmas_AppliesWalAndExposesEffectiveSet()
    {
        // Arrange
        await using var pool = new ConnectionPool(_connectionString, NullLogger<ConnectionPool>.Instance);

        // Act
        var connection = await pool.AcquireAsync();
        var journalMode = ReadPragma(connection, "journal_mode");
        pool.Release(connection);

        // Assert
        journalMode.Should().Be("wal");
        pool.EffectivePragmas["journal_mode"].Should().Be("wal");
    }

    [Fact]
    public async Task ConnectionPool_AcquireAsync_CustomPragmas_AppliesAndReportsEachValue()
    {
        // Arrange
        var pragmas = new SqlitePragmaOptions
        {
            JournalMode = "truncate",
            Synchronous = "FULL",
            BusyTimeoutMs = 1234,
            ForeignKeys = false,
        };
        await using var pool = new ConnectionPool(_connectionString, NullLogger<ConnectionPool>.Instance, pragmas: pragmas);

        // Act
        var connection = await pool.AcquireAsync();
        var busyTimeout = ReadPragma(connection, "busy_timeout");
        pool.Release(connection);

        // Assert
        pool.EffectivePragmas["journal_mode"].Should().Be("truncate");
        pool.EffectivePragmas["synchronous"].Should().Be("2");
        pool.EffectivePragmas["busy_timeout"].Should().Be("1234");
        pool.EffectivePragmas["foreign_keys"].Should().Be("0");
        busyTimeout.Should().Be("1234");
    }

    /// <summary>Runs a single PRAGMA read and returns its text value.</summary>
    private static string ReadPragma(SQLiteConnection connection, string name)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA {name}";
        return Convert.ToString(cmd.ExecuteScalar()) ?? string.Empty;
    }

    /// <summary>Deletes the temporary database and its WAL side files.</summary>
    public void Dispose()
    {
        foreach (var path in new[] { _databasePath, _databasePath + "-wal", _databasePath + "-shm" })
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
                // Best-effort cleanup of temp files; the OS reclaims them eventually.
            }
        }
    }
}
