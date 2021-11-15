using System;
using SqliteMultiTenant.DataOperations;

namespace SqliteMultiTenant.Benchmarks;

/// <summary>
/// Extension methods for QueryBuilderBenchmarks that build upon the base query builder methods
/// to create more complex SQL queries for benchmarking purposes.
/// </summary>
public static class QueryBuilderBenchmarksExtensions
{
    /// <summary>
    /// Adds pagination parameters to a query built by SelectWithOrderAndLimit using parameterized queries.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A SQL query string with pagination clauses.</returns>
    /// <exception cref="ArgumentNullException">Thrown if benchmarks is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if pageNumber is less than 1 or pageSize is less than 1.</exception>
    public static string AddPagination(this QueryBuilderBenchmarks benchmarks, int pageNumber, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var baseQuery = benchmarks.SelectWithOrderAndLimit();
        var offset = (pageNumber - 1) * pageSize;
        return $"{baseQuery} LIMIT @pageSize OFFSET @offset";
    }

    /// <summary>
    /// Adds a WHERE clause with status filter to a simple select query using parameterized queries.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="statusFilter">The status value to filter by.</param>
    /// <returns>A SQL query string with the status filter applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown if benchmarks is null.</exception>
    /// <exception cref="ArgumentException">Thrown if statusFilter is null or whitespace.</exception>
    public static string AddFilterWhere(this QueryBuilderBenchmarks benchmarks, string statusFilter)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentException.ThrowIfNullOrEmpty(statusFilter, nameof(statusFilter));

        var baseQuery = benchmarks.SimpleSelect();
        return $"{baseQuery} WHERE Status = @statusFilter";
    }

    /// <summary>
    /// Adds an index hint to a join query for benchmarking index performance.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="indexName">The name of the index to use.</param>
    /// <returns>A SQL query string with the index hint applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown if benchmarks is null.</exception>
    /// <exception cref="ArgumentException">Thrown if indexName is null or whitespace.</exception>
    public static string AddIndexHint(this QueryBuilderBenchmarks benchmarks, string indexName)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentException.ThrowIfNullOrEmpty(indexName, nameof(indexName));

        var baseQuery = benchmarks.SelectWithJoin();
        return $"{baseQuery} INDEXED BY @indexName";
    }
}
