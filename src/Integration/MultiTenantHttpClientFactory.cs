#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Integration
{
    /// <summary>
    /// Creates and manages HTTP clients with tenant-aware headers and configuration.
    /// </summary>
    public sealed class MultiTenantHttpClientFactory
    {
        private readonly ILogger<MultiTenantHttpClientFactory> _logger;
        private readonly ConcurrentDictionary<string, HttpClient> _clientCache;
        private readonly string _defaultUserAgent;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTenantHttpClientFactory"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="defaultUserAgent">The default user agent string.</param>
        public MultiTenantHttpClientFactory(ILogger<MultiTenantHttpClientFactory> logger,
            string defaultUserAgent = "SqliteMultiTenant/1.0")
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientCache = new ConcurrentDictionary<string, HttpClient>();
            _defaultUserAgent = defaultUserAgent;
        }

        /// <summary>
        /// Creates an HTTP client with tenant context.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="apiKey">The API key (optional).</param>
        /// <param name="timeoutSeconds">The timeout in seconds (default: 30).</param>
        /// <param name="baseAddress">The base address (optional).</param>
        /// <returns>The HTTP client instance.</returns>
        public HttpClient CreateClientForTenant(string tenantId, string apiKey = null,
            int timeoutSeconds = 30, string baseAddress = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            var cacheKey = $"{tenantId}_{apiKey}_{baseAddress}";

            return _clientCache.GetOrAdd(cacheKey, _ =>
            {
                var client = new HttpClient();

                // Set timeout
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                // Set base address
                if (!string.IsNullOrEmpty(baseAddress))
                {
                    client.BaseAddress = new Uri(baseAddress);
                }

                // Set default headers
                SetDefaultHeaders(client, tenantId, apiKey);

                _logger.LogInformation("Created HTTP client for tenant: {TenantId}", tenantId);

                return client;
            });
        }

        /// <summary>
        /// Gets or creates a cached client.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <returns>The cached HTTP client instance or null if not found.</returns>
        public HttpClient GetCachedClient(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return null;

            var cachedClient = _clientCache.Values.FirstOrDefault(c =>
                c.DefaultRequestHeaders.Contains("X-Tenant-Id"));

            return cachedClient;
        }

        /// <summary>
        /// Invalidates a cached client.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        public void InvalidateClient(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return;

            var keysToRemove = _clientCache.Keys
                .Where(k => k.StartsWith(tenantId))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _clientCache.TryRemove(key, out var client);
                client?.Dispose();
            }

            _logger.LogInformation("Invalidated HTTP client cache for tenant: {TenantId}", tenantId);
        }

        /// <summary>
        /// Clears all cached clients.
        /// </summary>
        public void ClearCache()
        {
            foreach (var client in _clientCache.Values)
            {
                client?.Dispose();
            }

            _clientCache.Clear();
            _logger.LogInformation("Cleared HTTP client cache");
        }

        /// <summary>
        /// Sets default headers for the HTTP client.
        /// </summary>
        /// <param name="client">The HTTP client instance.</param>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="apiKey">The API key (optional).</param>
        private void SetDefaultHeaders(HttpClient client, string tenantId, string apiKey = null)
        {
            client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
            client.DefaultRequestHeaders.Add("User-Agent", _defaultUserAgent);

            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);
            }

            client.DefaultRequestHeaders.Add("X-Request-Id", Guid.NewGuid().ToString());
            client.DefaultRequestHeaders.Add("X-Timestamp", DateTime.UtcNow.ToString("O"));
        }

        /// <summary>
        /// Disposes the HTTP client factory instance.
        /// </summary>
        public void Dispose()
        {
            ClearCache();
        }
    }

    /// <summary>
    /// Builder for creating HTTP clients with fluent API.
    /// </summary>
    public sealed class TenantHttpClientBuilder
    {
        private string _tenantId;
        private string _apiKey;
        private int _timeoutSeconds = 30;
        private string _baseAddress;
        private readonly Dictionary<string, string> _defaultHeaders;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantHttpClientBuilder"/> class.
        /// </summary>
        public TenantHttpClientBuilder()
        {
            _defaultHeaders = new Dictionary<string, string>();
        }

        /// <summary>
        /// Specifies the tenant ID.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <returns>The builder instance.</returns>
        public TenantHttpClientBuilder ForTenant(string tenantId)
        {
            _tenantId = tenantId;
            return this;
        }

        /// <summary>
        /// Specifies the API key.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <returns>The builder instance.</returns>
        public TenantHttpClientBuilder WithApiKey(string apiKey)
        {
            _apiKey = apiKey;
            return this;
        }

        /// <summary>
        /// Specifies the timeout in seconds.
        /// </summary>
        /// <param name="seconds">The timeout in seconds.</param>
        /// <returns>The builder instance.</returns>
        public TenantHttpClientBuilder WithTimeout(int seconds)
        {
            _timeoutSeconds = seconds;
            return this;
        }

        /// <summary>
        /// Specifies the base address.
        /// </summary>
        /// <param name="baseAddress">The base address.</param>
        /// <returns>The builder instance.</returns>
        public TenantHttpClientBuilder WithBaseAddress(string baseAddress)
        {
            _baseAddress = baseAddress;
            return this;
        }

        /// <summary>
        /// Adds a custom header.
        /// </summary>
        /// <param name="name">The header name.</param>
        /// <param name="value">The header value.</param>
        /// <returns>The builder instance.</returns>
        public TenantHttpClientBuilder AddHeader(string name, string value)
        {
            _defaultHeaders[name] = value;
            return this;
        }

        /// <summary>
        /// Builds the HTTP client instance.
        /// </summary>
        /// <returns>The HTTP client instance.</returns>
        public HttpClient Build()
        {
            if (string.IsNullOrWhiteSpace(_tenantId))
                throw new InvalidOperationException("Tenant ID is required");

            var client = new HttpClient();

            client.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

            if (!string.IsNullOrEmpty(_baseAddress))
            {
                client.BaseAddress = new Uri(_baseAddress);
            }

            client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId);

            if (!string.IsNullOrEmpty(_apiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _apiKey);
            }

            foreach (var header in _defaultHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            return client;
        }
    }
}
