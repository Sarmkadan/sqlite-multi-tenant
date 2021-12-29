// existing content ...

## DatabaseAccessException

The `DatabaseAccessException` is a custom exception class that represents a database access error. It provides additional context about the error, including the database ID and the type of operation that failed.

### Usage Example

```csharp
using SqliteMultiTenant.Exceptions;

// Create an instance of the exception processor
var exception = DatabaseAccessException.ConnectionFailed("my_database", new Exception("Connection failed"));

// Log the exception with context information
Console.WriteLine(exception.Message);
Console.WriteLine($"Database ID: {exception.DatabaseId}");
Console.WriteLine($"Operation Type: {exception.OperationType}");
```

// existing content ...
