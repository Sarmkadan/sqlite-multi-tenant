# ExceptionProcessor

`ExceptionProcessor` is the built-in implementation of [`IExceptionProcessor`](IExceptionProcessor.md). It converts an `Exception` into an `ErrorResponse`, maps known exception types to HTTP status codes and categories, and logs each processed exception.

Namespace: `SqliteMultiTenant.Exceptions`

## Construction

```csharp
public ExceptionProcessor(ILogger<ExceptionProcessor> logger)
```

The logger is used to record the exception category, mapped status code, generated error ID, exception type, and original exception message. A logging failure, or any other failure while processing the exception, is handled by the fallback response described below.

## Public methods

### `ProcessException`

```csharp
public ErrorResponse ProcessException(Exception exception)
```

Creates an `ErrorResponse` containing:

- a newly generated GUID string in `ErrorId`;
- the category returned by `GetErrorCategory`;
- the status code returned by `GetHttpStatusCode`;
- a user-facing message selected from the exception type;
- `DateTime.UtcNow` in `Timestamp`;
- details containing `ExceptionType`, the original `Message`, `Source` (or `"Unknown"`), and `StackTrace` (or `"Not available"`);
- a `TenantId` detail for `TenantNotFoundException`, using `"Unknown"` when its value is null; and
- a nested `ErrorResponse` when the exception has an immediate inner exception. This nested response has its own error ID, the category `"InnerException"`, the inner exception's original message, and a UTC timestamp. It does not recursively process deeper inner exceptions.

The user-facing message mapping is:

| Exception type | Message |
| --- | --- |
| `TenantNotFoundException` | `Tenant with ID '{TenantId}' was not found.` |
| `DatabaseAccessException` | `Unable to access database. Please try again later.` |
| `MigrationException` | `Migration error: {exception.Message}` |
| `BackupException` | `Backup error: {exception.Message}` |
| `ArgumentException` or `ArgumentNullException` | The original exception message |
| `TimeoutException` | `The operation timed out. Please try again.` |
| `UnauthorizedAccessException` | `You do not have permission to access this resource.` |
| `InvalidOperationException` | `The operation is not valid at this time.` |
| Any other exception, including `KeyNotFoundException` | `An unexpected error occurred. Please try again later.` |

If processing itself throws, the method logs that processing error and returns a minimal response with a new error ID, category `"UnexpectedError"`, message `"An unexpected error occurred"`, status code `500`, and a UTC timestamp.

### `GetHttpStatusCode`

```csharp
public int GetHttpStatusCode(Exception exception)
```

### `GetErrorCategory`

```csharp
public string GetErrorCategory(Exception exception)
```

These methods apply the following mappings:

| Exception type | HTTP status code | Category |
| --- | ---: | --- |
| `ArgumentException` or `ArgumentNullException` | `400` | `ValidationError` |
| `KeyNotFoundException` | `404` | `NotFound` |
| `UnauthorizedAccessException` | `401` | `Unauthorized` |
| `InvalidOperationException` | `409` | `InvalidOperation` |
| `TimeoutException` | `408` | `Timeout` |
| `TenantNotFoundException` | `404` | `TenantNotFound` |
| `DatabaseAccessException` | `500` | `DatabaseError` |
| `MigrationException` | `400` | `MigrationError` |
| `BackupException` | `500` | `BackupError` |
| Any other exception | `500` | `UnexpectedError` |

## Usage example

Register the implementation against its contract, then process an exception where errors are translated into an HTTP response:

```csharp
using SqliteMultiTenant.Exceptions;

builder.Services.AddSingleton<IExceptionProcessor, ExceptionProcessor>();

// In an endpoint, middleware component, or other service:
try
{
    await PerformTenantOperationAsync();
}
catch (Exception exception)
{
    ErrorResponse error = exceptionProcessor.ProcessException(exception);
    return Results.Json(error, statusCode: error.StatusCode);
}
```

`GetHttpStatusCode` and `GetErrorCategory` can also be called independently when only the mapping is needed.
