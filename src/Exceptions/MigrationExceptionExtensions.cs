namespace SqliteMultiTenant.Exceptions;

public static class MigrationExceptionExtensions
{
    public static bool IsExecutionFailure(this MigrationException exception)
    {
        return exception.Message.Contains("execution failed", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsVersionAlreadyApplied(this MigrationException exception)
    {
        return exception.Message.Contains("already applied", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetMigrationDetails(this MigrationException exception)
    {
        return $"Migration ID: {exception.MigrationId}, Version: {exception.MigrationVersion}";
    }
}
