# Migration

The `Migration` class represents a discrete database schema change unit within the `sqlite-multi-tenant` architecture, encapsulating both the definition of the change (scripts, versioning) and its runtime execution state (status, timing, errors). As a sealed entity, it serves as the primary data transfer object for tracking migration history across multiple tenant databases, ensuring that each tenant's schema evolution is recorded with full auditability regarding who executed the migration, how long it took, and whether it succeeded or failed.

## API

### `MigrationId`
*   **Type**: `public string`
*   **Description**: Gets the unique identifier for this specific migration record. This value distinguishes the migration instance within the tracking system, independent of the version number.

### `DatabaseId`
*   **Type**: `public string`
*   **Description**: Gets the identifier of the tenant database associated with this migration. This links the migration record to a specific physical or logical database instance within the multi-tenant environment.

### `Version`
*   **Type**: `public string`
*   **Description**: Gets the semantic version string associated with this migration. This typically follows a standard versioning scheme (e.g., "1.0.1") to order migrations chronologically.

### `Name`
*   **Type**: `public string`
*   **Description**: Gets the human-readable name of the migration, usually describing the specific schema change (e.g., "AddUsersTable").

### `Description`
*   **Type**: `public string?`
*   **Description**: Gets an optional detailed description of the migration's purpose or scope. Returns `null` if no description was provided.

### `UpScript`
*   **Type**: `public string`
*   **Description**: Gets the SQL script required to apply the migration (upgrade). This property is mandatory and must contain valid SQL commands to transition the schema forward.

### `DownScript`
*   **Type**: `public string?`
*   **Description**: Gets the optional SQL script required to revert the migration (downgrade). Returns `null` if the migration is not reversible or if a rollback script was not defined.

### `Status`
*   **Type**: `public MigrationStatus`
*   **Description**: Gets the current execution status of the migration (e.g., Pending, Running, Success, Failed). The `MigrationStatus` enum defines the allowable states.

### `CreatedAt`
*   **Type**: `public DateTime`
*   **Description**: Gets the timestamp indicating when this migration record was initially created or registered in the system.

### `ExecutedAt`
*   **Type**: `public DateTime?`
*   **Description**: Gets the timestamp when the execution of the `UpScript` began. Returns `null` if the migration has not yet started execution.

### `CompletedAt`
*   **Type**: `public DateTime?`
*   **Description**: Gets the timestamp when the migration successfully finished execution. Returns `null` if the migration is pending, running, or failed.

### `RolledBackAt`
*   **Type**: `public DateTime?`
*   **Description**: Gets the timestamp when a rollback operation (`DownScript`) was completed. Returns `null` if the migration has never been rolled back.

### `ExecutedBy`
*   **Type**: `public string?`
*   **Description**: Gets the identifier or name of the user or service account that triggered the migration execution. Returns `null` if the executor identity was not captured.

### `ErrorMessage`
*   **Type**: `public string?`
*   **Description**: Gets the error message captured if the migration execution failed. Returns `null` if the migration succeeded or has not yet failed.

### `ExecutionTimeMs`
*   **Type**: `public long`
*   **Description**: Gets the total duration of the migration execution in milliseconds. This value is typically populated after the migration completes or fails.

### `ExecutionOrder`
*   **Type**: `public int`
*   **Description**: Gets the integer value defining the sequence in which this migration should be applied relative to others. Lower values execute first.

### `IsRollbackable`
*   **Type**: `public bool`
*   **Description**: Gets a value indicating whether the migration supports a rollback operation. This is typically `true` if a valid `DownScript` exists and the current state allows reverting.

### `Database`
*   **Type**: `public TenantDatabase?`
*   **Description**: Gets the navigation property referencing the `TenantDatabase` object associated with this migration. Returns `null` if the database context is not loaded or the link is broken.

### `Validate`
*   **Type**: `public bool`
*   **Description**: Gets or sets a flag indicating whether validation logic should be executed against this migration before application. Setting this to `false` may bypass pre-flight checks.

## Usage

### Example 1: Inspecting Migration State
The following example demonstrates how to retrieve a migration record and inspect its execution history and error state if a failure occurred.

```csharp
public void AuditFailedMigration(Migration migration)
{
    if (migration.Status == MigrationStatus.Failed)
    {
        Console.WriteLine($"Migration '{migration.Name}' (v{migration.Version}) failed on database {migration.DatabaseId}.");
        Console.WriteLine($"Error: {migration.ErrorMessage}");
        Console.WriteLine($"Executed by: {migration.ExecutedBy ?? "Unknown"}");
        Console.WriteLine($"Duration: {migration.ExecutionTimeMs}ms");
        
        if (!string.IsNullOrEmpty(migration.DownScript) && migration.IsRollbackable)
        {
            Console.WriteLine("Rollback is available for this migration.");
        }
    }
}
```

### Example 2: Configuring a New Migration
This example shows the initialization of a new `Migration` instance with required scripts and metadata before it is queued for execution.

```csharp
public Migration CreateAddIndexMigration(string databaseId)
{
    var migration = new Migration
    {
        MigrationId = Guid.NewGuid().ToString(),
        DatabaseId = databaseId,
        Version = "2.1.0",
        Name = "AddEmailIndex",
        Description = "Creates a unique index on the Users.Email column for performance.",
        UpScript = "CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Email ON Users (Email);",
        DownScript = "DROP INDEX IF EXISTS IX_Users_Email;",
        ExecutionOrder = 105,
        CreatedAt = DateTime.UtcNow,
        Status = MigrationStatus.Pending,
        Validate = true
    };

    return migration;
}
```

## Notes

*   **Immutability of Execution Data**: While the class itself is not immutable, properties such as `CreatedAt`, `ExecutedAt`, `CompletedAt`, and `ExecutionTimeMs` represent historical facts. Modifying these manually after execution can corrupt the audit trail and should be avoided unless performing specific data correction operations.
*   **Nullability and Reversibility**: The presence of a `DownScript` does not automatically guarantee `IsRollbackable` is `true`; runtime conditions (such as the current `Status`) may prevent rollback even if a script exists. Always check `IsRollbackable` before attempting a revert.
*   **Thread Safety**: The `Migration` class is not thread-safe. In a multi-tenant environment where background workers might update `Status`, `ErrorMessage`, or timestamp properties concurrently, external synchronization (e.g., locks or atomic operations on the containing collection) is required when modifying an instance shared across threads.
*   **Database Context**: The `Database` navigation property may be `null` if the migration object was projected from a database query without including the related `TenantDatabase` entity. Accessing this property without ensuring it is loaded may result in missing context regarding the target tenant.
*   **Validation Bypass**: The `Validate` property allows bypassing pre-execution checks. Setting this to `false` should only be done when the caller guarantees the integrity of the `UpScript` externally, as skipping validation may lead to schema inconsistencies if the script is malformed.
