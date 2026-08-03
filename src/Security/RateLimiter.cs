#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Security;

/// <summary>
/// Implements rate limiting to prevent abuse and DoS attacks.
/// Supports token bucket algorithm with per-IP and per-user limits.
/// Provides configurable rate limits and cleanup of expired entries.
/// </summary>
public interface IRateLimiter
{
    Task<RateLimitResult> CheckLimitAsync(string identifier, int maxRequests, TimeSpan window);
    Task ResetAsync(string identifier);
    Task<RateLimitStatus> GetStatusAsync(string identifier);
}

public sealed class RateLimiter : IRateLimiter
{
    private readonly Dictionary<string, RateLimitBucket> _buckets;
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<RateLimiter> _logger;
    private readonly Timer _cleanupTimer;
    private readonly RateLimiterOptions _options;

    /// <summary>
    /// Creates a new <see cref="RateLimiter"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">
    /// Optional configuration. If <c>null</c>, default options are used.
    /// This overload maintains compatibility with existing call sites that only pass a logger.
    /// </param>
    public RateLimiter(ILogger<RateLimiter> logger, RateLimiterOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new RateLimiterOptions();
        _buckets = new Dictionary<string, RateLimitBucket>();
        _semaphore = new SemaphoreSlim(1);
        _cleanupTimer = new Timer(CleanupExpiredBuckets, null, _options.CleanupInterval, _options.CleanupInterval);
    }

    /// <summary>
    /// Checks if a request is allowed under the rate limit.
    /// Uses token bucket algorithm.
    /// </summary>
    public async Task<RateLimitResult> CheckLimitAsync(
        string identifier,
        int maxRequests,
        TimeSpan window)
    {
        try
        {
            await _semaphore.WaitAsync();

            var now = DateTime.UtcNow;

            // Get or create bucket
            if (!_buckets.TryGetValue(identifier, out var bucket))
            {
                bucket = new RateLimitBucket
                {
                    Identifier = identifier,
                    CreatedAt = now,
                    LastAccessedAt = now,
                    Requests = new List<DateTime>()
                };
                _buckets[identifier] = bucket;
            }

            bucket.LastAccessedAt = now;

            // Remove old requests outside the window
            var windowStart = now.Subtract(window);
            bucket.Requests.RemoveAll(r => r < windowStart);

            // Check if limit is exceeded
            bool allowed = bucket.Requests.Count < maxRequests;

            if (allowed)
            {
                bucket.Requests.Add(now);
            }

            var result = new RateLimitResult
            {
                IsAllowed = allowed,
                CurrentCount = bucket.Requests.Count,
                MaxCount = maxRequests,
                ResetTime = bucket.Requests.Count > 0
                    ? bucket.Requests.First().Add(window)
                    : now.Add(window)
            };

            if (!allowed)
            {
                _logger.LogWarning(
                    $"Rate limit exceeded for {identifier}: " +
                    $"{bucket.Requests.Count}/{maxRequests} requests");
            }

            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Resets the rate limit for an identifier.
    /// </summary>
    public async Task ResetAsync(string identifier)
    {
        try
        {
            await _semaphore.WaitAsync();

            if (_buckets.Remove(identifier))
            {
                _logger.LogInformation("Rate limit reset for {Identifier}", identifier);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets the current rate limit status.
    /// </summary>
    public async Task<RateLimitStatus> GetStatusAsync(string identifier)
    {
        try
        {
            await _semaphore.WaitAsync();

            if (_buckets.TryGetValue(identifier, out var bucket))
            {
                return new RateLimitStatus
                {
                    Identifier = identifier,
                    CurrentCount = bucket.Requests.Count,
                    CreatedAt = bucket.CreatedAt,
                    LastAccessedAt = bucket.LastAccessedAt
                };
            }

            return new RateLimitStatus { Identifier = identifier, CurrentCount = 0 };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets statistics about rate limiting.
    /// </summary>
    public async Task<RateLimiterStatistics> GetStatisticsAsync()
    {
        try
        {
            await _semaphore.WaitAsync();

            return new RateLimiterStatistics
            {
                ActiveBuckets = _buckets.Count,
                TotalRequests = _buckets.Values.Sum(b => b.Requests.Count),
                OldestBucket = _buckets.Values.Min(b => b.CreatedAt),
                NewestBucket = _buckets.Values.Max(b => b.CreatedAt),
                Timestamp = DateTime.UtcNow
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void CleanupExpiredBuckets(object? state)
    {
        try
        {
            _semaphore.Wait();

            var now = DateTime.UtcNow;
            var expirationTime = _options.ExpirationTime;
            var keysToRemove = _buckets
                .Where(kvp => now - kvp.Value.LastAccessedAt > expirationTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
                _buckets.Remove(key);

            if (keysToRemove.Count > 0)
                _logger.LogInformation("Cleaned up {Count} expired rate limit buckets", keysToRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during rate limiter cleanup: {Message}", ex.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}

public sealed class RateLimitBucket
{
    public string Identifier { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public List<DateTime> Requests { get; set; } = new();
}

public sealed class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int CurrentCount { get; set; }
    public int MaxCount { get; set; }
    public DateTime ResetTime { get; set; }
    public int RemainingRequests => Math.Max(0, MaxCount - CurrentCount);
    public TimeSpan TimeUntilReset => ResetTime - DateTime.UtcNow;
}

public sealed class RateLimitStatus
{
    public string Identifier { get; set; } = string.Empty;
    public int CurrentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
}

public sealed class RateLimiterStatistics
{
    public int ActiveBuckets { get; set; }
    public int TotalRequests { get; set; }
    public DateTime OldestBucket { get; set; }
    public DateTime NewestBucket { get; set; }
    public DateTime Timestamp { get; set; }
}
