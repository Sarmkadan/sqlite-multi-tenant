# IDomainEventHandler

`IDomainEventHandler<T>` is a generic interface in the `sqlite-multi-tenant` project that defines a contract for handling domain events within the system. Implementations process specific event types asynchronously, enabling decoupled reactions to tenant lifecycle changes, backup operations, and migration completions. The interface follows the Mediator pattern, where event dispatchers invoke handlers without knowledge of the concrete handler logic.

## API

### IDomainEventHandler\<T\>

A generic marker interface that binds a handler to a specific domain event type. The type parameter `T` must derive from `DomainEvent`.

**Type Parameters**
- `T` : The domain event type this handler processes.

**Members**

#### HandleAsync

```csharp
Task HandleAsync(T domainEvent, CancellationToken cancellationToken = default)
```

Executes the handler logic for the given domain event.

**Parameters**
- `domainEvent` : The event instance containing contextual data required for processing.
- `cancellationToken` : An optional token that signals cancellation of the asynchronous operation.

**Return Value**
A `Task` representing the asynchronous operation.

**Exceptions**
- Throws `ArgumentNullException` when `domainEvent` is `null`.
- Implementations may throw domain-specific exceptions (e.g., `InvalidOperationException` if prerequisite state is missing).

---

### TenantCreatedEventHandler

```csharp
public sealed class TenantCreatedEventHandler : IDomainEventHandler<TenantCreatedNotificationEvent>
```

Handles `TenantCreatedNotificationEvent` instances. Sealed to prevent further derivation.

**Constructor**
```csharp
public TenantCreatedEventHandler()
```
Initializes a new instance. Dependencies are injected via constructor parameters (not shown in the public surface).

**HandleAsync**
```csharp
public async Task HandleAsync(TenantCreatedNotificationEvent domainEvent, CancellationToken cancellationToken = default)
```
Processes tenant creation notifications—typically sending alerts, provisioning resources, or logging audit records.

---

### TenantDeletedEventHandler

```csharp
public sealed class TenantDeletedEventHandler : IDomainEventHandler<TenantDeletedEvent>
```

Handles `TenantDeletedEvent` instances.

**Constructor**
```csharp
public TenantDeletedEventHandler()
```
Initializes a new instance.

**HandleAsync**
```csharp
public async Task HandleAsync(TenantDeletedEvent domainEvent, CancellationToken cancellationToken = default)
```
Processes tenant deletion events—commonly triggering cleanup routines, removing associated data, or notifying downstream services.

---

### BackupCompletedEventHandler

```csharp
public sealed class BackupCompletedEventHandler : IDomainEventHandler<BackupCompletedNotificationEvent>
```

Handles `BackupCompletedNotificationEvent` instances.

**Constructor**
```csharp
public BackupCompletedEventHandler()
```
Initializes a new instance.

**HandleAsync**
```csharp
public async Task HandleAsync(BackupCompletedNotificationEvent domainEvent, CancellationToken cancellationToken = default)
```
Processes backup completion notifications—verifying backup integrity, updating status records, or triggering archival workflows.

---

### MigrationCompletedEventHandler

```csharp
public sealed class MigrationCompletedEventHandler : IDomainEventHandler<MigrationCompletedEvent>
```

Handles `MigrationCompletedEvent` instances.

**Constructor**
```csharp
public MigrationCompletedEventHandler()
```
Initializes a new instance.

**HandleAsync**
```csharp
public async Task HandleAsync(MigrationCompletedEvent domainEvent, CancellationToken cancellationToken = default)
```
Processes migration completion events—validating schema changes, updating version metadata, or enabling features dependent on the new schema.

---

### TenantCreatedNotificationEvent

```csharp
public sealed class TenantCreatedNotificationEvent : DomainEvent
```

Represents the event raised when a new tenant is created. Inherits from `DomainEvent`.

**Constructor**
```csharp
public TenantCreatedNotificationEvent() : base(nameof(TenantCreatedNotificationEvent))
```
Initializes the event with its type name as the event identifier.

**Properties**
- `TenantId` (`string`) : The unique identifier of the created tenant.
- `TenantName` (`string`) : The display name assigned to the tenant.
- `TenantDescription` (`string?`) : An optional description for the tenant. May be `null`.

---

### TenantDeletedEvent

```csharp
public sealed class TenantDeletedEvent : DomainEvent
```

Represents the event raised when a tenant is deleted.

**Constructor**
*(Inherited from `DomainEvent`; specific constructor signature not shown in public surface beyond the base call pattern.)*

**Properties**
- `TenantId` (`string`) : The unique identifier of the deleted tenant.
- `TenantName` (`string`) : The display name of the tenant at the time of deletion.

---

## Usage

### Example 1: Handling a Tenant Creation Notification

```csharp
// Assume handler is resolved via dependency injection
var handler = serviceProvider.GetRequiredService<IDomainEventHandler<TenantCreatedNotificationEvent>>();

var tenantCreatedEvent = new TenantCreatedNotificationEvent
{
    TenantId = "tenant-abc-123",
    TenantName = "Acme Corp",
    TenantDescription = "Primary tenant for Acme Corporation"
};

// Handle the event, passing a cancellation token with a 30-second timeout
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await handler.HandleAsync(tenantCreatedEvent, cts.Token);

// After this call, provisioning emails, resource setup, and audit logs are complete
```

### Example 2: Handling a Tenant Deletion with Cleanup

```csharp
var handler = serviceProvider.GetRequiredService<IDomainEventHandler<TenantDeletedEvent>>();

var tenantDeletedEvent = new TenantDeletedEvent
{
    TenantId = "tenant-xyz-789",
    TenantName = "Globex Inc"
};

try
{
    await handler.HandleAsync(tenantDeletedEvent, CancellationToken.None);
    Console.WriteLine($"Cleanup for tenant {tenantDeletedEvent.TenantName} completed.");
}
catch (InvalidOperationException ex)
{
    // Handle cases where tenant data was already partially removed
    Console.WriteLine($"Cleanup error: {ex.Message}");
}
```

---

## Notes

- **Thread Safety**: All handler implementations are `sealed` classes with no exposed mutable state. Their thread safety depends on the injected dependencies (e.g., database contexts, HTTP clients). If dependencies are not thread-safe, concurrent calls to `HandleAsync` on the same handler instance may cause race conditions. In typical DI containers, handlers are registered with transient or scoped lifetimes to avoid shared state across requests.
- **Cancellation**: Handlers accept a `CancellationToken` but individual implementations decide whether to honor it. Long-running operations (network calls, batch deletions) should periodically check `cancellationToken.IsCancellationRequested` and throw `OperationCanceledException` if cancellation is requested.
- **Event Inheritance**: `TenantCreatedNotificationEvent` and `TenantDeletedEvent` both derive from `DomainEvent`. The `TenantCreatedNotificationEvent` constructor explicitly passes its type name to the base constructor, enabling event type identification for dispatching and logging.
- **Nullability**: `TenantDescription` in `TenantCreatedNotificationEvent` is nullable (`string?`). Handlers must guard against `null` when formatting notifications or storing metadata.
- **Missing Properties**: `TenantDeletedEvent` exposes only `TenantId` and `TenantName`. Handlers that require additional context (e.g., deletion timestamp, initiating user) must obtain it from other sources, as the event payload is intentionally minimal.
- **Sealed Classes**: All handler and event classes are `sealed`, preventing inheritance. This design enforces explicit handler registration and avoids unexpected polymorphism in event processing pipelines.
