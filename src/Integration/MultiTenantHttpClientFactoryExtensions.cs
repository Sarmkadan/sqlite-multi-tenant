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
        public static HttpClient GetOrCreateClient(this MultiTenantHttpClientFactory factory, string tenantId)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));

            var cached = factory.GetCachedClient(tenantId);
            return cached ?? factory.CreateClientForTenant(tenantId);
        }

        /// <summary>
        /// Sets (or replaces) a default request header for the tenant's <see cref="HttpClient"/>.
        /// </summary>
        public static void SetDefaultHeader(this MultiTenantHttpClientFactory factory, string tenantId, string headerName, string headerValue)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (string.IsNullOrWhiteSpace(headerName)) throw new ArgumentException("Header name cannot be null or empty.", nameof(headerName));

            var client = factory.GetOrCreateClient(tenantId);

            // Remove any existing header with the same name to avoid duplicates.
            if (client.DefaultRequestHeaders.Contains(headerName))
                client.DefaultRequestHeaders.Remove(headerName);

            client.DefaultRequestHeaders.Add(headerName, headerValue);
        }

        /// <summary>
        /// Configures the request timeout for the tenant's <see cref="HttpClient"/>.
        /// </summary>
        public static void SetTimeout(this MultiTenantHttpClientFactory factory, string tenantId, TimeSpan timeout)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero.");

            var client = factory.GetOrCreateClient(tenantId);
            client.Timeout = timeout;
        }

        /// <summary>
        /// Invalidates the cached client for the given tenant and creates a fresh instance.
        /// </summary>
        public static HttpClient RefreshClient(this MultiTenantHttpClientFactory factory, string tenantId)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            factory.InvalidateClient(tenantId);
            return factory.CreateClientForTenant(tenantId);
        }
    }
}
