// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Events
{
    // Production event bus implementation with support for async handlers and priorities
    public class EventBusImpl
    {
        private readonly ConcurrentDictionary<string, List<EventSubscription>> _subscriptions;
        private readonly ILogger<EventBusImpl> _logger;
        private readonly ConcurrentQueue<PublishedEvent> _eventHistory;

        public EventBusImpl(ILogger<EventBusImpl> logger, int historySize = 1000)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptions = new ConcurrentDictionary<string, List<EventSubscription>>();
            _eventHistory = new ConcurrentQueue<PublishedEvent>();
        }

        // Subscribes to an event type with optional priority
        public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler, int priority = 0)
            where TEvent : DomainEvent
        {
            if (handler == null)
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

        // Publishes an event to all subscribers
        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent
        {
            if (@event == null)
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

        // Publishes event synchronously
        public void Publish<TEvent>(TEvent @event) where TEvent : DomainEvent
        {
            PublishAsync(@event).GetAwaiter().GetResult();
        }

        // Gets the event history for monitoring and debugging
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

        // Gets event statistics
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

        // Clears event history
        public void ClearHistory()
        {
            while (_eventHistory.TryDequeue(out _)) { }
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
                    if (toRemove != null)
                    {
                        handlers.Remove(toRemove);
                    }
                }
            }
        }
    }

    public class PublishedEvent
    {
        public Guid Id { get; set; }
        public string EventType { get; set; }
        public DateTime PublishedAt { get; set; }
        public string TenantId { get; set; }
        public int SuccessfulHandlers { get; set; }
    }

    public class EventStatistics
    {
        public string EventType { get; set; }
        public int SubscriberCount { get; set; }
        public int TotalPublished { get; set; }
    }
}
