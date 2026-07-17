using System;
using System.Collections.Generic;

namespace SqliteMultiTenant.Operations
{
    /// <summary>
    /// Provides validation helpers for <see cref="ConflictResolutionService"/> instances.
    /// </summary>
    public static class ConflictResolutionServiceValidation
    {
        /// <summary>
        /// Validates the specified <see cref="ConflictResolutionService"/> instance.
        /// </summary>
        /// <param name="value">The service instance to validate.</param>
        /// <returns>A list of human-readable validation problems; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static List<string> Validate(this ConflictResolutionService value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = new List<string>();

            // ConflictResolutionService itself has no validation constraints
            // The validation is based on the results it produces

            return errors;
        }

        /// <summary>
        /// Determines whether the specified <see cref="ConflictResolutionService"/> instance is valid.
        /// </summary>
        /// <param name="value">The service instance to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static bool IsValid(this ConflictResolutionService value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return Validate(value).Count == 0;
        }

        /// <summary>
        /// Ensures that the specified <see cref="ConflictResolutionService"/> instance is valid.
        /// </summary>
        /// <param name="value">The service instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the instance is not valid, containing the list of problems.</exception>
        public static void EnsureValid(this ConflictResolutionService value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = Validate(value);
            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    $"ConflictResolutionService is not valid. Problems:\n{string.Join("\n", errors)}",
                    nameof(value));
            }
        }

        /// <summary>
        /// Validates a <see cref="ConflictDetectionResult"/> instance.
        /// </summary>
        /// <param name="result">The detection result to validate.</param>
        /// <returns>A list of human-readable validation problems; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> is null.</exception>
        public static List<string> Validate(this ConflictDetectionResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            var errors = new List<string>();

            if (result.Conflicts is null)
            {
                errors.Add("ConflictDetectionResult.Conflicts cannot be null.");
            }

            return errors;
        }

        /// <summary>
        /// Validates a <see cref="DataConflict"/> instance.
        /// </summary>
        /// <param name="conflict">The conflict to validate.</param>
        /// <returns>A list of human-readable validation problems; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="conflict"/> is null.</exception>
        public static List<string> Validate(this DataConflict conflict)
        {
            ArgumentNullException.ThrowIfNull(conflict);

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(conflict.Field))
            {
                errors.Add("DataConflict.Field cannot be null or whitespace.");
            }

            if (!Enum.IsDefined(typeof(ConflictType), conflict.ConflictType))
            {
                errors.Add($"DataConflict.ConflictType '{conflict.ConflictType}' is not a valid ConflictType value.");
            }

            // LocalValue and RemoteValue can be null depending on conflict type

            return errors;
        }

        /// <summary>
        /// Validates a <see cref="ConflictResolutionResult"/> instance.
        /// </summary>
        /// <param name="result">The resolution result to validate.</param>
        /// <returns>A list of human-readable validation problems; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> is null.</exception>
        public static List<string> Validate(this ConflictResolutionResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            var errors = new List<string>();

            if (result.ResolvedValues is null)
            {
                errors.Add("ConflictResolutionResult.ResolvedValues cannot be null.");
            }

            if (result.IsSuccessful && !string.IsNullOrEmpty(result.Error))
            {
                errors.Add("ConflictResolutionResult.Error must be null or empty when IsSuccessful is true.");
            }

            if (!result.IsSuccessful && string.IsNullOrEmpty(result.Error))
            {
                errors.Add("ConflictResolutionResult.Error must be provided when IsSuccessful is false.");
            }

            return errors;
        }

        /// <summary>
        /// Validates all conflicts in a <see cref="ConflictDetectionResult"/>.
        /// </summary>
        /// <param name="result">The detection result containing conflicts to validate.</param>
        /// <returns>A list of human-readable validation problems; empty if all conflicts are valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> is null.</exception>
        public static List<string> ValidateConflicts(this ConflictDetectionResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            var errors = new List<string>();

            if (result.Conflicts is not null)
            {
                for (int i = 0; i < result.Conflicts.Count; i++)
                {
                    var conflict = result.Conflicts[i];
                    if (conflict is null)
                    {
                        errors.Add($"ConflictDetectionResult.Conflicts[{i}] cannot be null.");
                        continue;
                    }

                    var conflictErrors = Validate(conflict);
                    if (conflictErrors.Count > 0)
                    {
                        errors.AddRange(conflictErrors.Select(e => $"Conflict[{i}]: {e}"));
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Validates all resolved values in a <see cref="ConflictResolutionResult"/>.
        /// </summary>
        /// <param name="result">The resolution result containing resolved values to validate.</param>
        /// <returns>A list of human-readable validation problems; empty if all resolved values are valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> is null.</exception>
        public static List<string> ValidateResolvedValues(this ConflictResolutionResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            var errors = new List<string>();

            if (result.ResolvedValues is not null)
            {
                foreach (var kvp in result.ResolvedValues)
                {
                    if (string.IsNullOrEmpty(kvp.Key))
                    {
                        errors.Add("ConflictResolutionResult.ResolvedValues contains entry with null or empty key.");
                    }
                }
            }

            return errors;
        }
    }
}