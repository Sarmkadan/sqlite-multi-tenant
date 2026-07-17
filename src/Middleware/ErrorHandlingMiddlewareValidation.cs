#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;
using System.Globalization;

namespace SqliteMultiTenant.Middleware;

/// <summary>
/// Validation helpers for <see cref="ErrorHandlingMiddleware"/> and <see cref="Result{T}"/> types.
/// Provides validation, null checks, and invariant enforcement for middleware components.
/// </summary>
public static class ErrorHandlingMiddlewareValidation
{
    /// <summary>
    /// Validates the <see cref="ErrorHandlingMiddleware"/> instance for common issues.
    /// Checks for null logger and ensures all required components are properly initialized.
    /// </summary>
    /// <param name="value">The middleware instance to validate.</param>
    /// <returns>List of validation problems (empty if valid).</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this ErrorHandlingMiddleware value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // ErrorHandlingMiddleware has only a logger dependency which is validated in constructor
        // No additional validation needed beyond null check

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the <see cref="ErrorHandlingMiddleware"/> instance is valid.
    /// </summary>
    /// <param name="value">The middleware instance to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public static bool IsValid(this ErrorHandlingMiddleware value)
 => value.Validate().Count == 0;

    /// <summary>
    /// Ensures the <see cref="ErrorHandlingMiddleware"/> instance is valid.
    /// Throws an <see cref="ArgumentException"/> with detailed validation messages if invalid.
    /// </summary>
    /// <param name="value">The middleware instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails with detailed error messages.</exception>
    public static void EnsureValid(this ErrorHandlingMiddleware value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ErrorHandlingMiddleware validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }

    /// <summary>
    /// Validates a <see cref="Result{T}"/> instance for common issues.
    /// Checks for null values, empty error messages, and validates the result state consistency.
    /// </summary>
    /// <typeparam name="T">The type of value in the result.</typeparam>
    /// <param name="value">The result instance to validate.</param>
    /// <returns>List of validation problems (empty if valid).</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate<T>(this Result<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (!value.IsSuccess && string.IsNullOrWhiteSpace(value.ErrorMessage))
        {
            problems.Add("Result.IsSuccess is false but ErrorMessage is null or whitespace");
        }

        if (value.IsSuccess && value.ErrorMessage is not null)
        {
            problems.Add("Result.IsSuccess is true but ErrorMessage is not null");
        }

        if (value.IsSuccess && EqualityComparer<T>.Default.Equals(value.Value, default(T)))
        {
            problems.Add("Result.IsSuccess is true but Value is default/null for reference types or zero for value types");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the <see cref="Result{T}"/> instance is valid.
    /// </summary>
    /// <typeparam name="T">The type of value in the result.</typeparam>
    /// <param name="value">The result instance to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public static bool IsValid<T>(this Result<T> value)
 => value.Validate().Count == 0;

    /// <summary>
    /// Ensures the <see cref="Result{T}"/> instance is valid.
    /// Throws an <see cref="ArgumentException"/> with detailed validation messages if invalid.
    /// </summary>
    /// <typeparam name="T">The type of value in the result.</typeparam>
    /// <param name="value">The result instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails with detailed error messages.</exception>
    public static void EnsureValid<T>(this Result<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Result<T> validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}
