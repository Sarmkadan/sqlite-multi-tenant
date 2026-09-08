# HealthCheckService

`HealthCheckService` provides process, database, disk, and memory health checks for monitoring endpoints, load balancers, and orchestrators. It is defined in `src/Health/HealthCheckService.cs` in the `SqliteMultiTenant.Health` namespace.

## Relation to IHealthCheckService

`HealthCheckService` implements `IHealthCheckService`, whose contract is described in [IHealthCheckService.md](./IHealthCheckService.md). The interface and implementation are declared in the same source file. The implementation supplies all seven interface methods: aggregate and boolean system checks, a status-string check, and dedicated liveness and readiness checks.

`DetailedHealthCheckService` derives from `HealthCheckService`. It inherits the complete `IHealthCheckService` implementation and adds `GetDetailedDiagnostics()` for runtime details.

## Constructors

### HealthCheckService

```csharp
public HealthCheckService(ILogger<HealthCheckService> logger, string databasePath = ".")
```

Creates the health-check service. `logger` is required and causes an `ArgumentNullException` when null. `databasePath` identifies the filesystem location whose containing drive is checked; it defaults to the current directory.

### DetailedHealthCheckService

```csharp
public DetailedHealthCheckService(ILogger<DetailedHealthCheckService> logger, string databasePath = ".")
```

Creates the extended service and passes its logger and database path to the base implementation.

## Public Methods

### GetHealthStatusAsync

```csharp
public async Task<HealthCheckResponse> GetHealthStatusAsync()
```

Runs the database, disk, and memory checks and returns a component-level summary. The response contains `database`, `disk`, and `memory` entries. Its overall `Status` is `"healthy"` only when every component has that status; otherwise it is `"unhealthy"`.

### IsDatabaseHealthyAsync

```csharp
public Task<bool> IsDatabaseHealthyAsync()
```

Checks database health. The current implementation is a connectivity placeholder that logs success and returns `true`; if the operation throws, it logs a warning and returns `false`.

### IsDiskSpaceHealthyAsync

```csharp
public Task<bool> IsDiskSpaceHealthyAsync(long minimumFreeBytesRequired = 1_000_000_000)
```

Resolves the configured database path to its drive and compares available space with `minimumFreeBytesRequired`, which defaults to one billion bytes. It returns `false` when available space is below the threshold. Filesystem or permission errors are logged and treated as healthy so those errors do not fail the health check.

### IsSystemHealthyAsync

```csharp
public async Task<bool> IsSystemHealthyAsync()
```

Returns `true` only when the database, disk, and private memory checks all report healthy.

### GetDetailedStatusAsync

```csharp
public async Task<string> GetDetailedStatusAsync()
```

Calls `GetHealthStatusAsync()` and returns its overall `Status` value (`"healthy"` or `"unhealthy"`). Despite its name, it returns only the aggregate status string, not the component collection.

### CheckLivenessAsync

```csharp
public async Task<HealthStatusResult> CheckLivenessAsync()
```

Performs process-level checks without database or disk access. The returned result contains `process` and `memory` entries. Liveness is healthy only when the process has not exited and the private memory check succeeds. An unexpected exception produces an unhealthy result with an explanatory `process` entry.

### CheckReadinessAsync

```csharp
public async Task<HealthStatusResult> CheckReadinessAsync()
```

Checks whether the service is ready to accept traffic by running the database and disk checks. The returned result contains `database` and `disk` entries, and is healthy only when both are healthy. An unexpected exception produces an unhealthy result with an explanatory `database` entry.

### GetDetailedDiagnostics

```csharp
public Dictionary<string, object> GetDetailedDiagnostics()
```

Declared by `DetailedHealthCheckService`. Returns a diagnostic dictionary containing:

- `timestamp`: the current UTC time.
- `environment`: operating-system version, processor count, and .NET version.
- `process`: working-set memory, total processor time, and thread count.

## Result and Status DTOs

### HealthStatusResult

`HealthStatusResult` is declared in `HealthCheckService.cs` and is returned by the liveness and readiness methods.

| Property | Type | Description |
| --- | --- | --- |
| `Status` | `string` | Overall textual status, normally `"healthy"` or `"unhealthy"`. |
| `IsHealthy` | `bool` | Boolean form of the overall status. |
| `CheckedAt` | `DateTime` | UTC timestamp associated with the check. |
| `DurationMs` | `long` | Total elapsed time for the check in milliseconds. |
| `Checks` | `Dictionary<string, ComponentHealth>` | Component results keyed by names such as `process`, `memory`, `database`, or `disk`. |

`HealthCheckResponse` and `ComponentHealth` are used by this file but are declared in `src/Api/Responses/ApiResponses.cs`. `HealthCheckResponse` holds the aggregate status, check timestamp, and `Components` dictionary. Each `ComponentHealth` holds a status plus optional message and response time.

## Usage Example

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Health;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole());

IHealthCheckService healthChecks = new HealthCheckService(
    loggerFactory.CreateLogger<HealthCheckService>(),
    databasePath: "/var/lib/my-service/tenants");

var readiness = await healthChecks.CheckReadinessAsync();
Console.WriteLine($"Ready: {readiness.IsHealthy} ({readiness.Status})");

foreach (var (name, component) in readiness.Checks)
{
    Console.WriteLine($"{name}: {component.Status} - {component.Message}");
}

var health = await healthChecks.GetHealthStatusAsync();
Console.WriteLine($"Overall health: {health.Status}");

bool hasAtLeastFiveGbFree = await healthChecks.IsDiskSpaceHealthyAsync(
    minimumFreeBytesRequired: 5_000_000_000);
```
