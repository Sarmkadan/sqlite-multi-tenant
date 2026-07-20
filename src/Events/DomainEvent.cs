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
    public string EventId { get; } = Guid.NewGuid().ToString();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType { get; }
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
    public string TenantId { get; set; }
    public string TenantName { get; set; }
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
    public string TenantId { get; set; }
    public string OldName { get; set; }
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
    public string TenantId { get; set; }
    public string SuspendedBy { get; set; }
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
    public string BackupId { get; set; }
    public string DatabaseId { get; set; }
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
    public string BackupId { get; set; }
    public string DatabaseId { get; set; }
    public long SizeBytes { get; set; }
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
    public string BackupId { get; set; }
    public string DatabaseId { get; set; }
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
    public string MigrationId { get; set; }
    public string DatabaseId { get; set; }
    public string Version { get; set; }
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
    public string MigrationId { get; set; }
    public string DatabaseId { get; set; }
    public string Version { get; set; }
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
    public string MigrationId { get; set; }
    public string DatabaseId { get; set; }
    public string Version { get; set; }
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
    public string SourceTenantId { get; set; }
    public string TargetTenantId { get; set; }
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
    public string ComponentName { get; set; }
    public string ErrorMessage { get; set; }
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
    public Dictionary<string, object> Data { get; set; } = new();

    public CustomDomainEvent(string eventType) : base(eventType)
    {
    }
}
