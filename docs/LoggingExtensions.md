# LoggingExtensions

`LoggingExtensions` provides a centralized, structured logging facade for the `sqlite-multi-tenant` system. It exposes static convenience methods that encapsulate log-level selection, enrichment, and context propagation for tenant-scoped operations, database interactions, background processing, and infrastructure concerns. The companion nested type `OperationContext` enables automatic correlation of log entries within a disposable scope.

## API

### LogTenantOperation
```csharp
public static void LogTenantOperation(string tenantId, string operation, LogLevel level, string message, Exception? exception = null)
```
Records a log entry scoped to a specific tenant. Accepts the tenant identifier, an operation name, the severity level, a descriptive message, and an optional exception. Throws `ArgumentNullException` when `tenantId`, `operation`, or `message` is null.

### LogDatabaseOperation
```csharp
public static void LogDatabaseOperation(string databaseName, string operation, long elapsedMilliseconds, string? details = null)
```
Logs a database interaction with timing information. `databaseName` identifies the target database file or shard, `operation` describes the action (e.g., query, insert), and `elapsedMilliseconds` captures duration. Throws `ArgumentNullException` when `databaseName` or `operation` is null; throws `ArgumentOutOfRangeException` when `elapsedMilliseconds` is negative.

### LogBackupOperation
```csharp
public static void LogBackupOperation(string sourcePath, string destinationPath, bool success, long sizeBytes, string? errorMessage = null)
```
Logs the outcome of a backup procedure. Includes source and destination paths, a success flag, the size in bytes transferred or processed, and an optional error description for failed backups. Throws `ArgumentNullException` when `sourcePath` or `destinationPath` is null.

### LogMigrationOperation
```csharp
public static void LogMigrationOperation(string tenantId, int fromVersion, int toVersion, bool success, string? errorMessage = null)
```
Logs a schema or data migration event for a tenant. Captures the version transition range, outcome, and optional failure details. Throws `ArgumentNullException` when `tenantId` is null; throws `ArgumentOutOfRangeException` when either version number is negative.

### LogApiRequest
```csharp
public static void LogApiRequest(string method, string path, int statusCode, long elapsedMilliseconds, string? clientIp = null)
```
Logs an inbound API request with the HTTP method, request path, response status code, duration, and optional client IP address. Throws `ArgumentNullException` when `method` or `path` is null; throws `ArgumentOutOfRangeException` when `elapsedMilliseconds` is negative.

### LogCacheOperation
```csharp
public static void LogCacheOperation(string cacheName, string operation, bool hit, long elapsedMicroseconds, string? key = null)
```
Logs a cache access event. `cacheName` identifies the cache instance, `operation` describes the action (e.g., get, set), `hit` indicates whether the item was found, `elapsedMicroseconds` records latency in microseconds, and `key` optionally identifies the cache key. Throws `ArgumentNullException` when `cacheName` or `operation` is null; throws `ArgumentOutOfRangeException` when `elapsedMicroseconds` is negative.

### LogValidationError
```csharp
public static void LogValidationError(string entityType, string fieldName, string errorMessage, string? inputValue = null)
```
Logs a data validation failure. Identifies the entity type, the field that failed validation, the error description, and optionally the rejected input value. Throws `ArgumentNullException` when `entityType`, `fieldName`, or `errorMessage` is null.

### LogWebhookDelivery
```csharp
public static void LogWebhookDelivery(string webhookId, string targetUrl, int attemptNumber, bool success, int statusCode, long elapsedMilliseconds, string? errorMessage = null)
```
Logs a webhook delivery attempt. Includes the webhook configuration identifier, target URL, attempt number, success flag, HTTP response status code, duration, and optional error details. Throws `ArgumentNullException` when `webhookId` or `targetUrl` is null; throws `ArgumentOutOfRangeException` when `attemptNumber` is less than 1 or `elapsedMilliseconds` is negative.

### LogBackgroundJob
```csharp
public static void LogBackgroundJob(string jobName, string jobId, bool success, long elapsedMilliseconds, string? errorMessage = null)
```
Logs the execution outcome of a background job. Identifies the job by name and unique identifier, records success status, duration, and optional failure details. Throws `ArgumentNullException` when `jobName` or `jobId` is null; throws `ArgumentOutOfRangeException` when `elapsedMilliseconds` is negative.

### LogHealthCheck
```csharp
public static void LogHealthCheck(string componentName, bool healthy, long elapsedMilliseconds, string? details = null)
```
Logs a health-check probe result for a named component. Captures the healthy/unhealthy status, probe latency, and optional diagnostic details. Throws `ArgumentNullException` when `componentName` is null; throws `ArgumentOutOfRangeException` when `elapsedMilliseconds` is negative.

### LogConfigurationError
```csharp
public static void LogConfigurationError(string key, string errorMessage, string? source = null)
```
Logs a configuration error for a specific key. Includes the configuration key name, the error description, and an optional source identifier (e.g., file path, environment variable). Throws `ArgumentNullException` when `key` or `errorMessage` is null.

### OperationContext
```csharp
public sealed class OperationContext : IDisposable
```
Creates a scoped logging context that automatically attaches correlation identifiers and tenant metadata to all log entries produced within its lifetime. Implements `IDisposable`; disposing the instance restores the previous ambient context.

#### OperationContext constructor
```csharp
public OperationContext(string operationName, string? tenantId = null, string? correlationId = null)
```
Initializes a new operation scope. `operationName` is required and names the logical operation. `tenantId` optionally scopes all nested log calls to a specific tenant. `correlationId` optionally overrides the auto-generated correlation identifier. Throws `ArgumentNullException` when `operationName` is null.

#### Dispose
```csharp
public void Dispose()
```
Releases the operation scope and restores the prior logging context. Safe to call multiple times; subsequent calls have no effect.

## Usage

### Example 1: Tenant-scoped database migration with operation context
```csharp
using (var ctx = new OperationContext("TenantMigration", tenantId: "tenant-42"))
{
    LoggingExtensions.LogMigrationOperation("tenant-42", fromVersion: 3, toVersion: 4, success: true);

    var sw = Stopwatch.StartNew();
    // ... perform migration steps ...
    sw.Stop();

    LoggingExtensions.LogDatabaseOperation("tenant-42.db", "migrate", sw.ElapsedMilliseconds);
}
```
All log entries within the `using` block automatically share the same correlation identifier and tenant metadata, simplifying traceability across migration steps.

### Example 2: Webhook delivery with error handling
```csharp
var sw = Stopwatch.StartNew();
bool success = false;
int statusCode = 0;
string? error = null;

try
{
    // ... send webhook ...
    statusCode = 200;
    success = true;
}
catch (Exception ex)
{
    error = ex.Message;
    statusCode = 0;
}
finally
{
    sw.Stop();
    LoggingExtensions.LogWebhookDelivery(
        webhookId: "wh-abc123",
        targetUrl: "https://example.com/callback",
        attemptNumber: 2,
        success: success,
        statusCode: statusCode,
        elapsedMilliseconds: sw.ElapsedMilliseconds,
        errorMessage: error);
}
```

## Notes

- All static logging methods are thread-safe. They delegate to an underlying logger provider that must be configured before first use; calling any method before initialization results in a no-op or fallback behavior depending on provider setup.
- `OperationContext` relies on an `AsyncLocal`-backed ambient context and is safe for use across asynchronous continuations within the same logical call flow. Disposing an `OperationContext` on a different thread than the one that created it is supported but may produce unexpected correlation gaps if the original thread continues logging after disposal.
- Negative timing values passed to methods with `elapsedMilliseconds` or `elapsedMicroseconds` parameters throw `ArgumentOutOfRangeException` at the call site rather than silently clamping, ensuring invalid telemetry is rejected early.
- The `OperationContext` constructor does not begin any background activity or timers; it merely pushes context state. Nesting multiple `OperationContext` instances is permitted—each `Dispose` pops one level, restoring the previous scope.
- Methods that accept optional `Exception` or `errorMessage` parameters treat `null` as absence of error information; they do not substitute default messages.
