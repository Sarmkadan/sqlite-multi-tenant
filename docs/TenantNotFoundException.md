# TenantNotFoundException

The `TenantNotFoundException` is a sealed exception class that indicates a requested tenant could not be located in the multi-tenant database. It inherits from `System.Exception` and exposes the `TenantId` property to carry the identifier of the missing tenant, enabling callers to log or handle the failure with context.

## API

### `public string? TenantId { get; }`

Gets the tenant identifier that was not found. The value may be `null` if the exception was created without specifying a tenant ID.

### `public TenantNotFoundException()`

Initializes a new instance of the `TenantNotFoundException` class with no message, no inner exception, and a `null` tenant ID.

### `public TenantNotFoundException(string? tenantId)`

Initializes a new instance of the `TenantNotFoundException` class with a default message and the specified tenant identifier. The `TenantId` property is set to the provided value.

**Parameters**  
- `tenantId` – The identifier of the tenant that was not found. Can be `null`.

### `public TenantNotFoundException(string? tenantId, Exception innerException)`

Initializes a new instance of the `TenantNotFoundException` class with a default message, the specified tenant identifier, and a reference to the inner exception that is the cause of this exception. The `TenantId` property is set to the provided value.

**Parameters**  
- `tenantId` – The identifier of the tenant that was not found. Can be `null`.  
- `innerException` – The exception that is the cause of the current exception, or `null` if no inner exception is specified.

## Usage

The following examples demonstrate typical usage of `TenantNotFoundException`.

```csharp
// Example 1: Catching the exception and logging the missing tenant ID
try
{
    var tenant = await tenantRepository.GetTenantAsync("tenant-xyz");
}
catch (TenantNotFoundException ex)
{
    Console.WriteLine($"Tenant not found: {ex.TenantId ?? "unknown"}");
    // Log the error and possibly return a 404 response
}
```

```csharp
// Example 2: Throwing the exception with a specific tenant ID
public async Task<Tenant> GetTenantOrThrowAsync(string tenantId)
{
    var tenant = await database.Tenants.FindAsync(tenantId);
    if (tenant == null)
    {
        throw new TenantNotFoundException(tenantId);
    }
    return tenant;
}
```

## Notes

- The `TenantId` property may be `null` when the exception is created using the parameterless constructor or when the tenant identifier is not available at the throw site. Code that catches this exception should handle a `null` value gracefully.
- Instances of `TenantNotFoundException` are not thread-safe for mutation after construction. However, reading the `TenantId` property and other inherited members (e.g., `Message`, `InnerException`) is safe once the object is fully constructed.
- The class is sealed and cannot be inherited. If custom behavior is needed, consider wrapping this exception in another exception or using a separate exception type.
