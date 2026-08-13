#nullable enable
// =============================================================================
// Example: Tenant Database Maintenance Operations
// Demonstrates how to use ITenantDatabaseMaintenanceService for VACUUM, ANALYZE, and PRAGMA optimize
// =====================================================================

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Services;

namespace SqliteMultiTenant.Examples;

/// <summary>
/// Example demonstrating tenant database maintenance operations.
/// Shows how to execute VACUUM, ANALYZE, and comprehensive maintenance on tenant databases.
/// </summary>
public static class TenantDatabaseMaintenanceExample
{
    /// <summary>
    /// Demonstrates maintenance operations on a single tenant.
    /// </summary>
    public static async Task ExampleSingleTenantMaintenance(IServiceProvider serviceProvider, string? tenantId)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        Console.WriteLine("=== Tenant Database Maintenance Example ===\n");

        // Get the maintenance service from DI
        var maintenanceService = serviceProvider.GetRequiredService<ITenantDatabaseMaintenanceService>();

        // Example 1: Execute VACUUM to reclaim space
        Console.WriteLine("\n1. Executing VACUUM on tenant database...");
        var vacuumResult = await maintenanceService.VacuumTenantDatabaseAsync(tenantId);
        Console.WriteLine($"   Result: {vacuumResult.OperationSummary}");
        Console.WriteLine($"   Duration: {vacuumResult.DurationMs}ms");
        Console.WriteLine($"   Space reclaimed: {FormatFileSize(vacuumResult.SizeReductionBytes)}");

        // Example 2: Execute ANALYZE to update query statistics
        Console.WriteLine("\n2. Executing ANALYZE on tenant database...");
        var analyzeResult = await maintenanceService.AnalyzeTenantDatabaseAsync(tenantId);
        Console.WriteLine($"   Result: {analyzeResult.Operation}");
        Console.WriteLine($"   Duration: {analyzeResult.DurationMs}ms");

        // Example 3: Execute PRAGMA optimize for performance tuning
        Console.WriteLine("\n3. Executing PRAGMA optimize on tenant database...");
        var optimizeResult = await maintenanceService.OptimizeTenantDatabaseAsync(tenantId);
        Console.WriteLine($"   Result: {optimizeResult.Operation}");
        Console.WriteLine($"   Duration: {optimizeResult.DurationMs}ms");

        // Example 4: Perform full maintenance (VACUUM + ANALYZE + PRAGMA optimize)
        Console.WriteLine("\n4. Performing full maintenance on tenant database...");
        var fullResult = await maintenanceService.PerformFullMaintenanceAsync(tenantId);
        Console.WriteLine($"   Result: {fullResult.OperationSummary}");
        Console.WriteLine($"   Duration: {fullResult.DurationMs}ms");
        Console.WriteLine($"   Space reclaimed: {FormatFileSize(fullResult.SizeReductionBytes)}");
    }

    /// <summary>
    /// Demonstrates maintenance operations on all tenants.
    /// </summary>
    public static async Task ExampleAllTenantsMaintenance(IServiceProvider serviceProvider)
    {
        Console.WriteLine("=== Bulk Tenant Database Maintenance Example ===\n");

        // Get the maintenance service from DI
        var maintenanceService = serviceProvider.GetRequiredService<ITenantDatabaseMaintenanceService>();

        // Example 1: Execute VACUUM on all tenant databases
        Console.WriteLine("\n1. Executing VACUUM on all tenant databases...");
        var vacuumResults = await maintenanceService.VacuumAllTenantDatabasesAsync();
        LogResults(vacuumResults, "VACUUM");

        // Example 2: Execute ANALYZE on all tenant databases
        Console.WriteLine("\n2. Executing ANALYZE on all tenant databases...");
        var analyzeResults = await maintenanceService.AnalyzeAllTenantDatabasesAsync();
        LogResults(analyzeResults, "ANALYZE");

        // Example 3: Perform full maintenance on all tenant databases
        Console.WriteLine("\n3. Performing full maintenance on all tenant databases...");
        var fullResults = await maintenanceService.PerformFullMaintenanceOnAllAsync();
        LogResults(fullResults, "Full Maintenance");
    }

    private static void LogResults(IReadOnlyList<TenantMaintenanceResult> results, string operationName)
    {
        var successful = results.Count(r => r.IsSuccess);
        var failed = results.Count(r => !r.IsSuccess);
        var totalSpaceSaved = results.Where(r => r.IsSuccess).Sum(r => r.SizeReductionBytes);

        Console.WriteLine($"   Total tenants: {results.Count}");
        Console.WriteLine($"   Successful: {successful}");
        Console.WriteLine($"   Failed: {failed}");
        Console.WriteLine($"   Total space reclaimed: {FormatFileSize(totalSpaceSaved)}");

        if (failed > 0)
        {
            Console.WriteLine("\n   Failed operations:");
            foreach (var result in results.Where(r => !r.IsSuccess))
            {
                Console.WriteLine($"   - {result.TenantName}: {result.Error}");
            }
        }

        if (successful > 0)
        {
            Console.WriteLine($"\n   Sample results:");
            foreach (var result in results.Where(r => r.IsSuccess).Take(3))
            {
                Console.WriteLine($"   - {result.TenantName}: {FormatFileSize(result.SizeBeforeBytes)} → {FormatFileSize(result.SizeAfterBytes)} ({FormatFileSize(result.SizeReductionBytes)} saved)");
            }
            if (results.Count(r => r.IsSuccess) > 3)
            {
                Console.WriteLine($"   ... and {results.Count(r => r.IsSuccess) - 3} more");
            }
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        double size = bytes;

        while (size >= 1024 && counter < suffixes.Length - 1)
        {
            size /= 1024;
            counter++;
        }

        return $"{size:F2} {suffixes[counter]}";
    }
}
