#nullable enable

using System.Text;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Represents the result of a migration operation.
/// Contains success status, count of applied migrations, and any error information.
/// </summary>
public sealed record MigrationResult
{
    /// <summary>
    /// Indicates whether the migration operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The number of migrations that were successfully applied.
    /// </summary>
    public int AppliedCount { get; init; }

    /// <summary>
    /// Error message if the migration failed, otherwise null.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Indicates whether the migration operation completed successfully.
    /// </summary>
    public bool IsSuccess => Success && string.IsNullOrEmpty(Error);

    /// <summary>
    /// Gets a human-readable summary of the migration result.
    /// </summary>
    public string ResultSummary
    {
        get
        {
            if (IsSuccess)
            {
                return Success ? $"Success: {AppliedCount} migration(s) applied" : "Success";
            }

            var sb = new StringBuilder();
            sb.Append("Failed");
            if (!string.IsNullOrEmpty(Error))
            {
                sb.Append($": {Error}");
            }
            else if (!Success)
            {
                sb.Append(": Unknown error occurred");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Creates a successful migration result with the specified applied count.
    /// </summary>
    public static MigrationResult SuccessResult(int appliedCount = 0)
    {
        return new MigrationResult
        {
            Success = true,
            AppliedCount = appliedCount,
            Error = null
        };
    }

    /// <summary>
    /// Creates a failed migration result with the specified error message.
    /// </summary>
    public static MigrationResult FailureResult(string error)
    {
        return new MigrationResult
        {
            Success = false,
            AppliedCount = 0,
            Error = error
        };
    }
}