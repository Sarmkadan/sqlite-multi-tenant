# MigrationResult

The `MigrationResult` record represents the outcome of a database migration operation. It encapsulates success status, the number of applied migrations, and any error information that occurred during the process.

## Properties

### `Success`
```csharp
public bool Success { get; init; }
```
Indicates whether the migration operation completed successfully.

### `AppliedCount`
```csharp
public int AppliedCount { get; init; }
```
The total number of migrations that were successfully applied during the operation.

### `Error`
```csharp
public string? Error { get; init; }
```
Contains the error message if the migration failed. Returns `null` if the operation was successful.

### `IsSuccess`
```csharp
public bool IsSuccess => Success && string.IsNullOrEmpty(Error);
```
A computed property that returns `true` only if `Success` is `true` and `Error` is null or empty.

### `ResultSummary`
```csharp
public string ResultSummary { get; }
```
Provides a human-readable summary of the migration result. The output format varies based on the success state:
- **Success**: `"Success: {AppliedCount} migration(s) applied"` or `"Success"`
- **Failure**: `"Failed: {Error}"` or `"Failed: Unknown error occurred"`

## Factory Methods

### `SuccessResult`
```csharp
public static MigrationResult SuccessResult(int appliedCount = 0)
```
Creates a new `MigrationResult` instance representing a successful migration.
- **Parameters**:
  - `appliedCount`: Number of migrations applied (defaults to `0`).
- **Returns**: A `MigrationResult` with `Success = true`, `AppliedCount` set, and `Error = null`.

### `FailureResult`
```csharp
public static MigrationResult FailureResult(string error)
```
Creates a new `MigrationResult` instance representing a failed migration.
- **Parameters**:
  - `error`: The error message describing why the migration failed.
- **Returns**: A `MigrationResult` with `Success = false`, `AppliedCount = 0`, and `Error` set.

## Example Usage

```csharp
using SqliteMultiTenant.Models;

// Successful migration with 3 applied migrations
var successResult = MigrationResult.SuccessResult(appliedCount: 3);
Console.WriteLine(successResult.ResultSummary); 
// Output: Success: 3 migration(s) applied

// Successful migration with 0 applied migrations (no-op)
var noOpResult = MigrationResult.SuccessResult();
Console.WriteLine(noOpResult.ResultSummary); 
// Output: Success

// Failed migration
var failureResult = MigrationResult.FailureResult("Connection timeout");
Console.WriteLine(failureResult.ResultSummary); 
// Output: Failed: Connection timeout

// Check status
if (failureResult.IsSuccess)
{
    Console.WriteLine("Migration succeeded");
}
else
{
    Console.WriteLine($"Migration failed: {failureResult.Error}");
}
```
