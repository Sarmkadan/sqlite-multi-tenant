#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SqliteMultiTenant.Integration;

/// <summary>
/// Wrapper around HttpClient for safe and resilient HTTP operations.
/// Implements retry logic, timeout handling, and structured logging.
/// Useful for integration with external services and webhooks.
/// </summary>
public interface IHttpClientService
{
    Task<T> GetAsync<T>(string url, Dictionary<string, string> headers = null);
    Task<T> PostAsync<T>(string url, object body, Dictionary<string, string> headers = null);
    Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string content = null);
}

/// <summary>
/// HTTP client service implementation with retry and timeout policies.
/// </summary>
public sealed class HttpClientService : IHttpClientService {
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpClientService> _logger;
    private readonly HttpClientOptions _options;

    public HttpClientService(HttpClient httpClient, ILogger<HttpClientService> logger, HttpClientOptions options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new HttpClientOptions();

        // Configure default timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    /// <summary>
    /// Performs GET request with retry logic and JSON deserialization.
    /// </summary>
    public async Task<T> GetAsync<T>(string url, Dictionary<string, string> headers = null)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyHeaders(request, headers);

            var response = await SendWithRetryAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET request failed: {url}", url);
            throw;
        }
    }

    /// <summary>
    /// Performs POST request with JSON body and response deserialization.
    /// </summary>
    public async Task<T> PostAsync<T>(string url, object body, Dictionary<string, string> headers = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            ApplyHeaders(request, headers);

            var response = await SendWithRetryAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST request failed: {url}", url);
            throw;
        }
    }

    /// <summary>
    /// Sends raw HTTP request with custom method and body.
    /// </summary>
    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string content = null)
    {
        try
        {
            var request = new HttpRequestMessage(method, url);

            if (!string.IsNullOrEmpty(content))
                request.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

            return await SendWithRetryAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{method} request failed: {url}", method.Method, url);
            throw;
        }
    }

    /// <summary>
    /// Sends request with automatic retry on transient failures.
    /// Implements exponential backoff: 1s, 2s, 4s.
    /// </summary>
    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request)
    {
        int retryCount = 0;

        while (retryCount <= _options.MaxRetries)
        {
            try
            {
                _logger.LogDebug("Sending {method} request to {url}", request.Method, request.RequestUri);

                var response = await _httpClient.SendAsync(request);

                if (IsTransientError(response.StatusCode))
                {
                    retryCount++;
                    if (retryCount <= _options.MaxRetries)
                    {
                        var delayMs = (int)Math.Pow(2, retryCount - 1) * 1000;
                        _logger.LogWarning(
                            "Transient error {status}, retrying in {ms}ms [Attempt {retry}/{max}]",
                            response.StatusCode,
                            delayMs,
                            retryCount,
                            _options.MaxRetries);

                        await Task.Delay(delayMs);
                        continue;
                    }
                }

                return response;
            }
            catch (TaskCanceledException ex)
            {
                retryCount++;
                if (retryCount <= _options.MaxRetries)
                {
                    _logger.LogWarning(ex, "Request timeout, retrying [Attempt {retry}/{max}]", retryCount, _options.MaxRetries);
                    await Task.Delay(1000 * retryCount);
                }
                else
                {
                    throw;
                }
            }
        }

        throw new HttpRequestException("Max retry attempts exceeded");
    }

    /// <summary>
    /// Applies custom headers to request.
    /// </summary>
    private void ApplyHeaders(HttpRequestMessage request, Dictionary<string, string> headers)
    {
        if (headers is null)
            return;

        foreach (var header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }
    }

    /// <summary>
    /// Checks if HTTP status code represents a transient error (retryable).
    /// </summary>
    private bool IsTransientError(System.Net.HttpStatusCode statusCode)
    {
        return statusCode == System.Net.HttpStatusCode.RequestTimeout ||
               statusCode == System.Net.HttpStatusCode.TooManyRequests ||
               (int)statusCode >= 500;
    }
}

/// <summary>
/// Configuration options for HTTP client behavior.
/// </summary>
public sealed class HttpClientOptions {
    /// <summary>
    /// Request timeout in seconds (default: 30s).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retry attempts for transient failures (default: 3).
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Enable automatic compression for requests/responses.
    /// Default: true (reduces bandwidth for large payloads).
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Enable connection pooling for performance.
    /// Default: true (reuses connections across requests).
    /// </summary>
    public bool EnableConnectionPooling { get; set; } = true;
}
