# DependencyInjectionSetup

The `DependencyInjectionSetup` class serves as the central configuration entry point for the `sqlite-multi-tenant` library, providing static extension methods to register core services into the .NET dependency injection container and a fluent builder pattern for configuring multi-tenant SQLite options. It encapsulates the registration logic for API controllers, middleware, caching, event handling, formatting, validation, health checks, background workers, and integration services, ensuring that all necessary components for a multi-tenant SQLite architecture are correctly initialized and scoped within the application lifecycle.

## API

### Static Extension Methods

All methods listed below are static extension methods targeting `IServiceCollection`. They return the modified `IServiceCollection` instance to allow for method chaining.

#### `AddApiControllers`
Registers API controllers required for the multi-tenant system.
*   **Parameters**: `IServiceCollection services`
*   **Returns**: `IServiceCollection`
*   **Throws**: Throws `ArgumentNullException` if `services` is null. May throw if controller discovery fails.

#### `AddMiddlewareServices`
Registers middleware components used for request processing, tenant resolution, and pipeline management.
*   **Parameters**: `IServiceCollection services`
*   **Returns**: `IServiceCollection`
*   **Throws**: Throws `ArgumentNullException` if `services` is null.

#### `AddCachingServices`
Configures and registers caching mechanisms specific to tenant data isolation.
*   **Parameters**: `IServiceCollection services`
*   **Returns**: `IServiceCollection`
*   **Throws**: Throws `ArgumentNullException` if `services` is null.

#### `AddEventServices`
Registers the event bus and related handlers for domain events within the multi-tenant context.
*   **Parameters**: `IServiceCollection services`
*   **Returns**: `IServiceCollection`
*   **Throws**: Throws `ArgumentNullException` if `services` is null.

#### `AddFormatterServices`
Registers services responsible for data formatting and serialization specific to tenant requirements.
*   **Parameters**: `IServiceCollection services`
*   **Returns**: `IServiceCollection`
*   **Throws**: Throws `ArgumentNullException` if `services` is null.

#### `AddValidationServices`
Registers validation logic and validators for incoming tenant data and configuration models.
*   **Parameters**: `IServiceCollection services`
*   **Returns**: `IServiceCollection`
*   **Throws**: Throws `ArgumentNullException` if `services` is null.

#### `AddHealthCheckServices`
Registers health check endpoints to monitor the status of tenant databases and related services.
*   **Parameters**: `IServiceCollection services`
*   **Returns**: `IServiceCollection`
*   **Throws**: Throws `ArgumentNullException` if `services` is null.

#### `AddBackgroundWorkers`
Registers hosted background services responsible for maintenance tasks such as backups and cleanup.
*   **Parameters**: `IServiceCollection services`
*   **Returns**: `IServiceCollection`
*   **Throws**: Throws `ArgumentNullException` if `services` is null.

#### `AddIntegrationServices`
Registers external integration clients and adapters required by the multi-tenant system.
*   **Parameters**: `IServiceCollection services`
*   **Returns**: `IServiceCollection`
*   **Throws**: Throws `ArgumentNullException` if `services` is null.

#### `AddPhase2Services`
Registers services designated for the second phase of the application lifecycle or migration path.
*   **Parameters**: `IServiceCollection services`
*   **Returns**: `IServiceCollection`
*   **Throws**: Throws `ArgumentNullException` if `services` is null.

### MultiTenantOptionsBuilder

A sealed class used to fluently construct `SqliteMultiTenantOptions`.

#### Constructor: `MultiTenantOptionsBuilder`
Initializes a new instance of the builder.
*   **Parameters**: None.
*   **Returns**: A new `MultiTenantOptionsBuilder` instance.

#### `WithBackupRetention`
Configures the retention policy for database backups.
*   **Parameters**: Accepts configuration values for retention duration or count (specific type inferred from implementation context, typically `TimeSpan` or `int`).
*   **Returns**: `MultiTenantOptionsBuilder` (the current instance).
*   **Throws**: Throws `ArgumentOutOfRangeException` if the provided retention value is invalid (e.g., negative).

#### `WithMaxConnections`
Sets the maximum number of concurrent connections allowed per tenant database.
*   **Parameters**: `int maxConnections`
*   **Returns**: `MultiTenantOptionsBuilder`
*   **Throws**: Throws `ArgumentOutOfRangeException` if `maxConnections` is less than 1.

#### `WithConnectionTimeout`
Defines the timeout duration for establishing a connection to a tenant database.
*   **Parameters**: `TimeSpan timeout` (or equivalent duration type).
*   **Returns**: `MultiTenantOptionsBuilder`
*   **Throws**: Throws `ArgumentOutOfRangeException` if the timeout is negative or zero.

#### `WithEncryption`
Enables or configures encryption settings for the tenant databases.
*   **Parameters**: Configuration details for encryption (e.g., password, algorithm, or boolean flag).
*   **Returns**: `MultiTenantOptionsBuilder`
*   **Throws**: Throws `ArgumentException` if encryption parameters are invalid or missing required keys.

#### `WithBackupDirectory`
Specifies the file system path where database backups are stored.
*   **Parameters**: `string path`
*   **Returns**: `MultiTenantOptionsBuilder`
*   **Throws**: Throws `ArgumentException` if the path is null, empty, or invalid. Throws `IOException` if the directory cannot be accessed during validation.

#### `WithDatabaseDirectory`
Specifies the file system path where the primary tenant database files are stored.
*   **Parameters**: `string path`
*   **Returns**: `MultiTenantOptionsBuilder`
*   **Throws**: Throws `ArgumentException` if the path is null, empty, or invalid.

#### `WithLogging`
Configures logging verbosity and targets for the multi-tenant engine.
*   **Parameters**: Logging configuration options (e.g., `LogLevel`, logger factory).
*   **Returns**: `MultiTenantOptionsBuilder`
*   **Throws**: Throws `ArgumentNullException` if required logging configuration is null.

#### `Build`
Finalizes the configuration and returns the immutable options object.
*   **Parameters**: None.
*   **Returns**: `SqliteMultiTenantOptions`
*   **Throws**: Throws `InvalidOperationException` if mandatory properties (such as DatabaseDirectory) have not been set prior to calling this method.

## Usage

### Example 1: Basic Service Registration
This example demonstrates registering the core services required for a standard multi-tenant API setup within the `Program.cs` file.

```csharp
using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant;

var builder = WebApplication.CreateBuilder(args);

// Register core multi-tenant services
builder.Services
    .AddApiControllers()
    .AddMiddlewareServices()
    .AddCachingServices()
    .AddEventServices()
    .AddValidationServices()
    .AddHealthCheckServices();

// Register background maintenance workers
builder.Services.AddBackgroundWorkers();

var app = builder.Build();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHealthChecks("/health");
});

app.Run();
```

### Example 2: Configuring Tenant Options
This example illustrates the fluent configuration of SQLite-specific options using the `MultiTenantOptionsBuilder`, including setting paths, limits, and retention policies.

```csharp
using SqliteMultiTenant;

// Configure options for the multi-tenant engine
var options = new MultiTenantOptionsBuilder()
    .WithDatabaseDirectory("/var/data/tenants")
    .WithBackupDirectory("/var/backups/tenants")
    .WithMaxConnections(50)
    .WithConnectionTimeout(TimeSpan.FromSeconds(30))
    .WithBackupRetention(TimeSpan.FromDays(7))
    .WithEncryption(true)
    .WithLogging(LogLevel.Information)
    .Build();

// The 'options' object can now be passed to the service initialization 
// or registered as a singleton configuration object.
```

## Notes

*   **Thread Safety**: The `MultiTenantOptionsBuilder` is not thread-safe. Instances should not be shared across multiple threads during the configuration phase. However, the resulting `SqliteMultiTenantOptions` object returned by `Build` is immutable and safe for concurrent read access by multiple threads.
*   **Initialization Order**: While the static extension methods for `IServiceCollection` can generally be called in any order, it is recommended to register foundational services (like `AddMiddlewareServices` and `AddCachingServices`) before dependent services (like `AddApiControllers`) to ensure resolution consistency during application startup.
*   **Path Validation**: Methods `WithDatabaseDirectory` and `WithBackupDirectory` perform immediate validation on the provided strings. If the application runs in an environment with restricted file system permissions, these methods may throw exceptions at configuration time rather than at runtime. Ensure the application identity has write access to the specified directories before calling `Build`.
*   **Mandatory Configuration**: Calling `Build` without setting essential properties such as `WithDatabaseDirectory` will result in an `InvalidOperationException`. All path and connection limit configurations must be defined before finalizing the options object.
*   **Resource Limits**: The `WithMaxConnections` parameter directly influences the SQLite connection pool size. Setting this value too low may cause connection timeouts under load, while setting it excessively high may exhaust operating system file handle limits.
