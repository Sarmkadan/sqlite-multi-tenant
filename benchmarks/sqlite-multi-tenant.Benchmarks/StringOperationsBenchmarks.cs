// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using SqliteMultiTenant.Utilities;

namespace SqliteMultiTenant.Benchmarks;

/// <summary>
/// Measures allocation cost of the string-processing hot paths used during
/// cache-key generation, file-path sanitization, and schema mapping.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev")]
public class StringOperationsBenchmarks
{
    private const string HashInput     = "tenant-connection-string:acme-corp:primary-db";
    private const string CamelInput    = "myTenantDatabaseConnectionString";
    private const string SnakeInput    = "my_tenant_database_connection_string";
    private const string FilePathInput = "tenant<db>file:name/sub|dir\\test*file";

    [Benchmark(Baseline = true)]
    public string ComputeSha256Hash() =>
        StringUtilities.ComputeSha256Hash(HashInput);

    [Benchmark]
    public string ComputeMd5Hash() =>
        StringUtilities.ComputeMd5Hash(HashInput);

    [Benchmark]
    public string ToSnakeCase() =>
        StringUtilities.ToSnakeCase(CamelInput);

    [Benchmark]
    public string ToCamelCase() =>
        StringUtilities.ToCamelCase(SnakeInput);

    [Benchmark]
    public string SanitizeForFilePath() =>
        StringUtilities.SanitizeForFilePath(FilePathInput);
}
