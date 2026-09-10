#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace SqliteMultiTenant.Configuration
{
    // Configuration options for multi-tenant SQLite setup
    /// <summary>
    /// Configuration options for multi-tenant SQLite setup.
    /// </summary>
    public sealed class MultiTenantOptions {
        /// <summary>
        /// The base path where tenant databases are stored. Default: "./databases".
        /// </summary>
        public string BasePath { get; set; } = "./databases";
        /// <summary>
        /// Maximum number of connections allowed per tenant. Default: 10.
        /// </summary>
        public int MaxConnectionsPerTenant { get; set; } = 10;
        /// <summary>
        /// Default maximum number of connections for a tenant if not overridden. Default: 10.
        /// </summary>
        public int DefaultMaxConnections { get; set; } = 10;
        /// <summary>
        /// Maximum number of backup files to retain per tenant. Default: 20.
        /// </summary>
        public int MaxBackupCount { get; set; } = 20;
        /// <summary>
        /// Time span after which backups are deleted. Default: 30 days.
        /// </summary>
        public TimeSpan BackupRetention { get; set; } = TimeSpan.FromDays(30);
        /// <summary>
        /// Indicates whether backup scheduling is enabled. Default: true.
        /// </summary>
        public bool EnableBackupScheduling { get; set; } = true;
        /// <summary>
        /// Interval between automatic backups. Default: 1 hour.
        /// </summary>
        public TimeSpan BackupInterval { get; set; } = TimeSpan.FromHours(1);
        /// <summary>
        /// Indicates whether audit logging is enabled. Default: true.
        /// </summary>
        public bool EnableAuditLogging { get; set; } = true;
        /// <summary>
        /// Indicates whether performance monitoring is enabled. Default: true.
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = true;
        /// <summary>
        /// Indicates whether data encryption is enabled. Default: false.
        /// </summary>
        public bool EnableDataEncryption { get; set; } = false;
        /// <summary>
        /// Maximum number of cache entries. Default: 1000.
        /// </summary>
        public int MaxCacheSize { get; set; } = 1000;
        /// <summary>
        /// Default time-to-live for cache entries. Default: 1 hour.
        /// </summary>
        public TimeSpan DefaultCacheTTL { get; set; } = TimeSpan.FromHours(1);
        /// <summary>
        /// Number of rate limit requests allowed per minute. Default: 1000.
        /// </summary>
        public int RateLimitRequestsPerMinute { get; set; } = 1000;
        /// <summary>
        /// Path to the encryption key file. Default: "./keys".
        /// </summary>
        public string EncryptionKeyPath { get; set; } = "./keys";
        /// <summary>
        /// Indicates whether verbose logging is enabled. Default: false.
        /// </summary>
        public bool VerboseLogging { get; set; } = false;

        public override string ToString() =>
            $"MultiTenantOptions {{ BasePath = {BasePath}, MaxConnectionsPerTenant = {MaxConnectionsPerTenant}, DefaultMaxConnections = {DefaultMaxConnections}, MaxBackupCount = {MaxBackupCount}, BackupRetention = {BackupRetention}, EnableBackupScheduling = {EnableBackupScheduling} }}";
    }

    // Backup-specific configuration
    /// <summary>
    /// Backup-specific configuration.
    /// </summary>
    public sealed class BackupOptions {
        /// <summary>
        /// The path where backups are stored. Default: "./backups".
        /// </summary>
        public string BackupPath { get; set; } = "./backups";
        /// <summary>
        /// Maximum number of concurrent backup operations. Default: 3.
        /// </summary>
        public int MaxConcurrentBackups { get; set; } = 3;
        /// <summary>
        /// Timeout in seconds for backup operations. Default: 300.
        /// </summary>
        public int BackupTimeoutSeconds { get; set; } = 300;
        /// <summary>
        /// Indicates whether backups are compressed. Default: true.
        /// </summary>
        public bool CompressBackups { get; set; } = true;
        /// <summary>
        /// Indicates whether backup integrity is verified after creation. Default: true.
        /// </summary>
        public bool VerifyBackupIntegrity { get; set; } = true;
        /// <summary>
        /// Indicates whether differential backups are retained. Default: true.
        /// </summary>
        public bool RetainDifferentialBackups { get; set; } = true;
    }

    // Monitoring and alerting configuration
    /// <summary>
    /// Monitoring and alerting configuration.
    /// </summary>
    public sealed class MonitoringOptions {
        /// <summary>
        /// Indicates whether monitoring is enabled. Default: true.
        /// </summary>
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// Interval at which metrics are collected. Default: 60 seconds.
        /// </summary>
        public TimeSpan MetricsCollectionInterval { get; set; } = TimeSpan.FromSeconds(60);
        /// <summary>
        /// Maximum number of metric history entries to retain. Default: 1000.
        /// </summary>
        public int MaxMetricsHistory { get; set; } = 1000;
        /// <summary>
        /// Threshold in milliseconds for a query to be considered slow. Default: 1000.
        /// </summary>
        public long SlowQueryThresholdMs { get; set; } = 1000;
        /// <summary>
        /// Dictionary of alert thresholds keyed by metric name.
        /// </summary>
        public Dictionary<string, AlertThreshold> AlertThresholds { get; set; } =
            new Dictionary<string, AlertThreshold>();
    }

    // Alert threshold configuration
    /// <summary>
    /// Alert threshold configuration.
    /// </summary>
    public sealed class AlertThreshold {
        /// <summary>
        /// The name of the metric to monitor.
        /// </summary>
        public string MetricName { get; set; }
        /// <summary>
        /// The value at which a warning alert is triggered.
        /// </summary>
        public double WarningLevel { get; set; }
        /// <summary>
        /// The value at which a critical alert is triggered.
        /// </summary>
        public double CriticalLevel { get; set; }
        /// <summary>
        /// Indicates whether email alerts are enabled for this threshold.
        /// </summary>
        public bool EnableEmail { get; set; }
        /// <summary>
        /// Indicates whether log alerts are enabled for this threshold.
        /// </summary>
        public bool EnableLog { get; set; }
    }

    // Security configuration
    /// <summary>
    /// Security configuration.
    /// </summary>
    public sealed class SecurityOptions {
        /// <summary>
        /// Indicates whether tenant ID validation is required. Default: true.
        /// </summary>
        public bool RequireTenantIdValidation { get; set; } = true;
        /// <summary>
        /// Indicates whether query logging is enabled. Default: false.
        /// </summary>
        public bool EnableQueryLogging { get; set; } = false;
        /// <summary>
        /// Indicates whether connection encryption is enabled. Default: false.
        /// </summary>
        public bool EnableConnectionEncryption { get; set; } = false;
        /// <summary>
        /// Timeout for user sessions. Default: 1 hour.
        /// </summary>
        public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(1);
        /// <summary>
        /// Maximum number of failed login attempts before lockout. Default: 5.
        /// </summary>
        public int MaxFailedLoginAttempts { get; set; } = 5;
        /// <summary>
        /// Duration of account lockout after too many failed attempts. Default: 15 minutes.
        /// </summary>
        public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
        /// <summary>
        /// Indicates whether strong passwords are required. Default: false.
        /// </summary>
        public bool RequireStrongPasswords { get; set; } = false;
    }

    // Database maintenance configuration
    /// <summary>
    /// Database maintenance configuration.
    /// </summary>
    public sealed class MaintenanceOptions {
        /// <summary>
        /// Indicates whether automatic vacuum is enabled. Default: true.
        /// </summary>
        public bool EnableAutomaticVacuum { get; set; } = true;
        /// <summary>
        /// Interval at which automatic vacuum runs. Default: 1 day.
        /// </summary>
        public TimeSpan VacuumInterval { get; set; } = TimeSpan.FromDays(1);
        /// <summary>
        /// Indicates whether index rebuild is enabled. Default: true.
        /// </summary>
        public bool EnableIndexRebuild { get; set; } = true;
        /// <summary>
        /// Interval at which index rebuild runs. Default: 7 days.
        /// </summary>
        public TimeSpan IndexRebuildInterval { get; set; } = TimeSpan.FromDays(7);
        /// <summary>
        /// Indicates whether statistics update is enabled. Default: true.
        /// </summary>
        public bool EnableStatisticsUpdate { get; set; } = true;
        /// <summary>
        /// Interval at which statistics update runs. Default: 1 day.
        /// </summary>
        public TimeSpan StatisticsUpdateInterval { get; set; } = TimeSpan.FromDays(1);
    }

    // Feature flags
    /// <summary>
    /// Feature flags.
    /// </summary>
    public sealed class FeatureFlags {
        /// <summary>
        /// Indicates whether caching is enabled. Default: true.
        /// </summary>
        public bool EnableCaching { get; set; } = true;
        /// <summary>
        /// Indicates whether batching is enabled. Default: true.
        /// </summary>
        public bool EnableBatching { get; set; } = true;
        /// <summary>
        /// Indicates whether asynchronous operations are enabled. Default: true.
        /// </summary>
        public bool EnableAsyncOperations { get; set; } = true;
        /// <summary>
        /// Indicates whether webhooks are enabled. Default: false.
        /// </summary>
        public bool EnableWebhooks { get; set; } = false;
        /// <summary>
        /// Indicates whether external integrations are enabled. Default: false.
        /// </summary>
        public bool EnableExternalIntegrations { get; set; } = false;
        /// <summary>
        /// Indicates whether advanced analytics are enabled. Default: false.
        /// </summary>
        public bool EnableAdvancedAnalytics { get; set; } = false;
    }
}