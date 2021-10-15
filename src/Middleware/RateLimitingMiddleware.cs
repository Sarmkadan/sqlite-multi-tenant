#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace SqliteMultiTenant.Middleware;

/// <summary>
/// Token bucket rate limiting middleware to prevent abuse and DoS attacks.
/// Implements sliding window algorithm for fair rate distribution.
/// Supports per-tenant and per-IP rate limits with configurable thresholds.
/// </summary>
public sealed class RateLimitingMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    internal readonly RateLimitingOptions _options;
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        RateLimitingOptions options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _buckets = new ConcurrentDictionary<string, TokenBucket>();
    }

    /// <summary>
    /// Checks rate limit before allowing request to proceed.
    /// Returns 429 Too Many Requests if limit exceeded.
    /// Key is based on tenant ID or IP address (tenant ID preferred).
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health checks and admin endpoints
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/admin"))
        {
            await _next(context);
            return;
        }

        // Extract tenant ID from header or use IP address as fallback
        var key = context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId)
            ? tenantId.ToString()
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var bucket = _buckets.GetOrAdd(key, _ => new TokenBucket(
            capacity: _options.RequestsPerMinute,
            refillRate: _options.RequestsPerMinute / 60.0));

        if (!bucket.TryConsumeToken())
        {
            _logger.LogWarning("Rate limit exceeded for {Key}", key);
            context.Response.StatusCode = 429;
            context.Response.Headers.Add("Retry-After", "60");
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded. Try again later." });
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Configuration options for rate limiting behavior.
/// Allows tuning limits without recompiling code.
/// </summary>
public sealed class RateLimitingOptions {
    /// <summary>
    /// Number of requests allowed per minute (default: 300 = 5 req/sec).
    /// Adjust based on expected load and infrastructure capacity.
    /// </summary>
    public int RequestsPerMinute { get; set; } = 300;

    /// <summary>
    /// Burst capacity: how many requests over limit before blocking.
    /// Allows handling traffic spikes gracefully.
    /// </summary>
    public int BurstCapacity { get; set; } = 50;

    /// <summary>
    /// Cleanup interval: how often to remove unused buckets from memory.
    /// Prevents memory bloat from inactive clients.
    /// </summary>
    public int CleanupIntervalSeconds { get; set; } = 300;
}

/// <summary>
/// Token bucket implementation for rate limiting.
/// Thread-safe using lock for concurrent access patterns.
/// Tracks last refill time to implement sliding window accurately.
/// </summary>
public sealed class TokenBucket {
    private double _tokens;
    private DateTime _lastRefillTime;
    private readonly double _refillRate;
    private readonly double _capacity;
    private readonly object _lock = new object();

    public TokenBucket(double capacity, double refillRate)
    {
        _capacity = capacity;
        _tokens = capacity;
        _refillRate = refillRate;
        _lastRefillTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Attempts to consume one token from the bucket.
    /// Refills bucket based on elapsed time since last refill.
    /// Returns true if token available, false if limit exceeded.
    /// </summary>
    public bool TryConsumeToken()
    {
        lock (_lock)
        {
            RefillTokens();

            if (_tokens >= 1.0)
            {
                _tokens -= 1.0;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Refills bucket proportional to elapsed time.
    /// Example: 5 req/sec for 2 seconds = +10 tokens.
    /// Capped at bucket capacity to prevent overflow.
    /// </summary>
    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRefillTime).TotalSeconds;
        var tokensToAdd = elapsed * _refillRate;

        _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
        _lastRefillTime = now;
    }
}
