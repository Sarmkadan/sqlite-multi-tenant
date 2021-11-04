# IHealthCheckService

IHealthCheckService defines a standardized contract for monitoring the operational integrity of the `sqlite-multi-tenant` framework. Implementations provide diagnostic checks for database connectivity, disk space availability, and overall system status, enabling robust infrastructure monitoring and health reporting in multi-tenant SQLite environments.

## API

*   **`Task<HealthCheckResponse> GetHealthStatusAsync()`**
    *   **Purpose**: Retrieves the aggregated health status of the system.
    *   **Returns**: A `Task<HealthCheckResponse>` containing the current health assessment.
    *   **Throws**: May throw infrastructure-related exceptions if the system is unreachable.

*   **`Task<bool> IsDatabaseHealthyAsync()`**
    *   **Purpose**: Validates the connectivity and responsiveness of the configured SQLite databases.
    *   **Returns**: A `Task<bool>` that resolves to `true` if the databases are healthy, otherwise `false`.

*   **`Task<bool> IsDiskSpaceHealthyAsync()`**
    *   **Purpose**: Checks for adequate free disk storage to support system operations.
    *   **Returns**: A `Task<bool>` that resolves to `true` if sufficient disk space is available, otherwise `false`.

*   **`Task<bool> IsSystemHealthyAsync()`**
    *   **Purpose**: Performs a holistic health check to determine if the overall system infrastructure is functioning correctly.
    *   **Returns**: A `Task<bool>` that resolves to `true` if the system is healthy, otherwise `false`.

*   **`Task<string> GetDetailedStatusAsync()`**
    *   **Purpose**: Retrieves a comprehensive, human-readable textual report describing the system's current status for diagnostic purposes.
    *   **Returns**: A `Task<string>` containing the detailed status description.

*   **`Dictionary<string, object> GetDetailedDiagnostics`**
    *   **Purpose**: A property that returns a structured collection of diagnostic metrics and state information.
    *   **Returns**: A `Dictionary<string, object>` representing detailed diagnostics.

## Usage

```csharp
// Example 1: Basic health status check
var healthStatus = await healthCheckService.GetHealthStatusAsync();
if (!healthStatus.IsHealthy)
{
    _logger.LogError("System health degraded: {Message}", healthStatus.Message);
}
```

```csharp
// Example 2: Retrieving detailed diagnostic metrics
var diagnostics = healthCheckService.GetDetailedDiagnostics;
foreach (var entry in diagnostics)
{
    _logger.LogInformation("Diagnostic {Key}: {Value}", entry.Key, entry.Value);
}
```

## Notes

*   **Thread Safety**: Implementations of `IHealthCheckService` are expected to be thread-safe to ensure that concurrent diagnostic requests are handled reliably.
*   **Asynchronous Execution**: All methods are designed as asynchronous operations. Blocking calls should be avoided to prevent thread pool exhaustion and ensure responsiveness under load.
*   **Resource Sensitivity**: Diagnostic checks may involve I/O-bound operations (e.g., database connectivity, disk space polling). Ensure that timeout policies are configured appropriately to prevent health checks from causing cascading failures.
