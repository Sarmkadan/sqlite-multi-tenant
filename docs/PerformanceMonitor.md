# PerformanceMonitor

A utility for tracking and analyzing performance metrics of database operations in a multi-tenant SQLite environment. It provides detailed instrumentation for monitoring operation duration, success rates, tenant-specific performance, and system health.

## API

### `PerformanceMonitor`
A singleton-like utility class for centralized performance tracking.

#### `public PerformanceMonitor`
Initializes a new instance of the `PerformanceMonitor` class. This constructor is public to allow for dependency injection or testing scenarios.

#### `public PerformanceTracker StartOperation(string operationName, string tenantId)`
Starts tracking a new database operation.

- **operationName**: The name of the operation being tracked (e.g., "QueryUsers", "InsertOrder").
- **tenantId**: The identifier of the tenant associated with the operation.
- **Return value**: A `PerformanceTracker` instance for recording metrics and exceptions during the operation.
- **Throws**: `ArgumentNullException` if `operationName` or `tenantId` is null.

#### `public void RecordMetric(string operationName, long elapsedMilliseconds, bool isSuccess)`
Records a completed operation's performance metric.

- **operationName**: The name of the operation being recorded.
- **elapsedMilliseconds**: The duration of the operation in milliseconds.
- **isSuccess**: Whether the operation completed successfully.
- **Throws**: `ArgumentNullException` if `operationName` is null.

#### `public OperationStatistics GetOperationStats(string operationName)`
Retrieves aggregated statistics for a specific operation across all tenants.

- **operationName**: The name of the operation to retrieve statistics for.
- **Return value**: An `OperationStatistics` object containing average duration, success rate, and total count for the specified operation.
- **Throws**: `ArgumentNullException` if `operationName` is null.

#### `public Dictionary<string, OperationStatistics> GetAllStatistics()`
Retrieves aggregated statistics for all tracked operations.

- **Return value**: A dictionary mapping operation names to their respective `OperationStatistics` objects.

#### `public Dictionary<string, List<PerformanceMetric>> GetTenantMetrics()`
Retrieves raw performance metrics grouped by tenant.

- **Return value**: A dictionary mapping tenant IDs to lists of `PerformanceMetric` objects recorded for that tenant.

#### `public List<PerformanceMetric> GetSlowOperations(int thresholdMilliseconds)`
Retrieves metrics for operations exceeding a specified duration threshold.

- **thresholdMilliseconds**: The minimum duration (in milliseconds) for an operation to be considered slow.
- **Return value**: A list of `PerformanceMetric` objects for operations exceeding the threshold, sorted by duration (descending).

#### `public SystemHealthSummary GetHealthSummary()`
Generates a summary of system health based on recorded metrics.

- **Return value**: A `SystemHealthSummary` object containing overall success rate, average operation duration, and counts of slow or failed operations.

#### `public void ClearMetrics()`
Resets all recorded metrics and statistics. Useful for testing or between test runs.

### `PerformanceTracker : IDisposable`
A disposable tracker for a single operation, enabling structured recording of metrics and exceptions.

#### `public PerformanceTracker`
Initializes a new instance of the `PerformanceTracker` class. This constructor is called internally by `PerformanceMonitor.StartOperation`.

#### `public void Dispose()`
Releases resources associated with the tracker. Automatically called when the tracker is used in a `using` statement.

#### `public void RecordException(Exception ex)`
Records an exception that occurred during the tracked operation.

- **ex**: The exception to record.
- **Throws**: `ArgumentNullException` if `ex` is null.

### `PerformanceMetric`
Represents a single recorded performance metric for an operation.

#### `public string OperationName`
Gets the name of the operation.

#### `public long ElapsedMilliseconds`
Gets the duration of the operation in milliseconds.

#### `public string TenantId`
Gets the identifier of the tenant associated with the operation.

#### `public DateTime Timestamp`
Gets the time when the metric was recorded.

#### `public bool IsSuccess`
Gets whether the operation completed successfully.

## Usage

### Example 1: Basic Operation Tracking
