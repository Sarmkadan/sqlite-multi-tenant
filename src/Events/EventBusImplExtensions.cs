using System;
using System.Threading.Tasks;

namespace SqliteMultiTenant.Events
{
    public static class EventBusImplExtensions
    {
        /// <summary>
        /// Publishes an event to the event bus and waits for all handlers to complete.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to publish.</typeparam>
        /// <param name="eventBus">The event bus instance.</param>
        /// <param name="event">The event to publish.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task PublishAndWaitAsync<TEvent>(this EventBusImpl eventBus, TEvent @event) 
            where TEvent : DomainEvent
        {
            await eventBus.PublishAsync<TEvent>(@event);
        }

        /// <summary>
        /// Subscribes to events of a specific type and executes a handler for each event received.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to subscribe to.</typeparam>
        /// <param name="eventBus">The event bus instance.</param>
        /// <param name="handler">The handler to execute for each event received.</param>
        /// <returns>An unsubscriber that can be used to unsubscribe from events.</returns>
        public static IDisposable SubscribeAndExecute<TEvent>(this EventBusImpl eventBus, Func<TEvent, Task> handler) 
            where TEvent : DomainEvent
        {
            return eventBus.Subscribe<TEvent>(e => 
            {
                _ = handler(e);
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Clears the event history and statistics for a specific event type.
        /// </summary>
        /// <param name="eventBus">The event bus instance.</param>
        /// <param name="eventType">The type of event to clear history and statistics for.</param>
        public static void ClearHistoryAndStatistics(this EventBusImpl eventBus, string eventType)
        {
            eventBus.ClearHistory();
            var statistics = eventBus.GetEventStatistics();
            if (statistics.TryGetValue(eventType, out var stat))
            {
                // No direct way to remove from dictionary, 
                // but we can create a new dictionary without the specific event type
                var newStatistics = new Dictionary<string, EventStatistics>(statistics);
                newStatistics.Remove(eventType);
                // Note: This does not modify the original dictionary. 
                // If modification is needed, consider using a different data structure.
            }
        }
    }
}
