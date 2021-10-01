# PerformanceMiddleware

The `PerformanceMiddleware` component is an ASP.NET Core middleware that measures the execution time and memory consumption of each HTTP request, creates a `RequestMetrics` record, and forwards it to an injected `PerformanceMonitor` for storage and later retrieval.

## API

### PerformanceMiddleware (class)

A sealed middleware class that wraps the ASP.NET Core pipeline to capture request performance data.

### PerformanceMiddleware(RequestDelegate next, PerformanceMonitor monitor)

**Constructor**  
- **next** – The delegate representing the remaining middleware pipeline.  
- **monitor** – The `PerformanceMonitor` instance used to persist metrics.  

**Purpose** – Initializes the middleware with the pipeline delegate and monitor.  
**Throws** – `ArgumentNullException` if `next` or `monitor` is `null`.

### Task InvokeAsync(HttpContext context)

**Parameters**  
- **context** – The `HttpContext` for the current request.  

**Return Value** – A `Task` that completes when the request has been processed.  

**Purpose** – Wraps the execution of the next middleware, measures the elapsed time and memory usage, builds a `RequestMetrics` instance, and asynchronously records it via `PerformanceMonitor`. The method ensures metrics are recorded even if the downstream pipeline throws an exception.  
**Throws** – `ArgumentNullException` if `context` is `null`; any exception thrown by the `next` delegate is propagated after the metric has been recorded.

### RequestMetrics (class)

A sealed data transfer object representing a single request’s performance measurements.

#### string Method

The HTTP method (e.g., `GET`, `POST`) of the request.

#### string Path

The request path (e.g., `/api/values`).

#### int StatusCode

The HTTP status code returned by the application (e.g., `200`, `404`).

#### long ElapsedMs

The elapsed time of the request processing in milliseconds.

#### long MemoryUsedKb

The approximate memory consumed during the request, measured in kilobytes.

#### DateTime Timestamp

The UTC timestamp when the metric was recorded.

### PerformanceMonitor (class)

A sealed component responsible for storing `RequestMetrics` instances and providing query methods for aggregated statistics and recent entries.

#### Task RecordMetricAsync(RequestMetrics metric)

**Parameters**  
- **metric** – The request metric to store.  

**Return Value** – A `Task` that completes when the metric has been persisted.  

**Purpose** – Asynchronously saves the supplied metric to the underlying storage mechanism.  
**Throws** – `ArgumentNullException` if `metric` is `null`; may throw storage‑specific exceptions (e.g., `IOException`) if persistence fails.

#### Task<PerformanceStats> GetStatsAsync()

**Return Value** – A `Task<PerformanceStats>` yielding aggregated statistics for all recorded metrics.  

**Purpose** – Retrieves summary statistics such as totals, averages, and extremes.  
**Throws** – May throw if the monitor has not been initialized or if the storage is unavailable.

#### Task<List<RequestMetrics>> GetRecentMetricsAsync()

**Return Value** – A `Task<List<RequestMetrics>>` containing the most recent request metrics (the exact number is implementation‑defined).  

**Purpose** – Provides access to a snapshot of recent performance data for debugging or monitoring UI.  
**Throws** – May throw if the monitor is not ready or if an error occurs while fetching the data.

### PerformanceStats (class)

A sealed container for aggregate performance statistics derived from stored metrics.

#### int TotalRequests

The total number of requests that have been recorded.

#### double AverageElapsedMs

The average elapsed time across all recorded requests, in milliseconds.

#### long MaxElapsedMs

The maximum elapsed time observed among recorded requests, in milliseconds.

#### long MinElapsedMs

The minimum elapsed time observed among recorded requests, in milliseconds.

#### double AverageMemoryUsedKb

The average memory usage across all recorded requests, in kilobytes.

## Usage

### Example 1: Registering the middleware in an ASP.NET Core application

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Monitoring; // namespace containing PerformanceMiddleware and PerformanceMonitor

var builder = WebApplication.CreateBuilder(args);

// Register the monitor as a singleton (or scoped) service
builder.Services.AddSingleton<PerformanceMonitor>();

var app = builder.Build();

// Insert the performance middleware early in the pipeline
app.UseMiddleware<PerformanceMiddleware>();

app.MapGet("/", () => "Hello World!");
app.Run();
```

### Example 2: Retrieving statistics and recent metrics from the monitor

```csharp
using Microsoft.AspNetCore.Http;
using SqliteMultiTenant.Monitoring;

public class MetricsController : ControllerBase
{
    private readonly PerformanceMonitor _monitor;

    public MetricsController(PerformanceMonitor monitor)
    {
        _monitor = monitor;
    }

    [HttpGet("/metrics/stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _monitor.GetStatsAsync();
        return Ok(stats);
    }

    [HttpGet("/metrics/recent")]
    public async Task<IActionResult> GetRecent()
    {
        var recent = await _monitor.GetRecentMetricsAsync();
        return Ok(recent);
    }
}
```

## Notes

- The middleware records a metric **after** the downstream pipeline has completed, using a `try/finally` block to ensure that metrics are captured even if the next delegate throws an exception. The original exception is re‑thrown after recording.
- All members of `PerformanceMonitor` are intended to be thread‑safe; concurrent calls from multiple requests will not corrupt internal state.
- `GetRecentMetricsAsync` returns a snapshot; the list should not be mutated by callers as doing so may affect internal caching depending on the implementation.
- If the `PerformanceMonitor` backing store becomes unavailable, the async methods may throw storage‑specific exceptions (e.g., `IOException`). The middleware does not swallow these exceptions; they will propagate up the pipeline after the metric recording attempt.
- The `ElapsedMs` and `MemoryUsedKb` values are measured with `Stopwatch` and process memory counters, respectively; they reflect wall‑clock time and approximate managed memory usage, not precise native allocations.
