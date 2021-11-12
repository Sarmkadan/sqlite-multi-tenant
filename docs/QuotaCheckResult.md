# QuotaCheckResult

`QuotaCheckResult` represents the outcome of a storage quota evaluation for a specific tenant within the `sqlite-multi-tenant` system. It encapsulates the tenant's current disk usage, the configured quota limit, derived usage metrics, and boolean flags indicating whether the tenant has exceeded or is approaching the quota threshold. Instances are typically produced by `TenantQuotaEnforcer` methods and can be used to drive enforcement actions or reporting.

## API

### public required string TenantId
The unique identifier of the tenant to which this quota check applies. This property must be supplied when constructing the result.

### public required long CurrentSizeBytes
The total size, in bytes, consumed by the tenant at the moment the check was performed. This value is required and reflects actual measured storage.

### public long? QuotaBytes
The absolute quota limit assigned to the tenant, in bytes. A `null` value indicates that no quota has been configured, meaning the tenant has unlimited storage from an enforcement perspective.

### public double UsagePercent
The ratio of `CurrentSizeBytes` to `QuotaBytes`, expressed as a percentage (0–100+). When `QuotaBytes` is `null` or zero, this property returns `0`.

### public bool IsOverQuota
`true` if `QuotaBytes` has a non-null, positive value and `CurrentSizeBytes` exceeds it; otherwise `false`.

### public bool IsNearQuota
`true` if `QuotaBytes` has a non-null, positive value and `UsagePercent` is greater than or equal to `WarningThreshold` but `IsOverQuota` is `false`; otherwise `false`.

### public double WarningThreshold
The usage percentage at which `IsNearQuota` becomes `true`. This value is sourced from the associated `TenantQuotaEnforcer` configuration and controls the early-warning boundary.

### public TenantQuotaEnforcer
A reference to the `TenantQuotaEnforcer` instance that produced this result. Provides access to the enforcer’s configuration and allows chaining further quota operations for the same tenant.

### public async Task SetQuotaAsync
```csharp
public async Task SetQuotaAsync(long? quotaBytes)
```
Updates the quota limit for the tenant identified by `TenantId` to the specified value. A `null` argument removes any quota restriction. This method delegates to the underlying `TenantQuotaEnforcer` and persists the change.

- **Parameters:**
  - `quotaBytes`: The new quota in bytes, or `null` to clear the quota.
- **Returns:** A `Task` representing the asynchronous operation.
- **Exceptions:** May throw if the underlying storage operation fails (e.g., database connection lost).

### public async Task<long?> GetQuotaAsync
```csharp
public async Task<long?> GetQuotaAsync()
```
Retrieves the currently persisted quota limit for the tenant represented by this result. Returns `null` if no quota is set.

- **Returns:** The quota in bytes, or `null`.
- **Exceptions:** May throw if the quota store is unavailable.

### public async Task<QuotaCheckResult> CheckQuotaAsync
```csharp
public async Task<QuotaCheckResult> CheckQuotaAsync()
```
Re-evaluates the quota status for the tenant by measuring current disk usage and comparing it against the stored quota. Returns a fresh `QuotaCheckResult` reflecting the latest state.

- **Returns:** A new `QuotaCheckResult` instance with updated metrics.
- **Exceptions:** May throw if usage measurement or quota retrieval fails.

### public async Task<QuotaCheckResult> EnforceAsync
```csharp
public async Task<QuotaCheckResult> EnforceAsync()
```
Performs a quota check and, if the tenant is over quota, executes the enforcement action defined in the `TenantQuotaEnforcer` configuration (e.g., blocking writes, raising an alert). Returns the `QuotaCheckResult` after enforcement.

- **Returns:** A `QuotaCheckResult` reflecting the post-enforcement state.
- **Exceptions:** May throw if the check fails or the enforcement action itself encounters an error.

### public async Task<List<QuotaCheckResult>> ScanAllAsync
```csharp
public async Task<List<QuotaCheckResult>> ScanAllAsync()
```
Scans all tenants managed by the associated `TenantQuotaEnforcer`, performing a quota check for each. Returns a list of `QuotaCheckResult` instances, one per tenant.

- **Returns:** A `List<QuotaCheckResult>` containing the quota status for every tenant.
- **Exceptions:** May throw if the bulk scan encounters an unrecoverable error; individual tenant failures may be logged or aggregated depending on enforcer configuration.

## Usage

### Example 1: Checking and Enforcing a Single Tenant
```csharp
TenantQuotaEnforcer enforcer = new TenantQuotaEnforcer(connectionString);
QuotaCheckResult result = await enforcer.CheckQuotaAsync("tenant-42");

Console.WriteLine($"Tenant {result.TenantId}: {result.UsagePercent:F1}% used");

if (result.IsNearQuota && !result.IsOverQuota)
{
    Console.WriteLine("Approaching quota — sending warning notification.");
}

if (result.IsOverQuota)
{
    Console.WriteLine("Over quota — enforcing limit.");
    result = await result.EnforceAsync();
    Console.WriteLine($"Enforcement complete. Over quota: {result.IsOverQuota}");
}
```

### Example 2: Bulk Scanning and Adjusting Quotas
```csharp
TenantQuotaEnforcer enforcer = new TenantQuotaEnforcer(connectionString);
QuotaCheckResult baseline = await enforcer.CheckQuotaAsync("master");
List<QuotaCheckResult> allResults = await baseline.ScanAllAsync();

foreach (QuotaCheckResult tenantResult in allResults)
{
    if (tenantResult.UsagePercent > 90)
    {
        long? currentQuota = await tenantResult.GetQuotaAsync();
        if (currentQuota.HasValue)
        {
            long newQuota = (long)(currentQuota.Value * 1.5);
            await tenantResult.SetQuotaAsync(newQuota);
            Console.WriteLine($"Increased quota for {tenantResult.TenantId} to {newQuota} bytes");
        }
    }
}
```

## Notes

- **Null Quota Handling:** When `QuotaBytes` is `null`, `UsagePercent` returns `0`, and both `IsOverQuota` and `IsNearQuota` are `false`. This correctly represents an unlimited-storage tenant.
- **Zero Quota Edge Case:** If `QuotaBytes` is explicitly set to `0`, `UsagePercent` returns `0` (division by zero is avoided), but `IsOverQuota` will be `true` for any positive `CurrentSizeBytes`.
- **Warning Threshold Source:** `WarningThreshold` is read from the `TenantQuotaEnforcer` configuration at the time the `QuotaCheckResult` is created. Changing the enforcer’s threshold afterward does not retroactively update existing result instances.
- **Thread Safety:** `QuotaCheckResult` is a data-transfer object whose properties are set at construction. The instance methods (`SetQuotaAsync`, `GetQuotaAsync`, `CheckQuotaAsync`, `EnforceAsync`, `ScanAllAsync`) delegate to the shared `TenantQuotaEnforcer`, whose internal thread safety depends on the underlying database connection and synchronization mechanisms. Concurrent calls on the same enforcer from multiple threads should be protected externally if the database provider does not guarantee serialized access.
- **Freshness of Data:** A `QuotaCheckResult` is a point-in-time snapshot. To obtain current usage, call `CheckQuotaAsync` on the instance or the enforcer rather than relying on stale property values.
