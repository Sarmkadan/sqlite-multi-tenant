// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using SqliteMultiTenant.DataOperations;

namespace SqliteMultiTenant.Benchmarks;

/// <summary>
/// Measures SQL generation overhead in the QueryBuilder — exercised on every
/// repository call that resolves tenants, migrations, or backups.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev")]
public class QueryBuilderBenchmarks
{
    private QueryBuilder _builder = null!;

    [GlobalSetup]
    public void Setup() => _builder = new QueryBuilder("Tenants");

    [Benchmark(Baseline = true)]
    public string SimpleSelect()
    {
        _builder.Reset();
        return _builder
            .Select("TenantId", "Name", "Status", "CreatedAt")
            .Where("Status = @status", ("status", (object)"Active"))
            .Build();
    }

    [Benchmark]
    public string SelectWithOrderAndLimit()
    {
        _builder.Reset();
        return _builder
            .Select("TenantId", "Name", "Status", "CreatedAt", "UpdatedAt")
            .Where("Status = @status", ("status", (object)"Active"))
            .OrderBy("CreatedAt", "DESC")
            .Limit(50)
            .Offset(100)
            .Build();
    }

    [Benchmark]
    public string SelectWithJoin()
    {
        _builder.Reset();
        return _builder
            .Select("TenantId", "Name")
            .InnerJoin("TenantDatabases", "Tenants.TenantId = TenantDatabases.TenantId")
            .Where("Tenants.Status = @status", ("status", (object)"Active"))
            .Build();
    }

    [Benchmark]
    public string InsertBuild() =>
        new InsertBuilder("Tenants")
            .Value("TenantId", Guid.NewGuid().ToString())
            .Value("Name", "Acme Corporation")
            .Value("Status", "Active")
            .Value("CreatedAt", DateTime.UtcNow)
            .Value("UpdatedAt", DateTime.UtcNow)
            .Build()
            .query;
}
