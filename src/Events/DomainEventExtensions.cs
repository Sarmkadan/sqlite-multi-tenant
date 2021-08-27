using System;

namespace SqliteMultiTenant.Events
{
    public static class DomainEventExtensions
    {
        public static bool IsTenantEvent(this DomainEvent @event)
        {
            return @event is TenantCreatedEvent or TenantUpdatedEvent or TenantSuspendedEvent;
        }

        public static string GetTenantIdOrDefault(this DomainEvent @event)
        {
            return @event.TenantId ?? string.Empty;
        }

        public static bool IsBackupEvent(this DomainEvent @event)
        {
            return @event is BackupStartedEvent;
        }
    }
}
