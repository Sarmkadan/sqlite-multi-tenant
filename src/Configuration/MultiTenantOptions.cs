// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace SqliteMultiTenant.Configuration
{
    // Configuration options for multi-tenant SQLite setup
    public class MultiTenantOptions
    {
        public string BasePath { get; set; } = "./databases";
        public int MaxConnectionsPerTenant { get; set; } = 10;
        public int MaxBackupCount { get; set; } = 20;
        public TimeSpan BackupRetention { get; set; } = TimeSpan.FromDays(30);
        public bool EnableBackupScheduling { get; set; } = true;
        public TimeSpan BackupInterval { get; set; } = TimeSpan.FromHours(1);
        public bool EnableAuditLogging { get; set; } = true;
        public bool EnablePerformanceMonitoring { get; set; } = true;
        public bool EnableDataEncryption { get; set; } = false;
        public int MaxCacheSize { get; set; } = 1000;
        public TimeSpan DefaultCacheTTL { get; set; } = TimeSpan.FromHours(1);
        public int RateLimitRequestsPerMinute { get; set; } = 1000;
        public string EncryptionKeyPath { get; set; } = "./keys";
        public bool VerboseLogging { get; set; } = false;
    }

    // Backup-specific configuration
    public class BackupOptions
    {
        public string BackupPath { get; set; } = "./backups";
        public int MaxConcurrentBackups { get; set; } = 3;
        public int BackupTimeoutSeconds { get; set; } = 300;
        public bool CompressBackups { get; set; } = true;
        public bool VerifyBackupIntegrity { get; set; } = true;
        public bool RetainDifferentialBackups { get; set; } = true;
    }

    // Monitoring and alerting configuration
    public class MonitoringOptions
    {
        public bool Enabled { get; set; } = true;
        public TimeSpan MetricsCollectionInterval { get; set; } = TimeSpan.FromSeconds(60);
        public int MaxMetricsHistory { get; set; } = 1000;
        public long SlowQueryThresholdMs { get; set; } = 1000;
        public Dictionary<string, AlertThreshold> AlertThresholds { get; set; } =
            new Dictionary<string, AlertThreshold>();
    }

    // Alert threshold configuration
    public class AlertThreshold
    {
        public string MetricName { get; set; }
        public double WarningLevel { get; set; }
        public double CriticalLevel { get; set; }
        public bool EnableEmail { get; set; }
        public bool EnableLog { get; set; }
    }

    // Security configuration
    public class SecurityOptions
    {
        public bool RequireTenantIdValidation { get; set; } = true;
        public bool EnableQueryLogging { get; set; } = false;
        public bool EnableConnectionEncryption { get; set; } = false;
        public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(1);
        public int MaxFailedLoginAttempts { get; set; } = 5;
        public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
        public bool RequireStrongPasswords { get; set; } = false;
    }

    // Database maintenance configuration
    public class MaintenanceOptions
    {
        public bool EnableAutomaticVacuum { get; set; } = true;
        public TimeSpan VacuumInterval { get; set; } = TimeSpan.FromDays(1);
        public bool EnableIndexRebuild { get; set; } = true;
        public TimeSpan IndexRebuildInterval { get; set; } = TimeSpan.FromDays(7);
        public bool EnableStatisticsUpdate { get; set; } = true;
        public TimeSpan StatisticsUpdateInterval { get; set; } = TimeSpan.FromDays(1);
    }

    // Feature flags
    public class FeatureFlags
    {
        public bool EnableCaching { get; set; } = true;
        public bool EnableBatching { get; set; } = true;
        public bool EnableAsyncOperations { get; set; } = true;
        public bool EnableWebhooks { get; set; } = false;
        public bool EnableExternalIntegrations { get; set; } = false;
        public bool EnableAdvancedAnalytics { get; set; } = false;
    }
}
