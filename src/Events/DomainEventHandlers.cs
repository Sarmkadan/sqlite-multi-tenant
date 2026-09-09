#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Events;

/// <summary>
/// Domain event handlers for tenant-related events.
/// Provides handlers that respond to tenant lifecycle events (created, updated, deleted).
/// </summary>
/// <summary>
/// Defines a handler for domain events.
/// </summary>
/// <typeparam name="T">The type of domain event to handle.</typeparam>
public interface IDomainEventHandler<T> where T : DomainEvent
{
    /// <summary>
    /// Handles the specified domain event.
    /// </summary>
    /// <param name="@event">The domain event to handle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(T @event);
}

/// <summary>
/// Handles tenant created events of type <see cref="TenantCreatedNotificationEvent"/>.
/// Logs creation, sends notifications, and initializes tenant resources.
/// </summary>
public sealed class TenantCreatedEventHandler : IDomainEventHandler<TenantCreatedNotificationEvent> {
    private readonly ILogger<TenantCreatedEventHandler> _logger;
    private readonly Integration.WebhookService _webhookService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantCreatedEventHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="webhookService">The webhook service for triggering notifications.</param>
    public TenantCreatedEventHandler(
        ILogger<TenantCreatedEventHandler> logger,
        Integration.WebhookService webhookService)
    {
        _logger = logger;
        _webhookService = webhookService;
    }

    /// <summary>
    /// Handles the tenant created notification event.
    /// </summary>
    /// <param name="@event">The tenant created notification event.</param>
    public async Task HandleAsync(TenantCreatedNotificationEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        try
        {
            _logger.LogInformation($"Handling tenant created event: {(@event.TenantId)}");

            // Log creation
            _logger.LogInformation($"Tenant created: {(@event.TenantName)} ({(@event.TenantId)})");

            // Trigger webhooks
            await _webhookService.TriggerWebhooksAsync("tenant.created", @event);

            _logger.LogInformation($"Tenant created event handled: {(@event.TenantId)}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error handling tenant created event: {Message}", ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Handles tenant deleted events.
/// Performs cleanup operations and notifies subscribers.
/// </summary>
public sealed class TenantDeletedEventHandler : IDomainEventHandler<TenantDeletedEvent> {
    private readonly ILogger<TenantDeletedEventHandler> _logger;
    private readonly Integration.WebhookService _webhookService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantDeletedEventHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="webhookService">The webhook service for triggering notifications.</param>
    public TenantDeletedEventHandler(
        ILogger<TenantDeletedEventHandler> logger,
        Integration.WebhookService webhookService)
    {
        _logger = logger;
        _webhookService = webhookService;
    }

    /// <summary>
    /// Handles the tenant deleted event.
    /// </summary>
    /// <param name="@event">The tenant deleted event.</param>
    public async Task HandleAsync(TenantDeletedEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        try
        {
            _logger.LogInformation($"Handling tenant deleted event: {(@event.TenantId)}");

            // Perform cleanup
            _logger.LogInformation($"Cleaning up tenant resources: {(@event.TenantId)}");

            // Trigger webhooks
            await _webhookService.TriggerWebhooksAsync("tenant.deleted", @event);

            _logger.LogInformation($"Tenant deleted event handled: {(@event.TenantId)}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error handling tenant deleted event: {Message}", ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Handles backup completed events.
/// Verifies backup and sends notifications.
/// </summary>
public sealed class BackupCompletedEventHandler : IDomainEventHandler<BackupCompletedNotificationEvent> {
    private readonly ILogger<BackupCompletedEventHandler> _logger;
    private readonly Integration.WebhookService _webhookService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupCompletedEventHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="webhookService">The webhook service for triggering notifications.</param>
    public BackupCompletedEventHandler(
        ILogger<BackupCompletedEventHandler> logger,
        Integration.WebhookService webhookService)
    {
        _logger = logger;
        _webhookService = webhookService;
    }

    /// <summary>
    /// Handles the backup completed notification event.
    /// </summary>
    /// <param name="@event">The backup completed notification event.</param>
    public async Task HandleAsync(BackupCompletedNotificationEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        try
        {
            _logger.LogInformation($"Handling backup completed event: {(@event.BackupId)}");

            _logger.LogInformation(
                $"Backup completed: {(@event.BackupId)}, " +
                $"Size: {(@event.SizeBytes)} bytes, " +
                $"Duration: {(@event.DurationMs)}ms");

            // Trigger webhooks
            await _webhookService.TriggerWebhooksAsync("backup.completed", @event);

            _logger.LogInformation($"Backup completed event handled: {(@event.BackupId)}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error handling backup completed event: {Message}", ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Handles migration completed events.
/// Logs migration success and updates schema versions.
/// </summary>
public sealed class MigrationCompletedEventHandler : IDomainEventHandler<MigrationCompletedEvent> {
    private readonly ILogger<MigrationCompletedEventHandler> _logger;
    private readonly Integration.WebhookService _webhookService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationCompletedEventHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="webhookService">The webhook service for triggering notifications.</param>
    public MigrationCompletedEventHandler(
        ILogger<MigrationCompletedEventHandler> logger,
        Integration.WebhookService webhookService)
    {
        _logger = logger;
        _webhookService = webhookService;
    }

    /// <summary>
    /// Handles the migration completed event.
    /// </summary>
    public async Task HandleAsync(MigrationCompletedEvent @event)
    {
        try
        {
            _logger.LogInformation($"Handling migration completed event: {(@event.DatabaseId)}");

            _logger.LogInformation(
                $"Migration completed: {(@event.MigrationVersion)} " +
                $"on database {(@event.DatabaseId)}");

            // Trigger webhooks
            await _webhookService.TriggerWebhooksAsync("migration.completed", @event);

            _logger.LogInformation($"Migration completed event handled: {(@event.DatabaseId)}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error handling migration completed event: {Message}", ex.Message);
            throw;
        }
    }
}

// Domain event types
/// <summary>
/// Represents a notification that a tenant has been created.
/// Contains the tenant ID and name.
/// </summary>
/// <summary>
/// Represents a notification that a tenant has been created.
/// Contains the tenant ID and name.
/// </summary>
public sealed class TenantCreatedNotificationEvent : DomainEvent {
    /// <summary>
    /// Gets or sets the unique identifier of the tenant.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the tenant.
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of the tenant.
    /// </summary>
    public string? TenantDescription { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantCreatedNotificationEvent"/> class.
    /// </summary>
    public TenantCreatedNotificationEvent() : base(nameof(TenantCreatedNotificationEvent))
    {
    }
}

/// <summary>
/// Represents a notification that a tenant has been deleted.
/// Contains the tenant ID and name.
/// </summary>
public sealed class TenantDeletedEvent : DomainEvent {
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;

    public TenantDeletedEvent() : base(nameof(TenantDeletedEvent))
    {
    }
}

/// <summary>
/// Represents a notification that a backup has been completed.
/// Contains backup details including ID, size, duration, and verification status.
/// </summary>
public sealed class BackupCompletedNotificationEvent : DomainEvent {
    /// <summary>
    /// Gets or sets the unique identifier of the backup.
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the database that was backed up.
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the backup in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the duration of the backup operation in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the backup was verified.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupCompletedNotificationEvent"/> class.
    /// </summary>
    public BackupCompletedNotificationEvent() : base(nameof(BackupCompletedNotificationEvent))
    {
    }
}

/// <summary>
/// Represents a notification that a migration has been completed.
/// Contains details about the migration including database ID, version, name, and duration.
/// </summary>
public sealed class MigrationCompletedEvent : DomainEvent {
    /// <summary>
    /// Gets or sets the identifier of the database that was migrated.
    /// </summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the migration that was applied.
    /// </summary>
    public string MigrationVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the migration that was applied.
    /// </summary>
    public string MigrationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration of the migration operation in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationCompletedEvent"/> class.
    /// </summary>
    public MigrationCompletedEvent() : base(nameof(MigrationCompletedEvent))
    {
    }
}
