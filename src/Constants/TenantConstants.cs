#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Constants;

/// <summary>
/// Constants related to tenant management and configuration
/// </summary>
public static class TenantConstants
{
    /// <summary>Format string for default database names.</summary>
    public const string DefaultDatabaseNameFormat = "{0}_db.sqlite";
    /// <summary>Default name for EF migrations table.</summary>
    public const string DefaultMigrationsTableName = "__EFMigrationsHistory";
    /// <summary>Extension for backup files.</summary>
    public const string BackupFileExtension = ".backup.sqlite";
    /// <summary>Default directory for backups.</summary>
    public const string DefaultBackupDirectory = "backups";
    /// <summary>Default directory for tenant databases.</summary>
    public const string DefaultDatabaseDirectory = "databases";

    /// <summary>Maximum allowed length for tenant names.</summary>
    public const int MaxTenantNameLength = 128;
    /// <summary>Maximum allowed length for tenant IDs.</summary>
    public const int MaxTenantIdLength = 36;
    /// <summary>Maximum allowed length for database paths.</summary>
    public const int MaxDatabasePathLength = 260;
    /// <summary>Maximum number of connection retries.</summary>
    public const int MaxConnectionRetries = 3;
    /// <summary>Default timeout for database connections in seconds.</summary>
    public const int DefaultConnectionTimeoutSeconds = 30;
    /// <summary>Number of days to retain backups.</summary>
    public const int BackupRetentionDays = 30;

    /// <summary>Claim type for tenant ID in authentication.</summary>
    public const string TenantIdClaimType = "tenant_id";
    /// <summary>Claim type for tenant name in authentication.</summary>
    public const string TenantNameClaimType = "tenant_name";

    /// <summary>Metadata key to mark a tenant as read-only.</summary>
    public const string ReadOnlyMetadataKey = "readOnly";
}