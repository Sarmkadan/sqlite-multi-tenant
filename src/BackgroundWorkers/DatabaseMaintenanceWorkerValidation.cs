#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace SqliteMultiTenant.BackgroundWorkers;

/// <summary>
/// Provides validation helpers for <see cref="DatabaseMaintenanceOptions"/>.
/// Validates configuration values to ensure they are within acceptable ranges
/// and meet basic requirements for database maintenance operations.
/// </summary>
public static class DatabaseMaintenanceWorkerValidation
{
    /// <summary>
    /// Validates a <see cref="DatabaseMaintenanceOptions"/> instance.
    /// Returns a list of human-readable validation problems, or an empty list if valid.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <returns>List of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this DatabaseMaintenanceOptions? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Boolean configuration flags are always valid - no specific validation needed

        // Validate IntervalHours
        if (value.IntervalHours <= 0)
        {
            problems.Add($"IntervalHours must be positive, but was {value.IntervalHours}.");
        }
        else if (value.IntervalHours > 168) // 1 week in hours
        {
            problems.Add($"IntervalHours {value.IntervalHours} exceeds maximum of 168 hours (1 week).");
        }

        // Validate TimeoutSeconds
        if (value.TimeoutSeconds <= 0)
        {
            problems.Add($"TimeoutSeconds must be positive, but was {value.TimeoutSeconds}.");
        }
        else if (value.TimeoutSeconds > 3600) // 1 hour in seconds
        {
            problems.Add($"TimeoutSeconds {value.TimeoutSeconds} exceeds maximum of 3600 seconds (1 hour).");
        }

        // Validate DegreeOfParallelism
        if (value.DegreeOfParallelism < 0)
        {
            problems.Add($"DegreeOfParallelism must be non-negative, but was {value.DegreeOfParallelism}.");
        }
        else if (value.DegreeOfParallelism > Environment.ProcessorCount * 4)
        {
            problems.Add($"DegreeOfParallelism {value.DegreeOfParallelism} exceeds reasonable maximum based on {Environment.ProcessorCount} logical processors.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="DatabaseMaintenanceOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The options to check.</param>
    /// <returns>True if valid; false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this DatabaseMaintenanceOptions? value)
    {
        return value is not null && value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="DatabaseMaintenanceOptions"/> instance is valid.
    /// Throws an <see cref="ArgumentException"/> with a detailed message listing all validation problems.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is invalid.</exception>
    public static void EnsureValid(this DatabaseMaintenanceOptions? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"DatabaseMaintenanceOptions is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }
}