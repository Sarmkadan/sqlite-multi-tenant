# BulkInsertBuilder
The `BulkInsertBuilder` class is designed to facilitate bulk insertion of records into a SQLite database. It provides a fluent API for adding records and executing the insertion operation, allowing for efficient and flexible data import.

## API
* `BulkInsertBuilder()`: Initializes a new instance of the `BulkInsertBuilder` class.
* `AddRecord()`: Adds a single record to the bulk insertion operation. Returns the current `BulkInsertBuilder` instance.
* `AddRecords()`: Adds multiple records to the bulk insertion operation. Returns the current `BulkInsertBuilder` instance.
* `ExecuteAsync()`: Executes the bulk insertion operation asynchronously. Returns a `BulkInsertResult` object containing information about the operation's outcome.
* `GenerateSqlStatements()`: Generates the SQL statements for the bulk insertion operation. Returns a string containing the SQL statements.

The `BulkInsertResult` class contains the following properties:
* `TotalRecords`: The total number of records that were attempted to be inserted.
* `InsertedRecords`: The number of records that were successfully inserted.
* `IsSuccessful`: A boolean indicating whether the operation was successful.
* `Error`: A string containing any error message that occurred during the operation.

## Usage
```csharp
// Example 1: Simple bulk insertion
var builder = new BulkInsertBuilder();
builder.AddRecord(new { Name = "John Doe", Age = 30 });
builder.AddRecord(new { Name = "Jane Doe", Age = 25 });
var result = await builder.ExecuteAsync();
Console.WriteLine($"Inserted {result.InsertedRecords} out of {result.TotalRecords} records.");
```

```csharp
// Example 2: Bulk insertion with multiple records
var records = new[]
{
    new { Name = "John Doe", Age = 30 },
    new { Name = "Jane Doe", Age = 25 },
    new { Name = "Bob Smith", Age = 40 }
};
var builder = new BulkInsertBuilder();
builder.AddRecords(records);
var result = await builder.ExecuteAsync();
Console.WriteLine($"Inserted {result.InsertedRecords} out of {result.TotalRecords} records.");
```

## Notes
* The `BulkInsertBuilder` class is not thread-safe, and concurrent access to its methods may result in unexpected behavior.
* The `ExecuteAsync` method may throw exceptions if the underlying database operation fails. It is recommended to handle these exceptions accordingly.
* The `GenerateSqlStatements` method can be used to inspect the generated SQL statements before executing the operation. However, it is not recommended to modify the generated SQL statements, as this may compromise the integrity of the bulk insertion operation.
