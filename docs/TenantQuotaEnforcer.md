# TenantQuotaEnforcer

`TenantQuotaEnforcer` enforces per-tenant storage quotas stored in tenant metadata under the key "quota.maxBytes". It provides methods to set, check, and enforce quotas, as well as batch operations for multiple tenants.

## API

### public TenantQuotaEnforcer(ITenantService tenantService)

Creates a new instance of the quota enforcer.

- **Parameters:**
  - `tenantService`: The tenant service used to retrieve tenant information and metadata. Must not be null.
- **Exceptions:** `ArgumentNullException` if `tenantService` is null.

### public double WarningThreshold { get; set; }

Gets or sets the fraction (0-1) of quota at which `IsNearQuota` becomes true. Default is 0.9 (90%).

- **Value:** A double between 0 (exclusive) and 1 (inclusive).
- **Exceptions:** `ArgumentOutOfRangeException` if the value is less than or equal to 0 or greater than 1.

### public async Task SetQuotaAsync(string tenantId, long maxBytes, CancellationToken cancellationToken = default)

Sets (or updates) the quota for a tenant via `SetTenantMetadataAsync`. The quota is stored as an invariant-culture long string under the metadata key "quota.maxBytes".

- **Parameters:**
  - `tenantId`: The unique identifier of the tenant. Must not be null, empty, or whitespace.
  - `maxBytes`: The quota limit in bytes. Must be positive.
  - `cancellationToken`: Optional token to cancel the operation.
- **Exceptions:**
  - `ArgumentException` if `tenantId` is null, empty, or whitespace.
  - `ArgumentException` if `maxBytes` is not positive.
  - `OperationCanceledException` if the cancellation token is triggered.
  - May throw exceptions from the underlying `ITenantService` operations.

### public async Task<long?> GetQuotaAsync(string tenantId, CancellationToken cancellationToken = default)

Reads the tenant's quota from metadata. Returns null when the quota metadata is absent or unparsable.

- **Parameters:**
  - `tenantId`: The unique identifier of the tenant. Must not be null, empty, or whitespace.
  - `cancellationToken`: Optional token to cancel the operation.
- **Returns:** The quota in bytes as a nullable long, or null if no quota is configured.
- **Exceptions:**
  - `ArgumentException` if `tenantId` is null, empty, or whitespace.
  - `OperationCanceledException` if the cancellation token is triggered.
  - May throw exceptions from the underlying `ITenantService` operations.

### public async Task<QuotaCheckResult> CheckQuotaAsync(string tenantId, CancellationToken cancellationToken = default)

Compares the tenant's current database size against the quota and returns a `QuotaCheckResult`. Throws `TenantNotFoundException` when the tenant does not exist.

- **Parameters:**
  - `tenantId`: The unique identifier of the tenant. Must not be null, empty, or whitespace.
  - `cancellationToken`: Optional token to cancel the operation.
- **Returns:** A `QuotaCheckResult` containing the quota check details.
- **Exceptions:**
  - `ArgumentException` if `tenantId` is null, empty, or whitespace.
  - `TenantNotFoundException` if the tenant does not exist.
  - `OperationCanceledException` if the cancellation token is triggered.
  - May throw exceptions from the underlying `ITenantService` operations.
  - `InvalidOperationException` if the quota exists but is not positive.

### public async Task<QuotaCheckResult> EnforceAsync(string tenantId, bool autoSuspend = true, CancellationToken cancellationToken = default)

Checks the quota and, when exceeded and `autoSuspend` is true, calls `SuspendTenantAsync`. Returns the check result.

- **Parameters:**
  - `tenantId`: The unique identifier of the tenant. Must not be null, empty, or whitespace.
  - `autoSuspend`: If true and the tenant is over quota, the tenant will be suspended. Default is true.
  - `cancellationToken`: Optional token to cancel the operation.
- **Returns:** A `QuotaCheckResult` representing the quota check after any enforcement action.
- **Exceptions:**
  - `ArgumentException` if `tenantId` is null, empty, or whitespace.
  - `TenantNotFoundException` if the tenant does not exist.
  - `OperationCanceledException` if the cancellation token is triggered.
  - May throw exceptions from the underlying `ITenantService` operations.
  - `InvalidOperationException` if the quota exists but is not positive.

### public async Task<List<QuotaCheckResult>> CheckAllTenantsAsync(int maxDegreeOfParallelism = 4, CancellationToken cancellationToken = default)

Checks quota usage for all tenants with bounded parallelism, ordered by usage percentage descending.

- **Parameters:**
  - `maxDegreeOfParallelism`: The maximum number of concurrent operations. Must be at least 1. Default is 4.
  - `cancellationToken`: Optional token to cancel the operation.
- **Returns:** A list of `QuotaCheckResult` objects for all tenants, sorted by usage percentage descending.
- **Exceptions:**
  - `ArgumentOutOfRangeException` if `maxDegreeOfParallelism` is less than 1.
  - `OperationCanceledException` if the cancellation token is triggered.
  - May throw exceptions from the underlying `ITenantService` operations.

### public async Task<List<QuotaCheckResult>> GetTenantsOverQuotaAsync(CancellationToken cancellationToken = default)

Returns all tenants whose current usage meets or exceeds their configured quota.

- **Parameters:**
  - `cancellationToken`: Optional token to cancel the operation.
- **Returns:** A list of `QuotaCheckResult` objects for tenants over quota.
- **Exceptions:**
  - `OperationCanceledException` if the cancellation token is triggered.
  - May throw exceptions from the underlying `ITenantService` operations.

### public async Task<List<QuotaCheckResult>> ScanAllAsync(CancellationToken cancellationToken = default)

Scans all active tenants and returns results for tenants that are near or over quota, worst first.

- **Parameters:**
  - `cancellationToken`: Optional token to cancel the operation.
- **Returns:** A list of `QuotaCheckResult` objects for tenants near or over quota, sorted by usage percentage descending, with over-quota tenants prioritized.
- **Exceptions:**
  - `OperationCanceledException` if the cancellation token is triggered.
  - May throw exceptions from the underlying `ITenantService` operations.

## Usage of QuotaCheckResult and QuotaExceededException

The `TenantQuotaEnforcer` works closely with `QuotaCheckResult` and `QuotaExceededException`:

- **QuotaCheckResult**: Returned by all quota checking methods (`CheckQuotaAsync`, `EnforceAsync`, `CheckAllTenantsAsync`, etc.). It contains:
  - `TenantId`: The tenant identifier
  - `CurrentSizeBytes`: Current storage usage in bytes
  - `QuotaBytes`: Configured quota in bytes (null means unlimited)
  - `UsagePercent`: Usage as a percentage (0-100+)
  - `IsOverQuota`: True if usage exceeds quota
  - `IsNearQuota`: True if usage is at or above warning threshold but not over quota

- **QuotaExceededException**: While not directly thrown by `TenantQuotaEnforcer` methods, this exception is used elsewhere in the system when an operation would cause a tenant to exceed their quota. The enforcer helps prevent this by checking quotas before operations proceed.

## Usage Example

```csharp
using SqliteMultiTenant.Tenants;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Exceptions;

// Assuming you have an ITenantService implementation
ITenantService tenantService = new TenantService(connectionString);

// Create the quota enforcer
TenantQuotaEnforcer enforcer = new TenantQuotaEnforcer(tenantService);

// Optionally adjust the warning threshold (0.0-1.0)
enforcer.WarningThreshold = 0.8; // 80% instead of default 90%

// Set a quota for a tenant (100 MB)
await enforcer.SetQuotaAsync("tenant-123", 100L * 1024 * 1024);

// Check the quota status
QuotaCheckResult result = await enforcer.CheckQuotaAsync("tenant-123");

Console.WriteLine($"Tenant {result.TenantId}: {result.UsagePercent:F1}% used ({result.CurrentSizeBytes} bytes)");

if (result.IsNearQuota && !result.IsOverQuota)
{
    Console.WriteLine("Approaching quota — sending warning notification.");
    // Send warning email/notification to tenant admin
}

if (result.IsOverQuota)
{
    Console.WriteLine("Over quota — enforcing limit.");
    // Enforce the quota (will suspend tenant if autoSuspend is true)
    result = await enforcer.EnforceAsync("tenant-123");
    Console.WriteLine($"Enforcement complete. Over quota: {result.IsOverQuota}");
    
    // In a real system, you might block write operations or notify the tenant
}

// Check all tenants and find those over quota
List<QuotaCheckResult> overQuotaTenants = await enforcer.GetTenantsOverQuotaAsync();
foreach (QuotaCheckResult tenantResult in overQuotaTenants)
{
    Console.WriteLine($"Tenant {tenantResult.TenantId} is over quota: {tenantResult.UsagePercent:F1}% used");
    // Take action: notify tenant, block writes, etc.
}

// Scan for tenants near or over quota (warning and over-quota tenants)
List<QuotaCheckResult> problematicTenants = await enforcer.ScanAllAsync();
foreach (QuotaCheckResult tenantResult in problematicTenants)
{
    string status = tenantResult.IsOverQuota ? "OVER QUOTA" : "NEAR QUOTA";
    Console.WriteLine($"Tenant {tenantResult.TenantId}: {status} ({tenantResult.UsagePercent:F1}% used)");
}
```