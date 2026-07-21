# TenantIntegrityCheckResult

Represents the outcome of an integrity verification performed on a tenant's database in a multi-tenant SQLite environment. This type is used to report whether a tenant's database adheres to expected structural and content constraints after maintenance operations or during system health checks.

## API

### `TenantId`
- **Purpose**: Uniquely identifies the tenant whose integrity was checked.
- **Type**: `string`
- **Return value**: The tenant identifier as provided during system initialization.
- **Exceptions**: Never `null` or empty.

### `TenantName`
- **Purpose**: Human-readable name of the tenant, useful for logging and user-facing diagnostics.
- **Type**: `string`
- **Return value**: The tenant's display name.
- **Exceptions**: Never `null` or empty.

### `IsOk`
- **Purpose**: Indicates whether the integrity check passed without errors.
- **Type**: `bool`
- **Return value**:
  - `true` if the check completed successfully and no integrity issues were detected.
  - `false` if errors were encountered or structural problems were found.
- **Exceptions**: Never throws.

### `Error`
- **Purpose**: Contains a human-readable error message when `IsOk` is `false`.
- **Type**: `string?`
- **Return value**:
  - Non-null and non-empty when `IsOk` is `false`.
  - `null` when `IsOk` is `true`.
- **Exceptions**: Never throws.

### `IntegrityOutput`
- **Purpose**: Provides detailed output from the integrity verification tool or script, such as SQL diagnostic output or repair logs.
- **Type**: `string?`
- **Return value**:
  - Non-null when additional diagnostic information is available.
  - `null` when no such output exists.
- **Exceptions**: Never throws.

### `CheckedAt`
- **Purpose**: Timestamp indicating when the integrity check was performed.
- **Type**: `DateTime`
- **Return value**: The moment the check concluded.
- **Exceptions**: Never `default` or invalid.

## Usage
