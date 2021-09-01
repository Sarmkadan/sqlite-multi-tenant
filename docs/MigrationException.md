# MigrationException

The `MigrationException` class serves as the dedicated exception type for signaling errors within the SQLite multi-tenant migration pipeline. Inheriting directly from `System.Exception`, this sealed class provides specific context regarding failed migration operations by exposing the identifier and version of the problematic migration. It includes specialized static factory methods to standardize error reporting for common failure scenarios such as execution failures, rollback issues, missing migrations, or duplicate application attempts.

## API

### Constructors

#### `public MigrationException()`
Initializes a new instance of the `MigrationException` class with default values. This constructor is typically used when specific migration context (ID or version) is unavailable at the throw site or when wrapping a lower-level exception where the context is supplied via other means.

#### `public MigrationException(string message)`
Initializes a new instance of the `MigrationException` class with a specified error message.
*   **Parameters**:
    *   `message`: A string describing the specific error condition.
*   **Purpose**: Provides a human-readable explanation of the failure while leaving `MigrationId` and `MigrationVersion` as null.

#### `public MigrationException(string message, Exception innerException)`
Initializes a new instance of the `MigrationException` class with a specified error message and a reference to the inner exception that caused this exception.
*   **Parameters**:
    *   `message`: A string describing the specific error condition.
    *   `innerException`: The `System.Exception` instance that caused the current exception, or `null` if no inner exception is specified.
*   **Purpose**: Preserves the stack trace and details of the underlying system error (e.g., a SQL syntax error) while wrapping it in the domain-specific `MigrationException`.

### Properties

#### `public string? MigrationId`
Gets the unique identifier of the migration associated with this exception.
*   **Return Value**: The migration ID string if available; otherwise, `null`.
*   **Purpose**: Allows catch blocks to programmatically identify which specific migration script caused the failure.

#### `public string? MigrationVersion`
Gets the version number or timestamp of the migration associated with this exception.
*   **Return Value**: The migration version string if available; otherwise, `null`.
*   **Purpose**: Provides temporal or sequential context for the failed migration, useful for logging and debugging ordering issues.

### Static Factory Methods

#### `public static MigrationException ExecutionFailed(string migrationId, string? version, Exception innerException)`
Creates a new `MigrationException` instance specifically indicating that a migration script failed during the application phase.
*   **Parameters**:
    *   `migrationId`: The ID of the migration that failed.
    *   `version`: The version of the migration.
    *   `innerException`: The underlying exception thrown during SQL execution.
*   **Return Value**: A new `MigrationException` with `MigrationId` and `MigrationVersion` populated and the inner exception preserved.
*   **Purpose**: Standardizes error reporting for runtime SQL errors during the `Up` method of a migration.

#### `public static MigrationException RollbackFailed(string migrationId, string? version, Exception innerException)`
Creates a new `MigrationException` instance specifically indicating that a migration script failed during the rollback phase.
*   **Parameters**:
    *   `migrationId`: The ID of the migration that failed to rollback.
    *   `version`: The version of the migration.
    *   `innerException`: The underlying exception thrown during the rollback execution.
*   **Return Value**: A new `MigrationException` with `MigrationId` and `MigrationVersion` populated.
*   **Purpose**: Distinguishes rollback failures from initial application failures, which is critical for determining the consistency state of the tenant database.

#### `public static MigrationException NotFound(string migrationId)`
Creates a new `MigrationException` instance indicating that a requested migration could not be located.
*   **Parameters**:
    *   `migrationId`: The ID of the missing migration.
*   **Return Value**: A new `MigrationException` with `MigrationId` set and `MigrationVersion` typically null.
*   **Purpose**: Signals configuration errors where the migration history references a script that does not exist in the assembly or file system.

#### `public static MigrationException AlreadyApplied(string migrationId, string? version)`
Creates a new `MigrationException` instance indicating an attempt to apply a migration that has already been recorded as applied.
*   **Parameters**:
    *   `migrationId`: The ID of the duplicate migration.
    *   `version`: The version of the duplicate migration.
*   **Return Value**: A new `MigrationException` with both `MigrationId` and `MigrationVersion` populated.
*   **Purpose**: Prevents idempotency violations where the migration runner attempts to re-execute a script already present in the `__EFMigrationsHistory` or equivalent tracking table.

## Usage

### Handling Specific Migration Failures
The following example demonstrates how to catch a `MigrationException` during a tenant upgrade process, inspect the specific migration details, and log the error appropriately without crashing the entire host application.

```csharp
try 
{
    await migrator.MigrateTenantAsync(tenantId, targetVersion);
}
catch (MigrationException ex)
{
    // Log specific context about the failed migration
    logger.LogError(
        ex, 
        "Migration failed for Tenant {TenantId}. Migration ID: {MigrationId}, Version: {Version}",
        tenantId, 
        ex.MigrationId, 
        ex.MigrationVersion
    );

    // Handle specific scenarios based on available data
    if (ex.MigrationId == "20230510_AddIndexUsers")
    {
        // Trigger alert for critical schema change failure
        alertService.SendCriticalSchemaAlert(tenantId);
    }
    
    // Re-throw or return a graceful error response
    throw;
}
```

### Validating Migration State Before Execution
This example illustrates using the static factory methods conceptually (or handling exceptions thrown by them) to validate that a migration is safe to run, ensuring no duplicate applications occur.

```csharp
public async Task ApplyMigrationIfPending(string tenantId, MigrationDefinition migration)
{
    var isApplied = await repository.IsMigrationAppliedAsync(tenantId, migration.Id);
    
    if (isApplied)
    {
        // Throw a standardized exception using the static helper
        throw MigrationException.AlreadyApplied(migration.Id, migration.Version);
    }

    try 
    {
        await executor.ExecuteAsync(migration);
    }
    catch (SqlException sqlEx)
    {
        // Wrap system SQL errors in the domain-specific exception
        throw MigrationException.ExecutionFailed(migration.Id, migration.Version, sqlEx);
    }
}
```

## Notes

*   **Immutability**: As a `sealed` class inheriting from `Exception`, instances of `MigrationException` are intended to be immutable once thrown. The `MigrationId` and `MigrationVersion` properties should be set exclusively via constructors or static factory methods and not modified post-instantiation.
*   **Nullability**: Consumers must handle `null` values for `MigrationId` and `MigrationVersion` when using the parameterless constructor or the message-only constructor, as these properties are only guaranteed to be populated when using the specific static factory methods or specialized constructors.
*   **Thread Safety**: This class is thread-safe for reading properties after instantiation. However, like all .NET exceptions, it is not designed to be mutated concurrently. Standard exception handling patterns (throwing and catching) are inherently safe in multi-threaded environments provided the exception instance is not shared and modified between threads before being thrown.
*   **Serialization**: Since this class derives from `System.Exception`, it supports standard .NET exception serialization. However, care should be taken when crossing AppDomain boundaries or serializing over networks to ensure the `MigrationId` and `MigrationVersion` data is preserved if the serialization strategy relies on custom data dictionaries rather than public properties.
