# TenantRecoveryServiceExtensions

Provides extension methods for performing tenant database recovery operations such as repair, restoration, point-in-time recovery, and backup cleanup in a multi-tenant SQLite environment.

## API

### `RepairDatabasesAsync`

Repairs corrupted tenant databases by rebuilding indexes, verifying schema integrity, and optionally restoring missing tables or columns. This operation is idempotent and safe to run multiple times.

- **Parameters**
  - `service` (`ITenantRecoveryService`): The tenant recovery service instance.
  - `tenantIds` (`IEnumerable<string>?`): Optional collection of tenant IDs to repair. If `null`, all tenants are processed.
  - `cancellationToken` (`CancellationToken`): A token to monitor for cancellation requests.

- **Return Value**
  Returns a `Task<int>` that resolves to the total number of tenants successfully repaired.

- **Exceptions**
  Throws `ArgumentNullException` if `service` is `null`.
  Throws `OperationCanceledException` if the operation is canceled via `cancellationToken`.

---

### `RestoreFromBackupsAsync`

Restores tenant databases from the most recent valid backup set. The operation selects the latest backup that passes integrity checks and applies it to the target tenant database.

- **Parameters**
  - `service` (`ITenantRecoveryService`): The tenant recovery service instance.
  - `tenantIds` (`IEnumerable<string>?`): Optional collection of tenant IDs to restore. If `null`, all tenants are processed.
  - `cancellationToken` (`CancellationToken`): A token to monitor for cancellation requests.

- **Return Value**
  Returns a `Task<int>` that resolves to the total number of tenants successfully restored.

- **Exceptions**
  Throws `ArgumentNullException` if `service` is `null`.
  Throws `OperationCanceledException` if the operation is canceled via `cancellationToken`.
  Throws `InvalidOperationException` if no valid backup exists for a tenant.

---

### `PointInTimeRecoveryAsync`

Performs point-in-time recovery for specified tenants by restoring from a backup taken at or before the given timestamp. The operation finds the latest backup that does not exceed the specified time and applies it.

- **Parameters**
  - `service` (`ITenantRecoveryService`): The tenant recovery service instance.
  - `pointInTime` (`DateTime`): The maximum timestamp (inclusive) for backup selection.
  - `tenantIds` (`IEnumerable<string>?`): Optional collection of tenant IDs to recover. If `null`, all tenants are processed.
  - `cancellationToken` (`CancellationToken`): A token to monitor for cancellation requests.

- **Return Value**
  Returns a `Task<int>` that resolves to the total number of tenants successfully recovered.

- **Exceptions**
  Throws `ArgumentNullException` if `service` is `null`.
  Throws `ArgumentOutOfRangeException` if `pointInTime` is in the future.
  Throws `OperationCanceledException` if the operation is canceled via `cancellationToken`.
  Throws `InvalidOperationException` if no valid backup exists before or at `pointInTime` for a tenant.

---
### `CleanupStaleBackupsAsync`

Removes backup files that are older than the specified retention period or have exceeded the maximum allowed backup count per tenant. This helps manage disk usage while preserving recent recovery points.

- **Parameters**
  - `service` (`ITenantRecoveryService`): The tenant recovery service instance.
  - `retentionDays` (`int`): Minimum age (in days) a backup must have before it can be deleted.
  - `maxBackupsPerTenant` (`int`): Maximum number of backups to retain per tenant. Older backups beyond this count are removed regardless of age.
  - `tenantIds` (`IEnumerable<string>?`): Optional collection of tenant IDs to clean up. If `null`, all tenants are processed.
  - `cancellationToken` (`CancellationToken`): A token to monitor for cancellation requests.

- **Return Value**
  Returns a `Task<int>` that resolves to the total number of backup files deleted across all tenants.

- **Exceptions**
  Throws `ArgumentNullException` if `service` is `null`.
  Throws `ArgumentOutOfRangeException` if `retentionDays` is negative or `maxBackupsPerTenant` is less than 1.
  Throws `OperationCanceledException` if the operation is canceled via `cancellationToken`.

## Usage
