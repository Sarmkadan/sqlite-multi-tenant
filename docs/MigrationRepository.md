# MigrationRepository

The `MigrationRepository` class is a sealed implementation of the `IMigrationRepository` interface designed to manage persistence and retrieval of database migration records within the `sqlite-multi-tenant` project. It provides asynchronous operations to track the state of migrations across different tenant databases, supporting workflows for applying, rolling back, and auditing schema changes. This repository handles the storage of migration metadata, including versioning, execution status, and association with specific database instances.

## API

### Constructors

#### `public MigrationRepository()`
Initializes a new instance of the `MigrationRepository` class. This constructor sets up the necessary internal data access components required to interact with the underlying SQLite storage for migration records.

### Methods

#### `public async Task<List<Migration>> GetAllAsync()`
Retrieves a complete list of all migration records stored in the repository, regardless of their status or associated database.
*   **Returns**: A list containing all `Migration` objects. Returns an empty list if no records exist.
*   **Throws**: Throws an exception if the underlying database connection fails or the query cannot be executed.

#### `public async Task<Migration?> GetByIdAsync(Guid id)`
Fetches a specific migration record by its unique identifier.
*   **Parameters**:
    *   `id`: The unique GUID identifying the migration record.
*   **Returns**: The `Migration` object if found; otherwise, `null`.
*   **Throws**: Throws an exception if the database is unavailable.

#### `public async Task<List<Migration>> GetByDatabaseAsync(string databaseId)`
Retrieves all migration records associated with a specific tenant database.
*   **Parameters**:
    *   `databaseId`: The identifier of the target database.
*   **Returns**: A list of `Migration` objects linked to the specified `databaseId`. Returns an empty list if no migrations exist for this database.
*   **Throws**: Throws an exception if the query fails.

#### `public async Task<List<Migration>> GetPendingMigrationsAsync(string databaseId)`
Identifies migrations that are defined but have not yet been successfully applied to the specified database.
*   **Parameters**:
    *   `databaseId`: The identifier of the target database.
*   **Returns**: A list of `Migration` objects representing pending changes.
*   **Throws**: Throws an exception if the database state cannot be determined.

#### `public async Task<List<Migration>> GetAppliedMigrationsAsync(string databaseId)`
Retrieves a list of migrations that have been successfully executed against the specified database.
*   **Parameters**:
    *   `databaseId`: The identifier of the target database.
*   **Returns**: A list of successfully applied `Migration` objects.
*   **Throws**: Throws an exception if the retrieval operation fails.

#### `public async Task<List<Migration>> GetFailedMigrationsAsync(string databaseId)`
Fetches migrations that encountered errors during execution for the specified database.
*   **Parameters**:
    *   `databaseId`: The identifier of the target database.
*   **Returns**: A list of `Migration` objects marked as failed.
*   **Throws**: Throws an exception if the database cannot be queried.

#### `public async Task<Migration?> GetByVersionAsync(string databaseId, string version)`
Locates a specific migration record by matching the database ID and the migration version string.
*   **Parameters**:
    *   `databaseId`: The identifier of the target database.
    *   `version`: The version string of the migration.
*   **Returns**: The matching `Migration` object if found; otherwise, `null`.
*   **Throws**: Throws an exception if the query execution fails.

#### `public async Task<Migration> AddAsync(Migration migration)`
Persists a new migration record to the repository.
*   **Parameters**:
    *   `migration`: The `Migration` object to be added.
*   **Returns**: The added `Migration` object, typically including generated fields such as the unique ID or timestamps.
*   **Throws**: Throws an exception if the record already exists (violating uniqueness constraints) or if the write operation fails.

#### `public async Task UpdateAsync(Migration migration)`
Updates an existing migration record with new state information, such as changing status from "Pending" to "Applied" or recording error details.
*   **Parameters**:
    *   `migration`: The `Migration` object containing updated data.
*   **Returns**: A completed `Task`.
*   **Throws**: Throws an exception if the record does not exist or if the update operation fails.

#### `public async Task DeleteAsync(Guid id)`
Removes a migration record from the repository by its unique identifier.
*   **Parameters**:
    *   `id`: The unique GUID of the migration to delete.
*   **Returns**: A completed `Task`.
*   **Throws**: Throws an exception if the deletion fails or if constraints prevent removal.

#### `public async Task<bool> ExistsAsync(Guid id)`
Checks whether a migration record with the specified identifier exists in the repository.
*   **Parameters**:
    *   `id`: The unique GUID to check.
*   **Returns**: `true` if the record exists; otherwise, `false`.
*   **Throws**: Throws an exception if the database check cannot be performed.

#### `public async Task<int> GetCountByDatabaseAsync(string databaseId)`
Returns the total number of migration records associated with a specific database.
*   **Parameters**:
    *   `databaseId`: The identifier of the target database.
*   **Returns**: An integer representing the count of migrations.
*   **Throws**: Throws an exception if the count query fails.

#### `public async Task<List<Migration>> GetOrderedMigrationsAsync(string databaseId)`
Retrieves migrations for a specific database sorted by their execution order or version sequence.
*   **Parameters**:
    *   `databaseId`: The identifier of the target database.
*   **Returns**: A list of `Migration` objects ordered chronologically or by version.
*   **Throws**: Throws an exception if the sorting or retrieval fails.

## Usage

### Example 1: Checking and Applying Pending Migrations
This example demonstrates how to retrieve pending migrations for a specific tenant and process them.

```csharp
public async Task ApplyPendingMigrationsAsync(string tenantDbId, IMigrationRunner runner)
{
    var repository = new MigrationRepository();
    
    // Retrieve migrations that haven't been applied yet
    var pendingMigrations = await repository.GetPendingMigrationsAsync(tenantDbId);
    
    foreach (var migration in pendingMigrations)
    {
        try
        {
            // Execute the migration logic
            await runner.ExecuteAsync(migration);
            
            // Update status to Applied
            migration.Status = MigrationStatus.Applied;
            migration.AppliedAt = DateTime.UtcNow;
            await repository.UpdateAsync(migration);
        }
        catch (Exception ex)
        {
            // Record failure
            migration.Status = MigrationStatus.Failed;
            migration.ErrorMessage = ex.Message;
            await repository.UpdateAsync(migration);
            throw; // Re-throw or handle according to policy
        }
    }
}
```

### Example 2: Auditing Migration History for a Tenant
This example retrieves all applied migrations for a database to verify the current schema version.

```csharp
public async Task VerifySchemaVersionAsync(string tenantDbId, string expectedVersion)
{
    var repository = new MigrationRepository();
    
    // Get all successfully applied migrations in order
    var appliedMigrations = await repository.GetAppliedMigrationsAsync(tenantDbId);
    
    if (!appliedMigrations.Any())
    {
        throw new InvalidOperationException("No migrations have been applied to this database.");
    }
    
    // Check if the expected version exists in the applied list
    var isVersionApplied = appliedMigrations.Any(m => m.Version == expectedVersion);
    
    if (!isVersionApplied)
    {
        // Optionally fetch the specific record to inspect details
        var specificMigration = await repository.GetByVersionAsync(tenantDbId, expectedVersion);
        if (specificMigration != null && specificMigration.Status == MigrationStatus.Failed)
        {
            throw new InvalidOperationException($"Migration {expectedVersion} failed previously: {specificMigration.ErrorMessage}");
        }
        
        throw new InvalidOperationException($"Expected version {expectedVersion} has not been applied.");
    }
}
```

## Notes

*   **Thread Safety**: As a `sealed` class managing asynchronous database I/O, `MigrationRepository` instances should generally be treated as transient or scoped per operation. While the `async` methods allow non-blocking I/O, simultaneous calls to `UpdateAsync` or `DeleteAsync` for the same record from multiple threads without external locking may result in race conditions or database lock timeouts, depending on the underlying SQLite configuration.
*   **Null Handling**: Methods returning a single entity (`GetByIdAsync`, `GetByVersionAsync`) return `null` if the record is not found rather than throwing an exception. Callers must handle null checks appropriately.
*   **Data Integrity**: The `AddAsync` method assumes the provided `Migration` object is valid. Passing an object with a duplicate ID or invalid foreign key (database ID) will result in an exception from the underlying data provider.
*   **Ordering**: `GetOrderedMigrationsAsync` is critical for migration runners that rely on sequential execution. Do not rely on `GetByDatabaseAsync` for execution order, as it does not guarantee sorting.
*   **Existence Checks**: Use `ExistsAsync` before `AddAsync` if the uniqueness of the ID is not guaranteed by the caller, though `AddAsync` will enforce this at the database level.
