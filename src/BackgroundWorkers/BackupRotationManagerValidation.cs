#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace SqliteMultiTenant.BackgroundWorkers
{
    /// <summary>
    /// Provides validation helpers for <see cref="BackupRotationManager"/> and related types.
    /// </summary>
    public static class BackupRotationManagerValidation
    {
        /// <summary>
        /// Validates a <see cref="BackupRotationPolicy"/> instance.
        /// </summary>
        /// <param name="value">The policy to validate.</param>
        /// <returns>An enumerable of validation errors; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this BackupRotationPolicy? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = new List<string>();

            // Validate MaxBackupAge
            if (value.MaxBackupAge <= TimeSpan.Zero)
            {
                errors.Add($"MaxBackupAge must be greater than zero (current: {value.MaxBackupAge.TotalDays} days).");
            }
            else if (value.MaxBackupAge.TotalDays > 365 * 5)
            {
                errors.Add($"MaxBackupAge exceeds maximum allowed value of 5 years (current: {value.MaxBackupAge.TotalDays} days).");
            }

            // Validate MaxBackupCount
            if (value.MaxBackupCount <= 0)
            {
                errors.Add($"MaxBackupCount must be greater than zero (current: {value.MaxBackupCount}).");
            }
            else if (value.MaxBackupCount > 1000)
            {
                errors.Add($"MaxBackupCount exceeds maximum allowed value of 1000 (current: {value.MaxBackupCount}).");
            }

            // Validate MaxDiskUsage
            if (value.MaxDiskUsage <= 0)
            {
                errors.Add($"MaxDiskUsage must be greater than zero bytes (current: {value.MaxDiskUsage} bytes).");
            }
            else if (value.MaxDiskUsage > 100L * 1024 * 1024 * 1024 * 1024) // 100 TB
            {
                errors.Add($"MaxDiskUsage exceeds maximum allowed value of 100 TB (current: {FormatBytes(value.MaxDiskUsage)}).");
            }

            return errors.AsReadOnly();
        }

        /// <summary>
        /// Validates a <see cref="BackupRotationResult"/> instance.
        /// </summary>
        /// <param name="value">The result to validate.</param>
        /// <returns>An enumerable of validation errors; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this BackupRotationResult? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = new List<string>();

            // Validate TotalBackups
            if (value.TotalBackups < 0)
            {
                errors.Add($"TotalBackups cannot be negative (current: {value.TotalBackups}).");
            }

            // Validate RemainingBackups
            if (value.RemainingBackups < 0)
            {
                errors.Add($"RemainingBackups cannot be negative (current: {value.RemainingBackups}).");
            }

            // Validate DeletedByAge
            if (value.DeletedByAge < 0)
            {
                errors.Add($"DeletedByAge cannot be negative (current: {value.DeletedByAge}).");
            }

            // Validate DeletedByCount
            if (value.DeletedByCount < 0)
            {
                errors.Add($"DeletedByCount cannot be negative (current: {value.DeletedByCount}).");
            }

            // Validate ExecutedAt
            if (value.ExecutedAt == default)
            {
                errors.Add("ExecutedAt must be set to a valid DateTime.");
            }
            else if (value.ExecutedAt > DateTime.UtcNow.AddMinutes(5))
            {
                errors.Add($"ExecutedAt is in the future (current: {value.ExecutedAt:O}).");
            }
            else if (value.ExecutedAt < DateTime.UtcNow.AddYears(-1))
            {
                errors.Add($"ExecutedAt is more than one year in the past (current: {value.ExecutedAt:O}).");
            }

            // Validate IsSuccessful when Error is present
            if (!string.IsNullOrEmpty(value.Error) && value.IsSuccessful)
            {
                errors.Add("IsSuccessful must be false when Error is present.");
            }

            // Validate Error message
            if (!string.IsNullOrEmpty(value.Error))
            {
                if (value.Error.Length > 1000)
                {
                    errors.Add($"Error message exceeds maximum length of 1000 characters (current: {value.Error.Length}).");
                }

                if (value.Error.Contains("\n", StringComparison.Ordinal) || value.Error.Contains("\r", StringComparison.Ordinal))
                {
                    errors.Add("Error message contains line breaks.");
                }
            }

            return errors.AsReadOnly();
        }

        /// <summary>
        /// Validates a <see cref="BackupVerificationResult"/> instance.
        /// </summary>
        /// <param name="value">The result to validate.</param>
        /// <returns>An enumerable of validation errors; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this BackupVerificationResult? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = new List<string>();

            // Validate FilePath
            if (string.IsNullOrWhiteSpace(value.FilePath))
            {
                errors.Add("FilePath cannot be null or whitespace.");
            }
            else if (value.FilePath.Length > 2048)
            {
                errors.Add($"FilePath exceeds maximum length of 2048 characters (current: {value.FilePath.Length}).");
            }
            else if (!Uri.IsWellFormedUriString(value.FilePath, UriKind.Absolute) && !Path.IsPathRooted(value.FilePath))
            {
                errors.Add($"FilePath must be a valid absolute path or URI (current: '{value.FilePath}').");
            }

            // Validate FileName
            if (!string.IsNullOrEmpty(value.FileName) && value.FileName.Length > 255)
            {
                errors.Add($"FileName exceeds maximum length of 255 characters (current: {value.FileName.Length}).");
            }

            // Validate FileSize
            if (value.FileSize < 0)
            {
                errors.Add($"FileSize cannot be negative (current: {value.FileSize} bytes).");
            }

            // Validate FileSizeBytes
            if (value.FileSizeBytes < 0)
            {
                errors.Add($"FileSizeBytes cannot be negative (current: {value.FileSizeBytes} bytes).");
            }

            // Validate CreatedAt
            if (value.CreatedAt != default && value.CreatedAt > DateTime.UtcNow.AddMinutes(5))
            {
                errors.Add($"CreatedAt is in the future (current: {value.CreatedAt:O}).");
            }

            // Validate LastModified
            if (value.LastModified != default && value.LastModified > DateTime.UtcNow.AddMinutes(5))
            {
                errors.Add($"LastModified is in the future (current: {value.LastModified:O}).");
            }

            // Validate IsValid and IsReadable consistency
            if (value.IsValid && !value.IsReadable)
            {
                errors.Add("IsValid cannot be true when IsReadable is false.");
            }

            // Validate Error message
            if (!string.IsNullOrEmpty(value.Error))
            {
                if (value.Error.Length > 1000)
                {
                    errors.Add($"Error message exceeds maximum length of 1000 characters (current: {value.Error.Length}).");
                }

                if (value.Error.Contains("\n", StringComparison.Ordinal) || value.Error.Contains("\r", StringComparison.Ordinal))
                {
                    errors.Add("Error message contains line breaks.");
                }
            }

            return errors.AsReadOnly();
        }

        /// <summary>
        /// Validates a <see cref="BackupStatistics"/> instance.
        /// </summary>
        /// <param name="value">The statistics to validate.</param>
        /// <returns>An enumerable of validation errors; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this BackupStatistics? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = new List<string>();

            // Validate TotalBackups
            if (value.TotalBackups < 0)
            {
                errors.Add($"TotalBackups cannot be negative (current: {value.TotalBackups}).");
            }

            // Validate TotalDiskUsage
            if (value.TotalDiskUsage < 0)
            {
                errors.Add($"TotalDiskUsage cannot be negative (current: {value.TotalDiskUsage} bytes).");
            }
            else if (value.TotalDiskUsage > 100L * 1024 * 1024 * 1024 * 1024) // 100 TB
            {
                errors.Add($"TotalDiskUsage exceeds maximum allowed value of 100 TB (current: {FormatBytes(value.TotalDiskUsage)}).");
            }

            // Validate AverageBackupSize
            if (value.AverageBackupSize < 0)
            {
                errors.Add($"AverageBackupSize cannot be negative (current: {value.AverageBackupSize} bytes).");
            }

            // Validate date ranges
            if (value.OldestBackup.HasValue)
            {
                if (value.OldestBackup.Value > DateTime.UtcNow.AddMinutes(5))
                {
                    errors.Add($"OldestBackup is in the future (current: {value.OldestBackup.Value:O}).");
                }
                else if (value.OldestBackup.Value < DateTime.UtcNow.AddYears(-5))
                {
                    errors.Add($"OldestBackup is more than 5 years in the past (current: {value.OldestBackup.Value:O}).");
                }
            }

            if (value.NewestBackup.HasValue)
            {
                if (value.NewestBackup.Value > DateTime.UtcNow.AddMinutes(5))
                {
                    errors.Add($"NewestBackup is in the future (current: {value.NewestBackup.Value:O}).");
                }
                else if (value.NewestBackup.Value < DateTime.UtcNow.AddYears(-5))
                {
                    errors.Add($"NewestBackup is more than 5 years in the past (current: {value.NewestBackup.Value:O}).");
                }
            }

            // Validate that NewestBackup is not older than OldestBackup
            if (value.OldestBackup.HasValue && value.NewestBackup.HasValue && value.NewestBackup.Value < value.OldestBackup.Value)
            {
                errors.Add("NewestBackup cannot be older than OldestBackup.");
            }

            return errors.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the specified <see cref="BackupRotationPolicy"/> is valid.
        /// </summary>
        /// <param name="value">The policy to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(this BackupRotationPolicy? value) => value.Validate().Count == 0;

        /// <summary>
        /// Determines whether the specified <see cref="BackupRotationResult"/> is valid.
        /// </summary>
        /// <param name="value">The result to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(this BackupRotationResult? value) => value.Validate().Count == 0;

        /// <summary>
        /// Determines whether the specified <see cref="BackupVerificationResult"/> is valid.
        /// </summary>
        /// <param name="value">The result to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(this BackupVerificationResult? value) => value.Validate().Count == 0;

        /// <summary>
        /// Determines whether the specified <see cref="BackupStatistics"/> is valid.
        /// </summary>
        /// <param name="value">The statistics to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(this BackupStatistics? value) => value.Validate().Count == 0;

        /// <summary>
        /// Ensures that the specified <see cref="BackupRotationPolicy"/> is valid, throwing an <see cref="ArgumentException"/> if it is not.
        /// </summary>
        /// <param name="value">The policy to validate.</param>
        /// <exception cref="ArgumentException">Thrown if the policy is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static void EnsureValid(this BackupRotationPolicy? value)
        {
            var errors = value.Validate();
            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    $"BackupRotationPolicy is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
            }
        }

        /// <summary>
        /// Ensures that the specified <see cref="BackupRotationResult"/> is valid, throwing an <see cref="ArgumentException"/> if it is not.
        /// </summary>
        /// <param name="value">The result to validate.</param>
        /// <exception cref="ArgumentException">Thrown if the result is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static void EnsureValid(this BackupRotationResult? value)
        {
            var errors = value.Validate();
            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    $"BackupRotationResult is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
            }
        }

        /// <summary>
        /// Ensures that the specified <see cref="BackupVerificationResult"/> is valid, throwing an <see cref="ArgumentException"/> if it is not.
        /// </summary>
        /// <param name="value">The result to validate.</param>
        /// <exception cref="ArgumentException">Thrown if the result is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static void EnsureValid(this BackupVerificationResult? value)
        {
            var errors = value.Validate();
            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    $"BackupVerificationResult is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
            }
        }

        /// <summary>
        /// Ensures that the specified <see cref="BackupStatistics"/> is valid, throwing an <see cref="ArgumentException"/> if it is not.
        /// </summary>
        /// <param name="value">The statistics to validate.</param>
        /// <exception cref="ArgumentException">Thrown if the statistics are invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static void EnsureValid(this BackupStatistics? value)
        {
            var errors = value.Validate();
            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    $"BackupStatistics is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
            }
        }

        /// <summary>
        /// Formats a byte count as a human-readable string (e.g., "1.2 GB").
        /// </summary>
        /// <param name="bytes">The number of bytes to format.</param>
        /// <returns>A formatted string.</returns>
        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
            int counter = 0;
            double number = bytes;

            while (number >= 1024 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }

            return string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1}", number, suffixes[counter]);
        }
    }
}
