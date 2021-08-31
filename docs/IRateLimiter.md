# IRateLimiter

Provides an asynchronous API for enforcing rate limits on operations identified by a string key. The implementation uses a sliding‑window or fixed‑bucket algorithm internally, allowing callers to query whether a request is permitted, retrieve current usage statistics, and reset the limiter when needed.

## API

### `RateLimiter()`
Initializes a new instance of the `RateLimiter` class. The instance is ready to accept calls to its asynchronous methods after construction.

### `Task<RateLimitResult> CheckLimitAsync()`
Evaluates whether the next request is allowed under the current limit policy.

- **Return value**: A `RateLimitResult` indicating if the request is permitted (`IsAllowed`), the current count of requests within the window, the configured maximum (`MaxCount`), and the time at which the limit will reset (`ResetTime`).
- **Exceptions**: May throw `ObjectDisposedException` if the limiter has been disposed; may propagate any internal storage exceptions (e.g., SQLite errors) as `InvalidOperationException`.

### `Task ResetAsync()`
Resets the internal counters for all tracked identifiers, effectively clearing the request history.

- **Return value**: Completes when the reset operation has been persisted.
- **Exceptions**: Throws `ObjectDisposedException` if called after `Dispose`; may throw `InvalidOperationException` on underlying storage failures.

### `Task<RateLimitStatus> GetStatusAsync()`
Retrieves a snapshot of the current limiter state.

- **Return value**: A `RateLimitStatus` containing the identifier being tracked and the current request count (`CurrentCount`).
- **Exceptions**: Same as `CheckLimitAsync` – `ObjectDisposedException` when disposed, otherwise storage‑related exceptions.

### `Task<RateLimiterStatistics> GetStatisticsAsync()`
Obtains detailed statistics about limiter usage (e.g., total requests, hit/miss ratios).

- **Return value**: A `RateLimiterStatistics` instance; the exact members of this type are not part of the public contract documented here.
- **Exceptions**: Throws `ObjectDisposedException` if the limiter has been disposed; may throw `InvalidOperationException` for storage errors.

### `void Dispose()`
Releases any unmanaged resources (e.g., database connections) held by the limiter. After disposal, further calls to any instance method will throw `ObjectDisposedException`.

### Nested Types

#### `RateLimitBucket`
Represents the internal storage for a single identifier.

- `Identifier` (string): The key associated with this bucket.
- `CreatedAt` (DateTime): Timestamp when the bucket was first created.
- `LastAccessedAt` (DateTime): Timestamp of the most recent request that touched this bucket.
- `Requests` (List<DateTime>): Collection of timestamps for each request recorded in the bucket.

#### `RateLimitResult`
Outcome of a limit check.

- `IsAllowed` (bool): `true` if the request may proceed; otherwise `false`.
- `CurrentCount` (int): Number of requests recorded for the identifier within the current window.
- `MaxCount` (int): Configured maximum allowed requests in the window.
- `ResetTime` (DateTime): UTC time when the current window will expire and the count will be reset.

#### `RateLimitStatus`
Current usage snapshot for an identifier.

- `Identifier` (string): The key being monitored.
- `CurrentCount` (int): Number of requests recorded for this identifier in the active window.

## Usage

```csharp
using var limiter = new RateLimiter(); // assumes appropriate DI or factory setup

var result = await limiter.CheckLimitAsync();
if (result.IsAllowed)
{
    // Process the request
    await ProcessRequestAsync();
}
else
{
    // Inform the caller to retry later
    await RespondWithRetryAfterAsync(result.ResetTime);
}
```

```csharp
// Periodic maintenance task to clear stale data
await limiter.ResetAsync();

// Retrieve status for logging or monitoring
var status = await limiter.GetStatusAsync();
_logger.Info("Limiter {Id} count: {Count}", status.Identifier, status.CurrentCount);
```

## Notes

- The class is `sealed`; inheritance is not supported. Thread‑safety depends on the underlying implementation, but the public asynchronous methods are designed to be safely invoked concurrently from multiple threads. Callers should not rely on any specific ordering of operations beyond what the return values convey.
- Disposing the limiter while asynchronous operations are pending will cause those operations to complete with an `ObjectDisposedException`. It is recommended to await all in‑flight calls before invoking `Dispose`.
- The `CheckLimitAsync` method does not accept parameters; the identifier to evaluate is implicitly bound to the limiter instance (e.g., configured at construction time). If per‑key limiting is required, multiple `RateLimiter` instances should be used.
- The `Requests` list inside `RateLimitBucket` may grow unbounded if the limiter is not reset periodically; consumers should call `ResetAsync` or rely on internal expiration mechanisms to prevent excessive memory consumption.
