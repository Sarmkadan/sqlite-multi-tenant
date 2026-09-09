#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Thrown when backup operations fail
/// </summary>
public sealed class BackupException : MultiTenantException
{
    /// <summary>
    /// Gets the identifier of the backup associated with the failure.
    /// </summary>
    public string? BackupId { get; }

    /// <summary>
    /// Gets the identifier of the database associated with the backup operation.
    /// </summary>
    public string? DatabaseId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BackupException(string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public BackupException(string message, Exception innerException)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupException"/> class with backup and database context.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="backupId">The identifier of the backup associated with the failure.</param>
    /// <param name="databaseId">The identifier of the database associated with the backup operation.</param>
    /// <param name="innerException">The exception that caused the current exception, or <see langword="null"/>.</param>
    public BackupException(string message, string backupId, string databaseId, Exception? innerException = null)
        : base(message, innerException)
    {
        BackupId = backupId;
        DatabaseId = databaseId;
    }

    public static BackupException CreationFailed(string databaseId, Exception innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(databaseId);
        return new BackupException(
            $"Failed to create backup for database '{databaseId}'",
            string.Empty,
            databaseId,
            innerException);
    }

    public static BackupException VerificationFailed(string backupId, string databaseId, Exception? innerException = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(backupId);
        ArgumentException.ThrowIfNullOrEmpty(databaseId);
        return new BackupException(
            $"Backup verification failed for backup '{backupId}'",
            backupId,
            databaseId,
            innerException);
    }

    public static BackupException RestoreFailed(string backupId, string databaseId, Exception innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(backupId);
        ArgumentException.ThrowIfNullOrEmpty(databaseId);
        return new BackupException(
            $"Failed to restore backup '{backupId}' to database '{databaseId}'",
            backupId,
            databaseId,
            innerException);
    }

    public static BackupException NotFound(string backupId)
    {
        ArgumentException.ThrowIfNullOrEmpty(backupId);
        return new BackupException($"Backup with ID '{backupId}' was not found");
    }
}
