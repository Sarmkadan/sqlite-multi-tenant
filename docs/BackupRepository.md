# BackupRepository

Central repository component for managing database backup records within the sqlite-multi-tenant system. Provides CRUD operations and specialized queries for backup lifecycle management, including filtering by status, database identity, and temporal constraints.

## API

### `BackupRepository`
Constructor for the repository. Initializes the underlying data access layer for backup records.

### `async Task<List<Backup>> GetAllAsync()`
Retrieves all backup records from storage.

- **Returns**: A list of all `Backup` entities.
- **Throws**: `IOException` if the underlying storage is unavailable.

### `async Task<Backup?> GetByIdAsync(long id)`
Fetches a single backup by its unique identifier.

- **Parameters**:
  - `id` – The backup identifier.
- **Returns**: The `Backup` entity if found; otherwise, `null`.
- **Throws**: `ArgumentOutOfRangeException` if `id` is negative.

### `async Task<List<Backup>> GetByDatabaseAsync(string databaseName)`
Returns all backups associated with a specific database.

- **Parameters**:
  - `databaseName` – The name of the database.
- **Returns**: A list of `Backup` entities for the given database.
- **Throws**: `ArgumentException` if `databaseName` is null or whitespace.

### `async Task<List<Backup>> GetCompletedBackupsAsync()`
Retrieves all backups with a completed status.

- **Returns**: A list of `Backup` entities marked as completed.

### `async Task<List<Backup>> GetVerifiedBackupsAsync()`
Returns all backups that have been verified.

- **Returns**: A list of `Backup` entities with verified status.

### `async Task<List<Backup>> GetFailedBackupsAsync()`
Returns all backups that failed during creation or verification.

- **Returns**: A list of `Backup` entities with failed status.

### `async Task<Backup?> GetLatestBackupAsync(string databaseName)`
Fetches the most recent backup for a given database.

- **Parameters**:
  - `databaseName` – The name of the database.
- **Returns**: The latest `Backup` entity for the specified database, or `null` if none exist.
- **Throws**: `ArgumentException` if `databaseName` is null or whitespace.

### `async Task<Backup> AddAsync(Backup backup)`
Adds a new backup record to storage.

- **Parameters**:
  - `backup` – The `Backup` entity to add.
- **Returns**: The added `Backup` entity with updated identifiers.
- **Throws**:
  - `ArgumentNullException` if `backup` is `null`.
  - `InvalidOperationException` if the backup conflicts with existing records.

### `async Task UpdateAsync(Backup backup)`
Updates an existing backup record in storage.

- **Parameters**:
  - `backup` – The modified `Backup` entity.
- **Throws**:
  - `ArgumentNullException` if `backup` is `null`.
  - `KeyNotFoundException` if the backup does not exist.

### `async Task DeleteAsync(long id)`
Removes a backup record from storage.

- **Parameters**:
  - `id` – The identifier of the backup to remove.
- **Throws**: `KeyNotFoundException` if no backup with the given `id` exists.

### `async Task<bool> ExistsAsync(long id)`
Checks whether a backup with the specified identifier exists.

- **Parameters**:
  - `id` – The backup identifier.
- **Returns**: `true` if the backup exists; otherwise, `false`.
- **Throws**: `ArgumentOutOfRangeException` if `id` is negative.

### `async Task<int> GetCountByDatabaseAsync(string databaseName)`
Returns the total number of backups for a given database.

- **Parameters**:
  - `databaseName` – The name of the database.
- **Returns**: The count of backups associated with the database.
- **Throws**: `ArgumentException` if `databaseName` is null or whitespace.

### `async Task<List<Backup>> GetExpiredBackupsAsync(DateTime cutoff)`
Retrieves all backups older than the specified cutoff date.

- **Parameters**:
  - `cutoff` – The threshold date for expiration.
- **Returns**: A list of `Backup` entities older than `cutoff`.
- **Throws**: `ArgumentOutOfRangeException` if `cutoff` is in the future.

### `async Task<List<Backup>> GetPagedAsync(int pageIndex, int pageSize)`
Returns a paged subset of backup records.

- **Parameters**:
  - `pageIndex` – The zero-based page index.
  - `pageSize` – The number of records per page.
- **Returns**: A list of `Backup` entities for the requested page.
- **Throws**:
  - `ArgumentOutOfRangeException` if `pageIndex` or `pageSize` is negative.
  - `ArgumentException` if `pageSize` exceeds the maximum allowed page size.

## Usage
