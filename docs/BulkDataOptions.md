# BulkDataOptions

`BulkDataOptions` is a configuration class used to control the behavior of bulk data operations in the `sqlite-multi-tenant` project. It defines parameters such as batch sizes, concurrency limits, timeouts, and file system paths to optimize and customize large-scale data imports, exports, and processing operations.

## API

### `DefaultBatchSize`
- **Purpose**: Specifies the default number of records processed in a single batch during bulk operations.
- **Type**: `int`
- **Default**: Implementation-specific (typically set to a reasonable default, e.g., 1000).
- **Behavior**: Larger values may improve throughput but increase memory usage. Must be a positive integer.
- **Throws**: `ArgumentOutOfRangeException` if set to a non-positive value.

### `MaxConcurrentTables`
- **Purpose**: Limits the number of tables processed concurrently during bulk operations.
- **Type**: `int`
- **Default**: Typically set to a value balancing performance and resource usage (e.g., 4).
- **Behavior**: Higher values may improve throughput but increase CPU and memory pressure. Must be a positive integer.
- **Throws**: `ArgumentOutOfRangeException` if set to a non-positive value.

### `MaxBufferSizeBytes`
- **Purpose**: Defines the maximum size (in bytes) of in-memory buffers used during bulk operations.
- **Type**: `int`
- **Default**: Implementation-specific (e.g., 10 MB).
- **Behavior**: Larger buffers reduce I/O operations but increase memory consumption. Must be a positive integer.
- **Throws**: `ArgumentOutOfRangeException` if set to a non-positive value.

### `OperationTimeout`
- **Purpose**: Specifies the maximum duration allowed for a bulk operation to complete before timing out.
- **Type**: `TimeSpan`
- **Default**: Typically set to a value like `TimeSpan.FromMinutes(10)`.
- **Behavior**: Operations exceeding this duration will be aborted. Must be a positive `TimeSpan`.
- **Throws**: `ArgumentOutOfRangeException` if set to a non-positive value.

### `PublishDomainEvents`
- **Purpose**: Determines whether domain events (e.g., notifications of bulk operation progress) are published.
- **Type**: `bool`
- **Default**: `false`.
- **Behavior**: When `true`, enables event-driven monitoring of bulk operations. When `false`, suppresses event publishing.

### `EnableProgressReporting`
- **Purpose**: Controls whether progress reporting is enabled for bulk operations.
- **Type**: `bool`
- **Default**: `false`.
- **Behavior**: When `true`, progress callbacks or logs may be generated during operations. When `false`, progress reporting is disabled.

### `DefaultExportDirectory`
- **Purpose**: Specifies the default directory path for exporting bulk data (e.g., CSV or SQL files).
- **Type**: `string`
- **Default**: `null` (implementation may default to a temporary directory).
- **Behavior**: Must be a valid, writable directory path. If `null` or empty, the implementation may fall back to a default location.
- **Throws**: `DirectoryNotFoundException` if the path does not exist or is invalid.

### `BaseDatabasePath`
- **Purpose**: Defines the root directory path where tenant databases are stored.
- **Type**: `string`
- **Default**: `null` (implementation may default to a project-specific directory).
- **Behavior**: Must be a valid, writable directory path. If `null` or empty, the implementation may use a default location.
- **Throws**: `DirectoryNotFoundException` if the path does not exist or is invalid.

## Usage

### Example 1: Configuring Bulk Import for High Throughput
