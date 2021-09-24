# ConflictResolutionService

`ConflictResolutionService` detects and resolves data conflicts that arise when synchronizing tenant-specific records between local and remote sources. It provides a structured pipeline: first identifying conflicting fields via `DetectConflicts`, then producing a resolution strategy through `ResolveConflictsAsync`, and finally persisting the chosen resolution with `ApplyResolutionAsync`.

## API

### ConflictResolutionService

```csharp
public sealed class ConflictResolutionService
```

The service class itself. It is sealed and cannot be subclassed.

#### Constructor

```csharp
public ConflictResolutionService()
```

Initializes a new instance of the service. The parameterless constructor implies that all necessary dependencies are either self-contained or resolved internally.

#### DetectConflicts

```csharp
public ConflictDetectionResult DetectConflicts(object localRecord, object remoteRecord)
```

Compares two record objects field-by-field and returns a `ConflictDetectionResult` containing any detected discrepancies.

- **Parameters**:
  - `localRecord` — The local version of the record.
  - `remoteRecord` — The remote version of the record.
- **Returns**: A `ConflictDetectionResult` whose `Conflicts` list contains one `DataConflict` per mismatched field.
- **Exceptions**: Throws `ArgumentNullException` if either argument is `null`. Throws `InvalidOperationException` if the two objects are of different types and cannot be compared.

#### ResolveConflictsAsync

```csharp
public async Task<ConflictResolutionResult> ResolveConflictsAsync(
    ConflictDetectionResult detectionResult,
    ResolutionStrategy strategy)
```

Asynchronously determines resolved values for each conflict according to the supplied strategy.

- **Parameters**:
  - `detectionResult` — The result returned by `DetectConflicts`.
  - `strategy` — A `ResolutionStrategy` enum value (e.g., `TakeLocal`, `TakeRemote`, `Merge`) that dictates how each conflict is resolved.
- **Returns**: A `ConflictResolutionResult` containing a `ResolvedValues` dictionary keyed by field name and a success flag.
- **Exceptions**: Throws `ArgumentNullException` if `detectionResult` is `null`. Throws `ArgumentException` if the strategy is not defined.

#### ApplyResolutionAsync

```csharp
public async Task<bool> ApplyResolutionAsync(
    ConflictResolutionResult resolution,
    object targetRecord)
```

Writes the resolved values from a `ConflictResolutionResult` into the provided target record and persists the changes.

- **Parameters**:
  - `resolution` — The result from `ResolveConflictsAsync`.
  - `targetRecord` — The record object to update.
- **Returns**: `true` if the resolution was applied and persisted successfully; `false` otherwise.
- **Exceptions**: Throws `ArgumentNullException` if either argument is `null`. Throws `InvalidOperationException` if the target record does not contain a field present in `ResolvedValues`.

---

### ConflictDetectionResult

```csharp
public sealed class ConflictDetectionResult
```

Holds the outcome of a conflict detection pass.

#### Conflicts

```csharp
public List<DataConflict> Conflicts { get; }
```

A list of all detected conflicts. Empty when the local and remote records are identical.

#### AddConflict

```csharp
public void AddConflict(DataConflict conflict)
```

Appends a `DataConflict` to the `Conflicts` list. Typically called internally by `DetectConflicts`, but exposed for manual augmentation in advanced scenarios.

- **Parameters**:
  - `conflict` — The conflict to add.
- **Exceptions**: Throws `ArgumentNullException` if `conflict` is `null`.

---

### DataConflict

```csharp
public sealed class DataConflict
```

Describes a single conflicting field between two record versions.

#### Field

```csharp
public string Field { get; }
```

The name of the field where the conflict occurred.

#### ConflictType

```csharp
public ConflictType ConflictType { get; }
```

An enum value indicating the nature of the conflict (e.g., `ValueMismatch`, `DeletedLocally`, `DeletedRemotely`).

#### LocalValue

```csharp
public object LocalValue { get; }
```

The value of the field in the local record. May be `null`.

#### RemoteValue

```csharp
public object RemoteValue { get; }
```

The value of the field in the remote record. May be `null`.

---

### ConflictResolutionResult

```csharp
public sealed class ConflictResolutionResult
```

Represents the outcome of a conflict resolution attempt.

#### ResolvedValues

```csharp
public Dictionary<string, object> ResolvedValues { get; }
```

A dictionary mapping field names to their resolved values. Only fields that were in conflict are present.

#### IsSuccessful

```csharp
public bool IsSuccessful { get; }
```

`true` if resolution completed without errors; `false` if a problem occurred (check `Error` for details).

#### Error

```csharp
public string Error { get; }
```

An error message describing why resolution failed. `null` or empty when `IsSuccessful` is `true`.

## Usage

### Example 1: Detect, resolve with local-wins strategy, and apply

```csharp
var local = tenantRepo.GetLocalRecord(recordId);
var remote = await syncService.FetchRemoteRecordAsync(recordId);

var service = new ConflictResolutionService();

// Detect
var detection = service.DetectConflicts(local, remote);

if (detection.Conflicts.Any())
{
    // Resolve taking local values
    var resolution = await service.ResolveConflictsAsync(
        detection,
        ResolutionStrategy.TakeLocal);

    if (resolution.IsSuccessful)
    {
        bool applied = await service.ApplyResolutionAsync(resolution, local);
        if (applied)
        {
            Console.WriteLine("Conflicts resolved and saved locally.");
        }
    }
    else
    {
        Console.WriteLine($"Resolution failed: {resolution.Error}");
    }
}
```

### Example 2: Manual conflict inspection and selective merging

```csharp
var detection = service.DetectConflicts(localRecord, remoteRecord);

foreach (var conflict in detection.Conflicts)
{
    Console.WriteLine(
        $"Field '{conflict.Field}' — Local: {conflict.LocalValue}, Remote: {conflict.RemoteValue}");

    // Custom logic: for a specific field, prefer remote if local is null
    if (conflict.Field == "LastUpdatedBy" && conflict.LocalValue == null)
    {
        var customResolution = new ConflictResolutionResult
        {
            IsSuccessful = true,
            ResolvedValues = new Dictionary<string, object>
            {
                { conflict.Field, conflict.RemoteValue }
            }
        };

        await service.ApplyResolutionAsync(customResolution, localRecord);
    }
}
```

## Notes

- **Thread safety**: `ConflictResolutionService` holds no observable instance state. Its methods are safe to call concurrently from multiple threads, provided the record objects passed in are not mutated during the operation.
- **Type matching**: `DetectConflicts` requires the local and remote objects to share the same schema. Passing two unrelated types results in an `InvalidOperationException`.
- **Null values**: `DataConflict.LocalValue` and `DataConflict.RemoteValue` can both be `null`. A conflict where one side is `null` and the other is not is still reported as a conflict (typically with a `ConflictType` of `ValueMismatch`).
- **Empty conflicts**: `ConflictDetectionResult.Conflicts` may be an empty list. Calling `ResolveConflictsAsync` on an empty detection result succeeds immediately with an empty `ResolvedValues` dictionary.
- **Partial application**: `ApplyResolutionAsync` writes only the fields present in `ResolvedValues`. Other fields on the target record remain unchanged. If a resolved field does not exist on the target record, an `InvalidOperationException` is thrown.
- **Error state**: When `ConflictResolutionResult.IsSuccessful` is `false`, the `ResolvedValues` dictionary may be partially populated or empty. Consumers should always check `IsSuccessful` before using the resolved values.
