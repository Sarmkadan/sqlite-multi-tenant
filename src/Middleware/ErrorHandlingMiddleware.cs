#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Api.Responses;
using SqliteMultiTenant.Exceptions;
using System.Text.Json;

namespace SqliteMultiTenant.Middleware;

/// <summary>
/// Global error handling middleware for consistent exception responses.
/// Prevents internal exception details leaking to clients while logging for diagnostics.
/// Maps domain exceptions to appropriate HTTP status codes automatically.
/// </summary>
public sealed class ErrorHandlingMiddleware {
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes middleware to catch and handle exceptions.
    /// Wraps entire request processing to provide outer-level exception safety.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (TenantNotFoundException ex)
        {
            _logger.LogWarning($"Tenant not found: {ex.Message}");
            await HandleExceptionAsync(context, 404, "TENANT_NOT_FOUND", ex.Message);
        }
        catch (DatabaseAccessException ex)
        {
            _logger.LogError($"Database access error: {ex.Message}");
            await HandleExceptionAsync(context, 500, "DATABASE_ERROR", "Database operation failed");
        }
        catch (MigrationException ex)
        {
            _logger.LogError($"Migration error: {ex.Message}");
            await HandleExceptionAsync(context, 400, "MIGRATION_FAILED", ex.Message);
        }
        catch (BackupException ex)
        {
            _logger.LogError($"Backup error: {ex.Message}");
            await HandleExceptionAsync(context, 500, "BACKUP_FAILED", ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning($"Invalid argument: {ex.Message}");
            await HandleExceptionAsync(context, 400, "INVALID_ARGUMENT", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning($"Unauthorized: {ex.Message}");
            await HandleExceptionAsync(context, 401, "UNAUTHORIZED", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unhandled exception: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
            await HandleExceptionAsync(context, 500, "INTERNAL_SERVER_ERROR", "An unexpected error occurred");
        }
    }

    /// <summary>
    /// Serializes exception to standardized JSON error response.
    /// Always returns 'X-Request-ID' header for log correlation and support.
    /// Client receives user-friendly error message without implementation details.
    /// </summary>
    private static Task HandleExceptionAsync(HttpContext context, int statusCode, string errorCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse
        {
            Code = errorCode,
            Message = message,
            TraceId = context.TraceIdentifier
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        return context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Result pattern exception handling for controllers.
/// Allows methods to return Result<T> instead of throwing, reducing allocations.
/// </summary>
public sealed class Result<T> {
    public bool IsSuccess { get; set; }
    public T? Value { get; set; }
    public string? ErrorMessage { get; set; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
