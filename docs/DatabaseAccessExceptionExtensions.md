# DatabaseAccessExceptionExtensions

A static helper class providing extension methods for `DatabaseAccessException` to enhance error diagnostics and streamline exception handling workflows within multi-tenant SQLite applications. These methods facilitate rich error reporting by attaching contextual metadata and providing categorization helpers to distinguish between connection, query, and transaction-related faults.

## API

### WithMessage
Creates a new `DatabaseAccessException` instance with an updated error message, preserving the original exception's inner state and stack trace.

- **Parameters:**
  - `ex`: The source `DatabaseAccessException`.
  - `message`: The new error message to assign.
- **Returns:** A new `DatabaseAccessException` instance.

### WithContext
Attaches additional diagnostic information to the exception, such as tenant identifiers or affected database objects, to aid in troubleshooting.

- **Parameters:**
  - `ex`: The source `DatabaseAccessException`.
  - `context`: A string or object representing the diagnostic context.
- **Returns:** The `DatabaseAccessException` instance with the added context.

### IsConnectionFailure
Determines whether the exception originated from a failure to establish or maintain a connection to the SQLite database.

- **Parameters:**
  - `ex`: The `DatabaseAccessException` to evaluate.
- **Returns:** `true` if the exception is categorized as a connection failure; otherwise, `false`.

### IsQueryFailure
Determines whether the exception occurred during the execution of a SQL command or query.

- **Parameters:**
  - `ex`: The `DatabaseAccessException` to evaluate.
- **Returns:** `true` if the exception is categorized as a query failure; otherwise, `false`.

### IsTransactionFailure
Determines whether the exception is related to transaction management, such as deadlocks, timeouts, or commit/rollback errors.

- **Parameters:**
  - `ex`: The `DatabaseAccessException` to evaluate.
- **Returns:** `true` if the exception is categorized as a transaction failure; otherwise, `false`.

### ToDetailedString
Generates a comprehensive, human-readable string representation of the exception, including stack trace, inner exceptions, and attached diagnostic context.

- **Parameters:**
  - `ex`: The `DatabaseAccessException` to format.
- **Returns:** A formatted string containing detailed error information.

## Usage

### Categorizing Exceptions
```csharp
try
{
    // Execute database operation...
}
catch (DatabaseAccessException ex) when (ex.IsTransactionFailure())
{
    // Handle transient transaction failures (e.g., retry logic)
    logger.LogWarning("Transaction failed for tenant {TenantId}. Retrying...", tenantId);
}
catch (DatabaseAccessException ex) when (ex.IsQueryFailure())
{
    // Handle malformed SQL or schema issues
    logger.LogError("Query error: {Details}", ex.ToDetailedString());
}
```

### Enriching Exceptions with Context
```csharp
public async Task UpdateTenantDataAsync(string tenantId, Data data)
{
    try
    {
        await _repository.UpdateAsync(tenantId, data);
    }
    catch (DatabaseAccessException ex)
    {
        // Add tenant information to the exception before re-throwing
        throw ex.WithContext($"TenantId: {tenantId}");
    }
}
```

## Notes

- **Thread Safety:** These extension methods are thread-safe, as they do not maintain internal state. They operate strictly on the `DatabaseAccessException` instance provided as the `this` parameter.
- **Null Reference Exceptions:** If the source `DatabaseAccessException` instance is `null` when these extension methods are called, a `NullReferenceException` will be thrown. Ensure the exception instance is non-null before calling these methods.
- **Immutability:** While `WithContext` typically modifies the existing exception instance for diagnostic purposes, `WithMessage` returns a new instance. Be mindful of this behavior when chaining these methods.
