# DatabaseAccessException
The `DatabaseAccessException` class is a custom exception type designed to handle database access-related errors in a multi-tenant SQLite environment. It provides a standardized way to represent and handle exceptions that occur during database operations, allowing for more informative error messages and better error handling.

## API
* `public string? DatabaseId`: Gets the identifier of the database that was being accessed when the exception occurred.
* `public string? OperationType`: Gets the type of operation that was being performed when the exception occurred.
* `public DatabaseAccessException()`: Initializes a new instance of the `DatabaseAccessException` class.
* `public DatabaseAccessException(string? message)`: Initializes a new instance of the `DatabaseAccessException` class with a specified error message.
* `public DatabaseAccessException(string? message, Exception? innerException)`: Initializes a new instance of the `DatabaseAccessException` class with a specified error message and a reference to the inner exception that is the cause of this exception.
* `public static DatabaseAccessException ConnectionFailed`: Gets a pre-initialized `DatabaseAccessException` instance representing a connection failure.
* `public static DatabaseAccessException QueryFailed`: Gets a pre-initialized `DatabaseAccessException` instance representing a query failure.
* `public static DatabaseAccessException TransactionFailed`: Gets a pre-initialized `DatabaseAccessException` instance representing a transaction failure.

## Usage
The following examples demonstrate how to use the `DatabaseAccessException` class in a C# application:
```csharp
try
{
    // Attempt to connect to a database
    using (var connection = new SQLiteConnection("Data Source=:memory:"))
    {
        connection.Open();
        // Perform a query
        using (var command = new SQLiteCommand("SELECT * FROM non_existent_table", connection))
        {
            command.ExecuteNonQuery();
        }
    }
}
catch (DatabaseAccessException ex)
{
    Console.WriteLine($"Database access exception: {ex.Message}");
    Console.WriteLine($"Database ID: {ex.DatabaseId}");
    Console.WriteLine($"Operation type: {ex.OperationType}");
}

try
{
    // Attempt to perform a transaction
    using (var connection = new SQLiteConnection("Data Source=:memory:"))
    {
        connection.Open();
        using (var transaction = connection.BeginTransaction())
        {
            // Perform operations within the transaction
            using (var command = new SQLiteCommand("INSERT INTO non_existent_table (id) VALUES (1)", connection, transaction))
            {
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
    }
}
catch (DatabaseAccessException ex)
{
    Console.WriteLine($"Database access exception: {ex.Message}");
    Console.WriteLine($"Database ID: {ex.DatabaseId}");
    Console.WriteLine($"Operation type: {ex.OperationType}");
}
```

## Notes
When using the `DatabaseAccessException` class, consider the following edge cases and thread-safety remarks:
* The `DatabaseId` and `OperationType` properties may be null if the exception is thrown before these values can be determined.
* The pre-initialized instances (`ConnectionFailed`, `QueryFailed`, `TransactionFailed`) are thread-safe and can be safely accessed from multiple threads.
* When throwing a `DatabaseAccessException` instance, ensure that the error message and inner exception (if applicable) are properly set to provide informative error messages.
* In a multi-threaded environment, consider using a thread-safe logging mechanism to handle exceptions, as the `Console.WriteLine` statements in the examples above may not be thread-safe.
