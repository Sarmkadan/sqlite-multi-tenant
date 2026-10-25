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

    /// <summary>Initializes a new instance of the <see cref="DomainEvent"/> class.</summary>
    /// <param name="eventType">The type name of the event.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> is null, empty, or white space.</exception>
    protected DomainEvent(string eventType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        EventType = eventType;
    }
}

/// <summary>
/// Event raised when a new tenant is created.
/// Triggers downstream actions (database setup, webhook notifications).
/// </summary>
public sealed class TenantCreatedEvent : DomainEvent
{
    /// <summary>Gets or sets the tenant identifier.</summary>
    public new string? TenantId { get; set; }
    /// <summary>Gets or sets the tenant name.</summary>
    public string TenantName { get; set; } = string.Empty;
    /// <summary>Gets or sets the contact email.</summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="TenantCreatedEvent"/> class.</summary>
    public TenantCreatedEvent() : base(nameof(TenantCreatedEvent))
    {
    }

    /// <summary>Returns a summary of the event without the contact email.</summary>
    public override string ToString() => $"TenantCreatedEvent {{ TenantId = {TenantId}, TenantName = {TenantName} }}";
}

/// <summary>
/// Event raised when tenant is updated.
/// Useful for notification systems and audit trails.
/// </summary>
public sealed class TenantUpdatedEvent : DomainEvent
{
    /// <summary>Gets or sets the tenant identifier.</summary>
    public new string? TenantId { get; set; }
    /// <summary>Gets or sets the old name.</summary>
    public string OldName { get; set; } = string.Empty;
    /// <summary>Gets or sets the new name.</summary>
    public string NewName { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="TenantUpdatedEvent"/> class.</summary>
    public TenantUpdatedEvent() : base(nameof(TenantUpdatedEvent))
    {
    }

    /// <summary>Returns a summary of the event including the tenant identifier and old and new names.</summary>
    public override string ToString() => $"TenantUpdatedEvent {{ TenantId = {TenantId}, OldName = {OldName}, NewName = {NewName} }}";
}

/// <summary>
/// Event raised when tenant is suspended.
/// Triggers access cleanup and webhook notifications.
/// </summary>
public sealed class TenantSuspendedEvent : DomainEvent
{
    /// <summary>Gets or sets the tenant identifier.</summary>
    public new string? TenantId { get; set; }
    /// <summary>Gets or sets the user who suspended the tenant.</summary>
    public string SuspendedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the reason for suspension.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="TenantSuspendedEvent"/> class.</summary>
    public TenantSuspendedEvent() : base(nameof(TenantSuspendedEvent))
    {
    }

    /// <summary>Returns a summary of the event including the tenant identifier, suspending user, and reason.</summary>
    public override string ToString() => $"TenantSuspendedEvent {{ TenantId = {TenantId}, SuspendedBy = {SuspendedBy}, Reason = {Reason} }}";
}

/// <summary>
/// Event raised when backup starts.
/// Used for monitoring and webhook notifications.
/// </summary>
public sealed class BackupStartedEvent : DomainEvent
{
    /// <summary>Gets or sets the backup identifier.</summary>
    public string BackupId { get; set; } = string.Empty;
    /// <summary>Gets or sets the database identifier.</summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>Gets or sets the backup type.</summary>
    public string BackupType { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="BackupStartedEvent"/> class.</summary>
    public BackupStartedEvent() : base(nameof(BackupStartedEvent))
    {
    }

    /// <summary>Returns a summary of the event including the backup and database identifiers and backup type.</summary>
    public override string ToString() => $"BackupStartedEvent {{ BackupId = {BackupId}, DatabaseId = {DatabaseId}, BackupType = {BackupType} }}";
}

/// <summary>
/// Event raised when backup completes successfully.
/// Triggers verification and retention policy checks.
/// </summary>
public sealed class BackupCompletedEvent : DomainEvent
{
    /// <summary>Gets or sets the backup identifier.</summary>
    public string BackupId { get; set; } = string.Empty;
    /// <summary>Gets or sets the database identifier.</summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>Gets or sets the backup size in bytes.</summary>
    public long SizeBytes { get; set; }
    /// <summary>Gets or sets the backup duration in milliseconds.</summary>
    public long DurationMilliseconds { get; set; }

    /// <summary>Initializes a new instance of the <see cref="BackupCompletedEvent"/> class.</summary>
    public BackupCompletedEvent() : base(nameof(BackupCompletedEvent))
    {
    }

    /// <summary>Returns a summary of the event including the backup and database identifiers, size, and duration.</summary>
    public override string ToString() => $"BackupCompletedEvent {{ BackupId = {BackupId}, DatabaseId = {DatabaseId}, SizeBytes = {SizeBytes}, DurationMilliseconds = {DurationMilliseconds} }}";
}

/// <summary>
/// Event raised when backup fails.
/// Triggers alerts and retry mechanisms.
/// </summary>
public sealed class BackupFailedEvent : DomainEvent
{
    /// <summary>Gets or sets the backup identifier.</summary>
    public string BackupId { get; set; } = string.Empty;
    /// <summary>Gets or sets the database identifier.</summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>Gets or sets the error message.</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="BackupFailedEvent"/> class.</summary>
    public BackupFailedEvent() : base(nameof(BackupFailedEvent))
    {
    }

    /// <summary>Returns a summary of the event including the backup and database identifiers and error message.</summary>
    public override string ToString() => $"BackupFailedEvent {{ BackupId = {BackupId}, DatabaseId = {DatabaseId}, ErrorMessage = {ErrorMessage} }}";
}

/// <summary>
/// Event raised when migration is applied.
/// Tracks schema evolution and enables rollback tracking.
/// </summary>
public sealed class MigrationAppliedEvent : DomainEvent
{
    /// <summary>Gets or sets the migration identifier.</summary>
    public string MigrationId { get; set; } = string.Empty;
    /// <summary>Gets or sets the database identifier.</summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>Gets or sets the version.</summary>
    public string Version { get; set; } = string.Empty;
    /// <summary>Gets or sets the migration name.</summary>
    public string MigrationName { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="MigrationAppliedEvent"/> class.</summary>
    public MigrationAppliedEvent() : base(nameof(MigrationAppliedEvent))
    {
    }

    /// <summary>Returns a summary of the event including the migration and database identifiers, version, and name.</summary>
    public override string ToString() => $"MigrationAppliedEvent {{ MigrationId = {MigrationId}, DatabaseId = {DatabaseId}, Version = {Version}, MigrationName = {MigrationName} }}";
}

/// <summary>
/// Event raised when migration application fails.
/// Enables recovery procedures and notifications.
/// </summary>
public sealed class MigrationFailedEvent : DomainEvent
{
    /// <summary>Gets or sets the migration identifier.</summary>
    public string MigrationId { get; set; } = string.Empty;
    /// <summary>Gets or sets the database identifier.</summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>Gets or sets the version.</summary>
    public string Version { get; set; } = string.Empty;
    /// <summary>Gets or sets the error message.</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="MigrationFailedEvent"/> class.</summary>
    public MigrationFailedEvent() : base(nameof(MigrationFailedEvent))
    {
    }

    /// <summary>Returns a summary of the event including the migration and database identifiers, version, and error message.</summary>
    public override string ToString() => $"MigrationFailedEvent {{ MigrationId = {MigrationId}, DatabaseId = {DatabaseId}, Version = {Version}, ErrorMessage = {ErrorMessage} }}";
}

/// <summary>
/// Event raised when migration is rolled back.
/// Important for audit and compliance tracking.
/// </summary>
public sealed class MigrationRolledBackEvent : DomainEvent
{
    /// <summary>Gets or sets the migration identifier.</summary>
    public string MigrationId { get; set; } = string.Empty;
    /// <summary>Gets or sets the database identifier.</summary>
    public string DatabaseId { get; set; } = string.Empty;
    /// <summary>Gets or sets the version.</summary>
    public string Version { get; set; } = string.Empty;
    /// <summary>Gets or sets the user who rolled back the migration.</summary>
    public string RolledBackBy { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="MigrationRolledBackEvent"/> class.</summary>
    public MigrationRolledBackEvent() : base(nameof(MigrationRolledBackEvent))
    {
    }

    /// <summary>Returns a summary of the event including the migration and database identifiers, version, and rolling-back user.</summary>
    public override string ToString() => $"MigrationRolledBackEvent {{ MigrationId = {MigrationId}, DatabaseId = {DatabaseId}, Version = {Version}, RolledBackBy = {RolledBackBy} }}";
}

/// <summary>
/// Event raised when a tenant is cloned from another tenant.
/// Triggers downstream actions (audit logging, notifications).
/// </summary>
public sealed class TenantClonedEvent : DomainEvent
{
    /// <summary>Gets or sets the source tenant identifier.</summary>
    public string SourceTenantId { get; set; } = string.Empty;
    /// <summary>Gets or sets the target tenant identifier.</summary>
    public string TargetTenantId { get; set; } = string.Empty;
    /// <summary>Gets or sets the database path.</summary>
    public string DatabasePath { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="TenantClonedEvent"/> class.</summary>
    public TenantClonedEvent() : base(nameof(TenantClonedEvent))
    {
    }

    /// <summary>Returns a summary of the event including the source and target tenant identifiers and database path.</summary>
    public override string ToString() => $"TenantClonedEvent {{ SourceTenantId = {SourceTenantId}, TargetTenantId = {TargetTenantId}, DatabasePath = {DatabasePath} }}";
}

/// <summary>
/// Event raised when health check fails.
/// Enables alerting and incident response.
/// </summary>
public sealed class HealthCheckFailedEvent : DomainEvent
{
    /// <summary>Gets or sets the component name.</summary>
    public string ComponentName { get; set; } = string.Empty;
    /// <summary>Gets or sets the error message.</summary>
    public string ErrorMessage { get; set; } = string.Empty;
    /// <summary>Gets or sets the response time in milliseconds.</summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>Initializes a new instance of the <see cref="HealthCheckFailedEvent"/> class.</summary>
    public HealthCheckFailedEvent() : base(nameof(HealthCheckFailedEvent))
    {
    }

    /// <summary>Returns a summary of the event including the component name, error message, and response time.</summary>
    public override string ToString() => $"HealthCheckFailedEvent {{ ComponentName = {ComponentName}, ErrorMessage = {ErrorMessage}, ResponseTimeMs = {ResponseTimeMs} }}";
}

/// <summary>
/// Generic event for custom domain events.
/// Allows extensibility without modifying this class.
/// </summary>
public sealed class CustomDomainEvent : DomainEvent
{
    /// <summary>Gets or sets the custom event data.</summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>Initializes a new instance of the <see cref="CustomDomainEvent"/> class.</summary>
    /// <param name="eventType">The type name of the custom event.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> is null, empty, or white space.</exception>
    public CustomDomainEvent(string eventType) : base(eventType)
    {
    }

    /// <summary>Returns a summary of the event including the event type, tenant identifier, and number of data entries.</summary>
    public override string ToString() => $"CustomDomainEvent {{ EventType = {EventType}, TenantId = {TenantId}, DataCount = {Data.Count} }}";
}
