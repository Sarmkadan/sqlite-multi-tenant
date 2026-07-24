#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Integration
{
    /// <summary>
    /// Utility methods for validating HTTP client base addresses to prevent SSRF attacks.
    /// </summary>
    internal static class HttpClientAddressValidator
    {
        /// <summary>
        /// Validates a base address to prevent SSRF attacks.
        /// </summary>
        /// <param name="baseAddress">The base address to validate.</param>
        /// <param name="paramName">The name of the parameter being validated.</param>
        /// <param name="allowedHosts">An optional list of allowed hosts.</param>
        /// <param name="allowedHostRegex">An optional regex for allowed hosts.</param>
        /// <exception cref="ArgumentException">Thrown when the base address is invalid.</exception>
        internal static void ValidateBaseAddress(string baseAddress, string paramName,
            IEnumerable<string>? allowedHosts = null, Regex? allowedHostRegex = null)
        {
            if (string.IsNullOrEmpty(baseAddress))
            {
                return;
            }

            if (!Uri.TryCreate(baseAddress, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException("Base address must be a valid absolute URI.", paramName);
            }

            // Reject non-http(s) schemes
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Base address scheme must be '{Uri.UriSchemeHttp}' or '{Uri.UriSchemeHttps}'. Actual: '{uri.Scheme}'.",
                    paramName);
            }

            // Allowlist/Regex check
            bool isAllowed = false;
            if (allowedHosts != null && allowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            {
                isAllowed = true;
            }
            else if (allowedHostRegex != null && allowedHostRegex.IsMatch(uri.Host))
            {
                isAllowed = true;
            }

            if (isAllowed)
            {
                return;
            }

            // Reject loopback addresses (127.0.0.0/8, ::1)
            if (uri.IsLoopback)
            {
                throw new ArgumentException(
                    "Base address cannot be a loopback address (127.0.0.0/8, ::1).",
                    paramName);
            }

            // Reject link-local addresses (169.254.0.0/16, fe80::/10)
            if (IsLinkLocalAddress(uri.Host))
            {
                throw new ArgumentException(
                    "Base address cannot be a link-local address (169.254.0.0/16, fe80::/10).",
                    paramName);
            }

            // Reject private IP ranges
            if (IsPrivateIpAddress(uri.Host))
            {
                throw new ArgumentException(
                    "Base address cannot be a private IP address range (RFC 1918, RFC 4193, RFC 6598).",
                    paramName);
            }

            // Reject unspecified address (0.0.0.0)
            if (uri.Host.Equals("0.0.0.0", StringComparison.Ordinal) ||
                uri.Host.Equals("::", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Base address cannot be the unspecified address (0.0.0.0, ::).",
                    paramName);
            }
        }

        /// <summary>
        /// Checks if a hostname or IP address is a link-local address.
        /// </summary>
        private static bool IsLinkLocalAddress(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                return false;
            }

            // Try to parse as IP address first
            if (IPAddress.TryParse(host, out var ipAddress))
            {
                // IPv4 link-local: 169.254.0.0/16
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    var bytes = ipAddress.GetAddressBytes();
                    return bytes[0] == 169 && bytes[1] == 254;
                }

                // IPv6 link-local: fe80::/10
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    var bytes = ipAddress.GetAddressBytes();
                    return bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a hostname or IP address is in a private IP range.
        /// </summary>
        private static bool IsPrivateIpAddress(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                return false;
            }

            // Try to parse as IP address first
            if (IPAddress.TryParse(host, out var ipAddress))
            {
                // IPv4 private ranges (RFC 1918):
                // 10.0.0.0/8
                // 172.16.0.0/12
                // 192.168.0.0/16
                // IPv4 carrier-grade NAT (RFC 6598): 100.64.0.0/10
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    var bytes = ipAddress.GetAddressBytes();

                    // 10.0.0.0/8
                    if (bytes[0] == 10)
                    {
                        return true;
                    }

                    // 172.16.0.0/12
                    if (bytes[0] == 172 && (bytes[1] >= 16 && bytes[1] <= 31))
                    {
                        return true;
                    }

                    // 192.168.0.0/16
                    if (bytes[0] == 192 && bytes[1] == 168)
                    {
                        return true;
                    }

                    // 100.64.0.0/10
                    if (bytes[0] == 100 && (bytes[1] >= 64 && bytes[1] <= 127))
                    {
                        return true;
                    }
                }

                // IPv6 unique local addresses (RFC 4193): fc00::/7
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    var bytes = ipAddress.GetAddressBytes();
                    return (bytes[0] & 0xfe) == 0xfc;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Creates and manages HTTP clients with tenant-aware headers and configuration.
    /// </summary>
    public sealed class MultiTenantHttpClientFactory
    {
        private readonly ILogger<MultiTenantHttpClientFactory> _logger;
        private readonly ConcurrentDictionary<string, HttpClient> _clientCache;
        private readonly string _defaultUserAgent;
        private readonly IEnumerable<string>? _allowedHosts;
        private readonly Regex? _allowedHostRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTenantHttpClientFactory"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="defaultUserAgent">The default user agent string.</param>
        /// <param name="allowedHosts">An optional list of allowed hosts.</param>
        /// <param name="allowedHostRegex">An optional regex for allowed hosts.</param>
        public MultiTenantHttpClientFactory(ILogger<MultiTenantHttpClientFactory> logger,
            string defaultUserAgent = "SqliteMultiTenant/1.0",
            IEnumerable<string>? allowedHosts = null, Regex? allowedHostRegex = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientCache = new ConcurrentDictionary<string, HttpClient>();
            _defaultUserAgent = defaultUserAgent;
            _allowedHosts = allowedHosts;
            _allowedHostRegex = allowedHostRegex;
        }

        /// <summary>
        /// Creates an HTTP client with tenant context.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="apiKey">The API key (optional).</param>
        /// <param name="timeoutSeconds">The timeout in seconds (default: 30).</param>
        /// <param name="baseAddress">The base address (optional).</param>
        /// <returns>The HTTP client instance.</returns>
        /// <exception cref="ArgumentException">Thrown when baseAddress is invalid.</exception>
        public HttpClient CreateClientForTenant(string tenantId, string apiKey = null,
            int timeoutSeconds = 30, string baseAddress = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            // Validate base address for SSRF protection
            HttpClientAddressValidator.ValidateBaseAddress(baseAddress, nameof(baseAddress), _allowedHosts, _allowedHostRegex);

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
        private IEnumerable<string>? _allowedHosts;
        private Regex? _allowedHostRegex;

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
        /// Specifies the allowed hosts.
        /// </summary>
        /// <param name="allowedHosts">The allowed hosts.</param>
        /// <returns>The builder instance.</returns>
        public TenantHttpClientBuilder WithAllowedHosts(IEnumerable<string> allowedHosts)
        {
            _allowedHosts = allowedHosts;
            return this;
        }

        /// <summary>
        /// Specifies the allowed host regex.
        /// </summary>
        /// <param name="allowedHostRegex">The allowed host regex.</param>
        /// <returns>The builder instance.</returns>
        public TenantHttpClientBuilder WithAllowedHostRegex(Regex allowedHostRegex)
        {
            _allowedHostRegex = allowedHostRegex;
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
        /// <exception cref="ArgumentException">Thrown when baseAddress is invalid.</exception>
        public HttpClient Build()
        {
            if (string.IsNullOrWhiteSpace(_tenantId))
                throw new InvalidOperationException("Tenant ID is required");

            // Validate base address for SSRF protection
            HttpClientAddressValidator.ValidateBaseAddress(_baseAddress, nameof(_baseAddress), _allowedHosts, _allowedHostRegex);

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
