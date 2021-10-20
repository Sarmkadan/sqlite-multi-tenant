# MigrationController

The `MigrationController` class orchestrates the lifecycle of tenant‑specific schema migrations in the **sqlite‑multi‑tenant** library. It provides asynchronous APIs for creating migration definitions, retrieving pending migrations, applying batches of migrations, rolling back the most recent migration, and querying migration history. All operations return an `ApiResponse<T>` wrapper that encapsulates success status, payload, and any error information.

## API

### Constructor
```csharp
public MigrationController()
```
**Purpose** – Creates a new instance of the controller. No dependencies are required; the controller relies on ambient services configured elsewhere in the application.  
**Parameters** – None.  
**Return Value** – A ready‑to‑use `MigrationController` instance.  
**Throws** – None.

### CreateMigrationAsync
```csharp
public async Task<ApiResponse<MigrationResponse>> CreateMigrationAsync
```
**Purpose** – Registers a new migration definition for a tenant. The migration is stored but not yet applied.  
**Parameters** – The method expects a migration definition (type defined by the library) that contains the migration script, version, and metadata.  
**Return Value** – A `Task` that yields an `ApiResponse<MigrationResponse>`. On success, the `Response` property contains the created migration record, including its assigned identifier.  
**Throws** – 
- `ArgumentNullException` if the migration definition is `null`.  
- `InvalidOperationException` if a migration with the same version already exists for the tenant.  
- Any exception thrown by the underlying data store is propagated as a failed `ApiResponse` (i.e., `IsSuccess` is `false` and `Error` contains details).

### GetPendingMigrationsAsync
```csharp
public async Task<ApiResponse<IEnumerable<MigrationResponse>>> GetPendingMigrationsAsync
```
**Purpose** – Retrieves all migrations that have been registered but not yet applied for the tenant, ordered by version.  
**Parameters** – None.  
**Return Value** – A `Task` that yields an `ApiResponse<IEnumerable<MigrationResponse>>`. On success, the `Response` property contains an enumerable of pending migration records.  
**Throws** – 
- Any data‑access exception is captured in the returned `ApiResponse` (`IsSuccess` = `false`).  
- No exceptions are thrown directly from the method.

### ApplyMigrationsAsync
```csharp
public async Task<ApiResponse<MigrationBatchResponse>> ApplyMigrationsAsync
```
**Purpose** – Applies one or more pending migrations in a single transactional batch. The method selects the earliest pending migrations up to an optional limit and executes their scripts.  
**Parameters** – An optional integer specifying the maximum number of migrations to apply; if omitted, all pending migrations are processed.  
**Return Value** – A `Task` that yields an `ApiResponse<MigrationBatchResponse>`. On success, the `Response` includes the list of migrations that were applied and the resulting database version.  
**Throws** – 
- `ArgumentOutOfRangeException` if the supplied limit is negative.  
- If any migration script fails, the transaction is rolled back and the method returns a failed `ApiResponse` with details about the offending migration.  
- Other unexpected exceptions are also wrapped in the `ApiResponse` failure state.

### RollbackLastMigrationAsync
```csharp
public async Task<ApiResponse<MigrationResponse>> RollbackLastMigrationAsync
```
**Purpose** – Reverts the most recently applied migration for the tenant, if one exists. The rollback script associated with that migration is executed within a transaction.  
**Parameters** – None.  
**Return Value** – A `Task` that yields an `ApiResponse<MigrationResponse>`. On success, the `Response` contains the migration record that was rolled back.  
**Throws** – 
- `InvalidOperationException` if there are no applied migrations to roll back.  
- If the rollback script fails, the transaction is rolled back and the method returns a failed `ApiResponse` with error details.  
- Any unexpected exception is similarly wrapped in the response.

### GetMigrationHistoryAsync
```csharp
public async Task<ApiResponse<MigrationHistoryResponse>> GetMigrationHistoryAsync
```
**Purpose** – Returns a chronological log of all migrations that have been applied, including their status and timestamps.  
**Parameters** – None.  
**Return Value** – A `Task` that yields an `ApiResponse<MigrationHistoryResponse>`. On success, the `Response` contains the full migration history for the tenant.  
**Throws** – 
- Any data‑access error is captured in the returned `ApiResponse` (`IsSuccess` = `false`).  
- No exceptions are thrown directly from the method.

## Usage

### Example 1: Creating and applying a migration
```csharp
using SQLiteMultiTenant.Migration;

// Assume a configured tenant identifier for‑tenant scoped services provider is available.
var controller = new MigrationController();

// Define a new migration (the concrete type depends on the library).
var newMigration = new MigrationDefinition
{
    Version = "2024.09.01.01",
    Description = "Add user preferences table",
    Sql = @"CREATE TABLE UserPreferences (TenantId TEXT NOT NULL, Key TEXT NOT NULL, Value TEXT);"
};

// Register the migration.
var createResult = await controller.CreateMigrationAsync(newMigration);
if (!createResult.IsSuccess)
{
    Console.WriteLine($"Failed to create migration: {createResult.Error}");
    return;
}

// Apply all pending migrations.
var applyResult = await controller.ApplyMigrationsAsync();
if (applyResult.IsSuccess)
{
    Console.WriteLine($"Applied {applyResult.Response.AppliedCount} migration(s).");
}
else
{
    Console.WriteLine($"Migration application failed: {applyResult.Error}");
}
```

### Example 2: Inspecting history and rolling back
```csharp
var controller = new MigrationController();

// Fetch the migration history.
var historyResult = await controller.GetMigrationHistoryAsync();
if (!historyResult.IsSuccess)
{
    Console.WriteLine($"Unable to retrieve history: {historyResult.Error}");
    return;
}

Console.WriteLine("Migration history:");
foreach (var entry in historyResult.Response.Entries)
{
    Console.WriteLine($"- {entry.Version}: {entry.Description} (Applied at {entry.AppliedOn})");
}

// Roll back the most recent migration if history is not empty.
if (historyResult.Response.Entries.Any())
{
    var rollbackResult = await controller.RollbackLastMigrationAsync();
    if (rollbackResult.IsSuccess)
    {
        Console.WriteLine($"Rolled back migration {rollbackResult.Response.Version}.");
    }
    else
    {
        Console.WriteLine($"Rollback failed: {rollbackResult.Error}");
    }
}
```

## Notes
- **Thread safety** – The `MigrationController` class does not maintain mutable state; however, the underlying data store is shared across tenants. Concurrent calls to mutating methods (`CreateMigrationAsync`, `ApplyMigrationsAsync`, `RollbackLastMigrationAsync`) from multiple threads may result in race conditions. External synchronization or ensuring sequential invocation per tenant is recommended for strict consistency.  
- **Idempotency** – `CreateMigrationAsync` will reject duplicate version numbers, preventing accidental duplicate registrations. `ApplyMigrationsAsync` safely skips already‑applied migrations because it only processes pending entries.  
- **Error handling** – All public methods return an `ApiResponse<T>`; callers should inspect `IsSuccess` before accessing `Response`. Exceptions are not thrown for operational failures (e.g., validation errors, store errors) but are encapsulated within the response.  
- **Transaction scope** – `ApplyMigrationsAsync` and `RollbackLastMigrationAsync` execute their respective scripts inside a single transaction per call, ensuring that either all selected migrations are applied/rolled back or none are.  
- **Cancellation** – The asynchronous methods accept a `System.Threading.CancellationToken` via the underlying implementation; callers may pass a token to support cooperative cancellation.  
- **Resource disposal** – The controller holds no unmanaged resources and therefore does not implement `IDisposable`. No explicit cleanup is required after use.
