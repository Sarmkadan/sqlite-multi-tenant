#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Events;

/// <summary>
/// Base class for domain events representing significant occurrences in the system.
/// Events enable loose coupling between components and support audit logging.
/// All events are immutable after creation.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>Gets the unique identifier of the event.</summary>
    public string EventId { get; } = Guid.NewGuid().ToString();
    /// <summary>Gets the UTC timestamp when the event occurred.</summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    /// <summary>Gets the type name of the event.</summary>
    public string EventType { get; }
    /// <summary>Gets or sets the tenant identifier associated with the event.</summary>
    public string? TenantId { get; set; }

    protected DomainEvent(string eventType)
    {
        EventType = eventType;
    }
}

/// <summary>
/// Event raised when a new tenant is created.
/// Triggers downstream actions (database setup, webhook notifications).
/// </summary>
public sealed class TenantCreatedEvent : DomainEvent {
    /// <summary>Gets or sets the tenant identifier.</summary>
    public string TenantId { get; set; }
    /// <summary>Gets or sets the tenant name.</summary>
    public string TenantName { get; set; }
    /// <summary>Gets or sets the contact email.</summary>
    public string ContactEmail { get; set; }

    public TenantCreatedEvent() : base(nameof(TenantCreatedEvent))
    {
    }
}

/// <summary>
/// Event raised when tenant is updated.
/// Useful for notification systems and audit trails.
/// </summary>
public sealed class TenantUpdatedEvent : DomainEvent {
    /// <summary>Gets or sets the tenant identifier.</summary>
    public string TenantId { get; set; }
    /// <summary>Gets or sets the old name.</summary>
    public string OldName { get; set; }
    /// <summary>Gets or sets the new name.</summary>
    public string NewName { get; set; }

    public TenantUpdatedEvent() : base(nameof(TenantUpdatedEvent))
    {
    }
}

/// <summary>
/// Event raised when tenant is suspended.
/// Triggers access cleanup and webhook notifications.
/// </summary>
public sealed class TenantSuspendedEvent : DomainEvent {
    /// <summary>Gets or sets the tenant identifier.</summary>
    public string TenantId { get; set; }
    /// <summary>Gets or sets the user who suspended the tenant.</summary>
    public string SuspendedBy { get; set; }
    /// <summary>Gets or sets the reason for suspension.</summary>
    public string Reason { get; set; }

    public TenantSuspendedEvent() : base(nameof(TenantSuspendedEvent))
    {
    }
}

/// <summary>
/// Event raised when backup starts.
/// Used for monitoring and webhook notifications.
/// </summary>
public sealed class BackupStartedEvent : DomainEvent {
    /// <summary>Gets or sets the backup identifier.</summary>
    public string BackupId { get; set; }
    /// <summary>Gets or sets the database identifier.</summary>
    public string DatabaseId { get; set; }
    /// <summary>Gets or sets the backup type.</summary>
    public string BackupType { get; set; }

    public BackupStartedEvent() : base(nameof(BackupStartedEvent))
    {
    }
}

/// <summary>
/// Event raised when backup completes successfully.
/// Triggers verification and retention policy checks.
/// </summary>
public sealed class BackupCompletedEvent : DomainEvent {
    /// <summary>Gets or sets the backup identifier.</summary>
    public string BackupId { get; set; }
    /// <summary>Gets or sets the database identifier.</summary>
    public string DatabaseId { get; set; }
    /// <summary>Gets or sets the backup size in bytes.</summary>
    public long SizeBytes { get; set; }
    /// <summary>Gets or sets the backup duration in milliseconds.</summary>
    public long DurationMilliseconds { get; set; }

    public BackupCompletedEvent() : base(nameof(BackupCompletedEvent))
    {
    }
}

/// <summary>
/// Event raised when backup fails.
/// Triggers alerts and retry mechanisms.
/// </summary>
public sealed class BackupFailedEvent : DomainEvent {
    /// <summary>Gets or sets the backup identifier.</summary>
    public string BackupId { get; set; }
    /// <summary>Gets or sets the database identifier.</summary>
    public string DatabaseId { get; set; }
    /// <summary>Gets or sets the error message.</summary>
    public string ErrorMessage { get; set; }

    public BackupFailedEvent() : base(nameof(BackupFailedEvent))
    {
    }
}

/// <summary>
/// Event raised when migration is applied.
/// Tracks schema evolution and enables rollback tracking.
/// </summary>
public sealed class MigrationAppliedEvent : DomainEvent {
    /// <summary>Gets or sets the migration identifier.</summary>
    public string MigrationId { get; set; }
    /// <summary>Gets or sets the database identifier.</summary>
    public string DatabaseId { get; set; }
    /// <summary>Gets or sets the version.</summary>
    public string Version { get; set; }
    /// <summary>Gets or sets the migration name.</summary>
    public string MigrationName { get; set; }

    public MigrationAppliedEvent() : base(nameof(MigrationAppliedEvent))
    {
    }
}

/// <summary>
/// Event raised when migration application fails.
/// Enables recovery procedures and notifications.
/// </summary>
public sealed class MigrationFailedEvent : DomainEvent {
    /// <summary>Gets or sets the migration identifier.</summary>
    public string MigrationId { get; set; }
    /// <summary>Gets or sets the database identifier.</summary>
    public string DatabaseId { get; set; }
    /// <summary>Gets or sets the version.</summary>
    public string Version { get; set; }
    /// <summary>Gets or sets the error message.</summary>
    public string ErrorMessage { get; set; }

    public MigrationFailedEvent() : base(nameof(MigrationFailedEvent))
    {
    }
}

/// <summary>
/// Event raised when migration is rolled back.
/// Important for audit and compliance tracking.
/// </summary>
public sealed class MigrationRolledBackEvent : DomainEvent {
    /// <summary>Gets or sets the migration identifier.</summary>
    public string MigrationId { get; set; }
    /// <summary>Gets or sets the database identifier.</summary>
    public string DatabaseId { get; set; }
    /// <summary>Gets or sets the version.</summary>
    public string Version { get; set; }
    /// <summary>Gets or sets the user who rolled back the migration.</summary>
    public string RolledBackBy { get; set; }

    public MigrationRolledBackEvent() : base(nameof(MigrationRolledBackEvent))
    {
    }
}

/// <summary>
/// Event raised when a tenant is cloned from another tenant.
/// Triggers downstream actions (audit logging, notifications).
/// </summary>
public sealed class TenantClonedEvent : DomainEvent
{
    /// <summary>Gets or sets the source tenant identifier.</summary>
    public string SourceTenantId { get; set; }
    /// <summary>Gets or sets the target tenant identifier.</summary>
    public string TargetTenantId { get; set; }
    /// <summary>Gets or sets the database path.</summary>
    public string DatabasePath { get; set; }

    public TenantClonedEvent() : base(nameof(TenantClonedEvent))
    {
    }
}

/// <summary>
/// Event raised when health check fails.
/// Enables alerting and incident response.
/// </summary>
public sealed class HealthCheckFailedEvent : DomainEvent {
    /// <summary>Gets or sets the component name.</summary>
    public string ComponentName { get; set; }
    /// <summary>Gets or sets the error message.</summary>
    public string ErrorMessage { get; set; }
    /// <summary>Gets or sets the response time in milliseconds.</summary>
    public long ResponseTimeMs { get; set; }

    public HealthCheckFailedEvent() : base(nameof(HealthCheckFailedEvent))
    {
    }
}

/// <summary>
/// Generic event for custom domain events.
/// Allows extensibility without modifying this class.
/// </summary>
public sealed class CustomDomainEvent : DomainEvent {
    /// <summary>Gets or sets the custom event data.</summary>
    public Dictionary<string, object> Data { get; set; } = new();

    public CustomDomainEvent(string eventType) : base(eventType)
    {
    }
}
