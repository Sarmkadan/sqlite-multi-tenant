#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Events;

namespace SqliteMultiTenant.Integration;

/// <summary>
/// Webhook handler for delivering domain events to external systems.
/// Supports HTTP callbacks for event-driven integrations.
/// Implements retry logic with exponential backoff for reliability.
/// </summary>
public interface IWebhookHandler
{
    /// <summary>
    /// Delivers a webhook request, retrying on failure.
    /// </summary>
    /// <param name="delivery">The delivery request to send.</param>
    /// <param name="cancellationToken">A token to cancel the delivery.</param>
    Task DeliverAsync(WebhookDelivery delivery, CancellationToken cancellationToken);

    /// <summary>
    /// Registers a webhook subscription.
    /// </summary>
    /// <param name="subscription">The subscription to register.</param>
    Task RegisterAsync(WebhookHandlerSubscription subscription);

    /// <summary>
    /// Unregisters a webhook subscription by its identifier.
    /// </summary>
    /// <param name="webhookId">The identifier of the subscription to remove.</param>
    Task UnregisterAsync(string webhookId);
}

/// <summary>
/// Webhook subscription configuration.
/// </summary>
public sealed class WebhookHandlerSubscription
{
    /// <summary>
    /// Gets or sets the unique identifier of the subscription.
    /// </summary>
    public string WebhookId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the callback URL that receives the webhook requests.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event type to subscribe to, or <c>*</c> for all events.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the subscription is active.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the custom HTTP headers sent with each request.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the UTC time when the subscription was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Returns a summary of the subscription. The URL and header values are omitted
    /// because they may contain credentials or signing keys.
    /// </summary>
    /// <returns>A string describing the subscription without secrets.</returns>
    public override string ToString() =>
        $"WebhookHandlerSubscription {{ WebhookId = {WebhookId}, EventType = {EventType}, Enabled = {Enabled}, Headers = {Headers.Count}, CreatedAt = {CreatedAt:O} }}";
}

/// <summary>
/// Webhook delivery request.
/// </summary>
public sealed class WebhookDelivery
{
    /// <summary>
    /// Gets or sets the unique identifier of this delivery attempt sequence.
    /// </summary>
    public string DeliveryId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the identifier of the subscription this delivery belongs to.
    /// </summary>
    public string WebhookId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the callback URL the payload is posted to.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the domain event being delivered.
    /// </summary>
    public DomainEvent? Event { get; set; }

    /// <summary>
    /// Gets or sets the custom HTTP headers sent with the request, including any signature header.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of retries already performed.
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum number of retries allowed.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Returns a summary of the delivery. The URL, header values and event payload are omitted
    /// because they may contain credentials, signatures or sensitive data.
    /// </summary>
    /// <returns>A string describing the delivery without secrets or payload.</returns>
    public override string ToString() =>
        $"WebhookDelivery {{ DeliveryId = {DeliveryId}, WebhookId = {WebhookId}, EventId = {Event?.EventId ?? "null"}, EventType = {Event?.EventType ?? "null"}, Headers = {Headers.Count}, RetryCount = {RetryCount}, MaxRetries = {MaxRetries} }}";

    /// <summary>
    /// Verifies the HMAC-SHA256 signature of the webhook payload.
    /// </summary>
    /// <param name="secret">The secret key used to sign the payload.</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    public bool VerifySignature(string secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            throw new ArgumentException("Secret cannot be null or empty.", nameof(secret));
        }

        if (!Headers.TryGetValue("X-Signature", out var signature) || string.IsNullOrEmpty(signature))
        {
            return false;
        }

        var payload = JsonSerializer.Serialize(Event);
        return WebhookService.VerifySignature(payload, signature, secret);
    }
}

/// <summary>
/// Webhook handler implementation with HTTP delivery and retry logic.
/// </summary>
public sealed class WebhookHandler : IWebhookHandler
{
    private static readonly TimeSpan DefaultWebhookTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(2);

    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookHandler> _logger;
    private readonly Dictionary<string, WebhookHandlerSubscription> _subscriptions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookHandler"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to deliver webhooks.</param>
    /// <param name="logger">The logger for delivery diagnostics.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
    public WebhookHandler(HttpClient httpClient, ILogger<WebhookHandler> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Delivers webhook with automatic retry on failure.
    /// Uses exponential backoff: 2s, 4s, 8s between retries.
    /// </summary>
    public async Task DeliverAsync(WebhookDelivery delivery, CancellationToken cancellationToken)
    {
        if (delivery is null)
        {
            throw new ArgumentNullException(nameof(delivery));
        }

        while (delivery.RetryCount <= delivery.MaxRetries)
        {
            try
            {
                _logger.LogInformation(
                    "Delivering webhook [DeliveryId: {deliveryId}, Url: {url}, Retry: {retry}/{max}]",
                    delivery.DeliveryId,
                    delivery.Url,
                    delivery.RetryCount,
                    delivery.MaxRetries);

                var payload = JsonSerializer.Serialize(delivery.Event);
                var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

                // Add custom headers
                foreach (var header in delivery.Headers)
                {
                    content.Headers.Add(header.Key, header.Value);
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(DefaultWebhookTimeout);

                var response = await _httpClient.PostAsync(delivery.Url, content, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Webhook delivered successfully [DeliveryId: {deliveryId}]",
                        delivery.DeliveryId);
                    return; // Success
                }

                _logger.LogWarning(
                    "Webhook delivery failed with status {status} [DeliveryId: {deliveryId}]",
                    response.StatusCode,
                    delivery.DeliveryId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP error delivering webhook [DeliveryId: {deliveryId}]", delivery.DeliveryId);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Webhook delivery timeout [DeliveryId: {deliveryId}]", delivery.DeliveryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delivering webhook [DeliveryId: {deliveryId}]", delivery.DeliveryId);
            }

            delivery.RetryCount++;

            if (delivery.RetryCount <= delivery.MaxRetries)
            {
                var delayMs = (int)(DefaultRetryDelay.TotalMilliseconds * Math.Pow(2, delivery.RetryCount - 1));
                _logger.LogInformation(
                    "Retrying webhook delivery in {ms}ms [DeliveryId: {deliveryId}]",
                    delayMs,
                    delivery.DeliveryId);

                await Task.Delay(delayMs, cancellationToken);
            }
        }

        _logger.LogError("Webhook delivery failed after {retries} retries [DeliveryId: {deliveryId}]",
            delivery.MaxRetries,
            delivery.DeliveryId);
    }

    /// <summary>
    /// Registers a webhook subscription.
    /// </summary>
    public Task RegisterAsync(WebhookHandlerSubscription subscription)
    {
        if (subscription is null)
        {
            throw new ArgumentNullException(nameof(subscription));
        }

        _subscriptions[subscription.WebhookId] = subscription;

        _logger.LogInformation(
            "Webhook registered [WebhookId: {webhookId}, EventType: {eventType}, Url: {url}]",
            subscription.WebhookId,
            subscription.EventType,
            subscription.Url);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Unregisters a webhook subscription.
    /// </summary>
    public Task UnregisterAsync(string webhookId)
    {
        if (string.IsNullOrWhiteSpace(webhookId))
        {
            return Task.CompletedTask;
        }

        _subscriptions.Remove(webhookId);

        _logger.LogInformation("Webhook unregistered [WebhookId: {webhookId}]", webhookId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets subscriptions for an event type.
    /// </summary>
    public List<WebhookHandlerSubscription> GetSubscriptions(string eventType)
    {
        return _subscriptions.Values
            .Where(s => s.Enabled && (s.EventType == "*" || s.EventType == eventType))
            .ToList();
    }
}

/// <summary>
/// Event handler that delivers webhook notifications for all domain events.
/// </summary>
public sealed class WebhookEventHandler<T> : IEventHandler<T> where T : DomainEvent
{
    private readonly IWebhookHandler _webhookHandler;
    private readonly ILogger<WebhookEventHandler<T>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookEventHandler{T}"/> class.
    /// </summary>
    /// <param name="webhookHandler">The webhook handler used to deliver notifications.</param>
    /// <param name="logger">The logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException"><paramref name="webhookHandler"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
    public WebhookEventHandler(IWebhookHandler webhookHandler, ILogger<WebhookEventHandler<T>> logger)
    {
        _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Delivers webhooks asynchronously without blocking event processing.
    /// </summary>
    public async Task HandleAsync(T @event, CancellationToken cancellationToken)
    {
        // This would be enhanced with actual webhook delivery
        _logger.LogDebug("Would deliver webhooks for event: {eventType}", @event.EventType);

        await Task.CompletedTask;
    }
}
