#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Events;

/// <summary>
/// Publisher for domain events using pub-sub pattern.
/// Decouples components that need to react to domain events.
/// Supports both synchronous and asynchronous event handlers.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : DomainEvent;
    void Subscribe<T>(IEventHandler<T> handler) where T : DomainEvent;
}

/// <summary>
/// Event handler interface for typed event handling.
/// Implementation classes handle specific event types.
/// </summary>
public interface IEventHandler<T> where T : DomainEvent
{
    Task HandleAsync(T @event, CancellationToken cancellationToken);
}

/// <summary>
/// In-memory event publisher for local event dispatching.
/// Suitable for single-process deployments; use message queue for distributed systems.
/// </summary>
public sealed class EventPublisher : IEventPublisher {
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(ILogger<EventPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes an event to all registered handlers.
    /// Handlers are invoked asynchronously in parallel for performance.
    /// Exceptions in handlers are logged but don't prevent other handlers from running.
    /// </summary>
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : DomainEvent
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(T);

        _logger.LogInformation(
            "Publishing event: {eventType} [EventId: {eventId}]",
            @event.EventType,
            @event.EventId);

        if (!_handlers.TryGetValue(eventType, out var handlerList))
        {
            _logger.LogDebug("No handlers registered for event type: {eventType}", @event.EventType);
            return;
        }

        var tasks = new List<Task>();

        foreach (var handler in handlerList)
        {
            try
            {
                // Invoke handler asynchronously
                if (handler is Func<T, CancellationToken, Task> asyncHandler)
                {
                    tasks.Add(asyncHandler(@event, cancellationToken));
                }
                else if (handler is Action<T> syncHandler)
                {
                    // Wrap synchronous handler as async task
                    tasks.Add(Task.Run(() => syncHandler(@event), cancellationToken));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking event handler for {eventType}", @event.EventType);
            }
        }

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogDebug("Event handlers completed for {eventType}", @event.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error waiting for event handlers: {eventType}", @event.EventType);
        }
    }

    /// <summary>
    /// Subscribes a handler to an event type.
    /// Multiple handlers can be subscribed to the same event.
    /// </summary>
    public void Subscribe<T>(IEventHandler<T> handler) where T : DomainEvent
    {
        if (handler is null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(T);

        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Delegate>();

        // Create async delegate from handler
        Func<T, CancellationToken, Task> asyncDelegate = (e, ct) => handler.HandleAsync(e, ct);
        _handlers[eventType].Add(asyncDelegate);

        _logger.LogInformation(
            "Handler subscribed to event: {eventType} [HandlerType: {handlerType}]",
            typeof(T).Name,
            handler.GetType().Name);
    }

    /// <summary>
    /// Gets the count of handlers registered for an event type.
    /// Useful for testing and debugging.
    /// </summary>
    public int GetHandlerCount<T>() where T : DomainEvent
    {
        var eventType = typeof(T);
        return _handlers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
    }
}

/// <summary>
/// Logger event handler that logs all domain events.
/// Useful for audit trails and event history tracking.
/// </summary>
public sealed class LoggingEventHandler<T> : IEventHandler<T> where T : DomainEvent {
    private readonly ILogger<LoggingEventHandler<T>> _logger;

    public LoggingEventHandler(ILogger<LoggingEventHandler<T>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Logs event details for audit trail.
    /// </summary>
    public Task HandleAsync(T @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Event occurred: {eventType} [EventId: {eventId}, OccurredAt: {time}]",
            @event.EventType,
            @event.EventId,
            @event.OccurredAt);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Configuration for event publishing behavior.
/// </summary>
public sealed class EventPublisherOptions {
    /// <summary>
    /// Enable async event publishing (fire and forget).
    /// Default: true (recommended for performance).
    /// </summary>
    public bool EnableAsyncPublishing { get; set; } = true;

    /// <summary>
    /// Maximum timeout for all handlers to complete (in seconds).
    /// Default: 30 seconds.
    /// </summary>
    public int HandlerTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Continue publishing even if a handler throws exception.
    /// Default: true (prevents one bad handler from blocking others).
    /// </summary>
    public bool ContinueOnHandlerException { get; set; } = true;
}
