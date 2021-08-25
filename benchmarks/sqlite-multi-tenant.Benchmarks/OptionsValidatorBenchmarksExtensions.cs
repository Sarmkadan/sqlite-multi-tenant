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
        /// multi‑tenant and backup options. Returns a simple confirmation string.
        /// </summary>
        public static string RunAllValidations(this OptionsValidatorBenchmarks benchmarks)
        {
            if (benchmarks == null) throw new ArgumentNullException(nameof(benchmarks));

            benchmarks.Setup();
            benchmarks.ValidateMultiTenantOptions_Valid();
            benchmarks.ValidateBackupOptions_Valid();

            return "All option validations executed successfully.";
        }

        /// <summary>
        /// Measures the time taken by a single validation action.
        /// </summary>
        public static TimeSpan MeasureValidation(this OptionsValidatorBenchmarks benchmarks, Action validation)
        {
            if (benchmarks == null) throw new ArgumentNullException(nameof(benchmarks));
            if (validation == null) throw new ArgumentNullException(nameof(validation));

            var stopwatch = Stopwatch.StartNew();
            validation();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Runs both validation methods and returns a detailed timing report.
        /// </summary>
        public static string ValidateAndReport(this OptionsValidatorBenchmarks benchmarks)
        {
            if (benchmarks == null) throw new ArgumentNullException(nameof(benchmarks));

            benchmarks.Setup();

            var multiTenantTime = benchmarks.MeasureValidation(benchmarks.ValidateMultiTenantOptions_Valid);
            var backupTime = benchmarks.MeasureValidation(benchmarks.ValidateBackupOptions_Valid);

            return $"Multi‑tenant validation: {multiTenantTime.TotalMilliseconds:F2} ms, " +
                   $"Backup validation: {backupTime.TotalMilliseconds:F2} ms, " +
                   $"Total: {(multiTenantTime + backupTime).TotalMilliseconds:F2} ms.";
        }

        /// <summary>
        /// Executes the specified validation action repeatedly until it succeeds or the maximum
        /// number of attempts is reached. Returns <c>true</c> if the validation succeeded within
        /// the allowed attempts; otherwise re‑throws the last exception.
        /// </summary>
        public static bool ValidateWithRetry(this OptionsValidatorBenchmarks benchmarks, Action validation, int maxAttempts = 3)
        {
            if (benchmarks == null) throw new ArgumentNullException(nameof(benchmarks));
            if (validation == null) throw new ArgumentNullException(nameof(validation));
            if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

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

            // This line is never reached because the loop either returns true or re‑throws.
            return false;
        }
    }
}
