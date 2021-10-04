# TenantService
The `TenantService` class is a sealed implementation of the `ITenantService` interface, providing a set of methods for managing tenants in a multi-tenant system. It allows for creating, updating, and deleting tenants, as well as retrieving information about existing tenants, such as their database size and metadata.

## API
* `public TenantService`: The constructor for the `TenantService` class.
* `public async Task<Tenant?> GetTenantAsync`: Retrieves a tenant by its identifier. Returns `null` if the tenant does not exist.
* `public async Task<Tenant> CreateTenantAsync`: Creates a new tenant. Returns the newly created tenant.
* `public async Task UpdateTenantAsync`: Updates an existing tenant.
* `public async Task DeleteTenantAsync`: Deletes a tenant.
* `public async Task<List<Tenant>> GetAllTenantsAsync`: Retrieves a list of all tenants.
* `public async Task<List<Tenant>> GetActiveTenantsAsync`: Retrieves a list of active tenants.
* `public async Task ActivateTenantAsync`: Activates a tenant.
* `public async Task DeactivateTenantAsync`: Deactivates a tenant.
* `public async Task SuspendTenantAsync`: Suspends a tenant.
* `public async Task<bool> TenantExistsAsync`: Checks if a tenant exists.
* `public async Task<int> GetTenantCountAsync`: Retrieves the number of tenants.
* `public async Task<List<Tenant>> SearchTenantsAsync`: Searches for tenants based on a query.
* `public async Task SetTenantMetadataAsync`: Sets metadata for a tenant.
* `public async Task<TenantStorageInfo> GetTenantDatabaseSizeAsync`: Retrieves the database size of a tenant.

## Usage
The following examples demonstrate how to use the `TenantService` class:
```csharp
// Create a new tenant
var tenantService = new TenantService();
var newTenant = await tenantService.CreateTenantAsync(new Tenant { Name = "Example Tenant" });

// Get all active tenants
var activeTenants = await tenantService.GetActiveTenantsAsync();
foreach (var tenant in activeTenants)
{
    Console.WriteLine(tenant.Name);
}
```

## Notes
The `TenantService` class is designed to be thread-safe, allowing for concurrent access to its methods. However, it is essential to note that some methods, such as `UpdateTenantAsync` and `DeleteTenantAsync`, may throw exceptions if the tenant does not exist or if there are concurrent modifications. Additionally, the `SearchTenantsAsync` method may return an empty list if no tenants match the search query. The `GetTenantDatabaseSizeAsync` method may throw an exception if the tenant's database is not accessible. It is recommended to handle these exceptions and edge cases accordingly in your application.
