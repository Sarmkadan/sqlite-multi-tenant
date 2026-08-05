#nullable enable
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
/// Service implementation for tenant management.
/// </summary>
public sealed class TenantService : ITenantService {
    private readonly ITenantRepository _repository;
    private readonly ILogger<TenantService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantService"/> class.
    /// </summary>
    /// <param name="repository">The tenant repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when repository or logger is null.</exception>
    public TenantService(ITenantRepository repository, ILogger<TenantService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves a tenant by its unique identifier.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The tenant if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when tenantId is null, empty, or whitespace.</exception>
    public async Task<Tenant?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException($"Tenant ID parameter '{nameof(tenantId)}' must be a valid non-empty identifier.", nameof(tenantId));

        try
        {
            var tenant = await _repository.GetByIdAsync(tenantId, cancellationToken);
            if (tenant is not null)
            {
                tenant.MarkAsAccessed();
                await _repository.UpdateAsync(tenant, cancellationToken);
            }
            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving tenant {TenantId}: {Message}", tenantId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    /// <param name="name">The name of the tenant.</param>
    /// <param name="description">The optional description of the tenant.</param>
    /// <param name="contactEmail">The optional contact email of the tenant.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The created tenant.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null or empty, or validation fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a tenant with the same name already exists.</exception>
    public async Task<Tenant> CreateTenantAsync(string name, string? description = null, string? contactEmail = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Tenant name parameter '{nameof(name)}' cannot be null or empty during creation.", nameof(name));

        try
        {
            var existingTenant = await _repository.GetByNameAsync(name, cancellationToken);
            if (existingTenant is not null)
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
            _logger.LogInformation("Tenant created: {TenantId}", createdTenant.TenantId);
            return createdTenant;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating tenant: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing tenant.
    /// </summary>
    /// <param name="tenant">The tenant object to update.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentNullException">Thrown when tenant is null.</exception>
    /// <exception cref="TenantNotFoundException">Thrown when the tenant does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when tenant validation fails.</exception>
    public async Task UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (tenant is null)
            throw new ArgumentNullException(nameof(tenant));

        try
        {
            var existingTenant = await _repository.GetByIdAsync(tenant.TenantId, cancellationToken);
            if (existingTenant is null)
                throw new TenantNotFoundException(tenant.TenantId);

            tenant.UpdatedAt = DateTime.UtcNow;

            if (!tenant.Validate(out var errors))
                throw new ArgumentException($"Tenant validation failed: {string.Join(", ", errors)}");

            await _repository.UpdateAsync(tenant, cancellationToken);
            _logger.LogInformation("Tenant updated: {TenantId}", tenant.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating tenant: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Deletes a tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when tenantId is null or empty.</exception>
    /// <exception cref="TenantNotFoundException">Thrown when the tenant does not exist.</exception>
    public async Task DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        try
        {
            var existingTenant = await _repository.GetByIdAsync(tenantId, cancellationToken);
            if (existingTenant is null)
                throw new TenantNotFoundException(tenantId);

            await _repository.DeleteAsync(tenantId, cancellationToken);
            _logger.LogInformation("Tenant deleted: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error deleting tenant {TenantId}: {Message}", tenantId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all tenants.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of all tenants.</returns>
    public async Task<List<Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving all tenants: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all active tenants.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of active tenants.</returns>
    public async Task<List<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetActiveTenantsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving active tenants: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Activates a tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant to activate.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when tenantId is null or empty.</exception>
    /// <exception cref="TenantNotFoundException">Thrown when the tenant does not exist.</exception>
    public async Task ActivateTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        try
        {
            var tenant = await _repository.GetByIdAsync(tenantId, cancellationToken);
            if (tenant is null)
                throw new TenantNotFoundException(tenantId);

            tenant.Activate();
            await _repository.UpdateAsync(tenant, cancellationToken);
            _logger.LogInformation("Tenant activated: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error activating tenant {TenantId}: {Message}", tenantId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Deactivates a tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant to deactivate.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when tenantId is null or empty.</exception>
    /// <exception cref="TenantNotFoundException">Thrown when the tenant does not exist.</exception>
    public async Task DeactivateTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        try
        {
            var tenant = await _repository.GetByIdAsync(tenantId, cancellationToken);
            if (tenant is null)
                throw new TenantNotFoundException(tenantId);

            tenant.Deactivate();
            await _repository.UpdateAsync(tenant, cancellationToken);
            _logger.LogInformation("Tenant deactivated: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error deactivating tenant {TenantId}: {Message}", tenantId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Suspends a tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant to suspend.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when tenantId is null or empty.</exception>
    /// <exception cref="TenantNotFoundException">Thrown when the tenant does not exist.</exception>
    public async Task SuspendTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        try
        {
            var tenant = await _repository.GetByIdAsync(tenantId, cancellationToken);
            if (tenant is null)
                throw new TenantNotFoundException(tenantId);

            tenant.Status = TenantStatus.Suspended;
            tenant.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(tenant, cancellationToken);
            _logger.LogInformation("Tenant suspended: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error suspending tenant {TenantId}: {Message}", tenantId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Checks if a tenant exists.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True if the tenant exists; otherwise, false.</returns>
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
            _logger.LogError("Error checking tenant existence: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets the total count of tenants.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The total number of tenants.</returns>
    public async Task<int> GetTenantCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetTotalCountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting tenant count: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Searches for tenants based on a search term.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of tenants matching the search term.</returns>
    /// <exception cref="ArgumentException">Thrown when searchTerm is null or empty.</exception>
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
            _logger.LogError("Error searching tenants: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Sets metadata for a tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when tenantId or key is null or empty.</exception>
    /// <exception cref="TenantNotFoundException">Thrown when the tenant does not exist.</exception>
    public async Task SetTenantMetadataAsync(string tenantId, string key, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be empty", nameof(key));

        try
        {
            var tenant = await _repository.GetByIdAsync(tenantId, cancellationToken);
            if (tenant is null)
                throw new TenantNotFoundException(tenantId);

            tenant.SetMetadata(key, value);
            await _repository.UpdateAsync(tenant, cancellationToken);
            _logger.LogInformation("Tenant metadata updated: {TenantId} -> {Key}", tenantId, key);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error setting tenant metadata: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the database size for a tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Storage information about the tenant's database.</returns>
    /// <exception cref="ArgumentException">Thrown when tenantId is null or empty.</exception>
    /// <exception cref="TenantNotFoundException">Thrown when the tenant does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the tenant has no database path configured.</exception>
    public async Task<TenantStorageInfo> GetTenantDatabaseSizeAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        try
        {
            var tenant = await _repository.GetByIdAsync(tenantId, cancellationToken);
            if (tenant is null)
                throw new TenantNotFoundException(tenantId);

            if (string.IsNullOrWhiteSpace(tenant.DatabasePath))
                throw new InvalidOperationException($"Tenant {tenantId} has no database path configured.");

            long pageCount;
            int pageSize;

            var connectionString = $"Data Source={tenant.DatabasePath};";
            using (var connection = new System.Data.SQLite.SQLiteConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA page_count;";
                    pageCount = (long)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0L);
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA page_size;";
                    pageSize = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken) ?? 4096);
                }
            }

            long walSizeBytes = 0;
            var walPath = tenant.DatabasePath + "-wal";
            if (File.Exists(walPath))
                walSizeBytes = new FileInfo(walPath).Length;

            return new Models.TenantStorageInfo
            {
                TenantId = tenantId,
                PageCount = pageCount,
                PageSize = pageSize,
                SizeBytes = pageCount * pageSize,
                WalSizeBytes = walSizeBytes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving database size for tenant {TenantId}: {Message}", tenantId, ex.Message);
            throw;
        }
    }
}
