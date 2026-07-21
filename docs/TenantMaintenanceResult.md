# TenantMaintenanceResult

Represents the outcome of a maintenance operation performed on a tenant database, including metadata about the operation's execution, size changes, and any errors encountered. This type is used to track and report the status of tenant-level maintenance tasks such as backups, optimizations, or integrity checks.

## API

### `TenantId`
A string identifier uniquely representing the tenant associated with the maintenance operation.

### `TenantName`
A human-readable name of the tenant, used for logging and reporting purposes.

### `Operation`
A string describing the type of maintenance operation performed (e.g., "Backup", "Optimize", "IntegrityCheck").

### `StartedAt`
A `DateTime` value indicating when the maintenance operation began.

### `CompletedAt`
A nullable `DateTime` value indicating when the maintenance operation completed. If `null`, the operation is still in progress or has not yet finished.

### `SizeBeforeBytes`
A `long` value representing the size of the tenant database in bytes before the maintenance operation was executed.

### `SizeAfterBytes`
A `long` value representing the size of the tenant database in bytes after the maintenance operation completed.

### `IntermediateSizeBytes`
A nullable `long` value representing the size of the tenant database at an intermediate point during the operation. This may be `null` if no intermediate measurement was taken or applicable.

### `Error`
A nullable string containing the error message if the maintenance operation failed. If `null`, the operation completed successfully.

## Usage

```csharp
var result = new TenantMaintenanceResult
{
    TenantId = "tenant_123",
    TenantName = "Acme Corporation",
    Operation = "Backup",
    StartedAt = DateTime.UtcNow.AddMinutes(-10),
    CompletedAt = DateTime.UtcNow,
    SizeBeforeBytes = 1024000,
    SizeAfterBytes = 1024000,
    IntermediateSizeBytes = null,
    Error = null
};

Console.WriteLine($"Backup completed for {result.TenantName} at {result.CompletedAt}");
```

```csharp
var failedResult = new TenantMaintenanceResult
{
    TenantId = "tenant_456",
    TenantName = "Beta LLC",
    Operation = "IntegrityCheck",
    StartedAt = DateTime.UtcNow.AddMinutes(-5),
    CompletedAt = null,
    SizeBeforeBytes = 2048000,
    SizeAfterBytes = 0,
    IntermediateSizeBytes = 2048000,
    Error = "Database corruption detected during scan"
};

if (failedResult.Error != null)
{
    Console.WriteLine($"Operation failed: {failedResult.Error}");
}
```

## Notes

- `CompletedAt` being `null` indicates the operation has not yet completed. Consumers should check this value before relying on `SizeAfterBytes` or assuming successful completion.
- `IntermediateSizeBytes` may be `null` for operations that do not support or require intermediate measurements (e.g., simple backup operations).
- `Error` being `null` implies the operation completed without errors. Non-null values should be treated as fatal failures requiring manual intervention.
- This type contains no thread-safety mechanisms. Concurrent access to instances should be synchronized by the caller if used in multi-threaded contexts.
- Size values are expressed in bytes and may include or exclude transaction logs depending on the operation type. Refer to specific operation implementations for precise semantics.
