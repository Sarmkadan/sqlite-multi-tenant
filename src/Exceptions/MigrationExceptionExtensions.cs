namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Provides extension methods for <see cref="MigrationException"/> to facilitate exception analysis and handling.
/// </summary>
public static class MigrationExceptionExtensions
{
    /// <summary>
    /// Determines whether the exception represents an execution failure.
    /// </summary>
    /// <param name="exception">The exception to analyze. Cannot be null.</param>
    /// <returns>True if the exception message indicates an execution failure; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static bool IsExecutionFailure(this MigrationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.Message.Contains("execution failed", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the exception represents a migration that was already applied.
    /// </summary>
    /// <param name="exception">The exception to analyze. Cannot be null.</param>
    /// <returns>True if the exception message indicates the migration was already applied; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static bool IsVersionAlreadyApplied(this MigrationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.Message.Contains("already applied", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a formatted string containing migration details including ID and version.
    /// </summary>
    /// <param name="exception">The exception containing migration details. Cannot be null.</param>
    /// <returns>A formatted string with migration details.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static string GetMigrationDetails(this MigrationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"Migration ID: {exception.MigrationId ?? "null"}, Version: {exception.MigrationVersion ?? "null"}";
    }
}
