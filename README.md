// existing content ...

## IExceptionProcessor

The `IExceptionProcessor` interface provides a centralized exception processing and error handling mechanism. It converts exceptions to user-friendly error responses, handles logging, categorization, and HTTP status code mapping.

### Usage Example

```csharp
using SqliteMultiTenant.Exceptions;

// Create an instance of the exception processor
var exceptionProcessor = new ExceptionProcessor(ILogger<ExceptionProcessor>.CreateLogger());

// Process an exception and get the error response
var exception = new Exception("Something went wrong");
var errorResponse = exceptionProcessor.ProcessException(exception);

// Log the exception with context information
exception.LogWithContext(ILogger<ExceptionProcessor>.CreateLogger(), "My Context");

// Check if the exception is transient (can be retried)
var isTransient = exception.IsTransient();
```

// existing content ...
