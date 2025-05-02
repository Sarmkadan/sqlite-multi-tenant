// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SqliteMultiTenant.Middleware;

/// <summary>
/// Request/response logging middleware for audit and performance monitoring.
/// Logs all HTTP requests with timing, status codes, and user context.
/// Enables tracking user actions and identifying performance bottlenecks.
/// Implements structured logging for ELK/Datadog integration.
/// </summary>
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Logs incoming request details and outgoing response metrics.
    /// Captures request body and response status for audit trail.
    /// Uses stopwatch to measure request duration for SLA monitoring.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;

        var request = context.Request;
        var requestLog = new
        {
            RequestId = requestId,
            Timestamp = DateTime.UtcNow,
            Method = request.Method,
            Path = request.Path.Value,
            QueryString = request.QueryString.Value,
            RemoteIP = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = request.Headers.UserAgent.ToString(),
            ContentType = request.ContentType
        };

        _logger.LogInformation(
            "HTTP Request: {Method} {Path} | RequestId: {RequestId} | RemoteIP: {IP}",
            request.Method,
            request.Path,
            requestId,
            context.Connection.RemoteIpAddress);

        var originalBodyStream = context.Response.Body;

        try
        {
            // Capture response body for logging critical operations
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                await _next(context);

                stopwatch.Stop();

                var responseLog = new
                {
                    RequestId = requestId,
                    StatusCode = context.Response.StatusCode,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    ResponseContentType = context.Response.ContentType
                };

                // Log slow requests (> 5 seconds)
                if (stopwatch.ElapsedMilliseconds > 5000)
                {
                    _logger.LogWarning(
                        "Slow HTTP Response: {Method} {Path} | StatusCode: {StatusCode} | Duration: {Duration}ms",
                        request.Method,
                        request.Path,
                        context.Response.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }
                else if (context.Response.StatusCode >= 400)
                {
                    _logger.LogWarning(
                        "HTTP Response: {Method} {Path} | StatusCode: {StatusCode} | Duration: {Duration}ms",
                        request.Method,
                        request.Path,
                        context.Response.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "HTTP Response: {Method} {Path} | StatusCode: {StatusCode} | Duration: {Duration}ms",
                        request.Method,
                        request.Path,
                        context.Response.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                // Copy response body back to original stream
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "HTTP Request Error: {Method} {Path} | RequestId: {RequestId} | Duration: {Duration}ms",
                request.Method,
                request.Path,
                requestId,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}
