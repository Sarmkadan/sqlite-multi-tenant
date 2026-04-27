#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Events;
using System.Text.Json;

namespace SqliteMultiTenant.Integration;

/// <summary>
/// Webhook handler for delivering domain events to external systems.
/// Supports HTTP callbacks for event-driven integrations.
/// Implements retry logic with exponential backoff for reliability.
/// </summary>
public interface IWebhookHandler
{
    Task DeliverAsync(WebhookDelivery delivery, CancellationToken cancellationToken);
    Task RegisterAsync(WebhookSubscription subscription);
    Task UnregisterAsync(string webhookId);
}

/// <summary>
/// Webhook subscription configuration.
/// </summary>
public sealed class WebhookSubscription {
    public string WebhookId { get; set; } = Guid.NewGuid().ToString();
    public string Url { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public Dictionary<string, string> Headers { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Webhook delivery request.
/// </summary>
public sealed class WebhookDelivery {
    public string DeliveryId { get; set; } = Guid.NewGuid().ToString();
    public string WebhookId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DomainEvent Event { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Webhook handler implementation with HTTP delivery and retry logic.
/// </summary>
public sealed class WebhookHandler : IWebhookHandler {
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookHandler> _logger;
    private readonly Dictionary<string, WebhookSubscription> _subscriptions = new();

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
            throw new ArgumentNullException(nameof(delivery));

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
                cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30-second timeout

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
                // Exponential backoff: 2s * 2^(retry-1)
                var delayMs = (int)(2000 * Math.Pow(2, delivery.RetryCount - 1));
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
    public Task RegisterAsync(WebhookSubscription subscription)
    {
        if (subscription is null)
            throw new ArgumentNullException(nameof(subscription));

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
            return Task.CompletedTask;

        _subscriptions.Remove(webhookId);

        _logger.LogInformation("Webhook unregistered [WebhookId: {webhookId}]", webhookId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets subscriptions for an event type.
    /// </summary>
    public List<WebhookSubscription> GetSubscriptions(string eventType)
    {
        return _subscriptions.Values
            .Where(s => s.Enabled && (s.EventType == "*" || s.EventType == eventType))
            .ToList();
    }
}

/// <summary>
/// Event handler that delivers webhook notifications for all domain events.
/// </summary>
public sealed class WebhookEventHandler<T> : IEventHandler<T> where T : DomainEvent {
    private readonly IWebhookHandler _webhookHandler;
    private readonly ILogger<WebhookEventHandler<T>> _logger;

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
