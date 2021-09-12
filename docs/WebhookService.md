# WebhookService

`WebhookService` manages outbound webhook subscriptions and delivery within a multi-tenant SQLite environment. It allows tenants to register URLs that receive HTTP callbacks when named events occur, tracks delivery state and failure counts, and provides methods to trigger delivery to all active subscribers of a given event type.

## API

### WebhookService

```csharp
public sealed class WebhookService
```

A sealed service class responsible for persisting webhook subscriptions and executing HTTP deliveries. Instantiation is tenant-aware; the underlying connection or context is scoped to a single tenant.

---

### WebhookService Constructor

```csharp
public WebhookService(/* tenant-scoped dependencies */)
```

Creates a new instance bound to the current tenant's data store. The exact constructor parameters depend on the dependency injection configuration of the host project.

---

### SubscribeAsync

```csharp
public async Task<string> SubscribeAsync(string eventType, string webhookUrl,
    Dictionary<string, string>? headers = null, string? secret = null)
```

Registers a new webhook subscription for the tenant.

**Parameters:**
- `eventType` — A non-empty string identifying the event to subscribe to (e.g., `"order.created"`).
- `webhookUrl` — The fully qualified HTTPS URL to which event payloads will be POSTed.
- `headers` — Optional dictionary of custom HTTP headers to include with each delivery.
- `secret` — Optional shared secret used for HMAC signature generation on outgoing payloads.

**Returns:** The unique identifier (`string`) assigned to the newly created subscription.

**Throws:**
- `ArgumentException` if `eventType` or `webhookUrl` is null or whitespace.
- `InvalidOperationException` if the subscription limit for the tenant has been reached.

---

### UnsubscribeAsync

```csharp
public async Task<bool> UnsubscribeAsync(string subscriptionId)
```

Removes a subscription by its identifier. All delivery state and history associated with the subscription is deleted.

**Parameters:**
- `subscriptionId` — The unique identifier returned by `SubscribeAsync`.

**Returns:** `true` if the subscription existed and was removed; `false` if no subscription with the given ID was found.

**Throws:**
- `ArgumentException` if `subscriptionId` is null or whitespace.

---

### TriggerWebhooksAsync

```csharp
public async Task TriggerWebhooksAsync(string eventType, object payload)
```

Delivers the given payload to all active subscribers of the specified event type. Each delivery is an HTTP POST with a JSON body. The method iterates through matching subscriptions, sends requests, and updates `LastDeliveryAt` and `FailureCount` on each subscription based on the outcome.

**Parameters:**
- `eventType` — The event type whose subscribers should be notified.
- `payload` — An object that will be serialized to JSON as the request body.

**Returns:** A task that completes when all delivery attempts have finished. The method does not throw on individual delivery failures; those are recorded per subscription.

**Throws:**
- `ArgumentException` if `eventType` is null or whitespace.
- `ArgumentNullException` if `payload` is null.

---

### GetSubscriptionsAsync

```csharp
public async Task<List<WebhookSubscription>> GetSubscriptionsAsync(
    string? eventType = null, bool? isActive = null)
```

Retrieves subscriptions matching optional filters.

**Parameters:**
- `eventType` — If provided, returns only subscriptions for that event type.
- `isActive` — If provided, filters by the `IsActive` flag.

**Returns:** A list of `WebhookSubscription` objects, ordered by `CreatedAt` descending. Returns an empty list if no subscriptions match.

---

### WebhookSubscription

```csharp
public sealed class WebhookSubscription
```

A data object representing a single webhook subscription record.

**Members:**

| Member | Type | Description |
|---|---|---|
| `Id` | `string` | Unique identifier assigned at creation. |
| `EventType` | `string` | The event type this subscription listens for. |
| `WebhookUrl` | `string` | The destination URL for event payloads. |
| `Headers` | `Dictionary<string, string>` | Custom headers sent with each delivery. Never null; empty dictionary when no headers are set. |
| `Secret` | `string?` | Shared secret for payload signing, or null if not configured. |
| `CreatedAt` | `DateTime` | UTC timestamp of subscription creation. |
| `LastDeliveryAt` | `DateTime?` | UTC timestamp of the most recent delivery attempt, or null if never attempted. |
| `IsActive` | `bool` | Whether the subscription is eligible for delivery. Set to `false` automatically after a configurable number of consecutive failures. |
| `FailureCount` | `int` | Number of consecutive failed delivery attempts. Reset to zero on a successful delivery. |

## Usage

### Example 1: Subscribe, trigger, and inspect

```csharp
var webhookService = new WebhookService(tenantContext);

// Register a subscription with a secret for HMAC signing
string subId = await webhookService.SubscribeAsync(
    eventType: "invoice.paid",
    webhookUrl: "https://partner.example.com/hooks/invoice",
    headers: new Dictionary<string, string>
    {
        ["X-Custom-Header"] = "value"
    },
    secret: "whsec_shared_key"
);

// Trigger delivery to all active "invoice.paid" subscribers
var payload = new { InvoiceId = 42, Amount = 199.99m };
await webhookService.TriggerWebhooksAsync("invoice.paid", payload);

// Check delivery status
var subscriptions = await webhookService.GetSubscriptionsAsync("invoice.paid", isActive: true);
var sub = subscriptions.FirstOrDefault(s => s.Id == subId);
Console.WriteLine($"Last delivery: {sub?.LastDeliveryAt}, Failures: {sub?.FailureCount}");
```

### Example 2: Lifecycle management with deactivation and cleanup

```csharp
var webhookService = new WebhookService(tenantContext);

// Subscribe to multiple events
string orderSubId = await webhookService.SubscribeAsync("order.created", "https://api.example.com/orders");
string shipSubId = await webhookService.SubscribeAsync("shipment.dispatched", "https://api.example.com/shipments");

// Later: remove the order subscription entirely
bool removed = await webhookService.UnsubscribeAsync(orderSubId);
Console.WriteLine($"Order subscription removed: {removed}");

// Retrieve all active subscriptions across all event types
var allActive = await webhookService.GetSubscriptionsAsync(isActive: true);
foreach (var sub in allActive)
{
    Console.WriteLine($"{sub.Id}: {sub.EventType} -> {sub.WebhookUrl} (failures: {sub.FailureCount})");
}
```

## Notes

- **Tenant isolation:** All operations are scoped to the tenant associated with the `WebhookService` instance. Subscriptions created in one tenant are never visible or triggerable from another.
- **Delivery failure handling:** `TriggerWebhooksAsync` does not throw when an individual HTTP request fails. Instead, it increments `FailureCount` on that subscription. When `FailureCount` exceeds a configured threshold, `IsActive` is set to `false`, and the subscription will be skipped in future triggers until manually reactivated or the failure count is reset by a successful delivery.
- **Idempotency of `UnsubscribeAsync`:** Calling `UnsubscribeAsync` with a non-existent or already-removed ID returns `false` and does not throw. It is safe to call multiple times.
- **Thread safety:** The class is designed for use within scoped dependency injection lifetimes (e.g., per-request). It is not guaranteed to be thread-safe for concurrent calls on the same instance from multiple threads. Parallel trigger invocations for different event types should use separate instances.
- **Payload serialization:** The `payload` argument in `TriggerWebhooksAsync` is serialized using the default system JSON serializer. Callers should ensure the object graph is serializable and does not contain circular references.
- **URL validation:** `SubscribeAsync` performs basic validation that the URL is non-empty and well-formed but does not verify reachability at subscription time. Failures are only detected during delivery attempts.
- **Secret handling:** When a `Secret` is provided, an HMAC-SHA256 signature header (`X-Webhook-Signature`) is automatically appended to the outgoing request. The secret is stored as configured and never logged.
