#nullable enable
// Author: Vladyslav Zaiets | https://sarmkadan.com
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SqliteMultiTenant.Security;

/// <summary>
/// Provides extension methods for <see cref="RateLimitResult"/> and <see cref="RateLimitStatus"/>
/// that render rate limit information as HTTP headers and human-readable text.
/// </summary>
public static class RateLimitResultExtensions
{
    /// <summary>
    /// Gets the value for the HTTP <c>Retry-After</c> header, expressed in whole seconds.
    /// </summary>
    /// <param name="result">The rate limit check result.</param>
    /// <returns>
    /// The number of seconds (rounded up, at least 1) until the rate limit resets,
    /// or <c>null</c> when the request was allowed and no retry delay applies.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    public static string? ToRetryAfterHeaderValue(this RateLimitResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsAllowed)
        {
            return null;
        }

        var seconds = Math.Max(1L, (long)Math.Ceiling(result.TimeUntilReset.TotalSeconds));
        return seconds.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Builds the standard <c>X-RateLimit-Limit</c>, <c>X-RateLimit-Remaining</c> and <c>X-RateLimit-Reset</c>
    /// headers for a rate limit check result.
    /// </summary>
    /// <param name="result">The rate limit check result.</param>
    /// <returns>
    /// A read-only dictionary of header names to values. <c>X-RateLimit-Reset</c> is the reset time
    /// as Unix epoch seconds (UTC).
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    public static IReadOnlyDictionary<string, string> ToRateLimitHeaders(this RateLimitResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var resetUtc = DateTime.SpecifyKind(result.ResetTime, DateTimeKind.Utc);
        var resetEpochSeconds = new DateTimeOffset(resetUtc).ToUnixTimeSeconds();

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-RateLimit-Limit"] = result.MaxCount.ToString(CultureInfo.InvariantCulture),
            ["X-RateLimit-Remaining"] = result.RemainingRequests.ToString(CultureInfo.InvariantCulture),
            ["X-RateLimit-Reset"] = resetEpochSeconds.ToString(CultureInfo.InvariantCulture)
        };
    }

    /// <summary>
    /// Returns a human-readable summary of a rate limit check result.
    /// </summary>
    /// <param name="result">The rate limit check result.</param>
    /// <returns>
    /// A summary such as <c>Allowed: 3/10 requests, resets in 42s</c> or
    /// <c>Denied: 10/10 requests, resets in 42s</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    public static string ToDisplayString(this RateLimitResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var state = result.IsAllowed ? "Allowed" : "Denied";
        var resetIn = Math.Max(0L, (long)Math.Ceiling(result.TimeUntilReset.TotalSeconds));

        return FormattableString.Invariant($"{state}: {result.CurrentCount}/{result.MaxCount} requests, resets in {resetIn}s");
    }

    /// <summary>
    /// Returns a human-readable summary of the current rate limit status for an identifier.
    /// </summary>
    /// <param name="status">The rate limit status.</param>
    /// <returns>
    /// A summary such as <c>'203.0.113.7': 4 request(s) tracked, last accessed 2026-10-25 12:00:00 UTC</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="status"/> is <c>null</c>.</exception>
    public static string ToDisplayString(this RateLimitStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        var lastAccessed = status.LastAccessedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        return FormattableString.Invariant($"'{status.Identifier}': {status.CurrentCount} request(s) tracked, last accessed {lastAccessed} UTC");
    }
}
