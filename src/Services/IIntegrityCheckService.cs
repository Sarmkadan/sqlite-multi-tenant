#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service interface for performing SQLite database integrity checks on tenant databases.
/// Executes PRAGMA integrity_check per tenant and returns ok/failed status with messages.
/// Supports single tenant and batch operations with configurable parallelism.
/// </summary>
public interface IIntegrityCheckService
{
    /// <summary>
    /// Performs integrity check on a specific tenant database.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Integrity check result with ok/failed status and messages.</returns>
    Task<TenantIntegrityCheckResult> CheckTenantIntegrityAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs integrity check on multiple tenant databases with configurable parallelism.
    /// </summary>
    /// <param name="tenantIds">List of tenant identifiers to check.</param>
    /// <param name="maxDegreeOfParallelism">Maximum parallel operations (0 or 1 for sequential).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of integrity check results for all specified tenants.</returns>
    Task<List<TenantIntegrityCheckResult>> CheckTenantsIntegrityAsync(
        IEnumerable<string> tenantIds,
        int maxDegreeOfParallelism = 4,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs integrity check on all tenant databases with configurable parallelism.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Maximum parallel operations (0 or 1 for sequential).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of integrity check results for all tenants.</returns>
    Task<List<TenantIntegrityCheckResult>> CheckAllTenantsIntegrityAsync(
        int maxDegreeOfParallelism = 4,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs integrity check on all active tenant databases with configurable parallelism.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Maximum parallel operations (0 or 1 for sequential).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of integrity check results for active tenants only.</returns>
    Task<List<TenantIntegrityCheckResult>> CheckActiveTenantsIntegrityAsync(
        int maxDegreeOfParallelism = 4,
        CancellationToken cancellationToken = default);
}
