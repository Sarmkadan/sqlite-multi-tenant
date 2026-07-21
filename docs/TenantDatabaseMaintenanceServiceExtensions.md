# TenantDatabaseMaintenanceServiceExtensions

Extension methods for configuring and enabling tenant database maintenance services in multi-tenant SQLite applications. These methods simplify the registration of background maintenance tasks such as vacuuming, analyzing, and optimizing tenant databases through dependency injection.

## API

### `AddTenantDatabaseMaintenanceService(IServiceCollection services, Action<TenantDatabaseMaintenanceOptions> configureOptions)`

Registers tenant database maintenance services with the dependency injection container. The maintenance tasks (vacuum, analyze, optimize) are configured via the provided options delegate.

- **Parameters**
  - `services`: The `IServiceCollection` instance.
  - `configureOptions`: An action delegate that configures `TenantDatabaseMaintenanceOptions`.
- **Return Value**: The `IServiceCollection` for method chaining.
- **Exceptions**: Throws `ArgumentNullException` if `services` or `configureOptions` is `null`.

---

### `AddTenantDatabaseMaintenanceService(IServiceCollection services, TenantDatabaseMaintenanceOptions options)`

Registers tenant database maintenance services with the dependency injection container using pre-configured options.

- **Parameters**
  - `services`: The `IServiceCollection` instance.
  - `options`: The `TenantDatabaseMaintenanceOptions` instance containing maintenance configuration.
- **Return Value**: The `IServiceCollection` for method chaining.
- **Exceptions**: Throws `ArgumentNullException` if `services` or `options` is `null`.

---

### `EnableVacuum`

Gets or sets a value indicating whether the SQLite `VACUUM` command should be executed during maintenance.

- **Type**: `bool`
- **Default**: `true`
- **Remarks**: When `true`, the service will reclaim space in the database file by rebuilding it. This operation can be resource-intensive and may lock the database temporarily.

---

### `EnableAnalyze`

Gets or sets a value indicating whether the SQLite `ANALYZE` command should be executed during maintenance.

- **Type**: `bool`
- **Default**: `true`
- **Remarks**: When `true`, the service updates SQLite's statistics used by the query planner. This improves query performance but may increase maintenance time.

---

### `EnableOptimize`

Gets or sets a value indicating whether the SQLite `PRAGMA optimize` command should be executed during maintenance.

- **Type**: `bool`
- **Default**: `true`
- **Remarks**: When `true`, the service runs SQLite's automatic optimization pragma. This can improve performance but may not be necessary if other maintenance tasks are sufficient.

---

### `IntervalHours`

Gets or sets the interval, in hours, between maintenance runs.

- **Type**: `int`
- **Default**: `24`
- **Remarks**:
  - Must be a positive integer.
  - If set to `0`, maintenance runs once at startup.
  - Values greater than `0` enable periodic background execution.

---

### `TimeoutSeconds`

Gets or sets the maximum duration, in seconds, allowed for each maintenance task.

- **Type**: `int`
- **Default**: `300` (5 minutes)
- **Remarks**:
  - Must be a positive integer.
  - If a task exceeds this duration, it is aborted and logged.
  - Consider increasing for large databases or slow storage.

---
### `DegreeOfParallelism`

Gets or sets the maximum number of tenant databases to process concurrently during maintenance.

- **Type**: `int`
- **Default**: `1`
- **Remarks**:
  - Must be a positive integer.
  - Higher values increase throughput but may increase resource contention (CPU, I/O, locks).
  - Set to `1` for minimal resource usage or higher for faster batch processing.

## Usage

### Example 1: Basic Configuration with Defaults
