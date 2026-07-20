#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service interface for database maintenance operations on tenant databases.
/// Provides VACUUM, ANALYZE, and PRAGMA optimize operations to optimize SQLite database performance.
/// </summary>
public interface ITenantDatabaseMaintenanceService
{
    /// <summary>
    /// Executes VACUUM on a specific tenant database to reclaim space from deleted rows.
    /// VACUUM rebuilds the database file, repacking it into a minimal amount of disk space.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant maintenance result with before/after sizes and duration.</returns>
    Task<TenantMaintenanceResult> VacuumTenantDatabaseAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes VACUUM on all tenant databases.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of maintenance results for all tenants.</returns>
    Task<List<TenantMaintenanceResult>> VacuumAllTenantDatabasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes ANALYZE on a specific tenant database to update query planner statistics.
    /// ANALYZE collects statistics about tables and indexes and stores them in the sqlite_stat1 table.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant maintenance result with duration.</returns>
    Task<TenantMaintenanceResult> AnalyzeTenantDatabaseAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes ANALYZE on all tenant databases.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of maintenance results for all tenants.</returns>
    Task<List<TenantMaintenanceResult>> AnalyzeAllTenantDatabasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes PRAGMA optimize on a specific tenant database to update database statistics and optimize performance.
    /// PRAGMA optimize is a convenience pragma that runs ANALYZE and other optimizations.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant maintenance result with duration.</returns>
    Task<TenantMaintenanceResult> OptimizeTenantDatabaseAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes PRAGMA optimize on all tenant databases.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of maintenance results for all tenants.</returns>
    Task<List<TenantMaintenanceResult>> OptimizeAllTenantDatabasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes comprehensive maintenance (VACUUM + ANALYZE + PRAGMA optimize) on a specific tenant database.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant maintenance result with before/after sizes and duration.</returns>
    Task<TenantMaintenanceResult> PerformFullMaintenanceAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes comprehensive maintenance on all tenant databases.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of maintenance results for all tenants.</returns>
    Task<List<TenantMaintenanceResult>> PerformFullMaintenanceOnAllAsync(CancellationToken cancellationToken = default);
}
