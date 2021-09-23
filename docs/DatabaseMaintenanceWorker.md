# DatabaseMaintenanceWorker

Background service that performs periodic SQLite database maintenance tasks such as vacuuming, analyzing, and reindexing to optimize performance and reclaim space. It runs as a long-lived background worker with configurable intervals and parallelism.

## API

### `DatabaseMaintenanceWorker(DatabaseMaintenanceOptions options)`

Constructs a new instance of the maintenance worker with the specified options.

- **Parameters**
  - `options` – Configuration for maintenance operations including enable flags, intervals, and parallelism settings.
- **Throws**
  - `ArgumentNullException` – If `options` is `null`.

### `DatabaseMaintenanceOptions`

Configuration class for controlling which maintenance operations are performed and how often.

#### `EnableVacuum`

Gets or sets a value indicating whether the `VACUUM` command should be executed to reclaim space and defragment the database.

- **Type:** `bool`
- **Default:** `true`

#### `EnableAnalyze`

Gets or sets a value indicating whether the `ANALYZE` command should be executed to update statistics used by the query planner.

- **Type:** `bool`
- **Default:** `true`

#### `EnableReindex`

Gets or sets a value indicating whether the `REINDEX` command should be executed to rebuild indexes for improved performance.

- **Type:** `bool`
- **Default:** `true`

#### `IntervalHours`

Gets or sets the interval in hours between maintenance runs.

- **Type:** `int`
- **Default:** `24`
- **Constraints:** Must be greater than `0`.

#### `TimeoutSeconds`

Gets or sets the maximum time in seconds allowed for each maintenance operation before it is aborted.

- **Type:** `int`
- **Default:** `3600` (1 hour)
- **Constraints:** Must be greater than `0`.

#### `DegreeOfParallelism`

Gets or sets the maximum number of parallel operations to perform during maintenance.

- **Type:** `int`
- **Default:** `1`
- **Constraints:** Must be greater than `0`.

## Usage

### Basic Setup in ASP.NET Core
