# IMetricsService

`IMetricsService` provides methods to collect, aggregate, and retrieve runtime metrics for a multi-tenant SQLite-based application. It exposes operations to record request outcomes, error counts, backup statistics, and migration statuses across tenants, enabling monitoring of system health and performance.

## API

### `Task<MetricsSnapshot> GetMetricsAsync()`

Retrieves a snapshot of aggregated metrics captured at the time of the call.

- **Returns**: A `MetricsSnapshot` instance containing totals and breakdowns of requests, errors, response times, backups, migrations, and endpoint-specific metrics.
- **Throws**: `InvalidOperationException` if metrics collection is not initialized or if the underlying storage is unavailable.

---

### `Task RecordRequestAsync(string tenantId, string endpoint, long responseTimeMs, bool isSuccess)`

Records a single HTTP request handled for the specified tenant and endpoint.

- **Parameters**:
  - `tenantId` – Identifier of the tenant associated with the request.
  - `endpoint` – Path or route of the requested endpoint.
  - `responseTimeMs` – Duration of the request in milliseconds.
  - `isSuccess` – `true` if the request completed successfully; otherwise, `false`.
- **Throws**: `ArgumentException` if `tenantId` or `endpoint` is `null` or empty.
- **Throws**: `ArgumentOutOfRangeException` if `responseTimeMs` is negative.

---

### `Task RecordErrorAsync(string tenantId, string errorType)`

Records an error of the specified type for the given tenant.

- **Parameters**:
  - `tenantId` – Identifier of the tenant where the error occurred.
  - `errorType` – Categorical identifier of the error (e.g., "SqlException", "Timeout").
- **Throws**: `ArgumentException` if `tenantId` or `errorType` is `null` or empty.

---
### `Task RecordBackupAsync(string tenantId, long bytesTransferred, bool isSuccess)`

Records the outcome of a tenant-specific backup operation.

- **Parameters**:
  - `tenantId` – Identifier of the tenant whose data was backed up.
  - `bytesTransferred` – Number of bytes written during the backup.
  - `isSuccess` – `true` if the backup completed successfully; otherwise, `false`.
- **Throws**: `ArgumentException` if `tenantId` is `null` or empty.
- **Throws**: `ArgumentOutOfRangeException` if `bytesTransferred` is negative.

---
### `Task RecordMigrationAsync(string tenantId, bool isSuccess)`

Records the outcome of a tenant-specific database migration.

- **Parameters**:
  - `tenantId` – Identifier of the tenant whose database was migrated.
  - `isSuccess` – `true` if the migration completed successfully; otherwise, `false`.
- **Throws**: `ArgumentException` if `tenantId` is `null` or empty.

## Usage

### Basic Metrics Collection
