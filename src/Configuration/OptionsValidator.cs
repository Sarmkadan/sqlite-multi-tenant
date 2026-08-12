#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using SqliteMultiTenant.Configuration;

namespace SqliteMultiTenant.Configuration
{
    /// <summary>
    /// Provides validation helpers for configuration options.
    /// </summary>
    public static class OptionsValidator
    {
        /// <summary>
        /// Validates the specified <see cref="MultiTenantOptions"/> instance.
        /// </summary>
        /// <param name="options">The options to validate.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when any required property of <paramref name="options"/> is invalid.
        /// </exception>
        public static void Validate(MultiTenantOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (string.IsNullOrWhiteSpace(options?.BasePath))
                throw new ArgumentException("BasePath cannot be empty");

            if (options.MaxConnectionsPerTenant <= 0)
                throw new ArgumentException("MaxConnectionsPerTenant must be greater than 0");

            if (options.MaxBackupCount <= 0)
                throw new ArgumentException("MaxBackupCount must be greater than 0");

            if (options.BackupRetention <= TimeSpan.Zero)
                throw new ArgumentException("BackupRetention must be positive");
        }

        /// <summary>
        /// Validates the specified <see cref="BackupOptions"/> instance.
        /// </summary>
        /// <param name="options">The backup options to validate.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when any required property of <paramref name="options"/> is invalid.
        /// </exception>
        public static void Validate(BackupOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (options?.MaxConcurrentBackups <= 0)
                throw new ArgumentException("MaxConcurrentBackups must be greater than 0");

            if (options.BackupTimeoutSeconds <= 0)
                throw new ArgumentException("BackupTimeoutSeconds must be greater than 0");
        }

        /// <summary>
        /// Validates the specified <see cref="SecurityOptions"/> instance.
        /// </summary>
        /// <param name="options">The security options to validate.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when any required property of <paramref name="options"/> is invalid.
        /// </exception>
        public static void Validate(SecurityOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (options?.SessionTimeout <= TimeSpan.Zero)
                throw new ArgumentException("SessionTimeout must be positive");

            if (options.MaxFailedLoginAttempts <= 0)
                throw new ArgumentException("MaxFailedLoginAttempts must be greater than 0");

            if (options.LockoutDuration <= TimeSpan.Zero)
                throw new ArgumentException("LockoutDuration must be positive");
        }
    }
}
