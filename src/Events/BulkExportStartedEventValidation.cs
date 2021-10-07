#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Events;

/// <summary>
/// Provides validation helpers for <see cref="BulkExportStartedEvent"/> instances.
/// Ensures domain event data integrity before processing or publishing.
/// </summary>
public static class BulkExportStartedEventValidation
{
    private static readonly string[] ValidFormats = ["Json", "Csv", "Sql"];

    /// <summary>
    /// Validates a <see cref="BulkExportStartedEvent"/> instance.
    /// </summary>
    /// <param name="value">The event to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of human-readable validation problems.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this BulkExportStartedEvent? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(value.DatabaseId))
        {
            problems.Add("DatabaseId must not be null or whitespace.");
        }

        if (value.TableNames is null || value.TableNames.Count == 0)
        {
            problems.Add("TableNames must contain at least one table name.");
        }
        else
        {
            for (var i = 0; i < value.TableNames.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(value.TableNames[i]))
                {
                    problems.Add($"TableNames[{i}] must not be null or whitespace.");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(value.Format))
        {
            problems.Add("Format must not be null or whitespace.");
        }
        else if (!ValidFormats.Contains(value.Format, StringComparer.OrdinalIgnoreCase))
        {
            problems.Add($"Format must be one of: {string.Join(", ", ValidFormats)}.");
        }

        if (string.IsNullOrWhiteSpace(value.OperationId))
        {
            problems.Add("OperationId must not be null or whitespace.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="BulkExportStartedEvent"/> is valid.
    /// </summary>
    /// <param name="value">The event to check.</param>
    /// <returns>True if the event is valid; otherwise, false.</returns>
    public static bool IsValid(this BulkExportStartedEvent? value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="BulkExportStartedEvent"/> is valid.
    /// </summary>
    /// <param name="value">The event to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the event is invalid, containing a list of problems.</exception>
    public static void EnsureValid(this BulkExportStartedEvent? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"BulkExportStartedEvent is invalid. Problems: {string.Join(" ", problems)}",
                nameof(value));
        }
    }
}
