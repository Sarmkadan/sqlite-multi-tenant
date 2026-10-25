#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace SqliteMultiTenant.Configuration;

// Configuration options for multi-tenant SQLite setup
/// <summary>
/// Configuration options for multi-tenant SQLite setup.
/// </summary>
public sealed class MultiTenantOptions
{
    /// <summary>
    /// Default base path where tenant databases are stored.
    /// </summary>
    public const string DefaultBasePath = "./databases";
    /// <summary>
    /// Default maximum number of connections allowed per tenant.
    /// </summary>
    public const int DefaultMaxConnectionsPerTenant = 10;
    /// <summary>
    /// Default maximum number of connections for a tenant if not overridden.
    /// </summary>
    public const int DefaultMaxConnectionsFallback = 10;
    /// <summary>
    /// Default maximum number of backup files to retain per tenant.
    /// </summary>
    public const int DefaultMaxBackupCount = 20;
    /// <summary>
    /// Default time span after which backups are deleted (30 days).
    /// </summary>
    public static readonly TimeSpan DefaultBackupRetention = TimeSpan.FromDays(30);
    /// <summary>
    /// Default value indicating whether backup scheduling is enabled.
    /// </summary>
    public const bool DefaultEnableBackupScheduling = true;
    /// <summary>
    /// Default interval between automatic backups (1 hour).
    /// </summary>
    public static readonly TimeSpan DefaultBackupInterval = TimeSpan.FromHours(1);
    /// <summary>
    /// Default value indicating whether audit logging is enabled.
    /// </summary>
    public const bool DefaultEnableAuditLogging = true;
    /// <summary>
    /// Default value indicating whether performance monitoring is enabled.
    /// </summary>
    public const bool DefaultEnablePerformanceMonitoring = true;
    /// <summary>
    /// Default value indicating whether data encryption is enabled.
    /// </summary>
    public const bool DefaultEnableDataEncryption = false;
    /// <summary>
    /// Default maximum number of cache entries.
    /// </summary>
    public const int DefaultMaxCacheSize = 1000;
    /// <summary>
    /// Default time-to-live for cache entries (1 hour).
    /// </summary>
    public static readonly TimeSpan DefaultCacheTimeToLive = TimeSpan.FromHours(1);
    /// <summary>
    /// Default number of rate limit requests allowed per minute.
    /// </summary>
    public const int DefaultRateLimitRequestsPerMinute = 1000;
    /// <summary>
    /// Default path to the encryption key file.
    /// </summary>
    public const string DefaultEncryptionKeyPath = "./keys";
    /// <summary>
    /// Default value indicating whether verbose logging is enabled.
    /// </summary>
    public const bool DefaultVerboseLogging = false;

    /// <summary>
    /// The base path where tenant databases are stored. Default: "./databases".
    /// </summary>
    public string BasePath { get; set; } = DefaultBasePath;
    /// <summary>
    /// Maximum number of connections allowed per tenant. Default: 10.
    /// </summary>
    public int MaxConnectionsPerTenant { get; set; } = DefaultMaxConnectionsPerTenant;
    /// <summary>
    /// Default maximum number of connections for a tenant if not overridden. Default: 10.
    /// </summary>
    public int DefaultMaxConnections { get; set; } = DefaultMaxConnectionsFallback;
    /// <summary>
    /// Maximum number of backup files to retain per tenant. Default: 20.
    /// </summary>
    public int MaxBackupCount { get; set; } = DefaultMaxBackupCount;
    /// <summary>
    /// Time span after which backups are deleted. Default: 30 days.
    /// </summary>
    public TimeSpan BackupRetention { get; set; } = DefaultBackupRetention;
    /// <summary>
    /// Indicates whether backup scheduling is enabled. Default: true.
    /// </summary>
    public bool EnableBackupScheduling { get; set; } = DefaultEnableBackupScheduling;
    /// <summary>
    /// Interval between automatic backups. Default: 1 hour.
    /// </summary>
    public TimeSpan BackupInterval { get; set; } = DefaultBackupInterval;
    /// <summary>
    /// Indicates whether audit logging is enabled. Default: true.
    /// </summary>
    public bool EnableAuditLogging { get; set; } = DefaultEnableAuditLogging;
    /// <summary>
    /// Indicates whether performance monitoring is enabled. Default: true.
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = DefaultEnablePerformanceMonitoring;
    /// <summary>
    /// Indicates whether data encryption is enabled. Default: false.
    /// </summary>
    public bool EnableDataEncryption { get; set; } = DefaultEnableDataEncryption;
    /// <summary>
    /// Maximum number of cache entries. Default: 1000.
    /// </summary>
    public int MaxCacheSize { get; set; } = DefaultMaxCacheSize;
    /// <summary>
    /// Default time-to-live for cache entries. Default: 1 hour.
    /// </summary>
    public TimeSpan DefaultCacheTTL { get; set; } = DefaultCacheTimeToLive;
    /// <summary>
    /// Number of rate limit requests allowed per minute. Default: 1000.
    /// </summary>
    public int RateLimitRequestsPerMinute { get; set; } = DefaultRateLimitRequestsPerMinute;
    /// <summary>
    /// Path to the encryption key file. Default: "./keys".
    /// </summary>
    public string EncryptionKeyPath { get; set; } = DefaultEncryptionKeyPath;
    /// <summary>
    /// Indicates whether verbose logging is enabled. Default: false.
    /// </summary>
    public bool VerboseLogging { get; set; } = DefaultVerboseLogging;

    public override string ToString() =>
        $"MultiTenantOptions {{ BasePath = {BasePath}, MaxConnectionsPerTenant = {MaxConnectionsPerTenant}, DefaultMaxConnections = {DefaultMaxConnections}, MaxBackupCount = {MaxBackupCount}, BackupRetention = {BackupRetention}, EnableBackupScheduling = {EnableBackupScheduling} }}";
}

// Backup-specific configuration
/// <summary>
/// Backup-specific configuration.
/// </summary>
public sealed class BackupOptions
{
    /// <summary>
    /// Default path where backups are stored.
    /// </summary>
    public const string DefaultBackupPath = "./backups";
    /// <summary>
    /// Default maximum number of concurrent backup operations.
    /// </summary>
    public const int DefaultMaxConcurrentBackups = 3;
    /// <summary>
    /// Default timeout in seconds for backup operations.
    /// </summary>
    public const int DefaultBackupTimeoutSeconds = 300;
    /// <summary>
    /// Default value indicating whether backups are compressed.
    /// </summary>
    public const bool DefaultCompressBackups = true;
    /// <summary>
    /// Default value indicating whether backup integrity is verified after creation.
    /// </summary>
    public const bool DefaultVerifyBackupIntegrity = true;
    /// <summary>
    /// Default value indicating whether differential backups are retained.
    /// </summary>
    public const bool DefaultRetainDifferentialBackups = true;

    /// <summary>
    /// The path where backups are stored. Default: "./backups".
    /// </summary>
    public string BackupPath { get; set; } = DefaultBackupPath;
    /// <summary>
    /// Maximum number of concurrent backup operations. Default: 3.
    /// </summary>
    public int MaxConcurrentBackups { get; set; } = DefaultMaxConcurrentBackups;
    /// <summary>
    /// Timeout in seconds for backup operations. Default: 300.
    /// </summary>
    public int BackupTimeoutSeconds { get; set; } = DefaultBackupTimeoutSeconds;
    /// <summary>
    /// Indicates whether backups are compressed. Default: true.
    /// </summary>
    public bool CompressBackups { get; set; } = DefaultCompressBackups;
    /// <summary>
    /// Indicates whether backup integrity is verified after creation. Default: true.
    /// </summary>
    public bool VerifyBackupIntegrity { get; set; } = DefaultVerifyBackupIntegrity;
    /// <summary>
    /// Indicates whether differential backups are retained. Default: true.
    /// </summary>
    public bool RetainDifferentialBackups { get; set; } = DefaultRetainDifferentialBackups;
}

// Monitoring and alerting configuration
/// <summary>
/// Monitoring and alerting configuration.
/// </summary>
public sealed class MonitoringOptions
{
    /// <summary>
    /// Default value indicating whether monitoring is enabled.
    /// </summary>
    public const bool DefaultEnabled = true;
    /// <summary>
    /// Default interval at which metrics are collected (60 seconds).
    /// </summary>
    public static readonly TimeSpan DefaultMetricsCollectionInterval = TimeSpan.FromSeconds(60);
    /// <summary>
    /// Default maximum number of metric history entries to retain.
    /// </summary>
    public const int DefaultMaxMetricsHistory = 1000;
    /// <summary>
    /// Default threshold in milliseconds for a query to be considered slow.
    /// </summary>
    public const long DefaultSlowQueryThresholdMs = 1000;

    /// <summary>
    /// Indicates whether monitoring is enabled. Default: true.
    /// </summary>
    public bool Enabled { get; set; } = DefaultEnabled;
    /// <summary>
    /// Interval at which metrics are collected. Default: 60 seconds.
    /// </summary>
    public TimeSpan MetricsCollectionInterval { get; set; } = DefaultMetricsCollectionInterval;
    /// <summary>
    /// Maximum number of metric history entries to retain. Default: 1000.
    /// </summary>
    public int MaxMetricsHistory { get; set; } = DefaultMaxMetricsHistory;
    /// <summary>
    /// Threshold in milliseconds for a query to be considered slow. Default: 1000.
    /// </summary>
    public long SlowQueryThresholdMs { get; set; } = DefaultSlowQueryThresholdMs;
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
public sealed class AlertThreshold
{
    /// <summary>
    /// The name of the metric to monitor.
    /// </summary>
    public string MetricName { get; set; } = string.Empty;
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
public sealed class SecurityOptions
{
    /// <summary>
    /// Default value indicating whether tenant ID validation is required.
    /// </summary>
    public const bool DefaultRequireTenantIdValidation = true;
    /// <summary>
    /// Default value indicating whether query logging is enabled.
    /// </summary>
    public const bool DefaultEnableQueryLogging = false;
    /// <summary>
    /// Default value indicating whether connection encryption is enabled.
    /// </summary>
    public const bool DefaultEnableConnectionEncryption = false;
    /// <summary>
    /// Default timeout for user sessions (1 hour).
    /// </summary>
    public static readonly TimeSpan DefaultSessionTimeout = TimeSpan.FromHours(1);
    /// <summary>
    /// Default maximum number of failed login attempts before lockout.
    /// </summary>
    public const int DefaultMaxFailedLoginAttempts = 5;
    /// <summary>
    /// Default duration of account lockout after too many failed attempts (15 minutes).
    /// </summary>
    public static readonly TimeSpan DefaultLockoutDuration = TimeSpan.FromMinutes(15);
    /// <summary>
    /// Default value indicating whether strong passwords are required.
    /// </summary>
    public const bool DefaultRequireStrongPasswords = false;

    /// <summary>
    /// Indicates whether tenant ID validation is required. Default: true.
    /// </summary>
    public bool RequireTenantIdValidation { get; set; } = DefaultRequireTenantIdValidation;
    /// <summary>
    /// Indicates whether query logging is enabled. Default: false.
    /// </summary>
    public bool EnableQueryLogging { get; set; } = DefaultEnableQueryLogging;
    /// <summary>
    /// Indicates whether connection encryption is enabled. Default: false.
    /// </summary>
    public bool EnableConnectionEncryption { get; set; } = DefaultEnableConnectionEncryption;
    /// <summary>
    /// Timeout for user sessions. Default: 1 hour.
    /// </summary>
    public TimeSpan SessionTimeout { get; set; } = DefaultSessionTimeout;
    /// <summary>
    /// Maximum number of failed login attempts before lockout. Default: 5.
    /// </summary>
    public int MaxFailedLoginAttempts { get; set; } = DefaultMaxFailedLoginAttempts;
    /// <summary>
    /// Duration of account lockout after too many failed attempts. Default: 15 minutes.
    /// </summary>
    public TimeSpan LockoutDuration { get; set; } = DefaultLockoutDuration;
    /// <summary>
    /// Indicates whether strong passwords are required. Default: false.
    /// </summary>
    public bool RequireStrongPasswords { get; set; } = DefaultRequireStrongPasswords;
}

// Database maintenance configuration
/// <summary>
/// Database maintenance configuration.
/// </summary>
public sealed class MaintenanceOptions
{
    /// <summary>
    /// Default value indicating whether automatic vacuum is enabled.
    /// </summary>
    public const bool DefaultEnableAutomaticVacuum = true;
    /// <summary>
    /// Default interval at which automatic vacuum runs (1 day).
    /// </summary>
    public static readonly TimeSpan DefaultVacuumInterval = TimeSpan.FromDays(1);
    /// <summary>
    /// Default value indicating whether index rebuild is enabled.
    /// </summary>
    public const bool DefaultEnableIndexRebuild = true;
    /// <summary>
    /// Default interval at which index rebuild runs (7 days).
    /// </summary>
    public static readonly TimeSpan DefaultIndexRebuildInterval = TimeSpan.FromDays(7);
    /// <summary>
    /// Default value indicating whether statistics update is enabled.
    /// </summary>
    public const bool DefaultEnableStatisticsUpdate = true;
    /// <summary>
    /// Default interval at which statistics update runs (1 day).
    /// </summary>
    public static readonly TimeSpan DefaultStatisticsUpdateInterval = TimeSpan.FromDays(1);

    /// <summary>
    /// Indicates whether automatic vacuum is enabled. Default: true.
    /// </summary>
    public bool EnableAutomaticVacuum { get; set; } = DefaultEnableAutomaticVacuum;
    /// <summary>
    /// Interval at which automatic vacuum runs. Default: 1 day.
    /// </summary>
    public TimeSpan VacuumInterval { get; set; } = DefaultVacuumInterval;
    /// <summary>
    /// Indicates whether index rebuild is enabled. Default: true.
    /// </summary>
    public bool EnableIndexRebuild { get; set; } = DefaultEnableIndexRebuild;
    /// <summary>
    /// Interval at which index rebuild runs. Default: 7 days.
    /// </summary>
    public TimeSpan IndexRebuildInterval { get; set; } = DefaultIndexRebuildInterval;
    /// <summary>
    /// Indicates whether statistics update is enabled. Default: true.
    /// </summary>
    public bool EnableStatisticsUpdate { get; set; } = DefaultEnableStatisticsUpdate;
    /// <summary>
    /// Interval at which statistics update runs. Default: 1 day.
    /// </summary>
    public TimeSpan StatisticsUpdateInterval { get; set; } = DefaultStatisticsUpdateInterval;
}

// Feature flags
/// <summary>
/// Feature flags.
/// </summary>
public sealed class FeatureFlags
{
    /// <summary>
    /// Default value indicating whether caching is enabled.
    /// </summary>
    public const bool DefaultEnableCaching = true;
    /// <summary>
    /// Default value indicating whether batching is enabled.
    /// </summary>
    public const bool DefaultEnableBatching = true;
    /// <summary>
    /// Default value indicating whether asynchronous operations are enabled.
    /// </summary>
    public const bool DefaultEnableAsyncOperations = true;
    /// <summary>
    /// Default value indicating whether webhooks are enabled.
    /// </summary>
    public const bool DefaultEnableWebhooks = false;
    /// <summary>
    /// Default value indicating whether external integrations are enabled.
    /// </summary>
    public const bool DefaultEnableExternalIntegrations = false;
    /// <summary>
    /// Default value indicating whether advanced analytics are enabled.
    /// </summary>
    public const bool DefaultEnableAdvancedAnalytics = false;

    /// <summary>
    /// Indicates whether caching is enabled. Default: true.
    /// </summary>
    public bool EnableCaching { get; set; } = DefaultEnableCaching;
    /// <summary>
    /// Indicates whether batching is enabled. Default: true.
    /// </summary>
    public bool EnableBatching { get; set; } = DefaultEnableBatching;
    /// <summary>
    /// Indicates whether asynchronous operations are enabled. Default: true.
    /// </summary>
    public bool EnableAsyncOperations { get; set; } = DefaultEnableAsyncOperations;
    /// <summary>
    /// Indicates whether webhooks are enabled. Default: false.
    /// </summary>
    public bool EnableWebhooks { get; set; } = DefaultEnableWebhooks;
    /// <summary>
    /// Indicates whether external integrations are enabled. Default: false.
    /// </summary>
    public bool EnableExternalIntegrations { get; set; } = DefaultEnableExternalIntegrations;
    /// <summary>
    /// Indicates whether advanced analytics are enabled. Default: false.
    /// </summary>
    public bool EnableAdvancedAnalytics { get; set; } = DefaultEnableAdvancedAnalytics;
}
