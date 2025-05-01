// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using SqliteMultiTenant.Utilities;

namespace SqliteMultiTenant.Benchmarks;

/// <summary>
/// Measures throughput of the tenant validation pipeline — called on every
/// inbound API request that creates or resolves a tenant.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev")]
public class TenantValidationBenchmarks
{
    private const string ValidId        = "acme-corp-production";
    private const string ValidName      = "Acme Corporation Ltd";
    private const string NameToGenerate = "My Awesome SaaS Company";

    [Benchmark(Baseline = true)]
    public ValidationResult ValidateTenantId_Valid() =>
        TenantNameValidator.ValidateTenantId(ValidId);

    [Benchmark]
    public ValidationResult ValidateTenantId_Reserved() =>
        TenantNameValidator.ValidateTenantId("admin");

    [Benchmark]
    public ValidationResult ValidateTenantId_SqlInjection() =>
        TenantNameValidator.ValidateTenantId("t--DROP TABLE tenants");

    [Benchmark]
    public ValidationResult ValidateTenantName() =>
        TenantNameValidator.ValidateTenantName(ValidName);

    [Benchmark]
    public string GenerateTenantId() =>
        TenantNameValidator.GenerateTenantId(NameToGenerate);
}
