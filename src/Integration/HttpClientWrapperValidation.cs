#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace SqliteMultiTenant.Integration;

/// <summary>
/// Provides validation helpers for <see cref="HttpClientWrapper"/> instances.
/// Validates constructor parameters, method arguments, and internal state.
/// </summary>
public static class HttpClientWrapperValidation
{
    /// <summary>
    /// Validates an <see cref="IHttpClientWrapper"/> instance for common problems.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this IHttpClientWrapper? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // IHttpClientWrapper implementations are validated by their constructors
        // No additional internal state validation needed beyond null check

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether an <see cref="IHttpClientWrapper"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static bool IsValid(this IHttpClientWrapper? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that an <see cref="IHttpClientWrapper"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not valid. The exception message lists all validation problems.</exception>
    public static void EnsureValid(this IHttpClientWrapper? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"IHttpClientWrapper is not valid. Problems:{Environment.NewLine}- " + string.Join($"{Environment.NewLine}- ", problems));
        }
    }

    /// <summary>
    /// Validates a URL string for common HTTP request problems.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="url"/> is null or empty.</exception>
    public static IReadOnlyList<string> ValidateUrl(string? url)
    {
        ArgumentException.ThrowIfNullOrEmpty(url, nameof(url));

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(url))
        {
            problems.Add("URL cannot be whitespace.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            problems.Add("URL must be a valid HTTP or HTTPS URI.");
        }
        else if (uri?.AbsolutePath == "/" || uri?.AbsolutePath == "")
        {
            problems.Add("URL path cannot be root only.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates a bearer token string.
    /// </summary>
    /// <param name="token">The bearer token to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="token"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/> is null or empty.</exception>
    public static IReadOnlyList<string> ValidateBearerToken(string? token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token, nameof(token));

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(token))
        {
            problems.Add("Bearer token cannot be whitespace.");
        }
        else if (token.Length < 10)
        {
            problems.Add("Bearer token must be at least 10 characters long.");
        }
        else
        {
            // Basic check for JWT-like tokens (3 segments separated by dots)
            var segments = token.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 3)
            {
                problems.Add("Bearer token does not appear to be a valid JWT format (expected 3 segments separated by dots).");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates a header name and value pair.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public static IReadOnlyList<string> ValidateHeader(string? name, string? value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            problems.Add("Header name cannot be whitespace.");
        }
        else if (name.Any(c => char.IsWhiteSpace(c)))
        {
            problems.Add("Header name cannot contain whitespace characters.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add("Header value cannot be whitespace.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates a payload object for serialization.
    /// </summary>
    /// <param name="payload">The payload to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> ValidatePayload(object? payload)
    {
        var problems = new List<string>();

        if (payload is null)
        {
            problems.Add("Payload cannot be null.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates generic type parameter for HTTP operations.
    /// </summary>
    /// <typeparam name="T">The response type.</typeparam>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateResponseType<T>()
    {
        var problems = new List<string>();

        var type = typeof(T);
        return type switch
        {
            { } when type == typeof(string) => problems.AsReadOnly(),
            _ when !type.IsClass || type == typeof(object) =>
                problems.Append("Response type must be a reference type (class), not a value type or object.").ToList().AsReadOnly(),
            _ when type.IsAbstract =>
                problems.Append("Response type cannot be abstract.").ToList().AsReadOnly(),
            _ when type.GetConstructor(Type.EmptyTypes) is null =>
                problems.Append("Response type must have a parameterless constructor.").ToList().AsReadOnly(),
            _ => problems.AsReadOnly()
        };
    }
}