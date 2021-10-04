# MigrationService

`MigrationService` is the central implementation of `IMigrationService` responsible for managing schema migrations in a multi-tenant SQLite environment. It provides methods to create, retrieve, execute, roll back, and track the status of migrations across tenant-specific databases, ensuring each tenant’s schema evolution is handled independently and auditably.

## API

### MigrationService()

Creates a new instance of the `MigrationService`. Initialization typically involves establishing the necessary connections or dependencies required to interact with the underlying tenant database infrastructure.

### GetMigrationAsync(string migrationId)

Retrieves a single migration by its unique identifier.

- **Parameters:** `migrationId` – the unique string identifier of the migration to fetch.
- **Returns:** `Task<Migration?>` – the requested `Migration` object, or `null` if no migration with the given ID exists.
- **Exceptions:** Throws when the underlying data store is unreachable or the identifier is malformed.

### GetDatabaseMigrationsAsync(string databaseId)

Returns all migrations associated with a specific tenant database, regardless of their status.

- **Parameters:** `databaseId` – the identifier of the tenant database whose migrations are to be listed.
- **Returns:** `Task<List<Migration>>` – a complete list of migrations for the specified database.
- **Exceptions:** Throws when the database identifier is invalid or the migration store cannot be queried.

### GetPendingMigrationsAsync(string databaseId)

Returns all migrations for a given database that have not yet been applied.

- **Parameters:** `databaseId` – the identifier of the tenant database.
- **Returns:** `Task<List<Migration>>` – a list of pending migrations, ordered by their intended application sequence.
- **Exceptions:** Throws when the database identifier is invalid or the migration store cannot be queried.

### GetAppliedMigrationsAsync(string databaseId)

Returns all migrations that have been successfully applied to the specified database.

- **Parameters:** `databaseId` – the identifier of the tenant database.
- **Returns:** `Task<List<Migration>>` – a list of applied migrations, typically ordered by application timestamp.
- **Exceptions:** Throws when the database identifier is invalid or the migration store cannot be queried.

### CreateMigrationAsync(string databaseId, string sql, string description)

Creates a new migration record for a tenant database. The migration is not executed immediately; it is staged for later application.

- **Parameters:**
  - `databaseId` – the target tenant database identifier.
  - `sql` – the SQL script representing the schema change.
  - `description` – a human-readable summary of what the migration does.
- **Returns:** `Task<Migration>` – the newly created `Migration` object with a generated identifier and a default pending status.
- **Exceptions:** Throws when required parameters are null or empty, or when the migration store cannot be written to.

### ExecuteMigrationAsync(string migrationId)

Applies a pending migration to its target database. On success, the migration’s status is updated to reflect completion.

- **Parameters:** `migrationId` – the identifier of the migration to execute.
- **Returns:** `Task` – completes when the SQL script has been executed and the status updated.
- **Exceptions:** Throws when the migration is not found, has already been applied, or the SQL execution fails. In the event of a SQL failure, the migration may be marked as failed.

### RollbackMigrationAsync(string migrationId)

Attempts to reverse a previously applied migration. The exact behavior depends on the rollback SQL defined in the migration record.

- **Parameters:** `migrationId` – the identifier of the migration to roll back.
- **Returns:** `Task` – completes when the rollback script has been executed and the status reverted.
- **Exceptions:** Throws when the migration is not found, has not been applied, or the rollback SQL execution fails.

### MarkMigrationAsCompletedAsync(string migrationId)

Manually marks a migration as successfully completed without executing its SQL. This is useful for synchronizing state when a migration was applied externally.

- **Parameters:** `migrationId` – the identifier of the migration to mark.
- **Returns:** `Task` – completes when the status has been updated.
- **Exceptions:** Throws when the migration is not found or the status update fails.

### MarkMigrationAsFailedAsync(string migrationId, string errorMessage)

Marks a migration as failed and records an error message. Typically invoked after a caught exception during execution.

- **Parameters:**
  - `migrationId` – the identifier of the migration.
  - `errorMessage` – a description of the failure reason.
- **Returns:** `Task` – completes when the failure status and message have been persisted.
- **Exceptions:** Throws when the migration is not found or the status update fails.

### GetMigrationCountAsync(string databaseId)

Returns the total number of migrations recorded for a tenant database.

- **Parameters:** `databaseId` – the identifier of the tenant database.
- **Returns:** `Task<int>` – the count of migrations.
- **Exceptions:** Throws when the database identifier is invalid or the migration store cannot be queried.

### IsMigrationAppliedAsync(string migrationId)

Checks whether a specific migration has been applied to its database.

- **Parameters:** `migrationId` – the identifier of the migration.
- **Returns:** `Task<bool>` – `true` if the migration has been applied; otherwise `false`.
- **Exceptions:** Throws when the migration identifier is invalid or the migration store cannot be queried.

### GetFailedMigrationsAsync(string databaseId)

Returns all migrations for a given database that are in a failed state.

- **Parameters:** `databaseId` – the identifier of the tenant database.
- **Returns:** `Task<List<Migration>>` – a list of failed migrations, each containing an error message.
- **Exceptions:** Throws when the database identifier is invalid or the migration store cannot be queried.

## Usage

### Example 1: Staging and applying a migration for a tenant

```csharp
var migrationService = new MigrationService();

// Stage a new migration for tenant "tenant-42"
Migration migration = await migrationService.CreateMigrationAsync(
    databaseId: "tenant-42",
    sql: "ALTER TABLE Orders ADD COLUMN Discount REAL NOT NULL DEFAULT 0;",
    description: "Add discount column to Orders table"
);

// Verify it is pending
bool isApplied = await migrationService.IsMigrationAppliedAsync(migration.Id);
if (!isApplied)
{
    await migrationService.ExecuteMigrationAsync(migration.Id);
    Console.WriteLine($"Migration {migration.Id} applied successfully.");
}
```

### Example 2: Handling a failed migration and retrying

```csharp
var migrationService = new MigrationService();
string tenantId = "tenant-99";

// Retrieve and attempt to apply all pending migrations
List<Migration> pending = await migrationService.GetPendingMigrationsAsync(tenantId);

foreach (var migration in pending)
{
    try
    {
        await migrationService.ExecuteMigrationAsync(migration.Id);
    }
    catch (Exception ex)
    {
        await migrationService.MarkMigrationAsFailedAsync(migration.Id, ex.Message);
        Console.WriteLine($"Migration {migration.Id} failed: {ex.Message}");
    }
}

// Later, inspect failures
List<Migration> failed = await migrationService.GetFailedMigrationsAsync(tenantId);
foreach (var f in failed)
{
    Console.WriteLine($"Failed migration: {f.Id} — {f.ErrorMessage}");
}
```

## Notes

- **Idempotency:** `ExecuteMigrationAsync` should not be called on an already-applied migration; doing so will throw. Always check `IsMigrationAppliedAsync` or rely on `GetPendingMigrationsAsync` to select candidates.
- **Manual status overrides:** `MarkMigrationAsCompletedAsync` and `MarkMigrationAsFailedAsync` bypass execution entirely. Use them only when the actual schema change has been handled out-of-band or when recording a failure that occurred outside the normal execution path.
- **Rollback availability:** `RollbackMigrationAsync` depends on the presence of a valid rollback script in the migration record. If no rollback SQL was provided at creation time, the method will throw.
- **Multi-tenancy isolation:** All operations are scoped to a `databaseId`. Migrations for one tenant are never returned or affected by calls targeting another tenant.
- **Thread safety:** The service itself is stateless with respect to tenant data; all state is persisted in the underlying store. Concurrent calls for different tenants or different migrations are safe. Concurrent operations on the same migration (e.g., simultaneous execute and rollback) are subject to race conditions and should be serialized by the caller.
