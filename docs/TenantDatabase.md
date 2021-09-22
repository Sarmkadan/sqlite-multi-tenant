# TenantDatabase

Represents a tenant-specific SQLite database in a multi-tenant application, encapsulating metadata and lifecycle operations for tenant databases.

## API

### Properties

#### `DatabaseId`
- **Purpose**: Unique identifier for the database.
- **Type**: `string`
- **Access**: Read-only

#### `TenantId`
- **Purpose**: Identifier of the tenant this database belongs to.
- **Type**: `string`
- **Access**: Read-only

#### `Name`
- **Purpose**: Human-readable name of the database.
- **Type**: `string`
- **Access**: Read-only

#### `FilePath`
- **Purpose**: Filesystem path to the SQLite database file.
- **Type**: `string`
- **Access**: Read-only

#### `SizeBytes`
- **Purpose**: Size of the database file in bytes.
- **Type**: `long`
- **Access**: Read-only

#### `CreatedAt`
- **Purpose**: Timestamp when the database was created.
- **Type**: `DateTime`
- **Access**: Read-only

#### `UpdatedAt`
- **Purpose**: Timestamp when the database metadata was last updated.
- **Type**: `DateTime`
- **Access**: Read-only

#### `LastBackupAt`
- **Purpose**: Timestamp of the last backup, if any.
- **Type**: `DateTime?`
- **Access**: Read-only

#### `SchemaVersion`
- **Purpose**: Version of the database schema.
- **Type**: `int`
- **Access**: Read-only

#### `IsReadOnly`
- **Purpose**: Indicates whether the database is in read-only mode.
- **Type**: `bool`
- **Access**: Read-only

#### `ActiveConnectionCount`
- **Purpose**: Number of active connections to the database.
- **Type**: `int`
- **Access**: Read-only

#### `EncryptionKey`
- **Purpose**: Encryption key used for the database, if encrypted.
- **Type**: `string?`
- **Access**: Read-only

#### `RequiresEncryption`
- **Purpose**: Indicates whether the database requires encryption.
- **Type**: `bool`
- **Access**: Read-only

#### `Tenant`
- **Purpose**: Navigation property to the associated tenant.
- **Type**: `Tenant?`
- **Access**: Read-only

#### `Migrations`
- **Purpose**: Collection of schema migrations applied to the database.
- **Type**: `ICollection<Migration>`
- **Access**: Read-only

#### `Backups`
- **Purpose**: Collection of backups created for the database.
- **Type**: `ICollection<Backup>`
- **Access**: Read-only

#### `Validate`
- **Purpose**: Flag indicating whether the database should be validated.
- **Type**: `bool`
- **Access**: Read-write

### Methods

#### `UpdateLastBackupTime()`
- **Purpose**: Updates the `LastBackupAt` timestamp to the current UTC time.
- **Parameters**: None
- **Return Value**: None
- **Throws**: None

#### `UpdateSize()`
- **Purpose**: Updates the `SizeBytes` property to reflect the current size of the database file on disk.
- **Parameters**: None
- **Return Value**: None
- **Throws**: `IOException` if the file cannot be accessed or read.

## Usage
