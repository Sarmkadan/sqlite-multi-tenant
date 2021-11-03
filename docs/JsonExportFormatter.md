# JsonExportFormatter

`JsonExportFormatter` provides specialized JSON serialization and deserialization services designed to support data export and import operations within the `sqlite-multi-tenant` framework. It utilizes `System.Text.Json` to handle object conversions while offering predefined configurations for consistency across data export tasks.

## API

### JsonExportFormatter
Initializes a new instance of the `JsonExportFormatter` class.

### Format<T>(T value)
Serializes the specified object of type `T` into a JSON string using default settings.
- **Parameters**: `value` (the object to serialize).
- **Returns**: A JSON string representing the serialized object.

### Parse<T>(string json)
Deserializes the specified JSON string back into an object of type `T`.
- **Parameters**: `json` (the JSON string to deserialize).
- **Returns**: An object of type `T` derived from the JSON input.
- **Throws**: `JsonException` if the JSON string is invalid or incompatible with type `T`.

### FormatWithOptions<T>(T value, JsonSerializerOptions options)
Serializes the specified object of type `T` using the provided `JsonSerializerOptions`.
- **Parameters**: 
  - `value` (the object to serialize).
  - `options` (the `JsonSerializerOptions` configuration to apply).
- **Returns**: A JSON string representing the serialized object according to the provided options.

### GetMinimalOptions()
A static factory method that returns a `JsonSerializerOptions` instance configured for compact, minimal JSON output, suitable for storage or network transfer.
- **Returns**: A `JsonSerializerOptions` instance with minimal formatting.

### GetVerboseOptions()
A static factory method that returns a `JsonSerializerOptions` instance configured for readable JSON output (e.g., indentation), suitable for logging or human-readable exports.
- **Returns**: A `JsonSerializerOptions` instance with verbose, human-readable formatting.

## Usage

### Basic Serialization and Parsing
```csharp
var formatter = new JsonExportFormatter();
var myData = new TenantSettings { Id = 1, Name = "Alpha" };

// Serialize
string json = formatter.Format(myData);

// Deserialize
var parsedData = formatter.Parse<TenantSettings>(json);
```

### Using Predefined Options
```csharp
var formatter = new JsonExportFormatter();
var data = new List<TenantRecord> { /* ... */ };

// Serialize with verbose options for readability
string verboseJson = formatter.FormatWithOptions(data, JsonExportFormatter.GetVerboseOptions());

// Serialize with minimal options for compact storage
string compactJson = formatter.FormatWithOptions(data, JsonExportFormatter.GetMinimalOptions());
```

## Notes

- **Thread Safety**: `JsonExportFormatter` is stateless and thread-safe for serialization and deserialization operations, provided the underlying `JsonSerializerOptions` are not modified concurrently.
- **Null Handling**: When using `Parse<T>`, if the JSON input is `null` or empty, the method behavior adheres to standard `System.Text.Json` deserialization practices, typically returning `default(T)` or throwing depending on the framework version and input content.
- **Exceptions**: As this class wraps `System.Text.Json`, callers should anticipate and handle `JsonException` for malformed JSON during `Parse<T>` operations and `NotSupportedException` if the object graph contains types that cannot be serialized.
