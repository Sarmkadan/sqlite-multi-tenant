# EventPublisher

`EventPublisher` is a sealed, in-memory publisher for dispatching `DomainEvent` instances to strongly typed handlers within one process. It stores handlers by their exact event type, invokes all handlers registered for a published type in parallel, and logs publishing activity and handler failures. Subscriptions exist only for the lifetime of the publisher instance; they are not durable or shared between processes.

## Constructor

```csharp
public EventPublisher(ILogger<EventPublisher> logger)
```

Creates an empty publisher. The `logger` dependency records publication, subscription, completion, missing-handler, and error information. Passing `null` throws `ArgumentNullException`.

The publisher has no `EventPublisherOptions` constructor dependency. Although `EventPublisherOptions` is declared in the same source file, the current implementation does not consume it.

## Public methods

### `PublishAsync<T>`

```csharp
public Task PublishAsync<T>(
    T @event,
    CancellationToken cancellationToken = default)
    where T : DomainEvent
```

Publishes `event` to every handler registered for the exact compile-time type `T`. If no handlers are registered, the method logs that fact and completes. Registered handlers are invoked and their tasks are awaited together with `Task.WhenAll`, so handlers can run concurrently.

The cancellation token is passed to every `IEventHandler<T>` and is also used by any internally wrapped synchronous delegate. Passing a null event throws `ArgumentNullException`. Exceptions thrown while invoking or awaiting handlers are logged and are not rethrown to the caller; failure of one asynchronous handler does not prevent the other already-created handler tasks from running.

### `Subscribe<T>`

```csharp
public void Subscribe<T>(IEventHandler<T> handler)
    where T : DomainEvent
```

Registers an `IEventHandler<T>` for the exact event type `T`. Multiple handlers, including repeated subscriptions of the same handler instance, can be registered. Each subscription adds a delegate that forwards the event and cancellation token to `handler.HandleAsync`. Passing `null` throws `ArgumentNullException`.

There is no corresponding unsubscribe operation on `EventPublisher`.

### `GetHandlerCount<T>`

```csharp
public int GetHandlerCount<T>()
    where T : DomainEvent
```

Returns the number of registrations for the exact event type `T`, or `0` when none exist. This is a concrete `EventPublisher` convenience method and is not part of `IEventPublisher`.

## Relationship to IEventPublisher

`EventPublisher` implements the `IEventPublisher` contract documented in [IEventPublisher.md](IEventPublisher.md). The interface declares `PublishAsync<T>` and `Subscribe<T>`, which lets consumers depend on the abstraction and allows the implementation to be registered with dependency injection:

```csharp
services.AddSingleton<IEventPublisher, EventPublisher>();
```

`GetHandlerCount<T>` is available only when the instance is referenced as `EventPublisher`. The implementation also uses `IEventHandler<T>` as its subscriber abstraction.

## Relationship to EventBus

`EventPublisher` and `EventBus` are separate in-process publish/subscribe implementations; `EventPublisher` does not wrap, forward to, or otherwise depend on `EventBus`.

- `EventPublisher` implements `IEventPublisher` and subscribes `IEventHandler<T>` objects with the synchronous `Subscribe<T>` method.
- `EventBus` implements `IEventBus` and subscribes or unsubscribes `Func<T, Task>` delegates through asynchronous methods.
- Both dispatch matching handlers concurrently and log failures.
- `EventBus` places individual failed deliveries in a `DeadLetterQueue`; `EventPublisher` only logs handler failures.
- `EventBus` supports unsubscription, while `EventPublisher` does not.

Choose and inject the contract used by the surrounding component rather than expecting subscriptions made on one implementation to appear on the other.

## Usage example

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Events;

var publisher = new EventPublisher(
    NullLogger<EventPublisher>.Instance);

publisher.Subscribe(new TenantCreatedAuditHandler());

var tenantCreated = new TenantCreatedEvent
{
    TenantId = "tenant-42",
    TenantName = "Northwind",
    ContactEmail = "admin@northwind.example"
};

await publisher.PublishAsync(tenantCreated);

Console.WriteLine(
    $"Registered handlers: {publisher.GetHandlerCount<TenantCreatedEvent>()}");

sealed class TenantCreatedAuditHandler : IEventHandler<TenantCreatedEvent>
{
    public Task HandleAsync(
        TenantCreatedEvent @event,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Tenant created: {@event.TenantName}");
        return Task.CompletedTask;
    }
}
```

Keep subscription changes and publication coordinated by the application. The internal handler dictionary and lists are not synchronized for concurrent reads and writes.
