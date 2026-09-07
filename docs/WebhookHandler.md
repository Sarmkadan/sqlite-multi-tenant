# WebhookHandler

`WebhookHandler` is the in-memory `IWebhookHandler` implementation in `src/Integration/WebhookHandler.cs`. It stores webhook subscriptions for the lifetime of the handler instance and sends a `WebhookDelivery` as an HTTP `POST` containing the JSON-serialized domain event.

## Public API

### `WebhookHandler`

```csharp
public WebhookHandler(HttpClient httpClient, ILogger<WebhookHandler> logger)
```

Creates a handler with the supplied HTTP client and logger. A null argument causes `ArgumentNullException`.

```csharp
public Task DeliverAsync(WebhookDelivery delivery, CancellationToken cancellationToken)
```

Serializes `delivery.Event`, posts it to `delivery.Url` as `application/json`, and adds every entry in `delivery.Headers` to the content headers. A null `delivery` causes `ArgumentNullException`.

Each HTTP attempt uses a linked cancellation token with a 30-second timeout. The caller's cancellation token can therefore cancel the request earlier. An unsuccessful HTTP response or caught exception advances `RetryCount`; while it remains at or below `MaxRetries`, the method waits using exponential delays of 2, 4, 8, ... seconds. With the defaults (`RetryCount = 0`, `MaxRetries = 3`), at most four HTTP attempts are made. Delivery failures are logged and are not returned as a result or rethrown by the request loop. Cancellation can still propagate from the retry `Task.Delay`, which uses the caller's token.

```csharp
public Task RegisterAsync(WebhookHandlerSubscription subscription)
```

Adds or replaces the in-memory subscription keyed by `subscription.WebhookId`. A null subscription causes `ArgumentNullException`; the other subscription fields are not validated.

```csharp
public Task UnregisterAsync(string webhookId)
```

Removes the matching in-memory subscription. A null, empty, or whitespace ID is accepted as a no-op. Removing an unknown ID is also a no-op.

```csharp
public List<WebhookHandlerSubscription> GetSubscriptions(string eventType)
```

Returns a new list containing enabled subscriptions whose `EventType` exactly equals `eventType`, plus enabled wildcard (`"*"`) subscriptions. Comparison is case-sensitive. The method does not validate `eventType`.

### `WebhookDelivery`

```csharp
public bool VerifySignature(string secret)
```

Validates the delivery's `X-Signature` header against the JSON serialization of `Event`:

- A null or empty `secret` causes `ArgumentException`.
- A missing or empty `X-Signature` header returns `false`.
- Otherwise, verification delegates to `WebhookService.VerifySignature`, which computes an HMAC-SHA256 digest with the UTF-8 secret and payload, represents it as hexadecimal, and compares it case-insensitively.
- Header lookup uses the dictionary's configured comparer; the default `Headers` dictionary therefore treats `X-Signature` casing as significant.

The payload must serialize identically to the payload that was signed. The companion `WebhookService` emits the same `X-Signature` header, but its delivery envelope differs from the bare `DomainEvent` serialized by `WebhookHandler`, so callers should verify the exact body associated with the signature.

### `WebhookEventHandler<T>`

```csharp
public WebhookEventHandler(
    IWebhookHandler webhookHandler,
    ILogger<WebhookEventHandler<T>> logger)
```

Creates the generic domain-event handler. A null dependency causes `ArgumentNullException`.

```csharp
public Task HandleAsync(T @event, CancellationToken cancellationToken)
```

Logs the event type and completes. The current implementation does not query subscriptions or call `IWebhookHandler.DeliverAsync`; the cancellation token is not used.

## Related components

[`IWebhookHandler`](IWebhookHandler.md) is the contract implemented by `WebhookHandler`. It exposes `DeliverAsync`, `RegisterAsync`, and `UnregisterAsync`; `GetSubscriptions` belongs only to the concrete class.

[`WebhookService`](WebhookService.md) is a separate, higher-level in-memory subscription and delivery implementation. It owns its `HttpClient`, creates signed delivery envelopes, tracks failures, and exposes the static signature verifier used by `WebhookDelivery.VerifySignature`. Registrations held by `WebhookHandler` and `WebhookService` are independent and are not synchronized.

## Example

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Integration;

using var httpClient = new HttpClient();
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

var handler = new WebhookHandler(
    httpClient,
    loggerFactory.CreateLogger<WebhookHandler>());

var subscription = new WebhookHandlerSubscription
{
    WebhookId = "billing-events",
    Url = "https://partner.example.com/webhooks",
    EventType = "invoice.paid",
    Headers = new Dictionary<string, string>
    {
        ["X-Correlation-Source"] = "billing"
    }
};

await handler.RegisterAsync(subscription);

var matchingSubscriptions = handler.GetSubscriptions("invoice.paid");
var domainEvent = new CustomDomainEvent("invoice.paid")
{
    TenantId = "tenant-42",
    Data = new Dictionary<string, object>
    {
        ["invoiceId"] = 1234
    }
};

foreach (var match in matchingSubscriptions)
{
    await handler.DeliverAsync(
        new WebhookDelivery
        {
            WebhookId = match.WebhookId,
            Url = match.Url,
            Event = domainEvent,
            Headers = new Dictionary<string, string>(match.Headers)
        },
        CancellationToken.None);
}
```

`RegisterAsync` does not automatically deliver to matching subscriptions; the caller must create and submit each `WebhookDelivery`, as shown above.
