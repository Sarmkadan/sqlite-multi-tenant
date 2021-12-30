// existing content ...

## IEventBus

The `IEventBus` interface provides a lightweight event bus implementation for publishing and subscribing to events within the multi-tenant SQLite application. It supports strongly-typed events with compile-time safety and includes a dead-letter queue for handling failed event deliveries.

### Usage Example

```csharp
using SqliteMultiTenant.Events;

// Create an event bus instance
var eventBus = new EventBus();

// Subscribe to an event type
await eventBus.SubscribeAsync<MyCustomEvent>(async (ev) => {
    Console.WriteLine($"Received event: {ev.Id}");
    // Handle the event
});

// Publish an event
var customEvent = new MyCustomEvent { Id = Guid.NewGuid().ToString(), Data = "test" };
await eventBus.PublishAsync(customEvent);

// Check subscriber count
var subscribers = eventBus.GetSubscriberCount<MyCustomEvent>();

// Access dead-letter queue for failed events
var dlq = eventBus.GetDeadLetterQueue();

// Get failed events
var failedEvents = await dlq.GetFailedEventsAsync<MyCustomEvent>();

// Remove a failed event
if (failedEvents.Count > 0)
{
    await dlq.RemoveAsync(failedEvents[0].Id);
}

// Get count of failed events
var count = await dlq.GetCountAsync<MyCustomEvent>();
```

## DatabaseAccessException

The `DatabaseAccessException` is a custom exception class that represents a database access error. It provides additional context about the error, including the database ID and the type of operation that failed.

### Usage Example

```csharp
using SqliteMultiTenant.Exceptions;

// Create an instance of the exception processor
var exception = DatabaseAccessException.ConnectionFailed("my_database", new Exception("Connection failed"));

// Log the exception with context information
Console.WriteLine(exception.Message);
Console.WriteLine($"Database ID: {exception.DatabaseId}");
Console.WriteLine($"Operation Type: {exception.OperationType}");
```

## BackupException

The `BackupException` is a custom exception class that represents errors during backup operations. It provides additional context about the error, including the backup ID, database ID, and the type of backup operation that failed.

### Usage Example

```csharp
using SqliteMultiTenant.Exceptions;

// Create a backup exception for a failed creation operation
var creationException = BackupException.CreationFailed("customer_db", new Exception("Disk full"));
Console.WriteLine(creationException.Message);
Console.WriteLine($"Backup ID: {creationException.BackupId}");
Console.WriteLine($"Database ID: {creationException.DatabaseId}");

// Create a backup exception for a failed verification operation
var verificationException = BackupException.VerificationFailed("backup_20241215_1430", "customer_db");
Console.WriteLine(verificationException.Message);
Console.WriteLine($"Backup ID: {verificationException.BackupId}");
Console.WriteLine($"Database ID: {verificationException.DatabaseId}");

// Create a backup exception for a failed restore operation
var restoreException = BackupException.RestoreFailed("backup_20241215_1430", "customer_db", new Exception("File corrupted"));
Console.WriteLine(restoreException.Message);
Console.WriteLine($"Backup ID: {restoreException.BackupId}");
Console.WriteLine($"Database ID: {restoreException.DatabaseId}");

// Create a backup exception for a not found scenario
var notFoundException = BackupException.NotFound("backup_20241215_1430");
Console.WriteLine(notFoundException.Message);
```

## BulkExportStartedEvent

The `BulkExportStartedEvent` is raised when a bulk export operation begins. It provides essential context for monitoring dashboards and audit trails, including the source database identifier, tables being exported, output format, and a correlation token that links this event to subsequent `BulkExportCompletedEvent` or `BulkExportFailedEvent` events.

### Usage Example

```csharp
using SqliteMultiTenant.Events;

// Create a bulk export started event
var exportEvent = new BulkExportStartedEvent {
    DatabaseId = "customer_database_2024",
    TableNames = new List<string> { "Customers", "Orders", "Products" },
    Format = "Json",
    OperationId = Guid.NewGuid().ToString()
};

// Publish the event to the event bus
await eventBus.PublishAsync(exportEvent);

// Access event properties
Console.WriteLine($"Export started for database: {exportEvent.DatabaseId}");
Console.WriteLine($"Tables to export: {string.Join(", ", exportEvent.TableNames)}");
Console.WriteLine($"Export format: {exportEvent.Format}");
Console.WriteLine($"Operation ID: {exportEvent.OperationId}");
```

## DomainEvent

The `DomainEvent` class serves as the base class for all domain events in the multi-tenant SQLite application. It provides a standardized structure for domain events with essential metadata including a unique event identifier, timestamp, event type, and optional tenant context. Domain events enable loose coupling between components, support audit logging, and facilitate event-driven architecture patterns.

### Usage Example

```csharp
using SqliteMultiTenant.Events;

// Create a tenant created event
var tenantCreatedEvent = new TenantCreatedEvent {
    TenantId = "tenant_123",
    TenantName = "Acme Corporation",
    ContactEmail = "admin@acme.com"
};

// Set tenant context (optional)
tenantCreatedEvent.TenantId = "tenant_123";

// Access base event properties
Console.WriteLine($"Event ID: {tenantCreatedEvent.EventId}");
Console.WriteLine($"Occurred at: {tenantCreatedEvent.OccurredAt}");
Console.WriteLine($"Event type: {tenantCreatedEvent.EventType}");
Console.WriteLine($"Tenant ID: {tenantCreatedEvent.TenantId}");

// Publish the event to the event bus
await eventBus.PublishAsync(tenantCreatedEvent);

// Create a tenant updated event
var tenantUpdatedEvent = new TenantUpdatedEvent {
    TenantId = "tenant_123",
    OldName = "Acme Corporation",
    NewName = "Acme Corp LLC"
};

// Publish the updated event
await eventBus.PublishAsync(tenantUpdatedEvent);

// Create a tenant suspended event
var tenantSuspendedEvent = new TenantSuspendedEvent {
    TenantId = "tenant_456",
    SuspendedBy = "admin@acme.com",
    Reason = "Non-payment"
};

// Publish the suspended event
await eventBus.PublishAsync(tenantSuspendedEvent);
```

// existing content ...
