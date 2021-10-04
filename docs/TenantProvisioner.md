# TenantProvisioner

A utility class for provisioning, cloning, deprovisioning, and validating tenant-specific SQLite databases in a multi-tenant application. It handles tenant lifecycle operations including encrypted tenant provisioning, ensuring database isolation and integrity throughout the process.

## API

### `TenantProvisioner()`
Initializes a new instance of the `TenantProvisioner` class. This constructor has no parameters and prepares the provisioner for subsequent tenant operations.

### `async Task<Tenant> ProvisionTenantAsync(string tenantId, string connectionString)`
Provisions a new tenant database with the specified identifier and connection string.

- **tenantId**: The unique identifier for the tenant. Must not be null or whitespace.
- **connectionString**: The base connection string to use for the tenant's database. Must not be null or whitespace.
- **Return value**: A `Tenant` object representing the newly provisioned tenant.
- **Exceptions**: Throws `ArgumentException` if `tenantId` or `connectionString` is null or whitespace. Throws `InvalidOperationException` if the tenant already exists or if the provisioning operation fails.

### `async Task<string> CloneTenantAsync(string sourceTenantId, string newTenantId, string connectionString)`
Clones an existing tenant database to create a new tenant with a different identifier.

- **sourceTenantId**: The identifier of the tenant to clone. Must not be null or whitespace and must exist.
- **newTenantId**: The identifier for the new tenant. Must not be null or whitespace and must not already exist.
- **connectionString**: The base connection string to use for the new tenant's database. Must not be null or whitespace.
- **Return value**: The `tenantId` of the newly cloned tenant.
- **Exceptions**: Throws `ArgumentException` if any input parameter is null or whitespace. Throws `InvalidOperationException` if the source tenant does not exist or if the cloning operation fails.

### `async Task<bool> DeprovisionTenantAsync(string tenantId)`
Deletes the tenant database and removes all associated tenant data.

- **tenantId**: The identifier of the tenant to deprovision. Must not be null or whitespace.
- **Return value**: `true` if the deprovisioning was successful; otherwise, `false`.
- **Exceptions**: Throws `ArgumentException` if `tenantId` is null or whitespace. Throws `InvalidOperationException` if the tenant does not exist or if the deprovisioning operation fails.

### `async Task<bool> ValidateTenantDatabaseAsync(string tenantId)`
Validates the integrity and accessibility of a tenant-specific database.

- **tenantId**: The identifier of the tenant to validate. Must not be null or whitespace.
- **Return value**: `true` if the database is valid and accessible; otherwise, `false`.
- **Exceptions**: Throws `ArgumentException` if `tenantId` is null or whitespace. Throws `InvalidOperationException` if the tenant does not exist or if validation cannot be completed.

### `async Task<Tenant> ProvisionEncryptedTenantAsync(string tenantId, string connectionString, string encryptionKey)`
Provisions a new encrypted tenant database with the specified identifier, connection string, and encryption key.

- **tenantId**: The unique identifier for the tenant. Must not be null or whitespace.
- **connectionString**: The base connection string to use for the tenant's database. Must not be null or whitespace.
- **encryptionKey**: The encryption key to use for securing the tenant database. Must not be null or whitespace.
- **Return value**: A `Tenant` object representing the newly provisioned encrypted tenant.
- **Exceptions**: Throws `ArgumentException` if any input parameter is null or whitespace. Throws `InvalidOperationException` if the tenant already exists or if the provisioning operation fails.

## Usage

### Provisioning a new tenant
