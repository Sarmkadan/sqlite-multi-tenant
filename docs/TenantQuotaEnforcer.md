# TenantQuotaEnforcer

The `TenantQuotaEnforcer` class enforces per-tenant storage quotas stored in tenant metadata under the key `"quota.maxBytes"`. It provides methods to set, retrieve, check, and enforce quotas, as well as scan all tenants for quota violations.

## Constructor

```csharp
public TenantQuotaEnforcer(ITenantService tenantService)
```
Initializes a new instance of the `TenantQuotaEnforcer` class.
- **tenantService**: The service used to interact with tenant data and metadata. Required.
- **Throws**: `ArgumentNullException` if `tenantService` is null.

## Public Methods

### SetQuotaAsync
```csharp
public async Task SetQuotaAsync(string tenantId, long maxBytes, CancellationToken cancellationToken = default)
```
Sets or updates the storage quota for a specific tenant by writing to tenant metadata.
- **tenantId**: The identifier of the tenant.
- **maxBytes**: The maximum allowed storage size in bytes. Must be positive.
- **cancellationToken**: Token to monitor for cancellation requests.
- **Throws**: `ArgumentException` if `tenantId` is null/whitespace or `maxBytes` is not positive.

### GetQuotaAsync
```csharp
public async Task<long?> GetQuotaAsync(string tenantId, CancellationToken cancellationToken = default)
```
Reads the configured quota for a tenant from metadata. Returns `null` if the quota is not set or cannot be parsed.
- **tenantId**: The identifier of the tenant.
- **cancellationToken**: Token to monitor for cancellation requests.
- **Returns**: The quota in bytes, or `null` if not configured.

### CheckQuotaAsync
```csharp
public async Task<QuotaCheckResult> CheckQuotaAsync(string tenantId, CancellationToken cancellationToken = default)
```
Compares the tenant's current database size against their configured quota and returns a detailed usage snapshot.
- **tenantId**: The identifier of the tenant.
- **cancellationToken**: Token to monitor for cancellation requests.
- **Returns**: A `QuotaCheckResult` containing current size, quota, usage percentage, and status flags.
- **Throws**: `TenantNotFoundException` if the tenant does not exist.

### EnforceAsync
```csharp
public async Task<QuotaCheckResult> EnforceAsync(string tenantId, bool autoSuspend = true, CancellationToken cancellationToken = default)
```
Checks the quota and, if exceeded and `autoSuspend` is enabled, automatically suspends the tenant. Returns the check result.
- **tenantId**: The identifier of the tenant.
- **autoSuspend**: If `true`, calls `SuspendTenantAsync` when the quota is exceeded. Defaults to `true`.
- **cancellationToken**: Token to monitor for cancellation requests.
- **Returns**: A `QuotaCheckResult` reflecting the enforcement action.

### CheckAllTenantsAsync
```csharp
public async Task<List<QuotaCheckResult>> CheckAllTenantsAsync(int maxDegreeOfParallelism = 4, CancellationToken cancellationToken = default)
```
Checks quota usage for all tenants with bounded parallelism, returning results ordered by usage percentage descending.
- **maxDegreeOfParallelism**: Maximum number of concurrent checks. Defaults to `4`.
- **cancellationToken**: Token to monitor for cancellation requests.
- **Returns**: A list of `QuotaCheckResult` sorted by worst usage first.

### GetTenantsOverQuotaAsync
```csharp
public async Task<List<QuotaCheckResult>> GetTenantsOverQuotaAsync(CancellationToken cancellationToken = default)
```
Scans all tenants and returns only those whose current usage meets or exceeds their configured quota.
- **cancellationToken**: Token to monitor for cancellation requests.
- **Returns**: A list of `QuotaCheckResult` for tenants over their quota.

### ScanAllAsync
```csharp
public async Task<List<QuotaCheckResult>> ScanAllAsync(CancellationToken cancellationToken = default)
```
Scans all active tenants and returns results for tenants that are near or over quota, sorted by severity.
- **cancellationToken**: Token to monitor for cancellation requests.
- **Returns**: A list of `QuotaCheckResult` for tenants near or over quota, worst offenders first.

## QuotaCheckResult Usage

The `QuotaCheckResult` record (documented in `docs/QuotaCheckResult.md`) is the primary return type for all quota inspection methods. It encapsulates:
- `TenantId`: The tenant identifier.
- `CurrentSizeBytes`: The actual size of the tenant's database.
- `QuotaBytes`: The configured limit, or `null` for unlimited tenants.
- `UsagePercent`: Percentage of quota used (0 when unlimited).
- `IsOverQuota`: `true` if usage is at or above 100%.
- `IsNearQuota`: `true` if usage is at or above the `WarningThreshold` (default 90%) but not over quota.

Consumers use this result to make decisions about tenant access, billing, or notifications without needing to re-calculate percentages.

## QuotaExceededException Usage

`QuotaExceededException` (defined in `src/Exceptions/QuotaExceededException.cs`) is a custom exception designed to signal when a tenant has breached their storage limit. While `TenantQuotaEnforcer` currently handles enforcement by returning `IsOverQuota` in the `QuotaCheckResult` and optionally suspending the tenant via `EnforceAsync`, consumers or downstream services can throw `QuotaExceededException` when rejecting operations (e.g., file uploads, write requests) for tenants identified as over quota. The exception provides formatted messages detailing the tenant ID, quota limit, and current usage to aid in debugging and user feedback.

## Usage Example

```csharp
using SqliteMultiTenant.Tenants;
using SqliteMultiTenant.Models;
using Microsoft.Extensions.Logging;

// Setup (typically done via dependency injection)
var tenantService = /* obtain ITenantService instance */;
var enforcer = new TenantQuotaEnforcer(tenantService);

// Set a quota for a tenant (e.g., 1 GB)
await enforcer.SetQuotaAsync("tenant-123", 1_073_741_824);

// Check current usage
var result = await enforcer.CheckQuotaAsync("tenant-123");
Console.WriteLine($"Usage: {result.UsagePercent}%");
Console.WriteLine($"Over Quota: {result.IsOverQuota}");
Console.WriteLine($"Near Quota: {result.IsNearQuota}");

// Enforce quota (automatically suspends if over limit)
var enforcementResult = await enforcer.EnforceAsync("tenant-123", autoSuspend: true);

// Scan all tenants for warnings
var warnings = await enforcer.ScanAllAsync();
foreach (var tenant in warnings)
{
    Console.WriteLine($"Tenant {tenant.TenantId} is {(tenant.IsOverQuota ? "OVER" : "NEAR")} quota at {tenant.UsagePercent}%");
}

// Get only tenants that have exceeded their limit
var overQuotaTenants = await enforcer.GetTenantsOverQuotaAsync();
```
