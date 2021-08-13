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
public sealed class HttpClientWrapper {
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
        _httpClient = httpClient;
        _logger = logger;
        _maxRetries = maxRetries;
        _retryDelay = TimeSpan.FromMilliseconds(retryDelayMs);
    }

    /// <summary>
    /// Sends a GET request and deserializes response to specified type.
    /// Includes retry logic for transient failures.
    /// </summary>
    public async Task<T?> GetAsync<T>(string url) where T : class
    {
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
            _logger.LogError("GET error: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Sends a POST request with JSON payload and deserializes response.
    /// </summary>
    public async Task<T?> PostAsync<T>(string url, object payload) where T : class
    {
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
            _logger.LogError("POST error: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Sends a PUT request with JSON payload.
    /// </summary>
    public async Task<bool> PutAsync(string url, object payload)
    {
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
            _logger.LogError("PUT error: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Sends a DELETE request.
    /// </summary>
    public async Task<bool> DeleteAsync(string url)
    {
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
            _logger.LogError("DELETE error: {Message}", ex.Message);
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
    public void AddDefaultHeader(string name, string value)
    {
        _httpClient.DefaultRequestHeaders.Add(name, value);
    }

    /// <summary>
    /// Sets the authorization header with bearer token.
    /// </summary>
    public void SetBearerToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}
