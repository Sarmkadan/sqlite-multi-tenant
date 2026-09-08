# MetricsService Documentation

## Overview
The `MetricsService` class implements the `IMetricsService` interface and provides in-memory metrics collection with thread-safe aggregation for monitoring system performance and behavior. It tracks request counts, response times, error rates, backup operations, migration operations, and application errors.

## Relation to IMetricsService
This class implements the `IMetricsService` interface defined in [IMetricsService.md](./IMetricsService.md). All interface methods are implemented with thread-safe operations using `Interlocked` for counters and locks for collections where necessary.

## Public Methods

### `RecordRequest(string path, long durationMs, int statusCode)`
Records an HTTP request metric.
- **Parameters**:
  - `path`: The request path/endpoint
  - `durationMs`: Request duration in milliseconds
  - `statusCode`: HTTP status code
- **Behavior**: 
  - Increments total request counter
  - Increments error counter if status code >= 400
  - Records response time for average calculation
  - Updates endpoint-specific metrics (request count, success/error counts, average/min/max response times)

### `RecordBackup(long sizeBytes, long durationMs, bool success)`
Records a backup operation metric.
- **Parameters**:
  - `sizeBytes`: Size of backup in bytes
  - `durationMs`: Backup duration in milliseconds
  - `success`: Whether the backup succeeded
- **Behavior**:
  - Adds to total backup bytes
  - Increments total backups counter
  - Increments failed backups counter if not successful
  - Logs debug information

### `RecordMigration(string version, long durationMs, bool success)`
Records a database migration operation metric.
- **Parameters**:
  - `version`: Migration version identifier
  - `durationMs`: Migration duration in milliseconds
  - `success`: Whether the migration succeeded
- **Behavior**:
  - Increments total migrations counter
  - Increments failed migrations counter if not successful
  - Logs debug information

### `RecordError(string errorType, string message)`
Records an application error.
- **Parameters**:
  - `errorType`: Type/category of error
  - `message`: Error message
- **Behavior**:
  - Increments error count for the given error type
  - Logs warning information

### `GetSnapshot()`
Retrieves a snapshot of current metrics.
- **Returns**: `MetricsSnapshot` object containing all current metrics
- **Behavior**: 
  - Calculates average response time from recorded values
  - Returns deep copies of dictionaries to prevent external modification
  - Thread-safe implementation

### `Reset()`
Resets all metrics to initial state.
- **Behavior**:
  - Sets all counters to zero
  - Clears response times list
  - Clears error counts and endpoint metrics dictionaries
  - Logs information about reset
  - Useful for testing or daily rollover

### `GetReport()`
Exports metrics as a formatted human-readable text report.
- **Returns**: Formatted string report
- **Format**:
  ```
  === System Metrics Report ===
  Captured At: [ISO timestamp]

  HTTP Requests:
    Total: [count]
    Errors: [count]
    Error Rate: [percentage]%
    Avg Response Time: [value]ms

  Backups:
    Total: [count]
    Failed: [count]
    Success Rate: [percentage]%
    Total Data Backed Up: [value]GB

  Migrations:
    Total: [count]
    Failed: [count]
    Success Rate: [percentage]%
  ```

### `ToPrometheusExpositionFormat(TenantContext? tenantContext = null)`
Exports metrics in Prometheus text exposition format.
- **Parameters**:
  - `tenantContext`: Optional tenant context to filter metrics by tenant
- **Returns**: Prometheus formatted metrics string
- **Emitted Metrics**:
  - `sqlite_multi_tenant_requests_total` (counter)
  - `sqlite_multi_tenant_errors_total` (counter)
  - `sqlite_multi_tenant_error_rate` (gauge, percentage)
  - `sqlite_multi_tenant_request_duration_seconds` (gauge, average in seconds)
  - `sqlite_multi_tenant_backups_total` (counter)
  - `sqlite_multi_tenant_backups_failed_total` (counter)
  - `sqlite_multi_tenant_backup_size_bytes` (counter)
  - `sqlite_multi_tenant_migrations_total` (counter)
  - `sqlite_multi_tenant_migrations_failed_total` (counter)
  - `sqlite_multi_tenant_tenant_requests_total` (counter, with tenant_id label when tenantContext provided)
  - `sqlite_multi_tenant_tenant_errors_total` (counter, with tenant_id label)
  - `sqlite_multi_tenant_tenant_error_rate` (gauge, with tenant_id label)
  - `sqlite_multi_tenant_tenant_request_duration_seconds` (gauge, with tenant_id label)
  - `sqlite_multi_tenant_endpoint_requests_total` (counter, with tenant_id and endpoint labels)
  - `sqlite_multi_tenant_endpoint_errors_total` (counter, with tenant_id and endpoint labels)
  - `sqlite_multi_tenant_endpoint_request_duration_seconds` (gauge, with tenant_id and endpoint labels)
  - `sqlite_multi_tenant_errors_by_type_total` (counter, with tenant_id and error_type labels)
  - `sqlite_multi_tenant_build_info` (gauge, with version and captured_at labels)

## Snapshot DTO (MetricsSnapshot)

The `MetricsSnapshot` class contains the following fields:
- `CapturedAt` (DateTime): Timestamp when snapshot was taken (UTC)
- `TotalRequests` (long): Total number of HTTP requests
- `TotalErrors` (long): Total number of errors (status code >= 400)
- `AverageResponseTimeMs` (double): Average response time in milliseconds
- `TotalBackupBytes` (long): Total bytes backed up across all operations
- `TotalBackups` (int): Total number of backup operations
- `FailedBackups` (int): Number of failed backup operations
- `TotalMigrations` (int): Total number of database migrations
- `FailedMigrations` (int): Number of failed database migrations
- `ErrorCounts` (Dictionary<string, int>): Count of errors by error type
- `EndpointMetrics` (Dictionary<string, RequestMetrics>): Metrics by endpoint

### RequestMetrics (nested class)
- `Endpoint` (string): The endpoint path
- `RequestCount` (long): Total requests to this endpoint
- `SuccessCount` (long): Successful requests (status code < 400)
- `ErrorCount` (long): Failed requests (status code >= 400)
- `AverageResponseTimeMs` (double): Average response time for this endpoint
- `MaxResponseTimeMs` (long): Maximum response time for this endpoint
- `MinResponseTimeMs` (long): Minimum response time for this endpoint

## Thread Safety
All metric updates are thread-safe using:
- `Interlocked` operations for integer counters
- `lock` statement for list operations (response times)
- `ConcurrentDictionary` for error counts and endpoint metrics
- Deep copying of dictionaries in `GetSnapshot()` to prevent external modification

## Usage Example
```csharp
// Dependency injection
public class MyController : ControllerBase
{
    private readonly IMetricsService _metrics;
    
    public MyController(IMetricsService metrics)
    {
        _metrics = metrics;
    }
    
    public IActionResult GetData()
    {
        var timer = Stopwatch.StartNew();
        try
        {
            // Process request
            var result = GetDataFromDatabase();
            _metrics.RecordRequest(Request.Path, timer.ElapsedMilliseconds, 200);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _metrics.RecordRequest(Request.Path, timer.ElapsedMilliseconds, 500);
            _metrics.RecordError("DatabaseException", ex.Message);
            throw;
        }
    }
}
```