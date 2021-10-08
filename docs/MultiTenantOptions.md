# MultiTenantOptions

The `MultiTenantOptions` class serves as the primary configuration container for the `sqlite-multi-tenant` library, defining operational constraints, security policies, and maintenance schedules for multi-tenant SQLite database instances. As a sealed class, it encapsulates settings for connection pooling limits, backup strategies, audit logging, performance monitoring, and data encryption, ensuring that each tenant operates within defined resource boundaries while maintaining data integrity and compliance standards.

## API

### `public sealed class MultiTenantOptions`
The main configuration class. It cannot be inherited. Instances are typically instantiated via object initialization syntax to define the runtime behavior of the tenant manager.

### `public string BasePath`
Gets or sets the root file system path where tenant database files are stored.
*   **Purpose**: Defines the physical location for SQLite database files.
*   **Parameters**: None (Property setter accepts a `string`).
*   **Return Value**: The configured path string.
*   **Throws**: May throw an exception at runtime if the path is invalid, inaccessible, or does not exist when the database engine attempts to initialize.

### `public int MaxConnectionsPerTenant`
Gets or sets the maximum number of concurrent database connections allowed for a single tenant.
*   **Purpose**: Prevents a single tenant from exhausting shared database resources.
*   **Parameters**: None (Property setter accepts an `int`).
*   **Return Value**: The connection limit integer.
*   **Throws**: No immediate exceptions; invalid values (e.g., negative numbers) may cause configuration validation errors during startup.

### `public int DefaultMaxConnections`
Gets or sets the default maximum number of total connections available across all tenants if not overridden by specific tenant rules.
*   **Purpose**: Sets the global pool size limit for the application.
*   **Parameters**: None (Property setter accepts an `int`).
*   **Return Value**: The global connection limit integer.

### `public int MaxBackupCount`
Gets or sets the maximum number of backup files to retain per tenant.
*   **Purpose**: Controls disk usage by limiting the history of stored backups.
*   **Parameters**: None (Property setter accepts an `int`).
*   **Return Value**: The retention count integer.

### `public TimeSpan BackupRetention`
Gets or sets the duration for which backup files are kept before being eligible for deletion.
*   **Purpose**: Defines the time-based policy for backup cleanup.
*   **Parameters**: None (Property setter accepts a `TimeSpan`).
*   **Return Value**: The retention time span.

### `public bool EnableBackupScheduling`
Gets or sets a value indicating whether automatic backup tasks are enabled.
*   **Purpose**: Toggles the background scheduler for database snapshots.
*   **Parameters**: None (Property setter accepts a `bool`).
*   **Return Value**: `true` if scheduling is active; otherwise, `false`.

### `public TimeSpan BackupInterval`
Gets or sets the frequency at which automatic backups are performed.
*   **Purpose**: Determines the time gap between consecutive scheduled backups.
*   **Parameters**: None (Property setter accepts a `TimeSpan`).
*   **Return Value**: The interval time span.
*   **Throws**: May cause logic errors if set to `TimeSpan.Zero` or negative while `EnableBackupScheduling` is true.

### `public bool EnableAuditLogging`
Gets or sets a value indicating whether tenant actions and data access are logged for audit purposes.
*   **Purpose**: Enables compliance tracking and security forensics.
*   **Parameters**: None (Property setter accepts a `bool`).
*   **Return Value**: `true` if audit logging is active; otherwise, `false`.

### `public bool EnablePerformanceMonitoring`
Gets or sets a value indicating whether runtime metrics (latency, throughput) are collected.
*   **Purpose**: Facilitates observability and bottleneck detection.
*   **Parameters**: None (Property setter accepts a `bool`).
*   **Return Value**: `true` if monitoring is active; otherwise, `false`.

### `public bool EnableDataEncryption`
Gets or sets a value indicating whether data at rest should be encrypted.
*   **Purpose**: Enforces security standards for sensitive tenant data.
*   **Parameters**: None (Property setter accepts a `bool`).
*   **Return Value**: `true` if encryption is active; otherwise, `false`.

### `public int MaxCacheSize`
Gets or sets the maximum memory size allocated for caching query results or database pages.
*   **Purpose**: Optimizes read performance while bounding memory consumption.
*   **Parameters**: None (Property setter accepts an `int`).
*   **Return Value**: The cache size limit (typically in KB or MB, depending on implementation specifics).

### `public TimeSpan DefaultCacheTTL`
Gets or sets the default time-to-live for cached entries.
*   **Purpose**: Ensures cache freshness by expiring old entries.
*   **Parameters**: None (Property setter accepts a `TimeSpan`).
*   **Return Value**: The expiration time span.

### `public int RateLimitRequestsPerMinute`
Gets or sets the maximum number of requests a tenant can issue per minute.
*   **Purpose**: Protects the system from denial-of-service scenarios or abusive tenants.
*   **Parameters**: None (Property setter accepts an `int`).
*   **Return Value**: The request limit integer.

### `public string EncryptionKeyPath`
Gets or sets the file system path to the key used for data encryption.
*   **Purpose**: Locates the cryptographic material required when `EnableDataEncryption` is true.
*   **Parameters**: None (Property setter accepts a `string`).
*   **Return Value**: The path string.
*   **Throws**: May throw `FileNotFoundException` or `UnauthorizedAccessException` during initialization if the key file is missing or unreadable.

### `public bool VerboseLogging`
Gets or sets a value indicating whether detailed diagnostic messages are emitted.
*   **Purpose**: Assists in debugging complex multi-tenant issues.
*   **Parameters**: None (Property setter accepts a `bool`).
*   **Return Value**: `true` if verbose output is enabled; otherwise, `false`.

### `public sealed class BackupOptions`
A nested sealed class containing specific configurations for the backup subsystem.

#### `public string BackupPath`
Gets or sets the directory where backup files are written.
*   **Purpose**: Separates backup storage from the primary `BasePath`.
*   **Parameters**: None (Property setter accepts a `string`).
*   **Return Value**: The backup directory path.

#### `public int MaxConcurrentBackups`
Gets or sets the maximum number of backup operations that can run simultaneously.
*   **Purpose**: Prevents I/O saturation during backup windows.
*   **Parameters**: None (Property setter accepts an `int`).
*   **Return Value**: The concurrency limit integer.

#### `public int BackupTimeoutSeconds`
Gets or sets the maximum duration a single backup operation is allowed to run before being terminated.
*   **Purpose**: Ensures hung backup processes do not block resources indefinitely.
*   **Parameters**: None (Property setter accepts an `int`).
*   **Return Value**: The timeout duration in seconds.

## Usage

### Example 1: Basic Configuration with Security and Backups
This example demonstrates initializing the options for a production environment with encryption, scheduled backups, and strict connection limits.

```csharp
using System;
using SqliteMultiTenant;

var options = new MultiTenantOptions
{
    BasePath = "/var/data/tenants",
    MaxConnectionsPerTenant = 10,
    DefaultMaxConnections = 100,
    EnableDataEncryption = true,
    EncryptionKeyPath = "/etc/keys/master.key",
    EnableBackupScheduling = true,
    BackupInterval = TimeSpan.FromHours(6),
    MaxBackupCount = 5,
    BackupRetention = TimeSpan.FromDays(30),
    EnableAuditLogging = true,
    RateLimitRequestsPerMinute = 500
};

// Configure nested backup specifics
// Note: In a real implementation, this might be assigned to a property 
// if the class structure exposes a BackupOptions instance, 
// or used directly if the library accepts this nested type contextually.
var backupConfig = new MultiTenantOptions.BackupOptions
{
    BackupPath = "/var/backups/sqlite",
    MaxConcurrentBackups = 2,
    BackupTimeoutSeconds = 300
};

// Initialize the tenant manager with these options
// var manager = new TenantManager(options); 
```

### Example 2: Development Environment with Verbose Logging
This example configures the system for a local development environment, prioritizing diagnostic output and disabling heavy security features to simplify debugging.

```csharp
using System;
using SqliteMultiTenant;

var devOptions = new MultiTenantOptions
{
    BasePath = "./dev-data",
    MaxConnectionsPerTenant = 50, // Relaxed for testing
    DefaultMaxConnections = 200,
    EnableDataEncryption = false,
    EnableBackupScheduling = false, // Manual backups only
    EnableAuditLogging = false,
    EnablePerformanceMonitoring = true,
    VerboseLogging = true,
    MaxCacheSize = 1024,
    DefaultCacheTTL = TimeSpan.FromMinutes(5),
    RateLimitRequestsPerMinute = 10000 // High limit for load testing
};

// Nested backup options for manual triggers
var devBackupConfig = new MultiTenantOptions.BackupOptions
{
    BackupPath = "./dev-backups",
    MaxConcurrentBackups = 1,
    BackupTimeoutSeconds = 60
};

// Usage context
// var manager = new TenantManager(devOptions);
```

## Notes

*   **Thread Safety**: The `MultiTenantOptions` class is a Plain Old CLR Object (POCO) with mutable properties. It is **not** thread-safe for modification. Instances should be fully configured during application startup and treated as immutable during runtime operations. Passing a partially configured instance to multiple threads may result in race conditions where settings are read inconsistently.
*   **Path Validation**: The properties `BasePath`, `EncryptionKeyPath`, and `BackupOptions.BackupPath` accept raw strings. The class does not validate file system existence upon property assignment. Validation typically occurs lazily when the `TenantManager` or underlying storage engine attempts to access these paths. Ensure paths are absolute and permissions are correct before initialization to avoid runtime `IOException`s.
*   **Logical Consistency**: The configuration does not enforce logical relationships between properties automatically. For instance, setting `EnableDataEncryption` to `true` while leaving `EncryptionKeyPath` null or empty will likely result in a failure during the cryptographic provider initialization. Similarly, `BackupInterval` should be greater than `BackupTimeoutSeconds` (converted to TimeSpan) to prevent overlapping scheduled tasks if `MaxConcurrentBackups` is set to 1.
*   **Resource Exhaustion**: Setting `MaxConnectionsPerTenant` or `MaxCacheSize` to excessively high values without regard for `DefaultMaxConnections` or system memory can lead to resource exhaustion. The sum of per-tenant limits across active tenants should ideally be reconciled against the global `DefaultMaxConnections` and hardware constraints by the consuming application logic.
*   **Sealed Hierarchy**: Both `MultiTenantOptions` and `BackupOptions` are sealed. This prevents extension via inheritance, ensuring that the configuration schema remains strict and predictable for the library's internal validation logic.
