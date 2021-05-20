#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using SqliteMultiTenant.Configuration;

namespace SqliteMultiTenant.Benchmarks;

/// <summary>
/// Measures the performance of configuration options validation.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev")]
public sealed class OptionsValidatorBenchmarks {
    
    private MultiTenantOptions _validMultiTenantOptions = null!;
    private BackupOptions _validBackupOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        _validMultiTenantOptions = new MultiTenantOptions
        {
            BasePath = "/app/data",
            MaxConnectionsPerTenant = 10,
            MaxBackupCount = 5,
            BackupRetention = TimeSpan.FromDays(7)
        };

        _validBackupOptions = new BackupOptions
        {
            MaxConcurrentBackups = 2,
            BackupTimeoutSeconds = 60
        };
    }

    [Benchmark(Baseline = true)]
    public void ValidateMultiTenantOptions_Valid()
    {
        OptionsValidator.Validate(_validMultiTenantOptions);
    }

    [Benchmark]
    public void ValidateBackupOptions_Valid()
    {
        OptionsValidator.Validate(_validBackupOptions);
    }
}
