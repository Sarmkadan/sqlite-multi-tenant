#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Services;

namespace SqliteMultiTenant.Examples;

/// <summary>
/// Example demonstrating the TenantSizeReportService functionality.
/// Shows how to generate size reports for tenant databases.
/// </summary>
public static class TenantSizeReportExample
{
    public static async Task RunAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("=== Tenant Size Report Service Example ===\n");

        var reportService = serviceProvider.GetRequiredService<ITenantSizeReportService>();
        var logger = serviceProvider.GetRequiredService<ILogger<TenantSizeReportExample>>();

        // Example 1: Generate report for a single tenant
        Console.WriteLine("Example 1: Generate report for a single tenant");
        Console.WriteLine("-----------------------------------------------");

        var singleReport = await reportService.GenerateReportForTenantAsync("tenant-1", cancellationToken);
        Console.WriteLine($"Tenant: {singleReport.TenantName} ({singleReport.TenantId})");
        Console.WriteLine($"Database Path: {singleReport.DatabasePath}");
        Console.WriteLine($"Database Size: {singleReport.SizeHuman}");
        Console.WriteLine($"Page Count: {singleReport.PageCount:N0} pages");
        Console.WriteLine($"Page Size: {singleReport.PageSize} bytes");
        Console.WriteLine($"Free List: {singleReport.FreeListCount:N0} pages ({singleReport.FreeListPercentage:F1}% of database)");
        Console.WriteLine($"Free Space: {singleReport.FreeListSizeHuman}");
        Console.WriteLine($"File Size (on disk): {singleReport.FileSizeHuman}");
        Console.WriteLine($"WAL Size: {singleReport.WalSizeHuman}");
        Console.WriteLine($"Total Size (DB + WAL): {singleReport.TotalSizeHuman}");
        Console.WriteLine($"File Overhead: {singleReport.FileOverheadHuman}");
        Console.WriteLine();

        // Example 2: Generate report for all tenants
        Console.WriteLine("Example 2: Generate report for all tenants");
        Console.WriteLine("-----------------------------------------");

        var allReports = await reportService.GenerateReportForAllTenantsAsync(cancellationToken);
        Console.WriteLine($"Total Tenants: {allReports.Count}");
        Console.WriteLine();

        // Display top 5 largest tenants
        Console.WriteLine("Top 5 Largest Tenants:");
        Console.WriteLine("---------------------");
        for (int i = 0; i < Math.Min(5, allReports.Count); i++)
        {
            var report = allReports[i];
            Console.WriteLine($"{i + 1}. {report.TenantName} ({report.TenantId}): {report.TotalSizeHuman}");
        }
        Console.WriteLine();

        // Example 3: Generate formatted text table report
        Console.WriteLine("Example 3: Formatted Text Table Report");
        Console.WriteLine("-----------------------------------");
        var textTable = await reportService.GenerateTextTableReportAsync(cancellationToken);
        Console.WriteLine(textTable);
        Console.WriteLine();

        // Example 4: Generate complete report with summary
        Console.WriteLine("Example 4: Complete Report with Summary");
        Console.WriteLine("-------------------------------------");
        var completeReport = await reportService.GenerateCompleteReportAsync(cancellationToken);
        Console.WriteLine(completeReport);

        logger.LogInformation("Tenant size report example completed successfully");
    }
}
