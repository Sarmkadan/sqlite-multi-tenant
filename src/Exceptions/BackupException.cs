#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Thrown when backup operations fail
/// </summary>
public sealed class BackupException : Exception {
    public string? BackupId { get; }
    public string? DatabaseId { get; }

    public BackupException(string message)
        : base(message)
    {
    }

    public BackupException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public BackupException(string message, string backupId, string databaseId, Exception? innerException = null)
        : base(message, innerException)
    {
        BackupId = backupId;
        DatabaseId = databaseId;
    }

    public static BackupException CreationFailed(string databaseId, Exception innerException)
    {
        return new BackupException(
            $"Failed to create backup for database '{databaseId}'",
            string.Empty,
            databaseId,
            innerException);
    }

    public static BackupException VerificationFailed(string backupId, string databaseId, Exception? innerException = null)
    {
        return new BackupException(
            $"Backup verification failed for backup '{backupId}'",
            backupId,
            databaseId,
            innerException);
    }

    public static BackupException RestoreFailed(string backupId, string databaseId, Exception innerException)
    {
        return new BackupException(
            $"Failed to restore backup '{backupId}' to database '{databaseId}'",
            backupId,
            databaseId,
            innerException);
    }

    public static BackupException NotFound(string backupId)
    {
        return new BackupException($"Backup with ID '{backupId}' was not found");
    }
}
