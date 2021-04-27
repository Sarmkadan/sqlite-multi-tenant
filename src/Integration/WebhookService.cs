// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.Json;

namespace SqliteMultiTenant.Integration;

/// <summary>
/// Manages webhook delivery for asynchronous event notification.
/// Supports event filtering, retry logic, and delivery status tracking.
/// Webhooks are delivered to registered endpoints when events occur.
/// </summary>
public class WebhookService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookService> _logger;
    private readonly Dictionary<string, List<WebhookSubscription>> _subscriptions;
    private readonly SemaphoreSlim _semaphore;

    public WebhookService(ILogger<WebhookService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _subscriptions = new Dictionary<string, List<WebhookSubscription>>();
        _semaphore = new SemaphoreSlim(1);
    }

    /// <summary>
    /// Registers a webhook subscription for specific event types.
    /// Returns subscription ID for future reference or cancellation.
    /// </summary>
    public async Task<string> SubscribeAsync(
        string eventType,
        string webhookUrl,
        Dictionary<string, string>? headers = null,
        string? secret = null)
    {
        try
        {
            await _semaphore.WaitAsync();

            var subscription = new WebhookSubscription
            {
                Id = Guid.NewGuid().ToString(),
                EventType = eventType,
                WebhookUrl = webhookUrl,
                Headers = headers ?? new Dictionary<string, string>(),
                Secret = secret,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                FailureCount = 0
            };

            if (!_subscriptions.ContainsKey(eventType))
                _subscriptions[eventType] = new List<WebhookSubscription>();

            _subscriptions[eventType].Add(subscription);

            _logger.LogInformation($"Webhook subscribed: {eventType} -> {webhookUrl}");
            return subscription.Id;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Unsubscribes a webhook by its subscription ID.
    /// </summary>
    public async Task<bool> UnsubscribeAsync(string subscriptionId)
    {
        try
        {
            await _semaphore.WaitAsync();

            foreach (var subscriptionList in _subscriptions.Values)
            {
                var subscription = subscriptionList.FirstOrDefault(s => s.Id == subscriptionId);
                if (subscription != null)
                {
                    subscriptionList.Remove(subscription);
                    _logger.LogInformation($"Webhook unsubscribed: {subscriptionId}");
                    return true;
                }
            }

            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Triggers webhook delivery for an event to all registered subscribers.
    /// Executes asynchronously with retry logic for failed deliveries.
    /// </summary>
    public async Task TriggerWebhooksAsync(string eventType, object eventData)
    {
        try
        {
            if (!_subscriptions.TryGetValue(eventType, out var subscriptions))
                return;

            var tasks = subscriptions
                .Where(s => s.IsActive)
                .Select(s => DeliverWebhookAsync(s, eventData));

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Webhook trigger error: {ex.Message}");
        }
    }

    private async Task DeliverWebhookAsync(WebhookSubscription subscription, object eventData)
    {
        int maxAttempts = 3;
        int attempt = 0;
        TimeSpan delayBetweenRetries = TimeSpan.FromSeconds(5);

        while (attempt < maxAttempts)
        {
            try
            {
                attempt++;

                var payload = new
                {
                    EventType = subscription.EventType,
                    Timestamp = DateTime.UtcNow,
                    Data = eventData,
                    Attempt = attempt
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Add headers
                foreach (var header in subscription.Headers)
                {
                    content.Headers.Add(header.Key, header.Value);
                }

                // Add signature if secret is configured
                if (!string.IsNullOrEmpty(subscription.Secret))
                {
                    var signature = GenerateHmacSignature(json, subscription.Secret);
                    content.Headers.Add("X-Webhook-Signature", signature);
                }

                var response = await _httpClient.PostAsync(subscription.WebhookUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        $"Webhook delivered: {subscription.EventType} to {subscription.WebhookUrl}");
                    subscription.LastDeliveryAt = DateTime.UtcNow;
                    subscription.FailureCount = 0;
                    return;
                }

                _logger.LogWarning(
                    $"Webhook delivery failed: {subscription.EventType}, Status: {response.StatusCode}, Attempt: {attempt}");

                if (attempt < maxAttempts)
                    await Task.Delay(delayBetweenRetries);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Webhook delivery exception: {subscription.EventType}, Error: {ex.Message}, Attempt: {attempt}");

                if (attempt < maxAttempts)
                    await Task.Delay(delayBetweenRetries);
            }
        }

        subscription.FailureCount++;
        _logger.LogError(
            $"Webhook delivery failed after {maxAttempts} attempts: {subscription.EventType}");

        // Disable webhook after too many failures
        if (subscription.FailureCount > 10)
        {
            subscription.IsActive = false;
            _logger.LogWarning($"Webhook disabled due to excessive failures: {subscription.Id}");
        }
    }

    private string GenerateHmacSignature(string payload, string secret)
    {
        using (var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash);
        }
    }

    /// <summary>
    /// Gets all active subscriptions for an event type.
    /// </summary>
    public async Task<List<WebhookSubscription>> GetSubscriptionsAsync(string eventType)
    {
        try
        {
            await _semaphore.WaitAsync();

            if (_subscriptions.TryGetValue(eventType, out var subscriptions))
                return subscriptions.Where(s => s.IsActive).ToList();

            return new List<WebhookSubscription>();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

public class WebhookSubscription
{
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Secret { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastDeliveryAt { get; set; }
    public bool IsActive { get; set; }
    public int FailureCount { get; set; }
}
