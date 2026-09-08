# EventBus

`EventBus` is a sealed, in-process publish/subscribe implementation for `DomainEvent` types. It registers asynchronous handlers by concrete event type, invokes matching handlers in parallel, and records individual handler failures in an in-memory dead-letter queue.

The bus requires an `ILogger<EventBus>` and keeps its subscriptions and failed events only for the lifetime of the instance. It does not provide durable storage or cross-process delivery.

## `IEventBus`

`IEventBus` defines the core asynchronous publish/subscribe contract. Every generic event type must derive from `DomainEvent`.

### `PublishAsync<T>`

```csharp
Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
    where T : DomainEvent;
```

Publishes `event` to every handler registered for the exact type `T`. If no handlers are registered, the method logs a warning and completes. The cancellation token is part of the public contract but is not passed to handlers or otherwise observed by the current implementation.

### `SubscribeAsync<T>`

```csharp
Task SubscribeAsync<T>(Func<T, Task> handler)
    where T : DomainEvent;
```

Registers `handler` for events of type `T`. Registering the same delegate more than once creates multiple subscriptions and causes it to be invoked once per registration.

### `UnsubscribeAsync<T>`

```csharp
Task UnsubscribeAsync<T>(Func<T, Task> handler)
    where T : DomainEvent;
```

Removes one matching registration for `handler` from type `T`. The method completes without error when the event type or handler is not registered.

## `EventBus`

### Constructor

```csharp
public EventBus(ILogger<EventBus> logger)
```

Creates an empty subscriber registry and a new `DeadLetterQueue`, and uses `logger` for publication, subscription, and failure messages.

### `PublishAsync<T>`

```csharp
public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
    where T : DomainEvent
```

Looks up handlers by `typeof(T)` and invokes all matches concurrently through `Task.WhenAll`. An exception from one handler does not prevent the other handlers from running. Each failed invocation is logged and separately added to the dead-letter queue. Handler exceptions and other exceptions caught by the outer publication block are logged rather than propagated to the caller.

### `SubscribeAsync<T>`

```csharp
public Task SubscribeAsync<T>(Func<T, Task> handler)
    where T : DomainEvent
```

Adds a handler to the list for `T`. Updates to the subscriber registry are serialized with a semaphore.

### `UnsubscribeAsync<T>`

```csharp
public Task UnsubscribeAsync<T>(Func<T, Task> handler)
    where T : DomainEvent
```

Removes the first matching handler registration for `T`, if one exists. Registry updates are serialized with the same semaphore used by subscription.

Keep the original delegate if it will later be unsubscribed; a newly created lambda is a different delegate and normally will not match the registered handler.

### `GetSubscriberCount<T>`

```csharp
public int GetSubscriberCount<T>() where T : DomainEvent
```

Returns the number of handlers currently registered for the exact event type `T`, or `0` when the type has no subscriber list. This method is available on `EventBus` but is not a member of `IEventBus`.

### `GetDeadLetterQueue`

```csharp
public DeadLetterQueue GetDeadLetterQueue()
```

Returns the dead-letter queue owned by this bus instance. The same queue instance is returned on every call. This method is available on `EventBus` but is not a member of `IEventBus`.

## `DeadLetterQueue`

`DeadLetterQueue` is a sealed, semaphore-protected, in-memory collection of `FailedEvent` records. It retains at most 1,000 entries. When a new failure arrives at capacity, the oldest entry is removed before the new one is appended.

### `EnqueueAsync<T>`

```csharp
public Task EnqueueAsync<T>(T @event, Exception exception)
    where T : DomainEvent
```

Serializes the event with `System.Text.Json.JsonSerializer`, captures information from the exception, assigns a new string GUID, records the current UTC time, initializes `RetryCount` to zero, and appends the resulting `FailedEvent`.

### `GetFailedEventsAsync`

```csharp
public Task<List<FailedEvent>> GetFailedEventsAsync()
```

Returns a new list containing the queued records in insertion order. Changing the returned list does not change the queue, although the contained `FailedEvent` objects are the same mutable instances held by the queue.

### `RemoveAsync`

```csharp
public Task<bool> RemoveAsync(string failedEventId)
```

Finds the first queued record whose `Id` equals `failedEventId`. It removes that record and returns `true`; if no record matches, it returns `false`.

### `GetCountAsync`

```csharp
public Task<int> GetCountAsync()
```

Returns the current number of records in the queue.

## `FailedEvent`

`FailedEvent` is the mutable DTO stored by `DeadLetterQueue`.

| Field | Type | Description |
| --- | --- | --- |
| `Id` | `string` | A GUID formatted as a string and generated when the failure is enqueued. |
| `EventType` | `string` | The simple CLR type name of the event, from `typeof(T).Name`. |
| `EventData` | `string` | The event serialized as JSON. |
| `Exception` | `string` | The exception message. |
| `StackTrace` | `string?` | The exception stack trace, or `null` when unavailable. |
| `FailedAt` | `DateTime` | The UTC time at which the failure record was created. |
| `RetryCount` | `int` | A retry counter initialized to `0`; the queue does not increment it automatically. |

## Subscribe and publish example

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Events;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole());

var eventBus = new EventBus(loggerFactory.CreateLogger<EventBus>());

Func<TenantCreatedEvent, Task> handler = createdEvent =>
{
    Console.WriteLine(
        $"Tenant created: {createdEvent.TenantName} ({createdEvent.TenantId})");
    return Task.CompletedTask;
};

await eventBus.SubscribeAsync(handler);

await eventBus.PublishAsync(new TenantCreatedEvent
{
    TenantId = "tenant-42",
    TenantName = "Example Tenant",
    ContactEmail = "owner@example.com"
});

Console.WriteLine(eventBus.GetSubscriberCount<TenantCreatedEvent>()); // 1

await eventBus.UnsubscribeAsync(handler);
```

If a handler throws, `PublishAsync` still completes after all handlers finish. The failure can be inspected through `eventBus.GetDeadLetterQueue().GetFailedEventsAsync()`.
