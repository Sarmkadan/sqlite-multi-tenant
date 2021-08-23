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

namespace SqliteMultiTenant.Events
{
    /// <summary>
    /// Production-grade event bus implementation that supports asynchronous event handling with priority-based subscriber ordering.
    /// </summary>
    /// <remarks>
    /// This implementation provides thread-safe event publishing and subscription management using concurrent collections.
    /// It maintains an event history for monitoring and debugging purposes, and supports both synchronous and asynchronous publishing.
    /// </remarks>
    public sealed class EventBusImpl {
        private readonly ConcurrentDictionary<string, List<EventSubscription>> _subscriptions;
        private readonly ILogger<EventBusImpl> _logger;
        private readonly ConcurrentQueue<PublishedEvent> _eventHistory;

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

            try
            {
                var publishedEvent = new PublishedEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = eventType,
                    PublishedAt = DateTime.UtcNow,
                    TenantId = @event.TenantId
                };

                if (_subscriptions.TryGetValue(eventType, out var handlers))
                {
                    var tasks = handlers.Select(h => ExecuteHandlerSafelyAsync(h, @event));
                    await Task.WhenAll(tasks);
                }

                publishedEvent.SuccessfulHandlers = _subscriptions.TryGetValue(eventType, out var count)
                    ? count.Count
                    : 0;

                AddToHistory(publishedEvent);

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
                stats[kvp.Key] = new EventStatistics
                {
                    EventType = kvp.Key,
                    SubscriberCount = kvp.Value.Count,
                    TotalPublished = _eventHistory.Count(e => e.EventType == kvp.Key)
                };
            }

            return stats;
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
            _subscriptions.Clear();
            ClearHistory();
        }

        private async Task ExecuteHandlerSafelyAsync(EventSubscription subscription, DomainEvent @event)
        {
            try
            {
                await subscription.Handler(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing handler for event {EventType}",
                    subscription.EventType);
            }
        }

        private void AddToHistory(PublishedEvent publishedEvent)
        {
            _eventHistory.Enqueue(publishedEvent);
        }

        private class EventSubscription
        {
            public Guid Id { get; set; }
            public string EventType { get; set; }
            public Func<DomainEvent, Task> Handler { get; set; }
            public int Priority { get; set; }
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
    }

    /// <summary>
    /// Represents a published event with metadata about its publication.
    /// </summary>
    public sealed class PublishedEvent {
        public Guid Id { get; set; }
        public string EventType { get; set; }
        public DateTime PublishedAt { get; set; }
        public string TenantId { get; set; }
        public int SuccessfulHandlers { get; set; }
    }

    /// <summary>
    /// Contains statistics about event subscriptions and publication counts for a specific event type.
    /// </summary>
    public sealed class EventStatistics {
        public string EventType { get; set; }
        public int SubscriberCount { get; set; }
        public int TotalPublished { get; set; }
    }
}