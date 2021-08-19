#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service interface for tenant management operations.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Retrieves a tenant by its unique identifier.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant if found; otherwise, null.</returns>
    Task<Tenant?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    /// <param name="name">The name of the tenant.</param>
    /// <param name="description">Optional description of the tenant.</param>
    /// <param name="contactEmail">Optional contact email for the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created tenant.</returns>
    Task<Tenant> CreateTenantAsync(string name, string? description = null, string? contactEmail = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the details of an existing tenant.
    /// </summary>
    /// <param name="tenant">The tenant object with updated details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all tenants.</returns>
    Task<List<Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active tenants.</returns>
    Task<List<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ActivateTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeactivateTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suspends a tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SuspendTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tenant exists.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the tenant exists; otherwise, false.</returns>
    Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of all tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total number of tenants.</returns>
    Task<int> GetTenantCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for tenants based on a search term.
    /// </summary>
    /// <param name="searchTerm">The term to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of tenants matching the search term.</returns>
    Task<List<Tenant>> SearchTenantsAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets metadata for a tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetTenantMetadataAsync(string tenantId, string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the database size info for a tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant storage info.</returns>
    Task<TenantStorageInfo> GetTenantDatabaseSizeAsync(string tenantId, CancellationToken cancellationToken = default);
}
