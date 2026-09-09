#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Thrown when migration operations fail
/// </summary>
public sealed class MigrationException : MultiTenantException
{
    /// <summary>
    /// Gets the migration identifier.
    /// </summary>
    public string? MigrationId { get; }

    /// <summary>
    /// Gets the migration version.
    /// </summary>
    public string? MigrationVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MigrationException(string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(message));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MigrationException(string message, Exception innerException)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(message));
        ArgumentNullException.ThrowIfNull(nameof(innerException));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationException"/> class with specified error message, migration ID, version, and optional inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="migrationId">The identifier of the migration.</param>
    /// <param name="version">The version of the migration.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MigrationException(string message, string migrationId, string? version, Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(message));
        ArgumentException.ThrowIfNullOrEmpty(nameof(migrationId));
        ArgumentException.ThrowIfNullOrEmpty(nameof(version));
        MigrationId = migrationId;
        MigrationVersion = version;
    }

    public static MigrationException ExecutionFailed(string migrationId, string version, Exception innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(migrationId));
        ArgumentException.ThrowIfNullOrEmpty(nameof(version));
        ArgumentNullException.ThrowIfNull(nameof(innerException));
        return new MigrationException(
            $"Migration '{version}' (ID: {migrationId}) failed to execute",
            migrationId,
            version,
            innerException);
    }

    public static MigrationException RollbackFailed(string migrationId, string version, Exception innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(migrationId));
        ArgumentException.ThrowIfNullOrEmpty(nameof(version));
        ArgumentNullException.ThrowIfNull(nameof(innerException));
        return new MigrationException(
            $"Migration '{version}' (ID: {migrationId}) failed to rollback",
            migrationId,
            version,
            innerException);
    }

    public static MigrationException NotFound(string migrationId)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(migrationId));
        return new MigrationException($"Migration with ID '{migrationId}' was not found", migrationId, null);
    }

    public static MigrationException AlreadyApplied(string version)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(version));
        return new MigrationException($"Migration '{version}' has already been applied");
    }
}
