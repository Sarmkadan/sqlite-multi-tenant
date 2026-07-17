#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Extension methods for the <see cref="Migration"/> class providing additional functionality
/// for querying and analyzing migration states, durations, and statistics.
/// </summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Determines if the migration is in a terminal state (Completed, Failed, or RolledBack)
    /// </summary>
    /// <param name="migration">The migration instance</param>
    /// <returns>True if the migration is in a terminal state; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="migration"/> is null</exception>
    public static bool IsTerminal(this Migration migration)
    {
        ArgumentNullException.ThrowIfNull(migration);

        return migration.Status is MigrationStatus.Completed or MigrationStatus.Failed or MigrationStatus.RolledBack;
    }

    /// <summary>
    /// Gets the age of the migration in days since it was created
    /// </summary>
    /// <param name="migration">The migration instance</param>
    /// <returns>The age in days, or 0 if CreatedAt is not set</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="migration"/> is null</exception>
    public static double GetAgeInDays(this Migration migration)
    {
        ArgumentNullException.ThrowIfNull(migration);

        return migration.CreatedAt == default
            ? 0
            : (DateTime.UtcNow - migration.CreatedAt).TotalDays;
    }

    /// <summary>
    /// Gets the execution duration in a human-readable format
    /// </summary>
    /// <param name="migration">The migration instance</param>
    /// <returns>A formatted string representing the execution time, or "N/A" if not executed</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="migration"/> is null</exception>
    public static string GetExecutionDuration(this Migration migration)
    {
        ArgumentNullException.ThrowIfNull(migration);

        if (migration.ExecutedAt is null)
        {
            return "N/A";
        }

        var duration = migration.ExecutionTimeMs > 0
            ? TimeSpan.FromMilliseconds(migration.ExecutionTimeMs)
            : TimeSpan.Zero;

        return duration.TotalSeconds < 1
            ? "< 1s"
            : duration.TotalMinutes < 1
                ? $"{duration.TotalSeconds:F1}s"
                : duration.TotalHours < 1
                    ? $"{duration.TotalMinutes:F1}min"
                    : $"{duration.TotalHours:F2}h";
    }

    /// <summary>
    /// Gets the status display text with color coding based on status
    /// </summary>
    /// <param name="migration">The migration instance</param>
    /// <returns>A formatted status string with ANSI color codes for terminal display</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="migration"/> is null</exception>
    public static string GetStatusDisplay(this Migration migration)
    {
        ArgumentNullException.ThrowIfNull(migration);

        return migration.Status switch
        {
            MigrationStatus.Pending => "[PENDING]",
            MigrationStatus.Running => "[RUNNING]",
            MigrationStatus.Completed => "[COMPLETED]",
            MigrationStatus.Failed => "[FAILED]",
            MigrationStatus.RolledBack => "[ROLLED BACK]",
            _ => $"[{migration.Status}]"
        };
    }

    /// <summary>
    /// Gets migration statistics grouped by status
    /// </summary>
    /// <param name="migrations">Collection of migrations to analyze</param>
    /// <returns>A dictionary with status counts</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="migrations"/> is null</exception>
    public static IReadOnlyDictionary<MigrationStatus, int> GetStatusCounts(
        this IEnumerable<Migration> migrations)
    {
        ArgumentNullException.ThrowIfNull(migrations);

        return migrations
            .GroupBy(m => m.Status)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Gets migrations that are pending execution, ordered by execution order
    /// </summary>
    /// <param name="migrations">Collection of migrations to filter</param>
    /// <returns>Ordered enumerable of pending migrations</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="migrations"/> is null</exception>
    public static IEnumerable<Migration> GetPendingMigrations(
        this IEnumerable<Migration> migrations)
    {
        ArgumentNullException.ThrowIfNull(migrations);

        return migrations
            .Where(m => m.Status == MigrationStatus.Pending)
            .OrderBy(m => m.ExecutionOrder);
    }

    /// <summary>
    /// Gets the total execution time across all completed migrations in milliseconds
    /// </summary>
    /// <param name="migrations">Collection of migrations to analyze</param>
    /// <returns>Total execution time in milliseconds</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="migrations"/> is null</exception>
    public static long GetTotalExecutionTimeMs(this IEnumerable<Migration> migrations)
    {
        ArgumentNullException.ThrowIfNull(migrations);

        return migrations
            .Where(m => m.Status == MigrationStatus.Completed && m.ExecutionTimeMs > 0)
            .Sum(m => m.ExecutionTimeMs);
    }

    /// <summary>
    /// Gets the average execution time for completed migrations in milliseconds
    /// </summary>
    /// <param name="migrations">Collection of migrations to analyze</param>
    /// <returns>Average execution time in milliseconds, or 0 if no completed migrations</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="migrations"/> is null</exception>
    public static double GetAverageExecutionTimeMs(this IEnumerable<Migration> migrations)
    {
        ArgumentNullException.ThrowIfNull(migrations);

        var completedMigrations = migrations
            .Where(m => m.Status == MigrationStatus.Completed && m.ExecutionTimeMs > 0)
            .ToList();

        return completedMigrations.Count > 0
            ? completedMigrations.Average(m => m.ExecutionTimeMs)
            : 0;
    }

    /// <summary>
    /// Gets migrations that can be rolled back, ordered by execution order (newest first)
    /// </summary>
    /// <param name="migrations">Collection of migrations to filter</param>
    /// <returns>Ordered enumerable of rollbackable migrations</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="migrations"/> is null</exception>
    public static IEnumerable<Migration> GetRollbackableMigrations(
        this IEnumerable<Migration> migrations)
    {
        ArgumentNullException.ThrowIfNull(migrations);

        return migrations
            .Where(m => m.CanRollback())
            .OrderByDescending(m => m.ExecutedAt);
    }

    /// <summary>
    /// Gets the database name associated with the migration
    /// </summary>
    /// <param name="migration">The migration instance</param>
    /// <returns>The database name if available; otherwise, the database ID</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="migration"/> is null</exception>
    public static string GetDatabaseName(this Migration migration)
    {
        ArgumentNullException.ThrowIfNull(migration);

        return migration.Database?.Name ?? migration.DatabaseId;
    }

    /// <summary>
    /// Gets the formatted creation timestamp
    /// </summary>
    /// <param name="migration">The migration instance</param>
    /// <returns>Formatted creation timestamp using invariant culture</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="migration"/> is null</exception>
    public static string GetFormattedCreatedAt(this Migration migration)
    {
        ArgumentNullException.ThrowIfNull(migration);

        return migration.CreatedAt == default
            ? "N/A"
            : migration.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }
}