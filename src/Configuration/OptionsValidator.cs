// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using SqliteMultiTenant.Configuration;

namespace SqliteMultiTenant.Configuration
{
    // Validation helpers for configuration options
    public static class OptionsValidator
    {
        public static void Validate(MultiTenantOptions options)
        {
            if (string.IsNullOrWhiteSpace(options?.BasePath))
                throw new ArgumentException("BasePath cannot be empty");

            if (options.MaxConnectionsPerTenant <= 0)
                throw new ArgumentException("MaxConnectionsPerTenant must be greater than 0");

            if (options.MaxBackupCount <= 0)
                throw new ArgumentException("MaxBackupCount must be greater than 0");

            if (options.BackupRetention <= TimeSpan.Zero)
                throw new ArgumentException("BackupRetention must be positive");
        }

        public static void Validate(BackupOptions options)
        {
            if (options?.MaxConcurrentBackups <= 0)
                throw new ArgumentException("MaxConcurrentBackups must be greater than 0");

            if (options.BackupTimeoutSeconds <= 0)
                throw new ArgumentException("BackupTimeoutSeconds must be greater than 0");
        }

        public static void Validate(SecurityOptions options)
        {
            if (options?.SessionTimeout <= TimeSpan.Zero)
                throw new ArgumentException("SessionTimeout must be positive");

            if (options.MaxFailedLoginAttempts <= 0)
                throw new ArgumentException("MaxFailedLoginAttempts must be greater than 0");

            if (options.LockoutDuration <= TimeSpan.Zero)
                throw new ArgumentException("LockoutDuration must be positive");
        }
    }
}
