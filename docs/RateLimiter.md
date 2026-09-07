# RateLimiter

Implements rate limiting to prevent abuse and DoS attacks. Supports sliding window algorithm with per-identifier tracking. Provides configurable rate limits and automatic cleanup of expired entries.

## Algorithm

The rate limiter uses a sliding window approach:
- For each identifier, it maintains a list of request timestamps
- When checking a request, it removes timestamps older than the window from the list
- If the count of remaining timestamps is below the limit, the request is allowed and the current timestamp is added
- Otherwise, the request is rejected

This implementation provides a more accurate rate limit than fixed window algorithms by preventing bursts at window boundaries.

## Cleanup Mechanism

A timer periodically runs (default every 5 minutes) to remove buckets that haven't been accessed for the expiration time (default 1 hour). This prevents memory leaks from accumulating stale identifier data.

## Public Methods

### `RateLimiter(ILogger<RateLimiter> logger, RateLimiterOptions? options = null)`

Creates a new rate limiter instance.

- **Parameters**:
  - `logger`: Logger instance for diagnostic information
  - `options`: Optional configuration. If null, default options are used (CleanupInterval=5 minutes, ExpirationTime=1 hour)
- **Exceptions**: None

### `Task<RateLimitResult> CheckLimitAsync(string identifier, int maxRequests, TimeSpan window)`

Checks if a request is allowed under the rate limit for the given identifier.

- **Parameters**:
  - `identifier`: Unique key to track (e.g., IP address, user ID, API key)
  - `maxRequests`: Maximum number of requests allowed in the window
  - `window`: Time span defining the rate limit window
- **Return value**: `RateLimitResult` containing:
  - `IsAllowed`: Boolean indicating if the request is permitted
  - `CurrentCount`: Number of requests recorded for the identifier within the current window
  - `MaxCount`: Configured maximum allowed requests in the window
  - `ResetTime`: UTC time when the current window will expire and the count will be reset
- **Exceptions**:
  - `ArgumentException`: If identifier is null or empty
- **Thread safety**: Safe for concurrent calls from multiple threads

### `Task ResetAsync(string identifier)`

Resets the rate limit for a specific identifier, removing its tracking data.

- **Parameters**:
  - `identifier`: The identifier to reset
- **Return value**: Completes when the reset operation is finished
- **Exceptions**:
  - `ArgumentException`: If identifier is null or empty
- **Thread safety**: Safe for concurrent calls

### `Task<RateLimitStatus> GetStatusAsync(string identifier)`

Gets the current rate limit status for an identifier.

- **Parameters**:
  - `identifier`: The identifier to check
- **Return value**: `RateLimitStatus` containing:
  - `Identifier`: The key being monitored
  - `CurrentCount`: Number of requests recorded for this identifier in the active window
  - `CreatedAt`: Timestamp when the bucket was first created
  - `LastAccessedAt`: Timestamp of the most recent request
- **Exceptions**:
  - `ArgumentException`: If identifier is null or empty
- **Thread safety**: Safe for concurrent calls

### `Task<RateLimiterStatistics> GetStatisticsAsync()`

Gets overall statistics about the rate limiter's usage.

- **Return value**: `RateLimiterStatistics` containing:
  - `ActiveBuckets`: Number of identifier buckets currently being tracked
  - `TotalRequests`: Total number of requests across all buckets
  - `OldestBucket`: Creation timestamp of the oldest bucket
  - `NewestBucket`: Creation timestamp of the newest bucket
  - `Timestamp`: When the statistics were collected
- **Thread safety**: Safe for concurrent calls

### `void Dispose()`

Releases resources used by the rate limiter, stopping the cleanup timer.

- **Exceptions**: None
- **Thread safety**: Safe to call from any thread; subsequent method calls will throw `ObjectDisposedException`

## Relation to IRateLimiter

This class implements the `IRateLimiter` interface defined in [IRateLimiter.md](IRateLimiter.md). The interface provides the core contract for rate limiting operations, and this implementation fulfills all interface methods with the sliding window algorithm described above.

## Relation to RateLimitingMiddleware

The `RateLimiter` is used by the `RateLimitingMiddleware` ([see RateLimitingMiddleware.md](RateLimitingMiddleware.md)) to enforce rate limits per tenant in ASP.NET Core applications. The middleware extracts tenant identifiers from HTTP requests and delegates limit checking to this class.

## Configuration (RateLimiterOptions)

Behavior can be customized via the `RateLimiterOptions` class:

### `CleanupInterval`
- **Type**: `TimeSpan`
- **Default**: 5 minutes (`TimeSpan.FromMinutes(5)`)
- **Description**: Interval at which the cleanup timer runs to purge expired buckets

### `ExpirationTime`
- **Type**: `TimeSpan`
- **Default**: 1 hour (`TimeSpan.FromHours(1)`)
- **Description**: Time after which a bucket that has not been accessed is considered expired and removed

These options have sensible defaults suitable for most applications but can be adjusted based on specific traffic patterns and memory constraints.

## Usage Example

```csharp
// Create limiter with default options
var limiter = new RateLimiter(logger);

// Check if a request from IP address is allowed
var result = await limiter.CheckLimitAsync("192.168.1.1", 100, TimeSpan.FromMinutes(1));
if (result.IsAllowed)
{
    // Process request
}
else
{
    // Return 429 Too Many Requests
    // Suggest retry after: result.TimeUntilReset
}

// Periodic cleanup (handled automatically by internal timer)
// Manual reset if needed
await limiter.ResetAsync("192.168.1.1");

// Get statistics
var stats = await limiter.GetStatisticsAsync();
logger.Info($"Active limiters: {stats.ActiveBuckets}, Total requests: {stats.TotalRequests}");
```