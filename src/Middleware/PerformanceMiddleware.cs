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

public sealed class RequestMetrics {
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long ElapsedMs { get; set; }
    public long MemoryUsedKb { get; set; }
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

public static class PerformanceMiddlewareExtensions
{
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
