# MigrationExtensions

Utility class providing extension methods for working with `Migration` objects and related multi-tenant SQLite migration tracking. These methods simplify common tasks such as calculating migration ages, formatting execution metrics, and retrieving status summaries.

## API

### `IsTerminal(MigrationStatus status)`

Determines whether the given migration status represents a terminal state (i.e., a state from which no further transitions are possible).

- **Parameters**
  - `status`: The `MigrationStatus` value to evaluate.
- **Return Value**
  - `true` if the status is `Completed`, `Failed`, or `RolledBack`; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `status` is outside the defined enum range.

---

### `GetAgeInDays(Migration migration)`

Calculates the age of a migration in days, based on its `CreatedAt` timestamp.

- **Parameters**
  - `migration`: The `Migration` instance whose age is to be calculated.
- **Return Value**
  - A `double` representing the number of days since `CreatedAt` (UTC).
- **Exceptions**
  - Throws `ArgumentNullException` if `migration` is `null`.
  - Throws `InvalidOperationException` if `CreatedAt` is not set.

---

### `GetExecutionDuration(Migration migration)`

Formats the execution duration of a migration as a human-readable string (e.g., "2.5s", "1m 30s").

- **Parameters**
  - `migration`: The `Migration` instance whose duration is to be formatted.
- **Return Value**
  - A `string` representing the formatted duration.
- **Exceptions**
  - Throws `ArgumentNullException` if `migration` is `null`.
  - Throws `InvalidOperationException` if `TotalExecutionTimeMs` is negative.

---
### `GetStatusDisplay(MigrationStatus status)`

Maps a `MigrationStatus` to a user-friendly display string (e.g., "Completed", "Pending").

- **Parameters**
  - `status`: The `MigrationStatus` to display.
- **Return Value**
  - A `string` representing the display name of the status.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `status` is outside the defined enum range.

---
### `GetStatusCounts(IEnumerable<Migration> migrations)`

Aggregates migration counts by status across a collection of migrations.

- **Parameters**
  - `migrations`: The collection of `Migration` instances to analyze.
- **Return Value**
  - An `IReadOnlyDictionary<MigrationStatus, int>` mapping each status to its count.
- **Exceptions**
  - Throws `ArgumentNullException` if `migrations` is `null`.

---
### `GetPendingMigrations(IEnumerable<Migration> migrations)`

Filters a collection of migrations to return only those in a pending state (i.e., not terminal).

- **Parameters**
  - `migrations`: The collection of `Migration` instances to filter.
- **Return Value**
  - An `IEnumerable<Migration>` containing only pending migrations.
- **Exceptions**
  - Throws `ArgumentNullException` if `migrations` is `null`.

---
### `GetTotalExecutionTimeMs(IEnumerable<Migration> migrations)`

Sums the total execution time (in milliseconds) across a collection of migrations.

- **Parameters**
  - `migrations`: The collection of `Migration` instances to sum.
- **Return Value**
  - A `long` representing the total execution time in milliseconds.
- **Exceptions**
  - Throws `ArgumentNullException` if `migrations` is `null`.
  - Throws `OverflowException` if the sum exceeds `long.MaxValue`.

---
### `GetAverageExecutionTimeMs(IEnumerable<Migration> migrations)`

Calculates the average execution time (in milliseconds) across a collection of migrations.

- **Parameters**
  - `migrations`: The collection of `Migration` instances to average.
- **Return Value**
  - A `double` representing the average execution time in milliseconds.
- **Exceptions**
  - Throws `ArgumentNullException` if `migrations` is `null`.
  - Throws `InvalidOperationException` if the collection is empty.

---
### `GetRollbackableMigrations(IEnumerable<Migration> migrations)`

Filters a collection of migrations to return only those that are rollbackable (i.e., in `Completed` state).

- **Parameters**
  - `migrations`: The collection of `Migration` instances to filter.
- **Return Value**
  - An `IEnumerable<Migration>` containing only rollbackable migrations.
- **Exceptions**
  - Throws `ArgumentNullException` if `migrations` is `null`.

---
### `GetDatabaseName(Migration migration)`

Retrieves the name of the database associated with a migration.

- **Parameters**
  - `migration`: The `Migration` instance whose database name is to be retrieved.
- **Return Value**
  - A `string` representing the database name.
- **Exceptions**
  - Throws `ArgumentNullException` if `migration` is `null`.

---
### `GetFormattedCreatedAt(Migration migration)`

Formats the `CreatedAt` timestamp of a migration as an ISO 8601 string (e.g., "2024-05-20T14:30:00Z").

- **Parameters**
  - `migration`: The `Migration` instance whose timestamp is to be formatted.
- **Return Value**
  - A `string` representing the formatted timestamp.
- **Exceptions**
  - Throws `ArgumentNullException` if `migration` is `null`.
  - Throws `InvalidOperationException` if `CreatedAt` is not set.

## Usage

### Example 1: Listing pending migrations with status counts
