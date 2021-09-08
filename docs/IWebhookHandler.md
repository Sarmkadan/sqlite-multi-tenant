# IWebhookHandler

The `IWebhookHandler` interface defines the contract for subscribing to and delivering domain events via webhooks in a multi-tenant SQLite environment. Implementations are responsible for registering subscriptions, delivering events to configured endpoints, and managing the lifecycle of webhook deliveries including retries and error handling.

## API

### `WebhookHandlerSubscription`
A model representing a subscription to a specific event type for a given webhook.

- **`WebhookId`**
  The unique identifier of the webhook subscription.
  Type: `string`
  Access: Public read-only property

- **`Url`**
  The destination URL where events will be delivered.
  Type: `string`
  Access: Public read-only property

- **`EventType`**
  The type of domain event this subscription applies to (e.g., `OrderCreated`, `UserUpdated`).
  Type: `string`
  Access: Public read-only property

- **`Enabled`**
  Indicates whether the subscription is active and events should be delivered.
  Type: `bool`
  Access: Public read-only property

- **`Headers`**
  A dictionary of HTTP headers to include in each delivery request.
  Type: `Dictionary<string, string>`
  Access: Public read-only property

- **`CreatedAt`**
  The timestamp when the subscription was created.
  Type: `DateTime`
  Access: Public read-only property

### `WebhookDelivery`
A model representing a single delivery attempt of a domain event to a webhook endpoint.

- **`DeliveryId`**
  The unique identifier of the delivery attempt.
  Type: `string`
  Access: Public read-only property

- **`WebhookId`**
  The identifier of the associated webhook subscription.
  Type: `string`
  Access: Public read-only property

- **`Url`**
  The destination URL for this delivery.
  Type: `string`
  Access: Public read-only property

- **`Event`**
  The domain event being delivered.
  Type: `DomainEvent`
  Access: Public read-only property

- **`Headers`**
  The HTTP headers used for this delivery attempt.
  Type: `Dictionary<string, string>`
  Access: Public read-only property

- **`RetryCount`**
  The number of times this delivery has been retried.
  Type: `int`
  Access: Public read-only property

- **`MaxRetries`**
  The maximum number of retry attempts allowed for this delivery.
  Type: `int`
  Access: Public read-only property

### `WebhookHandler : IWebhookHandler`
The concrete implementation of `IWebhookHandler` for managing webhook subscriptions and deliveries.

- **`WebhookHandler`**
  Constructs a new instance of the webhook handler.
  Parameters: None
  Throws: None

- **`DeliverAsync`**
  Delivers a domain event to all matching and enabled subscriptions.
  Parameters:
  - `event` (`DomainEvent`) – The domain event to deliver.
  Returns: `Task` – A task representing the asynchronous operation.
  Throws:
  - `ArgumentNullException` – If `event` is `null`.
  - `InvalidOperationException` – If the delivery infrastructure is misconfigured.

- **`RegisterAsync`**
  Registers a new webhook subscription.
  Parameters:
  - `subscription` (`WebhookHandlerSubscription`) – The subscription to register.
  Returns: `Task` – A task representing the asynchronous operation.
  Throws:
  - `ArgumentNullException` – If `subscription` is `null`.
  - `ArgumentException` – If required fields in `subscription` are invalid.

- **`UnregisterAsync`**
  Removes an existing webhook subscription.
  Parameters:
  - `webhookId` (`string`) – The unique identifier of the subscription to remove.
  Returns: `Task` – A task representing the asynchronous operation.
  Throws:
  - `ArgumentNullException` – If `webhookId` is `null` or empty.
  - `KeyNotFoundException` – If no subscription with the given `webhookId` exists.

## Usage

### Registering a Webhook Subscription
