namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Provides extension methods for <see cref="BackupException"/> to facilitate error classification and diagnostics.
/// </summary>
public static class BackupExceptionExtensions
{
    /// <summary>
    /// Determines whether the exception represents a backup creation failure.
    /// </summary>
    /// <param name="ex">The backup exception to analyze. Must not be null.</param>
    /// <returns>True if the exception message indicates a creation failure; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is null.</exception>
    public static bool IsCreationFailure(this BackupException ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        return ex.Message.Contains("creation", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the exception represents a backup verification failure.
    /// </summary>
    /// <param name="ex">The backup exception to analyze. Must not be null.</param>
    /// <returns>True if the exception message indicates a verification failure; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is null.</exception>
    public static bool IsVerificationFailure(this BackupException ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        return ex.Message.Contains("verification", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the exception represents a backup restore failure.
    /// </summary>
    /// <param name="ex">The backup exception to analyze. Must not be null.</param>
    /// <returns>True if the exception message indicates a restore failure; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is null.</exception>
    public static bool IsRestoreFailure(this BackupException ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        return ex.Message.Contains("restore", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Formats detailed error information from the backup exception.
    /// </summary>
    /// <param name="ex">The backup exception containing error details. Must not be null.</param>
    /// <returns>A formatted string containing backup ID, database ID, and error message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is null.</exception>
    public static string GetErrorDetails(this BackupException ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        return $"BackupId: {ex.BackupId}, DatabaseId: {ex.DatabaseId}, Message: {ex.Message}";
    }
}
