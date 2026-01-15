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
    // Creates and manages HTTP clients with tenant-aware headers and configuration
    public sealed class MultiTenantHttpClientFactory {
        private readonly ILogger<MultiTenantHttpClientFactory> _logger;
        private readonly ConcurrentDictionary<string, HttpClient> _clientCache;
        private readonly string _defaultUserAgent;

        public MultiTenantHttpClientFactory(ILogger<MultiTenantHttpClientFactory> logger,
            string defaultUserAgent = "SqliteMultiTenant/1.0")
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientCache = new ConcurrentDictionary<string, HttpClient>();
            _defaultUserAgent = defaultUserAgent;
        }

        // Creates an HTTP client with tenant context
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

        // Gets or creates a cached client
        public HttpClient GetCachedClient(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return null;

            var cachedClient = _clientCache.Values.FirstOrDefault(c =>
                c.DefaultRequestHeaders.Contains("X-Tenant-Id"));

            return cachedClient;
        }

        // Invalidates a cached client
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

        // Clears all cached clients
        public void ClearCache()
        {
            foreach (var client in _clientCache.Values)
            {
                client?.Dispose();
            }

            _clientCache.Clear();
            _logger.LogInformation("Cleared HTTP client cache");
        }

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

        public void Dispose()
        {
            ClearCache();
        }
    }

    // Builder for creating HTTP clients with fluent API
    public sealed class TenantHttpClientBuilder {
        private string _tenantId;
        private string _apiKey;
        private int _timeoutSeconds = 30;
        private string _baseAddress;
        private readonly Dictionary<string, string> _defaultHeaders;

        public TenantHttpClientBuilder()
        {
            _defaultHeaders = new Dictionary<string, string>();
        }

        public TenantHttpClientBuilder ForTenant(string tenantId)
        {
            _tenantId = tenantId;
            return this;
        }

        public TenantHttpClientBuilder WithApiKey(string apiKey)
        {
            _apiKey = apiKey;
            return this;
        }

        public TenantHttpClientBuilder WithTimeout(int seconds)
        {
            _timeoutSeconds = seconds;
            return this;
        }

        public TenantHttpClientBuilder WithBaseAddress(string baseAddress)
        {
            _baseAddress = baseAddress;
            return this;
        }

        public TenantHttpClientBuilder AddHeader(string name, string value)
        {
            _defaultHeaders[name] = value;
            return this;
        }

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
