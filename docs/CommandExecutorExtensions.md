# CommandExecutorExtensions

The `CommandExecutorExtensions` class provides a comprehensive set of asynchronous extension methods for the `sqlite-multi-tenant` library. These extensions facilitate common administrative and operational tasks within a multi-tenant environment, including tenant lifecycle management, database migration monitoring, health diagnostics, and robust command execution patterns. By encapsulating these operations, the class ensures consistent error handling and simplified integration for applications managing multiple SQLite tenant databases.

## API

*   **`ExecuteWithSuccessMessageAsync(ICommandExecutor executor, ICommand command, string successMessage)`**
    Executes a command and returns a `CommandResult` containing the specified success message upon successful execution. Throws an exception if the command fails to execute.

*   **`CreateTenantAsync(ICommandExecutor executor, string tenantId, TenantOptions options)`**
    Initiates the creation of a new tenant within the system. Returns a `CommandResult` indicating the status of the creation operation. Throws an exception if the tenant cannot be created.

*   **`ListTenantsAsync(ICommandExecutor executor)`**
    Retrieves a list of all provisioned tenants. Returns a `CommandResult` containing the enumerable collection of tenant identifiers.

*   **`CreateBackupAsync(ICommandExecutor executor, string tenantId, string backupPath)`**
    Triggers a backup operation for the specified tenant database. Returns a `CommandResult` with the outcome of the backup process. Throws an exception if access to the database or storage path is restricted.

*   **`CheckPendingMigrationsAsync(ICommandExecutor executor, string tenantId)`**
    Verifies if there are unapplied database migrations for a specific tenant. Returns a `CommandResult` indicating whether pending migrations exist.

*   **`CheckHealthAsync(ICommandExecutor executor, string tenantId)`**
    Performs a diagnostic check on the database health for a given tenant. Returns a `CommandResult` reflecting the health status, such as connectivity and integrity.

*   **`ExecuteOrThrowAsync(ICommandExecutor executor, ICommand command)`**
    Executes a command and returns the `CommandResult` on success. If the execution results in an error, it throws an exception containing the failure details instead of returning a failed `CommandResult`.

*   **`ExecuteWithTimeoutAsync(ICommandExecutor executor, ICommand command, TimeSpan timeout)`**
    Executes a command within a specified time constraint. Returns a `CommandResult` indicating success or failure. Throws a `TimeoutException` if the execution exceeds the provided `TimeSpan`.

## Usage

**Example 1: Provisioning a New Tenant**
```csharp
public async Task ProvisionNewClient(ICommandExecutor executor, string tenantId)
{
    var options = new TenantOptions { ConnectionString = "..." };
    var result = await executor.CreateTenantAsync(tenantId, options);
    
    if (result.IsSuccess)
    {
        Console.WriteLine($"Tenant {tenantId} created successfully.");
    }
}
```

**Example 2: Running Health Checks and Migration Verification**
```csharp
public async Task MaintainTenantDatabase(ICommandExecutor executor, string tenantId)
{
    var healthResult = await executor.CheckHealthAsync(tenantId);
    var migrationResult = await executor.CheckPendingMigrationsAsync(tenantId);

    if (healthResult.IsSuccess && (bool)migrationResult.Data == false)
    {
        Console.WriteLine("Tenant database is healthy and up to date.");
    }
}
```

## Notes

*   **Thread Safety:** While the `CommandExecutorExtensions` themselves are static and stateless, they depend on the `ICommandExecutor` implementation provided. Implementations of `ICommandExecutor` are expected to be thread-safe to support concurrent operations across different tenants in a web or background processing context.
*   **Cancellation:** These methods do not explicitly accept `CancellationToken` parameters in their current signature. If long-running operations are required, ensure the underlying `ICommandExecutor` implementation handles graceful termination appropriately.
*   **Exception Handling:** Methods that throw exceptions typically do so when the underlying database provider reports a fatal error, such as a locking issue, unauthorized access, or I/O failure. Applications should wrap these calls in appropriate try-catch blocks to handle infrastructure-level failures.
*   **Database Locking:** SQLite has inherent limitations regarding concurrent write operations. When executing commands that modify the database schema or data via these extensions, ensure the `ICommandExecutor` is configured to handle `SQLiteException` codes related to database busy states.
