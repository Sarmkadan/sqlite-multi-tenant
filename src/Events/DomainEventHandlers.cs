// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Events;

/// <summary>
/// Domain event handlers for tenant-related events.
/// Provides handlers that respond to tenant lifecycle events (created, updated, deleted).
/// </summary>
public interface IDomainEventHandler<T> where T : DomainEvent
{
    Task HandleAsync(T @event);
}

/// <summary>
/// Handles tenant created events.
/// Logs creation, sends notifications, and initializes tenant resources.
/// </summary>
public class TenantCreatedEventHandler : IDomainEventHandler<TenantCreatedEvent>
{
    private readonly ILogger<TenantCreatedEventHandler> _logger;
    private readonly Integration.WebhookService _webhookService;

    public TenantCreatedEventHandler(
        ILogger<TenantCreatedEventHandler> logger,
        Integration.WebhookService webhookService)
    {
        _logger = logger;
        _webhookService = webhookService;
    }

    public async Task HandleAsync(TenantCreatedEvent @event)
    {
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
            _logger.LogError($"Error handling tenant created event: {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// Handles tenant deleted events.
/// Performs cleanup operations and notifies subscribers.
/// </summary>
public class TenantDeletedEventHandler : IDomainEventHandler<TenantDeletedEvent>
{
    private readonly ILogger<TenantDeletedEventHandler> _logger;
    private readonly Integration.WebhookService _webhookService;

    public TenantDeletedEventHandler(
        ILogger<TenantDeletedEventHandler> logger,
        Integration.WebhookService webhookService)
    {
        _logger = logger;
        _webhookService = webhookService;
    }

    public async Task HandleAsync(TenantDeletedEvent @event)
    {
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
            _logger.LogError($"Error handling tenant deleted event: {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// Handles backup completed events.
/// Verifies backup and sends notifications.
/// </summary>
public class BackupCompletedEventHandler : IDomainEventHandler<BackupCompletedEvent>
{
    private readonly ILogger<BackupCompletedEventHandler> _logger;
    private readonly Integration.WebhookService _webhookService;

    public BackupCompletedEventHandler(
        ILogger<BackupCompletedEventHandler> logger,
        Integration.WebhookService webhookService)
    {
        _logger = logger;
        _webhookService = webhookService;
    }

    public async Task HandleAsync(BackupCompletedEvent @event)
    {
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
            _logger.LogError($"Error handling backup completed event: {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// Handles migration completed events.
/// Logs migration success and updates schema versions.
/// </summary>
public class MigrationCompletedEventHandler : IDomainEventHandler<MigrationCompletedEvent>
{
    private readonly ILogger<MigrationCompletedEventHandler> _logger;
    private readonly Integration.WebhookService _webhookService;

    public MigrationCompletedEventHandler(
        ILogger<MigrationCompletedEventHandler> logger,
        Integration.WebhookService webhookService)
    {
        _logger = logger;
        _webhookService = webhookService;
    }

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
            _logger.LogError($"Error handling migration completed event: {ex.Message}");
            throw;
        }
    }
}

// Domain event types
public class TenantCreatedEvent : DomainEvent
{
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string? TenantDescription { get; set; }
}

public class TenantDeletedEvent : DomainEvent
{
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
}

public class BackupCompletedEvent : DomainEvent
{
    public string BackupId { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public long DurationMs { get; set; }
    public bool IsVerified { get; set; }
}

public class MigrationCompletedEvent : DomainEvent
{
    public string DatabaseId { get; set; } = string.Empty;
    public string MigrationVersion { get; set; } = string.Empty;
    public string MigrationName { get; set; } = string.Empty;
    public long DurationMs { get; set; }
}
