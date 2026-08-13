#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SqliteMultiTenant.Events
{
    /// <summary>
    /// Production-grade event bus implementation that supports asynchronous event handling with priority-based subscriber ordering.
    /// </summary>
    /// <remarks>
    /// This implementation provides thread-safe event publishing and subscription management using concurrent collections.
    /// It maintains an event history for monitoring and debugging purposes, and supports both synchronous and asynchronous publishing.
    /// </remarks>
    public sealed class EventBusImpl : IDisposable
    {
        private readonly ConcurrentDictionary<string, List<EventSubscription>> _subscriptions;
        private readonly ILogger<EventBusImpl> _logger;
        private readonly ConcurrentQueue<PublishedEvent> _eventHistory;
        private readonly ConcurrentDictionary<string, int> _handlerFailureCounts;
        private readonly ConcurrentDictionary<string, int> _successfulHandlerCounts;
        private readonly ConcurrentDictionary<string, int> _publishAttempts;
        private readonly ConcurrentDictionary<string, List<DeadLetterEvent>> _deadLetterQueue;
        private readonly ConcurrentDictionary<string, EventStatistics> _statistics;
        private const int MaxDeadLetterSize = 1000;
        private const int MaxRetryAttempts = 3;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventBusImpl"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording operational events and errors.</param>
        /// <param name="historySize">Maximum number of published events to retain in history (default: 1000).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public EventBusImpl(ILogger<EventBusImpl> logger, int historySize = 1000)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptions = new ConcurrentDictionary<string, List<EventSubscription>>();
            _eventHistory = new ConcurrentQueue<PublishedEvent>();
            _handlerFailureCounts = new ConcurrentDictionary<string, int>();
            _successfulHandlerCounts = new ConcurrentDictionary<string, int>();
            _publishAttempts = new ConcurrentDictionary<string, int>();
            _deadLetterQueue = new ConcurrentDictionary<string, List<DeadLetterEvent>>();
        }

        /// <summary>
        /// Subscribes an asynchronous handler to events of the specified type.
        /// </summary>
        /// <typeparam name="TEvent">The type of domain event to subscribe to.</typeparam>
        /// <param name="handler">The asynchronous handler function that processes the event.</param>
        /// <param name="priority">The priority level of the subscription (higher values execute first).</param>
        /// <returns>A disposable object that can be used to unsubscribe from the event.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
        public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler, int priority = 0)
            where TEvent : DomainEvent
        {
            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(TEvent).Name;
            var subscription = new EventSubscription
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                Handler = async (e) => await handler((TEvent)e),
                Priority = priority
            };

            _subscriptions.AddOrUpdate(eventType,
                new List<EventSubscription> { subscription },
                (_, list) =>
                {
                    list.Add(subscription);
                    // Sort by priority (higher first)
                    return list.OrderByDescending(s => s.Priority).ToList();
                });

            _logger.LogInformation("Handler subscribed to event {EventType} with priority {Priority}",
                eventType, priority);

            return new Unsubscriber(this, eventType, subscription.Id);
        }

        /// <summary>
        /// Asynchronously publishes an event to all registered subscribers.
        /// </summary>
        /// <typeparam name="TEvent">The type of domain event to publish.</typeparam>
        /// <param name="@event">The event instance to publish.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="@event"/> is null.</exception>
        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent
        {
            if (@event is null)
                throw new ArgumentNullException(nameof(@event));

            var eventType = typeof(TEvent).Name;


        // Track publish attempt using Interlocked for thread-safety
        _publishAttempts.AddOrUpdate(eventType, 1, (_, current) => current + 1);

        try
        {
            var publishedEvent = new PublishedEvent
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                PublishedAt = DateTime.UtcNow,
                TenantId = @event.TenantId
            };

            int successfulHandlerCount = 0;
            int failedHandlerCount = 0;

            if (_subscriptions.TryGetValue(eventType, out var handlers) && handlers.Count > 0)
            {
                var tasks = handlers.Select(h => ExecuteHandlerSafelyAsync(h, @event, eventType));
                await Task.WhenAll(tasks);
                successfulHandlerCount = handlers.Count(h => h.LastExecutionSucceeded);
                failedHandlerCount = handlers.Count - successfulHandlerCount;
            }
            else
            {
                // No subscribers registered - still track the publish attempt but no handlers executed
                _logger.LogDebug("No subscribers registered for event type: {EventType}", eventType);
            }

            publishedEvent.SuccessfulHandlers = successfulHandlerCount;

            AddToHistory(publishedEvent);

            // Track successful handlers using Interlocked for thread-safety
            if (successfulHandlerCount > 0)
            {
                _successfulHandlerCounts.AddOrUpdate(eventType, successfulHandlerCount, (_, current) => current + successfulHandlerCount);
            }

            // Track failed handlers using Interlocked for thread-safety
            if (failedHandlerCount > 0)
            {
                _handlerFailureCounts.AddOrUpdate(eventType, failedHandlerCount, (_, current) => current + failedHandlerCount);
            }

            _logger.LogDebug("Event published: {EventType} with {HandlerCount} handlers",
                eventType, publishedEvent.SuccessfulHandlers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event: {EventType}", eventType);
            throw;
        }

        }

        /// <summary>
        /// Synchronously publishes an event to all registered subscribers.
        /// </summary>
        /// <typeparam name="TEvent">The type of domain event to publish.</typeparam>
        /// <param name="@event">The event instance to publish.</param>
        public void Publish<TEvent>(TEvent @event) where TEvent : DomainEvent
        {
            PublishAsync(@event).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves a limited set of recently published events for monitoring and debugging purposes.
        /// </summary>
        /// <param name="take">Maximum number of events to return (default: 100).</param>
        /// <returns>A list of published events ordered by publication time (most recent first).</returns>
        public List<PublishedEvent> GetEventHistory(int take = 100)
        {
            var history = new List<PublishedEvent>();
            var tempQueue = new Queue<PublishedEvent>();

            while (_eventHistory.TryDequeue(out var evt))
            {
                tempQueue.Enqueue(evt);
                history.Add(evt);
            }

            // Re-queue events to maintain history
            while (tempQueue.TryDequeue(out var evt))
            {
                _eventHistory.Enqueue(evt);
            }

            return history.OrderByDescending(e => e.PublishedAt)
                .Take(take)
                .ToList();
        }

        /// <summary>
        /// Generates statistics about event subscriptions and publication counts.
        /// </summary>
        /// <returns>A dictionary mapping event type names to their statistics.</returns>
        public Dictionary<string, EventStatistics> GetEventStatistics()
        {
            var stats = new Dictionary<string, EventStatistics>();

            foreach (var kvp in _subscriptions)
            {
                var eventType = kvp.Key;
                var publishAttempts = _publishAttempts.TryGetValue(eventType, out var attempts) ? attempts : 0;
                var successfulHandlers = _successfulHandlerCounts.TryGetValue(eventType, out var successCount) ? successCount : 0;
                var failureCount = _handlerFailureCounts.TryGetValue(eventType, out var failCount) ? failCount : 0;

                stats[eventType] = new EventStatistics
                {
                    EventType = eventType,
                    SubscriberCount = kvp.Value.Count,
                    TotalPublished = _eventHistory.Count(e => e.EventType == eventType),
                    TotalPublishAttempts = publishAttempts,
                    SuccessfulHandlerInvocations = successfulHandlers,
                    FailedHandlerInvocations = failureCount
                };
            }

            return stats;
        }


        /// <summary>
        /// Gets all dead letter events from the queue.
        /// </summary>
        /// <param name="take">Maximum number of events to return (default: 100).</param>
        /// <returns>A list of dead letter events ordered by failure time (most recent first).</returns>
        public List<DeadLetterEvent> GetDeadLetterQueue(int take = 100)
        {
            var allEvents = new List<DeadLetterEvent>();

            foreach (var kvp in _deadLetterQueue)
            {
                allEvents.AddRange(kvp.Value);
            }

            return allEvents
                .OrderByDescending(e => e.FailedAt)
                .Take(take)
                .ToList();
        }

        /// <summary>
        /// Gets the count of dead letter events in the queue.
        /// </summary>
        /// <returns>The total number of dead letter events.</returns>
        public int GetDeadLetterCount()
        {
            return _deadLetterQueue.Values.Sum(v => v.Count);
        }

        /// <summary>
        /// Clears all dead letter events from the queue.
        /// </summary>
        public void ClearDeadLetterQueue()
        {
            _deadLetterQueue.Clear();
            _handlerFailureCounts.Clear();
        }

        /// <summary>
        /// Clears the event history, removing all stored published events.
        /// </summary>
        public void ClearHistory()
        {
            while (_eventHistory.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Releases all subscriptions and clears the event history.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _subscriptions.Clear();
                ClearHistory();
                ClearDeadLetterQueue();
            }

            _disposed = true;
        }

        private async Task ExecuteHandlerSafelyAsync(EventSubscription subscription, DomainEvent @event, string eventType)
        {
            try
            {
                await subscription.Handler(@event);
                subscription.LastExecutionSucceeded = true;

                // Reset failure count on successful execution
                _handlerFailureCounts.AddOrUpdate(eventType, 0, (_, _) => 0);
            }
            catch (Exception ex)
            {
                subscription.LastExecutionSucceeded = false;

                // Increment failure count for this event type
                var failureCount = _handlerFailureCounts.AddOrUpdate(
                    eventType,
                    1,
                    (_, current) => Math.Min(current + 1, MaxRetryAttempts + 1)
                );

                _logger.LogError(ex, "Error executing handler for event {EventType} (Failure #{FailureCount})",
                    eventType, failureCount);

                // Add to dead letter queue if max retries exceeded
                if (failureCount > MaxRetryAttempts)
                {
                    AddToDeadLetterQueue(eventType, @event, ex);
                }
            }
        }

        private void AddToHistory(PublishedEvent publishedEvent)
        {
            _eventHistory.Enqueue(publishedEvent);
        }

        private void AddToDeadLetterQueue(string eventType, DomainEvent @event, Exception exception)
        {
            var eventData = JsonSerializer.Serialize(@event);

            var deadLetterEvent = new DeadLetterEvent
            {
                EventType = eventType,
                EventData = eventData,
                Exception = exception.Message,
                StackTrace = exception.StackTrace,
                TenantId = @event.TenantId ?? string.Empty,
                RetryCount = _handlerFailureCounts.TryGetValue(eventType, out var count) ? count : 1
            };

            // Add to the specific event type's dead letter list
            var deadLettersForType = _deadLetterQueue.GetOrAdd(eventType, _ => new List<DeadLetterEvent>());

            // Enforce max size by removing oldest if needed
            if (deadLettersForType.Count >= MaxDeadLetterSize)
            {
                deadLettersForType.RemoveAt(0);
            }

            deadLettersForType.Add(deadLetterEvent);
        }

        private class EventSubscription
        {
            public Guid Id { get; set; }
            public string EventType { get; set; } = string.Empty;
            public Func<DomainEvent, Task> Handler { get; set; } = null!;
            public int Priority { get; set; }
            public bool LastExecutionSucceeded { get; set; }
        }

        private class Unsubscriber : IDisposable
        {
            private readonly EventBusImpl _eventBus;
            private readonly string _eventType;
            private readonly Guid _subscriptionId;

            public Unsubscriber(EventBusImpl eventBus, string eventType, Guid subscriptionId)
            {
                _eventBus = eventBus;
                _eventType = eventType;
                _subscriptionId = subscriptionId;
            }

            public void Dispose()
            {
                if (_eventBus._subscriptions.TryGetValue(_eventType, out var handlers))
                {
                    var toRemove = handlers.FirstOrDefault(h => h.Id == _subscriptionId);
                    if (toRemove is not null)
                    {
                        handlers.Remove(toRemove);
                    }
                }
            }
        }

        /// <summary>
        /// Provides a concise string representation of the EventBusImpl instance.
        /// </summary>
        public override string ToString()
        {
            return $"EventBusImpl {{ SubscriptionsCount = {_subscriptions.Count} }}";
        }
    }

    /// <summary>
    /// Represents a published event with metadata about its publication.
    /// </summary>
    public sealed class PublishedEvent
    {
        public Guid Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public int SuccessfulHandlers { get; set; }
    }

    /// <summary>
    /// Contains statistics about event subscriptions and publication counts for a specific event type.
    /// </summary>
    public sealed class EventStatistics
    {
        public string EventType { get; set; } = string.Empty;
        public int SubscriberCount { get; set; }
        public int TotalPublished { get; set; }
        public int TotalPublishAttempts { get; set; }
        public int SuccessfulHandlerInvocations { get; set; }
        public int FailedHandlerInvocations { get; set; }
    }

    /// <summary>
    /// Represents a dead letter event that failed to be processed after maximum retry attempts.
    /// </summary>
    public sealed class DeadLetterEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EventType { get; set; } = string.Empty;
        public string EventData { get; set; } = string.Empty;
        public string Exception { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public DateTime FailedAt { get; set; } = DateTime.UtcNow;
        public int RetryCount { get; set; }
        public string TenantId { get; set; } = string.Empty;
    }
}
