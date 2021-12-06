using System;

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
    }
}
