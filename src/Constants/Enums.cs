#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Constants;

/// <summary>
/// Enumeration for tenant status states
/// </summary>
public enum TenantStatus
{
    /// <summary>The tenant is active.</summary>
    Active = 0,
    /// <summary>The tenant is inactive.</summary>
    Inactive = 1,
    /// <summary>The tenant is suspended.</summary>
    Suspended = 2,
    /// <summary>The tenant is archived.</summary>
    Archived = 3,
    /// <summary>The tenant is deleted.</summary>
    Deleted = 4
}

/// <summary>
/// Enumeration for database migration status
/// </summary>
public enum MigrationStatus
{
    /// <summary>The migration is pending.</summary>
    Pending = 0,
    /// <summary>The migration is running.</summary>
    Running = 1,
    /// <summary>The migration completed successfully.</summary>
    Completed = 2,
    /// <summary>The migration failed.</summary>
    Failed = 3,
    /// <summary>The migration was rolled back.</summary>
    RolledBack = 4
}

/// <summary>
/// Enumeration for backup status
/// </summary>
public enum BackupStatus
{
    /// <summary>The backup is pending.</summary>
    Pending = 0,
    /// <summary>The backup is in progress.</summary>
    InProgress = 1,
    /// <summary>The backup completed successfully.</summary>
    Completed = 2,
    /// <summary>The backup failed.</summary>
    Failed = 3,
    /// <summary>The backup has been verified.</summary>
    Verified = 4
}

/// <summary>
/// Enumeration for backup type
/// </summary>
public enum BackupType
{
    /// <summary>A full backup.</summary>
    Full = 0,
    /// <summary>An incremental backup.</summary>
    Incremental = 1,
    /// <summary>A differential backup.</summary>
    Differential = 2
}

/// <summary>
/// Enumeration for connection state
/// </summary>
public enum ConnectionState
{
    /// <summary>The connection is closed.</summary>
    Closed = 0,
    /// <summary>The connection is open.</summary>
    Open = 1,
    /// <summary>The connection is being established.</summary>
    Connecting = 2,
    /// <summary>The connection is executing a command.</summary>
    Executing = 3,
    /// <summary>The connection is fetching data.</summary>
    Fetching = 4,
    /// <summary>The connection is broken.</summary>
    Broken = 5
}
