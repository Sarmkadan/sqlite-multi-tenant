#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Api.Responses;
using SqliteMultiTenant.Exceptions;

namespace SqliteMultiTenant.Middleware;

/// <summary>
/// Global error handling middleware for consistent exception responses.
/// Prevents internal exception details leaking to clients while logging for diagnostics.
/// Maps domain exceptions to appropriate HTTP status codes automatically.
/// </summary>
public sealed class ErrorHandlingMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="logger">Logger used to record handled exceptions.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
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
            _logger.LogWarning("Tenant not found: {Message} | TenantId: {TenantId}", ex.Message, ex.TenantId);
            await HandleExceptionAsync(context, 404, "TENANT_NOT_FOUND", ex.Message);
        }
        catch (DatabaseAccessException ex)
        {
            _logger.LogError("Database access error: {Message}", ex.Message);
            await HandleExceptionAsync(context, 500, "DATABASE_ERROR", "Database operation failed");
        }
        catch (MigrationException ex)
        {
            _logger.LogError("Migration error: {Message}", ex.Message);
            await HandleExceptionAsync(context, 400, "MIGRATION_FAILED", ex.Message);
        }
        catch (BackupException ex)
        {
            _logger.LogError("Backup error: {Message}", ex.Message);
            await HandleExceptionAsync(context, 500, "BACKUP_FAILED", ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument: {Message}", ex.Message);
            await HandleExceptionAsync(context, 400, "INVALID_ARGUMENT", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized: {Message}", ex.Message);
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

        var response = new SqliteMultiTenant.Api.Responses.ErrorResponse
        {
            Code = errorCode,
            Message = message,
            TraceId = context.TraceIdentifier
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        return context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Returns a short string representation of the middleware for diagnostics.
    /// </summary>
    /// <returns>A string describing the middleware instance.</returns>
    public override string ToString() => "ErrorHandlingMiddleware { _logger = <ILogger> }";
}

/// <summary>
/// Result pattern exception handling for controllers.
/// Allows methods to return Result&lt;T&gt; instead of throwing, reducing allocations.
/// </summary>
public sealed class Result<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the result value when the operation succeeded.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Gets or sets the error message when the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result carrying the specified value.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A result with <see cref="IsSuccess"/> set to true.</returns>
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };

    /// <summary>
    /// Creates a failed result carrying the specified error message.
    /// </summary>
    /// <param name="errorMessage">The description of the failure.</param>
    /// <returns>A result with <see cref="IsSuccess"/> set to false.</returns>
    public static Result<T> Failure(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };

    /// <summary>
    /// Returns a string representation of the result for diagnostics.
    /// </summary>
    /// <returns>A string containing the success flag, value, and error message.</returns>
    public override string ToString()
    {
        return $"Result<T> {{ IsSuccess = {IsSuccess}, Value = {Value}, ErrorMessage = {ErrorMessage} }}";
    }
}
