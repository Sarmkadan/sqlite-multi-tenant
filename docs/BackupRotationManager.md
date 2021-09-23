# BackupRotationManager

Centralizes the logic for enforcing backup retention policies, including age-based and count-based deletion, and provides verification and statistics for backup sets.

## API

### `BackupRotationManager`
Sealed class that orchestrates backup rotation according to a configurable policy.

### `BackupRotationManager()`
Constructs a new instance with default retention settings (MaxBackupAge = 30 days, MaxBackupCount = 10, MaxDiskUsage = 0).

### `async Task<BackupRotationResult> RotateBackupsAsync()`
Initiates the rotation process: verifies all backups, deletes those exceeding age or count limits, and returns a summary of actions taken.

- **Return value**: `BackupRotationResult` containing counts of total, remaining, and deleted backups, along with timestamp and any error.
- **Exceptions**: Throws `IOException` or `UnauthorizedAccessException` if file system operations fail.

### `async Task<List<BackupVerificationResult>> VerifyBackupsAsync()`
Scans the backup directory and returns a list of verification results for each backup file found.

- **Return value**: `List<BackupVerificationResult>` ordered by file path; each entry includes the file path and any verification error.
- **Exceptions**: Throws `DirectoryNotFoundException` if the backup directory does not exist.

### `long EstimateBackupDiskUsage()`
Calculates the total size in bytes of all backup files in the configured backup directory.

- **Return value**: Total disk usage as a `long`; returns 0 if the directory is empty or does not exist.

### `BackupStatistics GetBackupStatistics()`
Returns a snapshot of current backup set statistics including total count, oldest backup age, and total disk usage.

- **Return value**: `BackupStatistics` with properties: `TotalBackups`, `OldestBackupAge`, `TotalDiskUsage`.

### `BackupRotationPolicy`
Immutable policy defining retention rules.

- **`TimeSpan MaxBackupAge`**: Maximum allowed age of a backup before it becomes eligible for deletion. Default: 30 days.
- **`int MaxBackupCount`**: Maximum number of backups to retain regardless of age. Default: 10.
- **`long MaxDiskUsage`**: Maximum allowed total disk usage in bytes; 0 means unlimited. Default: 0.

### `BackupRotationResult`
Represents the outcome of a rotation operation.

- **`bool IsSuccessful`**: Indicates whether the rotation completed without errors.
- **`int TotalBackups`**: Number of backups present before rotation.
- **`int RemainingBackups`**: Number of backups remaining after rotation.
- **`int DeletedByAge`**: Count of backups deleted due to exceeding `MaxBackupAge`.
- **`int DeletedByCount`**: Count of backups deleted to stay within `MaxBackupCount`.
- **`DateTime ExecutedAt`**: Timestamp when rotation was executed.
- **`string Error`**: Non-null if rotation failed; contains the exception message.

### `BackupVerificationResult`
Represents the result of verifying a single backup file.

- **`string FilePath`**: Absolute path to the backup file that was verified.
- **Implicit**: Any non-null error message indicates verification failure.

## Usage
