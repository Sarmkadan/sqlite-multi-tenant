using System;
using System.Collections.Generic;

namespace SqliteMultiTenant.Services
{
    /// <summary>
    /// Provides validation extensions for the <see cref="TenantService"/> class.
    /// </summary>
    public static class TenantServiceValidation
    {
        /// <summary>
        /// Validates the <see cref="TenantService"/> instance.
        /// </summary>
        /// <param name="value">The service instance to validate.</param>
        /// <returns>A list of human-readable validation problems.</returns>
        public static IReadOnlyList<string> Validate(this TenantService value)
        {
            var problems = new List<string>();

            if (value == null)
            {
                problems.Add("TenantService instance cannot be null.");
            }

            // Note: Based on the provided member list, TenantService exposes only methods.
            // There are no public properties (strings, numbers, dates) defined in the specification
            // to validate for null/empty or range constraints.

            return problems;
        }

        /// <summary>
        /// Determines whether the <see cref="TenantService"/> instance is valid.
        /// </summary>
        /// <param name="value">The service instance to validate.</param>
        /// <returns><c>true</c> if the instance is valid; otherwise, <c>false</c>.</returns>
        public static bool IsValid(this TenantService value)
        {
            return Validate(value).Count == 0;
        }

        /// <summary>
        /// Ensures the <see cref="TenantService"/> instance is valid, throwing an exception if it is not.
        /// </summary>
        /// <param name="value">The service instance to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the instance is null.</exception>
        public static void EnsureValid(this TenantService value)
        {
            var errors = Validate(value);
            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join(Environment.NewLine, errors), nameof(value));
            }
        }
    }
}
