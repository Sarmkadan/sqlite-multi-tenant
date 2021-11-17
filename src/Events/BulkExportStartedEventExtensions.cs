using SqliteMultiTenant.Events;
using System;

namespace SqliteMultiTenant.Events
{
    /// <summary>
    /// Provides extension methods for <see cref="BulkExportStartedEvent"/> instances.
    /// Enables validation and formatting of bulk export operation events.
    /// </summary>
    public static class BulkExportStartedEventExtensions
    {
        /// <summary>
        /// Determines whether the bulk export operation represented by the event is valid.
        /// Validates that all required properties are non-null and non-empty.
        /// </summary>
        /// <param name="event">The event to validate.</param>
        /// <returns>True if the event represents a valid operation; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="event"/> is null.</exception>
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
        /// Generates a human-readable summary of the bulk export operation.
        /// </summary>
        /// <param name="event">The event containing export operation details.</param>
        /// <returns>A formatted string describing the export operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="event"/> is null.</exception>
        public static string GetOperationSummary(this BulkExportStartedEvent @event) =>
            $"Exporting from database '{@event.DatabaseId}' to format '{@event.Format}' for tables [{string.Join(", ", @event.TableNames)}]";
    }
}