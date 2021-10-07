# ConnectionPoolOptions

Configuration options for managing a tenant-specific SQLite connection pool, controlling size bounds, timeouts, and pruning behavior.

## API

### `MinPoolSize`
Gets or sets the minimum number of connections to maintain in the pool. Must be non-negative. Defaults to 0.

### `MaxPoolSize`
Gets or sets the maximum number of connections allowed in the pool. Must be greater than or equal to `MinPoolSize`. Defaults to 100.

### `IdleTimeout`
Gets or sets the duration after which idle connections are eligible for pruning. Must be non-negative. Defaults to 5 minutes.

### `AcquireTimeout`
Gets or sets the maximum duration to wait for a connection to become available. Must be non-negative. Throws `InvalidOperationException` if set while connections are in use.

### `MaxConnectionLifetime`
Gets or sets the maximum lifetime of a connection before it is closed and replaced. Must be non-negative. Defaults to 30 minutes.

### `PruneInterval`
Gets or sets the interval between connection pruning cycles. Must be non-negative. Defaults to 1 minute.

### `Validate()`
Validates the current configuration. Throws `InvalidOperationException` if any constraint is violated (e.g., `MaxPoolSize < MinPoolSize`).

### `TenantId`
Gets the tenant identifier associated with this pool configuration.

### `Available`
Gets the current number of available (idle) connections in the pool.

### `Total`
Gets the total number of connections currently in the pool, including both active and idle.

### `Waiting`
Gets the current number of threads waiting for a connection.

### `PrunedTotal`
Gets the total number of connections pruned since the pool was created.

### `LastPruneAt`
Gets the timestamp of the most recent pruning operation.

## Usage

### Example 1: Basic Configuration
