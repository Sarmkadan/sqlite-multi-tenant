# ServiceCollectionExtensions

The `ServiceCollectionExtensions` class provides extension methods for configuring dependency injection and middleware in an ASP.NET Core application that uses the `sqlite-multi-tenant` library. It also contains a nested `ServiceOptions` class that exposes tunable settings for caching, HTTP timeouts, auditing, metrics, and the event bus. Use these methods to quickly integrate multi-tenant SQLite support, exception handling, event handlers, health checks, formatters, and request/response logging into your application pipeline.

## API

### `AddSqliteMultiTenantServices`
```csharp
public static IServiceCollection AddSqliteMultiTenantServices(
    this IServiceCollection services,
    Action<ServiceOptions> configureOptions = null)
```
Registers the core multi-tenant SQLite services into the dependency injection container.  
**Parameters:**  
- `services` – The `IServiceCollection` to which services are added.  
- `configureOptions` – An optional delegate to configure the `ServiceOptions` instance used by the library.  

**Returns:** The same `IServiceCollection` instance for chaining.  

**Throws:**  
- `ArgumentNullException` if `services` is `null`.  

### `AddExceptionHandling`
```csharp
public static IServiceCollection AddExceptionHandling(
    this IServiceCollection services)
```
Adds exception handling policies (e.g., global exception filters or middleware components) to the service collection.  
**Parameters:**  
- `services` – The `IServiceCollection` to modify.  

**Returns:** The same `IServiceCollection` instance.  

**Throws:**  
- `ArgumentNullException` if `services` is `null`.  

### `AddEventHandlers`
```csharp
public static IServiceCollection AddEventHandlers(
    this IServiceCollection services)
```
Registers event handler implementations (typically for domain events or integration events) into the container.  
**Parameters:**  
- `services` – The `IServiceCollection` to modify.  

**Returns:** The same `IServiceCollection` instance.  

**Throws:**  
- `ArgumentNullException` if `services` is `null`.  

### `AddHealthChecks`
```csharp
public static IServiceCollection AddHealthChecks(
    this IServiceCollection services)
```
Adds health check endpoints and services for monitoring the application’s status (e.g., database connectivity, tenant store availability).  
**Parameters:**  
- `services` – The `IServiceCollection` to modify.  

**Returns:** The same `IServiceCollection` instance.  

**Throws:**  
- `ArgumentNullException` if `services` is `null`.  

### `AddFormatters`
```csharp
public static IServiceCollection AddFormatters(
    this IServiceCollection services)
```
Configures input/output formatters (e.g., JSON, XML) used by the multi-tenant API endpoints.  
**Parameters:**  
- `services` – The `IServiceCollection` to modify.  

**Returns:** The same `IServiceCollection` instance.  

**Throws:**  
- `ArgumentNullException` if `services` is `null`.  

### `UseRequestResponseLogging`
```csharp
public static IApplicationBuilder UseRequestResponseLogging(
    this IApplicationBuilder app)
```
Adds middleware that logs incoming HTTP requests and outgoing responses. Should be placed early in the pipeline.  
**Parameters:**  
- `app` – The `IApplicationBuilder` to configure.  

**Returns:** The same `IApplicationBuilder` instance for chaining.  

**Throws:**  
- `ArgumentNullException` if `app` is `null`.  

### `ServiceOptions` (sealed class)
```csharp
public sealed class ServiceOptions
```
Configuration options for the multi-tenant SQLite services. Properties are set via the delegate passed to `AddSqliteMultiTenantServices`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxCacheItems` | `int` | `100` | Maximum number of tenant database connections or metadata entries to keep in the in-memory cache. |
| `HttpClientTimeoutSeconds` | `int` | `30` | Timeout in seconds for HTTP calls made by the library (e.g., tenant resolution via external API). |
| `EnableAuiting` | `bool` | `false` | Enables automatic auditing of data changes (creation, modification, deletion) for tenant entities. |
| `EnableMetrics` | `bool` | `false` | Enables collection of metrics (e.g., request counts, cache hit rates) exposed via the health check or metrics endpoint. |
| `EnableEventBus` | `bool` | `false` | Enables the internal event bus for publishing domain events; requires `AddEventHandlers` to be called. |

**Throws:**  
- `ArgumentOutOfRangeException` if `MaxCacheItems` is set to a value less than `1`.  
- `ArgumentOutOfRangeException` if `HttpClientTimeoutSeconds` is set to a value less than `1`.  

## Usage

### Basic setup with default options
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using SqliteMultiTenant;

var builder = WebApplication.CreateBuilder(args);

// Register all multi-tenant services with default options
builder.Services.AddSqliteMultiTenantServices();
builder.Services.AddExceptionHandling();
builder.Services.AddEventHandlers();
builder.Services.AddHealthChecks();
builder.Services.AddFormatters();

var app = builder.Build();

// Add request/response logging middleware
app.UseRequestResponseLogging();

app.Run();
```

### Custom configuration and selective features
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using SqliteMultiTenant;

var builder = WebApplication.CreateBuilder(args);

// Configure services with custom options
builder.Services.AddSqliteMultiTenantServices(options =>
{
    options.MaxCacheItems = 500;
    options.HttpClientTimeoutSeconds = 60;
    options.EnableAuiting = true;
    options.EnableMetrics = true;
    options.EnableEventBus = true; // Requires AddEventHandlers
});

// Only add the features you need
builder.Services.AddExceptionHandling();
builder.Services.AddEventHandlers(); // Required because EnableEventBus is true
builder.Services.AddHealthChecks();
// AddFormatters is omitted – the default formatters will be used

var app = builder.Build();

app.UseRequestResponseLogging();

app.Run();
```

## Notes

- **Order of middleware:** `UseRequestResponseLogging` should be placed early in the pipeline (before other middleware that may short-circuit requests) to capture all requests and responses.  
- **Thread safety:** The `ServiceOptions` class is not thread-safe; configure it only during application startup, before the service provider is built. After the provider is created, the options are read-only.  
- **Cache behavior:** When `MaxCacheItems` is reached, the least recently used tenant entry is evicted. Setting this value too low may cause frequent cache misses and degrade performance.  
- **Auditing dependency:** Enabling `EnableAuiting` requires that the database schema includes audit tables; the library will not create them automatically.  
- **Event bus:** `EnableEventBus` has no effect unless `AddEventHandlers` is also called. If the event bus is enabled but no handlers are registered, events are published but silently discarded.  
- **Health checks:** The `AddHealthChecks` method registers a default health check endpoint at `/health` (configurable via standard ASP.NET Core health check options).  
- **Exception handling:** `AddExceptionHandling` registers a global exception handler that returns structured error responses. It does not replace custom middleware; it is additive.  
- **Null arguments:** All extension methods throw `ArgumentNullException` if the receiver (`IServiceCollection` or `IApplicationBuilder`) is `null`.
