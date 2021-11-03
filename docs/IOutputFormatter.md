# IOutputFormatter

The `IOutputFormatter` interface provides a standardized mechanism for serializing objects into specific string representations. It enables the interchangeable use of various serialization formats—such as JSON, XML, and CSV—within the `sqlite-multi-tenant` framework, ensuring consistent data output across different components.

## API

### IOutputFormatter
The base interface for all serialization formatters.

*   `string Format<T>(T obj)`: Serializes the specified object of type `T` into a formatted string.

### JsonFormatter
An implementation of `IOutputFormatter` that serializes objects into JSON format.

*   `string Format<T>(T obj)`: Serializes the provided object into a JSON-formatted string.

### CsvFormatter
An implementation of `IOutputFormatter` that serializes objects into CSV format.

*   `string Format<T>(T obj)`: Serializes the provided object into a CSV-formatted string.

### XmlFormatter
An implementation of `IOutputFormatter` that serializes objects into XML format.

*   `string Format<T>(T obj)`: Serializes the provided object into an XML-formatted string.

### FormatterFactory
A factory class used to resolve and instantiate the appropriate `IOutputFormatter`.

*   `FormatterFactory()`: Initializes a new instance of the `FormatterFactory` class.
*   `IOutputFormatter GetFormatter()`: Retrieves a default `IOutputFormatter` instance.
*   `IOutputFormatter GetFormatterByContentType(string contentType)`: Retrieves an `IOutputFormatter` instance corresponding to the specified content type string (e.g., "application/json").

### OutputFormatter
A high-level utility class for formatting objects without explicitly resolving a specific formatter instance.

*   `OutputFormatter()`: Initializes a new instance of the `OutputFormatter` class.
*   `string FormatObject(object obj)`: Formats a general object into its string representation using the configured formatter settings.

## Usage

### Example 1: Resolving a formatter via Factory
```csharp
var factory = new FormatterFactory();
// Resolve formatter based on content type
IOutputFormatter formatter = factory.GetFormatterByContentType("application/json");

var data = new { TenantId = 1, TenantName = "ExampleCorp" };
string jsonResult = formatter.Format(data);
```

### Example 2: Using the high-level OutputFormatter
```csharp
var formatter = new OutputFormatter();
var data = new { TenantId = 2, TenantName = "AnotherCorp" };

// Automatically format the object
string formattedResult = formatter.FormatObject(data);
```

## Notes

*   Formatter implementations are generally thread-safe for serialization operations, provided the objects being serialized are not modified by other threads during the serialization process.
*   The `Format<T>` and `FormatObject` methods may throw exceptions if serialization fails, such as when encountering circular references, unsupported types, or data access issues.
*   The `FormatterFactory` will throw if an unsupported or unknown content type is requested via `GetFormatterByContentType`.
