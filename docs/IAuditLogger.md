# IAuditLogger
The `IAuditLogger` interface is designed to provide a standardized way of logging and retrieving audit log entries in a multi-tenant SQLite environment. It allows for the logging of various events, such as changes to resources, and provides methods for retrieving and purging log entries. This interface is implemented by the `AuditLogger` class, which provides the actual logging functionality.

## API
The `IAuditLogger` interface includes the following members:
* `LogAsync`: Logs an audit event asynchronously. This method does not return a value and does not throw any exceptions as part of its normal operation, but may throw exceptions related to the underlying logging mechanism.
* `GetEntriesAsync`: Retrieves a list of audit log entries asynchronously. This method returns a `List<AuditLogEntry>` and may throw exceptions related to the underlying data retrieval mechanism.
* `GetEntryCountAsync`: Retrieves the number of audit log entries asynchronously. This method returns an `int` and may throw exceptions related to the underlying data retrieval mechanism.
* `PurgeOldEntriesAsync`: Purges old audit log entries asynchronously. This method does not return a value and does not throw any exceptions as part of its normal operation, but may throw exceptions related to the underlying logging mechanism.
* `GetStatisticsAsync`: Retrieves audit log statistics asynchronously. This method returns an `AuditLogStatistics` object and may throw exceptions related to the underlying data retrieval mechanism.

## Usage
Here are two examples of using the `IAuditLogger` interface:
```csharp
// Example 1: Logging an audit event
var auditLogger = new AuditLogger();
await auditLogger.LogAsync(new AuditLogEntry
{
    Id = Guid.NewGuid().ToString(),
    Timestamp = DateTime.UtcNow,
    EventType = "ResourceUpdated",
    Actor = "John Doe",
    ResourceId = "12345",
    ResourceType = "User",
    Description = "Updated user profile",
    Action = AuditAction.Update,
    Changes = new Dictionary<string, object>
    {
        { "Name", "Jane Doe" },
        { "Email", "jane.doe@example.com" }
    },
    IpAddress = "192.168.1.100"
});

// Example 2: Retrieving audit log entries
var auditLogger = new AuditLogger();
var filter = new AuditLogFilter { EventType = "ResourceUpdated" };
var entries = await auditLogger.GetEntriesAsync(filter);
foreach (var entry in entries)
{
    Console.WriteLine($"Event Type: {entry.EventType}, Actor: {entry.Actor}, Resource Id: {entry.ResourceId}");
}
```

## Notes
When using the `IAuditLogger` interface, consider the following edge cases and thread-safety remarks:
* The `LogAsync` method is designed to be thread-safe, allowing for concurrent logging of audit events.
* The `GetEntriesAsync`, `GetEntryCountAsync`, and `GetStatisticsAsync` methods are also designed to be thread-safe, allowing for concurrent retrieval of audit log data.
* However, the `PurgeOldEntriesAsync` method may have implications for concurrent logging and retrieval of audit log data, as it modifies the underlying log data.
* When using the `AuditLogFilter` class to filter audit log entries, consider the potential performance implications of filtering large datasets.
* The `IAuditLogger` interface does not provide any built-in mechanism for handling exceptions related to the underlying logging or data retrieval mechanisms. It is the responsibility of the implementing class to handle such exceptions appropriately.
