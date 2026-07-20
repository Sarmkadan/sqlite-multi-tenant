#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service interface for generating tenant database size reports.
/// Enumerates all tenant databases, collects storage metrics via PRAGMA statements,
/// and provides sorted reports with human-readable formatting.
/// </summary>
public interface ITenantSizeReportService
{
    /// <summary>
    /// Generates a size report for a single tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant size report record.</returns>
    Task<TenantSizeReportRecord> GenerateReportForTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates size reports for all tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of sorted tenant size report records (descending by total size).</returns>
    Task<List<TenantSizeReportRecord>> GenerateReportForAllTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a formatted text table report for all tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Formatted text table report.</returns>
    Task<string> GenerateTextTableReportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a complete report with summary and text table for all tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete formatted report.</returns>
    Task<string> GenerateCompleteReportAsync(CancellationToken cancellationToken = default);
}
