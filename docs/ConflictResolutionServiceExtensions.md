# ConflictResolutionServiceExtensions

The `ConflictResolutionServiceExtensions` class provides a set of static extension methods designed to facilitate the detection, analysis, and resolution of data conflicts within the `sqlite-multi-tenant` architecture. These utilities streamline the workflow for handling concurrent modifications across tenant databases by offering standardized mechanisms to identify conflicting fields, categorize conflict types, and apply resolution strategies with built-in retry logic.

## API

### CreateConflictDetectionResult
Generates a new `ConflictDetectionResult` instance based on the provided detection parameters. This method serves as the entry point for initializing a conflict detection session, encapsulating the state required to track discrepancies between local and remote data versions.
*   **Parameters**: Accepts arguments necessary to define the scope and context of the detection (specific signature details depend on the underlying `ConflictDetectionResult` constructor requirements).
*   **Returns**: A populated `ConflictDetectionResult` object.
*   **Throws**: May throw an exception if the input parameters are invalid or if the internal state cannot be initialized.

### GetConflictingFields
Analyzes the data entities involved in a conflict to identify specific fields that differ between versions.
*   **Parameters**: Takes the relevant data objects or conflict context required for comparison.
*   **Returns**: A `Dictionary<string, ConflictResolutionStrategy>` where the key is the field name and the value is the recommended or assigned resolution strategy for that field.
*   **Throws**: Throws if the entities provided are null or incompatible for comparison.

### ResolveConflictsAsync
Asynchronously processes a detected conflict using the defined strategies to produce a final resolution outcome. This method orchestrates the application of business rules to determine the winning values for conflicting fields.
*   **Parameters**: Requires a `ConflictDetectionResult` or similar context object containing the conflict data and strategies.
*   **Returns**: A `Task<ConflictResolutionResult>` representing the outcome of the resolution process, including success status and resolved data.
*   **Throws**: Throws `OperationCanceledException` if the cancellation token is triggered, or custom exceptions if the resolution logic fails (e.g., irreconcilable differences).

### ApplyResolutionWithRetryAsync
Attempts to apply the resolved changes to the database with an automatic retry mechanism in case of transient failures, such as database locks or temporary connectivity issues common in multi-tenant SQLite environments.
*   **Parameters**: Accepts the `ConflictResolutionResult` and potentially retry configuration (count, delay).
*   **Returns**: A `Task<bool>` indicating whether the resolution was successfully applied (`true`) or if all retry attempts failed (`false`).
*   **Throws**: May throw non-transient exceptions immediately (e.g., constraint violations), while transient errors trigger retries before returning `false` or throwing a final aggregate exception depending on implementation specifics.

### HasConflictType
Checks whether a specific type of conflict exists within the current detection result. This allows for quick filtering and branching logic based on the nature of the discrepancy (e.g., Update vs. Delete conflicts).
*   **Parameters**: The `ConflictDetectionResult` instance and the `ConflictType` enum value to check.
*   **Returns**: A `bool` value; `true` if the specified conflict type is present, otherwise `false`.
*   **Throws**: Throws if the `ConflictDetectionResult` is null.

### GetFirstConflictOfType
Retrieves the first occurrence of a specific conflict type from the detection results. This is useful when the handling logic needs to inspect the details of a particular category of conflict without iterating the entire collection.
*   **Parameters**: The `ConflictDetectionResult` instance and the target `ConflictType`.
*   **Returns**: A `DataConflict?` (nullable) containing the conflict details if found, or `null` if no matching conflict exists.
*   **Throws**: Throws if the `ConflictDetectionResult` is null.

## Usage

### Example 1: Detecting and Analyzing Field Conflicts
The following example demonstrates how to initialize a detection result, identify specific conflicting fields, and determine if a specific conflict type (e.g., `UpdateUpdate`) exists before proceeding.

```csharp
using SqliteMultiTenant.Conflicts;

// Assume 'localData' and 'remoteData' are the entities being compared
var detectionResult = ConflictResolutionServiceExtensions.CreateConflictDetectionResult(localData, remoteData);

// Check specifically for concurrent update conflicts
if (ConflictResolutionServiceExtensions.HasConflictType(detectionResult, ConflictType.UpdateUpdate))
{
    // Retrieve the specific details of the first update conflict
    var conflict = ConflictResolutionServiceExtensions.GetFirstConflictOfType(detectionResult, ConflictType.UpdateUpdate);
    
    if (conflict != null)
    {
        // Get a map of fields that differ and their suggested strategies
        var conflictingFields = ConflictResolutionServiceExtensions.GetConflictingFields(detectionResult);
        
        foreach (var field in conflictingFields)
        {
            Console.WriteLine($"Field '{field.Key}' requires strategy: {field.Value}");
        }
    }
}
```

### Example 2: Resolving and Applying Changes with Retry
This example shows the full lifecycle of resolving a conflict asynchronously and applying the changes to the database with automatic retry handling.

```csharp
using SqliteMultiTenant.Conflicts;

public async Task<bool> HandleConflictAsync(ConflictDetectionResult detectionResult)
{
    // Execute the resolution logic to determine final values
    var resolutionResult = await ConflictResolutionServiceExtensions.ResolveConflictsAsync(detectionResult);

    if (resolutionResult.IsSuccess)
    {
        // Attempt to apply the resolution, retrying on transient DB errors
        bool applied = await ConflictResolutionServiceExtensions.ApplyResolutionWithRetryAsync(
            resolutionResult, 
            maxRetries: 3, 
            delayMs: 500
        );

        if (!applied)
        {
            // Log failure after retries exhausted
            Console.Error.WriteLine("Failed to apply conflict resolution after multiple attempts.");
        }
        
        return applied;
    }

    return false;
}
```

## Notes

*   **Thread Safety**: As this class consists entirely of static methods that operate on passed-in instances (`ConflictDetectionResult`, `DataConflict`), the methods themselves are stateless and thread-safe. However, the objects passed as arguments are not guaranteed to be thread-safe; callers must ensure that the `ConflictDetectionResult` instance is not modified concurrently by other threads during these operations.
*   **Null Handling**: Methods such as `HasConflictType` and `GetFirstConflictOfType` explicitly rely on valid input instances. Passing a `null` `ConflictDetectionResult` will result in an exception. Callers should validate the existence of a detection result before invoking these helpers.
*   **Retry Logic Behavior**: The `ApplyResolutionWithRetryAsync` method is designed to handle transient SQLite locking exceptions (`SQLITE_BUSY`). It will not retry on logical errors such as constraint violations or data corruption; these will propagate immediately. The return value `false` indicates that all retry attempts were exhausted due to persistent transient failures.
*   **Nullable Return Types**: The `GetFirstConflictOfType` method returns a nullable `DataConflict?`. Consumers must check for `null` before accessing properties of the returned conflict object to avoid `NullReferenceException`.
