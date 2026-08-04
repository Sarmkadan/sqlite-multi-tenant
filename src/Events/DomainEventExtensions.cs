using System;
using System.Reflection;

namespace SqliteMultiTenant.Events
{
    /// <summary>
    /// Provides extension methods for working with <see cref="DomainEvent"/> types.
    /// </summary>
    public static class DomainEventExtensions
    {
        /// <summary>
        /// Determines whether the event is a tenant-related event.
        /// </summary>
        /// <param name="event">The domain event to check.</param>
        /// <returns><see langword="true"/> if the event is a tenant-related event; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/>.</exception>
        public static bool IsTenantEvent(this DomainEvent @event)
        {
            ArgumentNullException.ThrowIfNull(@event);
            return @event is TenantCreatedEvent or TenantUpdatedEvent or TenantSuspendedEvent;
        }

        /// <summary>
        /// Gets the tenant ID associated with the event, or an empty string if not available.
        /// </summary>
        /// <param name="event">The domain event.</param>
        /// <returns>The tenant ID if available; otherwise, an empty string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/>.</exception>
        public static string GetTenantIdOrDefault(this DomainEvent @event)
        {
            ArgumentNullException.ThrowIfNull(@event);
            return @event.TenantId ?? string.Empty;
        }

        /// <summary>
        /// Determines whether the event is a backup-related event.
        /// </summary>
        /// <param name="event">The domain event to check.</param>
        /// <returns><see langword="true"/> if the event is a backup-related event; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/>.</exception>
        public static bool IsBackupEvent(this DomainEvent @event)
        {
            ArgumentNullException.ThrowIfNull(@event);
            return @event is BackupStartedEvent or BackupCompletedEvent or BackupFailedEvent;
        }

        /// <summary>
        /// Determines whether the event occurred more than the specified <paramref name="age"/> ago.
        /// </summary>
        /// <param name="event">The domain event to evaluate.</param>
        /// <param name="age">The time span that defines how old the event must be.</param>
        /// <returns>
        /// <see langword="true"/> if the event's timestamp is older than the supplied <paramref name="age"/>; otherwise, <see langword="false"/>.
        /// If the event does not expose a timestamp, the method returns <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/>.</exception>
        public static bool IsOlderThan(this DomainEvent @event, TimeSpan age)
        {
            ArgumentNullException.ThrowIfNull(@event);

            // Try to locate a DateTime property that represents the event timestamp.
            // Common names are "OccurredOn" or "Timestamp".
            PropertyInfo? prop = @event.GetType().GetProperty("OccurredOn")
                                 ?? @event.GetType().GetProperty("Timestamp");

            if (prop is not null && prop.PropertyType == typeof(DateTime))
            {
                DateTime timestamp = (DateTime)prop.GetValue(@event)!;
                return (DateTime.UtcNow - timestamp) > age;
            }

            return false;
        }

        /// <summary>
        /// Returns a concise string representation of the event suitable for logging.
        /// </summary>
        /// <param name="event">The domain event to format.</param>
        /// <returns>A formatted string containing the event type, tenant id, correlation id and timestamp (if available).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/>.</exception>
        public static string ToLogString(this DomainEvent @event)
        {
            ArgumentNullException.ThrowIfNull(@event);

            string tenantId = @event.GetTenantIdOrDefault();

            // CorrelationId may not exist; attempt to read it via reflection.
            PropertyInfo? correlationProp = @event.GetType().GetProperty("CorrelationId");
            string correlationId = correlationProp?.GetValue(@event) as string ?? string.Empty;

            // Timestamp (if any)
            PropertyInfo? timestampProp = @event.GetType().GetProperty("OccurredOn")
                                         ?? @event.GetType().GetProperty("Timestamp");
            string timestamp = string.Empty;
            if (timestampProp is not null && timestampProp.PropertyType == typeof(DateTime))
            {
                DateTime dt = (DateTime)timestampProp.GetValue(@event)!;
                timestamp = dt.ToString("o"); // ISO 8601 format
            }

            return $"[{@event.GetType().Name}] TenantId={tenantId} CorrelationId={correlationId} Timestamp={timestamp}";
        }

        /// <summary>
        /// Sets the correlation identifier on the event and returns the same instance for fluent usage.
        /// </summary>
        /// <param name="event">The domain event to modify.</param>
        /// <param name="correlationId">The correlation identifier to assign.</param>
        /// <returns>The same <paramref name="event"/> instance after the correlation identifier has been set.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> or <paramref name="correlationId"/> is <see langword="null"/>.</exception>
        public static DomainEvent WithCorrelationId(this DomainEvent @event, string correlationId)
        {
            ArgumentNullException.ThrowIfNull(@event);
            ArgumentNullException.ThrowIfNull(correlationId);

            // Attempt to set a writable "CorrelationId" property via reflection.
            PropertyInfo? prop = @event.GetType().GetProperty("CorrelationId");
            if (prop is not null && prop.CanWrite)
            {
                prop.SetValue(@event, correlationId);
            }

            return @event;
        }
    }
}
