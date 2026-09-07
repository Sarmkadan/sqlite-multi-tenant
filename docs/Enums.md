# Enums and Constants Documentation

## Enums

### TenantStatus
- Active = 0
- Inactive = 1
- Suspended = 2
- Archived = 3
- Deleted = 4

### MigrationStatus
- Pending = 0
- Running = 1
- Completed = 2
- Failed = 3
- RolledBack = 4

### BackupStatus
- Pending = 0
- InProgress = 1
- Completed = 2
- Failed = 3
- Verified = 4

### BackupType
- Full = 0
- Incremental = 1
- Differential = 2

### ConnectionState
- Closed = 0
- Open = 1
- Connecting = 2
- Executing = 3
- Fetching = 4
- Broken = 5

## Constants

### TenantConstants
- DefaultDatabaseNameFormat = "{0}_db.sqlite"
- DefaultMigrationsTableName = "__EFMigrationsHistory"
- BackupFileExtension = ".backup.sqlite"
- DefaultBackupDirectory = "backups"
- DefaultDatabaseDirectory = "databases"
- MaxTenantNameLength = 128
- MaxTenantIdLength = 36
- MaxDatabasePathLength = 260
- MaxConnectionRetries = 3
- DefaultConnectionTimeoutSeconds = 30
- BackupRetentionDays = 30
- TenantIdClaimType = "tenant_id"
- TenantNameClaimType = "tenant_name"
- ReadOnlyMetadataKey = "readOnly"