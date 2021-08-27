using SqliteMultiTenant.Events;
using System;

namespace SqliteMultiTenant.Events
{
    public static class BulkExportStartedEventExtensions
    {
        public static bool IsValidOperation(this BulkExportStartedEvent @event)
        {
            return !string.IsNullOrEmpty(@event.DatabaseId) 
                && @event.TableNames.Count > 0 
                && !string.IsNullOrEmpty(@event.Format) 
                && !string.IsNullOrEmpty(@event.OperationId);
        }

        public static string GetOperationSummary(this BulkExportStartedEvent @event)
        {
            return $"Exporting from database { @event.DatabaseId } to format { @event.Format } for tables { string.Join(", ", @event.TableNames) }";
        }
    }
}
