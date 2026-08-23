#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Events;

/// <summary>
/// Advanced event bus implementing pub-sub pattern with async event handling.
/// Supports event filtering, dead letter queue, and error handling.
/// Provides centralized event management for the entire system.
/// </summary>
public interface IEventBus
{
    /// <summary>Publishes an event to all registered subscribers.</summary>
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : DomainEvent;
    /// <summary>Registers an event handler for a specific event type.</summary>
    Task SubscribeAsync<T>(Func<T, Task> handler) where T : DomainEvent;
    /// <summary>Unregisters an event handler for a specific event type.</summary>
    Task UnsubscribeAsync<T>(Func<T, Task> handler) where T : DomainEvent;
}

public sealed class EventBus : IEventBus {
    private readonly Dictionary<Type, List<Delegate>> _subscribers;
    private readonly ILogger<EventBus> _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly DeadLetterQueue _deadLetterQueue;

    /// <summary>
/// Initializes a new instance of the <see cref="EventBus"/> class.
/// </summary>
/// <param name="logger">The logger instance for recording operational events and errors.</param>
public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger;
        _subscribers = new Dictionary<Type, List<Delegate>>();
        _semaphore = new SemaphoreSlim(1);
        _deadLetterQueue = new DeadLetterQueue();
    }

    /// <summary>
    /// Publishes an event to all registered subscribers.
    /// Executes handlers in parallel with error isolation.
    /// </summary>
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : DomainEvent
    {
        try
        {
            var eventType = typeof(T);

            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                _logger.LogWarning("No subscribers for event: {Name}", eventType.Name);
                return;
            }

            _logger.LogInformation("Publishing event: {Name}", eventType.Name);

            var tasks = handlers.Select(async handler =>
            {
                try
                {
                    var handleMethod = (Func<T, Task>)handler;
                    await handleMethod(@event);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Event handler failed for {Name}: {Message}", eventType.Name, ex.Message);
                    await _deadLetterQueue.EnqueueAsync(@event, ex);
                }
            });

            await Task.WhenAll(tasks);

            _logger.LogInformation("Event published successfully: {Name}", eventType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError("Event publication failed: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Registers an event handler for a specific event type.
    /// Handlers are called when events of that type are published.
    /// </summary>
    public async Task SubscribeAsync<T>(Func<T, Task> handler) where T : DomainEvent
    {
        try
        {
            await _semaphore.WaitAsync();

            var eventType = typeof(T);

            if (!_subscribers.ContainsKey(eventType))
                _subscribers[eventType] = new List<Delegate>();

            _subscribers[eventType].Add(handler);

            _logger.LogInformation(
                $"Event handler registered for {eventType.Name}, " +
                $"Total handlers: {_subscribers[eventType].Count}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Unregisters an event handler.
    /// </summary>
    public async Task UnsubscribeAsync<T>(Func<T, Task> handler) where T : DomainEvent
    {
        try
        {
            await _semaphore.WaitAsync();

            var eventType = typeof(T);

            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
                _logger.LogInformation("Event handler unregistered for {Name}", eventType.Name);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets the number of subscribers for an event type.
    /// </summary>
    public int GetSubscriberCount<T>() where T : DomainEvent
    {
        var eventType = typeof(T);
        return _subscribers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
    }

    /// <summary>
    /// Gets dead letter queue for failed events.
    /// </summary>
    public DeadLetterQueue GetDeadLetterQueue() => _deadLetterQueue;
}

/// <summary>
/// Dead letter queue for events that failed to be processed.
/// Stores failed events and exceptions for later analysis and retry.
/// </summary>
public sealed class DeadLetterQueue {
    private readonly List<FailedEvent> _failedEvents;
    private readonly SemaphoreSlim _semaphore;
    private const int MaxQueueSize = 1000;

    public DeadLetterQueue()
    {
        _failedEvents = new List<FailedEvent>();
        _semaphore = new SemaphoreSlim(1);
    }

    /// <summary>
    /// Enqueues a failed event to the dead letter queue.
    /// </summary>
    public async Task EnqueueAsync<T>(T @event, Exception exception) where T : DomainEvent
    {
        try
        {
            await _semaphore.WaitAsync();

            if (_failedEvents.Count >= MaxQueueSize)
                _failedEvents.RemoveAt(0); // Remove oldest

            var failedEvent = new FailedEvent
            {
                Id = Guid.NewGuid().ToString(),
                EventType = typeof(T).Name,
                EventData = System.Text.Json.JsonSerializer.Serialize(@event),
                Exception = exception.Message,
                StackTrace = exception.StackTrace,
                FailedAt = DateTime.UtcNow,
                RetryCount = 0
            };

            _failedEvents.Add(failedEvent);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets all failed events in the queue.
    /// </summary>
    public async Task<List<FailedEvent>> GetFailedEventsAsync()
    {
        try
        {
            await _semaphore.WaitAsync();
            return new List<FailedEvent>(_failedEvents);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Removes a failed event from the queue.
    /// </summary>
    public async Task<bool> RemoveAsync(string failedEventId)
    {
        try
        {
            await _semaphore.WaitAsync();

            var failedEvent = _failedEvents.FirstOrDefault(e => e.Id == failedEventId);
            if (failedEvent is not null)
            {
                _failedEvents.Remove(failedEvent);
                return true;
            }

            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets the count of failed events in queue.
    /// </summary>
    public async Task<int> GetCountAsync()
    {
        try
        {
            await _semaphore.WaitAsync();
            return _failedEvents.Count;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

public sealed class FailedEvent {
    /// <summary>Gets or sets the failed event identifier.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the event type.</summary>
    public string EventType { get; set; } = string.Empty;
    /// <summary>Gets or sets the event data.</summary>
    public string EventData { get; set; } = string.Empty;
    /// <summary>Gets or sets the exception message.</summary>
    public string Exception { get; set; } = string.Empty;
    /// <summary>Gets or sets the stack trace.</summary>
    public string? StackTrace { get; set; }
    /// <summary>Gets or sets the timestamp when the event failed.</summary>
    public DateTime FailedAt { get; set; }
    /// <summary>Gets or sets the retry count.</summary>
    public int RetryCount { get; set; }
}
