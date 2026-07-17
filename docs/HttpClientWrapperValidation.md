# HttpClientWrapperValidation

`HttpClientWrapperValidation` is a static utility class responsible for validating the configuration and components of HTTP client requests within the `sqlite-multi-tenant` framework. It ensures that URLs, bearer tokens, headers, payloads, and response types conform to expected formats and constraints before being used in HTTP operations. This validation helps prevent runtime errors and enforces consistency in API interactions.

## API

### `Validate`
- **Purpose**: Aggregates validation results for all HTTP client components (URL, bearer token, headers, payload).
- **Parameters**: None (validates internal or default state).
- **Return Value**: `IReadOnlyList<string>` containing error messages for invalid configurations.
- **Exceptions**: None thrown; returns empty list if valid.

### `IsValid`
- **Purpose**: Indicates whether the current HTTP client configuration is valid.
- **Parameters**: None.
- **Return Value**: `bool` - `true` if no validation errors exist; otherwise `false`.
- **Exceptions**: None.

### `EnsureValid`
- **Purpose**: Throws an exception if the HTTP client configuration is invalid.
- **Parameters**: None.
- **Return Value**: `void`.
- **Exceptions**: Throws `InvalidOperationException` if `Validate()` returns non-empty errors.

### `ValidateUrl`
- **Purpose**: Validates the format and accessibility of a URL string.
- **Parameters**: `string url` - The URL to validate.
- **Return Value**: `IReadOnlyList<string>` of errors (e.g., malformed URI, missing scheme).
- **Exceptions**: Throws `ArgumentNullException` if `url` is `null`.

### `ValidateBearerToken`
- **Purpose**: Validates the structure and content of a bearer token.
- **Parameters**: `string token` - The bearer token to validate.
- **Return Value**: `IReadOnlyList<string>` of errors (e.g., empty token, invalid characters).
- **Exceptions**: Throws `ArgumentNullException` if `token` is `null`.

### `ValidateHeader`
- **Purpose**: Validates HTTP headers for required fields and proper formatting.
- **Parameters**: `IEnumerable<KeyValuePair<string, string>> headers` - The headers to validate.
- **Return Value**: `IReadOnlyList<string>` of errors (e.g., missing required headers, invalid header values).
- **Exceptions**: Throws `ArgumentNullException` if `headers` is `null`.

### `ValidatePayload`
- **Purpose**: Validates the structure and content of an HTTP request payload.
- **Parameters**: `object payload` - The payload object to validate.
- **Return Value**: `IReadOnlyList<string>` of errors (e.g., null payload, invalid JSON structure).
- **Exceptions**: Throws `ArgumentNullException` if `payload` is `null`.

### `ValidateResponseType<T>`
- **Purpose**: Validates that a response type `T` is compatible with expected HTTP response formats.
- **Parameters**: None (validates generic type `T` against predefined constraints).
- **Return Value**: `IReadOnlyList<string>` of errors (e.g., unsupported type, missing deserialization support).
- **Exceptions**: None thrown; returns errors based on type constraints.

## Usage

```csharp
// Example 1: Validate HTTP client configuration before making a request
var url = "https://api.example.com/data";
var token = "invalid_token";
var headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };
var payload = new { name = "test" };

var errors = HttpClientWrapperValidation.Validate();
if (!HttpClientWrapperValidation.IsValid)
{
    Console.WriteLine("Validation errors:");
    foreach (var error in errors)
    {
        Console.WriteLine($"- {error}");
    }
}
else
{
    // Proceed with HTTP request
}
```

```csharp
// Example 2: Validate response type compatibility
try
{
    HttpClientWrapperValidation.EnsureValid();
    var responseTypeErrors = HttpClientWrapperValidation.ValidateResponseType<MyResponseType>();
    if (responseTypeErrors.Any())
    {
        throw new NotSupportedException($"Invalid response type: {string.Join(", ", responseTypeErrors)}");
    }
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Configuration invalid: {ex.Message}");
}
```

## Notes

- All methods are static and thread-safe, assuming no shared mutable state is modified during validation.
- `ValidateUrl` requires URLs to include a valid scheme (e.g., `https://`) and may reject malformed or inaccessible endpoints.
- `ValidateBearerToken` enforces non-empty tokens and may validate against specific character sets or length constraints.
- `ValidateHeader` checks for required headers (e.g., authentication headers) and ensures header values conform to HTTP standards.
- `ValidatePayload` may reject payloads that cannot be serialized (e.g., circular references in objects).
- `ValidateResponseType<T>` relies on generic type constraints and may require types to implement interfaces like `ISerializable` or have parameterless constructors.
- `EnsureValid` is intended for fail-fast scenarios where invalid configurations should halt execution immediately.
