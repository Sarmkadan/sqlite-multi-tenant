# OperationStatus

`OperationStatus` and `OperationStatusBase` define the common lifecycle state and timing information for operations. `OperationStatusJsonConverter` serializes lifecycle states as lowercase JSON strings.

## `OperationStatus` enum

| Member | Meaning | JSON value |
| --- | --- | --- |
| `Pending` | The operation is waiting to execute. | `"pending"` |
| `Running` | The operation is currently executing. | `"running"` |
| `Succeeded` | The operation completed successfully. | `"succeeded"` |
| `Failed` | The operation failed. | `"failed"` |

The enum is decorated with `JsonConverterAttribute`, so `System.Text.Json` automatically uses `OperationStatusJsonConverter` whenever an `OperationStatus` value is serialized or deserialized.

## `OperationStatusBase`

`OperationStatusBase` is an abstract base class containing state shared by operation-status models.

### Stored properties

| Property | Type | Description |
| --- | --- | --- |
| `OperationId` | `string` | Unique operation identifier. It defaults to an empty string. |
| `Status` | `OperationStatus` | Current lifecycle status. Its default enum value is `Pending`. |
| `StartedAt` | `DateTime?` | Time at which execution started, or `null` if it has not been recorded. |
| `CompletedAt` | `DateTime?` | Time at which execution finished, or `null` if it has not been recorded. |
| `Error` | `string?` | Failure message, or `null` when no error is recorded. |
| `CreatedAt` | `DateTime` | Time at which the operation was created. |

### Computed properties

All computed properties below have `[JsonIgnore]` and are therefore omitted from JSON.

| Property | Type | Value |
| --- | --- | --- |
| `IsRunning` | `bool` | `true` when `Status == OperationStatus.Running`. |
| `IsCompleted` | `bool` | `true` when `Status == OperationStatus.Succeeded`. |
| `IsFailed` | `bool` | `true` when `Status == OperationStatus.Failed`. |
| `IsPending` | `bool` | `true` when `Status == OperationStatus.Pending`. |
| `DurationMs` | `long?` | The difference between `CompletedAt` and `StartedAt` in whole milliseconds, or `null` unless both timestamps are present. The conversion truncates fractional milliseconds. |
| `State` | `string` | The current enum member name converted to lowercase invariant text, such as `"running"`. |

`IsCompleted` specifically means successful completion; a failed operation has `IsFailed == true` and `IsCompleted == false`.

### Protected methods

Derived status classes can use the following lifecycle helpers.

#### `ValidateStatus()`

Checks these invariants and throws `ArgumentException` when one is violated:

- A non-pending status requires `StartedAt`.
- `Succeeded` requires `CompletedAt`.
- `Failed` is rejected when `CompletedAt` is `null` and `Error` is null or empty. Consequently, the current implementation accepts a failed state when either a completion time or a non-empty error is present.

#### `MarkRunning()`

Sets `Status` to `Running` and initializes `StartedAt` to `DateTime.UtcNow` only when it is currently `null`. An existing start time is preserved.

#### `MarkCompleted()`

Sets `Status` to `Succeeded`, sets `CompletedAt` to `DateTime.UtcNow`, and clears `Error`. It does not initialize `StartedAt`.

#### `MarkFailed(string error)`

Sets `Status` to `Failed`, sets `CompletedAt` to `DateTime.UtcNow`, and assigns the supplied `error`. It does not initialize `StartedAt` or validate that the error is non-empty.

## JSON serialization

`OperationStatusJsonConverter` writes an enum value as a lowercase JSON string. During deserialization, it accepts `pending`, `running`, `succeeded`, and `failed` case-insensitively. Any other value, including a JSON `null`, causes a `JsonException`. The converter expects a JSON string token.

The converter changes only `OperationStatus` values. Property naming and `DateTime` formatting continue to follow the active `JsonSerializerOptions`. The computed properties on `OperationStatusBase` are excluded because they use `[JsonIgnore]`.

### JSON example

With the default `System.Text.Json` property naming policy, a concrete class derived from `OperationStatusBase` can serialize as:

```json
{
  "OperationId": "backup-42",
  "Status": "succeeded",
  "StartedAt": "2026-09-08T10:00:00Z",
  "CompletedAt": "2026-09-08T10:00:01.250Z",
  "Error": null,
  "CreatedAt": "2026-09-08T09:59:55Z"
}
```

`IsRunning`, `IsCompleted`, `IsFailed`, `IsPending`, `DurationMs`, and `State` do not appear in the JSON. For this example, `IsCompleted` is `true`, `DurationMs` is `1250`, and `State` is `"succeeded"`.
