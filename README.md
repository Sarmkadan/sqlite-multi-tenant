## EventBusImpl

The `EventBusImpl` class provides a production-grade event bus implementation that supports asynchronous event handling with priority-based subscriber ordering. It maintains an event history for monitoring and debugging purposes.

### Usage Example

```csharp
using SqliteMultiTenant.Events;
using Microsoft.Extensions.Logging;

// Create an event bus instance
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<EventBusImpl>();
var eventBus = new EventBusImpl(logger);

// Subscribe to an event type
await eventBus.Subscribe<MyCustomEvent>(async (ev) => {
    Console.WriteLine($"Received event: {ev.Id}");
    // Handle the event
});

// Publish an event
var customEvent = new MyCustomEvent { Id = Guid.NewGuid().ToString(), Data = "test" };
await eventBus.PublishAsync(customEvent);

// Get event history
var history = eventBus.GetEventHistory();

// Get event statistics
var stats = eventBus.GetEventStatistics();

// Clear event history
eventBus.ClearHistory();

// Dispose the event bus
eventBus.Dispose();
```

## IDomainEventHandler

`IDomainEventHandler<T>` defines a contract for handling domain events of a specific type. Implementations receive a concrete event instance and perform asynchronous processing such as logging, notifying external systems, or cleaning up resources. The interface exposes a single method, `HandleAsync`, which returns a `Task` that completes when the handling logic finishes.

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Integration; // Adjust namespace if different

// Set up a logger for the handler
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<TenantCreatedEventHandler>();

// Assume a concrete webhook service implementation is available
var webhookService = new WebhookService(/* any required dependencies */);

// Create the handler instance
var handler = new TenantCreatedEventHandler(logger, webhookService);

// Create a tenant‑created notification event
var @event = new TenantCreatedNotificationEvent
{
    TenantId = "tenant-123",
    TenantName = "Acme Corp",
    TenantDescription = "Demo tenant for testing"
};

// Handle the event asynchronously
await handler.HandleAsync(@event);
```

## IEventPublisher

The `IEventPublisher` interface provides a mechanism for publishing domain events and managing event handlers. It supports both synchronous and asynchronous event handling, with built-in logging and error resilience. The `EventPublisher` class implements this interface and manages a registry of event handlers.

### Usage Example

```csharp
using SqliteMultiTenant.Events;
using Microsoft.Extensions.Logging;

// Create a logger and event publisher
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<EventPublisher>();
var eventPublisher = new EventPublisher(logger);

// Subscribe a logging handler for MyCustomEvent
eventPublisher.Subscribe<MyCustomEvent>(new LoggingEventHandler<MyCustomEvent>(logger));

// Create and publish a custom event
var myEvent = new MyCustomEvent
{
    EventId = Guid.NewGuid().ToString(),
    EventType = "MyCustomEvent",
    OccurredAt = DateTime.UtcNow
};

await eventPublisher.PublishAsync(myEvent);

// Check how many handlers are registered for this event type
int handlerCount = eventPublisher.GetHandlerCount<MyCustomEvent>();
Console.WriteLine($"Registered handlers: {handlerCount}");
```

