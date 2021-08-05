// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Centralized exception processing and error handling.
/// Converts exceptions to user-friendly error responses.
/// Handles logging, categorization, and HTTP status code mapping.
/// </summary>
public interface IExceptionProcessor
{
    ErrorResponse ProcessException(Exception exception);
    int GetHttpStatusCode(Exception exception);
    string GetErrorCategory(Exception exception);
}

public class ExceptionProcessor : IExceptionProcessor
{
    private readonly ILogger<ExceptionProcessor> _logger;

    public ExceptionProcessor(ILogger<ExceptionProcessor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Processes an exception and creates a user-friendly error response.
    /// </summary>
    public ErrorResponse ProcessException(Exception exception)
    {
        try
        {
            var category = GetErrorCategory(exception);
            var statusCode = GetHttpStatusCode(exception);
            var message = GetUserFriendlyMessage(exception);
            var errorId = Guid.NewGuid().ToString();

            _logger.LogError(
                $"Exception processed: {category}, StatusCode: {statusCode}, ErrorId: {errorId}, " +
                $"Exception: {exception.GetType().Name}, Message: {exception.Message}");

            return new ErrorResponse
            {
                ErrorId = errorId,
                Category = category,
                Message = message,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow,
                Details = GetErrorDetails(exception),
                InnerException = exception.InnerException != null
                    ? new ErrorResponse
                    {
                        ErrorId = Guid.NewGuid().ToString(),
                        Category = "InnerException",
                        Message = exception.InnerException.Message,
                        Timestamp = DateTime.UtcNow
                    }
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing exception: {ex.Message}");
            return new ErrorResponse
            {
                ErrorId = Guid.NewGuid().ToString(),
                Category = "UnexpectedError",
                Message = "An unexpected error occurred",
                StatusCode = 500,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Maps exception types to HTTP status codes.
    /// </summary>
    public int GetHttpStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException or ArgumentNullException => 400,
            KeyNotFoundException => 404,
            UnauthorizedAccessException => 401,
            InvalidOperationException => 409,
            TimeoutException => 408,
            TenantNotFoundException => 404,
            DatabaseAccessException => 500,
            MigrationException => 400,
            BackupException => 500,
            _ => 500
        };
    }

    /// <summary>
    /// Categorizes exceptions by type.
    /// </summary>
    public string GetErrorCategory(Exception exception)
    {
        return exception switch
        {
            ArgumentException or ArgumentNullException => "ValidationError",
            KeyNotFoundException => "NotFound",
            UnauthorizedAccessException => "Unauthorized",
            InvalidOperationException => "InvalidOperation",
            TimeoutException => "Timeout",
            TenantNotFoundException => "TenantNotFound",
            DatabaseAccessException => "DatabaseError",
            MigrationException => "MigrationError",
            BackupException => "BackupError",
            _ => "UnexpectedError"
        };
    }

    private string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            TenantNotFoundException ex => $"Tenant not found: {ex.Message}",
            DatabaseAccessException ex => "Unable to access database. Please try again later.",
            MigrationException ex => $"Migration error: {ex.Message}",
            BackupException ex => $"Backup error: {ex.Message}",
            ArgumentException or ArgumentNullException => exception.Message,
            TimeoutException => "The operation timed out. Please try again.",
            UnauthorizedAccessException => "You do not have permission to access this resource.",
            InvalidOperationException => "The operation is not valid at this time.",
            _ => "An unexpected error occurred. Please try again later."
        };
    }

    private Dictionary<string, object> GetErrorDetails(Exception exception)
    {
        return new Dictionary<string, object>
        {
            { "ExceptionType", exception.GetType().Name },
            { "Message", exception.Message },
            { "Source", exception.Source ?? "Unknown" },
            { "StackTrace", exception.StackTrace ?? "Not available" }
        };
    }
}

public class ErrorResponse
{
    public string ErrorId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
    public ErrorResponse? InnerException { get; set; }
}

/// <summary>
/// Extension methods for exception handling.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Gets the full error message including inner exceptions.
    /// </summary>
    public static string GetFullMessage(this Exception exception)
    {
        var messages = new List<string>();
        var current = exception;

        while (current != null)
        {
            messages.Add(current.Message);
            current = current.InnerException;
        }

        return string.Join(" -> ", messages);
    }

    /// <summary>
    /// Logs exception with context information.
    /// </summary>
    public static void LogWithContext(this Exception exception, ILogger logger, string context)
    {
        logger.LogError(
            $"Exception in {context}: {exception.GetType().Name} - {exception.GetFullMessage()}\n" +
            $"StackTrace: {exception.StackTrace}");
    }

    /// <summary>
    /// Checks if exception is transient (can be retried).
    /// </summary>
    public static bool IsTransient(this Exception exception)
    {
        return exception switch
        {
            TimeoutException or IOException => true,
            _ => false
        };
    }
}
