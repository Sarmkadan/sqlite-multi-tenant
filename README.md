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

### Usage Example

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