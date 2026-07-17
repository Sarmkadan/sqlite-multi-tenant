## EncryptionKeyManagerExtensions

The `EncryptionKeyManagerExtensions` class provides extension methods for the `EncryptionKeyManager` that simplify tenant-specific key operations with fluent-style APIs. These methods enable easy generation, rotation, retrieval, and management of encryption keys for individual tenants in a multi-tenant SQLite environment.

### Usage Example

```csharp
using SqliteMultiTenant.Security;
using System;

// Assume you have an EncryptionKeyManager instance
var keyManager = new EncryptionKeyManager(...);

// Example 1: Generate a new encryption key for a tenant
var newKey = await keyManager.GenerateKeyForTenantAsync("tenant-123");
Console.WriteLine($"Generated key version: {newKey.Version}");

// Example 2: Generate a key with master password
var secureKey = await keyManager.GenerateKeyForTenantAsync("tenant-123", "MyMasterPassword123!");

// Example 3: Rotate the tenant's encryption key
var rotatedKey = await keyManager.RotateKeyForTenantAsync("tenant-123");
Console.WriteLine($"Rotated to key version: {rotatedKey.Version}");

// Example 4: Check if tenant has an active key
var hasActiveKey = await keyManager.HasActiveKeyAsync("tenant-123");
Console.WriteLine("Tenant has active key: {hasActiveKey}");

// Example 5: Get the active key (returns null if not found)
var activeKey = await keyManager.GetActiveKeyForTenantAsync("tenant-123");

// Example 6: Get the active key (throws if not found)
var requiredKey = await keyManager.GetRequiredActiveKeyForTenantAsync("tenant-123");

// Example 7: Get a specific key version
var historicalKey = await keyManager.GetKeyVersionForTenantAsync("tenant-123", 1);

// Example 8: Delete all keys for a tenant
var deleted = await keyManager.DeleteTenantKeysAsync("tenant-123");
Console.WriteLine($"Keys deleted: {deleted}");
```

## ConnectionManagerIntegrationTests

The `ConnectionManagerIntegrationTests` class provides comprehensive unit tests for the `ConnectionManager` class, verifying that connection management operations work correctly. These tests cover retrieving a connection, reusing an existing connection when available, creating a new connection when the pool is not full, releasing a connection back to the pool, and clearing the tenant pool, ensuring the connection manager operates reliably in multi-tenant SQLite environments.

### Public Members

```csharp
public sealed class ConnectionManagerIntegrationTests : IDisposable
public ConnectionManagerIntegrationTests()
public async Task GetConnectionAsync_ShouldReturnOpenConnection
public async Task GetConnectionAsync_ShouldReuseConnection_WhenAvailable
public async Task GetConnectionAsync_ShouldCreateNewConnection_WhenPoolNotFull
public async Task ReleaseConnectionAsync_ShouldReturnConnectionToPool
public async Task ClearTenantPoolAsync_ShouldRemoveTenantPoolAndDisposeConnections
public async Task GetPoolStatistics_ShouldReturnCorrectStats
public async Task GetConnectionAsync_ShouldThrowArgumentNullException_WhenTenantIdIsNull
public async Task GetConnectionAsync_ShouldThrowArgumentNullException_WhenConnectionStringIsNull
public async Task GetConnectionAsync_ShouldRespectMaxConnectionsPerPool
public void Dispose
```

### Usage Example

```csharp
using SqliteMultiTenant.Connections;
using Microsoft.Extensions.Logging;
using System.Data.SQLite;
using System;

// Create logger and checker instance
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ConnectionManager>();
var connectionManager = new ConnectionManager(logger);

// Example 1: Get a connection
using var connection = await connectionManager.GetConnectionAsync();
Console.WriteLine(connection != null ? "Connection retrieved" : "Connection retrieval failed");

// Example 2: Release a connection
await connectionManager.ReleaseConnectionAsync(connection);
Console.WriteLine("Connection released");

// Example 3: Get pool statistics
var stats = await connectionManager.GetPoolStatistics();
Console.WriteLine(stats != null ? "Pool statistics retrieved" : "Pool statistics retrieval failed");
```

## HttpClientWrapperValidation

`HttpClientWrapperValidation` is a static helper class that provides validation utilities for `HttpClientWrapper` instances and related HTTP request components such as URLs, bearer tokens, headers, payloads, and response types. It offers methods to retrieve validation problems, check validity, and enforce correctness by throwing exceptions when validation fails.

### Usage Example

```csharp
using System;
using System.Collections.Generic;
using SqliteMultiTenant.Integration;

// Assume an existing HttpClientWrapper instance (from the library)
HttpClientWrapper client = new HttpClientWrapper(/* constructor arguments */);

// 1. Validate the wrapper instance and ensure it is correct
IReadOnlyList<string> instanceProblems = HttpClientWrapperValidation.Validate(client);
if (instanceProblems.Count > 0)
{
    Console.WriteLine("HttpClientWrapper has problems:");
    foreach (var p in instanceProblems) Console.WriteLine($"- {p}");
}
else
{
    Console.WriteLine("HttpClientWrapper instance is valid.");
}

// Shortcut to just get a boolean result
bool isValid = HttpClientWrapperValidation.IsValid(client);
Console.WriteLine($"IsValid: {isValid}");

// Throw an exception if the instance is not valid
HttpClientWrapperValidation.EnsureValid(client);

// 2. Validate a request URL
var urlProblems = HttpClientWrapperValidation.ValidateUrl("https://api.example.com/v1/resource");
Console.WriteLine(urlProblems.Count == 0 ? "URL is valid." : $"URL problems: {string.Join(", ", urlProblems)}");

// 3. Validate a bearer token
var tokenProblems = HttpClientWrapperValidation.ValidateBearerToken("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...");
Console.WriteLine(tokenProblems.Count == 0 ? "Bearer token is valid." : $"Token problems: {string.Join(", ", tokenProblems)}");

// 4. Validate a custom header
var headerProblems = HttpClientWrapperValidation.ValidateHeader("X-Custom-Header", "HeaderValue");
Console.WriteLine(headerProblems.Count == 0 ? "Header is valid." : $"Header problems: {string.Join(", ", headerProblems)}");

// 5. Validate a payload object before serialization
var payload = new { Id = 123, Name = "Sample" };
var payloadProblems = HttpClientWrapperValidation.ValidatePayload(payload);
Console.WriteLine(payloadProblems.Count == 0 ? "Payload is valid." : $"Payload problems: {string.Join(", ", payloadProblems)}");

// 6. Validate a response type for deserialization
public class ApiResponse
{
    public int Code { get; set; }
    public string? Message { get; set; }

    // Parameterless constructor required by the validator
    public ApiResponse() { }
}

var responseTypeProblems = HttpClientWrapperValidation.ValidateResponseType<ApiResponse>();
Console.WriteLine(responseTypeProblems.Count == 0 ? "Response type is valid." : $"Response type problems: {string.Join(", ", responseTypeProblems)}");
```

## TenantContextJsonExtensions

The `TenantContextJsonExtensions` class provides extension methods for serializing and deserializing `TenantContext` objects to and from JSON format. It simplifies working with tenant context data in multi-tenant applications by providing fluent-style APIs for JSON conversion with support for camelCase naming and null value handling.

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using System;

// Create a TenantContext instance
var tenantContext = new TenantContext
{
    TenantId = "tenant-123",
    TenantName = "Acme Corporation",
    UserId = "user-456",
    UserEmail = "john.doe@acme.com",
    EstablishedAt = DateTime.UtcNow.AddYears(-5),
    CreatedAt = DateTime.UtcNow,
    RequestId = "req-789",
    ConnectionId = "conn-abc",
    DatabasePath = "/data/tenant-123.db",
    IsValid = true,
    ContextData = new Dictionary<string, object>
    {
        { "subscriptionTier", "premium" },
        { "maxConnections", 100 },
        { "featuresEnabled", new[] { "multi-db", "backup", "analytics" } }
    }
};

// Serialize to JSON string
string json = tenantContext.ToJson();
Console.WriteLine(json);

// Serialize with indentation for readability
string prettyJson = tenantContext.ToJson(indented: true);
Console.WriteLine(prettyJson);

// Deserialize from JSON string
string jsonInput = @"{
    \"tenantId\": \"tenant-123\",
    \"tenantName\": \"Acme Corporation\",
    \"userId\": \"user-456\",
    \"userEmail\": \"john.doe@acme.com\",
    \"establishedAt\": \"2021-01-15T00:00:00\",
    \"createdAt\": \"2024-07-19T10:30:00\",
    \"requestId\": \"req-789\",
    \"connectionId\": \"conn-abc\",
    \"databasePath\": \"/data/tenant-123.db\",
    \"isValid\": true,
    \"contextData\": {
        \"subscriptionTier\": \"premium\",
        \"maxConnections\": 100
    }
}";
var deserializedContext = TenantContextJsonExtensions.FromJson(jsonInput);
Console.WriteLine(deserializedContext?.TenantName);

// Try to deserialize with error handling
if (TenantContextJsonExtensions.TryFromJson(jsonInput, out var result))
{
    Console.WriteLine("Deserialization succeeded!");
}
else
{
    Console.WriteLine("Deserialization failed!");
}
```

## MigrationExtensions

The `MigrationExtensions` class provides extension methods for the `Migration` class that simplify working with migration data in multi-tenant SQLite environments. These methods offer convenient ways to query migration states, calculate durations, analyze statistics, and format data for display or logging purposes.

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using System;
using System.Linq;

// Assume you have a list of migrations from your database context
var migrations = new List<Migration>
{
    new Migration
    {
        Id = Guid.NewGuid(),
        Name = "Create Users Table",
        Description = "Initial migration creating users table",
        Status = MigrationStatus.Completed,
        ExecutionOrder = 1,
        CreatedAt = DateTime.UtcNow.AddDays(-7),
        ExecutedAt = DateTime.UtcNow.AddDays(-6),
        ExecutionTimeMs = 1500,
        DatabaseId = "main"
    },
    new Migration
    {
        Id = Guid.NewGuid(),
        Name = "Add Indexes",
        Description = "Add indexes for performance optimization",
        Status = MigrationStatus.Completed,
        ExecutionOrder = 2,
        CreatedAt = DateTime.UtcNow.AddDays(-5),
        ExecutedAt = DateTime.UtcNow.AddDays(-4),
        ExecutionTimeMs = 850,
        DatabaseId = "main"
    },
    new Migration
    {
        Id = Guid.NewGuid(),
        Name = "Create Audit Log",
        Description = "Add audit logging functionality",
        Status = MigrationStatus.Pending,
        ExecutionOrder = 3,
        CreatedAt = DateTime.UtcNow.AddDays(-3),
        DatabaseId = "main"
    }
};

// Example 1: Check if a migration is in terminal state
var completedMigration = migrations.First(m => m.Status == MigrationStatus.Completed);
bool isTerminal = completedMigration.IsTerminal();
Console.WriteLine($"Is terminal: {isTerminal}"); // Output: Is terminal: True

// Example 2: Get migration age in days
var migration = migrations.First();
double ageInDays = migration.GetAgeInDays();
Console.WriteLine($"Migration age: {ageInDays:F2} days");

// Example 3: Get formatted execution duration
string duration = migration.GetExecutionDuration();
Console.WriteLine($"Execution duration: {duration}"); // Output: Execution duration: 1.5s

// Example 4: Get status display with formatting
string statusDisplay = migration.GetStatusDisplay();
Console.WriteLine($"Status: {statusDisplay}"); // Output: Status: [COMPLETED]

// Example 5: Get statistics for all migrations
var statusCounts = migrations.GetStatusCounts();
foreach (var kvp in statusCounts)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
// Output:
// Completed: 2
// Pending: 1

// Example 6: Get pending migrations ordered by execution
var pendingMigrations = migrations.GetPendingMigrations();
foreach (var pending in pendingMigrations)
{
    Console.WriteLine($"Pending: {pending.Name}");
}

// Example 7: Get total execution time across all completed migrations
long totalTimeMs = migrations.GetTotalExecutionTimeMs();
Console.WriteLine($"Total execution time: {totalTimeMs}ms"); // Output: Total execution time: 2350ms

// Example 8: Get average execution time for completed migrations
double avgTimeMs = migrations.GetAverageExecutionTimeMs();
Console.WriteLine($"Average execution time: {avgTimeMs:F0}ms"); // Output: Average execution time: 1175ms

// Example 9: Get rollbackable migrations (newest first)
var rollbackable = migrations.GetRollbackableMigrations();
foreach (var rollback in rollbackable)
{
    Console.WriteLine($"Rollbackable: {rollback.Name}");
}

// Example 10: Get database name
string dbName = migration.GetDatabaseName();
Console.WriteLine($"Database: {dbName}"); // Output: Database: main

// Example 11: Get formatted creation timestamp
string createdAt = migration.GetFormattedCreatedAt();
Console.WriteLine($"Created at: {createdAt}");
```

## CommandParserValidation

`CommandParserValidation` is a static helper class that provides validation utilities for `CommandParser`, `CommandHandler`, `Subcommand`, and `ParsedCommand` instances. It validates command structure, required arguments, and data integrity, offering methods to retrieve validation problems, check validity, and enforce correctness by throwing exceptions when validation fails.

### Usage Example

```csharp
using System;
using System.Collections.Generic;
using SqliteMultiTenant.Cli;

// Assume you have a CommandParser instance with registered commands
var commandParser = new CommandParser();

// Register some commands
commandParser.RegisterCommand("tenant", "Manage tenants", new CommandHandler
{
    Name = "tenant",
    Description = "Manage tenants",
    Subcommands = new List<Subcommand>
    {
        new Subcommand
        {
            Name = "list",
            Description = "List all tenants",
            RequiredArgs = new[] { "--format" }
        },
        new Subcommand
        {
            Name = "create",
            Description = "Create a new tenant",
            RequiredArgs = new[] { "--name", "--id" }
        }
    }
});

// 1. Validate the command parser instance
IReadOnlyList<string> parserProblems = commandParser.Validate();
if (parserProblems.Count > 0)
{
    Console.WriteLine("CommandParser has problems:");
    foreach (var p in parserProblems) Console.WriteLine($"- {p}");
}
else
{
    Console.WriteLine("CommandParser instance is valid.");
}

// Shortcut to just get a boolean result
bool isValid = commandParser.IsValid();
Console.WriteLine($"IsValid: {isValid}");

// Throw an exception if the command parser is not valid
CommandParserValidation.EnsureValid(commandParser);

// 2. Validate a CommandHandler instance
var tenantHandler = commandParser.GetCommandHandler("tenant");
IReadOnlyList<string> handlerProblems = tenantHandler.Validate();
Console.WriteLine(handlerProblems.Count == 0 ? "CommandHandler is valid." : $"CommandHandler problems: {string.Join(", ", handlerProblems)}");

// 3. Validate a Subcommand instance
var listSubcommand = tenantHandler.Subcommands.First(s => s.Name == "list");
IReadOnlyList<string> subcommandProblems = listSubcommand.Validate();
Console.WriteLine(subcommandProblems.Count == 0 ? "Subcommand is valid." : $"Subcommand problems: {string.Join(", ", subcommandProblems)}");

// 4. Validate a ParsedCommand instance
var parsedCommand = new ParsedCommand
{
    Success = true,
    MainCommand = "tenant",
    Subcommand = "list",
    Arguments = new[] { "--format", "json" },
    Description = "List all tenants in JSON format"
};
IReadOnlyList<string> parsedProblems = parsedCommand.Validate();
Console.WriteLine(parsedProblems.Count == 0 ? "ParsedCommand is valid." : $"ParsedCommand problems: {string.Join(", ", parsedProblems)}");
```
