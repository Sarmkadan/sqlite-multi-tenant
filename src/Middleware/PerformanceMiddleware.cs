#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SqliteMultiTenant.Middleware;

/// <summary>
/// Middleware that measures request/response performance and logs slow requests.
/// Tracks elapsed time, memory usage, and logs warnings for operations exceeding threshold.
/// Useful for identifying performance bottlenecks in the system.
/// </summary>
public sealed class PerformanceMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMiddleware> _logger;
    private readonly long _slowRequestThresholdMs;
    private readonly PerformanceMonitor _monitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="slowRequestThresholdMs">The threshold in milliseconds for a request to be considered slow. Defaults to 1000ms.</param>
    public PerformanceMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMiddleware> logger,
        long slowRequestThresholdMs = 1000)
    {
            ArgumentNullException.ThrowIfNull(next);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(next);
ArgumentNullException.ThrowIfNull(logger);
_next = next;
            _logger = logger;
            _slowRequestThresholdMs = slowRequestThresholdMs;
            _monitor = new PerformanceMonitor();
    }

    /// <summary>
    /// Processes an HTTP request to measure its performance.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var memoryBefore = GC.GetTotalMemory(false);
        var originalBodyStream = context.Response.Body;

        try
        {
            // Replace response body to capture response details
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                await _next(context);

                // Copy response back to original
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
            stopwatch.Stop();

            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsedKb = (memoryAfter - memoryBefore) / 1024;

            var requestMetrics = new RequestMetrics
            {
                Method = context.Request.Method,
                Path = context.Request.Path.ToString(),
                StatusCode = context.Response.StatusCode,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                MemoryUsedKb = memoryUsedKb,
                Timestamp = DateTime.UtcNow
            };

            // Log metrics
            LogRequestMetrics(requestMetrics);

            // Store in items for later retrieval
            context.Items["RequestMetrics"] = requestMetrics;

            // Add performance headers to response
            context.Response.Headers.Add("X-Response-Time-Ms", stopwatch.ElapsedMilliseconds.ToString());
            context.Response.Headers.Add("X-Memory-Used-Kb", memoryUsedKb.ToString());
        }
    }

    private void LogRequestMetrics(RequestMetrics metrics)
    {
        if (metrics.ElapsedMs > _slowRequestThresholdMs)
        {
            _logger.LogWarning(
                $"Slow request detected: {metrics.Method} {metrics.Path} " +
                $"took {metrics.ElapsedMs}ms (Status: {metrics.StatusCode}, " +
                $"Memory: {metrics.MemoryUsedKb}KB)");
        }
        else
        {
            _logger.LogInformation(
                $"Request: {metrics.Method} {metrics.Path} " +
                $"completed in {metrics.ElapsedMs}ms " +
                $"(Status: {metrics.StatusCode})");
        }
    }
}

/// <summary>
/// Contains metrics for a single HTTP request.
/// </summary>
public sealed class RequestMetrics {
    /// <summary>
    /// Gets or sets the HTTP method of the request.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path of the request.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the elapsed time of the request in milliseconds.
    /// </summary>
    public long ElapsedMs { get; set; }

    /// <summary>
    /// Gets or sets the memory used by the request in kilobytes.
    /// </summary>
    public long MemoryUsedKb { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the request was processed.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Tracks overall system performance statistics
/// </summary>
public sealed class PerformanceMonitor {
    private readonly List<RequestMetrics> _metrics = new();
    private readonly SemaphoreSlim _semaphore = new(1);
    private const int MaxMetricsStored = 1000;

    public async Task RecordMetricAsync(RequestMetrics metric)
    {
        await _semaphore.WaitAsync();
        try
        {
            _metrics.Add(metric);

            // Keep only recent metrics
            if (_metrics.Count > MaxMetricsStored)
                _metrics.RemoveRange(0, _metrics.Count - MaxMetricsStored);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<PerformanceStats> GetStatsAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_metrics.Count == 0)
                return new PerformanceStats();

            return new PerformanceStats
            {
                TotalRequests = _metrics.Count,
                AverageElapsedMs = _metrics.Average(m => m.ElapsedMs),
                MaxElapsedMs = _metrics.Max(m => m.ElapsedMs),
                MinElapsedMs = _metrics.Min(m => m.ElapsedMs),
                AverageMemoryUsedKb = _metrics.Average(m => m.MemoryUsedKb),
                ErrorCount = _metrics.Count(m => m.StatusCode >= 400)
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<RequestMetrics>> GetRecentMetricsAsync(int count = 10)
    {
        await _semaphore.WaitAsync();
        try
        {
            return _metrics.TakeLast(count).ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

public sealed class PerformanceStats {
    public int TotalRequests { get; set; }
    public double AverageElapsedMs { get; set; }
    public long MaxElapsedMs { get; set; }
    public long MinElapsedMs { get; set; }
    public double AverageMemoryUsedKb { get; set; }
    public int ErrorCount { get; set; }
}

/// <summary>
/// Extension methods for PerformanceMiddleware.
/// </summary>
public static class PerformanceMiddlewareExtensions
{
    /// <summary>
    /// Adds the PerformanceMiddleware to the pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="slowRequestThresholdMs">The threshold in milliseconds for a request to be considered slow. Defaults to 1000ms.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UsePerformanceTracking(
        this IApplicationBuilder app,
        long slowRequestThresholdMs = 1000)
    {
        return app.UseMiddleware<PerformanceMiddleware>(slowRequestThresholdMs);
    }

    /// <summary>
    /// Retrieves request metrics from HTTP context
    /// </summary>
    public static RequestMetrics? GetRequestMetrics(this HttpContext context)
    {
        if (context.Items.TryGetValue("RequestMetrics", out var metrics))
            return metrics as RequestMetrics;

        return null;
    }
}
