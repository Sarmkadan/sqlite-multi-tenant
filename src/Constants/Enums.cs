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
    Active = 0,
    Inactive = 1,
    Suspended = 2,
    Archived = 3,
    Deleted = 4
}

/// <summary>
/// Enumeration for database migration status
/// </summary>
public enum MigrationStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    RolledBack = 4
}

/// <summary>
/// Enumeration for backup status
/// </summary>
public enum BackupStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Verified = 4
}

/// <summary>
/// Enumeration for backup type
/// </summary>
public enum BackupType
{
    Full = 0,
    Incremental = 1,
    Differential = 2
}

/// <summary>
/// Enumeration for connection state
/// </summary>
public enum ConnectionState
{
    Closed = 0,
    Open = 1,
    Connecting = 2,
    Executing = 3,
    Fetching = 4,
    Broken = 5
}
