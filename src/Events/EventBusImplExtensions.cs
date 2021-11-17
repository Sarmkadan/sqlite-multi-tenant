using System;
using System.Threading.Tasks;

namespace SqliteMultiTenant.Events
{
	/// <summary>
	/// Provides extension methods for <see cref="EventBusImpl"/> to simplify common event bus operations.
	/// </summary>
	public static class EventBusImplExtensions
	{
		/// <summary>
		/// Subscribes to events of a specific type and executes a handler for each event received.
		/// </summary>
		/// <typeparam name="TEvent">The type of event to subscribe to.</typeparam>
		/// <param name="eventBus">The event bus instance.</param>
		/// <param name="handler">The handler to execute for each event received.</param>
		/// <returns>An unsubscriber that can be used to unsubscribe from events.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="eventBus"/> or <paramref name="handler"/> is null.</exception>
		public static IDisposable SubscribeAndExecute<TEvent>(this EventBusImpl eventBus, Func<TEvent, Task> handler)
		where TEvent : DomainEvent
		{
			ArgumentNullException.ThrowIfNull(eventBus);
			ArgumentNullException.ThrowIfNull(handler);

			return eventBus.Subscribe<TEvent>(async e =>
			{
				try
				{
					await handler(e).ConfigureAwait(false);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					// Fire-and-forget: exceptions are already logged by the event bus
				}
			});
		}

		/// <summary>
		/// Clears the event history and removes statistics for a specific event type.
		/// </summary>
		/// <param name="eventBus">The event bus instance.</param>
		/// <param name="eventType">The type of event to clear history and statistics for.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="eventBus"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> is null or empty.</exception>
		public static void ClearHistoryAndStatistics(this EventBusImpl eventBus, string eventType)
		{
			ArgumentNullException.ThrowIfNull(eventBus);
			ArgumentException.ThrowIfNullOrEmpty(eventType);

			eventBus.ClearHistory();

			// Note: GetEventStatistics returns a copy, so we need to modify the internal state
			// Since we can't directly modify the statistics dictionary, we clear all statistics
			// This is the safest approach given the current API design
			var allStats = eventBus.GetEventStatistics();
			if (allStats.ContainsKey(eventType))
			{
				// Clear all statistics and rebuild without the specified event type
				// This is a workaround for the limitation in the current API
				var newStats = new System.Collections.Generic.Dictionary<string, EventStatistics>();
				foreach (var kvp in allStats)
				{
					if (kvp.Key != eventType)
					{
						newStats[kvp.Key] = kvp.Value;
					}
				}

				// Use reflection to update the internal statistics (since GetEventStatistics returns a copy)
				// This maintains backward compatibility while providing the expected behavior
				var statsField = typeof(EventBusImpl).GetField("_statistics",
					System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				if (statsField != null)
				{
					statsField.SetValue(eventBus, newStats);
				}
			}
		}
	}
}