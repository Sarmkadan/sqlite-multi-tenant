# IntegrityCheckService

`IntegrityCheckService` implements `IIntegrityCheckService` and runs SQLite `PRAGMA integrity_check` against tenant database files. It supports checking one tenant, an explicit set of tenants, all registered tenants, or only active tenants.

The service resolves tenants through `ITenantService`, opens each tenant's database directly, and applies a 60-second SQLite command timeout. Successful SQLite output is normally `ok`; other output is retained for diagnostics in the returned result.

## Registration and usage

Resolve the service through `IIntegrityCheckService` after registering its implementation with dependency injection:

```csharp
services.AddScoped<IIntegrityCheckService, IntegrityCheckService>();
```

```csharp
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Services;

// Check one tenant.
TenantIntegrityCheckResult result =
    await integrityCheckService.CheckTenantIntegrityAsync(
        "tenant-123",
        cancellationToken);

Console.WriteLine($"Tenant: {result.TenantId}, IsOk: {result.IsOk}");

// Check a selected set of tenants.
IEnumerable<string> tenantIds = ["tenant-123", "tenant-456"];
List<TenantIntegrityCheckResult> selectedResults =
    await integrityCheckService.CheckTenantsIntegrityAsync(
        tenantIds,
        maxDegreeOfParallelism: 2,
        cancellationToken);

// Check every registered tenant.
List<TenantIntegrityCheckResult> allResults =
    await integrityCheckService.CheckAllTenantsIntegrityAsync(
        maxDegreeOfParallelism: 4,
        cancellationToken);

// Check only active tenants.
List<TenantIntegrityCheckResult> activeResults =
    await integrityCheckService.CheckActiveTenantsIntegrityAsync(
        maxDegreeOfParallelism: 4,
        cancellationToken);
```

## Methods

### `CheckTenantIntegrityAsync`

```csharp
Task<TenantIntegrityCheckResult> CheckTenantIntegrityAsync(
    string tenantId,
    CancellationToken cancellationToken = default);
```

Checks a single tenant database. The method:

1. Validates that `tenantId` is not null, empty, or whitespace.
2. Resolves the tenant with `ITenantService.GetTenantAsync`.
3. Verifies that the tenant has an existing database file.
4. Executes `PRAGMA integrity_check` and returns its status and diagnostic output.

An invalid identifier causes `ArgumentException`; an unknown tenant causes `TenantNotFoundException`; and a missing database path or file causes `InvalidOperationException`. Errors that occur after the tenant and file validation are captured in a failed result rather than propagated. Cancellation is also captured in a failed result if it occurs while the database check is executing.

### `CheckTenantsIntegrityAsync`

```csharp
Task<List<TenantIntegrityCheckResult>> CheckTenantsIntegrityAsync(
    IEnumerable<string> tenantIds,
    int maxDegreeOfParallelism = 4,
    CancellationToken cancellationToken = default);
```

Checks the supplied tenant identifiers. A null `tenantIds` argument causes `ArgumentNullException`. Each per-tenant exception is converted into a failed result whose `TenantName` is `"Unknown"` and whose `Error` contains the exception message, allowing the remaining checks to continue.

The results follow the input enumeration order. Duplicate identifiers are checked and returned more than once. An empty sequence returns an empty list.

### `CheckAllTenantsIntegrityAsync`

```csharp
Task<List<TenantIntegrityCheckResult>> CheckAllTenantsIntegrityAsync(
    int maxDegreeOfParallelism = 4,
    CancellationToken cancellationToken = default);
```

Loads every tenant from `ITenantService.GetAllTenantsAsync`, then checks each tenant identifier using the batch behavior described above. An installation with no registered tenants returns an empty list.

### `CheckActiveTenantsIntegrityAsync`

```csharp
Task<List<TenantIntegrityCheckResult>> CheckActiveTenantsIntegrityAsync(
    int maxDegreeOfParallelism = 4,
    CancellationToken cancellationToken = default);
```

Loads tenants from `ITenantService.GetActiveTenantsAsync`, so inactive tenants are excluded, then checks each returned tenant using the same batch behavior. If there are no active tenants, the method returns an empty list.

## `maxDegreeOfParallelism` semantics

The three batch methods default `maxDegreeOfParallelism` to `4` and pass it to the same internal batch routine.

- Values less than `1`, including `0` and negative values, are normalized to `1`.
- A value of `1` runs checks sequentially in enumeration order. Before starting each check, the loop tests the cancellation token; if cancellation has been requested, it stops and returns the results collected so far.
- A value greater than `1` selects the parallel path. In the current implementation, all per-tenant tasks are created and started before `Task.WhenAll` is awaited. Consequently, the value selects parallel execution but does not currently enforce a concurrency ceiling of that size.
- In the parallel path, per-tenant failures, including cancellation observed inside a tenant check, are normally represented as failed results. The returned list preserves the input tenant order.

Choose `1` when checks must be serialized. Values greater than `1` should be treated as enabling parallel execution rather than as a guaranteed throttle in the current implementation.

## Integrity result

Every completed check is represented by `TenantIntegrityCheckResult`. Its fields and computed summaries are documented in [TenantIntegrityCheckResult.md](TenantIntegrityCheckResult.md).

The principal fields populated by this service are:

| Field | Meaning |
| --- | --- |
| `TenantId` | Identifier of the checked tenant. |
| `TenantName` | Tenant display name, or `"Unknown"` when batch-level tenant resolution fails. |
| `IsOk` | `true` when SQLite output indicates a successful integrity check; otherwise `false`. |
| `Error` | Failure or diagnostic message when the check is unsuccessful. |
| `IntegrityOutput` | Raw, trimmed output returned by `PRAGMA integrity_check` when the command produces output. |
| `CheckedAt` | UTC timestamp assigned when the result is created. |

`IsSuccess`, `ResultSummary`, and `DetailedResult` are computed by `TenantIntegrityCheckResult` from these values.
