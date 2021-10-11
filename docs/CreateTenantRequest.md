# CreateTenantRequest

Represents a request to create a new tenant in a multi-tenant SQLite database system. Contains essential properties required to initialize a tenant, including identifying and contact information.

## API

### Properties

#### `Name`
- **Purpose**: Specifies the human-readable name of the tenant.
- **Type**: `string`
- **Constraints**: Must not be null or empty. Maximum length enforced by the system.
- **Throws**: `ArgumentException` if the value is null or empty.

#### `Description`
- **Purpose**: Provides a brief description of the tenant’s purpose or scope.
- **Type**: `string`
- **Constraints**: Optional. If provided, maximum length enforced by the system.

#### `ContactEmail`
- **Purpose**: Designates the primary contact email for administrative or support purposes.
- **Type**: `string`
- **Constraints**: Must be a valid email format if provided. Maximum length enforced by the system.
- **Throws**: `ArgumentException` if the value is not a valid email format.

---

# UpdateTenantRequest

Represents a request to update an existing tenant’s metadata. Used to modify tenant properties such as name, description, or contact information.

## API

### Properties

#### `Name`
- **Purpose**: Specifies the updated human-readable name of the tenant.
- **Type**: `string`
- **Constraints**: Must not be null or empty. Maximum length enforced by the system.
- **Throws**: `ArgumentException` if the value is null or empty.

#### `Description`
- **Purpose**: Provides an updated brief description of the tenant’s purpose or scope.
- **Type**: `string`
- **Constraints**: Optional. If provided, maximum length enforced by the system.

#### `ContactEmail`
- **Purpose**: Updates the primary contact email for administrative or support purposes.
- **Type**: `string`
- **Constraints**: Must be a valid email format if provided. Maximum length enforced by the system.
- **Throws**: `ArgumentException` if the value is not a valid email format.

---

# CreateMigrationRequest

Represents a request to create a new database migration script. Used to define schema changes that can be applied or rolled back in a controlled manner.

## API

### Properties

#### `DatabaseId`
- **Purpose**: Identifies the target database for the migration.
- **Type**: `string`
- **Constraints**: Must not be null or empty. Must correspond to an existing database in the system.
- **Throws**: `ArgumentException` if the value is null or empty, or if the database does not exist.

#### `Version`
- **Purpose**: Specifies the semantic version of the migration (e.g., "1.0.0").
- **Type**: `string`
- **Constraints**: Must follow semantic versioning format. Must be unique within the target database.
- **Throws**: `ArgumentException` if the format is invalid or the version already exists.

#### `Name`
- **Purpose**: Provides a human-readable name for the migration.
- **Type**: `string`
- **Constraints**: Optional. Maximum length enforced by the system.

#### `UpScript`
- **Purpose**: Contains the SQL script to apply the migration (e.g., schema changes).
- **Type**: `string`
- **Constraints**: Must not be null or empty. Must be valid SQL syntax for the target database.
- **Throws**: `ArgumentException` if the script is null, empty, or syntactically invalid.

#### `DownScript`
- **Purpose**: Contains the SQL script to roll back the migration (e.g., revert schema changes).
- **Type**: `string`
- **Constraints**: Optional. If provided, must be valid SQL syntax for the target database.
- **Throws**: `ArgumentException` if the script is non-empty but syntactically invalid.

---

# QueryMigrationsRequest

Represents a request to query migration history for a specific database. Used to retrieve applied or pending migrations with optional filtering and pagination.

## API

### Properties

#### `DatabaseId`
- **Purpose**: Identifies the target database for the query.
- **Type**: `string`
- **Constraints**: Must not be null or empty. Must correspond to an existing database in the system.
- **Throws**: `ArgumentException` if the value is null or empty, or if the database does not exist.

#### `Status`
- **Purpose**: Filters migrations by their current status (e.g., "Applied", "Pending").
- **Type**: `string`
- **Constraints**: Optional. If provided, must be a valid status value recognized by the system.
- **Throws**: `ArgumentException` if the value is not a recognized status.

#### `Limit`
- **Purpose**: Limits the number of results returned.
- **Type**: `int`
- **Constraints**: Must be a positive integer. Defaults to system-defined maximum if not specified.
- **Throws**: `ArgumentOutOfRangeException` if the value is negative.

#### `Offset`
- **Purpose**: Specifies the number of results to skip before returning results.
- **Type**: `int`
- **Constraints**: Must be a non-negative integer.
- **Throws**: `ArgumentOutOfRangeException` if the value is negative.

---

# RestoreBackupRequest

Represents a request to restore a database from a backup. Used to initiate recovery of a tenant’s database to a previous state.

## API

*(No public members documented.)*
