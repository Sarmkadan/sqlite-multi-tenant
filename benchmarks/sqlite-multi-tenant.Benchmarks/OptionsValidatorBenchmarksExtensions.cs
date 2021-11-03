using System;
using System.Diagnostics;

namespace SqliteMultiTenant.Benchmarks
{
    /// <summary>
    /// Extension methods that make it easier to work with <see cref="OptionsValidatorBenchmarks"/>
    /// in benchmark scenarios.
    /// </summary>
    public static class OptionsValidatorBenchmarksExtensions
    {
        /// <summary>
        /// Executes the full benchmark suite: runs <c>Setup</c> once and then validates both
        /// multi-tenant and backup options. Returns a simple confirmation string.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
        public static string RunAllValidations(this OptionsValidatorBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);

            benchmarks.Setup();
            benchmarks.ValidateMultiTenantOptions_Valid();
            benchmarks.ValidateBackupOptions_Valid();

            return "All option validations executed successfully.";
        }

        /// <summary>
        /// Measures the time taken by a single validation action.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance.</param>
        /// <param name="validation">The validation action to measure.</param>
        /// <returns>The elapsed time for the validation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> or <paramref name="validation"/> is <see langword="null"/>.</exception>
        public static TimeSpan MeasureValidation(this OptionsValidatorBenchmarks benchmarks, Action validation)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            ArgumentNullException.ThrowIfNull(validation);

            var stopwatch = Stopwatch.StartNew();
            validation();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Runs both validation methods and returns a detailed timing report.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance.</param>
        /// <returns>A formatted string with timing information.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
        public static string ValidateAndReport(this OptionsValidatorBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);

            benchmarks.Setup();

            var multiTenantTime = benchmarks.MeasureValidation(benchmarks.ValidateMultiTenantOptions_Valid);
            var backupTime = benchmarks.MeasureValidation(benchmarks.ValidateBackupOptions_Valid);

            return $"Multi-tenant validation: {multiTenantTime.TotalMilliseconds:F2} ms, " +
                   $"Backup validation: {backupTime.TotalMilliseconds:F2} ms, " +
                   $"Total: {(multiTenantTime + backupTime).TotalMilliseconds:F2} ms.";
        }

        /// <summary>
        /// Executes the specified validation action repeatedly until it succeeds or the maximum
        /// number of attempts is reached. Returns <c>true</c> if the validation succeeded within
        /// the allowed attempts; otherwise re-throws the last exception.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance.</param>
        /// <param name="validation">The validation action to execute.</param>
        /// <param name="maxAttempts">Maximum number of retry attempts. Must be greater than zero.</param>
        /// <returns><see langword="true"/> if validation succeeded; otherwise throws the last exception.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> or <paramref name="validation"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxAttempts"/> is less than 1.</exception>
        public static bool ValidateWithRetry(this OptionsValidatorBenchmarks benchmarks, Action validation, int maxAttempts = 3)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            ArgumentNullException.ThrowIfNull(validation);
            ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

            benchmarks.Setup();

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    validation();
                    return true;
                }
                catch
                {
                    if (attempt == maxAttempts) throw;
                }
            }

            return false;
        }
    }
}
