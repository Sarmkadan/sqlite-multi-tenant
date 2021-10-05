# TenantRecoveryService

Provides recovery operations for tenant databases in a multi-tenant SQLite environment. This sealed class encapsulates repair, backup restoration, stale backup cleanup, and point-in-time recovery functionality. Each method is asynchronous and returns a result indicating success or the number of affected items.

## API

### `public TenantRecoveryService()`

Initializes a new instance of the `TenantRecoveryService` class.

- **Parameters**: None.
- **Throws**: None.

### `public async Task<bool> RepairDatabaseAsync()`

Attempts to repair the current tenant database. The repair process uses SQLite’s built-in integrity checks and recovery mechanisms.

- **Parameters**: None.
- **Returns**: `true` if the database was successfully repaired; `false` if the repair failed or was not needed.
- **Throws**: `InvalidOperationException` if the service is not properly initialized.

### `public async Task<bool> RestoreFromBackupAsync()`

Restores the tenant database from its most recent valid backup. The backup file is expected to exist at a predefined location.

- **Parameters**: None.
- **Returns**: `true` if the restore completed successfully; `false` if no valid backup was found or the restore failed.
- **Throws**: `InvalidOperationException` if the service is not properly initialized.

### `public async Task<int> CleanupStaleBackupsAsync()`

Removes backup files that are older than the configured retention period.

- **Parameters**: None.
- **Returns**: The number of backup files that were deleted.
- **Throws**: `InvalidOperationException` if the service is not properly initialized.

### `public async Task<bool> PointInTimeRecoveryAsync()`

Restores the tenant database to a specific point in time using available backup snapshots and transaction logs.

- **Parameters**: None.
- **Returns**: `true` if the point-in-time recovery succeeded; `false` if the required data is unavailable or the operation failed.
- **Throws**: `InvalidOperationException` if the service is not properly initialized.

## Usage

### Basic repair and backup cleanup

```csharp
using var service = new TenantRecoveryService();

bool repaired = await service.RepairDatabaseAsync();
if (!repaired)
{
    bool restored = await service.RestoreFromBackupAsync();
    if (!restored)
    {
        Console.WriteLine("Database could not be recovered.");
    }
}

int cleaned = await service.CleanupStaleBackupsAsync();
Console.WriteLine($"Cleaned {cleaned} stale backup(s).");
```

### Point-in-time recovery after data corruption

```csharp
using var service = new TenantRecoveryService();

// Attempt to recover to a known good state
bool recovered = await service.PointInTimeRecoveryAsync();
if (recovered)
{
    Console.WriteLine("Database restored to target point in time.");
}
else
{
    // Fall back to full backup restore
    bool restored = await service.RestoreFromBackupAsync();
    Console.WriteLine(restored ? "Full backup restored." : "Recovery failed.");
}
```

## Notes

- All methods operate on a single tenant database. The service must be configured before use; the parameterless constructor assumes that configuration is provided externally (e.g., via dependency injection or ambient context).
- Methods return `false` or `0` on failure rather than throwing exceptions for expected error conditions (e.g., missing backup, database already healthy). Exceptions are reserved for invalid service state.
- `CleanupStaleBackupsAsync` uses a default retention period unless overridden by configuration.
- `PointInTimeRecoveryAsync` requires that transaction logs and incremental backups are available. If none exist, the method returns `false`.
- The class is not thread-safe. Concurrent calls to any method on the same instance may lead to undefined behavior. Use separate instances or external synchronization when operating on the same tenant database concurrently.
- Backup and recovery operations may be I/O intensive; consider using a dedicated thread or limiting concurrency in high-load scenarios.
