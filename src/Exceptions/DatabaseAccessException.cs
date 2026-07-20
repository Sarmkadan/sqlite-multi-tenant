#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Thrown when database access operations fail
/// </summary>
public sealed class DatabaseAccessException : Exception
{
    public string? DatabaseId { get; }
    public string? OperationType { get; }

    public DatabaseAccessException(string message)
        : base(message) { }

    public DatabaseAccessException(string message, Exception innerException)
        : base(message, innerException) { }

    public DatabaseAccessException(string message, string databaseId, string operationType, Exception? innerException = null)
        : base(message, innerException)
    {
        DatabaseId = databaseId;
        OperationType = operationType;
    }

    public static DatabaseAccessException ConnectionFailed(string databaseId, Exception innerException)
    {
        return new DatabaseAccessException(
            $"Failed to connect to database '{databaseId}'",
            databaseId,
            "Connection",
            innerException);
    }

    public static DatabaseAccessException QueryFailed(string databaseId, string query, Exception innerException)
    {
        return new DatabaseAccessException(
            $"Query execution failed on database '{databaseId}': {query}",
            databaseId,
            "Query",
            innerException);
    }

    public static DatabaseAccessException TransactionFailed(string databaseId, Exception innerException)
    {
        return new DatabaseAccessException(
            $"Transaction failed on database '{databaseId}'",
            databaseId,
            "Transaction",
            innerException);
    }

    public static DatabaseAccessException ReadOnlyViolation(string databaseId, string operation)
    {
        return new DatabaseAccessException(
            $"Database '{databaseId}' is in read-only mode. Write operation '{operation}' is not allowed.",
            databaseId,
            "WriteOperation",
            null);
    }
}