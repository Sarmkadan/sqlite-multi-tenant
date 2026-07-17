#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.DataOperations;

namespace SqliteMultiTenant.Tests;

public static class DataConsistencyCheckerTestsExtensions
{
    /// <summary>
    /// Creates a new DataConsistencyChecker instance with a mocked logger for testing purposes.
    /// </summary>
    /// <param name="test">The test instance to extend.</param>
    /// <param name="loggerMock">Optional logger mock. If null, a new NSubstitute mock will be created.</param>
    /// <returns>A new DataConsistencyChecker instance ready for testing.</returns>
    /// <exception cref="ArgumentNullException">Thrown if test is null.</exception>
    public static DataConsistencyChecker CreateChecker(
        this DataConsistencyCheckerTests test,
        ILogger<DataConsistencyChecker>? loggerMock = null)
    {
        ArgumentNullException.ThrowIfNull(test);

        var logger = loggerMock ?? Substitute.For<ILogger<DataConsistencyChecker>>();
        return new DataConsistencyChecker(logger);
    }

    /// <summary>
    /// Creates an in-memory SQLite connection for testing database integrity checks.
    /// </summary>
    /// <param name="test">The test instance to extend.</param>
    /// <param name="open">Whether to open the connection immediately. Defaults to <see langword="true"/>.</param>
    /// <returns>A new SQLiteConnection configured for in-memory testing.</returns>
    /// <exception cref="ArgumentNullException">Thrown if test is null.</exception>
    public static SQLiteConnection CreateMemoryConnection(
        this DataConsistencyCheckerTests test,
        bool open = true)
    {
        ArgumentNullException.ThrowIfNull(test);

        var connection = new SQLiteConnection("Data Source=:memory:");
        if (open)
        {
            connection.Open();
        }

        return connection;
    }

    /// <summary>
    /// Creates a connection string for a temporary file-based SQLite database.
    /// </summary>
    /// <param name="test">The test instance to extend.</param>
    /// <param name="dbName">Optional database name. If <see langword="null"/>, a GUID-based name will be generated.</param>
    /// <returns>A connection string for a temporary SQLite database file.</returns>
    /// <exception cref="ArgumentNullException">Thrown if test is null.</exception>
    public static string CreateTempConnectionString(
        this DataConsistencyCheckerTests test,
        string? dbName = null)
    {
        ArgumentNullException.ThrowIfNull(test);

        var name = dbName ?? $"temp_{Guid.NewGuid():N}.db";
        return $"Data Source={name};";
    }

    /// <summary>
    /// Verifies that a consistency check result indicates a healthy database state.
    /// </summary>
    /// <param name="test">The test instance to extend.</param>
    /// <param name="result">The consistency check result to verify.</param>
    /// <returns><see langword="true"/> if the database is healthy (all checks passed); otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if test or result is null.</exception>
    public static bool ShouldBeHealthy(
        this DataConsistencyCheckerTests test,
        ConsistencyCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(result);

        return result.IsHealthy;
    }
}