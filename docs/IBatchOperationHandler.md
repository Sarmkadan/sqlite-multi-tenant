# IBatchOperationHandler

`IBatchOperationHandler` defines the contract for processing batch operations against tenant-isolated SQLite databases. It receives a `BatchOperation` describing the work to be performed, executes it across the specified resources, and returns a `BatchOperationResult` summarizing outcomes for each resource. Implementations are responsible for translating the operation type and parameters into concrete database commands while respecting per-tenant connection boundaries.

## API

### BatchAtomicityMode

Defines the atomicity contract for batch operations:

- **CrossTenant (0)**: Operations across multiple tenant databases are best-effort. Each resource operation is independent. Failures in one tenant do not affect processing of other tenants. This is the default mode for backward compatibility.
- **SingleTenant (1)**: Operations against a single tenant database are transactional. All operations for that tenant are wrapped in a transaction and will be rolled back if any operation fails.

### BatchOperation

A sealed descriptor carrying all information needed to execute a batch operation.

| Member | Type | Purpose |
|---|---|---|
| `OperationId` | `string` | Unique identifier for the operation instance. |
| `OperationType` | `string` | Discriminator that determines which handler logic to invoke (e.g., `"Migrate"`, `"Vacuum"`, `"Validate"`). |
| `ResourceIds` | `List<string>` | Tenant or database identifiers targeted by this operation. An empty list typically means all known resources. |
| `Parameters` | `Dictionary<string, object>` | Arbitrary key-value pairs supplying operation-specific arguments (timeouts, flags, SQL text). |
| `CreatedAt` | `DateTime` | UTC timestamp when the operation was created. |
| `ContinueOnError` | `bool` | When true (default), the batch continues processing remaining resources after an error. When false, the operation attempts to maintain atomicity per tenant. |
| `AtomicityMode` | `BatchAtomicityMode` | Defines the atomicity contract: CrossTenant (best-effort) or SingleTenant (transactional). Defaults to CrossTenant. |

### BatchOperationResult

A sealed summary returned after the handler completes execution.

| Member | Type | Purpose |
|---|---|---|
| `OperationId` | `string` | Echoes the originating operation’s identifier. |
| `TotalResources` | `int` | Total number of resources processed. |
| `SuccessCount` | `int` | Number of resources that completed without error. |
| `FailureCount` | `int` | Number of resources that failed. |
| `ResourceResults` | `List<BatchResourceResult>` | Per-resource detail records. |
| `CompletedAt` | `DateTime` | UTC timestamp when processing finished. |
| `Duration` | `TimeSpan` | Wall-clock time from start to completion. |

### BatchResourceResult

A sealed per-resource outcome record.

| Member | Type | Purpose |
|---|---|---|
| `ResourceId` | `string` | The tenant or database identifier. |
| `Success` | `bool` | `true` if the operation succeeded for this resource. |
| `Message` | `string` | Human-readable detail (error description on failure, confirmation on success). |
| `DurationMs` | `long` | Elapsed milliseconds spent on this individual resource. |
| `Transactional` | `bool` | Indicates whether the operation was wrapped in a transaction (SingleTenant mode) or executed in best-effort mode (CrossTenant mode). |

### BatchOperationStatus

A sealed type representing the current state of a batch operation. The exact shape (enum values or properties) is defined by the project; it is used to track lifecycle transitions such as pending, running, completed, or failed.

## Atomicity Contract

### CrossTenant Mode (Default)

- **Behavior**: Best-effort execution where each resource operation is independent
- **Atomicity**: None across tenants. Failures in one tenant do not affect other tenants
- **ContinueOnError**: When true (default), continues processing remaining resources after errors
- **Use Case**: Operations spanning multiple tenants where partial failures are acceptable (e.g., bulk updates, validations)
- **Performance**: Higher throughput, no transaction overhead

### SingleTenant Mode

- **Behavior**: Transactional execution where all operations against a single tenant are wrapped in a transaction
- **Atomicity**: Per-tenant atomicity. If any operation fails, the entire transaction for that tenant is rolled back
- **ContinueOnError**: Not applicable - transaction rollback ensures atomicity
- **Use Case**: Critical operations against a single tenant where consistency is paramount (e.g., schema migrations, data integrity operations)
- **Performance**: Lower throughput due to transaction overhead, but guaranteed consistency

## Usage

### Example 1: Processing a migration operation across multiple tenants (CrossTenant mode)

```csharp
public async Task<BatchOperationResult> RunMigrationAsync(
    IBatchOperationHandler handler,
    IEnumerable<string> tenantIds,
    CancellationToken ct)
{
    var operation = new BatchOperation
    {
        OperationId = Guid.NewGuid().ToString("N"),
        OperationType = "Migrate",
        ResourceIds = tenantIds.ToList(),
        Parameters = new Dictionary<string, object>
        {
            ["TargetVersion"] = 5,
            ["TimeoutSeconds"] = 30
        },
        CreatedAt = DateTime.UtcNow,
        AtomicityMode = BatchAtomicityMode.CrossTenant, // Default
        ContinueOnError = true // Default
    };

    BatchOperationResult result = await handler.ProcessAsync(operation, ct);

    // Analyze results
    var failedMigrations = result.ResourceResults.Where(r => !r.Success);
    foreach (var resource in failedMigrations)
    {
        Console.WriteLine($"Migration failed for {resource.ResourceId}: {resource.Message}");
    }

    return result;
}
```

### Example 2: Validating all databases with transactional consistency (SingleTenant mode)

```csharp
public async Task<bool> ValidateAllDatabasesAsync(
    IBatchOperationHandler handler,
    CancellationToken ct)
{
    var operation = new BatchOperation
    {
        OperationId = $"validate-{DateTime.UtcNow:yyyyMMddHHmmss}",
        OperationType = "Validate",
        ResourceIds = new List<string>(), // empty means all
        Parameters = new Dictionary<string, object>
        {
            ["QuickCheck"] = false,
            ["MaxErrorsPerDb"] = 10
        },
        CreatedAt = DateTime.UtcNow,
        AtomicityMode = BatchAtomicityMode.SingleTenant, // Transactional mode
        ContinueOnError = false // Not used in SingleTenant mode
    };

    BatchOperationResult result = await handler.ProcessAsync(operation, ct);

    // In SingleTenant mode, if any tenant fails validation, the entire batch fails
    // The result will have FailureCount > 0 if any tenant failed
    return result.FailureCount == 0;
}
```

### Example 3: Bulk update with error isolation

```csharp
public async Task UpdateTenantSettingsAsync(
    IBatchOperationHandler handler,
    IEnumerable<string> tenantIds,
    Dictionary<string, object> settings,
    CancellationToken ct)
{
    var operation = new BatchOperation
    {
        OperationId = Guid.NewGuid().ToString("N"),
        OperationType = "UpdateSettings",
        ResourceIds = tenantIds.ToList(),
        Parameters = new Dictionary<string, object>
        {
            ["Settings"] = settings,
            ["ConcurrencyMode"] = "Optimistic"
        },
        CreatedAt = DateTime.UtcNow,
        AtomicityMode = BatchAtomicityMode.CrossTenant,
        ContinueOnError = true // Continue even if some tenants fail
    };

    var result = await handler.ProcessAsync(operation, ct);

    // Report failures but continue processing
    if (result.FailureCount > 0)
    {
        var failedTenants = result.ResourceResults.Where(r => !r.Success).Select(r => r.ResourceId);
        _logger.LogWarning("Failed to update settings for tenants: {Tenants}", string.Join(", ", failedTenants));
    }

    return result;
}
```

## Notes

- **Empty ResourceIds**: When `ResourceIds` is an empty list, the handler is expected to discover and process all available tenant databases. Implementations must define the discovery scope (e.g., all databases in a root directory, all registered tenants in a catalog).
- **OperationType dispatch**: The handler uses `OperationType` as a dispatch key. Unrecognized values should cause the entire operation to fail, typically by returning a result with `FailureCount == TotalResources` and descriptive messages, rather than throwing an unhandled exception.
- **Partial failure**: The handler must continue processing remaining resources after an individual resource fails (when ContinueOnError is true). The returned `BatchOperationResult` aggregates both successes and failures; it must never be null.
- **Parameter contract**: `Parameters` values are untyped at the dictionary level. Implementations must perform their own type checking and coercion. Missing expected keys should be treated as a configuration error for the affected resource.
- **Thread safety**: `BatchOperation`, `BatchOperationResult`, and `BatchResourceResult` are sealed types with public setters; they are not inherently thread-safe. Consumers should treat instances as single-owner objects. The handler’s `ProcessAsync` method may be invoked concurrently from multiple callers; implementations must ensure internal state (connection pools, file locks) is safe for parallel use.
- **Cancellation**: The handler accepts a `CancellationToken`. If cancellation is requested, the implementation should stop launching new per-resource work, attempt to cleanly interrupt in-progress database operations, and return a partial result reflecting work completed up to the cancellation point.
- **Transactional flag**: The `Transactional` property in `BatchResourceResult` indicates whether the operation was wrapped in a transaction. In CrossTenant mode, this will always be `false`. In SingleTenant mode, successful operations will have `true`.
- **Atomicity guarantees**: 
  - CrossTenant: No cross-tenant atomicity. Each tenant operation is independent.
  - SingleTenant: Per-tenant atomicity. All operations for a single tenant are transactional.