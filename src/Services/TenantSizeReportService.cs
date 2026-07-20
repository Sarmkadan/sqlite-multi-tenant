#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Data.SQLite;
using System.Text;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Services;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service implementation for generating tenant database size reports.
/// Enumerates all tenant databases, collects storage metrics via PRAGMA statements,
/// and provides sorted reports with human-readable formatting.
/// </summary>
public sealed class TenantSizeReportService : ITenantSizeReportService
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantSizeReportService> _logger;

    public TenantSizeReportService(
        ITenantService tenantService,
        ILogger<TenantSizeReportService> logger)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a size report for a single tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant size report record.</returns>
    public async Task<TenantSizeReportRecord> GenerateReportForTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        _logger.LogDebug("Generating size report for tenant {TenantId}", tenantId);

        var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken);
        if (tenant is null)
            throw new KeyNotFoundException($"Tenant with ID {tenantId} not found");

        if (string.IsNullOrWhiteSpace(tenant.DatabasePath) || !File.Exists(tenant.DatabasePath))
            throw new InvalidOperationException($"Tenant {tenantId} database file not found at {tenant.DatabasePath}");

        var fileInfo = new FileInfo(tenant.DatabasePath);

        long pageCount;
        int pageSize;
        long freeListCount;
        long walSizeBytes = 0;

        var connectionString = $"Data Source={tenant.DatabasePath};";

        await using (var connection = new SQLiteConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);

            // Get page count
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA page_count;";
                pageCount = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
            }

            // Get page size
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA page_size;";
                pageSize = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
            }

            // Get freelist count
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA freelist_count;";
                freeListCount = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
            }

            // Check for WAL file
            var walPath = tenant.DatabasePath + "-wal";
            if (File.Exists(walPath))
            {
                walSizeBytes = new FileInfo(walPath).Length;
            }
        }

        return new TenantSizeReportRecord
        {
            TenantId = tenant.TenantId,
            TenantName = tenant.Name,
            DatabasePath = tenant.DatabasePath,
            SizeBytes = pageCount * pageSize,
            PageCount = pageCount,
            PageSize = pageSize,
            FreeListCount = freeListCount,
            WalSizeBytes = walSizeBytes,
            FileSizeBytes = fileInfo.Length
        };
    }

    /// <summary>
    /// Generates size reports for all tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of sorted tenant size report records (descending by total size).</returns>
    public async Task<List<TenantSizeReportRecord>> GenerateReportForAllTenantsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating size reports for all tenants");

        var tenants = await _tenantService.GetAllTenantsAsync(cancellationToken);
        var records = new List<TenantSizeReportRecord>(tenants.Count);

        foreach (var tenant in tenants)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var record = await GenerateReportForTenantAsync(tenant.TenantId, cancellationToken);
                records.Add(record);
                _logger.LogDebug("Generated size report for tenant {TenantId} ({TenantName})", tenant.TenantId, tenant.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate size report for tenant {TenantId} ({TenantName})", tenant.TenantId, tenant.Name);
                // Add a minimal record with error information
                records.Add(new TenantSizeReportRecord
                {
                    TenantId = tenant.TenantId,
                    TenantName = tenant.Name,
                    DatabasePath = tenant.DatabasePath ?? "N/A",
                    SizeBytes = 0,
                    PageCount = 0,
                    PageSize = 0,
                    FreeListCount = 0,
                    WalSizeBytes = 0,
                    FileSizeBytes = 0
                });
            }
        }

        // Sort by total size descending (largest first)
        records.Sort();

        _logger.LogInformation("Generated {Count} tenant size reports", records.Count);
        return records;
    }

    /// <summary>
    /// Generates a formatted text table report for all tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Formatted text table report.</returns>
    public async Task<string> GenerateTextTableReportAsync(CancellationToken cancellationToken = default)
    {
        var records = await GenerateReportForAllTenantsAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine(TenantSizeReportRecord.GetTextTableHeader());

        foreach (var record in records)
        {
            sb.AppendLine(record.ToTextTableRow());
        }

        sb.AppendLine(TenantSizeReportRecord.GetTextTableFooter());
        return sb.ToString();
    }

    /// <summary>
    /// Generates a complete report with summary and text table for all tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete formatted report.</returns>
    public async Task<string> GenerateCompleteReportAsync(CancellationToken cancellationToken = default)
    {
        var records = await GenerateReportForAllTenantsAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine(TenantSizeReportRecord.GetSummaryReport(records));
        sb.AppendLine(await GenerateTextTableReportAsync(cancellationToken));

        return sb.ToString();
    }
}
