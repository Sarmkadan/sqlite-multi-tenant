#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Provides validation helpers for <see cref="MigrationException"/> instances
/// </summary>
public static class MigrationExceptionValidation
{
    /// <summary>
    /// Validates a <see cref="MigrationException"/> instance and returns a list of validation problems
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <returns>An empty list if valid; otherwise, a list of human-readable problems</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this MigrationException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate MigrationId if present
        if (value.MigrationId is not null)
        {
            if (string.IsNullOrWhiteSpace(value.MigrationId))
            {
                problems.Add("MigrationId cannot be empty or whitespace");
            }
            else if (value.MigrationId.Length > 255)
            {
                problems.Add("MigrationId exceeds maximum length of 255 characters");
            }
        }

        // Validate MigrationVersion if present
        if (value.MigrationVersion is not null)
        {
            if (string.IsNullOrWhiteSpace(value.MigrationVersion))
            {
                problems.Add("MigrationVersion cannot be empty or whitespace");
            }
            else if (value.MigrationVersion.Length > 50)
            {
                problems.Add("MigrationVersion exceeds maximum length of 50 characters");
            }
        }

        // Validate that if MigrationId is set, MigrationVersion should also be set for factory methods
        if (value.MigrationId is not null && value.MigrationVersion is null)
        {
            // This is a warning, not an error - some constructors allow null version
            problems.Add("MigrationId is set but MigrationVersion is null (may be intentional for some constructors)");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="MigrationException"/> instance is valid
    /// </summary>
    /// <param name="value">The exception to check</param>
    /// <returns>True if valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this MigrationException? value) => value is not null && !value.Validate().Any();

    /// <summary>
    /// Ensures that a <see cref="MigrationException"/> instance is valid, throwing an <see cref="ArgumentException"/> if not
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is invalid, containing a list of problems</exception>
    public static void EnsureValid(this MigrationException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"MigrationException is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }
}