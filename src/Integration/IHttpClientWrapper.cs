#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Threading.Tasks;

namespace SqliteMultiTenant.Integration;

/// <summary>
/// Defines the contract for an HTTP client wrapper that provides typed HTTP operations
/// with retry logic, timeout handling, and request/response logging.
/// </summary>
public interface IHttpClientWrapper
{
    /// <summary>
    /// Sends a GET request and deserializes response to specified type.
    /// Includes retry logic for transient failures.
    /// </summary>
    /// <typeparam name="T">The response type to deserialize.</typeparam>
    /// <param name="url">The request URL.</param>
    /// <returns>The deserialized response, or <see langword="null"/> on failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="url"/> is empty or whitespace.</exception>
    Task<T?> GetAsync<T>(string url) where T : class;

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
    Task<T?> PostAsync<T>(string url, object payload) where T : class;

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
    Task<bool> PutAsync(string url, object payload);

    /// <summary>
    /// Sends a DELETE request.
    /// </summary>
    /// <param name="url">The request URL.</param>
    /// <returns><see langword="true"/> if the request succeeded; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="url"/> is empty or whitespace.</exception>
    Task<bool> DeleteAsync(string url);

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
    void AddDefaultHeader(string name, string value);

    /// <summary>
    /// Sets the authorization header with bearer token.
    /// </summary>
    /// <param name="token">The bearer token.</param>
    /// <exception cref="ArgumentNullException"><paramref name="token"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/> is empty or whitespace.</exception>
    void SetBearerToken(string token);
}
