# IEventBus

The `IEventBus` interface defines the contract for an in-memory event distribution system within the `sqlite-multi-tenant` project, facilitating decoupled communication between components via a publish-subscribe pattern. It supports asynchronous message dispatching, dynamic subscription management, and includes a built-in dead letter queue mechanism to capture and inspect events that fail during processing, ensuring robustness in multi-tenant environments where event reliability is critical.

## API

The following members constitute the public surface area of the `EventBus` implementation.

### `EventBus` Class

The concrete implementation of the event bus logic.

*   **Constructor**: `public EventBus()`
    *   Initializes a new instance of the `EventBus` class with an empty subscriber list and an initialized dead letter queue.

*   **Method**: `public async Task PublishAsync<T>(T event)`
    *   **Purpose**: Dispatches an event of type `T` to all currently registered subscribers for that type.
    *   **Parameters**: `event` – The event instance to publish.
    *   **Return Value**: A `Task` representing the asynchronous operation. The task completes when all subscribers have been invoked or if an internal dispatch error occurs.
    *   **Exceptions**: May throw if a subscriber handler throws an unhandled exception that propagates up, potentially causing the event to be routed to the dead letter queue depending on internal error handling logic.

*   **Method**: `public async Task SubscribeAsync<T>(Func<T, Task> handler)`
    *   **Purpose**: Registers an asynchronous handler to receive events of type `T`.
    *   **Parameters**: `handler` – The asynchronous delegate to invoke when an event of type `T` is published.
    *   **Return Value**: A `Task` that completes when the subscription is successfully registered.
    *   **Exceptions**: Throws `ArgumentNullException` if `handler` is null.

*   **Method**: `public async Task UnsubscribeAsync<T>(Func<T, Task> handler)`
    *   **Purpose**: Removes a previously registered handler for events of type `T`.
    *   **Parameters**: `handler` – The delegate to remove.
    *   **Return Value**: A `Task` that completes when the unsubscription is processed.
    *   **Exceptions**: No exception is thrown if the handler was not previously subscribed; the operation is idempotent.

*   **Property**: `public int GetSubscriberCount<T>()`
    *   **Purpose**: Retrieves the current number of active subscribers for a specific event type `T`.
    *   **Return Value**: An integer representing the count of subscribers.
    *   **Exceptions**: None.

*   **Property**: `public DeadLetterQueue GetDeadLetterQueue { get; }`
    *   **Purpose**: Provides access to the queue storing events that failed during processing.
    *   **Return Value**: An instance of `DeadLetterQueue`.
    *   **Exceptions**: None.

### `DeadLetterQueue` Class

Manages storage and retrieval of events that failed execution.

*   **Constructor**: `public DeadLetterQueue()`
    *   Initializes a new instance of the dead letter queue.

*   **Method**: `public async Task EnqueueAsync<T>(T event, Exception exception)`
    *   **Purpose**: Adds a failed event and its associated exception to the queue.
    *   **Parameters**: 
        *   `event` – The event instance that failed.
        *   `exception` – The exception caught during processing.
    *   **Return Value**: A `Task` representing the asynchronous enqueue operation.
    *   **Exceptions**: Throws `ArgumentNullException` if `event` or `exception` is null.

*   **Method**: `public async Task<List<FailedEvent>> GetFailedEventsAsync()`
    *   **Purpose**: Retrieves a snapshot of all currently stored failed events.
    *   **Return Value**: A `List<FailedEvent>` containing the serialized failure records.
    *   **Exceptions**: None.

*   **Method**: `public async Task<bool> RemoveAsync(string id)`
    *   **Purpose**: Removes a specific failed event from the queue by its unique identifier.
    *   **Parameters**: `id` – The unique identifier of the failed event.
    *   **Return Value**: A `Task<bool>` indicating whether the event was found and removed (`true`) or not found (`false`).
    *   **Exceptions**: Throws `ArgumentNullException` if `id` is null or empty.

*   **Method**: `public async Task<int> GetCountAsync()`
    *   **Purpose**: Returns the total number of events currently held in the dead letter queue.
    *   **Return Value**: An integer count.
    *   **Exceptions**: None.

### `FailedEvent` Class

A data transfer object representing a captured failure.

*   **Properties**:
    *   `public string Id`: The unique identifier generated upon failure capture.
    *   `public string EventType`: The full type name of the original event.
    *   `public string EventData`: The serialized payload of the original event.
    *   `public string Exception`: The message of the exception that caused the failure.
    *   `public string? StackTrace`: The stack trace of the exception, if available; otherwise null.
    *   `public DateTime FailedAt`: The UTC timestamp when the failure occurred.

## Usage

### Example 1: Basic Publish-Subscribe Pattern

This example demonstrates subscribing to a `TenantCreatedEvent`, publishing it, and verifying the subscriber count.

```csharp
using System;
using System.Threading.Tasks;

public class TenantCreatedEvent 
{
    public string TenantId { get; set; }
    public string Name { get; set; }
}

public async Task RunBasicFlow()
{
    var bus = new EventBus();

    // Define a handler
    Func<TenantCreatedEvent, Task> handler = async (evt) =>
    {
        Console.WriteLine($"Tenant created: {evt.Name} (ID: {evt.TenantId})");
        await Task.Delay(10); // Simulate work
    };

    // Subscribe
    await bus.SubscribeAsync<TenantCreatedEvent>(handler);

    // Verify subscription
    int count = bus.GetSubscriberCount<TenantCreatedEvent>();
    Console.WriteLine($"Active subscribers: {count}"); // Output: 1

    // Publish event
    var newTenant = new TenantCreatedEvent { TenantId = "t_123", Name = "Acme Corp" };
    await bus.PublishAsync(newTenant);

    // Unsubscribe
    await bus.UnsubscribeAsync<TenantCreatedEvent>(handler);
}
```

### Example 2: Handling Failures via Dead Letter Queue

This example simulates a handler failure, captures the error in the dead letter queue, and inspects the resulting `FailedEvent`.

```csharp
using System;
using System.Threading.Tasks;
using System.Linq;

public class PaymentProcessedEvent 
{
    public decimal Amount { get; set; }
}

public async Task RunFailureHandling()
{
    var bus = new EventBus();

    // Subscribe with a handler that intentionally throws
    Func<PaymentProcessedEvent, Task> failingHandler = async (evt) =>
    {
        if (evt.Amount < 0)
        {
            throw new InvalidOperationException("Amount cannot be negative");
        }
        await Task.CompletedTask;
    };

    await bus.SubscribeAsync<PaymentProcessedEvent>(failingHandler);

    // Publish an event that triggers the exception
    var badEvent = new PaymentProcessedEvent { Amount = -50.00m };
    
    try 
    {
        await bus.PublishAsync(badEvent);
    }
    catch (Exception) 
    {
        // In some implementations, exceptions might propagate; 
        // assuming internal catch routes to DLQ for this example context.
    }

    // Inspect Dead Letter Queue
    var dlq = bus.GetDeadLetterQueue;
    var failedEvents = await dlq.GetFailedEventsAsync();

    if (failedEvents.Any())
    {
        var failure = failedEvents.First();
        Console.WriteLine($"Failed Event Type: {failure.EventType}");
        Console.WriteLine($"Error Message: {failure.Exception}");
        Console.WriteLine($"Occurred At: {failure.FailedAt}");

        // Remove the processed failure
        bool removed = await dlq.RemoveAsync(failure.Id);
        Console.WriteLine($"Cleanup successful: {removed}");
    }
}
```

## Notes

*   **Thread Safety**: The `EventBus` implementation utilizes asynchronous patterns for all state-modifying operations (`SubscribeAsync`, `UnsubscribeAsync`, `PublishAsync`). While the use of `async` suggests non-blocking I/O or concurrency handling, callers should ensure that subscription modifications do not occur simultaneously with high-frequency publishing in race-condition-sensitive scenarios unless the underlying collection types within `EventBus` are explicitly concurrent. The `GetSubscriberCount<T>` method provides a point-in-time snapshot and may not reflect immediate changes if called concurrently with subscription updates.
*   **Exception Propagation**: If a subscriber handler throws an exception during `PublishAsync`, the behavior depends on the internal implementation of the dispatch loop. Based on the presence of `DeadLetterQueue`, it is expected that exceptions are caught internally, the event is serialized to the DLQ via `EnqueueAsync`, and the exception may or may not be re-thrown to the caller. Consumers should wrap `PublishAsync` in try-catch blocks if strict transactional consistency is required.
*   **Handler Equality**: `UnsubscribeAsync` relies on delegate equality to remove handlers. If anonymous lambdas are used for subscription, they cannot be unsubscribed later unless the specific delegate instance is stored and passed to `UnsubscribeAsync`. It is recommended to store handler references in variables or use named methods for manageable lifecycle control.
*   **Data Serialization**: The `FailedEvent` class stores `EventData` as a string. This implies that the `EventBus` performs serialization (likely JSON) when an event fails. Complex objects with circular references or non-serializable properties in the event payload may cause secondary failures during the dead letter enqueueing process.
