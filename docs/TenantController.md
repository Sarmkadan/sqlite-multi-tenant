# TenantController

The `TenantController` provides asynchronous operations for managing tenants in a multi‑tenant SQLite‑backed service. It encapsulates calls to the underlying API, returning typed responses wrapped in `ApiResponse<T>` to convey success, error details, and status codes.

## API

### `public TenantController()`
Initializes a new instance of the controller. No parameters are required; dependencies such as HTTP clients or configuration are supplied via dependency injection or internal defaults.

### `public async Task<ApiResponse<TenantResponse>> CreateTenantAsync(TenantCreateRequest request)`
Creates a new tenant using the supplied `request` payload.  
- **Parameters**  
  - `request`: Contains the data required to provision a tenant (e.g., name, connection string, settings).  
- **Return value**  
  - `ApiResponse<TenantResponse>`: On success, the `Data` property holds the created tenant’s details; `IsSuccess` is true and `StatusCode` reflects the HTTP result.  
- **Exceptions**  
  - Throws `ArgumentNullException` if `request` is null.  
  - May throw `HttpRequestException` for network failures; the resulting `ApiResponse` will contain error information instead of throwing.

### `public async Task<ApiResponse<TenantResponse>> GetTenantAsync(string tenantId)`
Retrieves the details of a single tenant identified by `tenantId`.  
- **Parameters**  
  - `tenantId`: Unique identifier of the tenant (typically a GUID or string key).  
- **Return value**  
  - `ApiResponse<TenantResponse>`: Contains the tenant data when found; otherwise `IsSuccess` is false with an appropriate error code.  
- **Exceptions**  
  - Throws `ArgumentException` if `tenantId` is null, empty, or whitespace.  
  - Network or server errors are reported via the `ApiResponse` return value.

### `public async Task<ApiResponse<IEnumerable<TenantResponse>>> ListAllTenantsAsync(TenantListRequest? filter = null)`
Returns a collection of all tenants, optionally filtered or paginated via `filter`.  
- **Parameters**  
  - `filter`: Optional criteria such as page size, continuation token, or status filters; pass `null` for an unfiltered list.  
- **Return value**  
  - `ApiResponse<IEnumerable<TenantResponse>>`: The `Data` property enumerates tenant summaries; `IsSuccess` indicates whether the call succeeded.  
- **Exceptions**  
  - Throws `ArgumentOutOfRangeException` if any filter values are invalid (e.g., negative page size).  
  - Service‑side errors are encapsulated in the returned `ApiResponse`.

### `public async Task<ApiResponse<TenantResponse>> UpdateTenantAsync(string tenantId, TenantUpdateRequest request)`
Updates an existing tenant’s properties.  
- **Parameters**  
  - `tenantId`: Identifier of the tenant to modify.  
  - `request`: Contains the fields to be changed; only supplied properties are applied.  
- **Return value**  
  - `ApiResponse<TenantResponse>`: Reflects the tenant’s state after the update when successful.  
- **Exceptions**  
  - Throws `ArgumentNullException` if either `tenantId` or `request` is null.  
  - Throws `ArgumentException` for an empty `tenantId`.  
  - Conflict or validation errors are reported via the `ApiResponse`.

### `public async Task<ApiResponse<object>> SuspendTenantAsync(string tenantId)`
Suspends a tenant, preventing further operations until it is reactivated.  
- **Parameters**  
  - `tenantId`: Identifier of the tenant to suspend.  
- **Return value**  
  - `ApiResponse<object>`: The `Data` property is typically null; success is indicated by `IsSuccess`.  
- **Exceptions**  
  - Throws `ArgumentException` if `tenantId` is null or empty.  
  - Errors such as tenant not found or already suspended are conveyed through the response object.

## Usage

```csharp
// Example 1: Creating a new tenant
var controller = new TenantController();
var createReq = new TenantCreateRequest
{
    Name = "Acme Corp",
    AdminEmail = "admin@acme.com",
    // … other required fields
};
ApiResponse<TenantResponse> createResp = await controller.CreateTenantAsync(createReq);
if (createResp.IsSuccess)
{
    var tenant = createResp.Data;
    Console.WriteLine($"Tenant created with ID {tenant.Id}");
}
else
{
    Console.Error.WriteLine($"Creation failed: {createResp.ErrorMessage}");
}
```

```csharp
// Example 2: Listing all active tenants
var controller = new TenantController();
var listReq = new TenantListRequest { PageSize = 50, Status = TenantStatus.Active };
ApiResponse<IEnumerable<TenantResponse>> listResp = await controller.ListAllTenantsAsync(listReq);
if (listResp.IsSuccess)
{
    foreach (var t in listResp.Data)
    {
        Console.WriteLine($"{t.Id}: {t.Name}");
    }
}
else
{
    Console.Error.WriteLine($"List request failed: {listResp.ErrorMessage}");
}
```

## Notes

- All methods are stateless; the controller itself does not retain mutable data, making it safe for concurrent invocation from multiple threads.  
- Passing `null` for required arguments results in an `ArgumentNullException` before any network call is made.  
- Invalid identifiers (empty strings, whitespace-only strings) trigger `ArgumentException`.  
- The controller does not automatically retry transient failures; callers should implement retry policies if needed.  
- Responses from the API are wrapped in `ApiResponse<T>`; inspect `IsSuccess` and `StatusCode` to differentiate between business‑logic errors (e.g., validation conflicts) and transport‑level issues.  
- Because the class is `sealed`, it cannot be subclassed; extension of behavior should be achieved through composition or decorator patterns rather than inheritance.
