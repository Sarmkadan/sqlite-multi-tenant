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
    public const string DefaultDatabaseNameFormat = "{0}_db.sqlite";
    public const string DefaultMigrationsTableName = "__EFMigrationsHistory";
    public const string BackupFileExtension = ".backup.sqlite";
    public const string DefaultBackupDirectory = "backups";
    public const string DefaultDatabaseDirectory = "databases";

    public const int MaxTenantNameLength = 128;
    public const int MaxTenantIdLength = 36;
    public const int MaxDatabasePathLength = 260;
    public const int MaxConnectionRetries = 3;
    public const int DefaultConnectionTimeoutSeconds = 30;
    public const int BackupRetentionDays = 30;

    public const string TenantIdClaimType = "tenant_id";
    public const string TenantNameClaimType = "tenant_name";
}
