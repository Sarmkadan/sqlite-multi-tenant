# ServiceConfiguration

`ServiceConfiguration` is a static class that provides configuration options for the SQLite Multi-Tenant library. It exposes properties to control database connection behavior, encryption, logging, and backup settings, as well as extension methods for registering the multi-tenant service with an `IServiceCollection`.

## API

### `AddSqliteMultiTenant(IServiceCollection services, Action<ServiceConfiguration> configure = null)`
Registers the SQLite multi-tenant services with the dependency injection container.

- **services**: The `IServiceCollection` to which the services are added.
- **configure**: Optional action to configure the `ServiceConfiguration` instance.
- **Returns**: The `IServiceCollection` for method chaining.
- **Throws**: `ArgumentNullException` if `services` is `null`.

### `AddSqliteMultiTenant(IServiceCollection services, ServiceConfiguration configuration)`
Registers the SQLite multi-tenant services with the dependency injection container using a pre-configured `ServiceConfiguration` instance.

- **services**: The `IServiceCollection` to which the services are added.
- **configuration**: The `ServiceConfiguration` instance containing the settings.
- **Returns**: The `IServiceCollection` for method chaining.
- **Throws**: `ArgumentNullException` if either `services` or `configuration` is `null`.

### `MaxConnections`
Gets or sets the maximum number of concurrent database connections allowed. Must be a positive integer.

- **Value**: The maximum number of connections.
- **Throws**: `ArgumentOutOfRangeException` if set to a value less than or equal to zero.

### `ConnectionTimeoutSeconds`
Gets or sets the connection timeout in seconds. Must be a non-negative integer.

- **Value**: The timeout in seconds.
- **Throws**: `ArgumentOutOfRangeException` if set to a negative value.

### `BackupRetentionDays`
Gets or sets the number of days to retain database backups. Must be a non-negative integer.

- **Value**: The retention period in days.
- **Throws**: `ArgumentOutOfRangeException` if set to a negative value.

### `EnableEncryption`
Gets or sets a value indicating whether database encryption is enabled.

- **Value**: `true` to enable encryption; otherwise, `false`.

### `BackupDirectory`
Gets or sets the directory where database backups are stored. Must be a valid, writable path.

- **Value**: The backup directory path.
- **Throws**: `ArgumentException` if the path is invalid or unwritable.

### `DatabaseDirectory`
Gets or sets the directory where tenant databases are stored. Must be a valid, writable path.

- **Value**: The database directory path.
- **Throws**: `ArgumentException` if the path is invalid or unwritable.

### `EnableLogging`
Gets or sets a value indicating whether detailed logging is enabled.

- **Value**: `true` to enable logging; otherwise, `false`.

## Usage

### Basic Configuration
