# RateLimitingMiddleware

A middleware component for ASP.NET Core that enforces rate limiting using a token bucket algorithm. This implementation tracks request rates per tenant and prevents abuse by rejecting requests that exceed configured thresholds, while allowing short bursts of traffic within defined capacity limits.

## API

### `RateLimitingMiddleware` class

#### `public RateLimitingMiddleware(RequestDelegate next, RateLimitingOptions options)`
Constructor that initializes the middleware with the next request delegate in the pipeline and rate limiting configuration.

- **Parameters**:
  - `next`: The next middleware in the ASP.NET Core pipeline.
  - `options`: Configuration options for rate limiting behavior.
- **Exceptions**:
  - `ArgumentNullException`: Thrown if `next` or `options` is `null`.

#### `public async Task InvokeAsync(HttpContext context)`
Processes an incoming HTTP request, applying rate limiting logic based on the tenant identifier.

- **Parameters**:
  - `context`: The `HttpContext` for the current request.
- **Returns**: A `Task` representing the asynchronous operation.
- **Behavior**:
  - Extracts the tenant identifier from the request (typically from headers or route data).
  - Attempts to consume a token from the tenant-specific bucket.
  - Returns HTTP 429 (Too Many Requests) if the rate limit is exceeded.
  - Otherwise, allows the request to proceed to the next middleware.

---

### `RateLimitingOptions` class

#### `public int RequestsPerMinute`
The number of requests allowed per tenant per minute. Defaults to `60`.

#### `public int BurstCapacity`
The maximum number of requests a tenant can make in a short burst, regardless of the per-minute rate. Defaults to `10`.

#### `public int CleanupIntervalSeconds`
The interval (in seconds) at which unused tenant buckets are cleaned up to free memory. Defaults to `300` (5 minutes).

---

### `TokenBucket` class

#### `public TokenBucket(int requestsPerMinute, int burstCapacity)`
Constructor that initializes a token bucket with the specified rate and burst capacity.

- **Parameters**:
  - `requestsPerMinute`: The sustained request rate per minute.
  - `burstCapacity`: The maximum burst capacity.
- **Exceptions**:
  - `ArgumentOutOfRangeException`: Thrown if `requestsPerMinute` or `burstCapacity` is less than or equal to `0`.

#### `public bool TryConsumeToken()`
Attempts to consume a single token from the bucket.

- **Returns**:
  - `true` if a token was successfully consumed.
  - `false` if the bucket is empty (rate limit exceeded).
- **Behavior**:
  - Replenishes tokens at the configured rate.
  - Tokens are consumed on a first-come, first-served basis.
  - Thread-safe for concurrent calls.

## Usage

### Example 1: Basic Middleware Registration
