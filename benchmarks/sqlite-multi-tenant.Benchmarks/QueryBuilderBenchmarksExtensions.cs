using SqliteMultiTenant.DataOperations;

namespace SqliteMultiTenant.Benchmarks;

public static class QueryBuilderBenchmarksExtensions
{
    /// <summary>
    /// Adds pagination parameters to a query built by SelectWithOrderAndLimit
    /// </summary>
    public static string AddPagination(this QueryBuilderBenchmarks benchmarks, int pageNumber, int pageSize)
    {
        var baseQuery = benchmarks.SelectWithOrderAndLimit();
        return $"{baseQuery} LIMIT {pageSize} OFFSET {(pageNumber - 1) * pageSize}";
    }

    /// <summary>
    /// Adds a WHERE clause with status filter to a simple select query
    /// </summary>
    public static string AddFilterWhere(this QueryBuilderBenchmarks benchmarks, string statusFilter)
    {
        var baseQuery = benchmarks.SimpleSelect();
        return $"{baseQuery} WHERE Status = '{statusFilter}'";
    }

    /// <summary>
    /// Adds an index hint to a join query for benchmarking index performance
    /// </summary>
    public static string AddIndexHint(this QueryBuilderBenchmarks benchmarks, string indexName)
    {
        var baseQuery = benchmarks.SelectWithJoin();
        return $"{baseQuery} INDEXED BY {indexName}";
    }
}
