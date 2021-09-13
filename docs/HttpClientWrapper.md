# HttpClientWrapper

The `HttpClientWrapper` class provides a sealed, high-level abstraction for performing asynchronous HTTP operations within the `sqlite-multi-tenant` project. It encapsulates standard HTTP verbs (GET, POST, PUT, DELETE) with built-in JSON serialization and deserialization, while offering convenient methods for managing default request headers and bearer token authentication. This wrapper simplifies client-side communication by reducing boilerplate code associated with `HttpClient` usage and ensuring consistent error handling and response processing across the application.

## API

### Constructors

#### `public HttpClientWrapper()`
Initializes a new instance of the `HttpClientWrapper` class. This constructor sets up the underlying HTTP client infrastructure required for subsequent requests.

### HTTP Verbs

#### `public async Task<T?> GetAsync<T>(string requestUri)`
Sends an HTTP GET request to the specified URI and deserializes the JSON response body into an object of type `T`.
*   **Parameters**:
    *   `requestUri`: The relative or absolute URI to send the request to.
*   **Return Value**: Returns a `Task` that resolves to the deserialized object of type `T`, or `null` if the response body is empty or the status code indicates no content.
*   **Exceptions**: Throws an exception if the network request fails, the response status code indicates a server error, or if the JSON content cannot be deserialized into type `T`.

#### `public async Task<T?> PostAsync<T>(string requestUri, object? content)`
Sends an HTTP POST request with the provided content serialized as JSON to the specified URI and deserializes the JSON response body into an object of type `T`.
*   **Parameters**:
    *   `requestUri`: The relative or absolute URI to send the request to.
    *   `content`: The object to be serialized and sent in the request body. Can be `null`.
*   **Return Value**: Returns a `Task` that resolves to the deserialized object of type `T` from the response, or `null` if no content is returned.
*   **Exceptions**: Throws an exception if the network request fails, the server returns an error status code, or serialization/deserialization fails.

#### `public async Task<bool> PutAsync(string requestUri, object? content)`
Sends an HTTP PUT request with the provided content serialized as JSON to the specified URI.
*   **Parameters**:
    *   `requestUri`: The relative or absolute URI to send the request to.
    *   `content`: The object to be serialized and sent in the request body. Can be `null`.
*   **Return Value**: Returns a `Task` that resolves to `true` if the request completes with a success status code (2xx), otherwise `false`.
*   **Exceptions**: Throws an exception primarily on network failures or critical protocol errors; non-2xx status codes generally result in a `false` return rather than an exception.

#### `public async Task<bool> DeleteAsync(string requestUri)`
Sends an HTTP DELETE request to the specified URI.
*   **Parameters**:
    *   `requestUri`: The relative or absolute URI to send the request to.
*   **Return Value**: Returns a `Task` that resolves to `true` if the request completes with a success status code (2xx), otherwise `false`.
*   **Exceptions**: Throws an exception primarily on network failures or critical protocol errors; non-2xx status codes generally result in a `false` return rather than an exception.

### Configuration

#### `public void AddDefaultHeader(string name, string value)`
Adds a header to the internal request configuration that will be included in all subsequent HTTP requests made by this instance.
*   **Parameters**:
    *   `name`: The name of the header.
    *   `value`: The value of the header.
*   **Exceptions**: May throw if the header name is invalid or if the specific header does not support the provided value format.

#### `public void SetBearerToken(string? token)`
Configures the `Authorization` header for subsequent requests using the Bearer scheme. If `token` is `null` or empty, the Authorization header is removed.
*   **Parameters**:
    *   `token`: The access token string. Pass `null` to clear the token.
*   **Remarks**: This method internally calls `AddDefaultHeader` targeting the "Authorization" key with the "Bearer " prefix.

## Usage

### Example 1: Fetching Data with Authentication
The following example demonstrates initializing the wrapper, setting a bearer token for authentication, and retrieving a typed list of resources.

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class UserService
{
    private readonly HttpClientWrapper _client;

    public UserService()
    {
        _client = new HttpClientWrapper();
        _client.SetBearerToken("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...");
    }

    public async Task<List<User>> GetActiveUsersAsync()
    {
        // Performs GET /api/users?status=active and deserializes JSON to List<User>
        var users = await _client.GetAsync<List<User>>("/api/users?status=active");
        
        return users ?? new List<User>();
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

### Example 2: Creating and Updating Resources
This example shows how to POST new data and conditionally update it using the boolean return values of mutation methods.

```csharp
using System.Threading.Tasks;

public class DataSyncService
{
    private readonly HttpClientWrapper _client;

    public DataSyncService()
    {
        _client = new HttpClientWrapper();
        _client.AddDefaultHeader("X-Tenant-ID", "tenant-123");
    }

    public async Task<bool> SyncRecordAsync(RecordData data)
    {
        // Attempt to create the record
        var createdRecord = await _client.PostAsync<RecordData>("/api/records", data);

        if (createdRecord != null && createdRecord.Id > 0)
        {
            // Modify local data
            createdRecord.LastSynced = System.DateTime.UtcNow;

            // Attempt to update the record on the server
            bool updateSuccess = await _client.PutAsync($"/api/records/{createdRecord.Id}", createdRecord);
            
            return updateSuccess;
        }

        return false;
    }
}

public class RecordData
{
    public int Id { get; set; }
    public string Payload { get; set; }
    public System.DateTime LastSynced { get; set; }
}
```

## Notes

*   **Thread Safety**: As the class is `sealed` and manages internal state for headers (via `AddDefaultHeader` and `SetBearerToken`), instances should generally be treated as not thread-safe for configuration changes. While concurrent read operations (GET/POST/PUT/DELETE) may function correctly depending on the underlying `HttpClient` implementation, modifying headers while requests are in flight can lead to race conditions where requests inadvertently inherit headers intended for other contexts. It is recommended to configure the instance fully before initiating concurrent requests or to use separate instances per logical context.
*   **Null Handling**: The `GetAsync<T>` and `PostAsync<T>` methods explicitly return `null` for empty responses or 204 No Content status codes. Callers must handle potential `null` returns to avoid `NullReferenceException`.
*   **Error Propagation**: Mutation methods (`PutAsync`, `DeleteAsync`) return `false` for non-success HTTP status codes (e.g., 400 Bad Request, 404 Not Found) rather than throwing exceptions. However, network-level failures (DNS issues, timeouts, connection refused) will still throw exceptions. Callers should wrap calls in try-catch blocks if network resilience is required.
*   **Header Overwriting**: Calling `SetBearerToken` will overwrite any existing "Authorization" header previously set via `AddDefaultHeader`. Conversely, calling `AddDefaultHeader("Authorization", ...)` after `SetBearerToken` will overwrite the bearer token configuration.
