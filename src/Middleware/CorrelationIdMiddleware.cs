#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SqliteMultiTenant.Middleware;

/// <summary>
/// Middleware that adds correlation IDs to requests for distributed tracing.
/// Generates unique ID if not present in headers, and includes it in responses.
/// Enables request tracking across multiple services and logs.
/// </summary>
public sealed class CorrelationIdMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get correlation ID from request header or generate new one
        string correlationId = GetOrGenerateCorrelationId(context);

        // Store in context items for later retrieval
        context.Items[CorrelationIdHeader] = correlationId;

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
                context.Response.Headers.Add(CorrelationIdHeader, correlationId);

            return Task.CompletedTask;
        });

        // Log request with correlation ID
        _logger.LogInformation(
            $"[{correlationId}] {context.Request.Method} {context.Request.Path}");

        await _next(context);

        // Log response with correlation ID
        _logger.LogInformation(
            $"[{correlationId}] Response: {context.Response.StatusCode}");
    }

    private string GetOrGenerateCorrelationId(HttpContext context)
    {
        // Try to get from request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValues))
        {
            var correlationId = headerValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(correlationId))
                return correlationId;
        }

        // Try to get from query string
        if (context.Request.Query.TryGetValue("correlationId", out var queryValues))
        {
            var correlationId = queryValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(correlationId))
                return correlationId;
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>
/// Extension method to register CorrelationIdMiddleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Retrieves correlation ID from current HTTP context
    /// </summary>
    public static string GetCorrelationId(this HttpContext context)
    {
        if (context.Items.TryGetValue("X-Correlation-Id", out var correlationId))
            return correlationId?.ToString() ?? string.Empty;

        return string.Empty;
    }
}
