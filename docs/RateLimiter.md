# Rate Limiter

## Overview
The `RateLimiter` component provides a thread-safe mechanism to prevent abuse and Denial of Service (DoS) attacks by enforcing request limits per identifier (e.g., IP address or user ID). It implements a sliding window counter approach to track request timestamps and enforce limits within configurable time windows.

## Configuration (`RateLimiterOptions`)
Behavior is controlled via `RateLimiterOptions`, which exposes two primary configuration properties:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CleanupInterval` | `TimeSpan` | 5 minutes | Interval at which the background cleanup timer runs to purge expired buckets. |
| `ExpirationTime` | `TimeSpan` | 1 hour | Time after which a bucket that has not been accessed is considered expired and removed from memory. |

## Interface (`IRateLimiter`)
The implementation adheres to the `IRateLimiter` contract, ensuring consistent behavior across the application. See [docs/IRateLimiter.md](IRateLimiter.md) for the full interface definition.

## Public API & Signatures
The `RateLimiter` class exposes the following public methods:

```csharp
public RateLimiter(ILogger<RateLimiter> logger, RateLimiterOptions? options = null)
public Task<RateLimitResult> CheckLimitAsync(string identifier, int maxRequests, TimeSpan window)
public Task ResetAsync(string identifier)
public Task<RateLimitStatus> GetStatusAsync(string identifier)
public Task<RateLimiterStatistics> GetStatisticsAsync()
public void Dispose()
```

### Method Details
- **`CheckLimitAsync`**: Evaluates whether a request is allowed. Returns a `RateLimitResult` indicating allowance, current usage, and reset time.
- **`ResetAsync`**: Clears all tracking data for a specific identifier.
- **`GetStatusAsync`**: Retrieves the current state and request count for an identifier.
- **`GetStatisticsAsync`**: Returns aggregate statistics about active buckets and total tracked requests.
- **`Dispose`**: Cleans up the background cleanup timer.

## Algorithm
The rate limiter uses a **sliding window counter** approach backed by a `Dictionary<string, RateLimitBucket>` (`_buckets`). Each `RateLimitBucket` holds a `List<DateTime>` of request timestamps. The implementation visible in `RateLimiter.cs` follows these steps:

1. **Thread Safety**: All public methods acquire a lock via `_semaphore.WaitAsync()` to prevent concurrent dictionary access issues.
2. **Bucket Retrieval/Creation**: `CheckLimitAsync` looks up the identifier in `_buckets`. If missing, a new `RateLimitBucket` is instantiated with `CreatedAt` and `LastAccessedAt` set to `DateTime.UtcNow`.
3. **Window Pruning**: Old requests are removed using `bucket.Requests.RemoveAll(r => r < windowStart)`, where `windowStart = now.Subtract(window)`.
4. **Limit Check & Recording**: If `bucket.Requests.Count < maxRequests`, the request is allowed and `now` is added to the list. A `RateLimitResult` is returned with `IsAllowed`, `CurrentCount`, `MaxCount`, and `ResetTime`.
5. **Background Cleanup**: The `Timer` callback `CleanupExpiredBuckets` runs every `CleanupInterval`. It iterates over `_buckets`, identifies keys where `now - kvp.Value.LastAccessedAt > expirationTime`, and removes them. This prevents memory leaks from stale identifiers.

## Integration
- **`IRateLimiter`**: The class implements the `IRateLimiter` interface, allowing dependency injection and polymorphic usage throughout the codebase.
- **`RateLimitingMiddleware`**: The middleware consumes `IRateLimiter` to intercept incoming HTTP requests, apply limits, and return appropriate `429 Too Many Requests` responses or headers when thresholds are breached. See [docs/RateLimitingMiddleware.md](RateLimitingMiddleware.md) for middleware configuration and usage examples.

## References
- [IRateLimiter.md](IRateLimiter.md)
- [RateLimitingMiddleware.md](RateLimitingMiddleware.md)
