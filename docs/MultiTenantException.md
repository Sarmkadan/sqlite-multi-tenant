# MultiTenantException

Base exception type for all multi‑tenant related errors. Provides a <see cref="TenantId"/> property to identify the tenant that caused the error.

## API

### Properties

#### `TenantId`
`public string? TenantId`

Gets the identifier of the tenant related to the exception, if any. Returns `null` when the failure occurred before a tenant identifier could be assigned or when the context does not involve a specific tenant.

### Constructors

#### `MultiTenantException(string message)`
`protected MultiTenantException(string message)`

Initializes a new instance with a descriptive error message.

| Parameter | Type     | Purpose                                          |
|-----------|----------|--------------------------------------------------|
| `message` | `string` | The message that describes the error condition.  |

#### `MultiTenantException(string message, Exception innerException)`
`protected MultiTenantException(string message, Exception innerException)`

Initializes a new instance with a descriptive error message and a reference to the inner exception that caused this error.

| Parameter        | Type        | Purpose                                              |
|------------------|-------------|------------------------------------------------------|
| `message`        | `string`    | The message that describes the error condition.      |
| `innerException` | `Exception` | The exception that is the cause of the current one.  |

#### `MultiTenantException(string message, string tenantId)`
`protected MultiTenantException(string message, string tenantId)`

Initializes a new instance with a descriptive error message and tenant identifier.

| Parameter    | Type     | Purpose                                          |
|--------------|----------|--------------------------------------------------|
| `message`    | `string` | The message that describes the error condition.  |
| `tenantId`   | `string` | The identifier of the tenant related to the error. |

#### `MultiTenantException(string message, Exception innerException, string tenantId)`
`protected MultiTenantException(string message, Exception innerException, string tenantId)`

Initializes a new instance with a descriptive error message, a reference to the inner exception that caused this error, and tenant identifier.

| Parameter        | Type        | Purpose                                              |
|------------------|-------------|------------------------------------------------------|
| `message`        | `string`    | The message that describes the error condition.      |
| `innerException` | `Exception` | The exception that is the cause of the current one.  |
| `tenantId`       | `string`    | The identifier of the tenant related to the error.   |

## Derived Exceptions

The following exception types inherit from `MultiTenantException`:

- `DatabaseAccessException` - Thrown when database access operations fail
- `MigrationException` - Thrown when migration operations fail  
- `TenantNotFoundException` - Thrown when a tenant is not found in the system
- `BackupException` - Thrown when backup operations fail

## Usage

### Example 1: Throwing with Tenant Context

```csharp
public void ProcessTenantData(string tenantId)
{
    try
    {
        // Attempt tenant-specific operation...
        var data = GetTenantData(tenantId);
        // Process data...
    }
    catch (Exception ex)
    {
        // Wrap the exception with tenant context
        throw new MultiTenantException(
            $"Failed to process data for tenant '{tenantId}'", 
            ex, 
            tenantId);
    }
}
```

### Example 2: Catching and Inspecting Tenant Errors

```csharp
try
{
    await tenantService.UpdateTenantAsync(tenantId, updates);
}
catch (MultiTenantException ex) when (ex.TenantId == tenantId)
{
    Console.WriteLine($"Operation failed for tenant '{ex.TenantId}': {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Caused by: {ex.InnerException.Message}");
    }
    // Decide whether to retry or escalate
}
```

## Notes

- The `TenantId` property is a nullable string. Always check for `null` before using it in log messages or conditional logic, as it may not be populated for failures that occur before the tenant identifier is resolved.
- This type is abstract; it cannot be instantiated directly. Use one of the derived exception types or inherit from it to create custom multi-tenant exceptions.
- Thread safety: Instance members are not synchronized. If multiple threads mutate `TenantId` on the same instance after construction, external synchronization is required.