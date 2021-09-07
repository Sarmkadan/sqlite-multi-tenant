# EventBusImpl

`EventBusImpl` is a sealed, in-process event bus that dispatches domain events to registered handlers with priority ordering. It maintains a history of published events and per-event-type statistics, supports both synchronous and asynchronous publication, and exposes subscription management through disposable tokens. The implementation is scoped to a single application instance and does not provide cross-process or durable messaging.

## API

### Constructors

- **`public EventBusImpl()`**  
  Initializes a new instance of the event bus with empty handler registrations, an empty event history, and zeroed statistics. No external dependencies are required.

### Subscription

- **`public IDisposable Subscribe<TEvent>(Func<DomainEvent, Task> handler, int priority = 0)`**  
  Registers an asynchronous handler for events of type `TEvent`.  
  *Parameters*:  
  - `handler` — the delegate to invoke when an event of type `TEvent` is published. Must accept a `DomainEvent` and return a `Task`.  
  - `priority` — lower values execute first; default is 0. Handlers with equal priority run in registration order.  
  *Returns*: an `IDisposable` token that, when disposed, removes the subscription.  
  *Throws*: `ArgumentNullException` if `handler` is null.

### Publication

- **`public void Publish<TEvent>(TEvent @event)`**  
  Synchronously fires all registered handlers for `TEvent` in priority order. Each handler’s task is executed synchronously (blocking the calling thread until completion). If a handler throws, the exception propagates immediately and subsequent handlers are not invoked. The event is recorded in history and statistics regardless of handler success.  
  *Throws*: any exception thrown by a handler; `ArgumentNullException` if `@event` is null.

- **`public async Task PublishAsync<TEvent>(TEvent @event)`**  
  Asynchronously fires all registered handlers for `TEvent` in priority order. Handlers are awaited sequentially; if one throws, the exception propagates and remaining handlers are skipped. The event is recorded in history and statistics before the first handler is invoked.  
  *Returns*: a `Task` that completes when all handlers have executed or an exception occurs.  
  *Throws*: any exception thrown by a handler; `ArgumentNullException` if `@event` is null.

### Diagnostics

- **`public List<PublishedEvent> GetEventHistory()`**  
  Returns a snapshot of all events published since the last call to `ClearHistory()` or instantiation. The list is a copy; modifications do not affect internal state. Each entry contains the event ID, type name, timestamp, and tenant identifier.

- **`public Dictionary<string, EventStatistics> GetEventStatistics()`**  
  Returns a snapshot of per-event-type statistics keyed by the fully qualified type name. Values include total published count and the timestamp of the most recent publication. The dictionary is a copy.

- **`public void ClearHistory()`**  
  Removes all entries from the event history. Statistics are not reset.

### Disposal

- **`public void Dispose()`**  
  Clears all handler registrations, event history, and statistics. After disposal, any call to `Publish`, `PublishAsync`, `Subscribe`, or diagnostic methods throws `ObjectDisposedException`. Safe to call multiple times.

### Nested Types

- **`public sealed class PublishedEvent`**  
  Immutable record of a published event.  
  Members:  
  - `public Guid Id` — unique identifier of the event instance.  
  - `public string EventType` — fully qualified type name.  
  - `public DateTime PublishedAt` — UTC timestamp of publication.  
  - `public string TenantId` — tenant identifier extracted from the event, or null if not multi-tenant.

- **`public sealed class Unsubscriber : IDisposable`**  
  Token returned by `Subscribe`. Calling `Dispose()` removes the associated handler. Safe to call multiple times; subsequent disposals are no-ops.

## Usage

### Example 1: Basic subscription and synchronous publication

```csharp
var bus = new EventBusImpl();

// Subscribe with default priority
IDisposable subscription = bus.Subscribe<OrderPlaced>(async domainEvent =>
{
    var orderEvent = (OrderPlaced)domainEvent;
    await SendConfirmationEmailAsync(orderEvent.OrderId);
});

// Publish synchronously — handlers run on the calling thread
bus.Publish(new OrderPlaced { OrderId = 123, TenantId = "tenant-A" });

// Later, unsubscribe
subscription.Dispose();
```

### Example 2: Priority-ordered handlers with diagnostics

```csharp
var bus = new EventBusImpl();

// High-priority audit handler runs first
bus.Subscribe<OrderPlaced>(async e => await AuditLogAsync(e), priority: -10);

// Default-priority notification handler runs second
bus.Subscribe<OrderPlaced>(async e => await NotifyWarehouseAsync(e));

await bus.PublishAsync(new OrderPlaced { OrderId = 456, TenantId = "tenant-B" });

// Inspect diagnostics
List<PublishedEvent> history = bus.GetEventHistory();
Dictionary<string, EventStatistics> stats = bus.GetEventStatistics();

Console.WriteLine($"Events published: {history.Count}");
foreach (var kvp in stats)
    Console.WriteLine($"{kvp.Key}: {kvp.Value.TotalPublished}");
```

## Notes

- **Thread safety**: `Subscribe`, `Publish`, `PublishAsync`, `ClearHistory`, and `Dispose` are safe to call concurrently. Diagnostic methods (`GetEventHistory`, `GetEventStatistics`) return consistent snapshots but may reflect state changes from concurrent publications.
- **Handler execution order**: Handlers are invoked sequentially in ascending priority order. Equal-priority handlers execute in the order they were subscribed. This design avoids concurrent handler execution and simplifies exception handling.
- **Exception propagation**: If a handler throws, publication stops immediately. Handlers later in the sequence are not executed. The event is still recorded in history and statistics because recording occurs before handler invocation.
- **Disposal semantics**: Disposing the bus disposes all active `Unsubscriber` tokens implicitly. Disposing an individual `Unsubscriber` removes only that handler; other subscriptions remain intact.
- **Event history growth**: The history grows unboundedly until `ClearHistory()` or `Dispose()` is called. Long-lived instances should periodically clear history to avoid memory pressure.
- **Statistics reset**: Statistics accumulate for the lifetime of the bus and are only reset by disposal. `ClearHistory()` does not affect statistics.
- **Null events**: Both `Publish` and `PublishAsync` throw `ArgumentNullException` for null events. Handlers receive the concrete event type, not a wrapper.
