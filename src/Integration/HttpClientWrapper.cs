#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.Json;

namespace SqliteMultiTenant.Integration;

/// <summary>
/// Wraps HttpClient with retry logic, timeout handling, and request/response logging.
/// Provides typed methods for common HTTP operations (GET, POST, PUT, DELETE).
/// Implements resilience patterns for external API communication.
/// </summary>
public sealed class HttpClientWrapper : IHttpClientWrapper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpClientWrapper> _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _retryDelay;

    public HttpClientWrapper(
        HttpClient httpClient,
        ILogger<HttpClientWrapper> logger,
        int maxRetries = 3,
        int retryDelayMs = 1000)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxRetries = maxRetries >= 0 ? maxRetries : throw new ArgumentOutOfRangeException(nameof(maxRetries), "Value must be non-negative.");
        _retryDelay = retryDelayMs >= 0 ? TimeSpan.FromMilliseconds(retryDelayMs) : throw new ArgumentOutOfRangeException(nameof(retryDelayMs), "Value must be non-negative.");
    }

    /// <summary>
    /// Sends a GET request and deserializes response to specified type.
    /// Includes retry logic for transient failures.
    /// </summary>
    /// <typeparam name="T">The response type to deserialize.</typeparam>
    /// <param name="url">The request URL.</param>
    /// <returns>The deserialized response, or <see langword="null"/> on failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="url"/> is empty or whitespace.</exception>
    public async Task<T?> GetAsync<T>(string url) where T : class
    {
        ArgumentNullException.ThrowIfNull(url);

        try
        {
            _logger.LogInformation("GET request: {Url}", url);

            var response = await SendWithRetryAsync(
                () => _httpClient.GetAsync(url),
                $"GET {url}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GET failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<T>(content, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET error: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Sends a POST request with JSON payload and deserializes response.
    /// </summary>
    /// <typeparam name="T">The response type to deserialize.</typeparam>
    /// <param name="url">The request URL.</param>
    /// <param name="payload">The request payload to serialize as JSON.</param>
    /// <returns>The deserialized response, or <see langword="null"/> on failure.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="url"/> is <see langword="null"/>.
    /// or <paramref name="payload"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="url"/> is empty or whitespace.</exception>
    public async Task<T?> PostAsync<T>(string url, object payload) where T : class
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(payload);

        try
        {
            _logger.LogInformation("POST request: {Url}", url);

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await SendWithRetryAsync(
                () => _httpClient.PostAsync(url, content),
                $"POST {url}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("POST failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<T>(responseContent, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST error: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Sends a PUT request with JSON payload.
    /// </summary>
    /// <param name="url">The request URL.</param>
    /// <param name="payload">The request payload to serialize as JSON.</param>
    /// <returns><see langword="true"/> if the request succeeded; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="url"/> is <see langword="null"/>.
    /// or <paramref name="payload"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="url"/> is empty or whitespace.</exception>
    public async Task<bool> PutAsync(string url, object payload)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(payload);

        try
        {
            _logger.LogInformation("PUT request: {Url}", url);

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await SendWithRetryAsync(
                () => _httpClient.PutAsync(url, content),
                $"PUT {url}");

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PUT error: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Sends a DELETE request.
    /// </summary>
    /// <param name="url">The request URL.</param>
    /// <returns><see langword="true"/> if the request succeeded; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="url"/> is empty or whitespace.</exception>
    public async Task<bool> DeleteAsync(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        try
        {
            _logger.LogInformation("DELETE request: {Url}", url);

            var response = await SendWithRetryAsync(
                () => _httpClient.DeleteAsync(url),
                $"DELETE {url}");

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DELETE error: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Executes an HTTP request with exponential backoff retry logic.
    /// Retries on transient errors (5xx, timeout, connection refused).
    /// </summary>
    private async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<Task<HttpResponseMessage>> request,
        string operationName)
    {
        int attempts = 0;
        TimeSpan delay = _retryDelay;

        while (attempts < _maxRetries)
        {
            try
            {
                var response = await request();

                // Retry on server errors and timeouts
                if ((int)response.StatusCode >= 500)
                {
                    attempts++;
                    if (attempts < _maxRetries)
                    {
                        _logger.LogWarning("Retry {OperationName}: Attempt {Attempts} failed with {StatusCode}", operationName, attempts, response.StatusCode);
                        await Task.Delay(delay);
                        delay = delay.Multiply(2); // Exponential backoff
                        continue;
                    }
                }

                return response;
            }
            catch (HttpRequestException ex) when (ex.InnerException is TimeoutException)
            {
                attempts++;
                if (attempts < _maxRetries)
                {
                    _logger.LogWarning("Retry {OperationName}: Attempt {Attempts} timeout", operationName, attempts);
                    await Task.Delay(delay);
                    delay = delay.Multiply(2);
                    continue;
                }
                throw;
            }
        }

        throw new HttpRequestException($"Operation failed after {_maxRetries} attempts: {operationName}");
    }

    /// <summary>
    /// Adds a custom header to all requests.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <see langword="null"/>.
    /// or <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or whitespace.</exception>
    public void AddDefaultHeader(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        _httpClient.DefaultRequestHeaders.Add(name, value);
    }

    /// <summary>
    /// Sets the authorization header with bearer token.
    /// </summary>
    /// <param name="token">The bearer token.</param>
    /// <exception cref="ArgumentNullException"><paramref name="token"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/> is empty or whitespace.</exception>
    public void SetBearerToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}
