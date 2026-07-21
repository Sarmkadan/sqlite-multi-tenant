# TenantDatabaseMaintenanceService

Provides asynchronous methods for performing database maintenance operations on tenant-specific SQLite databases, including vacuuming, analyzing, and optimizing operations at both individual and bulk levels.

## API

### `TenantDatabaseMaintenanceService`

Initializes a new instance of the `TenantDatabaseMaintenanceService` with required dependencies for tenant database maintenance operations.

### `async Task<TenantMaintenanceResult> VacuumTenantDatabaseAsync(string tenantId)`

Performs a `VACUUM` operation on the specified tenant's database to reclaim space and optimize the database file.

- **Parameters**:
  - `tenantId`: The unique identifier of the tenant whose database will be vacuumed.
- **Return value**: A `TenantMaintenanceResult` indicating success or failure and including relevant metrics.
- **Exceptions**: Throws `ArgumentNullException` if `tenantId` is null or empty. Throws `TenantNotFoundException` if the tenant does not exist.

### `async Task<List<TenantMaintenanceResult>> VacuumAllTenantDatabasesAsync()`

Performs a `VACUUM` operation on all tenant databases.

- **Return value**: A list of `TenantMaintenanceResult` objects, one for each tenant, indicating the outcome of each operation.
- **Exceptions**: Throws `InvalidOperationException` if no tenants are registered.

### `async Task<TenantMaintenanceResult> AnalyzeTenantDatabaseAsync(string tenantId)`

Runs an `ANALYZE` command on the specified tenant's database to update query planner statistics.

- **Parameters**:
  - `tenantId`: The unique identifier of the tenant whose database will be analyzed.
- **Return value**: A `TenantMaintenanceResult` indicating success or failure and including relevant metrics.
- **Exceptions**: Throws `ArgumentNullException` if `tenantId` is null or empty. Throws `TenantNotFoundException` if the tenant does not exist.

### `async Task<List<TenantMaintenanceResult>> AnalyzeAllTenantDatabasesAsync()`

Runs an `ANALYZE` command on all tenant databases.

- **Return value**: A list of `TenantMaintenanceResult` objects, one for each tenant, indicating the outcome of each operation.
- **Exceptions**: Throws `InvalidOperationException` if no tenants are registered.

### `async Task<TenantMaintenanceResult> OptimizeTenantDatabaseAsync(string tenantId)`

Performs a combined optimization routine on the specified tenant's database, including `VACUUM` and `ANALYZE`.

- **Parameters**:
  - `tenantId`: The unique identifier of the tenant whose database will be optimized.
- **Return value**: A `TenantMaintenanceResult` indicating success or failure and including relevant metrics.
- **Exceptions**: Throws `ArgumentNullException` if `tenantId` is null or empty. Throws `TenantNotFoundException` if the tenant does not exist.

### `async Task<List<TenantMaintenanceResult>> OptimizeAllTenantDatabasesAsync()`

Performs a combined optimization routine (`VACUUM` and `ANALYZE`) on all tenant databases.

- **Return value**: A list of `TenantMaintenanceResult` objects, one for each tenant, indicating the outcome of each operation.
- **Exceptions**: Throws `InvalidOperationException` if no tenants are registered.

### `async Task<TenantMaintenanceResult> PerformFullMaintenanceAsync(string tenantId)`

Performs a comprehensive maintenance routine on the specified tenant's database, including `VACUUM`, `ANALYZE`, and any additional optimizations.

- **Parameters**:
  - `tenantId`: The unique identifier of the tenant whose database will undergo full maintenance.
- **Return value**: A `TenantMaintenanceResult` indicating success or failure and including relevant metrics.
- **Exceptions**: Throws `ArgumentNullException` if `tenantId` is null or empty. Throws `TenantNotFoundException` if the tenant does not exist.

### `async Task<List<TenantMaintenanceResult>> PerformFullMaintenanceOnAllAsync()`

Performs a comprehensive maintenance routine on all tenant databases.

- **Return value**: A list of `TenantMaintenanceResult` objects, one for each tenant, indicating the outcome of each operation.
- **Exceptions**: Throws `InvalidOperationException` if no tenants are registered.

## Usage
