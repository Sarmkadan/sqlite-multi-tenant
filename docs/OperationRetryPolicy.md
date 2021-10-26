# OperationRetryPolicy

`OperationRetryPolicy` is a utility class that provides configurable retry logic for operations that may transiently fail, such as database operations in multi-tenant SQLite environments. It supports customizable retry counts, delays, backoff strategies, and logging to improve resilience against temporary failures.

## API

### `OperationRetryPolicy` (constructor)

Initializes a new instance of the `OperationRetryPolicy` class with the specified retry configuration.

- **Parameters**
  - `maxRetries`: The maximum number of retry attempts.
  - `initialDelay`: The initial delay between retry attempts.
  - `backoffMultiplier`: The multiplier applied to the delay after each retry attempt.
  - `logger`: An optional logger for recording retry attempts and failures.

- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `maxRetries` is negative or `initialDelay` is not positive.

---

### `ExecuteAsync<T>(Func<Task<T>> operation)`

Executes the provided asynchronous operation with retry logic applied.

- **Parameters**
  - `operation`: A function that returns a `Task<T>` representing the operation to execute.

- **Return Value**
  - Returns a `Task<T>` that completes with the result of the operation after successful execution or after all retry attempts are exhausted.

- **Exceptions**
  - Throws `ArgumentNullException` if `operation` is `null`.
  - Propagates any exceptions thrown by the operation if all retry attempts fail.

---

### `ExecuteAsync(Func<Task> operation)`

Executes the provided asynchronous operation with retry logic applied.

- **Parameters**
  - `operation`: A function that returns a `Task` representing the operation to execute.

- **Return Value**
  - Returns a `Task` that completes when the operation succeeds or after all retry attempts are exhausted.

- **Exceptions**
  - Throws `ArgumentNullException` if `operation` is `null`.
  - Propagates any exceptions thrown by the operation if all retry attempts fail.

---

### `RetryPolicyBuilder`

A builder class for constructing `OperationRetryPolicy` instances with fluent configuration.

---

### `RetryPolicyBuilder.WithMaxRetries(int maxRetries)`

Sets the maximum number of retry attempts.

- **Parameters**
  - `maxRetries`: The maximum number of retry attempts.

- **Return Value**
  - Returns the current `RetryPolicyBuilder` instance for method chaining.

- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `maxRetries` is negative.

---

### `RetryPolicyBuilder.WithInitialDelay(TimeSpan initialDelay)`

Sets the initial delay between retry attempts.

- **Parameters**
  - `initialDelay`: The initial delay before the first retry.

- **Return Value**
  - Returns the current `RetryPolicyBuilder` instance for method chaining.

- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `initialDelay` is not positive.

---
### `RetryPolicyBuilder.WithBackoffMultiplier(double multiplier)`

Sets the multiplier applied to the delay after each retry attempt.

- **Parameters**
  - `multiplier`: The factor by which the delay is multiplied after each retry.

- **Return Value**
  - Returns the current `RetryPolicyBuilder` instance for method chaining.

- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `multiplier` is not positive.

---
### `RetryPolicyBuilder.WithLogger(IAuditLogger logger)`

Sets the logger used to record retry attempts and failures.

- **Parameters**
  - `logger`: The logger instance to use.

- **Return Value**
  - Returns the current `RetryPolicyBuilder` instance for method chaining.

---
### `OperationRetryPolicy Build()`

Builds and returns a configured `OperationRetryPolicy` instance.

- **Return Value**
  - Returns a new `OperationRetryPolicy` instance with the configured settings.

- **Exceptions**
  - Throws `InvalidOperationException` if required settings (e.g., `maxRetries` or `initialDelay`) are missing or invalid.

## Usage

### Example 1: Basic retry with default settings
