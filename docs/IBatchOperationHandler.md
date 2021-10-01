# IBatchOperationHandler

`IBatchOperationHandler` defines the contract for processing batch operations against tenant-isolated SQLite databases. It receives a `BatchOperation` describing the work to be performed, executes it across the specified resources, and returns a `BatchOperationResult` summarizing outcomes for each resource. Implementations are responsible for translating the operation type and parameters into concrete database commands while respecting per-tenant connection boundaries.

## API

### BatchOperation

A sealed descriptor carrying all information needed to execute a batch operation.

| Member | Type | Purpose |
|---|---|---|
| `OperationId` | `string` | Unique identifier for the operation instance. |
| `OperationType` | `string` | Discriminator that determines which handler logic to invoke (e.g., `"Migrate"`, `"Vacuum"`, `"Validate"`). |
| `ResourceIds` | `List<string>` | Tenant or database identifiers targeted by this operation. An empty list typically means all known resources. |
| `Parameters` | `Dictionary<string, object>` | Arbitrary key-value pairs supplying operation-specific arguments (timeouts, flags, SQL text). |
| `CreatedAt` | `DateTime` | UTC timestamp when the operation was created. |

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

### BatchOperationStatus

A sealed type representing the current state of a batch operation. The exact shape (enum values or properties) is defined by the project; it is used to track lifecycle transitions such as pending, running, completed, or failed.

## Usage

### Example 1: Processing a migration operation across multiple tenants

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
        CreatedAt = DateTime.UtcNow
    };

    BatchOperationResult result = await handler.ProcessAsync(operation, ct);

    foreach (var resource in result.ResourceResults.Where(r => !r.Success))
    {
        Console.WriteLine($"Migration failed for {resource.ResourceId}: {resource.Message}");
    }

    return result;
}
```

### Example 2: Validating all databases without specifying explicit resource IDs

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
        CreatedAt = DateTime.UtcNow
    };

    BatchOperationResult result = await handler.ProcessAsync(operation, ct);

    return result.FailureCount == 0;
}
```

## Notes

- **Empty ResourceIds**: When `ResourceIds` is an empty list, the handler is expected to discover and process all available tenant databases. Implementations must define the discovery scope (e.g., all databases in a root directory, all registered tenants in a catalog).
- **OperationType dispatch**: The handler uses `OperationType` as a dispatch key. Unrecognized values should cause the entire operation to fail, typically by returning a result with `FailureCount == TotalResources` and descriptive messages, rather than throwing an unhandled exception.
- **Partial failure**: The handler must continue processing remaining resources after an individual resource fails. The returned `BatchOperationResult` aggregates both successes and failures; it must never be null.
- **Parameter contract**: `Parameters` values are untyped at the dictionary level. Implementations must perform their own type checking and coercion. Missing expected keys should be treated as a configuration error for the affected resource.
- **Thread safety**: `BatchOperation`, `BatchOperationResult`, and `BatchResourceResult` are sealed types with public setters; they are not inherently thread-safe. Consumers should treat instances as single-owner objects. The handler’s `ProcessAsync` method may be invoked concurrently from multiple callers; implementations must ensure internal state (connection pools, file locks) is safe for parallel use.
- **Cancellation**: The handler accepts a `CancellationToken`. If cancellation is requested, the implementation should stop launching new per-resource work, attempt to cleanly interrupt in-progress database operations, and return a partial result reflecting work completed up to the cancellation point.
