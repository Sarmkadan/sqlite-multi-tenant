using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SqliteMultiTenant.Integration
{
    /// <summary>
    /// Extension methods that add convenient helpers for <see cref="HttpClientWrapper"/>.
    /// </summary>
    public static class HttpClientWrapperExtensions
    {
        /// <summary>
        /// Adds a default <c>Accept: application/json</c> header to the wrapped <see cref="HttpClientWrapper"/>.
        /// </summary>
        /// <param name="client">The <see cref="HttpClientWrapper"/> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="client"/> is <c>null</c>.</exception>
        public static void AddJsonAcceptHeader(this HttpClientWrapper client)
        {
            ArgumentNullException.ThrowIfNull(client);
            client.AddDefaultHeader("Accept", "application/json");
        }

        /// <summary>
        /// Sends a GET request and deserialises the response as a read‑only list of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The element type of the expected collection.</typeparam>
        /// <param name="client">The <see cref="HttpClientWrapper"/> instance.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <returns>
        /// An <see cref="IReadOnlyList{T}"/> containing the deserialized items, or <c>null</c>
        /// if the underlying <see cref="HttpClientWrapper.GetAsync{T}"/> returns <c>null</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="client"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="requestUri"/> is <c>null</c> or empty.</exception>
        public static async Task<IReadOnlyList<T>?> GetListAsync<T>(this HttpClientWrapper client, string requestUri)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrEmpty(requestUri);
            return await client.GetAsync<IReadOnlyList<T>>(requestUri).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a DELETE request and returns <c>true</c> only when the operation succeeded.
        /// </summary>
        /// <param name="client">The <see cref="HttpClientWrapper"/> instance.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <returns><c>true</c> if the DELETE request succeeded; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="client"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="requestUri"/> is <c>null</c> or empty.</exception>
        public static async Task<bool> DeleteIfExistsAsync(this HttpClientWrapper client, string requestUri)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrEmpty(requestUri);
            return await client.DeleteAsync(requestUri).ConfigureAwait(false);
        }
    }
}
