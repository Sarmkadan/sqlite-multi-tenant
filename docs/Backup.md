# Backup

Represents a backup of a tenant database in the sqlite-multi-tenant system, including metadata about its creation, status, verification, and storage characteristics.

## API

### `BackupId`
Unique identifier for the backup. Used as a primary key in the backup catalog.

### `DatabaseId`
Identifier of the tenant database this backup belongs to.

### `BackupPath`
Full filesystem path where the backup archive is stored.

### `BackupType`
Type of backup performed (e.g., full, incremental). See `BackupType` enum.

### `Status`
Current lifecycle status of the backup. See `BackupStatus` enum.

### `CreatedAt`
Timestamp when the backup process was initiated.

### `CompletedAt`
Timestamp when the backup process finished successfully. `null` if incomplete.

### `VerifiedAt`
Timestamp when the backup was verified for integrity. `null` if unverified.

### `SizeBytes`
Size of the backup archive in bytes.

### `OriginalSizeBytes`
Size of the original database at the time of backup in bytes.

### `CompressionRatio`
Compression ratio achieved during backup, calculated as `(OriginalSizeBytes - SizeBytes) / OriginalSizeBytes * 100`. Integer percentage.

### `CreatedBy`
Identifier of the user or system that initiated the backup. `null` if system-generated.

### `VerifiedBy`
Identifier of the user or system that verified the backup. `null` if unverified.

### `ErrorMessage`
Human-readable error message if the backup failed. `null` if successful.

### `DurationMs`
Total duration of the backup process in milliseconds.

### `IsEncrypted`
Indicates whether the backup archive is encrypted.

### `IsVerified`
Indicates whether the backup has been verified for integrity.

### `ExpiresAt`
Timestamp when the backup is eligible for automatic deletion. `null` if no expiration.

### `Tags`
Optional set of tags for categorizing or filtering backups. `null` if none.

## Usage
