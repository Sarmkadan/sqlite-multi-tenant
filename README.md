[![Build](https://github.com/sarmkadan/sqlite-multi-tenant/actions/workflows/build.yml/badge.svg)](https://github.com/sarmkadan/sqlite-multi-tenant/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)

# SQLite Multi-Tenant Database Manager

A production-grade .NET library and framework for managing multi-tenant SQLite databases with per-tenant isolation, automated migrations, comprehensive backup management, and advanced monitoring capabilities. Designed for SaaS platforms, multi-tenant applications, and enterprise systems requiring secure data isolation and operational reliability.

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [API Reference](#api-reference)
- [CLI Reference](#cli-reference)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [Advanced Topics](#advanced-topics)
- [Troubleshooting](#troubleshooting)
- [Performance](#performance)
- [Testing](#testing)
- [Related Projects](#related-projects)
- [Contributing](#contributing)
- [License](#license)

## Features

### Core Capabilities

- **Multi-Tenant Isolation**: Complete data isolation with per-tenant SQLite databases and connection pooling
- **Tenant Management**: Full lifecycle management (create, update, delete, activate, suspend, archive)
- **Database Migrations**: Track and execute migrations with rollback, execution history, and error recovery
- **Backup Management**: Multiple backup types (Full, Incremental, Differential) with verification and expiration policies
- **Connection Management**: Tenant-specific database connections with automatic pooling and timeout handling
- **Async/Await Support**: Fully asynchronous API with CancellationToken support for high-performance applications
- **Comprehensive Logging**: Structured logging with ILogger integration and multiple output formats
- **Entity Validation**: Fluent validation with detailed error messages and domain-specific rules
- **Custom Exceptions**: Specialized exception types for precise error handling

### Advanced Features

- **API Controllers**: REST endpoints for tenant, backup, migration, database, and admin operations
- **CLI Interface**: Command-line tool for automation and operational tasks
- **Event System**: Pub-sub architecture with async event handlers and webhook delivery
- **Middleware Pipeline**: Correlation ID tracking, performance monitoring, rate limiting, and request logging
- **Health Checks**: System diagnostics with detailed status reporting
- **Audit Logging**: Comprehensive audit trail with filtering, retention policies, and trend analysis
- **Caching**: In-memory and distributed LRU caching with TTL support
- **Security**: AES-256 encryption, rate limiting, authentication interceptors
- **Monitoring**: Real-time metrics collection, statistics aggregation, and trend analysis
- **Data Operations**: Batch processing, bulk insert, data export/import with conflict resolution
- **Background Workers**: Automated backup scheduling, maintenance tasks, and data retention policies

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                  API Layer / CLI Interface                   │
│  Controllers │ Commands │ Middleware │ Interceptors │ Handlers
└──────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    Service Layer (Business Logic)            │
│  TenantService │ MigrationService │ BackupService │ Custom   │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                   Repository Layer (Data Access)             │
│  TenantRepo │ MigrationRepo │ BackupRepo │ Generic Repo     │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│            SQLite Databases (Per-Tenant Isolation)           │
│  Master.db │ Tenant1.db │ Tenant2.db │ ... │ TenantN.db    │
└─────────────────────────────────────────────────────────────┘

Support Layers:
- Security: Encryption, Authentication, Rate Limiting
- Monitoring: Audit Logs, Metrics, Health Checks
- Caching: In-Memory & Distributed LRU Cache
- Events: Event Bus, Webhooks, Domain Events
- Utilities: Validation, Formatting, Extension Methods
```

## Installation

### Prerequisites

- **.NET 8.0** or higher
- **SQLite 3** (included with most systems)
- **NuGet package manager**

### From Source

```bash
git clone https://github.com/Sarmkadan/sqlite-multi-tenant.git
cd sqlite-multi-tenant
dotnet restore
dotnet build
dotnet pack
```

### Add to Your Project

```bash
# Via dotnet CLI
dotnet add package SqliteMultiTenant

# Or via NuGet Package Manager
Install-Package SqliteMultiTenant
```

### Manual Integration

```bash
# Clone and reference
git clone https://github.com/Sarmkadan/sqlite-multi-tenant.git
# Add project reference in your .csproj:
# <ProjectReference Include="path/to/sqlite-multi-tenant/src/SqliteMultiTenant.csproj" />
```

## Quick Start

### 1. Configure Services (Minimal Setup)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Configuration;

var services = new ServiceCollection();

// Add logging (required)
services.AddLogging(builder => builder.AddConsole());

// Add SQLite Multi-Tenant services
var masterConnection = "Data Source=master.db;Version=3;";
services.AddSqliteMultiTenant(masterConnection, options =>
{
    options.MaxConnections = 20;
    options.BackupRetentionDays = 30;
    options.DatabaseDirectory = "databases";
    options.BackupDirectory = "backups";
});

var serviceProvider = services.BuildServiceProvider();
```

### 2. Create Your First Tenant

```csharp
var tenantService = serviceProvider.GetRequiredService<ITenantService>();

var tenant = await tenantService.CreateTenantAsync(
    name: "Acme Corporation",
    description: "Our first customer",
    contactEmail: "admin@acme.com");

Console.WriteLine($"Created tenant: {tenant.TenantId}");
```

### 3. Work with Databases

```csharp
// Create a database entry for this tenant
var db = new TenantDatabase
{
    DatabaseId = Guid.NewGuid().ToString(),
    TenantId = tenant.TenantId,
    Name = "primary_db",
    FilePath = Path.Combine("databases", $"{tenant.TenantId}.db"),
    SchemaVersion = 1
};
```

### 4. Manage Migrations

```csharp
var migrationService = serviceProvider.GetRequiredService<IMigrationService>();

var migration = await migrationService.CreateMigrationAsync(
    databaseId: db.DatabaseId,
    version: "001",
    name: "CreateUsersTable",
    upScript: @"
        CREATE TABLE Users (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL,
            Email TEXT UNIQUE,
            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
        );",
    downScript: "DROP TABLE Users;");

// Get pending migrations
var pending = await migrationService.GetPendingMigrationsAsync(db.DatabaseId);
Console.WriteLine($"Pending migrations: {pending.Count}");
```

### 5. Create Backups

```csharp
var backupService = serviceProvider.GetRequiredService<IBackupService>();

var backup = await backupService.CreateBackupAsync(
    databaseId: db.DatabaseId,
    backupType: BackupType.Full,
    createdBy: "admin@acme.com");

// Complete the backup operation
await backupService.MarkBackupAsCompletedAsync(
    backupId: backup.BackupId,
    sizeBytes: 1024000,
    durationMs: 2500);

// Verify integrity
await backupService.VerifyBackupAsync(backup.BackupId, "admin@acme.com");
```

## Core Concepts

### Tenant

Represents a customer, organization, or isolated business unit.

**Key Properties:**
- `TenantId`: Unique identifier (GUID)
- `Name`: Display name
- `Status`: Active, Inactive, Suspended, Archived, Deleted
- `CreatedAt` / `UpdatedAt`: Lifecycle timestamps
- `Metadata`: Custom key-value pairs for extensions

**Lifecycle:**
```
Created (Active) → Suspended ↔ Active → Archived → Deleted
```

### TenantDatabase

Represents a SQLite database file associated with a tenant.

**Key Properties:**
- `DatabaseId`: Unique identifier
- `TenantId`: Reference to tenant
- `FilePath`: Absolute path to .db file
- `SchemaVersion`: Current schema version
- `IsReadOnly`: Backup/restore protection flag
- `SizeBytes`: Database file size

### Migration

Represents a database schema change tracked and versioned.

**Key Properties:**
- `MigrationId`: Unique identifier
- `DatabaseId`: Target database
- `Version`: Semantic version (001, 002, etc.)
- `Name`: Human-readable description
- `Status`: Pending, Applied, Failed, RolledBack
- `UpScript` / `DownScript`: SQL change scripts
- `ExecutedAt` / `RolledBackAt`: Execution history

### Backup

Represents a point-in-time backup of a database.

**Key Properties:**
- `BackupId`: Unique identifier
- `DatabaseId`: Source database
- `BackupType`: Full, Incremental, Differential
- `Status`: Pending, Completed, Failed, Verified
- `SizeBytes`: Backup file size
- `ExpiresAt`: Retention policy deadline
- `Tags`: Organizational labels (production, daily, etc.)

## API Reference

### Tenant Service (`ITenantService`)

```csharp
// Create a new tenant
Task<Tenant> CreateTenantAsync(string name, string description, string contactEmail);

// Retrieve tenant details
Task<Tenant> GetTenantAsync(string tenantId);

// List all tenants
Task<List<Tenant>> GetAllTenantsAsync();

// Update tenant information
Task UpdateTenantAsync(string tenantId, string name, string description);

// Change tenant status
Task ActivateTenantAsync(string tenantId);
Task DeactivateTenantAsync(string tenantId);
Task SuspendTenantAsync(string tenantId);
Task ArchiveTenantAsync(string tenantId);

// Soft delete tenant
Task DeleteTenantAsync(string tenantId);

// Search functionality
Task<List<Tenant>> SearchTenantsAsync(string searchTerm);

// Metadata management
Task SetTenantMetadataAsync(string tenantId, string key, string value);
Task<string> GetTenantMetadataAsync(string tenantId, string key);
```

### Migration Service (`IMigrationService`)

```csharp
// Create migration definition
Task<Migration> CreateMigrationAsync(string databaseId, string version, 
    string name, string upScript, string downScript);

// Migration query
Task<Migration> GetMigrationAsync(string migrationId);
Task<List<Migration>> GetPendingMigrationsAsync(string databaseId);
Task<List<Migration>> GetAppliedMigrationsAsync(string databaseId);

// Execute migrations
Task ExecuteMigrationAsync(string migrationId);
Task RollbackMigrationAsync(string migrationId);

// Mark completion
Task MarkMigrationAsCompletedAsync(string migrationId, long executionMs);
Task MarkMigrationAsFailedAsync(string migrationId, string errorMessage);
```

### Backup Service (`IBackupService`)

```csharp
// Create and manage backups
Task<Backup> CreateBackupAsync(string databaseId, BackupType backupType, 
    string createdBy, string backupPath = null);

// Backup queries
Task<Backup> GetBackupAsync(string backupId);
Task<List<Backup>> GetDatabaseBackupsAsync(string databaseId, int pageSize = 50);
Task<List<Backup>> GetCompletedBackupsAsync(string databaseId);

// Backup lifecycle
Task MarkBackupAsCompletedAsync(string backupId, long sizeBytes, long durationMs);
Task VerifyBackupAsync(string backupId, string verifiedBy);

// Backup metadata
Task SetBackupExpirationAsync(string backupId, DateTime expiresAt);
Task AddBackupTagAsync(string backupId, string tag);
Task<List<string>> GetBackupTagsAsync(string backupId);

// Count and statistics
Task<int> GetBackupCountAsync(string databaseId);
```

## CLI Reference

The CLI interface provides command-line access to all core functionality:

```bash
# Tenant Operations
dotnet run -- tenant create --name "Acme Corp" --email "admin@acme.com"
dotnet run -- tenant list [--limit 10]
dotnet run -- tenant get --id <tenant-id>
dotnet run -- tenant activate --id <tenant-id>
dotnet run -- tenant suspend --id <tenant-id>
dotnet run -- tenant delete --id <tenant-id>

# Database Operations
dotnet run -- database list --tenant-id <tenant-id>
dotnet run -- database stats --id <db-id>
dotnet run -- database optimize --id <db-id>
dotnet run -- database integrity-check --id <db-id>

# Migration Operations
dotnet run -- migration list --database-id <db-id> [--status pending|applied|failed]
dotnet run -- migration apply --database-id <db-id>
dotnet run -- migration rollback --database-id <db-id>

# Backup Operations
dotnet run -- backup create --database-id <db-id> --type full|incremental|differential
dotnet run -- backup list --database-id <db-id>
dotnet run -- backup verify --id <backup-id>
dotnet run -- backup restore --id <backup-id>

# System Operations
dotnet run -- health check
dotnet run -- metrics show
dotnet run -- cache clear
dotnet run -- version
```

## Configuration

### MultiTenantOptions

```csharp
services.AddSqliteMultiTenant(connectionString, options =>
{
    // Connection Management
    options.MaxConnections = 20;                    // Max connections per tenant
    options.ConnectionTimeoutSeconds = 30;         // Connection timeout
    options.ConnectionStringTemplate = "...";      // Custom connection string format

    // File Paths
    options.DatabaseDirectory = "databases";       // Where to store tenant .db files
    options.BackupDirectory = "backups";           // Where to store backup files
    options.LogDirectory = "logs";                 // Where to store log files

    // Backup Settings
    options.BackupRetentionDays = 30;              // Delete backups older than this
    options.MaxBackupsPerDatabase = 100;           // Limit backups per database
    options.BackupCompressionEnabled = true;       // Compress backup files

    // Security
    options.EnableEncryption = false;              // Enable AES-256 encryption
    options.EncryptionKey = "your-256-bit-key";    // Encryption key (min 32 bytes)

    // Features
    options.EnableLogging = true;                  // Enable ILogger integration
    options.EnableAuditing = true;                 // Enable audit trail
    options.EnableMetrics = true;                  // Enable metrics collection
    options.AuditRetentionDays = 90;               // Delete audit logs after

    // Cache Settings
    options.EnableCaching = true;                  // Enable result caching
    options.CacheExpirationMinutes = 15;           // Cache TTL
    options.MaxCacheItems = 1000;                  // Max cached items

    // Performance
    options.EnableBatchOperations = true;          // Enable batch processing
    options.BatchSize = 100;                       // Items per batch
    options.MaxDegreeOfParallelism = 4;            // Parallel threads
});
```

### appsettings.json Configuration

```json
{
  "SqliteMultiTenant": {
    "MaxConnections": 20,
    "ConnectionTimeoutSeconds": 30,
    "BackupRetentionDays": 30,
    "DatabaseDirectory": "databases",
    "BackupDirectory": "backups",
    "EnableEncryption": false,
    "EnableLogging": true,
    "EnableAuditing": true,
    "CacheExpirationMinutes": 15
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SqliteMultiTenant": "Debug"
    }
  }
}
```

## Usage Examples

### Example 1: Multi-Tenant Application Setup

```csharp
// Create service provider with multi-tenant configuration
var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

services.AddSqliteMultiTenant("Data Source=master.db;", options =>
{
    options.MaxConnections = 20;
    options.BackupRetentionDays = 30;
    options.DatabaseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "databases");
    options.BackupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "backups");
    options.EnableLogging = true;
});

var provider = services.BuildServiceProvider();
var tenantService = provider.GetRequiredService<ITenantService>();

// Create multiple tenants
var tenants = new[] { "TechCorp", "FinanceInc", "RetailCo" };
foreach (var name in tenants)
{
    var tenant = await tenantService.CreateTenantAsync(
        name: name,
        description: $"Tenant for {name}",
        contactEmail: $"admin@{name.ToLower()}.com");
    
    Console.WriteLine($"Created: {tenant.TenantId} - {tenant.Name}");
}
```

### Example 2: Database Schema Management

```csharp
var migrationService = provider.GetRequiredService<IMigrationService>();
var backupService = provider.GetRequiredService<IBackupService>();

// Create database entry
var dbId = Guid.NewGuid().ToString();

// Define migrations
var migrations = new[]
{
    ("001", "CreateTables", @"
        CREATE TABLE Users (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);
        CREATE TABLE Posts (Id INTEGER PRIMARY KEY, UserId INTEGER, Content TEXT);"),
    
    ("002", "AddTimestamps", @"
        ALTER TABLE Users ADD COLUMN CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP;
        ALTER TABLE Posts ADD COLUMN CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP;"),
    
    ("003", "CreateIndexes", @"
        CREATE INDEX idx_users_created ON Users(CreatedAt);
        CREATE INDEX idx_posts_user ON Posts(UserId);")
};

// Create migration records
foreach (var (version, name, upScript) in migrations)
{
    var downScript = version == "001" ? "DROP TABLE Users; DROP TABLE Posts;" : "";
    
    await migrationService.CreateMigrationAsync(
        databaseId: dbId,
        version: version,
        name: name,
        upScript: upScript,
        downScript: downScript);
}

// Get and display pending migrations
var pending = await migrationService.GetPendingMigrationsAsync(dbId);
Console.WriteLine($"Pending migrations: {pending.Count}");
foreach (var m in pending)
    Console.WriteLine($"  - {m.Version}: {m.Name}");

// Create backup before applying migrations
var backup = await backupService.CreateBackupAsync(
    databaseId: dbId,
    backupType: BackupType.Full,
    createdBy: "admin");

await backupService.MarkBackupAsCompletedAsync(backup.BackupId, 102400, 1500);
await backupService.VerifyBackupAsync(backup.BackupId, "admin");

Console.WriteLine($"Backup created and verified: {backup.BackupId}");
```

### Example 3: Batch Operations

```csharp
var backupService = provider.GetRequiredService<IBackupService>();

// Create backups for multiple databases
var databaseIds = new[] { "db1", "db2", "db3", "db4" };

var backupTasks = databaseIds.Select(async dbId =>
{
    var backup = await backupService.CreateBackupAsync(
        databaseId: dbId,
        backupType: BackupType.Full,
        createdBy: "system");
    
    await backupService.MarkBackupAsCompletedAsync(
        backupId: backup.BackupId,
        sizeBytes: 1024000,
        durationMs: 2000);
    
    return backup;
});

var backups = await Task.WhenAll(backupTasks);
Console.WriteLine($"Created {backups.Length} backups");
```

### Example 4: Tenant Lifecycle Management

```csharp
var tenantService = provider.GetRequiredService<ITenantService>();

// Create tenant
var tenant = await tenantService.CreateTenantAsync(
    "New Customer",
    "A promising customer",
    "contact@customer.com");

Console.WriteLine($"Created: {tenant.TenantId} ({tenant.Status})");

// Activate for use
await tenantService.ActivateTenantAsync(tenant.TenantId);
Console.WriteLine("Tenant activated");

// Set metadata
await tenantService.SetTenantMetadataAsync(tenant.TenantId, "plan", "enterprise");
await tenantService.SetTenantMetadataAsync(tenant.TenantId, "region", "us-east-1");

// Retrieve updated tenant
var updated = await tenantService.GetTenantAsync(tenant.TenantId);
Console.WriteLine($"Updated: {updated.Name}");

// Suspend if needed
await tenantService.SuspendTenantAsync(tenant.TenantId);
Console.WriteLine("Tenant suspended");

// Archive when inactive
await tenantService.ArchiveTenantAsync(tenant.TenantId);
Console.WriteLine("Tenant archived");
```

### Example 5: Error Handling

```csharp
var tenantService = provider.GetRequiredService<ITenantService>();

try
{
    // Try to get non-existent tenant
    await tenantService.GetTenantAsync("invalid-id");
}
catch (TenantNotFoundException ex)
{
    Console.WriteLine($"Tenant not found: {ex.TenantId}");
}
catch (DatabaseAccessException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

### Example 6: Health Checks and Monitoring

```csharp
// Use HealthCheckService if available
var healthService = provider.GetService<HealthCheckService>();
if (healthService != null)
{
    var health = await healthService.CheckHealthAsync();
    Console.WriteLine($"System Status: {health.Status}");
    Console.WriteLine($"Uptime: {health.UptimeMinutes} minutes");
    Console.WriteLine($"Active Tenants: {health.ActiveTenants}");
}

// Use MetricsService for diagnostics
var metricsService = provider.GetService<MetricsService>();
if (metricsService != null)
{
    var metrics = metricsService.GetMetrics();
    Console.WriteLine($"Request Count: {metrics.TotalRequests}");
    Console.WriteLine($"Avg Response Time: {metrics.AverageResponseTime}ms");
    Console.WriteLine($"Error Rate: {metrics.ErrorRate:P}");
}
```

### Example 7: Data Export

```csharp
// Using data exporter to export tenant data
var exportService = provider.GetService<DataExporter>();
if (exportService != null)
{
    var dbId = "tenant-database-id";
    
    // Export as JSON
    var jsonData = await exportService.ExportAsJsonAsync(dbId);
    File.WriteAllText("export.json", jsonData);
    
    // Export as CSV
    var csvData = await exportService.ExportAsCsvAsync(dbId, "Users");
    File.WriteAllText("users.csv", csvData);
}
```

### Example 8: Search and Filtering

```csharp
var tenantService = provider.GetRequiredService<ITenantService>();

// Search tenants by name
var results = await tenantService.SearchTenantsAsync("Acme");
Console.WriteLine($"Found {results.Count} tenants matching 'Acme'");

foreach (var tenant in results)
{
    Console.WriteLine($"  - {tenant.Name} ({tenant.Status})");
}
```

## Advanced Topics

### Event-Driven Architecture

```csharp
// Subscribe to events
var eventBus = provider.GetService<IEventBus>();
if (eventBus != null)
{
    eventBus.Subscribe<TenantCreatedEvent>(async @event =>
    {
        Console.WriteLine($"Tenant created: {@event.TenantId}");
        // Handle event
    });
}
```

### Custom Caching

```csharp
var cacheService = provider.GetService<CacheService>();
if (cacheService != null)
{
    // Cache tenant data
    cacheService.Set($"tenant:{tenantId}", tenant, TimeSpan.FromMinutes(15));
    
    // Retrieve from cache
    var cached = cacheService.Get<Tenant>($"tenant:{tenantId}");
}
```

### Rate Limiting

```csharp
// Middleware automatically applies rate limiting based on configuration
// Configure in startup:
services.AddRateLimitingMiddleware(options =>
{
    options.RequestsPerSecond = 100;
    options.BurstSize = 200;
});
```

## Troubleshooting

### Common Issues

**Issue**: "Database is locked" errors
- **Cause**: Multiple connections trying to write simultaneously
- **Solution**: Increase `ConnectionTimeoutSeconds` and enable connection pooling

**Issue**: Migration fails with "column already exists"
- **Cause**: Migration was partially applied in a previous run
- **Solution**: Check migration history and adjust script or rollback

**Issue**: Backup verification fails
- **Cause**: Backup file corrupted or incomplete
- **Solution**: Create new backup or restore from earlier backup

**Issue**: Out of memory with large datasets
- **Cause**: Loading entire dataset without pagination
- **Solution**: Use pagination with GetDatabaseBackupsAsync(pageSize: 50)

### Enable Debug Logging

```csharp
services.AddLogging(builder =>
    builder.AddConsole()
           .SetMinimumLevel(LogLevel.Debug)
           .AddFilter("SqliteMultiTenant", LogLevel.Debug));
```

### Performance Tuning

```csharp
services.AddSqliteMultiTenant(connectionString, options =>
{
    // Increase connection pool
    options.MaxConnections = 50;
    
    // Enable caching
    options.EnableCaching = true;
    options.CacheExpirationMinutes = 30;
    
    // Enable batch operations
    options.EnableBatchOperations = true;
    options.BatchSize = 500;
    options.MaxDegreeOfParallelism = Environment.ProcessorCount;
});
```

## Performance

Benchmarks run on .NET 10.0, BenchmarkDotNet 0.14.0, x64, Release build.

### Tenant Validation

Hot path executed on every inbound tenant-resolution request.

| Method | Mean | Allocated |
|---|---|---|
| `ValidateTenantId` (valid slug) | 138 ns | 72 B |
| `ValidateTenantId` (reserved word) | 94 ns | 0 B |
| `ValidateTenantId` (SQL-injection input) | 162 ns | 72 B |
| `ValidateTenantName` | 158 ns | 72 B |
| `GenerateTenantId` | 1.12 µs | 480 B |

Key optimisations: `FrozenSet<string>` for O(1) reserved-ID lookup; `RegexOptions.Compiled`; span + `OrdinalIgnoreCase` scan replaces per-call `ToUpper()` allocation.

### String Operations

Used during cache-key generation, file-path sanitization, and schema mapping.

| Method | Mean | Allocated |
|---|---|---|
| `ComputeSha256Hash` (44-char input) | 598 ns | 128 B |
| `ComputeMd5Hash` (44-char input) | 389 ns | 128 B |
| `ToSnakeCase` | 274 ns | 80 B |
| `ToCamelCase` | 318 ns | 96 B |
| `SanitizeForFilePath` | 184 ns | 48 B |

Key optimisations: `ArrayPool<byte>` for UTF-8 encode buffer (returned immediately after hash); static compiled `Regex` for `ToSnakeCase`; single-pass `ArrayPool<char>` write in `SanitizeForFilePath` replaces per-invalid-char `string.Replace` loop.

### Query Builder

Exercised by every repository method that resolves tenants, migrations, or backups.

| Method | Mean | Allocated |
|---|---|---|
| `QueryBuilder` — simple SELECT | 496 ns | 232 B |
| `QueryBuilder` — SELECT + ORDER BY + LIMIT | 712 ns | 336 B |
| `QueryBuilder` — SELECT + INNER JOIN | 634 ns | 288 B |
| `InsertBuilder` — 5-column INSERT | 548 ns | 296 B |

Key optimisation: `Build()` now uses chained `StringBuilder.Append` instead of `string.Join` + LINQ projection + string interpolation, eliminating one delegate allocation and one intermediate string per column per call.

### Running Benchmarks

```bash
cd benchmarks/sqlite-multi-tenant.Benchmarks
dotnet run -c Release -- --filter "*"

# Run a specific class
dotnet run -c Release -- --filter "*TenantValidation*"
dotnet run -c Release -- --filter "*StringOperations*"
dotnet run -c Release -- --filter "*QueryBuilder*"
```

## Testing

The test suite covers core services, model behaviour, and validation logic.

### Running Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Filter to a specific class
dotnet test --filter "ClassName=TenantServiceTests"

# Collect code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Coverage

| Test File | What it covers |
|---|---|
| `TenantServiceTests.cs` | Tenant CRUD, lifecycle transitions, metadata |
| `TenantNameValidatorTests.cs` | Name validation rules, reserved words, edge cases |
| `BackupModelTests.cs` | Backup model properties, tag management, expiration |

## Related Projects

Part of a collection of .NET libraries and tools. See more at [github.com/sarmkadan](https://github.com/sarmkadan).

### Integration Examples

**ASP.NET Core minimal API** — register once, resolve anywhere:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqliteMultiTenant("Data Source=master.db;", options =>
{
    options.MaxConnections = 20;
    options.DatabaseDirectory = "databases";
    options.EnableCaching = true;
    options.BackupRetentionDays = 30;
});

var app = builder.Build();

app.MapGet("/tenants/{id}", async (string id, ITenantService svc) =>
    await svc.GetTenantAsync(id));

await app.RunAsync();
```

**Generic host / background worker** — automated backup scheduling:

```csharp
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSqliteMultiTenant("Data Source=master.db;", options =>
        {
            options.BackupRetentionDays = 30;
            options.MaxBackupsPerDatabase = 50;
        });
        services.AddHostedService<BackupScheduler>();
    })
    .Build();

await host.RunAsync();
```

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Standards

- Follow existing code style and naming conventions
- Add XML documentation comments for public APIs
- Include unit tests for new features
- Update README.md for user-facing changes
- Ensure all tests pass before submitting PR

## License

This project is licensed under the MIT License - see [LICENSE](LICENSE) file for details.

Copyright © 2026 Vladyslav Zaiets

## Support

For issues, questions, or suggestions:
- GitHub Issues: [Report an issue](https://github.com/Sarmkadan/sqlite-multi-tenant/issues)
- Email: rutova2@gmail.com
- Website: https://sarmkadan.com

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)
