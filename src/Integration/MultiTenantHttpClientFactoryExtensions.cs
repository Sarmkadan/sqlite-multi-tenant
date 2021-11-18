using System;
using System.Net.Http;

namespace SqliteMultiTenant.Integration
{
    /// <summary>
    /// Extension methods that make working with <see cref="MultiTenantHttpClientFactory"/>
    /// more convenient in typical multi‑tenant scenarios.
    /// </summary>
    public static class MultiTenantHttpClientFactoryExtensions
    {
        /// <summary>
        /// Returns a cached <see cref="HttpClient"/> for the specified tenant if one exists;
        /// otherwise creates a new client using the factory.
        /// </summary>
        /// <param name="factory">The HTTP client factory instance.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>The cached or newly created HTTP client.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tenantId"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
        public static HttpClient GetOrCreateClient(this MultiTenantHttpClientFactory factory, string tenantId)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

            var cached = factory.GetCachedClient(tenantId);
            return cached ?? factory.CreateClientForTenant(tenantId);
        }

        /// <summary>
        /// Sets (or replaces) a default request header for the tenant's <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="factory">The HTTP client factory instance.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="headerName">The header name to set.</param>
        /// <param name="headerValue">The header value to set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tenantId"/> is <see langword="null"/>, empty, or consists only of whitespace.
        ///   or <paramref name="headerName"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
        public static void SetDefaultHeader(this MultiTenantHttpClientFactory factory, string tenantId, string headerName, string headerValue)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
            ArgumentException.ThrowIfNullOrWhiteSpace(headerName);

            var client = factory.GetOrCreateClient(tenantId);

            // Remove any existing header with the same name to avoid duplicates.
            if (client.DefaultRequestHeaders.Contains(headerName))
            {
                client.DefaultRequestHeaders.Remove(headerName);
            }

            client.DefaultRequestHeaders.Add(headerName, headerValue);
        }

        /// <summary>
        /// Configures the request timeout for the tenant's <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="factory">The HTTP client factory instance.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="timeout">The request timeout duration.</param>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tenantId"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is less than or equal to <see cref="TimeSpan.Zero"/>.</exception>
        public static void SetTimeout(this MultiTenantHttpClientFactory factory, string tenantId, TimeSpan timeout)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero.");
            }

            var client = factory.GetOrCreateClient(tenantId);
            client.Timeout = timeout;
        }

        /// <summary>
        /// Invalidates the cached client for the given tenant and creates a fresh instance.
        /// </summary>
        /// <param name="factory">The HTTP client factory instance.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>The newly created HTTP client.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tenantId"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
        public static HttpClient RefreshClient(this MultiTenantHttpClientFactory factory, string tenantId)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

            factory.InvalidateClient(tenantId);
            return factory.CreateClientForTenant(tenantId);
        }
    }
}
