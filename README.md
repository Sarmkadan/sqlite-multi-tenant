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

## IRequestInterceptor

The `IRequestInterceptor` interface provides a mechanism for preprocessing HTTP requests and post-processing responses in ASP.NET Core applications. Interceptors enable cross-cutting concerns like tenant context extraction, request validation, correlation ID tracking, and audit logging without cluttering controller logic. The interface supports both request pre-processing (with validation) and response post-processing hooks.

## Result

The `Result` type provides a standardized wrapper for API responses, enabling consistent error handling and success tracking across the application. It supports both data-bearing operations (`Result<T>`) and paginated results (`PaginatedResult<T>`), with built-in metadata for tracing and debugging. The type includes success status, error collection, and optional message fields to simplify API response construction.

### Usage Example

```csharp
using SqliteMultiTenant.Api.Responses;
using Microsoft.Extensions.Logging;

// Create a logger
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

// Example 1: Successful result with data
var user = new { Id = 1, Name = "John Doe", Email = "john@example.com" };
var successResult = Result<object>.Ok(user, "User retrieved successfully");

if (successResult.Success)
{
    Console.WriteLine(successResult.Message);
    Console.WriteLine($"User: {successResult.Data}");
}

// Example 2: Failed result with error message
var errorResult = Result<object>.Fail("User not found");

if (!errorResult.Success)
{
    Console.WriteLine("Errors:");
    foreach (var error in errorResult.Errors)
    {
        Console.WriteLine($"- {error}");
    }
}

// Example 3: Failed result with multiple errors
var validationErrors = new List<string>
{
    "Email is required",
    "Email format is invalid",
    "Password must be at least 8 characters"
};
var multiErrorResult = Result<object>.Fail(validationErrors);

// Example 4: Result with metadata
await Task.Delay(100); // Simulate async operation
var resultWithMetadata = Result<object>.Ok(user);
resultWithMetadata.Metadata = new ResultMetadata
{
    Timestamp = DateTime.UtcNow,
    TraceId = Guid.NewGuid().ToString(),
    StatusCode = 200,
    AdditionalData = new Dictionary<string, object>
    {
        { "processingTimeMs", 100 },
        { "method", "GetUserById" }
    }
};

// Example 5: Paginated result
var users = new List<object>
{
    new { Id = 1, Name = "User 1" },
    new { Id = 2, Name = "User 2" },
    new { Id = 3, Name = "User 3" }
};

var paginatedResult = PaginatedResult<object>.Ok(
    items: users,
    pageNumber: 1,
    pageSize: 10,
    totalCount: 42
);

Console.WriteLine($"Page {paginatedResult.Pagination.PageNumber} of {paginatedResult.Pagination.TotalPages}");
Console.WriteLine($"Total items: {paginatedResult.Pagination.TotalCount}");

// Example 6: Failed paginated result
var failedPaginatedResult = PaginatedResult<object>.Fail("Database connection failed");
```

## ApiResponse

The `ApiResponse<T>` class provides a standardized wrapper for API responses, enabling consistent error handling and success tracking across the application. It implements the Result pattern to provide status codes, success indicators, messages, and data in a single object, eliminating HTTP status code ambiguity at the application layer. The generic type parameter allows for strongly-typed data payloads while maintaining a consistent response structure.

### Public Members

```csharp
public sealed class ApiResponse<T>
public int StatusCode { get; set; }
public bool IsSuccess { get; set; }
public string Message { get; set; }
public T? Data { get; set; }
public Dictionary<string, string>? Errors { get; set; }
public DateTime Timestamp { get; set; }

public static ApiResponse<T> Success(T data, string message = "Success")
public static ApiResponse<T> Created(T data, string message = "Created")
public static ApiResponse<T> BadRequest(string message, Dictionary<string, string>? errors = null)
public static ApiResponse<T> NotFound(string message)
public static ApiResponse<T> Conflict(string message)
public static ApiResponse<T> InternalServerError(string message)
public static ApiResponse<T> Unauthorized(string message = "Unauthorized")
public static ApiResponse<T> Forbidden(string message = "Forbidden")
public static ApiResponse<T> Error(string message)
```

### Usage Example

```csharp
using SqliteMultiTenant.Api.Responses;
using Microsoft.Extensions.Logging;

// Create a logger
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

// Example 1: Successful response with data
var user = new { Id = 1, Name = "John Doe", Email = "john@example.com" };
var successResponse = ApiResponse<object>.Success(user, "User retrieved successfully");

if (successResponse.IsSuccess)
{
    Console.WriteLine($"Status: {successResponse.StatusCode}");
    Console.WriteLine($"Message: {successResponse.Message}");
    Console.WriteLine($"Data: {successResponse.Data}");
    Console.WriteLine($"Timestamp: {successResponse.Timestamp}");
}

// Example 2: Error response with validation errors
var validationErrors = new Dictionary<string, string>
{
    { "email", "Email format is invalid" },
    { "password", "Password must be at least 8 characters" }
};
var errorResponse = ApiResponse<object>.BadRequest("Validation failed", validationErrors);

if (!errorResponse.IsSuccess)
{
    Console.WriteLine($"Error: {errorResponse.Message}");
    Console.WriteLine($"Status Code: {errorResponse.StatusCode}");
    foreach (var error in errorResponse.Errors ?? new Dictionary<string, string>())
    {
        Console.WriteLine($"  - {error.Key}: {error.Value}");
    }
}

// Example 3: Standard HTTP status responses
var notFoundResponse = ApiResponse<object>.NotFound("User with ID 42 not found");
var conflictResponse = ApiResponse<object>.Conflict("Username already exists");
var unauthorizedResponse = ApiResponse<object>.Unauthorized("Invalid credentials");
var forbiddenResponse = ApiResponse<object>.Forbidden("Insufficient permissions");
var internalErrorResponse = ApiResponse<object>.InternalServerError("Database connection failed");

// Example 4: Created response for POST operations
var createdResponse = ApiResponse<object>.Created(
    new { Id = 42, Name = "New User" },
    "User created successfully"
);
```

## TenantController

The `TenantController` class provides a REST API controller for comprehensive tenant lifecycle management operations. It handles CRUD operations for multi-tenant database instances with built-in validation, audit logging, and error handling. The controller enforces business rules such as unique tenant names, valid email formats, and proper authorization checks while providing standardized API responses through the `ApiResponse<T>` wrapper.

### Public Members

```csharp
public sealed class TenantController
public TenantController(ITenantService tenantService, ILogger<TenantController> logger)
public async Task<ApiResponse<TenantResponse>> CreateTenantAsync(CreateTenantRequest request)
public async Task<ApiResponse<TenantResponse>> GetTenantAsync(string tenantId)
public async Task<ApiResponse<IEnumerable<TenantResponse>>> ListAllTenantsAsync()
public async Task<ApiResponse<TenantResponse>> UpdateTenantAsync(string tenantId, UpdateTenantRequest request)
public async Task<ApiResponse<object>> SuspendTenantAsync(string tenantId, string suspendedBy)
```

### Usage Example

```csharp
using SqliteMultiTenant.Api.Controllers;
using SqliteMultiTenant.Api.Requests;
using SqliteMultiTenant.Api.Responses;
using SqliteMultiTenant.Services;
using Microsoft.Extensions.Logging;

// Setup dependency injection
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<TenantController>();

// Create tenant service (would normally be injected)
var tenantService = new TenantService(/* dependencies */);

// Create the controller instance
var tenantController = new TenantController(tenantService, logger);

// Example 1: Create a new tenant
var createRequest = new CreateTenantRequest
{
    Name = "Acme Corporation",
    Description = "Global technology solutions provider for enterprise clients",
    ContactEmail = "admin@acme-corp.com"
};

var createResponse = await tenantController.CreateTenantAsync(createRequest);

if (createResponse.IsSuccess)
{
    Console.WriteLine($"Created tenant: {createResponse.Data?.TenantId}");
    Console.WriteLine($"Status: {createResponse.Data?.Status}");
}

// Example 2: Get a tenant by ID
var tenantId = "acme-corp";
var getResponse = await tenantController.GetTenantAsync(tenantId);

if (getResponse.IsSuccess)
{
    Console.WriteLine($"Found tenant: {getResponse.Data?.Name}");
    Console.WriteLine($"Created at: {getResponse.Data?.CreatedAt}");
}

// Example 3: List all tenants
var listResponse = await tenantController.ListAllTenantsAsync();

if (listResponse.IsSuccess)
{
    Console.WriteLine($"Total tenants: {listResponse.Data?.Count()}");
    foreach (var tenant in listResponse.Data ?? Enumerable.Empty<TenantResponse>())
    {
        Console.WriteLine($" - {tenant.Name} ({tenant.Status})");
    }
}

// Example 4: Update tenant information
var updateRequest = new UpdateTenantRequest
{
    Name = "Acme Corporation Updated",
    Description = "Updated description for the tenant"
};

var updateResponse = await tenantController.UpdateTenantAsync(tenantId, updateRequest);

if (updateResponse.IsSuccess)
{
    Console.WriteLine("Tenant updated successfully");
}

// Example 5: Suspend a tenant
var suspendResponse = await tenantController.SuspendTenantAsync(tenantId, "admin@acme-corp.com");

if (suspendResponse.IsSuccess)
{
    Console.WriteLine("Tenant suspended successfully");
}
```

## MigrationController

The `MigrationController` class provides REST API endpoints for database migration management, enabling schema evolution, version control, and rollback capabilities across tenant databases. It handles the creation of new migrations, tracking of migration history, application of pending migrations, and rollback of the most recent migration when safe to do so. The controller ensures schema consistency and provides audit trails for all migration operations.

### Public Members

```csharp
public sealed class MigrationController
public MigrationController(IMigrationService migrationService, ILogger<MigrationController> logger)
public async Task<ApiResponse<MigrationResponse>> CreateMigrationAsync(CreateMigrationRequest request)
public async Task<ApiResponse<IEnumerable<MigrationResponse>>> GetPendingMigrationsAsync(string databaseId)
public async Task<ApiResponse<MigrationBatchResponse>> ApplyMigrationsAsync(string databaseId, string appliedBy)
public async Task<ApiResponse<MigrationResponse>> RollbackLastMigrationAsync(string databaseId, string rollbackBy)
public async Task<ApiResponse<MigrationHistoryResponse>> GetMigrationHistoryAsync(string databaseId)
```

### Usage Example

```csharp
using SqliteMultiTenant.Api.Controllers;
using SqliteMultiTenant.Api.Requests;
using SqliteMultiTenant.Api.Responses;
using SqliteMultiTenant.Services;
using Microsoft.Extensions.Logging;

// Setup dependency injection
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<MigrationController>();

// Create migration service (would normally be injected)
var migrationService = new MigrationService(/* dependencies */);

// Create the controller instance
var migrationController = new MigrationController(migrationService, logger);

// Example 1: Create a new migration with up/down scripts
var createRequest = new CreateMigrationRequest
{
    DatabaseId = "acme-corp",
    Version = "1.2.3",
    Name = "AddTenantsTable",
    UpScript = @"
        CREATE TABLE IF NOT EXISTS Tenants (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            IsActive BOOLEAN NOT NULL DEFAULT 1
        );",
    DownScript = @"
        DROP TABLE IF EXISTS Tenants;
    "
};

var createResponse = await migrationController.CreateMigrationAsync(createRequest);

if (createResponse.IsSuccess)
{
    Console.WriteLine($"Migration created: {createResponse.Data?.MigrationId}");
    Console.WriteLine($"Version: {createResponse.Data?.Version}");
    Console.WriteLine($"Status: {createResponse.Data?.Status}");
}

// Example 2: Get pending migrations for a database
var pendingMigrations = await migrationController.GetPendingMigrationsAsync("acme-corp");

if (pendingMigrations.IsSuccess)
{
    Console.WriteLine($"Found {pendingMigrations.Data?.Count()} pending migrations");
    foreach (var migration in pendingMigrations.Data ?? Enumerable.Empty<MigrationResponse>())
    {
        Console.WriteLine($" - {migration.Version}: {migration.Name} ({migration.Status})");
    }
}

// Example 3: Apply pending migrations
var applyResponse = await migrationController.ApplyMigrationsAsync(
    databaseId: "acme-corp",
    appliedBy: "migration-admin@acme-corp.com"
);

if (applyResponse.IsSuccess)
{
    Console.WriteLine($"Applied {applyResponse.Data?.SuccessfulCount} migrations");
    Console.WriteLine($"Total migrations: {applyResponse.Data?.TotalMigrations}");
}

// Example 4: Rollback the last migration (if rollbackable)
var rollbackResponse = await migrationController.RollbackLastMigrationAsync(
    databaseId: "acme-corp",
    rollbackBy: "admin@acme-corp.com"
);

if (rollbackResponse.IsSuccess)
{
    Console.WriteLine($"Rolled back migration: {rollbackResponse.Data?.Version}");
}

// Example 5: Get complete migration history for audit purposes
var historyResponse = await migrationController.GetMigrationHistoryAsync("acme-corp");

if (historyResponse.IsSuccess)
{
    var history = historyResponse.Data;
    Console.WriteLine($"Database: {history?.DatabaseId}");
    Console.WriteLine($"Pending migrations: {history?.PendingCount}");
    Console.WriteLine($"Applied migrations: {history?.AppliedCount}");
    Console.WriteLine($"Last migration: {history?.LastMigrationDate:yyyy-MM-dd HH:mm:ss}");
}
```

## BackupController

The `BackupController` class provides REST API endpoints for comprehensive backup management and disaster recovery operations. It enables creating, verifying, restoring, and organizing backups for tenant databases, ensuring data protection compliance and enabling recovery from data loss scenarios. The controller integrates with the backup service to handle backup lifecycle operations while providing standardized API responses through the `ApiResponse<T>` wrapper.

### Public Members

```csharp
public sealed class BackupController
public BackupController(IBackupService backupService, ITenantService tenantService, ILogger<BackupController> logger)
public async Task<ApiResponse<BackupResponse>> CreateBackupAsync(string databaseId, string createdBy)
public async Task<ApiResponse<BackupResponse>> GetBackupAsync(string backupId)
public async Task<ApiResponse<IEnumerable<BackupResponse>>> ListBackupsAsync(string databaseId)
public async Task<ApiResponse<object>> VerifyBackupAsync(string backupId, string verifiedBy)
public async Task<ApiResponse<object>> RestoreBackupAsync(string backupId, string databaseId, string restoredBy)
public async Task<ApiResponse<object>> TagBackupAsync(string backupId, string tag)
```

### Usage Example

```csharp
using SqliteMultiTenant.Api.Controllers;
using SqliteMultiTenant.Api.Responses;
using SqliteMultiTenant.Services;
using Microsoft.Extensions.Logging;

// Setup dependency injection
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<BackupController>();

// Create services (would normally be injected)
var backupService = new BackupService(/* dependencies */);
var tenantService = new TenantService(/* dependencies */);

// Create the controller instance
var backupController = new BackupController(backupService, tenantService, logger);

// Example 1: Create a new backup for a tenant database
var createResponse = await backupController.CreateBackupAsync(
    databaseId: "acme-corp",
    createdBy: "admin@acme-corp.com"
);

if (createResponse.IsSuccess)
{
    Console.WriteLine($"Backup created: {createResponse.Data?.BackupId}");
    Console.WriteLine($"Backup type: {createResponse.Data?.BackupType}");
    Console.WriteLine($"Status: {createResponse.Data?.Status}");
}

// Example 2: Get backup metadata and status
var getResponse = await backupController.GetBackupAsync("backup-12345");

if (getResponse.IsSuccess)
{
    Console.WriteLine($"Backup found: {getResponse.Data?.BackupId}");
    Console.WriteLine($"Database: {getResponse.Data?.DatabaseId}");
    Console.WriteLine($"Created at: {getResponse.Data?.CreatedAt}");
    Console.WriteLine($"Size: {getResponse.Data?.SizeBytes} bytes");
}

// Example 3: List all backups for a database
var listResponse = await backupController.ListBackupsAsync("acme-corp");

if (listResponse.IsSuccess)
{
    Console.WriteLine($"Found {listResponse.Data?.Count()} backups");
    foreach (var backup in listResponse.Data ?? Enumerable.Empty<BackupResponse>())
    {
        Console.WriteLine($" - Backup {backup.BackupId}: {backup.Status} ({backup.BackupType})");
    }
}

// Example 4: Verify backup integrity
var verifyResponse = await backupController.VerifyBackupAsync(
    backupId: "backup-12345",
    verifiedBy: "backup-admin@acme-corp.com"
);

if (verifyResponse.IsSuccess)
{
    Console.WriteLine("Backup verification successful");
}

// Example 5: Restore database from backup (admin operation)
var restoreResponse = await backupController.RestoreBackupAsync(
    backupId: "backup-12345",
    databaseId: "acme-corp",
    restoredBy: "admin@acme-corp.com"
);

if (restoreResponse.IsSuccess)
{
    Console.WriteLine("Restore initiated successfully");
}

// Example 6: Tag backup for organizational purposes
var tagResponse = await backupController.TagBackupAsync(
    backupId: "backup-12345",
    tag: "production"
);

if (tagResponse.IsSuccess)
{
    Console.WriteLine("Backup tagged successfully");
}
```

## DatabaseController

The `DatabaseController` class provides REST API endpoints for database-specific operations including statistics, maintenance, integrity checks, schema inspection, and exports. It serves as a centralized controller for managing all database-level operations across tenant databases, providing insights into database health and enabling administrative tasks.

### Public Members

```csharp
public sealed class DatabaseController : ControllerBase
public DatabaseController(ILogger<DatabaseController> logger)
public IActionResult GetDatabaseStats(string databaseId)
public async Task<IActionResult> OptimizeDatabase(string databaseId)
public async Task<IActionResult> CheckIntegrity(string databaseId)
public IActionResult GetSchema(string databaseId)
public async Task<IActionResult> ExportDatabase(string databaseId, string format = "json")

public sealed class DatabaseStats
    public string DatabaseId { get; set; }
    public long FileSizeBytes { get; set; }
    public int TableCount { get; set; }
    public int IndexCount { get; set; }
    public DateTime LastVacuumTime { get; set; }
    public bool IsCorrupted { get; set; }
    public DateTime Timestamp { get; set; }

public sealed class OptimizationResult
    public string DatabaseId { get; set; }
    public long DurationMs { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
```

### Usage Example

```csharp
using SqliteMultiTenant.Api.Controllers;
using SqliteMultiTenant.Api.Responses;
using Microsoft.Extensions.Logging;

// Setup dependency injection
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<DatabaseController>();

// Create the controller instance
var databaseController = new DatabaseController(logger);

// Example 1: Get database statistics
var statsResponse = databaseController.GetDatabaseStats("acme-corp");

if (statsResponse is OkObjectResult okResult && okResult.Value is ApiResponse<DatabaseStats> statsApiResponse)
{
    var stats = statsApiResponse.Data;
    Console.WriteLine($"Database: {stats?.DatabaseId}");
    Console.WriteLine($"Size: {stats?.FileSizeBytes:N0} bytes");
    Console.WriteLine($"Tables: {stats?.TableCount}");
    Console.WriteLine($"Indexes: {stats?.IndexCount}");
    Console.WriteLine($"Last VACUUM: {stats?.LastVacuumTime:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"Corrupted: {stats?.IsCorrupted}");
}

// Example 2: Optimize database performance
var optimizeResponse = await databaseController.OptimizeDatabase("acme-corp");

if (optimizeResponse is OkObjectResult optimizeOkResult && optimizeOkResult.Value is ApiResponse<OptimizationResult> optimizeApiResponse)
{
    var result = optimizeApiResponse.Data;
    Console.WriteLine($"Optimization completed for: {result?.DatabaseId}");
    Console.WriteLine($"Duration: {result?.DurationMs}ms");
    Console.WriteLine($"Message: {result?.Message}");
}

// Example 3: Check database integrity
var integrityResponse = await databaseController.CheckIntegrity("acme-corp");

if (integrityResponse is OkObjectResult integrityOkResult && integrityOkResult.Value is ApiResponse<IntegrityCheckResult> integrityApiResponse)
{
    var result = integrityApiResponse.Data;
    Console.WriteLine($"Integrity check for: {result?.DatabaseId}");
    Console.WriteLine($"Valid: {result?.IsValid}");
    Console.WriteLine($"Errors: {result?.ErrorCount}");
    
    if (!result?.IsValid ?? false)
    {
        Console.WriteLine("Integrity issues found:");
        foreach (var error in result?.Errors ?? new List<string>())
        {
            Console.WriteLine($"  - {error}");
        }
    }
}

// Example 4: Get database schema
var schemaResponse = databaseController.GetSchema("acme-corp");

if (schemaResponse is OkObjectResult schemaOkResult && schemaOkResult.Value is ApiResponse<DatabaseSchema> schemaApiResponse)
{
    var schema = schemaApiResponse.Data;
    Console.WriteLine($"Schema for: {schema?.DatabaseId}");
    Console.WriteLine($"Tables: {schema?.Tables.Count}");
    
    foreach (var table in schema?.Tables ?? new List<TableSchema>())
    {
        Console.WriteLine($"\nTable: {table.TableName} ({table.RowCount} rows)");
        Console.WriteLine("Columns:");
        foreach (var column in table.Columns)
        {
            Console.WriteLine($"  - {column.ColumnName} ({column.DataType})" + 
                           (column.IsPrimaryKey ? " [PK]" : "") +
                           (column.IsNullable ? " [NULL]" : ""));
        }
    }
}

// Example 5: Export database
var exportResponse = await databaseController.ExportDatabase("acme-corp", "json");

if (exportResponse is OkObjectResult exportOkResult && exportOkResult.Value is ApiResponse<ExportResult> exportApiResponse)
{
    var result = exportApiResponse.Data;
    Console.WriteLine($"Export initiated for: {result?.DatabaseId}");
    Console.WriteLine($"Format: {result?.Format}");
    Console.WriteLine($"Download URL: {result?.DownloadUrl}");
    Console.WriteLine($"Expires at: {result?.ExpiresAt:yyyy-MM-dd HH:mm:ss}");
}
```

## AdminController

The `AdminController` class provides administrative endpoints for system-level operations, health monitoring, and diagnostics. It serves as the central hub for system administrators to monitor system health, retrieve performance metrics, clear caches, and access diagnostic information. All endpoints are protected and require administrative privileges.

### Public Members

```csharp
public sealed class AdminController : ControllerBase
public AdminController(HealthCheckService healthCheckService, MetricsService metricsService, ILogger<AdminController> logger)
public async Task<IActionResult> GetHealthAsync()
public IActionResult GetMetrics()
public IActionResult GetMetricsDashboard()
public IActionResult ClearCache()
public IActionResult ForceGarbageCollection()
public IActionResult GetDiagnostics()

public sealed class HealthCheckResponse
public bool IsHealthy { get; set; }
public string Status { get; set; }
public DateTime Timestamp { get; set; }
public string Version { get; set; }

public sealed class SystemMetrics
public DateTime Timestamp { get; set; }
public long ProcessMemoryMb { get; set; }
public int ThreadCount { get; set; }
public int ActiveConnections { get; set; }
public long RequestsProcessed { get; set; }
public double AverageResponseTimeMs { get; set; }
```

### Usage Example

```csharp
using SqliteMultiTenant.Api.Controllers;
using SqliteMultiTenant.Health;
using SqliteMultiTenant.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

// Setup dependency injection
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<AdminController>();

// Create services (would normally be injected)
var healthCheckService = new HealthCheckService(/* dependencies */);
var metricsService = new MetricsService(/* dependencies */);

// Create the controller instance
var adminController = new AdminController(healthCheckService, metricsService, logger);

// Example 1: Check system health
var healthResult = await adminController.GetHealthAsync();
if (healthResult is OkObjectResult okResult && okResult.Value is ApiResponse<HealthCheckResponse> healthResponse)
{
    Console.WriteLine($"System healthy: {healthResponse.Data?.IsHealthy}");
    Console.WriteLine($"Status: {healthResponse.Data?.Status}");
    Console.WriteLine($"Version: {healthResponse.Data?.Version}");
}

// Example 2: Get system metrics
var metricsResult = adminController.GetMetrics();
if (metricsResult is OkObjectResult metricsOkResult && metricsOkResult.Value is ApiResponse<SystemMetrics> metricsResponse)
{
    var metrics = metricsResponse.Data;
    Console.WriteLine($"Memory usage: {metrics?.ProcessMemoryMb} MB");
    Console.WriteLine($"Thread count: {metrics?.ThreadCount}");
    Console.WriteLine($"Requests processed: {metrics?.RequestsProcessed}");
    Console.WriteLine($"Average response time: {metrics?.AverageResponseTimeMs} ms");
}

// Example 3: Get metrics dashboard
var dashboardResult = adminController.GetMetricsDashboard();
if (dashboardResult is OkObjectResult dashboardOkResult && dashboardOkResult.Value is ApiResponse<MetricsSnapshot> dashboardResponse)
{
    var snapshot = dashboardResponse.Data;
    Console.WriteLine($"Total requests: {snapshot?.TotalRequests}");
    Console.WriteLine($"Error rate: {snapshot?.ErrorRate:P}");
}

// Example 4: Clear system cache
var clearResult = adminController.ClearCache();
if (clearResult is OkObjectResult clearOkResult && clearOkResult.Value is ApiResponse<CacheClearResult> clearResponse)
{
    Console.WriteLine($"Cache cleared: {clearResponse.Data?.Message}");
    Console.WriteLine($"Memory freed: {clearResponse.Data?.MemoryFreedBytes} bytes");
}

// Example 5: Force garbage collection
var gcResult = adminController.ForceGarbageCollection();
if (gcResult is OkObjectResult gcOkResult && gcOkResult.Value is ApiResponse<object> gcResponse)
{
    Console.WriteLine("Garbage collection completed successfully");
}

// Example 6: Get system diagnostics
var diagnosticsResult = adminController.GetDiagnostics();
if (diagnosticsResult is OkObjectResult diagnosticsOkResult && diagnosticsOkResult.Value is ApiResponse<DiagnosticsInfo> diagnosticsResponse)
{
    var diagnostics = diagnosticsResponse.Data;
    Console.WriteLine($".NET version: {diagnostics?.DotNetVersion}");
    Console.WriteLine($"OS: {diagnostics?.OSVersion}");
    Console.WriteLine($"Processor count: {diagnostics?.ProcessorCount}");
    Console.WriteLine($"Uptime: {diagnostics?.Uptime.TotalHours:F2} hours");
}
```

## SettingsController

The `SettingsController` provides a REST API for managing application settings in a multi-tenant environment. It supports retrieving, setting, removing, and batch-updating settings, with capabilities to check for setting existence and retrieve application information.

### Usage Example

```csharp
using SqliteMultiTenant.Api.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

// Setup
var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SettingsController>();
var controller = new SettingsController();

// 1. Get all settings
var allSettings = controller.GetAllSettings();

// 2. Get a single setting
var setting = controller.GetSetting("Theme");

// 3. Set/Update a setting
var setRequest = new SettingsController.SetSettingRequest { Value = "Dark" };
var setResult = controller.SetSetting("Theme", setRequest);

// 4. Batch update settings
var batchList = new List<SettingsController.SettingValue> {
    new SettingsController.SettingValue { Key = "Theme", Value = "Light", Type = "string" }
};
var batchResult = controller.UpdateBatchSettings(batchList);

// 5. Remove a setting
var removeResult = controller.RemoveSetting("Theme");

// 6. Check a setting
var exists = controller.CheckSetting("Theme");

// 7. Get App Info
var appInfo = controller.GetAppInfo();
```

## CreateTenantRequest

The `CreateTenantRequest` class represents the data transfer object used to create a new tenant in the multi-tenant SQLite system. It contains the essential tenant information required for provisioning: name, description, and contact email address. This request is validated in the controller to ensure all required fields are provided before tenant creation proceeds.

### Usage Example

```csharp
using SqliteMultiTenant.Api.Requests;
using SqliteMultiTenant.Services;
using Microsoft.Extensions.Logging;

// Create a logger
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

// Create a tenant creation request
var createRequest = new CreateTenantRequest
{
    Name = "Acme Corporation",
    Description = "Global technology solutions provider for enterprise clients",
    ContactEmail = "admin@acme-corp.com"
};

// Use the request to create a tenant
// var tenantService = new TenantService(...);
// var newTenant = await tenantService.CreateTenantAsync(createRequest);
```




### Usage Example


```csharp
using SqliteMultiTenant.Api.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

// Create interceptors for different concerns
var tenantInterceptor = new TenantContextInterceptor(
    loggerFactory.CreateLogger<TenantContextInterceptor>()
);

var validationInterceptor = new RequestValidationInterceptor(
    loggerFactory.CreateLogger<RequestValidationInterceptor>()
);

var correlationInterceptor = new CorrelationIdInterceptor(
    loggerFactory.CreateLogger<CorrelationIdInterceptor>()
);

// Example: Use interceptor pipeline in ASP.NET Core
var builder = WebApplication.CreateBuilder(args);

// Register interceptors
builder.Services.AddSingleton<IRequestInterceptor>(tenantInterceptor);
builder.Services.AddSingleton<IRequestInterceptor>(validationInterceptor);
builder.Services.AddSingleton<IRequestInterceptor>(correlationInterceptor);

var app = builder.Build();

// Middleware that uses interceptors
app.Use(async (context, next) =>
{
    // Create pipeline and register interceptors
    var pipeline = new InterceptorPipeline(
        app.Services.GetRequiredService<ILogger<InterceptorPipeline>>()
    );
    pipeline.Register(tenantInterceptor);
    pipeline.Register(validationInterceptor);
    pipeline.Register(correlationInterceptor);

    // Execute request interceptors
    if (await pipeline.ExecuteRequestInterceptorsAsync(context))
    {
        await next(context);
    }
    else
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Request validation failed");
    }

    // Execute response interceptors
    await pipeline.ExecuteResponseInterceptorsAsync(context);
});

app.Run();
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


## IConfigurationManager

The `IConfigurationManager` interface provides centralized configuration management for the multi-tenant SQLite application. It supports type-safe configuration access with default values, runtime updates, and validation. The `ConfigurationManager` implementation handles both in-memory configuration and integration with `Microsoft.Extensions.Configuration.IConfiguration` sources, making it suitable for both standalone and ASP.NET Core applications.

### Usage Example

```csharp
using SqliteMultiTenant.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ConfigurationManager>();

// Example 1: Create an in-memory configuration manager
var configManager = new ConfigurationManager(logger);

// Set configuration values
configManager.Set("Database:TimeoutSeconds", 30);
configManager.Set("Database:MaxConnections", 20);
configManager.Set("Features:MultiTenant", true);

// Get configuration values with type safety and defaults
var timeout = configManager.Get("Database:TimeoutSeconds", 15);
var maxConnections = configManager.Get("Database:MaxConnections", 10);
var featureEnabled = configManager.Get("Features:MultiTenant", false);

Console.WriteLine($"Timeout: {timeout}, MaxConnections: {maxConnections}, MultiTenant: {featureEnabled}");

// Try to get a value
if (configManager.TryGet("Database:TimeoutSeconds", out int? timeoutValue))
{
    Console.WriteLine($"Timeout value: {timeoutValue}");
}

// Check if a key exists
bool hasTimeout = configManager.Contains("Database:TimeoutSeconds");

// Remove a configuration key
configManager.Remove("Features:MultiTenant");

// Get all configuration values
var allConfig = configManager.GetAll();
foreach (var kvp in allConfig)
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}

// Example 2: Create a configuration manager with IConfiguration source
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());

// Configure multi-tenant options
services.Configure<MultiTenantOptions>(options =>
{
    options.DefaultMaxConnections = 50;
    options.BasePath = "/data/sqlite-databases";
    options.BackupRetentionDays = 30;
});

var serviceProvider = services.BuildServiceProvider();

// Create configuration manager with IConfiguration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var configManagerWithSource = new ConfigurationManager(
    configuration,
    logger,
    serviceProvider.GetRequiredService<IOptions<MultiTenantOptions>>());

// Get a tenant-specific setting
var tenantSetting = configManagerWithSource.GetTenantSetting("acme-corp", "Theme");

// Get the validated multi-tenant options
var multiTenantOptions = configManagerWithSource.GetMultiTenantOptions();
Console.WriteLine($"Max connections: {multiTenantOptions.DefaultMaxConnections}");

// Get a configuration section
var databaseSection = configManagerWithSource.GetSection("Database");
var connectionString = databaseSection.GetValue<string>("ConnectionString");
```

## ServiceCollectionExtensions

The `ServiceCollectionExtensions` class provides extension methods for registering SQLite Multi-Tenant services in the dependency injection container. It enables fluent configuration of core services, caching, event bus, integration services, monitoring, validation, and background workers through a comprehensive set of extension methods. Each method can be used independently for granular control or combined for complete service registration.

### Public Members

```csharp
public static IServiceCollection AddSqliteMultiTenantServices(this IServiceCollection services, Action<ServiceOptions>? configureOptions = null)
public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
public static IServiceCollection AddEventHandlers(this IServiceCollection services)
public static IServiceCollection AddHealthChecks(this IServiceCollection services)
public static IServiceCollection AddFormatters(this IServiceCollection services)
public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)

public sealed class ServiceOptions
public int MaxCacheItems
public int HttpClientTimeoutSeconds
public bool EnableAuiting
public bool EnableMetrics
public bool EnableEventBus
```

### Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Integration;
using Microsoft.Extensions.Logging;

// Create service collection
var services = new ServiceCollection();

// Configure logging
services.AddLogging(builder => builder.AddConsole());

// Register all core services with custom options
services.AddSqliteMultiTenantServices(options =>
{
    options.MaxCacheItems = 5000;
    options.HttpClientTimeoutSeconds = 60;
    options.EnableAuiting = true;
    options.EnableMetrics = true;
    options.EnableEventBus = true;
});

// Register exception handling services
services.AddExceptionHandling();

// Register event handlers for domain events
services.AddEventHandlers();

// Register health check services
services.AddHealthChecks();

// Register formatters for different output formats
services.AddFormatters();

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Resolve services as needed
var configurationManager = serviceProvider.GetRequiredService<IConfigurationManager>();
var webhookService = serviceProvider.GetRequiredService<WebhookService>();
var exceptionProcessor = serviceProvider.GetRequiredService<Exceptions.IExceptionProcessor>();
var healthCheckService = serviceProvider.GetRequiredService<Health.HealthCheckService>();
```

### Using Request/Response Logging Middleware

```csharp
using Microsoft.AspNetCore.Builder;
using SqliteMultiTenant.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to DI container
builder.Services.AddSqliteMultiTenantServices();

var app = builder.Build();

// Configure request/response logging middleware
app.UseRequestResponseLogging();

app.Run();
```

## ServiceConfiguration

The `ServiceConfiguration` class provides extension methods for configuring multi-tenant SQLite services in the dependency injection container. It enables centralized registration of repositories, services, and configuration options through the `SqliteMultiTenantOptions` class, supporting both basic and advanced configuration scenarios.

### Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Services;

// Create service collection
var services = new ServiceCollection();

// Basic configuration - registers all services with default options
services.AddSqliteMultiTenant(
    masterConnectionString: "Data Source=master.db;Version=3;");

// Advanced configuration - customizes multi-tenant options
services.AddSqliteMultiTenant(
    masterConnectionString: "Data Source=master.db;Version=3;",
    configureOptions: options =>
    {
        options.MaxConnections = 50;
        options.ConnectionTimeoutSeconds = 60;
        options.BackupRetentionDays = 90;
        options.EnableEncryption = true;
        options.BackupDirectory = "/secure/backups";
        options.DatabaseDirectory = "/data/sqlite-databases";
        options.EnableLogging = true;
    });

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Resolve services
var tenantService = serviceProvider.GetRequiredService<ITenantService>();
var backupService = serviceProvider.GetRequiredService<IBackupService>();
```

## ConfigurationExtensions

The `ConfigurationExtensions` class provides a comprehensive set of extension methods for working with `IConfiguration` in .NET applications. It simplifies reading configuration values with type safety, fallback handling, and validation, while supporting environment variable overrides for sensitive values. The class also includes a `ConfigurationBuilder` helper for centralized configuration setup.

### Key Features

- **Safe value retrieval** with type conversion and default values
- **Required value validation** with descriptive error messages
- **Section binding** to strongly-typed objects
- **Connection string management** with fallback support
- **Environment variable overrides** for sensitive configuration
- **Configuration validation** with required keys checking
- **Configuration export** as dictionary for debugging
- **Configuration reloading** for hot-reload scenarios

### Usage Example

```csharp
using Microsoft.Extensions.Configuration;
using SqliteMultiTenant.Configuration;

// Create configuration with multiple sources
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables("MYAPP_")
    .Build();

// Safe value retrieval with type conversion and default
var timeout = configuration.GetValueSafe<int>("Database:TimeoutSeconds", 30);
var maxConnections = configuration.GetValueSafe<int>("Database:MaxConnections", 10);
var featureEnabled = configuration.GetValueSafe<bool>("Features:MultiTenant", false);

// Required value validation - throws if missing
var requiredConnectionString = configuration.GetRequiredValue("Database:ConnectionString");

// Bind configuration section to strongly-typed object
var databaseConfig = configuration.BindSection<DatabaseSettings>("Database");

// Get connection string with fallback
var connectionString = configuration.GetConnectionStringSafe("PrimaryDatabase");

// Check if configuration key exists
bool hasLoggingEnabled = configuration.HasValue("Logging:Enabled");

// Get configuration section as dictionary for debugging
var databaseSettings = configuration.GetSectionAsDictionary("Database");

// Validate required configuration keys
var validationErrors = configuration.ValidateConfiguration(
    "Database:ConnectionString",
    "Database:TimeoutSeconds",
    "Logging:Level"
);
if (validationErrors.Any())
{
    foreach (var error in validationErrors)
    {
        Console.WriteLine($"Configuration error: {error}");
    }
}

// Get value with environment variable override
var apiKey = configuration.GetValueWithEnvironmentOverride("Api:Key");
// Will check environment variable "API_KEY" if not found in config

// Use the built-in ConfigurationBuilder helper
var standardConfig = ConfigurationExtensions.ConfigurationBuilder.BuildStandardConfiguration();
```

### Public Members

```csharp
public static T GetValueSafe<T>(this IConfiguration config, string key, T defaultValue = default)
public static string GetRequiredValue(this IConfiguration config, string key)
public static T BindSection<T>(this IConfiguration config, string sectionKey) where T : new()
public static string GetConnectionStringSafe(this IConfiguration config, string name, string defaultValue = null)
public static bool HasValue(this IConfiguration config, string key)
public static Dictionary<string, string> GetSectionAsDictionary(this IConfiguration config, string sectionKey)
public static IEnumerable<string> ValidateConfiguration(this IConfiguration config, params string[] requiredKeys)
public static void Reload(this IConfigurationRoot config)
public static string GetValueWithEnvironmentOverride(this IConfiguration config, string key, string envVar = null)

public sealed class ConfigurationBuilder
public ConfigurationBuilder()
public ConfigurationBuilder AddJsonFile(string path, bool optional = false, bool reloadOnChange = true)
public ConfigurationBuilder AddEnvironmentVariables(string prefix = null)
public ConfigurationBuilder AddInMemory(Dictionary<string, string> settings)
public IConfigurationRoot Build()
public static IConfigurationRoot BuildStandardConfiguration(string environment = null)
```

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

## TenantService

The `TenantService` class provides comprehensive tenant lifecycle management for the multi-tenant SQLite system. It handles tenant creation, retrieval, updating, deletion, activation/deactivation, suspension, and metadata management across tenant databases. The service supports both connection-per-tenant and shared-schema isolation strategies.

### Usage Example

```csharp
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;
using Microsoft.Extensions.Logging;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<TenantService>();

// Create the tenant service
var tenantService = new TenantService(
  new TenantRepository(/* database connection */),
  logger
);

// Example 1: Create a new tenant
var newTenant = await tenantService.CreateTenantAsync(new Tenant
{
  Id = "acme-corp",
  Name = "Acme Corporation",
  Description = "Global technology solutions provider",
  IsActive = true,
  Metadata = new Dictionary<string, string>
  {
    { "industry", "technology" },
    { "region", "global" }
  }
});

Console.WriteLine($"Created tenant: {newTenant.Id}");

// Example 2: Get a tenant by ID
var tenant = await tenantService.GetTenantAsync("acme-corp");
if (tenant != null)
{
  Console.WriteLine($"Found tenant: {tenant.Name} (Status: {(tenant.IsActive ? "Active" : "Inactive")})");
}

// Example 3: Update tenant information
await tenantService.UpdateTenantAsync(new Tenant
{
  Id = "acme-corp",
  Name = "Acme Corporation",
  Description = "Global technology solutions provider - Updated",
  IsActive = true,
  Metadata = new Dictionary<string, string>
  {
    { "industry", "technology" },
    { "region", "global" },
    { "employees", "5000" }
  }
});

// Example 4: Activate a tenant
await tenantService.ActivateTenantAsync("acme-corp");

// Example 5: Get all active tenants
var activeTenants = await tenantService.GetActiveTenantsAsync();
Console.WriteLine($"Active tenants: {activeTenants.Count}");

// Example 6: Search tenants by criteria
var searchResults = await tenantService.SearchTenantsAsync(
  searchTerm: "acme",
  isActive: true,
  maxResults: 10
);

// Example 7: Set tenant metadata
await tenantService.SetTenantMetadataAsync(
  tenantId: "acme-corp",
  metadata: new Dictionary<string, string>
  {
    { "subscriptionTier", "enterprise" },
    { "lastLogin", DateTime.UtcNow.ToString("o") }
  }
);

// Example 8: Get tenant database size
var storageInfo = await tenantService.GetTenantDatabaseSizeAsync("acme-corp");
Console.WriteLine($"Tenant database size: {storageInfo.SizeBytes:N0} bytes");

// Example 9: Check if tenant exists
bool exists = await tenantService.TenantExistsAsync("acme-corp");
Console.WriteLine($"Tenant exists: {exists}");

// Example 10: Get tenant count
int tenantCount = await tenantService.GetTenantCountAsync();
Console.WriteLine($"Total tenants: {tenantCount}");

// Example 11: Get all tenants
var allTenants = await tenantService.GetAllTenantsAsync();
Console.WriteLine($"All tenants: {allTenants.Count}");

// Example 12: Deactivate a tenant
await tenantService.DeactivateTenantAsync("acme-corp");

// Example 13: Delete a tenant
await tenantService.DeleteTenantAsync("acme-corp");
```

## TenantProvisioner

The `TenantProvisioner` class handles the complete lifecycle of tenant database provisioning. It creates isolated SQLite databases for each tenant with schema initialization, supports cloning for replication, and manages deprovisioning with cleanup. The provisioner supports both regular and encrypted tenant databases using SQLCipher.

### Usage Example

```csharp
using SqliteMultiTenant.Tenants;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Database;
using Microsoft.Extensions.Logging;

// Setup dependencies
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<TenantProvisioner>();
var tenantRepository = new TenantRepository(/* connection */);
var schemaManager = new SchemaManager(loggerFactory.CreateLogger<SchemaManager>(), connectionString);

// Create provisioner instance
var provisioner = new TenantProvisioner(
    tenantRepository,
    schemaManager,
    logger,
    basePath: "/data/tenants",
    loggerFactory
);

// Example 1: Provision a new tenant
var newTenant = await provisioner.ProvisionTenantAsync(
    tenantId: "acme-corp",
    tenantName: "Acme Corporation"
);
Console.WriteLine($"Provisioned tenant: {newTenant.TenantId}");

// Example 2: Clone an existing tenant for testing or backup
var clonedDbPath = await provisioner.CloneTenantAsync(
    sourceTenantId: "acme-corp",
    targetTenantId: "acme-corp-test"
);
Console.WriteLine($"Cloned database to: {clonedDbPath}");

// Example 3: Provision an encrypted tenant (requires SQLCipher)
var encryptedTenant = await provisioner.ProvisionEncryptedTenantAsync(
    tenantId: "secure-tenant",
    tenantName: "Secure Tenant",
    encryptionKey: "my-secret-key-123"
);
Console.WriteLine($"Provisioned encrypted tenant: {encryptedTenant.TenantId}");

// Example 4: Validate tenant database integrity
bool isValid = await provisioner.ValidateTenantDatabaseAsync("acme-corp");
Console.WriteLine($"Database valid: {isValid}");

// Example 5: Deprovision a tenant (irreversible operation)
bool deprovisioned = await provisioner.DeprovisionTenantAsync(
    tenantId: "acme-corp-test",
    deleteBackups: true
);
Console.WriteLine($"Tenant deprovisioned: {deprovisioned}");
```

## SchemaManager

The `SchemaManager` class provides centralized schema management for SQLite databases, enabling safe schema modifications, table operations, and index management. It handles schema initialization, column additions, table renaming, and index creation with built-in validation to prevent duplicates and ensure idempotent operations.

### Public Members

```csharp
public sealed class SchemaManager
public SchemaManager(ILogger<SchemaManager> logger, string connectionString)
public async Task InitializeSchemaAsync(string tenantId)
public async Task<bool> AddColumnAsync(string tenantId, string tableName, string columnName, string columnDefinition)
public async Task RenameTableAsync(string oldTableName, string newTableName)
public async Task<bool> CreateIndexAsync(string tableName, string indexName, params string[] columns)
public async Task<List<string>> GetTablesAsync()
```

### Usage Example

```csharp
using SqliteMultiTenant.Database;
using Microsoft.Extensions.Logging;
using System.Data.SQLite;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<SchemaManager>();

// Create the schema manager with connection string
var schemaManager = new SchemaManager(
    logger,
    "Data Source=acme-corp.db;Version=3;"
);

// Example 1: Initialize the standard multi-tenant schema
await schemaManager.InitializeSchemaAsync("acme-corp");

// Example 2: Add a new column to an existing table
bool columnAdded = await schemaManager.AddColumnAsync(
    tenantId: "acme-corp",
    tableName: "Customers",
    columnName: "LastLoginDate",
    columnDefinition: "TEXT NULL"
);

if (columnAdded)
{
    Console.WriteLine("Column added successfully");
}

// Example 3: Rename a table
await schemaManager.RenameTableAsync(
    oldTableName: "OldCustomers",
    newTableName: "LegacyCustomers"
);

// Example 4: Create an index on frequently queried columns
bool indexCreated = await schemaManager.CreateIndexAsync(
    tableName: "Orders",
    indexName: "idx_Orders_CustomerId_Date",
    columns: new[] { "CustomerId", "OrderDate" }
);

if (indexCreated)
{
    Console.WriteLine("Index created successfully");
}

// Example 5: Get all tables in the database
var tables = await schemaManager.GetTablesAsync();
foreach (var table in tables)
{
    Console.WriteLine($"Table: {table}");
}
```

## MigrationService

The `MigrationService` class provides comprehensive database migration management for the multi-tenant SQLite system. It handles the creation, execution, tracking, and rollback of database migrations across tenant databases, enabling schema evolution and data transformations while maintaining audit trails and version control.

### Usage Example

```csharp
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;
using Microsoft.Extensions.Logging;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<MigrationService>();

// Create the migration service
var migrationService = new MigrationService(
    new MigrationRepository(/* database connection */),
    logger
);

// Example 1: Create a new migration
var migration = await migrationService.CreateMigrationAsync(
    databaseId: "acme-corp-db",
    version: "1.2.3",
    name: "AddTenantsTable",
    upScript: @"
CREATE TABLE IF NOT EXISTS Tenants (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN NOT NULL DEFAULT 1
);",
    downScript: @"
DROP TABLE IF EXISTS Tenants;
"
);

Console.WriteLine($"Migration created: {migration.MigrationId}");

// Example 2: Execute a migration
await migrationService.ExecuteMigrationAsync(
    migrationId: migration.MigrationId,
    executedBy: "migration-service"
);

// Simulate migration execution time
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// Execute database schema changes here...
System.Threading.Thread.Sleep(150); // Simulate work
stopwatch.Stop();

// Mark migration as completed
await migrationService.MarkMigrationAsCompletedAsync(
    migrationId: migration.MigrationId,
    executionTimeMs: stopwatch.ElapsedMilliseconds
);

// Example 3: Check if a migration is applied
bool isApplied = await migrationService.IsMigrationAppliedAsync(
    databaseId: "acme-corp-db",
    version: "1.2.3"
);

Console.WriteLine($"Migration is applied: {isApplied}");

// Example 4: Get all migrations for a database
var allMigrations = await migrationService.GetDatabaseMigrationsAsync("acme-corp-db");
foreach (var m in allMigrations)
{
    Console.WriteLine($"Migration: {m.Name} - Status: {m.Status}");
}

// Example 5: Get pending migrations
var pendingMigrations = await migrationService.GetPendingMigrationsAsync("acme-corp-db");
Console.WriteLine($"Pending migrations: {pendingMigrations.Count}");

// Example 6: Rollback a migration (if rollbackable)
if (migration.IsRollbackable)
{
    await migrationService.RollbackMigrationAsync(
        migrationId: migration.MigrationId,
        executedBy: "rollback-service"
    );
}

// Example 7: Get migration count
int migrationCount = await migrationService.GetMigrationCountAsync("acme-corp-db");
Console.WriteLine($"Total migrations: {migrationCount}");
```

## MultiTenantOptions

The `MultiTenantOptions` class provides centralized configuration for multi-tenant SQLite database operations. It controls connection pooling, backup scheduling, performance monitoring, data encryption, caching behavior, and rate limiting across all tenant databases. Configure these options through dependency injection or programmatically to tailor the system to your specific multi-tenant requirements.

### Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Services;

// Create service collection
var services = new ServiceCollection();

// Configure multi-tenant options with custom settings
services.Configure<MultiTenantOptions>(options =>
{
    options.BasePath = "/var/lib/sqlite-databases";
    options.DefaultMaxConnections = 50;
    options.MaxConnectionsPerTenant = 20;
    options.MaxBackupCount = 30;
    options.BackupRetention = TimeSpan.FromDays(90);
    options.EnableBackupScheduling = true;
    options.BackupInterval = TimeSpan.FromHours(2);
    options.EnableAuditLogging = true;
    options.EnablePerformanceMonitoring = true;
    options.EnableDataEncryption = true;
    options.EncryptionKeyPath = "/etc/sqlite-keys";
    options.MaxCacheSize = 5000;
    options.DefaultCacheTTL = TimeSpan.FromHours(2);
    options.RateLimitRequestsPerMinute = 5000;
    options.VerboseLogging = false;
});

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Resolve services that use MultiTenantOptions
var tenantService = serviceProvider.GetRequiredService<ITenantService>();
var backupService = serviceProvider.GetRequiredService<IBackupService>();
```

## DependencyInjectionSetup

The `DependencyInjectionSetup` class provides centralized dependency injection configuration for the multi-tenant SQLite application. It follows the composition root pattern to register all application services including API controllers, middleware, caching, events, formatters, validation, health checks, background workers, and integration services. The class provides both granular service registration methods and a convenience `AddPhase2Services` method that registers all services in one call.

### Public Members

```csharp
public static class DependencyInjectionSetup
public static IServiceCollection AddApiControllers(this IServiceCollection services)
public static IServiceCollection AddMiddlewareServices(this IServiceCollection services)
public static IServiceCollection AddCachingServices(this IServiceCollection services)
public static IServiceCollection AddEventServices(this IServiceCollection services)
public static IServiceCollection AddFormatterServices(this IServiceCollection services)
public static IServiceCollection AddValidationServices(this IServiceCollection services)
public static IServiceCollection AddHealthCheckServices(this IServiceCollection services, string databasePath = ".")
public static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
public static IServiceCollection AddIntegrationServices(this IServiceCollection services)
public static IServiceCollection AddPhase2Services(
    this IServiceCollection services,
    string databasePath = ".")
public sealed class MultiTenantOptionsBuilder
public MultiTenantOptionsBuilder WithBackupRetention(int days)
public MultiTenantOptionsBuilder WithMaxConnections(int count)
public MultiTenantOptionsBuilder WithConnectionTimeout(int seconds)
public MultiTenantOptionsBuilder WithEncryption(bool enabled)
public MultiTenantOptionsBuilder WithBackupDirectory(string path)
public MultiTenantOptionsBuilder WithDatabaseDirectory(string path)
public MultiTenantOptionsBuilder WithLogging(bool enabled)
public SqliteMultiTenantOptions Build()
```

### Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Api.Controllers;
using Microsoft.Extensions.Logging;

// Create service collection
var services = new ServiceCollection();

// Configure logging
services.AddLogging(builder => builder.AddConsole());

// Register all Phase 2 services in one call
services.AddPhase2Services(databasePath: "/data/sqlite-databases");

// Or register services individually for more control
services.AddApiControllers();
services.AddMiddlewareServices();
services.AddCachingServices();
services.AddEventServices();
services.AddFormatterServices();
services.AddValidationServices();
services.AddHealthCheckServices(databasePath: "/data/sqlite-databases");
services.AddBackgroundWorkers();
services.AddIntegrationServices();

// Build the service provider
var serviceProvider = services.BuildServiceProvider();

// Resolve services as needed
var tenantController = serviceProvider.GetRequiredService<TenantController>();
var backupController = serviceProvider.GetRequiredService<BackupController>();

// Example: Configure multi-tenant options using the builder pattern
var options = new MultiTenantOptionsBuilder()
    .WithBackupRetention(days: 30)
    .WithMaxConnections(count: 100)
    .WithConnectionTimeout(seconds: 30)
    .WithEncryption(enabled: true)
    .WithBackupDirectory(path: "/backups")
    .WithDatabaseDirectory(path: "/data/sqlite-databases")
    .WithLogging(enabled: true)
    .Build();

Console.WriteLine($"Multi-tenant options configured: MaxConnections={options.MaxConnections}, " +
    $"BackupRetentionDays={options.BackupRetentionDays}");
```

### Usage Example

```csharp
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;
using Microsoft.Extensions.Logging;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<MigrationService>();

// Create the migration service
var migrationService = new MigrationService(
    new MigrationRepository(/* database connection */),
    logger
);

// Example 1: Create a new migration
var migration = await migrationService.CreateMigrationAsync(
    databaseId: "acme-corp-db",
    version: "1.2.3",
    name: "AddTenantsTable",
    upScript: @"
CREATE TABLE IF NOT EXISTS Tenants (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN NOT NULL DEFAULT 1
);",
    downScript: @"
DROP TABLE IF EXISTS Tenants;
"
);

Console.WriteLine($"Migration created: {migration.MigrationId}");

// Example 2: Execute a migration
await migrationService.ExecuteMigrationAsync(
    migrationId: migration.MigrationId,
    executedBy: "migration-service"
);

// Simulate migration execution time
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// Execute database schema changes here...
System.Threading.Thread.Sleep(150); // Simulate work
stopwatch.Stop();

// Mark migration as completed
await migrationService.MarkMigrationAsCompletedAsync(
    migrationId: migration.MigrationId,
    executionTimeMs: stopwatch.ElapsedMilliseconds
);

// Example 3: Check if a migration is applied
bool isApplied = await migrationService.IsMigrationAppliedAsync(
    databaseId: "acme-corp-db",
    version: "1.2.3"
);

Console.WriteLine($"Migration is applied: {isApplied}");

// Example 4: Get all migrations for a database
var allMigrations = await migrationService.GetDatabaseMigrationsAsync("acme-corp-db");
foreach (var m in allMigrations)
{
    Console.WriteLine($"Migration: {m.Name} - Status: {m.Status}");
}

// Example 5: Get pending migrations
var pendingMigrations = await migrationService.GetPendingMigrationsAsync("acme-corp-db");
Console.WriteLine($"Pending migrations: {pendingMigrations.Count}");

// Example 6: Rollback a migration (if rollbackable)
if (migration.IsRollbackable)
{
    await migrationService.RollbackMigrationAsync(
        migrationId: migration.MigrationId,
        executedBy: "rollback-service"
    );
}

// Example 7: Get migration count
int migrationCount = await migrationService.GetMigrationCountAsync("acme-corp-db");
Console.WriteLine($"Total migrations: {migrationCount}");
```

## TenantRecoveryService

The `TenantRecoveryService` class provides disaster recovery capabilities for tenant databases, enabling database repair, backup restoration, stale backup cleanup, and point-in-time recovery operations. This service is essential for maintaining database integrity and recovering from corruption, accidental data loss, or other disasters.

### Public Members

```csharp
public sealed class TenantRecoveryService
public TenantRecoveryService(ITenantRepository tenantRepository, ILogger<TenantRecoveryService> logger)
public async Task<bool> RepairDatabaseAsync(string tenantId)
public async Task<bool> RestoreFromBackupAsync(string tenantId, string backupPath)
public async Task<int> CleanupStaleBackupsAsync(string tenantId, TimeSpan retentionPeriod)
public async Task<bool> PointInTimeRecoveryAsync(string tenantId, DateTime targetTime, string backupDirectory)
```

### Usage Example

```csharp
using SqliteMultiTenant.Tenants;
using SqliteMultiTenant.Repositories;
using Microsoft.Extensions.Logging;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<TenantRecoveryService>();

// Create required dependencies
var tenantRepository = new TenantRepository(/* database connection */);

// Create the recovery service instance
var recoveryService = new TenantRecoveryService(tenantRepository, logger);

// Example 1: Repair a corrupted database
bool repairSuccess = await recoveryService.RepairDatabaseAsync("acme-corp");
Console.WriteLine($"Database repair successful: {repairSuccess}");

// Example 2: Restore from a backup
bool restoreSuccess = await recoveryService.RestoreFromBackupAsync(
    tenantId: "acme-corp",
    backupPath: "/backups/acme-corp-2024-07-16.db.backup"
);
Console.WriteLine($"Database restore successful: {restoreSuccess}");

// Example 3: Cleanup stale backups (older than 30 days)
int deletedCount = await recoveryService.CleanupStaleBackupsAsync(
    tenantId: "acme-corp",
    retentionPeriod: TimeSpan.FromDays(30)
);
Console.WriteLine($"Deleted {deletedCount} stale backup files");

// Example 4: Perform point-in-time recovery
bool recoverySuccess = await recoveryService.PointInTimeRecoveryAsync(
    tenantId: "acme-corp",
    targetTime: new DateTime(2024, 7, 15, 14, 30, 0),
    backupDirectory: "/backups"
);
Console.WriteLine($"Point-in-time recovery successful: {recoverySuccess}");
```

### Usage Example

```csharp
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;
using Microsoft.Extensions.Logging;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<MigrationService>();

// Create the migration service
var migrationService = new MigrationService(
    new MigrationRepository(/* database connection */),
    logger
);

// Example 1: Create a new migration
var migration = await migrationService.CreateMigrationAsync(
    databaseId: "acme-corp-db",
    version: "1.2.3",
    name: "AddTenantsTable",
    upScript: @"
        CREATE TABLE IF NOT EXISTS Tenants (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            IsActive BOOLEAN NOT NULL DEFAULT 1
        );",
    downScript: @"
        DROP TABLE IF EXISTS Tenants;
    "
);

Console.WriteLine($"Migration created: {migration.MigrationId}");

// Example 2: Execute a migration
await migrationService.ExecuteMigrationAsync(
    migrationId: migration.MigrationId,
    executedBy: "migration-service"
);

// Simulate migration execution time
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// Execute database schema changes here...
System.Threading.Thread.Sleep(150); // Simulate work
stopwatch.Stop();

// Mark migration as completed
await migrationService.MarkMigrationAsCompletedAsync(
    migrationId: migration.MigrationId,
    executionTimeMs: stopwatch.ElapsedMilliseconds
);

// Example 3: Check if a migration is applied
bool isApplied = await migrationService.IsMigrationAppliedAsync(
    databaseId: "acme-corp-db",
    version: "1.2.3"
);

Console.WriteLine($"Migration is applied: {isApplied}");

// Example 4: Get all migrations for a database
var allMigrations = await migrationService.GetDatabaseMigrationsAsync("acme-corp-db");
foreach (var m in allMigrations)
{
    Console.WriteLine($"Migration: {m.Name} - Status: {m.Status}");
}

// Example 5: Get pending migrations
var pendingMigrations = await migrationService.GetPendingMigrationsAsync("acme-corp-db");
Console.WriteLine($"Pending migrations: {pendingMigrations.Count}");

// Example 6: Rollback a migration (if rollbackable)
if (migration.IsRollbackable)
{
    await migrationService.RollbackMigrationAsync(
        migrationId: migration.MigrationId,
        executedBy: "rollback-service"
    );
}

// Example 7: Get migration count
int migrationCount = await migrationService.GetMigrationCountAsync("acme-corp-db");
Console.WriteLine($"Total migrations: {migrationCount}");
```

## TenantIsolationVerifier

The `TenantIsolationVerifier` class provides comprehensive verification of tenant isolation boundaries in multi-tenant SQLite databases. It validates that tenant data remains isolated across different isolation strategies (connection-per-tenant and shared-schema), detects potential data leakage between tenants, and ensures query-level tenant isolation is properly enforced.


### Usage Example

```csharp
using SqliteMultiTenant.Tenants;
using Microsoft.Extensions.Logging;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<TenantIsolationVerifier>();

// Create the isolation verifier instance
var verifier = new TenantIsolationVerifier(
    new TenantDatabaseService(/* dependencies */),
    logger
);

// Example 1: Verify tenant isolation for a specific tenant
var verificationResult = await verifier.VerifyTenantIsolationAsync("acme-corp");

Console.WriteLine($"Tenant: {verificationResult.TenantId}");
Console.WriteLine($"Isolation valid: {verificationResult.IsIsolated}");
Console.WriteLine($"Audit log isolation: {(verificationResult.AuditLogIsolationValid ? "PASS" : "FAIL")}");
Console.WriteLine($"Connection restriction: {(verificationResult.ConnectionRestrictionValid ? "PASS" : "FAIL")}");
Console.WriteLine($"Query isolation: {(verificationResult.QueryIsolationValid ? "PASS" : "FAIL")}");
Console.WriteLine($"Verified at: {verificationResult.VerifiedAt}");

// Example 2: Detect potential data leaks across tenants
var dataLeaks = await verifier.DetectPotentialDataLeaksAsync();

if (dataLeaks.Any())
{
    Console.WriteLine($"Found {dataLeaks.Count} potential data leakage issues:");
    foreach (var leak in dataLeaks)
    {
        Console.WriteLine($" - [{leak.Severity}] {leak.Type}: {leak.Description}");
    }
}
else
{
    Console.WriteLine("No data leakage detected ✓");
}

// Example 3: Validate that a specific query enforces tenant isolation
var queryValidation = await verifier.ValidateQueryTenantIsolationAsync(
    tenantId: "acme-corp",
    query: "SELECT * FROM Invoices WHERE TenantId = @tenantId"
);

Console.WriteLine($"Query validation for tenant '{queryValidation.TenantId}':");
Console.WriteLine($"Query contains tenant filter: {queryValidation.ContainsTenantFilter}");
Console.WriteLine($"Query: {queryValidation.Query}");
```

## ConnectionManager

The `ConnectionManager` class provides centralized connection pooling and lifecycle management for per-tenant SQLite databases. It efficiently manages database connections across multiple tenants, enabling connection reuse to minimize resource overhead and improve performance. The manager supports both regular and encrypted connections (using SQLCipher), and provides monitoring capabilities through pool statistics.

### Public Members

```csharp
public sealed class ConnectionManager : IDisposable
public ConnectionManager(ILogger<ConnectionManager> logger, int maxConnectionsPerPool = 10)
public async Task<SQLiteConnection> GetConnectionAsync(string tenantId, string connectionString, CancellationToken cancellationToken = default)
public async Task<SQLiteConnection> GetEncryptedConnectionAsync(string tenantId, string connectionString, string encryptionKey, CancellationToken cancellationToken = default)
public async Task ReleaseConnectionAsync(string tenantId, SQLiteConnection connection)
public async Task ClearTenantPoolAsync(string tenantId)
public Dictionary<string, PoolStatistics> GetPoolStatistics()
public void Dispose()

private class ConnectionPool : IAsyncDisposable
public async Task<SQLiteConnection> GetConnectionAsync(CancellationToken cancellationToken)
public async Task ReleaseConnectionAsync(SQLiteConnection connection)
public ValueTask DisposeAsync()
public void Dispose()

public sealed class PoolStatistics
public string TenantId { get; set; }
public int AvailableConnections { get; set; }
public int TotalConnections { get; set; }
public int WaitingRequests { get; set; }
```

### Usage Example

```csharp
using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Database;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ConnectionManager>();

// Create the connection manager with default pool size (10 connections per tenant)
var connectionManager = new ConnectionManager(logger);

// Example 1: Get a regular connection for a tenant
var regularConnection = await connectionManager.GetConnectionAsync(
    tenantId: "acme-corp",
    connectionString: "Data Source=acme-corp.db;Version=3;"
);

// Use the connection for database operations
using var command = regularConnection.CreateCommand();
command.CommandText = "SELECT COUNT(*) FROM Invoices WHERE TenantId = @tenantId";
command.Parameters.AddWithValue("@tenantId", "acme-corp");
var count = await command.ExecuteScalarAsync();

// Release the connection back to the pool when done
await connectionManager.ReleaseConnectionAsync("acme-corp", regularConnection);

// Example 2: Get an encrypted connection (requires SQLCipher package)
var encryptedConnection = await connectionManager.GetEncryptedConnectionAsync(
    tenantId: "secure-tenant",
    connectionString: "Data Source=secure-tenant.db;Version=3;",
    encryptionKey: "my-secret-key-1234567890abcdef"
);

// Use the encrypted connection for sensitive operations
using var secureCommand = encryptedConnection.CreateCommand();
secureCommand.CommandText = "SELECT * FROM SensitiveData";
var results = await secureCommand.ExecuteReaderAsync();

// Release the encrypted connection
await connectionManager.ReleaseConnectionAsync("secure-tenant", encryptedConnection);

// Example 3: Monitor connection pool statistics
var statistics = connectionManager.GetPoolStatistics();
foreach (var stat in statistics)
{
    Console.WriteLine($"Tenant: {stat.Key}");
    Console.WriteLine($" Available connections: {stat.Value.AvailableConnections}");
    Console.WriteLine($" Total connections: {stat.Value.TotalConnections}");
    Console.WriteLine($" Waiting requests: {stat.Value.WaitingRequests}");
}

// Example 4: Clear a tenant's connection pool (e.g., during tenant deletion)
await connectionManager.ClearTenantPoolAsync("acme-corp");

// Example 5: Dispose the connection manager when shutting down the application
connectionManager.Dispose();
```

## BackupService

The `BackupService` class provides comprehensive backup management for multi-tenant SQLite databases. It handles creation, tracking, verification, and rotation of database backups with support for both full and incremental backups, progress reporting, and automated cleanup based on retention policies.

### Usage Example

```csharp
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;
using Microsoft.Extensions.Logging;
using System.Data.SQLite;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<BackupService>();

// Create the backup service
var backupService = new BackupService(
  new BackupRepository(/* database connection */),
  logger
);

// Example 1: Create a new backup
var backup = await backupService.CreateBackupAsync(
  databaseId: "acme-corp",
  backupType: BackupType.Full,
  createdBy: "backup-service"
);

Console.WriteLine($"Backup created: {backup.BackupId}");

// Example 2: Execute the backup process (copy database file)
var sourcePath = "acme-corp.db";
var destinationPath = backup.BackupPath;

// Create progress reporter
var progress = new Progress<BackupProgress>();
progress.ProgressChanged += (sender, progressArgs) => {
  Console.WriteLine($"Backup progress: {progressArgs.PercentComplete:F1}% " +
                  $"({progressArgs.PagesCopied}/{progressArgs.TotalPages} pages)");
};

// Perform the backup with progress tracking
await backupService.BackupWithProgressAsync(
  sourceDatabasePath: sourcePath,
  destinationPath: destinationPath,
  progress: progress
);

// Mark backup as completed with size and duration
var fileInfo = new FileInfo(destinationPath);
await backupService.MarkBackupAsCompletedAsync(
  backupId: backup.BackupId,
  sizeBytes: fileInfo.Length,
  durationMs: 1250
);

// Example 3: Verify the backup integrity
await backupService.VerifyBackupAsync(
  backupId: backup.BackupId,
  verifiedBy: "backup-verifier"
);

// Example 4: Add tags to the backup
await backupService.AddBackupTagAsync(backup.BackupId, "daily");
await backupService.AddBackupTagAsync(backup.BackupId, "full");

// Example 5: Get backup information
var retrievedBackup = await backupService.GetBackupAsync(backup.BackupId);
Console.WriteLine($"Backup status: {retrievedBackup?.Status}");
Console.WriteLine($"Backup size: {retrievedBackup?.SizeBytes} bytes");

// Example 6: List all backups for a database
var allBackups = await backupService.GetDatabaseBackupsAsync("acme-corp");
Console.WriteLine($"Total backups: {allBackups.Count}");

// Example 7: Get the latest backup
var latestBackup = await backupService.GetLatestBackupAsync("acme-corp");
Console.WriteLine($"Latest backup: {latestBackup?.BackupId}");

// Example 8: Set custom expiration date
await backupService.SetBackupExpirationAsync(
  backupId: backup.BackupId,
  expirationDate: DateTime.UtcNow.AddDays(90)
);

// Example 9: Count backups for a database
int backupCount = await backupService.GetBackupCountAsync("acme-corp");
Console.WriteLine($"Backup count: {backupCount}");

// Example 10: Delete expired backups
var expiredBackups = await backupService.GetExpiredBackupsAsync();
foreach (var expiredBackup in expiredBackups)
{
  await backupService.DeleteBackupAsync(expiredBackup.BackupId);
}
```

## IConnectionPoolManager

The `IConnectionPoolManager` interface provides a centralized mechanism for managing per-tenant SQLite connection pools. It handles connection acquisition, release, and eviction while enforcing configurable pool-size limits and automatically pruning idle or long-lived connections. This ensures efficient resource utilization and prevents connection leaks in multi-tenant applications.

### Public Members

```csharp
public interface IConnectionPoolManager : IAsyncDisposable
public async Task<SQLiteConnection> AcquireAsync(string tenantId, string connectionString, CancellationToken cancellationToken = default)
public Task ReleaseAsync(string tenantId, SQLiteConnection connection)
public Task EvictTenantAsync(string tenantId)
public IReadOnlyDictionary<string, PoolStatisticsSnapshot> GetStatistics()
```

### Usage Example

```csharp
using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Database;

// Create a logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ConnectionPoolManager>();

// Configure connection pool options
var poolOptions = new ConnectionPoolOptions
{
    MaxPoolSize = 20,
    MinPoolSize = 5,
    IdleTimeout = TimeSpan.FromMinutes(5),
    MaxConnectionLifetime = TimeSpan.FromHours(1),
    AcquireTimeout = TimeSpan.FromSeconds(30),
    PruneInterval = TimeSpan.FromMinutes(1)
};

// Create the connection pool manager
var connectionPoolManager = new ConnectionPoolManager(poolOptions, logger);

// Example 1: Acquire a connection for a tenant
var connectionString = "Data Source=acme-corp.db;Version=3;";
var connection = await connectionPoolManager.AcquireAsync("acme-corp", connectionString);

// Use the connection for database operations
using var command = connection.CreateCommand();
command.CommandText = "SELECT COUNT(*) FROM Invoices WHERE TenantId = @tenantId";
command.Parameters.AddWithValue("@tenantId", "acme-corp");
var count = await command.ExecuteScalarAsync();

// Example 2: Release the connection back to the pool when done
await connectionPoolManager.ReleaseAsync("acme-corp", connection);

// Example 3: Get statistics for all tenant pools
var statistics = connectionPoolManager.GetStatistics();
foreach (var stat in statistics)
{
    Console.WriteLine($"Tenant: {stat.Key}");
    Console.WriteLine($"  Available connections: {stat.Value.Available}");
    Console.WriteLine($"  Total connections: {stat.Value.Total}");
    Console.WriteLine($"  Waiting for connections: {stat.Value.Waiting}");
    Console.WriteLine($"  Pruned connections: {stat.Value.PrunedTotal}");
}

// Example 4: Evict a tenant's connections (e.g., during tenant deletion)
await connectionPoolManager.EvictTenantAsync("acme-corp");

// Example 5: Dispose the pool manager when shutting down the application
await connectionPoolManager.DisposeAsync();
```

## ConflictResolutionService

The `ConflictResolutionService` class provides conflict detection and resolution capabilities for multi-tenant SQLite databases. It handles scenarios where data has been modified both locally and remotely, allowing you to detect conflicts, apply resolution strategies, and persist the resolved values back to the database. This is particularly useful for merge operations, data synchronization workflows, and handling concurrent updates from different sources.

## ApiResponseBuilder

The `ApiResponseBuilder<T>` class provides a fluent interface for constructing consistent, well-structured API responses with standardized error handling and metadata support. It enables building responses with proper HTTP status codes, success/failure states, detailed error information, and custom metadata through a clean builder pattern.

### Usage Example

```csharp
using SqliteMultiTenant.Api;
using System.Net;

// Create a new response builder
var responseBuilder = new ApiResponseBuilder<object>();

// Build a successful response with data
var successResponse = responseBuilder
    .WithStatusCode(HttpStatusCode.OK)
    .WithMessage("User retrieved successfully")
    .WithData(new { Id = 123, Name = "John Doe", Email = "john@example.com" })
    .Success()
    .Build();

Console.WriteLine($"Status: {successResponse.StatusCode}");
Console.WriteLine($"Success: {successResponse.IsSuccess}");
Console.WriteLine($"Message: {successResponse.Message}");

// Build an error response with validation errors
var validationResponse = new ApiResponseBuilder<object>()
    .WithStatusCode(HttpStatusCode.BadRequest)
    .WithMessage("Validation failed")
    .AddError("Email is required", "VALIDATION_ERROR", "email")
    .AddError("Password must be at least 8 characters", "VALIDATION_ERROR", "password")
    .ValidationError()
    .Build();

Console.WriteLine($"Errors: {string.Join(", ", validationResponse.Errors.Select(e => e.Message))}");

// Build a not found response
var notFoundResponse = new ApiResponseBuilder<object>()
    .WithStatusCode(HttpStatusCode.NotFound)
    .NotFound("User with ID 999 not found")
    .Build();

// Build a server error response from an exception
var errorResponse = ApiResponseBuilder<object>.ExceptionResponseBuilder
    .FromException(new InvalidOperationException("Database connection failed"))
    .WithMessage("Database operation failed")
    .Build();

// Build a response with metadata
var responseWithMetadata = new ApiResponseBuilder<Dictionary<string, object>>()
    .WithStatusCode(HttpStatusCode.OK)
    .WithMessage("Operation completed")
    .WithData(new Dictionary<string, object> { { "users", 42 }, { "active", true } })
    .AddMetadata("page", 1)
    .AddMetadata("pageSize", 10)
    .AddMetadata("total", 420)
    .Success()
    .Build();
```

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

// Create a bulk insert builder for the "Customers" table
var bulkInsert = new BulkInsertBuilder(connection, logger, "Customers", batchSize: 500);

// Add records to the batch
bulkInsert.AddRecord(new Dictionary<string, object>
{
    {"Id", 1},
    {"Name", "Acme Corporation"},
    {"Email", "contact@acme.com"}
});

bulkInsert.AddRecord(new Dictionary<string, object>
{
    {"Id", 2},
    {"Name", "Globex Corporation"},
    {"Email", "info@globex.com"}
});

// Execute the bulk insert
var result = await bulkInsert.ExecuteAsync();

if (result.IsSuccessful)
{
    Console.WriteLine($"Inserted {result.InsertedRecords} records successfully");
}
else
{
    Console.WriteLine($"Error: {result.Error}");
}

// Clean up
connection.Close();
```

## ConnectionPoolOptions

The `ConnectionPoolOptions` class provides configuration for managing per-tenant SQLite connection pools. It controls pool sizing, idle connection behavior, and connection lifetime management to optimize database resource usage and prevent connection exhaustion.

### Public Members

```csharp
public int MinPoolSize
public int MaxPoolSize
public TimeSpan IdleTimeout
public TimeSpan AcquireTimeout
public TimeSpan MaxConnectionLifetime
public TimeSpan PruneInterval
public void Validate()
```

### Usage Example

```csharp
using SqliteMultiTenant.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Configure connection pooling options at application startup
var services = new ServiceCollection();

services.Configure<ConnectionPoolOptions>(options =>
{
    options.MinPoolSize = 2;           // Keep at least 2 connections alive per tenant
    options.MaxPoolSize = 20;          // Allow up to 20 concurrent connections per tenant
    options.IdleTimeout = TimeSpan.FromMinutes(10);  // Idle connections expire after 10 minutes
    options.AcquireTimeout = TimeSpan.FromSeconds(45); // Wait up to 45 seconds for a connection
    options.MaxConnectionLifetime = TimeSpan.FromHours(2); // Replace connections after 2 hours
    options.PruneInterval = TimeSpan.FromSeconds(30);  // Prune idle connections every 30 seconds
});

var serviceProvider = services.BuildServiceProvider();
var poolOptions = serviceProvider.GetRequiredService<IOptions<ConnectionPoolOptions>>().Value;

// Validate configuration before use
poolOptions.Validate();

// Register the connection pool manager with DI
services.AddSingleton<ConnectionPoolManager>();

// Example: Create a connection pool manager with custom options
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ConnectionPoolManager>();

var customOptions = new ConnectionPoolOptions
{
    MinPoolSize = 1,
    MaxPoolSize = 15,
    IdleTimeout = TimeSpan.FromMinutes(5),
    AcquireTimeout = TimeSpan.FromSeconds(30),
    MaxConnectionLifetime = TimeSpan.FromHours(1),
    PruneInterval = TimeSpan.FromSeconds(60)
};

var poolManager = new ConnectionPoolManager(customOptions, logger);
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

## PerformanceMiddleware

The `PerformanceMiddleware` class provides request performance monitoring and metrics collection for ASP.NET Core applications. It automatically records request timing, memory usage, and other performance metrics for every HTTP request, enabling performance analysis, monitoring, and optimization. The middleware integrates with `PerformanceMonitor` to aggregate and retrieve statistics across multiple requests.

### Usage Example

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Middleware;
using SqliteMultiTenant.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

// Register PerformanceMiddleware early in the pipeline to monitor all requests
builder.Services.AddSingleton<PerformanceMonitor>();
var app = builder.Build();

app.UsePerformanceMiddleware();

// Your endpoints and middleware configuration
app.MapGet("/api/tenants/{id}", async (string id, PerformanceMonitor performanceMonitor) =>
{
    // Simulate some work
    await Task.Delay(50);
    
    // The middleware automatically recorded metrics for this request
    // You can access aggregated performance statistics
    var stats = await performanceMonitor.GetStatsAsync();
    
    return Results.Ok(new {
        TenantId = id,
        AverageResponseTime = stats.AverageElapsedMs,
        RequestsProcessed = stats.TotalRequests
    });
});

app.Run();
```

### Public Members

```csharp
public sealed class PerformanceMiddleware
public PerformanceMiddleware(RequestDelegate next, PerformanceMonitor performanceMonitor)
public async Task InvokeAsync(HttpContext context)

public sealed class RequestMetrics
public string Method { get; }
public string Path { get; }
public int StatusCode { get; }
public long ElapsedMs { get; }
public long MemoryUsedKb { get; }
public DateTime Timestamp { get; }

public sealed class PerformanceMonitor
public async Task RecordMetricAsync(RequestMetrics metrics)
public async Task<PerformanceStats> GetStatsAsync()
public async Task<List<RequestMetrics>> GetRecentMetricsAsync(int limit = 100)

public sealed class PerformanceStats
public int TotalRequests { get; }
public double AverageElapsedMs { get; }
public long MaxElapsedMs { get; }
public long MinElapsedMs { get; }
public double AverageMemoryUsedKb { get; }
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



## ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` class provides centralized exception handling for ASP.NET Core applications, converting exceptions into structured `Result<T>` responses and ensuring consistent error responses across all API endpoints. It automatically handles exceptions, logs them using the configured logger, and returns appropriate HTTP status codes with detailed error information.

### Usage Example

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Middleware;
using SqliteMultiTenant.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

var app = builder.Build();

// Register ErrorHandlingMiddleware to enable centralized exception handling
app.UseErrorHandling();

// Example endpoint that might throw exceptions
app.MapGet("/api/tenants/{id}", async (string id, HttpContext context) =>
{
    // Simulate a service that might fail
    var tenantService = new TenantService();
    
    // This will automatically be wrapped in proper error handling
    var result = await tenantService.GetTenantAsync(id);
    
    if (result.IsSuccess)
    {
        return Results.Ok(result.Value);
    }
    
    // Error responses are automatically handled by the middleware
    return Results.BadRequest(result.ErrorMessage);
});

app.Run();
```

## CorrelationIdMiddleware

The `CorrelationIdMiddleware` class adds unique correlation IDs to HTTP requests for distributed tracing and request tracking across services. It automatically generates a correlation ID if one isn't present in request headers or query parameters, stores it in the HTTP context for retrieval, and includes it in response headers. This enables end-to-end request tracking and logging across microservices or layered applications.

### Usage Example

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add logging services
builder.Logging.AddConsole();

var app = builder.Build();

// Register CorrelationIdMiddleware to enable correlation ID tracking
app.UseCorrelationId();

// Your endpoints and middleware configuration
app.MapGet("/api/health", () => "OK");
app.MapGet("/api/data", (HttpContext context) => {
    // Retrieve the correlation ID from the current HTTP context
    string correlationId = context.GetCorrelationId();
    
    Console.WriteLine($"Processing request with correlation ID: {correlationId}");
    
    return Results.Ok(new { 
        Message = "Request processed",
        CorrelationId = correlationId
    });
});

app.Run();
```

### Key Features

- **Automatic Correlation ID Generation**: Generates a new GUID correlation ID if none is provided in headers or query parameters
- **Header Integration**: Uses `X-Correlation-Id` header for request/response correlation
- **Context Storage**: Stores correlation ID in `HttpContext.Items` for easy retrieval throughout the request pipeline
- **Response Headers**: Automatically adds correlation ID to response headers for downstream services
- **Logging Integration**: Provides correlation ID in log messages for end-to-end tracing
- **Query Parameter Support**: Can also accept correlation ID from `?correlationId=` query parameter

### Public Members

```csharp
public sealed class CorrelationIdMiddleware
public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
public async Task InvokeAsync(HttpContext context)

public static class CorrelationIdMiddlewareExtensions
public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
public static string GetCorrelationId(this HttpContext context)
```

### How It Works

1. **Request Processing**: When a request arrives, the middleware checks for an existing correlation ID in:
   - Request headers (`X-Correlation-Id`)
   - Query parameters (`?correlationId=`)

2. **ID Generation**: If no correlation ID is found, a new GUID is generated

3. **Context Storage**: The correlation ID is stored in `HttpContext.Items` for retrieval by other middleware or endpoints

4. **Response Headers**: The correlation ID is added to the response headers before the response is sent

5. **Logging**: All log messages include the correlation ID for end-to-end traceability

### Integration with ASP.NET Core

To use `CorrelationIdMiddleware` in your ASP.NET Core application:

```csharp
// In Program.cs or Startup.cs
var app = builder.Build();

// Add CorrelationIdMiddleware early in the pipeline
app.UseCorrelationId();

// Add other middleware
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(...);

app.Run();
```

### Retrieving Correlation ID in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetOrder(int id)
    {
        string correlationId = HttpContext.GetCorrelationId();
        
        _logger.LogInformation("Processing order {OrderId} with correlation {CorrelationId}", id, correlationId);
        
        // Your business logic here
        
        return Ok(new { OrderId = id, CorrelationId = correlationId });
    }
}
```

### Benefits

- **Distributed Tracing**: Track requests across multiple services using a consistent correlation ID
- **Debugging**: Easily correlate log messages from different components processing the same request
- **Monitoring**: Monitor request flow and identify bottlenecks using correlation IDs
- **Audit Trail**: Create comprehensive audit trails with request IDs for compliance and debugging

### Best Practices

- Add `UseCorrelationId()` early in your middleware pipeline
- Include correlation ID in all log messages
- Propagate correlation ID to downstream services via headers
- Use the same header name (`X-Correlation-Id`) consistently across services

## RateLimitingMiddleware

The `RateLimitingMiddleware` class provides token bucket-based rate limiting to protect your multi-tenant application from abuse and DoS attacks. It implements a sliding window algorithm that fairly distributes request capacity over time, preventing sudden bursts while allowing sustained traffic. The middleware supports both per-tenant (via `X-Tenant-Id` header) and per-IP rate limiting, with configurable thresholds for requests per minute, burst capacity, and cleanup intervals.

### Usage Example

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure rate limiting options
builder.Services.Configure<RateLimitingOptions>(options =>
{
    options.RequestsPerMinute = 600; // 10 requests per second
    options.BurstCapacity = 100; // Allow bursts up to 100 requests
    options.CleanupIntervalSeconds = 600; // Clean up every 10 minutes
});

builder.Logging.AddConsole();

var app = builder.Build();

// Register RateLimitingMiddleware early in the pipeline to protect all endpoints
app.UseMiddleware<RateLimitingMiddleware>();

// Your other middleware and endpoints
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/tenants/{id}", (string id) => 
{
    // Your endpoint logic
    return Results.Ok(new { TenantId = id });
});

app.Run();
```

### Configuration Options

The `RateLimitingOptions` class allows you to configure rate limiting behavior without recompiling your application:

- **RequestsPerMinute**: Number of requests allowed per minute (default: 300 = 5 req/sec)
- **BurstCapacity**: How many requests over the limit are allowed before blocking (default: 50)
- **CleanupIntervalSeconds**: How often to remove unused rate limit buckets from memory (default: 300 seconds = 5 minutes)

### How It Works

1. **Token Bucket Algorithm**: Each client (tenant or IP) gets a bucket with a capacity of `RequestsPerMinute` tokens
2. **Sliding Window**: Tokens are refilled at a rate of `RequestsPerMinute / 60` tokens per second
3. **Request Processing**: Each request consumes one token; if no tokens available, returns HTTP 429 with `Retry-After` header
4. **Tenant Isolation**: Uses `X-Tenant-Id` header when available, falls back to IP address for anonymous requests
5. **Memory Management**: Unused buckets are automatically cleaned up to prevent memory bloat

### Public Members

```csharp
public sealed class RateLimitingMiddleware
public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, RateLimitingOptions options)
public async Task InvokeAsync(HttpContext context)

public sealed class RateLimitingOptions
public int RequestsPerMinute { get; set; }
public int BurstCapacity { get; set; }
public int CleanupIntervalSeconds { get; set; }

public sealed class TokenBucket
public TokenBucket(double capacity, double refillRate)
public bool TryConsumeToken()
```

## CommandExecutor

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