# BackupException

A sealed exception type that represents errors occurring during backup operations in the multi-tenant SQLite system. It provides factory methods for common failure scenarios and carries contextual identifiers for the backup and database involved, enabling precise error diagnosis and handling.

## API

### Properties

#### `BackupId`
`public string? BackupId`

Gets the identifier of the backup operation that failed, if available. Returns `null` when the failure occurred before a backup identifier could be assigned or when the context does not involve a specific backup instance.

#### `DatabaseId`
`public string? DatabaseId`

Gets the identifier of the tenant database associated with the failed backup operation, if available. Returns `null` when the failure is not tied to a particular database or when the database identity could not be determined at the point of failure.

### Constructors

#### `BackupException()`
`public BackupException()`

Initializes a new instance without a specified message or inner exception. Suitable for scenarios where the failure context is conveyed entirely through the static factory methods or when additional details are set after construction.

#### `BackupException(string message)`
`public BackupException(string message)`

Initializes a new instance with a descriptive error message.

| Parameter | Type     | Purpose                                          |
|-----------|----------|--------------------------------------------------|
| `message` | `string` | The message that describes the error condition.  |

#### `BackupException(string message, Exception innerException)`
`public BackupException(string message, Exception innerException)`

Initializes a new instance with a descriptive error message and a reference to the inner exception that caused this error.

| Parameter        | Type        | Purpose                                              |
|------------------|-------------|------------------------------------------------------|
| `message`        | `string`    | The message that describes the error condition.      |
| `innerException` | `Exception` | The exception that is the cause of the current one.  |

### Static Factory Methods

#### `CreationFailed`
`public static BackupException CreationFailed`

Returns a pre-configured instance representing a failure to create a backup. The returned exception carries a standard message indicating that backup creation could not be completed. Callers can set `BackupId` and `DatabaseId` on the returned instance before throwing it.

#### `VerificationFailed`
`public static BackupException VerificationFailed`

Returns a pre-configured instance representing a failure during backup integrity verification. The returned exception carries a standard message indicating that the backup data did not pass verification checks. Callers can set `BackupId` and `DatabaseId` on the returned instance before throwing it.

#### `RestoreFailed`
`public static BackupException RestoreFailed`

Returns a pre-configured instance representing a failure to restore a database from a backup. The returned exception carries a standard message indicating that the restore operation could not be completed. Callers can set `BackupId` and `DatabaseId` on the returned instance before throwing it.

#### `NotFound`
`public static BackupException NotFound`

Returns a pre-configured instance representing a failure to locate a requested backup. The returned exception carries a standard message indicating that the specified backup could not be found. Callers can set `BackupId` and `DatabaseId` on the returned instance before throwing it.

## Usage

### Example 1: Throwing on Backup Creation Failure

```csharp
public async Task CreateBackupAsync(string databaseId)
{
    try
    {
        // Attempt backup creation logic...
        await backupService.CreateAsync(databaseId);
    }
    catch (Exception ex)
    {
        var backupEx = BackupException.CreationFailed;
        backupEx.DatabaseId = databaseId;
        // BackupId may not be available if creation failed early
        throw backupEx;
    }
}
```

### Example 2: Catching and Inspecting Specific Backup Errors

```csharp
try
{
    await backupService.RestoreAsync(backupId, targetDatabaseId);
}
catch (BackupException ex) when (ex.BackupId == backupId)
{
    Console.WriteLine($"Restore failed for backup '{ex.BackupId}' on database '{ex.DatabaseId}': {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Caused by: {ex.InnerException.Message}");
    }
    // Decide whether to retry or escalate
}
```

## Notes

- The `BackupId` and `DatabaseId` properties are nullable strings. Always check for `null` before using them in log messages or conditional logic, as they may not be populated for failures that occur before these identifiers are resolved.
- The static factory methods return new instances on each invocation. If you need to preserve a reference to a specific instance, store it rather than calling the factory method repeatedly.
- This type is sealed; it cannot be subclassed. All custom behavior for backup error handling should be implemented through composition or by inspecting the exception's properties.
- Thread safety: Instance members are not synchronized. If multiple threads mutate `BackupId` or `DatabaseId` on the same instance after construction, external synchronization is required. The static factory methods themselves are safe to call from any thread, as they return independent instances.
- When using the parameterless constructor or factory methods, consider setting `BackupId` and `DatabaseId` before the exception is thrown to ensure that catch blocks have access to the full context.
