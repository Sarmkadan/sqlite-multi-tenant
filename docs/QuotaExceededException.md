# QuotaExceededException

The `QuotaExceededException` is a sealed exception class that indicates an operation would cause a tenant to exceed its storage quota. It inherits from `System.Exception` and carries the tenant identifier, quota, current storage usage, and proposed size increase so callers can report or handle the rejected operation with context.

## API

### `public string? TenantId { get; }`

Gets the identifier of the tenant whose quota would be exceeded. The property is nullable at the type level, although every public constructor requires a non-nullable `string` parameter.

### `public long QuotaBytes { get; }`

Gets the tenant's storage quota in bytes.

### `public long CurrentSizeBytes { get; }`

Gets the tenant database's current size in bytes.

### `public long DeltaBytes { get; }`

Gets the number of bytes the proposed operation would add.

### `public QuotaExceededException(string tenantId, long quotaBytes, long currentSizeBytes, long deltaBytes)`

Initializes a new instance with the supplied quota details and an automatically formatted message. The message includes the tenant ID, quota, current size, delta, proposed size, and proposed quota percentage.

**Parameters**  
- `tenantId` – The identifier of the tenant whose quota would be exceeded.  
- `quotaBytes` – The tenant's storage quota in bytes.  
- `currentSizeBytes` – The tenant database's current size in bytes.  
- `deltaBytes` – The number of bytes the operation would add.

### `public QuotaExceededException(string message, string tenantId, long quotaBytes, long currentSizeBytes, long deltaBytes)`

Initializes a new instance with a custom exception message and the supplied quota details.

**Parameters**  
- `message` – The custom exception message.  
- `tenantId` – The identifier of the tenant whose quota would be exceeded.  
- `quotaBytes` – The tenant's storage quota in bytes.  
- `currentSizeBytes` – The tenant database's current size in bytes.  
- `deltaBytes` – The number of bytes the operation would add.

### `public QuotaExceededException(string message, string tenantId, long quotaBytes, long currentSizeBytes, long deltaBytes, Exception? innerException)`

Initializes a new instance with a custom exception message, the supplied quota details, and the exception that caused the current failure.

**Parameters**  
- `message` – The custom exception message.  
- `tenantId` – The identifier of the tenant whose quota would be exceeded.  
- `quotaBytes` – The tenant's storage quota in bytes.  
- `currentSizeBytes` – The tenant database's current size in bytes.  
- `deltaBytes` – The number of bytes the operation would add.  
- `innerException` – The exception that caused the current exception, or `null` when no underlying exception is available.

## Usage

`TenantQuotaEnforcer` returns a `QuotaCheckResult`; it does not throw `QuotaExceededException` itself. A caller can translate an over-quota result into the exception when rejecting a write:

```csharp
const string tenantId = "tenant-123";
const long pendingWriteBytes = 4096;

try
{
    QuotaCheckResult result = await quotaEnforcer.EnforceAsync(tenantId);

    if (result.IsOverQuota && result.QuotaBytes is long quotaBytes)
    {
        throw new QuotaExceededException(
            tenantId,
            quotaBytes,
            result.CurrentSizeBytes,
            pendingWriteBytes);
    }
}
catch (QuotaExceededException ex)
{
    Console.WriteLine(
        $"Tenant {ex.TenantId} cannot add {ex.DeltaBytes} bytes " +
        $"({ex.CurrentSizeBytes} of {ex.QuotaBytes} bytes already used).");
}
```

## Notes

- The automatically generated message uses binary size units (`KB`, `MB`, `GB`, and `TB`, each based on 1,024 bytes) and reports the proposed usage percentage to two decimal places.
- All quota values are captured when the exception is constructed and exposed as read-only properties.
- The class is sealed and cannot be inherited.
