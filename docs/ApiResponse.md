# ApiResponse
`ApiResponse<T>` is a generic container used throughout the sqlite-multi-tenant project to represent the result of an operation in a uniform, HTTP‑like fashion. It bundles a status code, success flag, descriptive message, optional payload, validation errors, and a timestamp, while providing static factory methods for common response scenarios.

## API
### ApiResponse<T> (sealed class)

| Member | Type | Purpose | Parameters | Return Value | Throws |
|--------|------|---------|------------|--------------|--------|
| `StatusCode` | `int` | HTTP‑style status code indicating the outcome (e.g., 200, 404). | – | The current status code value. | None |
| `IsSuccess` | `bool` | `true` when `StatusCode` falls in the 2xx range; otherwise `false`. | – | Boolean success indicator. | None |
| `Message` | `string` | Human‑readable description of the result (e.g., “OK”, “Not found”). | – | The message string; may be empty or `null`. | None |
| `Data` | `T?` | The payload of type `T` returned by the operation. Can be `null` even when `IsSuccess` is `true`. | – | The payload instance or `null`. | None |
| `Errors` | `Dictionary<string, string>?` | Validation or business‑rule errors keyed by property name or error code. `null` when no errors are present. | – | Dictionary of error messages or `null`. | None |
| `Timestamp` | `DateTime` | UTC date and time when the response instance was created. | – | The creation timestamp. | None |
| `Success` | `static ApiResponse<T>` | Pre‑configured response representing a successful operation (status 200). | – | An `ApiResponse<T>` with `StatusCode = 200`, `IsSuccess = true`, default `Message`, and empty `Errors`. | None |
| `Created` | `static ApiResponse<T>` | Pre‑configured response for a resource creation (status 201). | – | An `ApiResponse<T>` with `StatusCode = 201`, `IsSuccess = true`. | None |
| `BadRequest` | `static ApiResponse<T>` | Pre‑configured response for client errors (status 400). | – | An `ApiResponse<T>` with `StatusCode = 400`, `IsSuccess = false`. | None |
| `NotFound` | `static ApiResponse<T>` | Pre‑configured response for missing resources (status 404). | – | An `ApiResponse<T>` with `StatusCode = 404`, `IsSuccess = false`. | None |
| `Conflict` | `static ApiResponse<T>` | Pre‑configured response for conflicting state (status 409). | – | An `ApiResponse<T>` with `StatusCode = 409`, `IsSuccess = false`. | None |
| `InternalServerError` | `static ApiResponse<T>` | Pre‑configured response for unexpected server errors (status 500). | – | An `ApiResponse<T>` with `StatusCode = 500`, `IsSuccess = false`. | None |
| `Unauthorized` | `static ApiResponse<T>` | Pre‑configured response for missing or invalid authentication (status 401). | – | An `ApiResponse<T>` with `StatusCode = 401`, `IsSuccess = false`. | None |
| `Forbidden` | `static ApiResponse<T>` | Pre‑configured response for authenticated but unauthorized access (status 403). | – | An `ApiResponse<T>` with `StatusCode = 403`, `IsSuccess = false`. | None |
| `Error` | `static ApiResponse<T>` | Generic error response (defaults to status 500). Can be customized after retrieval. | – | An `ApiResponse<T>` with `IsSuccess = false`. | None |

### TenantResponse (sealed nested class)

| Member | Type | Purpose |
|--------|------|---------|
| `TenantId` | `string` | Unique identifier for the tenant. |
| `Name` | `string` | Human‑readable name of the tenant. |
| `Status` | `string` | Current state of the tenant (e.g., `"Active"`, `"Suspended"`). |

## Usage
```csharp
// Example 1: Returning a successful response with data
public ApiResponse<UserDto> GetUser(int userId)
{
    var user = _userRepository.FindById(userId);
    if (user == null)
        return ApiResponse<UserDto>.NotFound; // static factory

    var dto = new UserDto { Id = user.Id, Email = user.Email };
    return new ApiResponse<UserDto>
    {
        StatusCode = 200,
        IsSuccess = true,
        Message = "User retrieved",
        Data = dto,
        Timestamp = DateTime.UtcNow
    };
}

// Example 2: Handling a validation error response
public ApiResponse<TenantDto> CreateTenant(TenantDto input)
{
    if (string.IsNullOrWhiteSpace(input.Name))
    {
        var resp = ApiResponse<TenantDto>.BadRequest;
        resp.Message = "Tenant name is required";
        resp.Errors = new Dictionary<string, string>
        {
            { "Name", "Name must not be empty" }
        };
        return resp;
    }

    // ... creation logic ...
    return ApiResponse<TenantDto>.Created;
}
```

## Notes
- The `Data` property may be `null` even when `IsSuccess` is `true`; callers should not assume a non‑null payload based solely on the success flag.
- `Errors` is lazily initialized; it remains `null` when no validation issues exist. Setting it to an empty dictionary is unnecessary but permitted.
- `Timestamp` is set by the constructor or factory methods to `DateTime.UtcNow`; modifying it after creation does not affect the semantics of the response.
- All static factory members return **immutable** instances (their properties are read‑only after initialization). Consequently, they are safe to use concurrently from multiple threads without additional synchronization.
- Instances created via object initializers or constructors are mutable; if such instances are shared across threads, external synchronization is required to avoid race conditions.
- `TenantResponse` is a simple data‑transfer object with no behavior; it is intended for serialization and does not enforce any invariants beyond those applied by the caller.
