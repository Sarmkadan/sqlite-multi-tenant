// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service interface for tenant management operations
/// </summary>
public interface ITenantService
{
    Task<Tenant?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Tenant> CreateTenantAsync(string name, string? description = null, string? contactEmail = null, CancellationToken cancellationToken = default);
    Task UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<List<Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default);
    Task<List<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);
    Task ActivateTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task DeactivateTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task SuspendTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<int> GetTenantCountAsync(CancellationToken cancellationToken = default);
    Task<List<Tenant>> SearchTenantsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task SetTenantMetadataAsync(string tenantId, string key, string value, CancellationToken cancellationToken = default);
}
