# IEventPublisher

The `IEventPublisher` interface provides a lightweight publish‑subscribe mechanism for domain events within the sqlite‑multi‑tenant application. It allows components to raise events asynchronously, register handlers synchronously, and query the number of registered handlers for a given event type. The default implementation, `EventPublisher`, respects configurable options controlling async execution, handler timeouts, and error‑continuation behavior.

## API

### IEventPublisher

#### `Task PublishAsync<T>(T domainEvent)` where `T : DomainEvent`
Publishes a domain event to all currently subscribed handlers.  
- **Parameters**  
  - `domainEvent`: The event instance to publish. Must not be `null`.  
- **Return Value**  
  - A `Task` that completes when all handlers have finished processing the event (respecting the publisher’s async mode and timeout settings).  
- **Exceptions**  
  - `ArgumentNullException` if `domainEvent` is `null`.  
  - `OperationCanceledException` if the publishing operation is cancelled via a cancellation token internal to the publisher.  
  - Any exception thrown by a handler is propagated unless `ContinueOnHandlerException` is set to `true`; in that case the exception is logged and publishing continues.

#### `void Subscribe<T>(IEventHandler<T> handler)` where `T : DomainEvent`
Registers a handler to receive events of type `T`.  
- **Parameters**  
  - `handler`: The handler instance to subscribe. Must not be `null`.  
- **Return Value**  
  - None.  
- **Exceptions**  
  - `ArgumentNullException` if `handler` is `null`.  
  - InvalidOperationException if the same handler instance is already subscribed for `T` (duplicates are not allowed).

#### `int GetHandlerCount<T>()` where `T : DomainEvent`
Returns the number of handlers currently subscribed to events of type `T`.  
- **Parameters**  
  - None.  
- **Return Value**  
  - An integer indicating the count of registered handlers for `T`. Returns `0` if no handlers are subscribed.  
- **Exceptions**  
  - None.

### EventPublisher (default implementation of IEventPublisher)

#### `EventPublisher(EventPublisherOptions options = null)`
Creates a new publisher instance.  
- **Parameters**  
  - `options`: Optional configuration; if `null`, default options are used (`EnableAsyncPublishing = true`, `HandlerTimeoutSeconds = 5`, `ContinueOnHandlerException = false`).  
- **Return Value**  
  - A new `EventPublisher` ready for use.  
- **Exceptions**  
  - `ArgumentOutOfRangeException` if any` if `options.HandlerTimeoutSeconds` is less than `1`.

### EventPublisherOptions

#### `bool EnableAsyncPublishing { get; set; }`
When `true`, `PublishAsync` returns immediately after invoking handlers; handlers are executed on thread‑pool threads. When `false`, handlers are invoked synchronously on the caller’s thread. Defaults to `true`.

#### `int HandlerTimeoutSeconds { get; set; }`
Maximum time (in seconds) the publisher waits for a single handler to complete before considering it timed out. Only applies when `EnableAsyncPublishing` is `true`. Defaults to `5`. Must be greater than `0`.

#### `bool ContinueOnHandlerException { get; set; }`
When `true`, an exception thrown by a handler does not abort the publishing process; the exception is logged and processing proceeds to the next handler. When `false`, the first handler exception aborts publishing and is propagated to the caller. Defaults to `false`.

### LoggingEventHandler<T> where `T : DomainEvent`

#### `LoggingEventHandler(ILogger<LoggingEventHandler<T>> logger)`
Creates a handler that logs the receipt and handling of a domain event.  
- **Parameters**  
  - `logger`: The logger instance used for logging; must not be `null`.  
- **Return Value**  
  - A new `LoggingEventHandler<T>` ready to be subscribed.  
- **Exceptions**  
  - `ArgumentNullException` if `logger` is `null`.

#### `Task HandleAsync(T domainEvent)`
Processes the domain event by logging its type and payload at the `Information` level.  
- **Parameters**  
  - `domainEvent`: The event to handle; must not be `null`.  
- **Return Value**  
  - A completed `Task`.  
- **Exceptions**  
  - `ArgumentNullException` if `domainEvent` is `null`.  
  - Any exception thrown by the underlying logger is propagated.

## Usage

### Basic publishing with default options
```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Events;

// Assume DomainEvent and IEventHandler<T> are defined elsewhere.
var publisher = new EventPublisher(); // uses default options

// Subscribe a simple logger‑based handler.
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<LoggingEventHandler<OrderCreated>>;
var handler = new LoggingEventHandler<OrderCreated>(logger);
publisher.Subscribe(handler);

// Publish an event.
var @event = new OrderCreated { OrderId = 42, CustomerId = 7 };
await publisher.PublishAsync(@event);
```

### Custom options and synchronous handling
```csharp
var options = new EventPublisherOptions
{
    EnableAsyncPublishing = false,          // run handlers on the caller's thread
    HandlerTimeoutSeconds = 10,
    ContinueOnHandlerException = true       // log errors but keep processing
};

var publisher = new EventPublisher(options);

// Subscribe multiple handlers.
publisher.Subscribe(new AuditLogHandler());
publisher.Subscribe(new NotificationHandler());

// Publish; this call blocks until all handlers finish.
await publisher.PublishAsync(new InventoryAdjusted { ProductId = 13, Delta = -5 });
```

## Notes
- **Thread‑safety**: `Subscribe<T>`, `Unsubscribe<T>` (if present), and `GetHandlerCount<T>` are thread‑safe and may be called concurrently with `PublishAsync`. Handlers themselves are invoked concurrently when `EnableAsyncPublishing` is `true`; implementers must ensure their handlers are thread‑safe or rely on the publisher’s serialization guarantees.
- **Handler lifetime**: The publisher does not take ownership of subscribed handlers; callers must manage handler disposal and ensure handlers are unsubscribed before they are disposed to avoid invoking disposed instances.
- **Exception handling**: When `ContinueOnHandlerException` is `false`, the first handler exception aborts further processing and is bubbled up to the caller of `PublishAsync`. When `true`, exceptions are logged (via the handler’s own logging mechanism or the publisher’s internal logger if provided) and publishing proceeds to the next handler.
- **Timeout enforcement**: Timeouts are only respected when asynchronous publishing is enabled. A handler that exceeds `HandlerTimeoutSeconds` results in a `TimeoutException` being thrown from `PublishAsync` (unless `ContinueOnHandlerException` suppresses it).
- **Null checks**: All public members validate their arguments for `null` and throw `ArgumentNullException` accordingly; callers should ensure non‑null inputs.
