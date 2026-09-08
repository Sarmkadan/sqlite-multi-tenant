# BatchOperationHandler

`BatchOperationHandler` is the built-in implementation of [`IBatchOperationHandler`](IBatchOperationHandler.md). It validates batch limits and tenant authorization, executes each requested resource, records per-resource results, and keeps an in-memory status entry keyed by operation ID. All types on this page are in the `SqliteMultiTenant.Operations` namespace and are declared in `src/Operations/BatchOperationHandler.cs`.

## Public API

### `IBatchOperationHandler`

```csharp
public interface IBatchOperationHandler
```

The abstraction implemented by `BatchOperationHandler`. The separate [interface documentation](IBatchOperationHandler.md) describes the intended contract; the signatures below are the contract actually declared by the source.

```csharp
Task<BatchOperationResult> ExecuteAsync(
    BatchOperation operation,
    CancellationToken cancellationToken);
```

Executes a batch and returns its aggregate and per-resource results.

```csharp
Task<BatchOperationStatus> GetStatusAsync(string operationId);
```

Returns the status stored for `operationId`. Although the declared return type is non-nullable, the implementation produces a null result when the ID is not present.

### `BatchAtomicityMode`

```csharp
public enum BatchAtomicityMode
{
    CrossTenant = 0,
    SingleTenant = 1
}
```

- `CrossTenant` executes each resource independently in best-effort mode.
- `SingleTenant` selects the handler's transactional path. In the current implementation that path simulates a successful transaction; it does not open a database transaction or dispatch the requested operation.

### `BatchOperation`

```csharp
public sealed class BatchOperation
```

Describes a batch request.

| Public property | Default | Meaning |
|---|---:|---|
| `string OperationId { get; set; }` | New GUID string | Identifier used in results, logs, and status tracking. |
| `string OperationType { get; set; }` | `string.Empty` | Operation discriminator passed to the internal dispatch path. |
| `List<string> ResourceIds { get; set; }` | Empty list | Resource/tenant IDs to process. The implementation does no discovery when this list is empty. |
| `Dictionary<string, object> Parameters { get; set; }` | Empty dictionary | Operation-specific payload. |
| `DateTime CreatedAt { get; set; }` | `DateTime.UtcNow` | Request creation time; execution does not otherwise consume it. |
| `bool ContinueOnError { get; set; }` | `true` | Best-effort preference recorded in the request and logs. The current execution loops do not branch on this property. |
| `BatchAtomicityMode AtomicityMode { get; set; }` | `CrossTenant` | Chooses best-effort or simulated transactional execution. |

### `BatchOperationResult`

```csharp
public sealed class BatchOperationResult
```

Summarizes a completed or cancellation-shortened execution.

| Public property | Meaning |
|---|---|
| `string OperationId { get; set; }` | ID copied from the request. |
| `int TotalResources { get; set; }` | Count of entries originally supplied in `ResourceIds`, including empty entries and duplicates. |
| `int SuccessCount { get; set; }` | Number of processed resources that succeeded. |
| `int FailureCount { get; set; }` | Number of processed resources that failed. |
| `List<BatchResourceResult> ResourceResults { get; set; }` | Result for every resource actually processed. |
| `DateTime CompletedAt { get; set; }` | Initialized to UTC creation time of the result object; the handler does not reset it during finalization. |
| `TimeSpan Duration { get; set; }` | Elapsed wall-clock execution time. |

```csharp
public BatchOperationAggregateStatus GetOverallStatus();
```

Returns `Empty` when `TotalResources` is zero, `AllSucceeded` when there are successes and no failures, `AllFailed` when there are failures and no successes, and `PartialSuccess` otherwise. A cancelled or filtered batch can therefore be `PartialSuccess` even when it has no failures.

### `BatchResourceResult`

```csharp
public sealed class BatchResourceResult
```

Represents one processed resource.

| Public property | Meaning |
|---|---|
| `string ResourceId { get; set; }` | Resource/tenant identifier. |
| `bool Success { get; set; }` | Whether processing succeeded. |
| `string Message { get; set; }` | Success detail or captured exception message. |
| `long DurationMs { get; set; }` | Processing time in milliseconds. |
| `bool Transactional { get; set; }` | Whether the transactional path was used. |

### `BatchOperationStatus`

```csharp
public sealed class BatchOperationStatus : OperationStatusBase
```

Adds batch progress to the lifecycle fields supplied by `OperationStatusBase`.

| Public member | Meaning |
|---|---|
| `int TotalResources { get; set; }` | Original number of resource IDs. |
| `int ProcessedResources { get; set; }` | Number of success and failure results recorded so far. |
| `int ProgressPercent { get; }` | Integer percentage `(ProcessedResources * 100) / TotalResources`, or `0` when the total is zero. |

```csharp
public BatchOperationStatus();
```

Creates a pending status with `OperationId` initially set to `"BatchOperationStatus"` and `CreatedAt` set to the current UTC time. `ExecuteAsync` replaces the ID with the request's ID.

```csharp
public void MarkRunning();
public void MarkCompleted();
public void MarkFailed(string error);
public void Validate();
```

These methods expose the corresponding lifecycle operations from `OperationStatusBase`. `Validate` throws `ArgumentException` when inherited status data is invalid.

### `BatchOperationAggregateStatus`

```csharp
public enum BatchOperationAggregateStatus
{
    AllSucceeded,
    AllFailed,
    PartialSuccess,
    Empty
}
```

The aggregate returned by `BatchOperationResult.GetOverallStatus`.

### `BatchOperationHandler`

```csharp
public sealed class BatchOperationHandler : IBatchOperationHandler
```

The registered batch handler implementation.

```csharp
public BatchOperationHandler(
    ILogger<BatchOperationHandler> logger,
    TenantContextHelper tenantContextHelper,
    SqliteMultiTenantOptions? options = null);
```

Creates a handler. `logger` and `tenantContextHelper` are required and cause `ArgumentNullException` when null. Passing no options selects the default limits described below.

```csharp
public Task<BatchOperationResult> ExecuteAsync(
    BatchOperation operation,
    CancellationToken cancellationToken);
```

Validates the request and its limits, checks that every non-empty resource ID equals the current tenant ID, creates a running status, groups duplicate IDs, and processes the groups sequentially. Null input throws `ArgumentNullException`; missing or mismatched tenant context throws `UnauthorizedAccessException`; limit violations throw `BatchTooLargeException`. Cancellation stops subsequent work and returns the results accumulated so far, after marking the status completed.

```csharp
public Task<BatchOperationStatus> GetStatusAsync(string operationId);
```

Returns the in-memory status for an operation ID, or a task whose result is null if no entry exists. Entries are local to this handler instance and are not persisted or removed by this class.

## Batch limits and `BatchTooLargeException`

The constructor reads limits from `SqliteMultiTenantOptions`:

| Limit | Option | Default | Enforcement |
|---|---|---:|---|
| Resource count | `MaxBatchItems` | `500` | Rejected only when the configured value is greater than zero and `ResourceIds.Count` is greater than it. A value of zero or less disables this check. |
| Parameter payload | `MaxBatchPayloadSizeBytes` | `1,048,576` bytes (1 MiB) | Rejected only when the configured value is greater than zero and the estimated parameter size is greater than it. A value of zero or less disables this check. |

Payload size is an estimate, not a serialized byte count. Each dictionary key and string value contributes two bytes per UTF-16 character; every other non-null value contributes 128 bytes; null contributes no value bytes. `ResourceIds` and the other `BatchOperation` fields are not included in the payload estimate.

Both checks run synchronously near the start of `ExecuteAsync`, before tenant authorization, status creation, or resource processing. The private `ValidateBatchSize` method is the only location in this class that throws `BatchTooLargeException`:

1. An item-count violation uses `BatchTooLargeException(maxItemCount, actualItemCount)`.
2. A payload violation uses `BatchTooLargeException(maxItemCount, actualItemCount, maxPayloadSizeBytes, actualPayloadSizeBytes)`.

Equality with a limit is accepted. If both limits are exceeded, the item-count check runs first, so its two-argument exception is observed.

## Registration and interface relationship

The library registers `IBatchOperationHandler` to `BatchOperationHandler` with scoped lifetime. Consumers should normally depend on the interface and call `ExecuteAsync`/`GetStatusAsync`. The models and enums on this page share the same source file as the interface and implementation. See [`IBatchOperationHandler.md`](IBatchOperationHandler.md) for the broader contract discussion; where that document refers to `ProcessAsync`, the source-level method name is `ExecuteAsync`.

## Usage example

The handler authorizes only the current tenant, so this example submits that tenant ID. Repeated occurrences are allowed and are each processed.

```csharp
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Operations;

public sealed class TenantBatchService
{
    private readonly IBatchOperationHandler _batchHandler;

    public TenantBatchService(IBatchOperationHandler batchHandler)
    {
        _batchHandler = batchHandler;
    }

    public async Task<BatchOperationResult> RunAsync(
        string currentTenantId,
        CancellationToken cancellationToken)
    {
        var operation = new BatchOperation
        {
            OperationType = "create-backup",
            ResourceIds = new List<string> { currentTenantId },
            Parameters = new Dictionary<string, object>
            {
                ["Label"] = "before-upgrade"
            },
            AtomicityMode = BatchAtomicityMode.CrossTenant
        };

        try
        {
            BatchOperationResult result = await _batchHandler.ExecuteAsync(
                operation,
                cancellationToken);

            BatchOperationStatus status = await _batchHandler.GetStatusAsync(
                operation.OperationId);

            Console.WriteLine(
                $"{status.ProgressPercent}%: {result.GetOverallStatus()}");

            return result;
        }
        catch (BatchTooLargeException ex)
        {
            Console.WriteLine(
                $"Batch rejected: {ex.ActualItemCount}/{ex.MaxItemCount} items, " +
                $"{ex.ActualPayloadSizeBytes}/{ex.MaxPayloadSizeBytes} payload bytes.");
            throw;
        }
    }
}
```

The current internal operation dispatcher is a placeholder: in `CrossTenant` mode it logs and completes successfully, while the `SingleTenant` path returns a simulated transactional success. The public API and validation/status behavior above describe what the present implementation does.
