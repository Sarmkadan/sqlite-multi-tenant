#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Extension methods for <see cref="DatabaseAccessException"/> to provide additional functionality
/// </summary>
public static class DatabaseAccessExceptionExtensions
{
    /// <summary>
    /// Creates a new <see cref="DatabaseAccessException"/> with the same database context but a different message
    /// </summary>
    /// <param name="exception">The original exception</param>
    /// <param name="newMessage">The new error message</param>
    /// <returns>A new exception with the updated message</returns>
    public static DatabaseAccessException WithMessage(this DatabaseAccessException exception, string newMessage)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(newMessage);

        return new DatabaseAccessException(
            newMessage,
            exception.DatabaseId,
            exception.OperationType ?? "Unknown",
            exception.InnerException);
    }

    /// <summary>
    /// Creates a new <see cref="DatabaseAccessException"/> with additional context information appended to the message
    /// </summary>
    /// <param name="exception">The original exception</param>
    /// <param name="contextInfo">Additional context to append to the message</param>
    /// <returns>A new exception with enhanced message</returns>
    public static DatabaseAccessException WithContext(this DatabaseAccessException exception, string contextInfo)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(contextInfo);

        var enhancedMessage = exception.Message + " | Context: " + contextInfo;
        return new DatabaseAccessException(
            enhancedMessage,
            exception.DatabaseId,
            exception.OperationType ?? "Unknown",
            exception.InnerException);
    }

    /// <summary>
    /// Determines if the exception represents a connection failure
    /// </summary>
    /// <param name="exception">The exception to check</param>
    /// <returns>True if the operation type is 'Connection'</returns>
    public static bool IsConnectionFailure(this DatabaseAccessException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return string.Equals(exception.OperationType, "Connection", StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines if the exception represents a query failure
    /// </summary>
    /// <param name="exception">The exception to check</param>
    /// <returns>True if the operation type is 'Query'</returns>
    public static bool IsQueryFailure(this DatabaseAccessException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return string.Equals(exception.OperationType, "Query", StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines if the exception represents a transaction failure
    /// </summary>
    /// <param name="exception">The exception to check</param>
    /// <returns>True if the operation type is 'Transaction'</returns>
    public static bool IsTransactionFailure(this DatabaseAccessException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return string.Equals(exception.OperationType, "Transaction", StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets a formatted string representation of the exception including database context
    /// </summary>
    /// <param name="exception">The exception</param>
    /// <returns>Formatted string with all relevant information</returns>
    public static string ToDetailedString(this DatabaseAccessException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var builder = new System.Text.StringBuilder();
        builder.Append("DatabaseAccessException: ");
        builder.Append(exception.Message);

        if (!string.IsNullOrEmpty(exception.DatabaseId))
        {
            builder.Append("\nDatabase: ");
            builder.Append(exception.DatabaseId);
        }

        if (!string.IsNullOrEmpty(exception.OperationType))
        {
            builder.Append("\nOperation: ");
            builder.Append(exception.OperationType);
        }

        if (exception.InnerException != null)
        {
            builder.Append("\nInnerException: ");
            builder.Append(exception.InnerException.GetType().Name);
            builder.Append(": ");
            builder.Append(exception.InnerException.Message);
        }

        return builder.ToString();
    }
}