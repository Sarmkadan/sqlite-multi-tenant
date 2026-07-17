# RateLimitingMiddlewareValidation

Provides validation utilities for rate-limiting middleware configurations in multi-tenant SQLite applications. These static methods validate rate limit parameters such as burst limits, request counts, and time windows to ensure they conform to expected constraints and avoid misconfiguration that could degrade performance or cause throttling issues.

## API

### `Validate(IRateLimitConfiguration config)`

Validates an entire rate limit configuration object. Returns a list of validation error messages if the configuration is invalid; otherwise, returns an empty list.

- **Parameters**
  - `config`: The rate limit configuration to validate.
- **Returns**
  - `IReadOnlyList<string>`: A read-only list of error messages describing any validation failures. If empty, the configuration is valid.
- **Throws**
  - `ArgumentNullException`: If `config` is `null`.

### `Validate(int burstLimit, int requestCount, TimeSpan window)`

Validates individual rate limit parameters: burst limit, request count, and time window. Returns a list of validation error messages if any parameter is invalid; otherwise, returns an empty list.

- **Parameters**
  - `burstLimit`: The maximum number of requests allowed in a burst.
  - `requestCount`: The number of requests allowed within the time window.
  - `window`: The time window during which requests are counted.
- **Returns**
  - `IReadOnlyList<string>`: A read-only list of error messages describing any validation failures. If empty, the parameters are valid.
- **Throws**
  - `ArgumentOutOfRangeException`: If `burstLimit`, `requestCount`, or `window` are outside acceptable ranges.

### `Validate(IRateLimitPolicy policy)`

Validates a rate limit policy object. Returns a list of validation error messages if the policy is invalid; otherwise, returns an empty list.

- **Parameters**
  - `policy`: The rate limit policy to validate.
- **Returns**
  - `IReadOnlyList<string>`: A read-only list of error messages describing any validation failures. If empty, the policy is valid.
- **Throws**
  - `ArgumentNullException`: If `policy` is `null`.

### `IsValid(IRateLimitConfiguration config)`

Determines whether a rate limit configuration object is valid. Returns `true` if the configuration is valid; otherwise, returns `false`.

- **Parameters**
  - `config`: The rate limit configuration to validate.
- **Returns**
  - `bool`: `true` if the configuration is valid; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `config` is `null`.

### `IsValid(int burstLimit, int requestCount, TimeSpan window)`

Determines whether individual rate limit parameters are valid. Returns `true` if all parameters are valid; otherwise, returns `false`.

- **Parameters**
  - `burstLimit`: The maximum number of requests allowed in a burst.
  - `requestCount`: The number of requests allowed within the time window.
  - `window`: The time window during which requests are counted.
- **Returns**
  - `bool`: `true` if the parameters are valid; otherwise, `false`.
- **Throws**
  - `ArgumentOutOfRangeException`: If `burstLimit`, `requestCount`, or `window` are outside acceptable ranges.

### `IsValid(IRateLimitPolicy policy)`

Determines whether a rate limit policy object is valid. Returns `true` if the policy is valid; otherwise, returns `false`.

- **Parameters**
  - `policy`: The rate limit policy to validate.
- **Returns**
  - `bool`: `true` if the policy is valid; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `policy` is `null`.

### `EnsureValid(IRateLimitConfiguration config)`

Validates a rate limit configuration object and throws an exception if it is invalid. Does nothing if the configuration is valid.

- **Parameters**
  - `config`: The rate limit configuration to validate.
- **Throws**
  - `ArgumentNullException`: If `config` is `null`.
  - `InvalidOperationException`: If the configuration is invalid, with a message describing the validation failure.

### `EnsureValid(int burstLimit, int requestCount, TimeSpan window)`

Validates individual rate limit parameters and throws an exception if any parameter is invalid. Does nothing if all parameters are valid.

- **Parameters**
  - `burstLimit`: The maximum number of requests allowed in a burst.
  - `requestCount`: The number of requests allowed within the time window.
  - `window`: The time window during which requests are counted.
- **Throws**
  - `ArgumentOutOfRangeException`: If `burstLimit`, `requestCount`, or `window` are outside acceptable ranges.
  - `InvalidOperationException`: If the parameters are invalid, with a message describing the validation failure.

### `EnsureValid(IRateLimitPolicy policy)`

Validates a rate limit policy object and throws an exception if it is invalid. Does nothing if the policy is valid.

- **Parameters**
  - `policy`: The rate limit policy to validate.
- **Throws**
  - `ArgumentNullException`: If `policy` is `null`.
  - `InvalidOperationException`: If the policy is invalid, with a message describing the validation failure.

## Usage
