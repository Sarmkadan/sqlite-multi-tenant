// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Logging;

/// <summary>
/// Extension methods for structured logging with semantic context.
/// Improves log searchability and analysis in centralized logging systems.
/// Follows structured logging best practices for production systems.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Logs tenant operation with full context.
    /// Includes tenant ID, operation, and result for audit trails.
    /// </summary>
    public static void LogTenantOperation(
        this ILogger logger,
        string operation,
        string tenantId,
        string result,
        long? durationMs = null)
    {
        var level = result == "success" ? LogLevel.Information : LogLevel.Warning;

        logger.Log(level,
            "Tenant operation: {operation} | TenantId: {tenantId} | Result: {result} | Duration: {duration}ms",
            operation,
            tenantId,
            result,
            durationMs ?? 0);
    }

    /// <summary>
    /// Logs database operation with performance metrics.
    /// Helps identify slow database queries and operations.
    /// </summary>
    public static void LogDatabaseOperation(
        this ILogger logger,
        string operation,
        string databaseId,
        long durationMs,
        bool success = true)
    {
        var level = success ? LogLevel.Debug : LogLevel.Error;

        if (durationMs > 5000)
            level = LogLevel.Warning; // Slow query

        logger.Log(level,
            "Database operation: {operation} | DatabaseId: {databaseId} | Duration: {duration}ms | Success: {success}",
            operation,
            databaseId,
            durationMs,
            success);
    }

    /// <summary>
    /// Logs backup operation with size and timing.
    /// Critical for monitoring backup success and performance.
    /// </summary>
    public static void LogBackupOperation(
        this ILogger logger,
        string operation,
        string backupId,
        long sizeBytes,
        long durationMs,
        bool success = true)
    {
        var level = success ? LogLevel.Information : LogLevel.Error;

        logger.Log(level,
            "Backup operation: {operation} | BackupId: {backupId} | Size: {size}MB | Duration: {duration}ms",
            operation,
            backupId,
            sizeBytes / 1_000_000,
            durationMs);
    }

    /// <summary>
    /// Logs migration operation with version tracking.
    /// Important for understanding schema evolution.
    /// </summary>
    public static void LogMigrationOperation(
        this ILogger logger,
        string operation,
        string migrationId,
        string version,
        string name,
        long durationMs,
        bool success = true)
    {
        var level = success ? LogLevel.Information : LogLevel.Error;

        logger.Log(level,
            "Migration operation: {operation} | Version: {version} | Name: {name} | Duration: {duration}ms",
            operation,
            version,
            name,
            durationMs);
    }

    /// <summary>
    /// Logs API request with status code and timing.
    /// Used by logging middleware for performance tracking.
    /// </summary>
    public static void LogApiRequest(
        this ILogger logger,
        string method,
        string path,
        int statusCode,
        long durationMs)
    {
        var level = statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

        logger.Log(level,
            "API request: {method} {path} | StatusCode: {status} | Duration: {duration}ms",
            method,
            path,
            statusCode,
            durationMs);
    }

    /// <summary>
    /// Logs cache operation (hit/miss/eviction).
    /// Helps tune cache configuration for optimal performance.
    /// </summary>
    public static void LogCacheOperation(
        this ILogger logger,
        string operation,
        string cacheKey,
        bool hit,
        long? durationMs = null)
    {
        var level = hit ? LogLevel.Debug : LogLevel.Debug;

        logger.Log(level,
            "Cache operation: {operation} | Key: {key} | Hit: {hit} | Duration: {duration}ms",
            operation,
            cacheKey,
            hit,
            durationMs ?? 0);
    }

    /// <summary>
    /// Logs validation error with field-level details.
    /// Improves user experience by pinpointing validation failures.
    /// </summary>
    public static void LogValidationError(
        this ILogger logger,
        string entityType,
        Dictionary<string, string> errors)
    {
        var errorMessage = string.Join("; ", errors.Select(e => $"{e.Key}: {e.Value}"));

        logger.LogWarning(
            "Validation error for {entityType}: {errors}",
            entityType,
            errorMessage);
    }

    /// <summary>
    /// Logs webhook delivery attempt with retry information.
    /// Critical for debugging integration issues.
    /// </summary>
    public static void LogWebhookDelivery(
        this ILogger logger,
        string webhookId,
        string url,
        int retry,
        int maxRetries,
        bool success = true)
    {
        var level = success ? LogLevel.Information : LogLevel.Warning;

        logger.Log(level,
            "Webhook delivery: {webhookId} | URL: {url} | Retry: {retry}/{maxRetries} | Success: {success}",
            webhookId,
            url,
            retry,
            maxRetries,
            success);
    }

    /// <summary>
    /// Logs background job execution with performance metrics.
    /// Used by background workers to track execution health.
    /// </summary>
    public static void LogBackgroundJob(
        this ILogger logger,
        string jobName,
        long durationMs,
        int itemsProcessed = 0,
        bool success = true)
    {
        var level = success ? LogLevel.Information : LogLevel.Error;

        logger.Log(level,
            "Background job: {jobName} | Duration: {duration}ms | Items: {items} | Success: {success}",
            jobName,
            durationMs,
            itemsProcessed,
            success);
    }

    /// <summary>
    /// Logs health check result.
    /// Used to diagnose system issues and alert on failures.
    /// </summary>
    public static void LogHealthCheck(
        this ILogger logger,
        string componentName,
        bool healthy,
        long durationMs,
        string message = null)
    {
        var level = healthy ? LogLevel.Debug : LogLevel.Warning;

        logger.Log(level,
            "Health check: {component} | Status: {status} | Duration: {duration}ms | Message: {message}",
            componentName,
            healthy ? "healthy" : "unhealthy",
            durationMs,
            message ?? string.Empty);
    }

    /// <summary>
    /// Logs configuration error for debugging setup issues.
    /// </summary>
    public static void LogConfigurationError(
        this ILogger logger,
        string configKey,
        string expectedValue,
        string actualValue = null)
    {
        logger.LogError(
            "Configuration error: {key} | Expected: {expected} | Actual: {actual}",
            configKey,
            expectedValue,
            actualValue ?? "not set");
    }
}

/// <summary>
/// Structured logging context for operation tracking.
/// Implements IDisposable to auto-log completion on scope exit.
/// </summary>
public class OperationContext : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly System.Diagnostics.Stopwatch _stopwatch;

    public OperationContext(ILogger logger, string operationName)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operationName = operationName;
        _stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation("Operation started: {operation}", operationName);
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _logger.LogInformation("Operation completed: {operation} | Duration: {duration}ms",
            _operationName,
            _stopwatch.ElapsedMilliseconds);
    }
}
