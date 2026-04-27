#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Repositories;

/// <summary>
/// Repository interface for tenant CRUD and query operations
/// </summary>
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);
    Task<List<Tenant>> GetByStatusAsync(int status, CancellationToken cancellationToken = default);
    Task<Tenant> AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task DeleteAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<List<Tenant>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<List<Tenant>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
