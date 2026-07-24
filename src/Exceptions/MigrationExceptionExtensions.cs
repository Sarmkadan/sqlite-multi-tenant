namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Provides extension methods for <see cref="MigrationException"/> to facilitate exception analysis and handling.
/// These methods enable checking specific migration failure conditions and extracting migration metadata.
/// </summary>
public static class MigrationExceptionExtensions
{
    /// <summary>
    /// Determines whether the exception represents an execution failure.
    /// </summary>
    /// <param name="exception">The exception to analyze. Cannot be null.</param>
    /// <returns>True if the exception message contains "execution failed" (case-insensitive); otherwise, false.</returns>
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
    /// <returns>True if the exception message contains "already applied" (case-insensitive); otherwise, false.</returns>
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
    /// <returns>
    /// A string in the format "Migration ID: {MigrationId}, Version: {MigrationVersion}".
    /// Null values for <see cref="MigrationException.MigrationId"/> or <see cref="MigrationException.MigrationVersion"/>
    /// are represented as "null" in the output.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static string GetMigrationDetails(this MigrationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"Migration ID: {exception.MigrationId ?? "null"}, Version: {exception.MigrationVersion ?? "null"}";
    }

    /// <summary>
    /// Creates a migration failure record from a MigrationException.
    /// </summary>
    /// <param name="exception">The exception containing migration details. Cannot be null.</param>
    /// <returns>A MigrationFailure record with details from the exception.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static Models.MigrationFailure ToMigrationFailure(this MigrationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return Models.MigrationFailure.Create(
            migrationId: exception.MigrationId ?? "unknown",
            version: exception.MigrationVersion ?? "unknown",
            name: exception.Message,
            errorMessage: exception.Message,
            exception: exception
        );
    }

    /// <summary>
    /// Determines whether the exception represents a database constraint violation.
    /// </summary>
    /// <param name="exception">The exception to analyze. Cannot be null.</param>
    /// <returns>True if the exception message contains "constraint" or "violation" (case-insensitive); otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static bool IsConstraintViolation(this MigrationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var message = exception.Message + (exception.InnerException?.Message ?? "");
        return message.Contains("constraint", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("violation", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the exception represents a duplicate key or already exists error.
    /// </summary>
    /// <param name="exception">The exception to analyze. Cannot be null.</param>
    /// <returns>True if the exception message contains "already exists" or "duplicate" (case-insensitive); otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static bool IsDuplicateKeyError(this MigrationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var message = exception.Message + (exception.InnerException?.Message ?? "");
        return message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the exception represents a timeout or deadlock error.
    /// </summary>
    /// <param name="exception">The exception to analyze. Cannot be null.</param>
    /// <returns>True if the exception message contains "timeout" or "deadlock" (case-insensitive); otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static bool IsTimeoutOrDeadlock(this MigrationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var message = exception.Message + (exception.InnerException?.Message ?? "");
        return message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("deadlock", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a formatted error summary for the migration failure.
    /// </summary>
    /// <param name="exception">The exception containing migration details. Cannot be null.</param>
    /// <returns>A user-friendly error summary.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static string GetErrorSummary(this MigrationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.IsVersionAlreadyApplied())
            return "This migration has already been applied to the database";

        if (exception.IsConstraintViolation())
            return "Database constraint violation detected - check your schema changes";

        if (exception.IsDuplicateKeyError())
            return "Duplicate key or already exists error - the object may already be in the database";

        if (exception.IsTimeoutOrDeadlock())
            return "Database operation timed out or deadlock detected - retry the migration";

        if (exception.IsExecutionFailure())
            return "Migration execution failed - check the database logs for details";

        return "Migration failed - check the exception details for more information";
    }
}
