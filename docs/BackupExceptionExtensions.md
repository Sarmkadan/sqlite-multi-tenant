# BackupExceptionExtensions

`BackupExceptionExtensions` provides a set of static extension methods for the `Exception` class, designed to facilitate the identification and diagnostic analysis of exceptions that arise within the `sqlite-multi-tenant` backup, verification, and restoration workflows. These methods encapsulate common logic required to categorize failure states and extract meaningful error details, promoting consistent error handling and logging practices across the application.

## API

### IsCreationFailure
Identifies whether the exception originated during the backup creation process.

*   **Parameters:** `this Exception exception`
*   **Return Value:** `bool` - `true` if the exception indicates a failure during backup creation; otherwise `false`.
*   **Exceptions:** Throws `ArgumentNullException` if the provided `exception` is `null`.

### IsVerificationFailure
Identifies whether the exception originated during the backup verification process.

*   **Parameters:** `this Exception exception`
*   **Return Value:** `bool` - `true` if the exception indicates a failure during backup verification; otherwise `false`.
*   **Exceptions:** Throws `ArgumentNullException` if the provided `exception` is `null`.

### IsRestoreFailure
Identifies whether the exception originated during the database restoration process.

*   **Parameters:** `this Exception exception`
*   **Return Value:** `bool` - `true` if the exception indicates a failure during restoration; otherwise `false`.
*   **Exceptions:** Throws `ArgumentNullException` if the provided `exception` is `null`.

### GetErrorDetails
Extracts the specific error information associated with a backup-related exception.

*   **Parameters:** `this Exception exception`
*   **Return Value:** `string` - A string containing detailed information about the failure, extracted from the exception.
*   **Exceptions:** Throws `ArgumentNullException` if the provided `exception` is `null`.

## Usage

### Categorized Exception Handling
This example demonstrates how to use the extension methods to handle different types of backup failures specifically.

```csharp
try
{
    // Perform backup operation
}
catch (Exception ex)
{
    if (ex.IsCreationFailure())
    {
        _logger.LogError("Backup creation failed: {Details}", ex.GetErrorDetails());
    }
    else if (ex.IsVerificationFailure())
    {
        _logger.LogError("Backup verification failed: {Details}", ex.GetErrorDetails());
    }
    else
    {
        _logger.LogError("An unexpected error occurred: {Message}", ex.Message);
    }
}
```

### Logging Restoration Errors
This example highlights using `GetErrorDetails` to ensure restoration errors are logged with appropriate context.

```csharp
public void RestoreDatabase(string backupPath)
{
    try
    {
        // Perform restoration
    }
    catch (Exception ex) when (ex.IsRestoreFailure())
    {
        string details = ex.GetErrorDetails();
        throw new InvalidOperationException($"Restoration failed: {details}", ex);
    }
}
```

## Notes

*   **Thread-Safety:** These methods are inherently thread-safe, as they perform read-only operations on the `Exception` instance provided. They do not maintain or modify any internal static state.
*   **Input Validation:** All extension methods in this class will throw an `ArgumentNullException` if a `null` exception instance is passed. Ensure that proper null checks are performed before calling these methods if there is a possibility that the caught exception is `null`.
*   **Extension Method Usage:** As these are extension methods, ensure `using` statements are correctly configured to include the namespace containing `BackupExceptionExtensions`.
