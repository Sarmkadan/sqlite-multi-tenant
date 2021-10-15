#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace SqliteMultiTenant.Database
{
    /// <summary>
    /// Provides validation and validation helper methods for <see cref="SchemaManager"/> instances.
    /// </summary>
    public static class SchemaManagerValidation
    {
        /// <summary>
        /// Validates the specified <see cref="SchemaManager"/> instance.
        /// </summary>
        /// <param name="value">The <see cref="SchemaManager"/> instance to validate.</param>
        /// <returns>A list of human-readable validation problems; empty if the instance is valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this SchemaManager value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // SchemaManager doesn't have any state to validate beyond constructor parameters
            // which are validated at construction time

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the specified <see cref="SchemaManager"/> instance is valid.
        /// </summary>
        /// <param name="value">The <see cref="SchemaManager"/> instance to check.</param>
        /// <returns><c>true</c> if the instance is valid; otherwise, <c>false</c>.</returns>
        public static bool IsValid(this SchemaManager value)
        {
            return value.Validate().Count == 0;
        }

        /// <summary>
        /// Ensures that the specified <see cref="SchemaManager"/> instance is valid.
        /// </summary>
        /// <param name="value">The <see cref="SchemaManager"/> instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the instance is not valid, containing a list of problems.</exception>
        public static void EnsureValid(this SchemaManager value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();

            if (problems.Count > 0)
            {
                throw new ArgumentException(
                    $"SchemaManager is not valid. Problems: {string.Join("; ", problems)}");
            }
        }
    }
}