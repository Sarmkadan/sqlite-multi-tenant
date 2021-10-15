# AdminController
AdminController provides administrative endpoints for monitoring and managing the application’s health, metrics, cache, and diagnostics. It is intended to be hosted within an ASP.NET Core pipeline and called by operators or monitoring tools to obtain runtime information or trigger maintenance operations.

## API
### Constructor
- **public AdminController()**  
  *Purpose*: Initializes a new instance of the AdminController.  
  *Parameters*: None.  
  *Return value*: A ready‑to‑use AdminController instance.  
  *Throws*: May throw if internal dependencies required for health checks, metrics collection, or diagnostics cannot be resolved (e.g., missing configuration).

### GetHealthAsync
- **public async Task<IActionResult> GetHealthAsync()**  
  *Purpose*: Asynchronously retrieves the current health status of the application.  
  *Parameters*: None.  
  *Return value*: A `Task<IActionResult>` that yields an `ObjectResult` containing a `HealthCheckResponse` (HTTP 200) when the check completes, or an error result (e.g., `ProblemDetails`) if the health check fails.  
  *Throws*: May throw `OperationCanceledException` if the request is cancelled; may throw `InvalidOperationException` if a health‑check component is unavailable.

### GetMetrics
- **public IActionResult GetMetrics()**  
  *Purpose*: Returns a snapshot of system‑level metrics.  
  *Parameters*: None.  
  *Return value*: An `IActionResult` (typically a `JsonResult`) with serialized `SystemMetrics` (HTTP 200).  
  *Throws*: May throw if metric sources (performance counters, etc.) cannot be accessed.

### GetMetricsDashboard
- **public IActionResult GetMetricsDashboard()**  
  *Purpose*: Returns an HTML view that visualizes the current metrics in a dashboard format.  
  *Parameters*: None.  
  *Return value*: An `IActionResult` (usually a `ViewResult`) rendering the dashboard page.  
  *Throws*: May throw if the corresponding view file is missing, cannot be found, or fails to compile.

### ClearCache
- **public IActionResult ClearCache()**  
  *Purpose*: Clears any application‑level caches maintained by the host.  
  *Parameters*: None.  
  *Return value*: An `IActionResult` indicating success (e.g., `OkResult`) or failure.  
  *Throws*: May throw if the underlying cache provider throws an exception during the clear operation.

### ForceGarbageCollection
- **public IActionResult ForceGarbageCollection()**  
  *Purpose*: Forces a garbage collection cycle and reports the outcome.  
  *Parameters*: None.  
  *Return value*: An `IActionResult` (typically `OkResult`) indicating that `GC.Collect` was invoked.  
  *Throws*: May throw if `GC.Collect` encounters an unexpected error (rare).

### GetDiagnostics
- **public IActionResult GetDiagnostics()**  
  *Purpose*: Returns diagnostic information such as configuration values, loaded assemblies, and environment details.  
  *Parameters*: None.  
  *Return value*: An `IActionResult` containing a diagnostic payload (often JSON).  
  *Throws*: May throw if diagnostic sources (e.g., configuration providers) cannot be read.

### HealthCheckResponse
- **public sealed class HealthCheckResponse**  
  *Purpose*: Data transfer object that represents the result of a health check.  
  *Properties*:  
    - `public bool IsHealthy { get; set; }` – Overall health flag.  
    - `public string Status { get; set; }` – Human‑readable status (e.g., "Healthy", "Degraded").  
    - `public DateTime Timestamp { get; set; }` – Time at which the check was performed.  
    - `public string Version { get; set; }` – Application version string.  
  *Constructor*: Implicit parameterless constructor.  
  *Throws*: None (properties are simple get/set).

### SystemMetrics
- **public sealed class SystemMetrics**  
  *Purpose*: Data transfer object that holds runtime metrics.  
  *Properties*:  
    - `public DateTime Timestamp { get; set; }` – Moment when the metrics were sampled.  
    - `public long ProcessMemoryMb { get; set; }` – Memory usage of the process in megabytes.  
    - `public int ThreadCount { get; set; }` – Number of managed threads.  
    - `public int ActiveConnections { get; set; }` – Count of active network/connections.  
    - `public long RequestsProcessed { get; set; }` – Total requests processed since startup.  
    - `public double AverageResponseTimeMs { get; set; }` – Average response time in milliseconds.  
  *Constructor*: Implicit parameterless constructor.  
  *Throws*: None.

## Usage
```csharp
// Example 1: Invoking the health endpoint from a client using HttpClient
using var http = new HttpClient();
var response = await http.GetAsync("https://api.example.com/admin/health");
response.EnsureSuccessStatusCode();
var health = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();
Console.WriteLine($"Healthy: {health.IsHealthy}, Status: {health.Status}");
```

```csharp
// Example 2: Using the controller within an ASP.NET Core minimal API
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers(); // registers AdminController
var app = builder.Build();
app.MapControllers();
app.Run();

// After the app is running, a GET to /admin/metrics returns JSON like:
// { "timestamp":"2025-09-24T12:34:56Z", "processMemoryMb":128, ... }
```

## Notes
- The controller is effectively stateless; its action methods do not rely on mutable instance fields, so concurrent requests are safe except for operations that affect global process state (`ClearCache`, `ForceGarbageCollection`). Those methods are idempotent but may produce race conditions if called simultaneously from multiple threads.
- `GetHealthAsync` is asynchronous; callers should await the returned task to avoid blocking threads. Cancellation tokens are honored automatically by ASP.NET Core.
- The returned DTOs (`HealthCheckResponse`, `SystemMetrics`) are intended to be immutable after deserialization; mutating them has no effect on the controller’s state.
- Exceptions thrown inside an action are caught by the ASP.NET Core pipeline and transformed into a 500 (Internal Server Error) response unless the action explicitly returns a different status code.
- Because `AdminController` inherits from `ControllerBase`, it must be used within a request pipeline that provides the necessary services (e.g., logging, configuration). Instantiating it directly with `new AdminController()` outside of such a pipeline may lead to missing dependencies and exceptions.
