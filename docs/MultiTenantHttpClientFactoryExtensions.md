# MultiTenantHttpClientFactoryExtensions

The `MultiTenantHttpClientFactoryExtensions` class provides a set of static extension methods designed to simplify the management, configuration, and lifecycle of `HttpClient` instances within a multi-tenant SQLite application architecture. It abstracts the complexities of tenant-specific client creation, header management, timeout configuration, and instance refreshing.

## API

### GetOrCreateClient
Retrieves an existing `HttpClient` instance associated with the specified tenant identifier or creates a new instance if one does not exist.

*   **Parameters:**
    *   `factory`: The `IHttpClientFactory` instance.
    *   `tenantId`: A unique `string` identifier for the tenant.
*   **Returns:** An `HttpClient` configured for the specified tenant.
*   **Throws:** `ArgumentNullException` if `factory` or `tenantId` is null.

### SetDefaultHeader
Configures a default request header for an `HttpClient` instance. This header will be included in all subsequent requests made by the client.

*   **Parameters:**
    *   `client`: The `HttpClient` instance to configure.
    *   `name`: The name of the header.
    *   `value`: The value of the header.
*   **Returns:** `void`.
*   **Throws:** `InvalidOperationException` if the header cannot be set due to the client's current state.

### SetTimeout
Configures the request timeout for an `HttpClient` instance.

*   **Parameters:**
    *   `client`: The `HttpClient` instance to configure.
    *   `timeout`: A `TimeSpan` representing the maximum duration for requests.
*   **Returns:** `void`.

### RefreshClient
Invalidates the cached `HttpClient` instance for the specified tenant, forcing the factory to create a new instance upon the next request.

*   **Parameters:**
    *   `factory`: The `IHttpClientFactory` instance.
    *   `tenantId`: The unique `string` identifier for the tenant.
*   **Returns:** An `HttpClient` which is a freshly instantiated client for the tenant.

## Usage

### Example 1: Basic Tenant-Specific Client Usage
```csharp
public class TenantService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TenantService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task PerformRequest(string tenantId)
    {
        var client = _httpClientFactory.GetOrCreateClient(tenantId);
        
        client.SetDefaultHeader("X-Tenant-ID", tenantId);
        client.SetTimeout(TimeSpan.FromSeconds(30));

        var response = await client.GetAsync("https://api.example.com/data");
        response.EnsureSuccessStatusCode();
    }
}
```

### Example 2: Forcing a Client Refresh
```csharp
public async Task UpdateTenantConfiguration(string tenantId)
{
    // Force the client to be recreated if configuration changes significantly
    var client = _httpClientFactory.RefreshClient(tenantId);
    
    // Apply new configuration
    client.SetDefaultHeader("X-Tenant-ID", tenantId);
    client.SetTimeout(TimeSpan.FromSeconds(60));
}
```

## Notes

*   **Thread Safety:** While `HttpClient` is designed to be thread-safe for making requests, configuration methods like `SetDefaultHeader` and `SetTimeout` are not thread-safe if called while requests are in progress. Ensure configuration is performed during client initialization or before it is shared across concurrent operations.
*   **Lifecycle Management:** Instances retrieved via `GetOrCreateClient` are managed by the `IHttpClientFactory`. Do not dispose of these `HttpClient` instances manually, as this may disrupt the internal caching and pooling mechanisms of the factory.
*   **Instance Caching:** `GetOrCreateClient` relies on the underlying `IHttpClientFactory` cache. Excessive calls to `RefreshClient` may lead to socket exhaustion if high volumes of clients are created and disposed of rapidly.
