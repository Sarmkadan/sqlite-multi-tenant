#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Thrown when database access operations fail
/// </summary>
public sealed class DatabaseAccessException : MultiTenantException
{
    /// <summary>
    /// Gets the identifier of the database associated with the access exception, if available.
    /// </summary>
    public string? DatabaseId { get; }

    /// <summary>
    /// Gets the type of database operation that failed.
    /// </summary>
    public string? OperationType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseAccessException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DatabaseAccessException(string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseAccessException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DatabaseAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        ArgumentNullException.ThrowIfNull(innerException);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseAccessException"/> class with a specified error message,
    /// database identifier, operation type, and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="databaseId">The identifier of the database associated with the access exception.</param>
    /// <param name="operationType">The type of database operation that failed.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public DatabaseAccessException(string message, string databaseId, string operationType, Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        ArgumentException.ThrowIfNullOrEmpty(databaseId);
        ArgumentException.ThrowIfNullOrEmpty(operationType);
        DatabaseId = databaseId;
        OperationType = operationType;
    }

    public static DatabaseAccessException ConnectionFailed(string databaseId, Exception innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(databaseId);
        ArgumentNullException.ThrowIfNull(innerException);
        return new DatabaseAccessException(
            $"Failed to connect to database '{databaseId}'",
            databaseId,
            "Connection",
            innerException);
    }

    public static DatabaseAccessException QueryFailed(string databaseId, string query, Exception innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(databaseId);
        ArgumentException.ThrowIfNullOrEmpty(query);
        ArgumentNullException.ThrowIfNull(innerException);
        return new DatabaseAccessException(
            $"Query execution failed on database '{databaseId}': {query}",
            databaseId,
            "Query",
            innerException);
    }

    public static DatabaseAccessException TransactionFailed(string databaseId, Exception innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(databaseId);
        ArgumentNullException.ThrowIfNull(innerException);
        return new DatabaseAccessException(
            $"Transaction failed on database '{databaseId}'",
            databaseId,
            "Transaction",
            innerException);
    }

    public static DatabaseAccessException ReadOnlyViolation(string databaseId, string operation)
    {
        ArgumentException.ThrowIfNullOrEmpty(databaseId);
        ArgumentException.ThrowIfNullOrEmpty(operation);
        return new DatabaseAccessException(
            $"Database '{databaseId}' is in read-only mode. Write operation '{operation}' is not allowed.",
            databaseId,
            "WriteOperation",
            null);
    }
}