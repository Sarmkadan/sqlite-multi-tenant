# ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` is a sealed component designed to intercept request processing within the `sqlite-multi-tenant` application pipeline, capturing exceptions and operational failures to return standardized `Result<T>` responses. By encapsulating error logic within this middleware, the system ensures consistent error propagation and prevents unhandled exceptions from leaking raw stack traces or internal state to the client, while maintaining a clear separation between successful data retrieval and failure states via the generic `Result<T>` pattern.

## API

### `ErrorHandlingMiddleware` Class
A sealed class that implements the middleware logic for error interception. Being sealed, it cannot be inherited, ensuring the error handling behavior remains consistent and unmodified throughout the application lifecycle.

### `ErrorHandlingMiddleware()`
Constructs a new instance of the middleware. This constructor typically accepts dependencies via dependency injection (though specific parameters are not exposed in the public signature provided), initializing the component for use within the request pipeline.

### `Task InvokeAsync(HttpContext context)`
The primary entry point executed by the hosting environment for each request.
*   **Purpose**: Executes the next delegate in the pipeline within a try-catch block. If an exception occurs, it catches the error and writes a failure `Result` to the response; otherwise, it allows the response to proceed or wraps the successful outcome.
*   **Parameters**:
    *   `context`: The current `HttpContext` containing request and response data.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Throws**: This method is designed to swallow exceptions and convert them into failure results; it should not throw under normal operational conditions unless the underlying infrastructure fails critically.

### `Result<T>` Class
A sealed generic container used to encapsulate the outcome of an operation, distinguishing between success and failure states without using exceptions for control flow.

### `bool IsSuccess`
Indicates whether the operation completed successfully.
*   **Purpose**: A flag used by consumers to determine if `Value` contains valid data or if `ErrorMessage` contains diagnostic information.
*   **Value**: `true` if the result represents success; `false` otherwise.

### `T? Value`
Holds the payload of the operation if successful.
*   **Purpose**: Contains the returned data when `IsSuccess` is `true`.
*   **Value**: The generic type `T` if successful; `null` if the operation failed or if the successful result inherently contains no data.

### `string? ErrorMessage`
Holds the description of the failure if the operation was unsuccessful.
*   **Purpose**: Provides a human-readable or machine-parseable explanation of the error when `IsSuccess` is `false`.
*   **Value**: A string describing the error if failed; `null` if the operation was successful.

### `static Result<T> Success(T value)`
Factory method to create a successful result.
*   **Purpose**: Constructs a `Result<T>` instance with `IsSuccess` set to `true` and `Value` populated.
*   **Parameters**:
    *   `value`: The data to wrap in the result.
*   **Return Value**: A `Result<T>` instance representing success.
*   **Throws**: None.

### `static Result<T> Failure(string errorMessage)`
Factory method to create a failed result.
*   **Purpose**: Constructs a `Result<T>` instance with `IsSuccess` set to `false` and `ErrorMessage` populated.
*   **Parameters**:
    *   `errorMessage`: The description of the error.
*   **Return Value**: A `Result<T>` instance representing failure.
*   **Throws**: None.

## Usage

### Example 1: Wrapping a Service Call
This example demonstrates how a service layer might use the `Result<T>` pattern to return data or errors without throwing exceptions, which the middleware can then process uniformly.

```csharp
public class TenantService
{
    public async Task<Result<TenantData>> GetTenantAsync(string id)
    {
        try
        {
            var data = await Database.FetchTenantAsync(id);
            if (data == null)
            {
                return Result<TenantData>.Failure($"Tenant with ID '{id}' not found.");
            }
            return Result<TenantData>.Success(data);
        }
        catch (Exception ex)
        {
            return Result<TenantData>.Failure($"Database error: {ex.Message}");
        }
    }
}

// Consumption in a Controller or Handler
var result = await _tenantService.GetTenantAsync("tenant-123");
if (!result.IsSuccess)
{
    // Handle error logic locally or return to middleware
    return StatusCode(500, result.ErrorMessage);
}

var tenant = result.Value;
```

### Example 2: Middleware Pipeline Configuration
This example shows how the `ErrorHandlingMiddleware` is registered in the application startup to ensure all requests pass through the error interception logic.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddScoped<TenantService>();

var app = builder.Build();

// Insert middleware into the pipeline
// It should be placed early enough to catch errors from downstream components
app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapGet("/tenants/{id}", async (string id, TenantService service) =>
{
    var result = await service.GetTenantAsync(id);
    
    if (!result.IsSuccess)
    {
        // The middleware may handle the final response formatting, 
        // but returning the result object allows consistent typing
        return Results.BadRequest(result.ErrorMessage);
    }

    return Results.Ok(result.Value);
});

app.Run();
```

## Notes

*   **Thread Safety**: The `ErrorHandlingMiddleware` class is sealed and stateless regarding request-specific data (state is passed via the `HttpContext` parameter), making it safe for concurrent use across multiple threads handling simultaneous requests. The `Result<T>` class is also immutable once constructed via its static factory methods, ensuring thread safety when sharing result instances.
*   **Null Handling**: Consumers must check `IsSuccess` before accessing `Value`. If `IsSuccess` is `false`, `Value` will be `null` (for reference types) or default (for value types), and accessing it without verification may lead to `NullReferenceException` in strict null-context environments. Similarly, `ErrorMessage` is only guaranteed to be populated when `IsSuccess` is `false`.
*   **Exception Swallowing**: The `InvokeAsync` method is designed to catch exceptions. If specific critical errors (e.g., `OutOfMemoryException`, `StackOverflowException`) need to bypass this middleware and crash the process for diagnostic purposes, additional filtering logic would be required inside the implementation, as the public signature implies a blanket interception strategy.
*   **Generic Constraints**: The `Result<T>` type accepts any `T`, including nullable reference types. When `T` is a reference type, `Value` is explicitly nullable (`T?`), reinforcing the need for state checking via `IsSuccess`.
