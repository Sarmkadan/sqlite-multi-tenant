# SqliteMultiTenant

A multi-tenant data layer for SQLite that supports two isolation strategies:
a dedicated database file per tenant (connection-per-tenant) or a single shared
database where every table carries a `TenantId` discriminator (shared-schema).

## Quickstart

The 30-line sample below provisions two tenants in connection-per-tenant mode,
writes a row for each, and shows that neither tenant can read the other's data.

```csharp
using System.Data.SQLite;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Database;

// One physical file per tenant == hard isolation boundary.
static string Conn(string tenant) => $"Data Source={tenant}.db;Version=3;";

using var connections = new ConnectionManager(NullLogger<ConnectionManager>.Instance);

foreach (var tenant in new[] { "acme", "globex" })
{
    await using var conn = await connections.GetConnectionAsync(tenant, Conn(tenant));
    using var create = conn.CreateCommand();
    create.CommandText = "CREATE TABLE IF NOT EXISTS Invoices (Id INTEGER PRIMARY KEY, Note TEXT);";
    await create.ExecuteNonQueryAsync();

    using var insert = conn.CreateCommand();
    insert.CommandText = "INSERT INTO Invoices (Id, Note) VALUES (1, @note);";
    insert.Parameters.AddWithValue("@note", $"{tenant}-private");
    await insert.ExecuteNonQueryAsync();
}

// acme's connection can only ever see acme's file.
await using var acme = await connections.GetConnectionAsync("acme", Conn("acme"));
using var read = acme.CreateCommand();
read.CommandText = "SELECT Note FROM Invoices";
Console.WriteLine(await read.ExecuteScalarAsync()); // -> acme-private (never globex-private)
```

For shared-schema mode, keep one connection string and add `WHERE TenantId = @tid`
to every read, write, and delete. See `tests/.../TenantIsolationEnforcementTests.cs`
for executable proof of both strategies, and `BackupRestoreRoundTripTests.cs` for
the backup/restore cycle.

## Choosing an isolation strategy

| Concern | Connection-per-tenant | Shared-schema |
| --- | --- | --- |
| Isolation guarantee | Hard - physical file boundary, no query can cross it | Soft - depends on every query carrying `TenantId` |
| Blast radius of a bad query | Single tenant | All tenants |
| Per-tenant backup / restore | Trivial (copy one file) | Requires filtered export |
| Per-tenant encryption keys | Natural (one key per file) | Not possible per row |
| Noisy-neighbour isolation | Strong (separate files/locks) | Weak (shared write lock) |
| Number of tenants that scale well | Tens to low thousands | Thousands to millions |
| Cross-tenant reporting | Hard (must attach/union files) | Easy (single query) |
| Schema migrations | Run N times, once per file | Run once |
| Open file-handle / connection cost | Grows with tenant count | Constant |
| Best fit | Regulated data, few large tenants, strict isolation | Many small tenants, shared analytics |

Rule of thumb: default to connection-per-tenant when isolation or per-tenant
backup/encryption matters; reach for shared-schema when you have a very large
number of small tenants or need cheap cross-tenant queries.

## EventBusImpl

The `EventBusImpl` class provides a production-grade event bus implementation that supports asynchronous event handling with priority-based subscriber ordering. It maintains an event history for monitoring and debugging purposes.

### Usage Example

```csharp
using SqliteMultiTenant.Events;
using Microsoft.Extensions.Logging;

// Create an event bus instance
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<EventBusImpl>();
var eventBus = new EventBusImpl(logger);

// Subscribe to an event type
await eventBus.Subscribe<MyCustomEvent>(async (ev) => {
    Console.WriteLine($"Received event: {ev.Id}");
    // Handle the event
});

// Publish an event
var customEvent = new MyCustomEvent { Id = Guid.NewGuid().ToString(), Data = "test" };
await eventBus.PublishAsync(customEvent);

// Get event history
var history = eventBus.GetEventHistory();

// Get event statistics
var stats = eventBus.GetEventStatistics();

// Clear event history
eventBus.ClearHistory();

// Dispose the event bus
eventBus.Dispose();
```

## IDomainEventHandler

`IDomainEventHandler<T>` defines a contract for handling domain events of a specific type. Implementations receive a concrete event instance and perform asynchronous processing such as logging, notifying external systems, or cleaning up resources. The interface exposes a single method, `HandleAsync`, which returns a `Task` that completes when the handling logic finishes.

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Integration; // Adjust namespace if different

// Set up a logger for the handler
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<TenantCreatedEventHandler>();

// Assume a concrete webhook service implementation is available
var webhookService = new WebhookService(/* any required dependencies */);

// Create the handler instance
var handler = new TenantCreatedEventHandler(logger, webhookService);

// Create a tenant‑created notification event
var @event = new TenantCreatedNotificationEvent
{
    TenantId = "tenant-123",
    TenantName = "Acme Corp",
    TenantDescription = "Demo tenant for testing"
};

// Handle the event asynchronously
await handler.HandleAsync(@event);
```

## IHttpClientService

The `IHttpClientService` interface provides a resilient HTTP client wrapper for making safe HTTP requests with built-in retry logic, timeout handling, and structured logging. It simplifies integration with external services and webhooks by handling common HTTP concerns like transient error retries, request timeouts, and response deserialization.


### Usage Example

```csharp
using SqliteMultiTenant.Integration;
using Microsoft.Extensions.Logging;
using System.Net.Http;

// Create a logger and HttpClient
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<HttpClientService>();
var httpClient = new HttpClient();

// Configure options (optional)
var options = new HttpClientOptions
{
    TimeoutSeconds = 60,
    MaxRetries = 5,
    EnableCompression = true,
    EnableConnectionPooling = true
};

// Create the HTTP client service
var httpClientService = new HttpClientService(httpClient, logger, options);

// Make a GET request to fetch JSON data
var userData = await httpClientService.GetAsync<Dictionary<string, object>>(
    "https://api.example.com/users/123"
);
Console.WriteLine($"User data: {userData["name"]}");

// Make a POST request with JSON body and get typed response
var newUser = new { name = "John Doe", email = "john@example.com" };
var createdUser = await httpClientService.PostAsync<Dictionary<string, object>>(
    "https://api.example.com/users",
    newUser,
    new Dictionary<string, string> { { "Authorization", "Bearer token123" } }
);
Console.WriteLine($"Created user ID: {createdUser["id"]}");

// Send a custom HTTP request
var response = await httpClientService.SendAsync(
    HttpMethod.Put,
    "https://api.example.com/users/123",
    "{\"status\": \"active\"}"
);
response.EnsureSuccessStatusCode();
```

## IWebhookHandler

The `IWebhookHandler` interface provides a contract for subscribing to domain events and delivering them to external webhook endpoints. It manages webhook subscriptions, event delivery attempts, and retry logic for failed deliveries. Implementations handle registration, unregistration, and asynchronous delivery of events to configured webhook URLs.

### Usage Example

```csharp
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Integration;
using Microsoft.Extensions.Logging;
using System.Net.Http;

// Create a logger and HTTP client
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<WebhookHandler>();
var httpClient = new HttpClient();

// Create the webhook handler with HTTP client
var webhookHandler = new WebhookHandler(httpClient, logger);

// Register a new webhook subscription
var subscription = new WebhookHandlerSubscription
{
    WebhookId = Guid.NewGuid().ToString(),
    Url = "https://webhook.site/12345",
    EventType = "TenantCreatedNotificationEvent",
    Enabled = true,
    Headers = new Dictionary<string, string>
    {
        { "X-Api-Key", "secret-key-123" },
        { "Content-Type", "application/json" }
    },
    CreatedAt = DateTime.UtcNow
};

await webhookHandler.RegisterAsync(subscription);

// Create a domain event to deliver
var tenantEvent = new TenantCreatedNotificationEvent
{
    TenantId = "tenant-123",
    TenantName = "Acme Corp",
    TenantDescription = "Demo tenant for testing",
    EventId = Guid.NewGuid().ToString(),
    OccurredAt = DateTime.UtcNow
};

// Deliver the event to the registered webhook
var delivery = new WebhookDelivery
{
    DeliveryId = Guid.NewGuid().ToString(),
    WebhookId = subscription.WebhookId,
    Url = subscription.Url,
    Event = tenantEvent,
    Headers = subscription.Headers,
    RetryCount = 0,
    MaxRetries = 3
};

await webhookHandler.DeliverAsync(delivery);

// Unregister the webhook when no longer needed
await webhookHandler.UnregisterAsync(subscription.WebhookId);
```

## MultiTenantHttpClientFactory

The `MultiTenantHttpClientFactory` class creates and manages HTTP clients with tenant-aware headers and configuration. It provides both direct client creation and a fluent builder pattern for configuring HTTP clients with tenant-specific settings such as API keys, timeouts, base addresses, and custom headers. Clients are cached for reuse across requests to improve performance.

### Usage Example

```csharp
using SqliteMultiTenant.Integration;
using Microsoft.Extensions.Logging;
using System.Net.Http;

// Create a logger and factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<MultiTenantHttpClientFactory>();
var factory = new MultiTenantHttpClientFactory(logger);

// Create a client with tenant context (creates and caches a new client)
var client = factory.CreateClientForTenant(
    tenantId: "tenant-123",
    apiKey: "your-api-key-here",
    timeoutSeconds: 60,
    baseAddress: "https://api.example.com"
);

// Make an authenticated request to an external API
var response = await client.GetAsync("/users");
response.EnsureSuccessStatusCode();
var content = await response.Content.ReadAsStringAsync();

// Get a cached client by tenant ID
var cachedClient = factory.GetCachedClient("tenant-123");

// Invalidate a specific tenant's client (useful when tenant config changes)
factory.InvalidateClient("tenant-123");

// Clear all cached clients when shutting down the application
factory.ClearCache();

// Dispose the factory (automatically clears cache)
factory.Dispose();
```

### Using the TenantHttpClientBuilder

```csharp
using SqliteMultiTenant.Integration;
using Microsoft.Extensions.Logging;

// Create a logger
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<MultiTenantHttpClientFactory>();
var factory = new MultiTenantHttpClientFactory(logger);

// Use the fluent builder pattern to configure a client
var client = new TenantHttpClientBuilder()
    .ForTenant("tenant-456")
    .WithApiKey("builder-api-key-123")
    .WithTimeout(120)
    .WithBaseAddress("https://api.another-service.com")
    .AddHeader("X-Custom-Header", "custom-value")
    .Build();

// Make requests with the configured client
var response = await client.GetAsync("/data");
response.EnsureSuccessStatusCode();
```

## WebhookService

The `WebhookService` class manages webhook subscriptions and asynchronous event delivery to external endpoints. It supports event filtering, retry logic for failed deliveries, and automatic deactivation of webhooks after repeated failures. The service handles registration, unregistration, and delivery of events with configurable headers and optional HMAC signature verification.

### Usage Example

```csharp
using SqliteMultiTenant.Integration;
using Microsoft.Extensions.Logging;

// Create a logger for the webhook service
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<WebhookService>();

// Create the webhook service instance
var webhookService = new WebhookService(logger);

// Subscribe to a specific event type
var subscriptionId = await webhookService.SubscribeAsync(
    eventType: "TenantCreatedNotificationEvent",
    webhookUrl: "https://webhook.site/12345",
    headers: new Dictionary<string, string>
    {
        { "X-Api-Key", "your-secret-key-here" },
        { "Content-Type", "application/json" }
    },
    secret: "webhook-secret-123"
);
Console.WriteLine($"Webhook subscription created with ID: {subscriptionId}");

// Get all active subscriptions for an event type
var subscriptions = await webhookService.GetSubscriptionsAsync("TenantCreatedNotificationEvent");
foreach (var subscription in subscriptions)
{
    Console.WriteLine($"Subscription: {subscription.Id} -> {subscription.WebhookUrl}");
}

// Trigger webhooks for an event (delivers to all registered subscribers)
var tenantEvent = new TenantCreatedNotificationEvent
{
    TenantId = "tenant-123",
    TenantName = "Acme Corp",
    TenantDescription = "Demo tenant for testing",
    EventId = Guid.NewGuid().ToString(),
    OccurredAt = DateTime.UtcNow
};
await webhookService.TriggerWebhooksAsync("TenantCreatedNotificationEvent", tenantEvent);

// Unsubscribe when no longer needed
var unsubscribed = await webhookService.UnsubscribeAsync(subscriptionId);
Console.WriteLine($"Unsubscription successful: {unsubscribed}");
```

## IEventPublisher

The `IEventPublisher` interface provides a mechanism for publishing domain events and managing event handlers. It supports both synchronous and asynchronous event handling, with built-in logging and error resilience. The `EventPublisher` class implements this interface and manages a registry of event handlers.

### Usage Example

```csharp
using SqliteMultiTenant.Events;
using Microsoft.Extensions.Logging;

// Create a logger and event publisher
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<EventPublisher>();
var eventPublisher = new EventPublisher(logger);

// Subscribe a logging handler for MyCustomEvent
eventPublisher.Subscribe<MyCustomEvent>(new LoggingEventHandler<MyCustomEvent>(logger));

// Create and publish a custom event
var myEvent = new MyCustomEvent
{
    EventId = Guid.NewGuid().ToString(),
    EventType = "MyCustomEvent",
    OccurredAt = DateTime.UtcNow
};

await eventPublisher.PublishAsync(myEvent);

// Check how many handlers are registered for this event type
int handlerCount = eventPublisher.GetHandlerCount<MyCustomEvent>();
Console.WriteLine($"Registered handlers: {handlerCount}");
```

## HttpClientWrapper

The `HttpClientWrapper` class provides a resilient wrapper around `HttpClient` to handle HTTP requests with automatic retry logic, exponential backoff, and structured logging. It simplifies interacting with external APIs by providing high-level typed methods for GET, POST, PUT, and DELETE operations, while ensuring robust error handling.

### Usage Example

```csharp
using SqliteMultiTenant.Integration;
using Microsoft.Extensions.Logging;
using System.Net.Http;

// Create a logger and an HttpClient instance
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<HttpClientWrapper>();
var httpClient = new HttpClient();

// Create the wrapper instance
var wrapper = new HttpClientWrapper(httpClient, logger, maxRetries: 3, retryDelayMs: 1000);

// Configure request headers
wrapper.AddDefaultHeader("X-Custom-Header", "Value");
wrapper.SetBearerToken("my-secure-token");

// Perform a typed GET request
var data = await wrapper.GetAsync<Dictionary<string, string>>("https://api.example.com/data");

// Perform a typed POST request
var payload = new { Key = "Value" };
var result = await wrapper.PostAsync<Dictionary<string, string>>("https://api.example.com/post", payload);

// Perform a PUT request
bool putSuccess = await wrapper.PutAsync("https://api.example.com/put", payload);


## CliApplication

The `CliApplication` class serves as the main entry point for the CLI, orchestrating command parsing, execution, and providing structured output. It integrates with dependency injection to handle various tenant, database, and backup operations while ensuring consistent logging and user feedback. The associated `ConsoleWriter` provides a convenient, color-coded mechanism for displaying success, error, warning, and informational messages to the terminal.

### Usage Example

```csharp
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<CliApplication>();
var consoleWriter = new ConsoleWriter();
var parser = new CommandParser();
var executor = new CommandExecutor();

var app = new CliApplication(parser, executor, logger, consoleWriter);
var args = new[] { "tenant", "list" };
var exitCode = await app.RunAsync(args);
consoleWriter.WriteSuccess($"Application exited with code: {exitCode}");
```

## CommandParser

The `CommandParser` class provides a robust command-line interface parser for the SQLite multi-tenant CLI application. It parses raw command-line arguments into structured `ParsedCommand` objects, enabling hierarchical command structures with subcommands, required arguments, and help generation. The parser validates command syntax and provides detailed error messages when commands are malformed.

### Usage Example

```csharp
using SqliteMultiTenant.Cli;
using System;

// Create a command parser instance
var parser = new CommandParser();

// Parse a simple command with main command and arguments
var parsed = parser.Parse(new[] { "tenant", "create", "acme-corp", "--description", "Acme Corporation" });

if (parsed.Success)
{
    Console.WriteLine($"Main command: {parsed.MainCommand}");
    Console.WriteLine($"Subcommand: {parsed.Subcommand}");
    Console.WriteLine($"Arguments: {string.Join(", ", parsed.Arguments)}");
    Console.WriteLine($"Description: {parsed.Description}");
}
else
{
    Console.WriteLine($"Error: {parsed.Message}");
}

// Parse a command with subcommands and required arguments
var subcommandParsed = parser.Parse(new[] { "backup", "create", "--tenant-id", "tenant-123", "--output", "/backups/db-backup.zip" });

if (subcommandParsed.IsHelpCommand)
{
    Console.WriteLine("Showing help for backup create command");
}

// Parse a help command
var helpParsed = parser.Parse(new[] { "help", "tenant" });
if (helpParsed.IsHelpCommand)
{
    Console.WriteLine("Displaying tenant command help");
}
```

## CommandLineParser

The `CommandLineParser` class provides a robust mechanism for registering and parsing command-line arguments in the SQLite multi-tenant application. It supports hierarchical command structures with subcommands, options, flags, and aliases, and facilitates automatic help text generation for CLI tools.

## DataRetentionPolicy

The `DataRetentionPolicy` class implements automated data retention management for multi-tenant SQLite databases. It applies configurable retention rules to automatically archive or delete old records based on age criteria, helping maintain database performance and compliance with data retention policies.

### Usage Example

```csharp
using SqliteMultiTenant.BackgroundWorkers;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<DataRetentionPolicy>();

// Create the retention policy service
var retentionPolicy = new DataRetentionPolicy(logger);

// Get default policy configuration for a tenant
var policy = retentionPolicy.GetDefaultPolicy("acme-corp");

// Add a custom retention rule for audit logs older than 2 years
policy.Rules.Add(new RetentionRule
{
    TableName = "AuditLog",
    DateColumn = "CreatedAt",
    RetentionType = RetentionType.YearsOld,
    RetentionValue = 2,
    IsEnabled = true,
    ArchiveBeforeDelete = false
});

// Add a rule for temporary data older than 30 days with archiving
policy.Rules.Add(new RetentionRule
{
    TableName = "TemporaryData",
    DateColumn = "ExpirationDate",
    RetentionType = RetentionType.DaysOld,
    RetentionValue = 30,
    IsEnabled = true,
    ArchiveBeforeDelete = true,
    ArchiveTableName = "ArchivedTemporaryData"
});

// Apply the retention policy to a tenant database
var connectionString = "Data Source=acme-corp.db;Version=3;";
await using var connection = new SQLiteConnection(connectionString);
connection.Open();

var result = await retentionPolicy.ApplyRetentionPolicyAsync(connection, policy);

if (result.IsSuccessful)
{
    Console.WriteLine($"Retention policy executed successfully!");
    Console.WriteLine($"Total records deleted: {result.TotalRecordsDeleted}");
    Console.WriteLine($"Executed at: {result.ExecutedAt}");
    
    foreach (var ruleResult in result.ProcessedRules.Values)
    {
        Console.WriteLine($"Table: {ruleResult.TableName}");
        Console.WriteLine($"  Records deleted: {ruleResult.RecordsDeleted}");
        Console.WriteLine($"  Status: {ruleResult.Status}");
    }
}
else
{
    Console.WriteLine($"Failed to apply retention policy: {result.Error}");
}

```

```csharp
using SqliteMultiTenant.Cli;
using System;

// Initialize with arguments
var parser = new CommandLineParser(new[] { "tenant", "--description", "A new tenant" });

// Register a command and its options
parser.RegisterCommand("tenant", "Manage tenants", (cmd) => { Console.WriteLine("Tenant command invoked"); })
    .RegisterOption("description", "Tenant description", 'd', required: false);

// Parse the arguments
var parsed = parser.Parse();

if (parsed.IsValid)
{
    Console.WriteLine($"Command: {parsed.Command}");
    Console.WriteLine($"Description: {parsed.GetOption("description", "No description provided")}");
}
else
{
    Console.WriteLine($"Error: {parsed.Error}");
}
```

## DatabaseMaintenanceWorker

The `DatabaseMaintenanceWorker` class is a background service that performs routine SQLite database maintenance operations to optimize performance and reclaim storage space. It runs VACUUM, ANALYZE, and REINDEX commands on a configurable schedule to maintain database health across all tenant databases.

### Usage Example

```csharp
using SqliteMultiTenant.BackgroundWorkers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<DatabaseMaintenanceWorker>();

// Configure database maintenance options
var options = Options.Create(new DatabaseMaintenanceOptions
{
    EnableVacuum = true,
    EnableAnalyze = true,
    EnableReindex = true,
    IntervalHours = 24,
    TimeoutSeconds = 300,
    DegreeOfParallelism = 2
});

// Create the database maintenance worker
var maintenanceWorker = new DatabaseMaintenanceWorker(
    logger,
    options,
    new TenantDatabaseService(/* dependencies */)
);

// Start the background maintenance service
await maintenanceWorker.StartAsync();

// The worker will now run maintenance every 24 hours
// Maintenance includes:
// - VACUUM to reclaim space and rebuild database
// - ANALYZE to update statistics for query planner
// - REINDEX to rebuild indexes for optimal performance

// Stop the maintenance service when shutting down
await maintenanceWorker.StopAsync();
```

## IScheduledTaskService

The `IScheduledTaskService` interface provides a mechanism for registering, managing, and executing background tasks on a configurable schedule. It supports task registration with custom intervals, status tracking, and graceful start/stop operations. Tasks are executed asynchronously and their execution status can be queried at runtime.

### Usage Example

```csharp
using SqliteMultiTenant.BackgroundWorkers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ScheduledTaskService>();

// Create the scheduled task service
var taskService = new ScheduledTaskService(logger);

// Register a background task to run every 30 seconds
var cleanupTask = async () =>
{
    Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Running cleanup task...");
    await Task.Delay(1000); // Simulate work
    Console.WriteLine("Cleanup completed successfully");
};

taskService.RegisterTask("cleanup-job", cleanupTask, TimeSpan.FromSeconds(30));

// Register another task to run every 5 minutes
var backupTask = async () =>
{
    Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Running backup task...");
    await Task.Delay(2000); // Simulate work
    Console.WriteLine("Backup completed successfully");
};

taskService.RegisterTask("backup-job", backupTask, TimeSpan.FromMinutes(5));

// Start the task service to begin executing registered tasks
await taskService.StartAsync();
Console.WriteLine("Task service started");

// Wait for a while to see tasks execute
await Task.Delay(TimeSpan.FromMinutes(1));

// Check the status of a specific task
var status = await taskService.GetTaskStatusAsync("cleanup-job");
Console.WriteLine($"Cleanup task executed {status.ExecutionCount} times");
Console.WriteLine($"Next execution: {status.NextExecutionAt:HH:mm:ss}");

// Stop the task service when shutting down the application
await taskService.StopAsync();
Console.WriteLine("Task service stopped");

// Unregister a task when it's no longer needed
taskService.UnregisterTask("backup-job");
```

## TenantStorageInfo

The `TenantStorageInfo` record provides storage usage statistics for a single tenant database, including database size, page count, page size, and WAL file size. It is typically returned by storage monitoring operations to track tenant database growth and resource consumption.

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using System;

// Create storage info for a tenant database
var storageInfo = new TenantStorageInfo
{
    TenantId = "acme-corp",
    SizeBytes = 1_048_576,      // 1 MB database
    PageCount = 131_072,        // 131,072 pages
    PageSize = 8_192,           // 8 KB pages
    WalSizeBytes = 524_288      // 512 KB WAL file
};

Console.WriteLine($"Tenant: {storageInfo.TenantId}");
Console.WriteLine($"Database size: {storageInfo.SizeBytes:N0} bytes ({storageInfo.SizeBytes / 1024:N0} KB)");
Console.WriteLine($"Total size (with WAL): {storageInfo.TotalSizeBytes:N0} bytes ({storageInfo.TotalSizeBytes / 1024:N0} KB)");
Console.WriteLine($"Pages: {storageInfo.PageCount:N0}, Page size: {storageInfo.PageSize} bytes");
Console.WriteLine($"WAL size: {storageInfo.WalSizeBytes:N0} bytes");

// Access computed property
if (storageInfo.TotalSizeBytes > 2_000_000)
{
    Console.WriteLine("Storage threshold exceeded!");
}
```

## LoggingExtensions

The `LoggingExtensions` class provides structured logging extension methods for the SQLite multi-tenant application. It enables semantic, context-rich logging that improves log searchability and analysis in centralized logging systems. The extension methods follow structured logging best practices and automatically include relevant context for each operation type.

### Usage Example

```csharp
using SqliteMultiTenant.Logging;
using Microsoft.Extensions.Logging;
using System;

// Create a logger
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

// Log tenant operations
logger.LogTenantOperation("TenantCreated", "acme-corp", "success", 150);
logger.LogTenantOperation("TenantDeleted", "globex", "failed", 45);

// Log database operations with performance tracking
logger.LogDatabaseOperation("QueryExecution", "acme-corp-db", 250, success: true);
logger.LogDatabaseOperation("Migration", "shared-schema", 8500, success: false); // Slow operation

// Log backup operations
logger.LogBackupOperation("CreateBackup", "acme-2024-07-16", 15_728_640, 1250, success: true);

// Log migration operations
logger.LogMigrationOperation("ApplyMigration", "m20240716-001", "1.2.3", "AddTenantsTable", 3200, success: true);

// Log API requests
logger.LogApiRequest("GET", "/api/tenants/acme-corp", 200, 42);
logger.LogApiRequest("POST", "/api/tenants", 400, 156); // Bad request

// Log cache operations
logger.LogCacheOperation("GetTenant", "tenant:acme-corp:config", hit: true, durationMs: 2);
logger.LogCacheOperation("SetTenant", "tenant:globex:metadata", hit: false, durationMs: 8);

// Log validation errors
var validationErrors = new Dictionary<string, string>
{
    { "Name", "Name is required" },
    { "Email", "Email format is invalid" }
};
logger.LogValidationError("Tenant", validationErrors);

// Log webhook delivery
logger.LogWebhookDelivery("wh-12345", "https://webhook.site/abc", retry: 1, maxRetries: 3, success: false);

// Log background jobs
logger.LogBackgroundJob("TenantCleanupJob", 12500, itemsProcessed: 42, success: true);

// Log health checks
logger.LogHealthCheck("DatabaseConnection", healthy: true, durationMs: 25, message: "Connection established");
logger.LogHealthCheck("BackupService", healthy: false, durationMs: 1500, message: "Backup directory not found");

// Log configuration errors
logger.LogConfigurationError("Database:ConnectionString", "Server=localhost;Database=multi-tenant", "Server=unknown-host");

// Use OperationContext for scoped operations
using (var operation = new OperationContext(logger, "FullTenantSetup"))
{
    // Your tenant setup logic here
    // Operation completion is automatically logged on Dispose
}
```
## IRequestResponseLogger

The `IRequestResponseLogger` interface provides a mechanism for logging HTTP request and response details for debugging, monitoring, and analytics purposes. It captures comprehensive information including headers, body content, query parameters, IP addresses, status codes, and timing metrics. The implementation includes sampling to manage log volume and thread-safe operations for concurrent access.

### Usage Example

```csharp
using SqliteMultiTenant.Logging;
using Microsoft.Extensions.Logging;
using System.Net.Http;

// Create a logger factory and logger
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<RequestResponseLogger>();

// Create the logger instance
var requestResponseLogger = new RequestResponseLogger(logger);

// Log a sample HTTP request
var requestLog = new RequestLog
{
    Method = "GET",
    Path = "/api/users",
    Host = "localhost:5000",
    Body = "{ \"userId\": 123 }",
    Headers = new Dictionary<string, string>
    {
        { "Authorization", "Bearer token123" },
        { "Content-Type", "application/json" },
        { "X-Request-Id", "req-456" }
    },
    QueryParameters = new Dictionary<string, string>
    {
        { "page", "1" },
        { "limit", "10" }
    },
    IpAddress = "192.168.1.100"
};

await requestResponseLogger.LogRequestAsync(requestLog);

// Log a sample HTTP response
var responseLog = new ResponseLog
{
    StatusCode = 200,
    DurationMs = 42,
    Body = "{\"users\": [{\"id\": 123, \"name\": \"John Doe\"}]}",
    ResponseSize = 68,
    Headers = new Dictionary<string, string>
    {
        { "Content-Type", "application/json" },
        { "X-Response-Time", "42ms" }
    }
};

await requestResponseLogger.LogResponseAsync(responseLog);

// Retrieve request logs with filtering
var requestLogs = await requestResponseLogger.GetRequestLogsAsync(new LogFilter
{
    Method = "GET",
    Path = "/api",
    Limit = 50
});

Console.WriteLine($"Found {requestLogs.Count} matching request logs");

// Retrieve response logs with filtering
var responseLogs = await requestResponseLogger.GetResponseLogsAsync(new LogFilter
{
    StatusCode = 200,
    Limit = 50
});

Console.WriteLine($"Found {responseLogs.Count} successful response logs");

// Get comprehensive statistics
var statistics = await requestResponseLogger.GetStatisticsAsync();
Console.WriteLine($"Total requests: {statistics.TotalRequestsLogged}");
Console.WriteLine($"Total responses: {statistics.TotalResponsesLogged}");
Console.WriteLine($"Average request size: {statistics.AverageRequestSize:F2} bytes");
Console.WriteLine($"Average response time: {statistics.AverageResponseTime:F2} ms");
Console.WriteLine($"Most common path: {statistics.MostCommonPath}");
Console.WriteLine($"Most common method: {statistics.MostCommonMethod}");
```

## TenantContext

The `TenantContext` class provides tenant-aware context information throughout the application, carrying tenant identification, user details, request metadata, and extensible context data. It is designed to flow through the application's request pipeline, enabling automatic tenant isolation and contextual logging without requiring explicit tenant parameters in every method.

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using Microsoft.Extensions.Logging;
using System;

// Create a tenant context for a new request
var tenantContext = new TenantContext
{
    TenantId = "acme-corp",
    TenantName = "Acme Corporation",
    UserId = "user-456",
    UserEmail = "john.doe@acme.com",
    EstablishedAt = new DateTime(2024, 1, 15),
    CreatedAt = DateTime.UtcNow,
    RequestId = "req-789",
    ConnectionId = "conn-abc-123",
    DatabasePath = "/data/acme-corp.db",
    ContextData = new Dictionary<string, object>
    {
        { "requestSource", "web-portal" },
        { "userAgent", "Mozilla/5.0" },
        { "sessionId", "sess-xyz-789" }
    }
};

// Validate the context
if (tenantContext.IsValid)
{
    Console.WriteLine($"Valid tenant context for {tenantContext.TenantName}");
    Console.WriteLine($"Tenant established: {tenantContext.EstablishedAt:yyyy-MM-dd}");
}
else
{
    Console.WriteLine("Invalid tenant context");
    tenantContext.Validate(); // Returns validation errors
}

// Access context data
var requestSource = tenantContext.GetContextData("requestSource") as string;
Console.WriteLine($"Request source: {requestSource}");

// Update context data
tenantContext.SetContextData("processingStartTime", DateTime.UtcNow);

// Invalidate the context when tenant is no longer valid
// tenantContext.Invalidate();

// String representation
Console.WriteLine($"Tenant context: {tenantContext}");
```

## ConflictResolutionService

The `ConflictResolutionService` class provides conflict detection and resolution capabilities for multi-tenant SQLite databases. It handles scenarios where data has been modified both locally and remotely, allowing you to detect conflicts, apply resolution strategies, and persist the resolved values back to the database. This is particularly useful for merge operations, data synchronization workflows, and handling concurrent updates from different sources.

### Usage Example

```csharp
using SqliteMultiTenant.Operations;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ConflictResolutionService>();

// Create the conflict resolution service
var conflictService = new ConflictResolutionService(logger);

// Simulate local and remote data versions (e.g., from a sync operation)
var localData = new Dictionary<string, object>
{
    { "Name", "Acme Corporation" },
    { "Status", "Active" },
    { "EmployeeCount", 150 },
    { "LastUpdated", DateTime.UtcNow.AddDays(-1) }
};

var remoteData = new Dictionary<string, object>
{
    { "Name", "Acme Corporation" },
    { "Status", "Inactive" },  // Conflict: different status
    { "EmployeeCount", 200 },  // Conflict: different employee count
    { "Revenue", 1_500_000 } // Conflict: field exists remotely but not locally
};

// Step 1: Detect conflicts
var conflictResult = conflictService.DetectConflicts(localData, remoteData);

if (conflictResult.HasConflicts)
{
    Console.WriteLine($"Found {conflictResult.Conflicts.Count} conflicts:");
    foreach (var conflict in conflictResult.Conflicts)
    {
        Console.WriteLine($"  - {conflict.Field}: {conflict.ConflictType}");
        Console.WriteLine($"    Local: {conflict.LocalValue}");
        Console.WriteLine($"    Remote: {conflict.RemoteValue}");
    }

    // Step 2: Resolve conflicts using a strategy
    var resolutionResult = await conflictService.ResolveConflictsAsync(
        conflictResult,
        ConflictResolutionStrategy.Merge
    );

    if (resolutionResult.IsSuccessful)
    {
        Console.WriteLine("Conflicts resolved successfully!");
        foreach (var resolved in resolutionResult.ResolvedValues)
        {
            Console.WriteLine($"  {resolved.Key} = {resolved.Value}");
        }

        // Step 3: Apply resolutions to database
        var connectionString = "Data Source=acme-corp.db;Version=3;";
        await using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        bool applied = await conflictService.ApplyResolutionAsync(
            connection,
            "Tenants",
            "Id",
            "acme-corp",
            resolutionResult
        );

        Console.WriteLine($"Resolution applied to database: {applied}");
    }
}
```

## IBatchProcessor

The `IBatchProcessor` interface and its implementation `BatchProcessor` provide a robust mechanism for processing collections of items in parallel with built-in error isolation and detailed result tracking. It's ideal for batch operations where individual failures shouldn't stop the entire batch, such as processing multiple tenant records, database migrations, or API calls.




### Usage Example


```csharp
using SqliteMultiTenant.Operations;
using Microsoft.Extensions.Logging;
using System.Data.SQLite;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<BatchProcessor>();

// Create the batch processor
var batchProcessor = new BatchProcessor(logger);

// Sample data to process - tenant IDs to archive
var tenantIds = new[] { "tenant-001", "tenant-002", "tenant-003", "tenant-004" };

// Define the batch operation - archive each tenant's database
var archiveOperation = async (string tenantId) =>
{
  // Simulate archiving a tenant database
  await Task.Delay(100); // Simulate work
  return $"Archived {tenantId}";
};

// Process the batch with 2 concurrent operations
var result = await batchProcessor.ProcessAsync(tenantIds, archiveOperation, maxConcurrency: 2);

// Analyze results
Console.WriteLine(result.ToString());
Console.WriteLine($"Successful operations: {result.SuccessCount}");
Console.WriteLine($"Failed operations: {result.ErrorCount}");

// Process errors if any occurred
if (result.ErrorCount > 0)
{
  Console.WriteLine("Errors encountered:");
  foreach (var error in result.Errors)
  {
    Console.WriteLine($" - Item {error.ItemId}: {error.Exception} - {error.Message}");
    if (!string.IsNullOrEmpty(error.StackTrace))
    {
      Console.WriteLine($"   Stack trace: {error.StackTrace}");
    }
  }
}

// Access successful results
foreach (var successResult in result.SuccessfulResults)
{
  Console.WriteLine($"Success: {successResult}");
}

// Alternative: Process without result transformation (fire-and-forget style)
var cleanupOperation = async (string tenantId) =>
{
  // Simulate cleanup operation
  await Task.Delay(50);
  // No return value needed
};

var simpleResult = await batchProcessor.ProcessAsync(tenantIds, cleanupOperation);
Console.WriteLine($"Cleanup completed: {simpleResult.SuccessCount} succeeded");
```

## IBatchOperationHandler

The `IBatchOperationHandler` interface provides a mechanism for executing batch operations across multiple resources with parallel processing, progress tracking, and detailed result reporting. It enables efficient bulk operations like database migrations, backups, or tenant management while handling partial failures gracefully.

### Usage Example

```csharp
using SqliteMultiTenant.Operations;
using Microsoft.Extensions.Logging;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<BatchOperationHandler>();

// Create the batch operation handler
var batchHandler = new BatchOperationHandler(logger);

// Define a batch operation to process multiple tenants
var operation = new BatchOperation
{
    OperationId = Guid.NewGuid().ToString(),
    OperationType = "apply-migration",
    ResourceIds = new List<string> { "tenant-001", "tenant-002", "tenant-003", "tenant-004" },
    Parameters = new Dictionary<string, object>
    {
        { "migration-name", "AddTenantsTable" },
        { "timeout-seconds", 30 }
    }
};

// Execute the batch operation
var result = await batchHandler.ExecuteAsync(operation, CancellationToken.None);

// Analyze results
Console.WriteLine($"Operation completed: {result.SuccessCount}/{result.TotalResources} successful");
Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds}ms");

// Process individual resource results
foreach (var resourceResult in result.ResourceResults)
{
    Console.WriteLine($"Resource {resourceResult.ResourceId}: {(resourceResult.Success ? "Success" : "Failed")}");
    if (!resourceResult.Success)
    {
        Console.WriteLine($"  Error: {resourceResult.Message}");
    }
}

// Get operation status (useful for polling)
var status = await batchHandler.GetStatusAsync(operation.OperationId);
Console.WriteLine($"Progress: {status.ProgressPercent}% ({status.ProcessedResources}/{status.TotalResources})");
```

## DataExporter

The `DataExporter` class provides functionality to export data from a SQLite database table into various portable formats such as JSON, CSV, and raw SQL INSERT statements. It's designed for data migration, backup, and integration scenarios where you need to extract table data for external processing, reporting, or archival purposes.

### Usage Example

```csharp
using SqliteMultiTenant.DataOperations;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<DataExporter>();

// Create the data exporter instance
var dataExporter = new DataExporter(logger);

// Example 1: Export a table as JSON with metadata
var connectionString = "Data Source=example.db;Version=3;";
await using var connection = new SQLiteConnection(connectionString);
await connection.OpenAsync();

var jsonExport = await dataExporter.ExportAsJsonAsync(connection, "Customers", includeMeta: true);
Console.WriteLine(jsonExport);

// Example 2: Export a table as JSON without metadata
var jsonDataOnly = await dataExporter.ExportAsJsonAsync(connection, "Products", includeMeta: false);
Console.WriteLine(jsonDataOnly);

// Example 3: Export a table as CSV with headers
var csvExport = await dataExporter.ExportAsCsvAsync(connection, "Orders", includeHeaders: true);
Console.WriteLine(csvExport);

// Example 4: Export a table as CSV without headers
var csvDataOnly = await dataExporter.ExportAsCsvAsync(connection, "Invoices", includeHeaders: false);
Console.WriteLine(csvDataOnly);

// Example 5: Export as SQL INSERT statements
var sqlExport = await dataExporter.ExportAsSqlAsync(connection, "Users");
Console.WriteLine(sqlExport);
```

## BulkDataService

The `BulkDataService` class provides high-performance bulk data export and import operations for multi-tenant SQLite databases. It supports exporting entire databases or individual tables to CSV, JSON, or SQL formats, and importing data from these formats back into the database. The service uses streaming for large datasets, integrates with the domain event bus for monitoring, and leverages batch processing for concurrent table operations.

### Public Members

```csharp
public BulkDataService(
    DataExporter exporter,
    DataImporter importer,
    IBatchProcessor batchProcessor,
    IEventBus eventBus,
    ILogger<BulkDataService> logger,
    BulkDataOptions options)
public async Task<BulkExportResult> ExportDatabaseAsync(
    string databaseId,
    BulkDataFormat format,
    ExportOptions? options = null,
    IProgress<ExportProgress>? progress = null,
    CancellationToken cancellationToken = default)
public async Task<BulkExportResult> ExportTableAsync(
    string databaseId,
    string tableName,
    BulkDataFormat format,
    ExportOptions? options = null,
    IProgress<ExportProgress>? progress = null,
    CancellationToken cancellationToken = default)
public async IAsyncEnumerable<ExportBatch> StreamExportAsync(
    string databaseId,
    string tableName,
    BulkDataFormat format,
    int batchSize = 1_000,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
public async Task<BulkImportResult> ImportTableAsync(
    string databaseId,
    string tableName,
    Stream dataStream,
    BulkDataFormat format,
    ImportOptions? options = null,
    IProgress<ImportProgress>? progress = null,
    CancellationToken cancellationToken = default)
public async Task<BulkImportResult> StreamImportAsync(
    string databaseId,
    IAsyncEnumerable<ImportBatch> batches,
    ImportOptions? options = null,
    IProgress<ImportProgress>? progress = null,
    CancellationToken cancellationToken = default)
```

### Usage Example

```csharp
using SqliteMultiTenant.BulkOperations;
using SqliteMultiTenant.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<BulkDataService>();

// Configure bulk data options
var options = Options.Create(new BulkDataOptions
{
    DefaultBatchSize = 1000,
    MaxConcurrentTables = 3,
    DefaultExportDirectory = "./exports",
    BaseDatabasePath = "/data/sqlite-databases"
});

// Create required dependencies
var exporter = new DataExporter();
var importer = new DataImporter();
var batchProcessor = new BatchProcessor(logger);
var eventBus = new EventBusImpl(logger);

// Create the bulk data service
var bulkDataService = new BulkDataService(
    exporter,
    importer,
    batchProcessor,
    eventBus,
    logger,
    options.Value
);

// Example 1: Export an entire database to JSON
var exportResult = await bulkDataService.ExportDatabaseAsync(
    databaseId: "acme-corp",
    format: BulkDataFormat.Json,
    options: new ExportOptions
    {
        IncludeMetadata = true,
        OutputFilePath = "./exports/acme-corp-backup.json"
    }
);

if (exportResult.IsSuccess)
{
    Console.WriteLine($"Exported {exportResult.TotalRowsExported} rows from {exportResult.TablesProcessed.Count} tables");
    Console.WriteLine($"Output saved to: {exportResult.OutputPath}");
}

// Example 2: Export a single table to CSV with progress reporting
var progress = new Progress<ExportProgress>();
progress.ProgressChanged += (sender, progressArgs) =>
{
    Console.WriteLine($"Export progress: {progressArgs.PercentComplete:F1}% - {progressArgs.RowsProcessed} rows processed");
};

var tableExportResult = await bulkDataService.ExportTableAsync(
    databaseId: "acme-corp",
    tableName: "Customers",
    format: BulkDataFormat.Csv,
    options: new ExportOptions { IncludeCsvHeaders = true },
    progress: progress
);

// Example 3: Stream export a large table in batches
await foreach (var batch in bulkDataService.StreamExportAsync(
    databaseId: "acme-corp",
    tableName: "Orders",
    format: BulkDataFormat.Json,
    batchSize: 500
))
{
    Console.WriteLine($"Processing batch {batch.SequenceNumber} with {batch.RowCount} rows");
    // Process each batch (e.g., send to external system, transform, etc.)
    // batch.Data contains the serialized batch content
}

// Example 4: Import data from a JSON file
using var fileStream = File.OpenRead("./exports/acme-corp-backup.json");
var importResult = await bulkDataService.ImportTableAsync(
    databaseId: "acme-corp",
    tableName: "Customers",
    dataStream: fileStream,
    format: BulkDataFormat.Json,
    options: new ImportOptions { TruncateBeforeImport = true }
);

if (importResult.IsSuccess)
{
    Console.WriteLine($"Imported {importResult.TotalRowsImported} rows successfully");
}

// Example 5: Stream import from multiple batches
var batches = GenerateImportBatches(); // Your IAsyncEnumerable<ImportBatch> implementation
var streamImportResult = await bulkDataService.StreamImportAsync(
    databaseId: "acme-corp",
    batches: batches,
    options: new ImportOptions { SkipFailedRows = true }
);
```

## DataConsistencyChecker

The `DataConsistencyChecker` class provides comprehensive data integrity validation for SQLite multi-tenant databases. It performs integrity checks, detects duplicate records, validates record counts, and identifies constraint violations, foreign key issues, and missing indexes. This is essential for maintaining database health and ensuring data consistency across tenant databases.

### Public Members

```csharp
public sealed class DataConsistencyChecker
public DataConsistencyChecker()
public async Task<ConsistencyCheckResult> CheckDatabaseIntegrityAsync()
public async Task<List<DuplicateRecord>> FindDuplicatesAsync()
public async Task<bool> ValidateRecordCountsAsync()

public sealed class ConsistencyCheckResult
public bool IsHealthy { get; }
public bool IntegrityCheckPassed { get; }
public List<string> OrphanedRecords { get; }
public List<ConstraintViolation> ForeignKeyViolations { get; }
public List<string> MissingIndexes { get; }
public Dictionary<string, TableStatistics> TableStatistics { get; }
public DateTime CheckedAt { get; }

public sealed class ConstraintViolation
public string Table { get; set; }
public long Rowid { get; set; }
public string ParentTable { get; set; }
public long ParentRowid { get; set; }

public sealed class TableStatistics
public string TableName { get; set; }
public long RowCount { get; set; }
public long IndexCount { get; set; }
```

### Usage Example

```csharp
using SqliteMultiTenant.DataOperations;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<DataConsistencyChecker>();

// Create a SQLite connection to the tenant database
var connectionString = "Data Source=acme-corp.db;Version=3;";
await using var connection = new SQLiteConnection(connectionString);
await connection.OpenAsync();

// Create the data consistency checker instance
var consistencyChecker = new DataConsistencyChecker();

// Example 1: Run a comprehensive database integrity check
var integrityResult = await consistencyChecker.CheckDatabaseIntegrityAsync();

if (integrityResult.IsHealthy)
{
    Console.WriteLine("Database integrity check PASSED ✓");
    Console.WriteLine($"Integrity check passed: {integrityResult.IntegrityCheckPassed}");
    Console.WriteLine($"Checked at: {integrityResult.CheckedAt:yyyy-MM-dd HH:mm:ss}");
    
    // Display table statistics
    foreach (var tableStat in integrityResult.TableStatistics)
    {
        Console.WriteLine($"\nTable: {tableStat.Key}");
        Console.WriteLine($"  Rows: {tableStat.Value.RowCount}");
        Console.WriteLine($"  Indexes: {tableStat.Value.IndexCount}");
    }
    
    if (integrityResult.ForeignKeyViolations.Any())
    {
        Console.WriteLine("\nForeign key violations found:");
        foreach (var violation in integrityResult.ForeignKeyViolations)
        {
            Console.WriteLine($"  - Table '{violation.Table}' (row {violation.Rowid}) " +
                           $"references missing parent in '{violation.ParentTable}' (row {violation.ParentRowid})");
        }
    }
    
    if (integrityResult.MissingIndexes.Any())
    {
        Console.WriteLine("\nMissing indexes detected:");
        foreach (var missingIndex in integrityResult.MissingIndexes)
        {
            Console.WriteLine($"  - {missingIndex}");
        }
    }
}
else
{
    Console.WriteLine("Database integrity check FAILED ✗");
    Console.WriteLine("Issues detected:");
    
    if (integrityResult.ForeignKeyViolations.Any())
    {
        Console.WriteLine($"  Foreign key violations: {integrityResult.ForeignKeyViolations.Count}");
    }
    
    if (integrityResult.OrphanedRecords.Any())
    {
        Console.WriteLine($"  Orphaned records: {integrityResult.OrphanedRecords.Count}");
    }
    
    if (integrityResult.MissingIndexes.Any())
    {
        Console.WriteLine($"  Missing indexes: {integrityResult.MissingIndexes.Count}");
    }
}

// Example 2: Find duplicate records in the database
await connection.OpenAsync();
var duplicateRecords = await consistencyChecker.FindDuplicatesAsync();

if (duplicateRecords.Any())
{
    Console.WriteLine($"\nFound {duplicateRecords.Count} duplicate record(s):");
    foreach (var duplicate in duplicateRecords)
    {
        Console.WriteLine($"  - Table: {duplicate.TableName}");
        Console.WriteLine($"    Key: {duplicate.KeyColumn} = {duplicate.KeyValue}");
        Console.WriteLine($"    Duplicate rows: {string.Join(", ", duplicate.RowIds)}");
    }
}
else
{
    Console.WriteLine("\nNo duplicate records found ✓");
}

// Example 3: Validate record counts across all tables
var countValidationResult = await consistencyChecker.ValidateRecordCountsAsync();

if (countValidationResult)
{
    Console.WriteLine("\nRecord count validation PASSED ✓");
}
else
{
    Console.WriteLine("\nRecord count validation FAILED ✗");
}

// Clean up
connection.Close();
```

## BulkDataOptions

The `BulkDataOptions` class provides global configuration for the async bulk import/export subsystem. It controls batch sizes, concurrency limits, timeouts, and other operational parameters that apply across all bulk operations unless overridden by operation-specific options.

### Usage Example

```csharp
using SqliteMultiTenant.BulkOperations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Configure bulk data options via dependency injection
var services = new ServiceCollection();

services.Configure<BulkDataOptions>(options =>
{
    options.DefaultBatchSize = 5000;           // Larger batches for better throughput
    options.MaxConcurrentTables = 5;            // More parallel table processing
    options.MaxBufferSizeBytes = 20_000_000;  // 20 MB buffer
    options.OperationTimeout = TimeSpan.FromMinutes(30);
    options.PublishDomainEvents = true;         // Enable event publishing
    options.EnableProgressReporting = true;      // Enable progress callbacks
    options.DefaultExportDirectory = "./bulk-exports";
    options.BaseDatabasePath = "/data/sqlite-databases";
});

// Or configure via configuration file (appsettings.json)
// services.Configure<BulkDataOptions>(Configuration.GetSection("BulkData"));

var serviceProvider = services.BuildServiceProvider();

// Resolve the configured options
var bulkDataOptions = serviceProvider.GetRequiredService<IOptions<BulkDataOptions>>().Value;

Console.WriteLine($"Default batch size: {bulkDataOptions.DefaultBatchSize}");
Console.WriteLine($"Max concurrent tables: {bulkDataOptions.MaxConcurrentTables}");
Console.WriteLine($"Base database path: {bulkDataOptions.BaseDatabasePath}");
```

## DataImporter

The `DataImporter` class provides functionality for importing data into SQLite databases from various formats including JSON, CSV, and SQL files. It supports batch processing, transaction management, and progress reporting for efficient data loading operations.

### Usage Example

```csharp
using SqliteMultiTenant.BulkOperations;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;

// Create a SQLite connection
var connectionString = "Data Source=example.db;Version=3;";
var connection = new SQLiteConnection(connectionString);
connection.Open();

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<DataImporter>();

// Create the data importer instance
var dataImporter = new DataImporter();

// Example 1: Import data from a JSON file
var jsonFilePath = "./data/import.json";
var jsonImportResult = await dataImporter.ImportFromJsonAsync(
    connection, 
    "Customers",
    jsonFilePath,
    new ImportOptions
    {
        TruncateBeforeImport = true,
        BatchSize = 1000
    }
);

Console.WriteLine($"JSON import completed: {jsonImportResult.TotalRowsImported} rows imported");

// Example 2: Import data from a CSV file
var csvFilePath = "./data/import.csv";
var csvImportResult = await dataImporter.ImportFromCsvAsync(
    connection,
    "Products",
    csvFilePath,
    new ImportOptions
    {
        SkipHeaderRow = true,
        ColumnMapping = new Dictionary<string, string>
        {
            {"product_id", "Id"},
            {"product_name", "Name"},
            {"product_price", "Price"}
        }
    }
);

Console.WriteLine($"CSV import completed: {csvImportResult.TotalRowsImported} rows imported");

// Example 3: Import data from SQL statements
var sqlFilePath = "./data/import.sql";
var sqlImportResult = await dataImporter.ImportFromSqlAsync(
    connection,
    sqlFilePath,
    new ImportOptions
    {
        BatchSize = 500,
        TimeoutSeconds = 300
    }
);

Console.WriteLine($"SQL import completed: {sqlImportResult.TotalRowsImported} rows affected");

// Clean up
connection.Close();
```

## QueryBuilder

The QueryBuilder class provides a fluent SQL query builder for constructing parameterized SELECT statements. It supports WHERE, AND/OR conditions, INNER/LEFT JOIN, ORDER BY, LIMIT, and OFFSET clauses. Column names are automatically bracket-quoted for safety.

### Usage Example
```csharp
var query = new QueryBuilder("Users")
    .Select("Name", "Email")
    .Where("IsActive = @active", ("active", true))
    .OrderBy("Name")
    .Limit(10)
    .Build();

// Result: SELECT [Name], [Email] FROM [Users] WHERE IsActive = @active ORDER BY [Name] ASC LIMIT 10
```

```csharp
// Complex query with joins and multiple conditions
var complexQuery = new QueryBuilder("Orders")
    .Select("o.Id", "o.Total", "c.Name as CustomerName")
    .InnerJoin("Customers c", "c.Id = o.CustomerId")
    .Where("o.Status = @status", ("status", "Completed"))
    .And("o.OrderDate >= @minDate", ("minDate", new DateTime(2024, 1, 1)))
    .OrderBy("o.OrderDate", "DESC")
    .Limit(50)
    .Offset(100)
    .Build();

// Result: SELECT [o].[Id], [o].[Total], [c].[Name] as CustomerName FROM [Orders] INNER JOIN Customers c ON c.Id = o.CustomerId WHERE (o.Status = @status) AND (o.OrderDate >= @minDate) ORDER BY [o].[OrderDate] DESC LIMIT 50 OFFSET 100
```

## BulkInsertBuilder

The `BulkInsertBuilder` class provides an efficient way to insert multiple records into a SQLite database table using batch processing and transaction management. It supports fluent interface for adding records, configurable batch sizes, and both execution and SQL generation modes. This is particularly useful for bulk data loading scenarios where performance is critical.

### Public Members

```csharp
public sealed class BulkInsertBuilder
public BulkInsertBuilder(SQLiteConnection connection, ILogger<BulkInsertBuilder> logger, string tableName, int batchSize = 1000)
public BulkInsertBuilder AddRecord(Dictionary<string, object> record)
public BulkInsertBuilder AddRecords(IEnumerable<Dictionary<string, object>> records)
public async Task<BulkInsertResult> ExecuteAsync()
public string GenerateSqlStatements()

public sealed class BulkInsertResult
public int TotalRecords { get; set; }
public int InsertedRecords { get; set; }
public bool IsSuccessful { get; set; }
public string Error { get; set; }
```

### Usage Example

```csharp
using SqliteMultiTenant.Operations;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;

// Create a SQLite connection
var connectionString = "Data Source=example.db;Version=3;";
var connection = new SQLiteConnection(connectionString);
connection.Open();

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<BulkInsertBuilder>();

// Create the bulk insert builder
var bulkInsertBuilder = new BulkInsertBuilder(connection, logger, "Customers");

// Add records using the fluent interface
bulkInsertBuilder
    .AddRecord(new Dictionary<string, object>
    {
        { "Id", 1 },
        { "Name", "John Doe" },
        { "Email", "john@example.com" },
        { "CreatedAt", DateTime.UtcNow }
    })
    .AddRecord(new Dictionary<string, object>
    {
        { "Id", 2 },
        { "Name", "Jane Smith" },
        { "Email", "jane@example.com" },
        { "CreatedAt", DateTime.UtcNow }
    })
    .AddRecords(new[]
    {
        new Dictionary<string, object>
        {
            { "Id", 3 },
            { "Name", "Bob Johnson" },
            { "Email", "bob@example.com" },
            { "CreatedAt", DateTime.UtcNow }
        },
        new Dictionary<string, object>
        {
            { "Id", 4 },
            { "Name", "Alice Williams" },
            { "Email", "alice@example.com" },
            { "CreatedAt", DateTime.UtcNow }
        }
    });

// Execute the bulk insert operation
var result = await bulkInsertBuilder.ExecuteAsync();

if (result.IsSuccessful)
{
    Console.WriteLine($"Successfully inserted {result.InsertedRecords} of {result.TotalRecords} records");
}
else
{
    Console.WriteLine($"Failed to insert records: {result.Error}");
}

// Alternatively, generate SQL statements without executing
var sqlStatements = bulkInsertBuilder.GenerateSqlStatements();
Console.WriteLine("Generated SQL statements:");
Console.WriteLine(sqlStatements);

// Clean up
connection.Close();
```

## BackupRotationManager

The `BackupRotationManager` class manages automatic rotation and cleanup of tenant database backups according to configurable retention policies. It enforces limits on backup age, total backup count, and disk usage, automatically deleting old backups when thresholds are exceeded. The manager also provides verification capabilities to ensure backup integrity and statistics for monitoring backup storage usage.

### Usage Example

```csharp
using SqliteMultiTenant.BackgroundWorkers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<BackupRotationManager>();

// Configure backup rotation policy
var policyOptions = Options.Create(new BackupRotationPolicy
{
    MaxBackupAge = TimeSpan.FromDays(30),      // Keep backups for max 30 days
    MaxBackupCount = 10,                      // Keep max 10 backups
    MaxDiskUsage = 5 * 1024 * 1024 * 1024   // Max 5 GB disk usage
});

// Create the backup rotation manager
var backupRotationManager = new BackupRotationManager(
    logger,
    policyOptions,
    new TenantDatabaseService(/* dependencies */),
    new BackupVerificationService(/* dependencies */)
);

// Estimate current backup disk usage
long currentUsage = await backupRotationManager.EstimateBackupDiskUsage();
Console.WriteLine($"Current backup disk usage: {currentUsage:N0} bytes");

// Get backup statistics
var statistics = backupRotationManager.GetBackupStatistics();
Console.WriteLine($"Total backups: {statistics.TotalBackups}");
Console.WriteLine($"Oldest backup: {statistics.OldestBackupDate}");
Console.WriteLine($"Newest backup: {statistics.NewestBackupDate}");

// Rotate backups (automatically enforces policy)
var rotationResult = await backupRotationManager.RotateBackupsAsync();
Console.WriteLine($"Rotation successful: {rotationResult.IsSuccessful}");
Console.WriteLine($"Total backups before rotation: {rotationResult.TotalBackups}");
Console.WriteLine($"Backups deleted by age: {rotationResult.DeletedByAge}");
Console.WriteLine($"Backups deleted by count: {rotationResult.DeletedByCount}");
Console.WriteLine($"Remaining backups: {rotationResult.RemainingBackups}");

// Verify remaining backups
var verificationResults = await backupRotationManager.VerifyBackupsAsync();
foreach (var result in verificationResults)
{
    Console.WriteLine($"Verified: {result.FilePath} - {(result.IsValid ? "OK" : "FAILED")}");
}
```

## ExportProgress

The `ExportProgress` record represents incremental progress snapshots emitted during bulk export operations. It is designed for streaming export pipelines where large datasets are processed in batches, allowing consumers to update progress bars, monitoring dashboards, or other UI elements without buffering the entire result set.

### Usage Example

```csharp
using SqliteMultiTenant.BulkOperations;
using Microsoft.Extensions.Logging;

// Create a logger for progress tracking
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ExportProgress>();

// Simulate export progress tracking for a table
var progress = new ExportProgress(
    TableName: "Customers",
    RowsProcessed: 1500,
    TotalRowsEstimate: 5000,
    BatchSequence: 1
);

Console.WriteLine($"Exporting {progress.TableName}");
Console.WriteLine($"Progress: {progress.PercentComplete:F1}%");
Console.WriteLine($"Rows processed: {progress.RowsProcessed}/{progress.TotalRowsEstimate}");
Console.WriteLine($"Batch sequence: {progress.BatchSequence}");

// Track progress across multiple batches
var progressUpdates = new[]
{
    new ExportProgress("Customers", 1000, 5000, 0),
    new ExportProgress("Customers", 2500, 5000, 1),
    new ExportProgress("Customers", 4000, 5000, 2),
    new ExportProgress("Customers", 5000, 5000, 3)
};

foreach (var update in progressUpdates)
{
    Console.WriteLine($"Batch {update.BatchSequence}: {update.PercentComplete:F1}% complete");
}
```

## Backup

The `Backup` class represents a backup operation for a tenant database, capturing metadata about the backup process including timing, size, status, and encryption settings. It is used to track backup jobs and their outcomes for monitoring, verification, and restoration purposes.

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using System;

// Create a backup instance representing a completed backup operation
var backup = new Backup
{
    BackupId = Guid.NewGuid().ToString(),
    DatabaseId = "acme-corp-db",
    BackupPath = "/backups/acme-corp-2024-07-16.db.backup",
    BackupType = BackupType.Full,
    Status = BackupStatus.Completed,
    CreatedAt = DateTime.UtcNow.AddMinutes(-15),
    CompletedAt = DateTime.UtcNow,
    VerifiedAt = DateTime.UtcNow.AddSeconds(-30),
    SizeBytes = 15_728_640, // 15 MB
    OriginalSizeBytes = 20_971_520, // 20 MB
    CompressionRatio = 25, // 25% of original size
    CreatedBy = "backup-service",
    VerifiedBy = "backup-verifier",
    ErrorMessage = null,
    DurationMs = 1250, // 1.25 seconds
    IsEncrypted = true,
    IsVerified = true,
    ExpiresAt = DateTime.UtcNow.AddDays(30),
    Tags = "daily,full,encrypted"
};

Console.WriteLine($"Backup created: {backup.BackupId}");
Console.WriteLine($"Database: {backup.DatabaseId}");
Console.WriteLine($"Type: {backup.BackupType}");
Console.WriteLine($"Status: {backup.Status}");
Console.WriteLine($"Size: {backup.SizeBytes:N0} bytes (compressed from {backup.OriginalSizeBytes:N0})");
Console.WriteLine($"Compression: {backup.CompressionRatio}%");
Console.WriteLine($"Encrypted: {backup.IsEncrypted}");
Console.WriteLine($"Verified: {backup.IsVerified}");
Console.WriteLine($"Expires: {backup.ExpiresAt:yyyy-MM-dd}");

// Access computed properties
if (backup.IsVerified && backup.CompletedAt.HasValue)
{
    var duration = backup.CompletedAt.Value - backup.CreatedAt;
    Console.WriteLine($"Backup completed in {duration.TotalSeconds:F2} seconds");
}
```

## TenantDatabase

The `TenantDatabase` class represents a database associated with a tenant in the multi-tenant SQLite system. It tracks database metadata including file paths, sizes, encryption settings, connection counts, and backup history. This class is central to managing tenant-specific databases and their lifecycle operations.

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using System;

// Create a new tenant database instance
var tenantDb = new TenantDatabase
{
    DatabaseId = Guid.NewGuid().ToString(),
    TenantId = "acme-corp",
    Name = "Acme Corporation Database",
    FilePath = "/data/acme-corp.db",
    SizeBytes = 1_048_576, // 1 MB
    SchemaVersion = 2,
    IsReadOnly = false,
    RequiresEncryption = true,
    EncryptionKey = Guid.NewGuid().ToString(),
    ActiveConnectionCount = 0,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

// Validate the database entity
if (tenantDb.Validate(out var errors))
{
    Console.WriteLine("Database entity is valid");
}
else
{
    Console.WriteLine("Validation errors:");
    foreach (var error in errors)
    {
        Console.WriteLine($"- {error}");
    }
}

// Update database size after operations
tenantDb.UpdateSize(2_097_152); // 2 MB
Console.WriteLine($"Database size updated to: {tenantDb.SizeBytes:N0} bytes");

// Record a backup operation
tenantDb.UpdateLastBackupTime();
Console.WriteLine($"Last backup: {tenantDb.LastBackupAt}");

// Increment connection count when a connection is opened
tenantDb.IncrementConnectionCount();
Console.WriteLine($"Active connections: {tenantDb.ActiveConnectionCount}");

// Check encryption status
Console.WriteLine($"Is encrypted: {tenantDb.IsEncrypted}");

// Decrement connection count when a connection is closed
tenantDb.DecrementConnectionCount();
Console.WriteLine($"Active connections after close: {tenantDb.ActiveConnectionCount}");
```

## TenantSettings

The `TenantSettings` class represents tenant-specific configuration settings stored in the database. It provides a flexible key-value store for tenant preferences, feature flags, and other configuration data with support for type-safe value retrieval and encryption. The class includes validation, change tracking, and active/inactive state management for configuration lifecycle control.

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using System;

// Create a tenant settings instance for a new configuration
var settings = new TenantSettings
{
    SettingId = Guid.NewGuid().ToString(),
    TenantId = "acme-corp",
    SettingKey = "MaxConcurrentJobs",
    SettingValue = "10",
    Description = "Maximum number of concurrent background jobs for this tenant",
    DataType = "int",
    IsEncrypted = false,
    IsActive = true,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
    LastModifiedBy = "admin@acme.com"
};

// Validate the settings
if (settings.Validate())
{
    Console.WriteLine("Settings are valid");
}

// Update the setting value
settings.UpdateValue("15");

// Get the typed value
int maxJobs = settings.GetValue<int>();
Console.WriteLine($"Max concurrent jobs: {maxJobs}");

// Set the active state
settings.SetActive(false);
Console.WriteLine($"Is active: {settings.IsActive}");

// Create another setting with encrypted value
var encryptedSetting = new TenantSettings
{
    SettingId = Guid.NewGuid().ToString(),
    TenantId = "globex",
    SettingKey = "ApiKey",
    SettingValue = "secret-api-key-123",
    Description = "External API key for third-party integration",
    DataType = "string",
    IsEncrypted = true,
    IsActive = true,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

// Set a typed value
encryptedSetting.SetValue("new-secret-key-456");
string apiKey = encryptedSetting.GetValue<string>();
Console.WriteLine($"API key: {apiKey}");

// Check if setting is valid for use
if (settings.IsActive && settings.Validate())
{
    Console.WriteLine("Setting is ready for use");
}
```

## Migration

The `Migration` class represents a database migration for a tenant, tracking the execution of schema changes and data migrations. It captures metadata about the migration process including scripts, timing, status, and execution details, enabling rollback capabilities and comprehensive migration auditing.

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Constants;
using System;

// Create a migration instance for adding a new table
var migration = new Migration
{
  MigrationId = "m20240716-001",
  DatabaseId = "acme-corp-db",
  Version = "1.2.3",
  Name = "AddTenantsTable",
  Description = "Add Tenants table for multi-tenant support",
  UpScript = @"
CREATE TABLE IF NOT EXISTS Tenants (
  Id TEXT PRIMARY KEY,
  Name TEXT NOT NULL,
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  IsActive BOOLEAN NOT NULL DEFAULT 1
);",
  DownScript = @"
DROP TABLE IF EXISTS Tenants;",
  Status = MigrationStatus.Pending,
  ExecutionOrder = 1,
  IsRollbackable = true,
  CreatedAt = DateTime.UtcNow
};

// Validate the migration
if (migration.Validate(out var errors))
{
  Console.WriteLine("Migration is valid");
}
else
{
  Console.WriteLine("Migration validation errors:");
  foreach (var error in errors)
  {
    Console.WriteLine($"- {error}");
  }
}

// Mark migration as started
migration.MarkAsStarted("migration-service");
Console.WriteLine($"Migration started at: {migration.ExecutedAt}");

// Simulate migration execution (in real code, this would execute the UpScript)
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// Execute database schema changes here...
System.Threading.Thread.Sleep(150); // Simulate work
stopwatch.Stop();

// Mark migration as completed
migration.MarkAsCompleted(stopwatch.ElapsedMilliseconds);
Console.WriteLine($"Migration completed in {migration.ExecutionTimeMs}ms");
Console.WriteLine($"Status: {migration.Status}");
Console.WriteLine($"Completed at: {migration.CompletedAt}");

// Check if migration can be rolled back
if (migration.CanRollback())
{
  Console.WriteLine("Migration can be rolled back");
}

// Get display name
Console.WriteLine($"Migration display name: {migration.GetDisplayName()}");
```

## CommandExecutor

The `CommandExecutor` class executes parsed CLI commands asynchronously and returns structured results. It encapsulates the business logic for tenant management, database operations, and backup/restore workflows, returning success status and descriptive messages for each operation.

### Usage Example

```csharp
using SqliteMultiTenant.Cli;
using Microsoft.Extensions.Logging;

// Create a logger and executor instance
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<CommandExecutor>();
var executor = new CommandExecutor();

// Execute a tenant list command
var result = await executor.ExecuteAsync(new[] { "tenant", "list" });

if (result.Success)
{
    Console.WriteLine("Tenants retrieved successfully:");
    Console.WriteLine(result.Message);
}
else
{
    Console.WriteLine($"Error: {result.Message}");
}

// Execute a backup command with required arguments
var backupResult = await executor.ExecuteAsync(new[] { "backup", "create", "--tenant-id", "acme", "--output", "/backups/acme.db.zip" });

if (backupResult.Success)
{
    Console.WriteLine($"Backup created: {backupResult.Message}");
}
else
{
    Console.WriteLine($"Backup failed: {backupResult.Message}");
}
```

The `CommandExecutor` integrates with `CommandParser` to transform parsed commands into executable operations, handling both simple commands and complex workflows with multiple arguments and subcommands.


The `CliApplication` class is used to run the CLI application with the given arguments. 

### Usage Example

```csharp
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<CliApplication>();
var consoleWriter = new ConsoleWriter();
var parser = new CommandParser();
var executor = new CommandExecutor();

var app = new CliApplication(parser, executor, logger, consoleWriter);
var args = new[] { "tenant", "list" };
var exitCode = await app.RunAsync(args);
consoleWriter.WriteSuccess($"Application exited with code: {exitCode}");
```
```