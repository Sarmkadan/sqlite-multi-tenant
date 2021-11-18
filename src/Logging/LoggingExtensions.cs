#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
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
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="result">The operation result (success/failure).</param>
    /// <param name="durationMs">Optional duration in milliseconds.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/>, <paramref name="operation"/>, <paramref name="tenantId"/>, or <paramref name="result"/> is null.</exception>
    public static void LogTenantOperation(
        this ILogger logger,
        string operation,
        string tenantId,
        string result,
        long? durationMs = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(result);

        var level = result.Equals("success", StringComparison.OrdinalIgnoreCase)
            ? LogLevel.Information
            : LogLevel.Warning;

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
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The database operation type.</param>
    /// <param name="databaseId">The database identifier.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> or <paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="databaseId"/> is null or empty.</exception>
    public static void LogDatabaseOperation(
        this ILogger logger,
        string operation,
        string databaseId,
        long durationMs,
        bool success = true)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrEmpty(databaseId);

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
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The backup operation type.</param>
    /// <param name="backupId">The backup identifier.</param>
    /// <param name="sizeBytes">Backup size in bytes.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/>, <paramref name="operation"/>, or <paramref name="backupId"/> is null.</exception>
    public static void LogBackupOperation(
        this ILogger logger,
        string operation,
        string backupId,
        long sizeBytes,
        long durationMs,
        bool success = true)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(backupId);

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
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The migration operation type.</param>
    /// <param name="migrationId">The migration identifier.</param>
    /// <param name="version">The migration version.</param>
    /// <param name="name">The migration name.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/>, <paramref name="operation"/>, <paramref name="migrationId"/>, <paramref name="version"/>, or <paramref name="name"/> is null.</exception>
    public static void LogMigrationOperation(
        this ILogger logger,
        string operation,
        string migrationId,
        string version,
        string name,
        long durationMs,
        bool success = true)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(migrationId);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(name);

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
    /// <param name="logger">The logger instance.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The request path.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/>, <paramref name="method"/>, or <paramref name="path"/> is null.</exception>
    public static void LogApiRequest(
        this ILogger logger,
        string method,
        string path,
        int statusCode,
        long durationMs)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(path);

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
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The cache operation type.</param>
    /// <param name="cacheKey">The cache key.</param>
    /// <param name="hit">Whether the operation was a cache hit.</param>
    /// <param name="durationMs">Optional duration in milliseconds.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/>, <paramref name="operation"/>, or <paramref name="cacheKey"/> is null.</exception>
    public static void LogCacheOperation(
        this ILogger logger,
        string operation,
        string cacheKey,
        bool hit,
        long? durationMs = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(cacheKey);

        var level = hit ? LogLevel.Debug : LogLevel.Information;

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
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The entity type being validated.</param>
    /// <param name="errors">Dictionary of field errors.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/>, <paramref name="entityType"/>, or <paramref name="errors"/> is null.</exception>
    public static void LogValidationError(
        this ILogger logger,
        string entityType,
        Dictionary<string, string> errors)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(errors);

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
    /// <param name="logger">The logger instance.</param>
    /// <param name="webhookId">The webhook identifier.</param>
    /// <param name="url">The webhook URL.</param>
    /// <param name="retry">Current retry attempt.</param>
    /// <param name="maxRetries">Maximum retry attempts.</param>
    /// <param name="success">Whether the delivery succeeded.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/>, <paramref name="webhookId"/>, or <paramref name="url"/> is null.</exception>
    public static void LogWebhookDelivery(
        this ILogger logger,
        string webhookId,
        string url,
        int retry,
        int maxRetries,
        bool success = true)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(webhookId);
        ArgumentNullException.ThrowIfNull(url);

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
    /// <param name="logger">The logger instance.</param>
    /// <param name="jobName">The job name.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="itemsProcessed">Number of items processed.</param>
    /// <param name="success">Whether the job succeeded.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> or <paramref name="jobName"/> is null.</exception>
    public static void LogBackgroundJob(
        this ILogger logger,
        string jobName,
        long durationMs,
        int itemsProcessed = 0,
        bool success = true)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(jobName);

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
    /// <param name="logger">The logger instance.</param>
    /// <param name="componentName">The component being checked.</param>
    /// <param name="healthy">Whether the component is healthy.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="message">Optional health check message.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> or <paramref name="componentName"/> is null.</exception>
    public static void LogHealthCheck(
        this ILogger logger,
        string componentName,
        bool healthy,
        long durationMs,
        string message = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(componentName);

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
    /// <param name="logger">The logger instance.</param>
    /// <param name="configKey">The configuration key.</param>
    /// <param name="expectedValue">The expected value.</param>
    /// <param name="actualValue">The actual value (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/>, <paramref name="configKey"/>, or <paramref name="expectedValue"/> is null.</exception>
    public static void LogConfigurationError(
        this ILogger logger,
        string configKey,
        string expectedValue,
        string actualValue = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configKey);
        ArgumentNullException.ThrowIfNull(expectedValue);

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
public sealed class OperationContext : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly System.Diagnostics.Stopwatch _stopwatch;

    public OperationContext(ILogger logger, string operationName)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(operationName);

        _logger = logger;
        _operationName = operationName;
        _stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation("Operation started: {operation}", operationName);
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _logger.LogInformation("Operation completed: {operation} | Duration: {duration}ms",
            _operationName, _stopwatch.ElapsedMilliseconds);
    }
}