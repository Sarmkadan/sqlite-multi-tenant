// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service implementation for tenant management
/// </summary>
public class TenantService : ITenantService
{
    private readonly ITenantRepository _repository;
    private readonly ILogger<TenantService> _logger;

    public TenantService(ITenantRepository repository, ILogger<TenantService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Tenant?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException($"Tenant ID parameter '{nameof(tenantId)}' must be a valid non-empty identifier.", nameof(tenantId));

        try
        {
            var tenant = await _repository.GetByIdAsync(tenantId, cancellationToken);
            if (tenant != null)
            {
                tenant.MarkAsAccessed();
                await _repository.UpdateAsync(tenant, cancellationToken);
            }
            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving tenant {tenantId}: {ex.Message}");
            throw;
        }
    }

    public async Task<Tenant> CreateTenantAsync(string name, string? description = null, string? contactEmail = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Tenant name parameter '{nameof(name)}' cannot be null or empty during creation.", nameof(name));

        try
        {
            var existingTenant = await _repository.GetByNameAsync(name, cancellationToken);
            if (existingTenant != null)
                throw new InvalidOperationException($"Tenant with name '{name}' already exists");

            var tenant = new Tenant
            {
                TenantId = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                ContactEmail = contactEmail,
                Status = TenantStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDataIsolated = true,
                MaxConnections = 10
            };

            if (!tenant.Validate(out var errors))
                throw new ArgumentException($"Tenant validation failed: {string.Join(", ", errors)}");

            var createdTenant = await _repository.AddAsync(tenant, cancellationToken);
            _logger.LogInformation($"Tenant created: {createdTenant.TenantId}");
            return createdTenant;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating tenant: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (tenant == null)
            throw new ArgumentNullException(nameof(tenant));

        try
        {
            var existingTenant = await _repository.GetByIdAsync(tenant.TenantId, cancellationToken);
            if (existingTenant == null)
                throw new TenantNotFoundException(tenant.TenantId);

            tenant.UpdatedAt = DateTime.UtcNow;

            if (!tenant.Validate(out var errors))
                throw new ArgumentException($"Tenant validation failed: {string.Join(", ", errors)}");

            await _repository.UpdateAsync(tenant, cancellationToken);
            _logger.LogInformation($"Tenant updated: {tenant.TenantId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating tenant: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        try
        {
            var existingTenant = await _repository.GetByIdAsync(tenantId, cancellationToken);
            if (existingTenant == null)
                throw new TenantNotFoundException(tenantId);

            await _repository.DeleteAsync(tenantId, cancellationToken);
            _logger.LogInformation($"Tenant deleted: {tenantId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting tenant {tenantId}: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving all tenants: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetActiveTenantsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving active tenants: {ex.Message}");
            throw;
        }
    }

    public async Task ActivateTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        try
        {
            var tenant = await _repository.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
                throw new TenantNotFoundException(tenantId);

            tenant.Activate();
            await _repository.UpdateAsync(tenant, cancellationToken);
            _logger.LogInformation($"Tenant activated: {tenantId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error activating tenant {tenantId}: {ex.Message}");
            throw;
        }
    }

    public async Task DeactivateTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        try
        {
            var tenant = await _repository.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
                throw new TenantNotFoundException(tenantId);

            tenant.Deactivate();
            await _repository.UpdateAsync(tenant, cancellationToken);
            _logger.LogInformation($"Tenant deactivated: {tenantId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deactivating tenant {tenantId}: {ex.Message}");
            throw;
        }
    }

    public async Task SuspendTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        try
        {
            var tenant = await _repository.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
                throw new TenantNotFoundException(tenantId);

            tenant.Status = TenantStatus.Suspended;
            tenant.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(tenant, cancellationToken);
            _logger.LogInformation($"Tenant suspended: {tenantId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error suspending tenant {tenantId}: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return false;

        try
        {
            return await _repository.ExistsAsync(tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking tenant existence: {ex.Message}");
            throw;
        }
    }

    public async Task<int> GetTenantCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetTotalCountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting tenant count: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Tenant>> SearchTenantsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));

        try
        {
            return await _repository.SearchAsync(searchTerm, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error searching tenants: {ex.Message}");
            throw;
        }
    }

    public async Task SetTenantMetadataAsync(string tenantId, string key, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be empty", nameof(key));

        try
        {
            var tenant = await _repository.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
                throw new TenantNotFoundException(tenantId);

            tenant.SetMetadata(key, value);
            await _repository.UpdateAsync(tenant, cancellationToken);
            _logger.LogInformation($"Tenant metadata updated: {tenantId} -> {key}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error setting tenant metadata: {ex.Message}");
            throw;
        }
    }
}
