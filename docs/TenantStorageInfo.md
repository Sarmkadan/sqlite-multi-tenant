# TenantStorageInfo

Represents storage metrics for a single tenant in a SQLite multi-tenant database, including size and page information derived from the underlying storage engine.

## API

### `TenantId`
- **Purpose**: Uniquely identifies the tenant whose storage metrics are represented.
- **Type**: `string`
- **Return value**: The tenant identifier as a non-null string.
- **Exceptions**: Never throws; always returns a valid string.

### `SizeBytes`
- **Purpose**: Reports the total storage size in bytes occupied by the tenant's database files.
- **Type**: `long`
- **Return value**: The size in bytes; always non-negative.
- **Exceptions**: Never throws; always returns a valid non-negative value.

### `PageCount`
- **Purpose**: Indicates the total number of database pages allocated for the tenant.
- **Type**: `int`
- **Return value**: The count of pages; always non-negative.
- **Exceptions**: Never throws; always returns a valid non-negative value.

### `PageSize`
- **Purpose**: Specifies the size in bytes of each database page used by the tenant.
- **Type**: `int`
- **Return value**: The page size in bytes; always positive.
- **Exceptions**: Never throws; always returns a positive value.

### `WalSizeBytes`
- **Purpose**: Provides the size in bytes of the Write-Ahead Logging (WAL) file for the tenant.
- **Type**: `long`
- **Return value**: The WAL size in bytes; always non-negative.
- **Exceptions**: Never throws; always returns a valid non-negative value.

## Usage
