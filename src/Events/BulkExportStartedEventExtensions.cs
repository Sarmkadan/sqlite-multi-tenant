using SqliteMultiTenant.Events;
using System;

namespace SqliteMultiTenant.Events
{
    /// <summary>
    /// Provides extension methods for <see cref="BulkExportStartedEvent"/> instances,
    /// enabling validation of the event data and generation of a human‑readable summary
    /// of the bulk export operation.
    /// </summary>
    public static class BulkExportStartedEventExtensions
    {
        /// <summary>
        /// Determines whether the supplied <see cref="BulkExportStartedEvent"/> contains
        /// all required information for a bulk export operation.
        /// </summary>
        /// <param name="event">The event to validate.</param>
        /// <returns>
        /// <c>true</c> if the event is considered valid; otherwise, <c>false</c>.
        /// An event is valid when:
        /// <list type="bullet">
        ///   <item><description><see cref="BulkExportStartedEvent.DatabaseId"/> is not null, empty, or whitespace.</description></item>
        ///   <item><description><see cref="BulkExportStartedEvent.TableNames"/> is not null, contains at least one entry, and every table name is not null, empty, or whitespace.</description></item>
        ///   <item><description><see cref="BulkExportStartedEvent.Format"/> is not null, empty, or whitespace.</description></item>
        ///   <item><description><see cref="BulkExportStartedEvent.OperationId"/> is not null, empty, or whitespace.</description></item>
        /// </list>
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="event"/> is <c>null</c>.</exception>
        public static bool IsValidOperation(this BulkExportStartedEvent @event)
        {
            ArgumentNullException.ThrowIfNull(@event);

            return !string.IsNullOrWhiteSpace(@event.DatabaseId)
                && @event.TableNames is not null
                && @event.TableNames.Count > 0
                && @event.TableNames.All(t => !string.IsNullOrWhiteSpace(t))
                && !string.IsNullOrWhiteSpace(@event.Format)
                && !string.IsNullOrWhiteSpace(@event.OperationId);
        }

        /// <summary>
        /// Generates a concise, human‑readable description of the bulk export operation
        /// represented by the event.
        /// </summary>
        /// <param name="event">The event containing export operation details.</param>
        /// <returns>
        /// A formatted string that includes the source database identifier, the target format,
        /// and a comma‑separated list of the tables to be exported, e.g.
        /// <c>"Exporting from database 'myDb' to format 'json' for tables [TableA, TableB]"</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="event"/> is <c>null</c>.</exception>
        public static string GetOperationSummary(this BulkExportStartedEvent @event) =>
            $"Exporting from database '{@event.DatabaseId}' to format '{@event.Format}' for tables [{string.Join(", ", @event.TableNames)}]";
    }
}
