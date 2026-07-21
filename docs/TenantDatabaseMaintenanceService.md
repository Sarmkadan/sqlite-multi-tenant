# TenantDatabaseMaintenanceService

Provides asynchronous database maintenance operations for tenant-specific SQLite databases in a multi-tenant application. The service encapsulates common maintenance tasks such as vacuuming, analyzing, and optimizing individual tenant databases or all tenant databases collectively.

## API

### `TenantDatabaseMaintenanceService`

Initializes a new instance of the `TenantDatabaseMaintenanceService` with required dependencies for tenant database access and maintenance operations.

### `async Task<TenantMaintenanceResult> VacuumTenantDatabaseAsync(string tenantId)`

Performs a `VACUUM` operation on the tenant database identified by `tenantId` to reclaim space and defragment the database file.

- **Parameters**
  - `tenantId` – The unique identifier of the tenant whose database should be vacuumed.
- **Return value**
  - A `TenantMaintenanceResult` indicating success or failure, including timing metrics and any errors encountered.
- **Exceptions**
  - Throws `ArgumentNullException` if `tenantId` is `null`.
  - Throws `InvalidOperationException` if the tenant database does not exist or is inaccessible.

### `async Task<List<TenantMaintenanceResult>> VacuumAllTenantDatabasesAsync()`

Performs a `VACUUM` operation on all registered tenant databases.

- **Return value**
  - A list of `TenantMaintenanceResult` objects, one per tenant, in no particular order.
- **Exceptions**
  - Throws `InvalidOperationException` if no tenant databases are registered or accessible.

### `async Task<TenantMaintenanceResult> AnalyzeTenantDatabaseAsync(string tenantId)`

Runs a `ANALYZE` command on the tenant database identified by `tenantId` to update SQLite’s internal statistics used by the query planner.

- **Parameters**
  - `tenantId` – The unique identifier of the tenant whose database should be analyzed.
- **Return value**
  - A `TenantMaintenanceResult` indicating success or failure, including timing metrics and any errors encountered.
- **Exceptions**
  - Throws `ArgumentNullException` if `tenantId` is `null`.
  - Throws `InvalidOperationException` if the tenant database does not exist or is inaccessible.

### `async Task<List<TenantMaintenanceResult>> AnalyzeAllTenantDatabasesAsync()`

Runs a `ANALYZE` command on all registered tenant databases.

- **Return value**
  - A list of `TenantMaintenanceResult` objects, one per tenant, in no particular order.
- **Exceptions**
  - Throws `InvalidOperationException` if no tenant databases are registered or accessible.

### `async Task<TenantMaintenanceResult> OptimizeTenantDatabaseAsync(string tenantId)`

Executes a sequence of maintenance operations on the tenant database identified by `tenantId` to improve performance: `VACUUM`, `ANALYZE`, and optional index rebuilds.

- **Parameters**
  - `tenantId` – The unique identifier of the tenant whose database should be optimized.
- **Return value**
  - A `TenantMaintenanceResult` indicating success or failure, including timing metrics and any errors encountered.
- **Exceptions**
  - Throws `ArgumentNullException` if `tenantId` is `null`.
  - Throws `InvalidOperationException` if the tenant database does not exist or is inaccessible.

### `async Task<List<TenantMaintenanceResult>> OptimizeAllTenantDatabasesAsync()`

Executes a sequence of maintenance operations on all registered tenant databases.

- **Return value**
  - A list of `TenantMaintenanceResult` objects, one per tenant, in no particular order.
- **Exceptions**
  - Throws `InvalidOperationException` if no tenant databases are registered or accessible.

### `async Task<TenantMaintenanceResult> PerformFullMaintenanceAsync(string tenantId)`

Performs a comprehensive maintenance routine on the tenant database identified by `tenantId`: integrity check, vacuum, analyze, and optional reindexing.

- **Parameters**
  - `tenantId` – The unique identifier of the tenant whose database should undergo full maintenance.
- **Return value**
  - A `TenantMaintenanceResult` indicating success or failure, including timing metrics and any errors encountered.
- **Exceptions**
  - Throws `ArgumentNullException` if `tenantId` is `null`.
  - Throws `InvalidOperationException` if the tenant database does not exist or is inaccessible.

### `async Task<List<TenantMaintenanceResult>> PerformFullMaintenanceOnAllAsync()`

Performs a comprehensive maintenance routine on all registered tenant databases.

- **Return value**
  - A list of `TenantMaintenanceResult` objects, one per tenant, in no particular order.
- **Exceptions**
  - Throws `InvalidOperationException` if no tenant databases are registered or accessible.

## Usage
