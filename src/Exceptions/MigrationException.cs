// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Thrown when migration operations fail
/// </summary>
public class MigrationException : Exception
{
    public string? MigrationId { get; }
    public string? MigrationVersion { get; }

    public MigrationException(string message)
        : base(message)
    {
    }

    public MigrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public MigrationException(string message, string migrationId, string? version, Exception? innerException = null)
        : base(message, innerException)
    {
        MigrationId = migrationId;
        MigrationVersion = version;
    }

    public static MigrationException ExecutionFailed(string migrationId, string version, Exception innerException)
    {
        return new MigrationException(
            $"Migration '{version}' (ID: {migrationId}) failed to execute",
            migrationId,
            version,
            innerException);
    }

    public static MigrationException RollbackFailed(string migrationId, string version, Exception innerException)
    {
        return new MigrationException(
            $"Migration '{version}' (ID: {migrationId}) failed to rollback",
            migrationId,
            version,
            innerException);
    }

    public static MigrationException NotFound(string migrationId)
    {
        return new MigrationException($"Migration with ID '{migrationId}' was not found", migrationId, null);
    }

    public static MigrationException AlreadyApplied(string version)
    {
        return new MigrationException($"Migration '{version}' has already been applied");
    }
}
