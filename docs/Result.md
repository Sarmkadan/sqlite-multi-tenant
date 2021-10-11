# Result
The `Result` class is a generic type used to represent the outcome of an operation, providing a standardized way to handle success and failure scenarios. It contains properties to indicate success, retrieve data, and access error messages and metadata. This class is designed to simplify error handling and provide a more expressive way to communicate the outcome of operations.

## API
### Result<T>
* `public bool Success`: Indicates whether the operation was successful.
* `public T? Data`: Contains the result data if the operation was successful.
* `public List<string> Errors`: A list of error messages if the operation failed.
* `public string? Message`: An optional message providing additional context.
* `public ResultMetadata? Metadata`: Optional metadata associated with the result.
* `public static Result<T> Ok`: A static property representing a successful result with no data.
* `public static Result<T> Fail`: A static property representing a failed result with no data or errors.

### PaginatedResult<T>
* `public bool Success`: Indicates whether the operation was successful.
* `public List<T> Items`: A list of items if the operation was successful.
* `public PaginationMetadata Pagination`: Metadata related to pagination.
* `public List<string> Errors`: A list of error messages if the operation failed.
* `public string? Message`: An optional message providing additional context.
* `public static PaginatedResult<T> Ok`: A static property representing a successful paginated result.
* `public static PaginatedResult<T> Fail`: A static property representing a failed paginated result.

### ResultMetadata
* `public DateTime Timestamp`: The timestamp when the result was generated.
* `public string? TraceId`: An optional trace ID for logging and debugging purposes.

## Usage
The following examples demonstrate how to use the `Result` class:
```csharp
// Example 1: Successful operation
var result = new Result<string> { Success = true, Data = "Hello, World!" };
if (result.Success)
{
    Console.WriteLine(result.Data); // Output: Hello, World!
}

// Example 2: Failed operation with error message
var failedResult = new Result<string> { Success = false, Errors = new List<string> { "Invalid input" } };
if (!failedResult.Success)
{
    Console.WriteLine(string.Join(", ", failedResult.Errors)); // Output: Invalid input
}
```

## Notes
When using the `Result` class, consider the following edge cases:
* If `Success` is `true`, `Data` should be non-null, and `Errors` should be empty.
* If `Success` is `false`, `Data` should be null, and `Errors` should contain at least one error message.
* The `Result` class is designed to be thread-safe, as it only contains immutable properties. However, when creating instances of `Result`, ensure that the properties are properly synchronized to avoid concurrency issues.
