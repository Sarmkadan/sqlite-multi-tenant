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
    public string? MigrationId { get; }
    public string? MigrationVersion { get; }

    public MigrationException(string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(message));
    }

    public MigrationException(string message, Exception innerException)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(message));
        ArgumentNullException.ThrowIfNull(nameof(innerException));
    }

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
