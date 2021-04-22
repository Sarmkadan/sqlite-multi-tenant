# Architecture Guide

## Overview

SQLite Multi-Tenant uses a layered, service-oriented architecture designed for scalability, maintainability, and separation of concerns. This document explains the architectural design and key components.

## Architectural Layers

### 1. Presentation Layer (API / CLI)

Handles user input and output.

**Components:**
- **API Controllers** (`Api/Controllers/`) - RESTful endpoints for all operations
- **CLI Application** (`Cli/`) - Command-line interface for automation
- **Middleware Pipeline** - Request/response processing (correlation IDs, error handling, logging)
- **Response Wrappers** - Standardized JSON responses with metadata

**Key Files:**
- `Api/Controllers/TenantController.cs` - Tenant CRUD operations
- `Api/Controllers/BackupController.cs` - Backup management
- `Api/Controllers/MigrationController.cs` - Migration tracking
- `Api/Controllers/DatabaseController.cs` - Database statistics
- `Cli/CliApplication.cs` - CLI orchestration

### 2. Service Layer (Business Logic)

Implements core functionality and business rules.

**Core Services:**
- **ITenantService** - Tenant lifecycle management
- **IMigrationService** - Database schema versioning
- **IBackupService** - Point-in-time backups

**Supporting Services:**
- **IHealthCheckService** - System health diagnostics
- **IMetricsService** - Performance metrics
- **IAuditLogger** - Audit trail tracking
- **ICacheService** - Result caching
- **IEventBus** - Event pub-sub

**Key Responsibilities:**
- Business rule validation
- Service coordination
- Error handling
- Event publishing
- Caching strategy

### 3. Data Access Layer (Repositories)

Handles all database operations using the Repository pattern.

**Repository Interfaces:**
- **ITenantRepository** - Tenant data operations
- **IMigrationRepository** - Migration tracking
- **IBackupRepository** - Backup metadata
- **IGenericRepository<T>** - Base CRUD operations

**Characteristics:**
- Async/await support
- Pagination
- Filtering
- Efficient queries
- SQLite-specific optimizations

### 4. Data Layer

Actual SQLite databases with per-tenant isolation.

**Structure:**
```
Master Database (master.db)
├── Tenants table
├── TenantDatabases table
├── Migrations table
└── Backups table

Tenant Database 1 (tenant1.db)
├── User-defined tables
└── Schema versioning

Tenant Database 2 (tenant2.db)
├── User-defined tables
└── Schema versioning
```

## Core Abstractions

### Domain Models

Located in `Models/`:

```csharp
public class Tenant
{
    public string TenantId { get; set; }          // Unique identifier
    public string Name { get; set; }              // Display name
    public TenantStatus Status { get; set; }      // Lifecycle status
    public Dictionary<string, string> Metadata { get; set; } // Custom data
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
}

public class TenantDatabase
{
    public string DatabaseId { get; set; }        // Unique identifier
    public string TenantId { get; set; }          // Foreign key
    public string FilePath { get; set; }          // SQLite file location
    public int SchemaVersion { get; set; }        // Schema version
    public long SizeBytes { get; set; }           // Database file size
    public bool IsReadOnly { get; set; }          // Protection flag
}

public class Migration
{
    public string MigrationId { get; set; }       // Unique identifier
    public string DatabaseId { get; set; }        // Target database
    public string Version { get; set; }           // Semantic version
    public string Name { get; set; }              // Description
    public string UpScript { get; set; }          // Apply script
    public string DownScript { get; set; }        // Rollback script
    public MigrationStatus Status { get; set; }   // Execution status
    public DateTime? ExecutedAt { get; set; }     // When applied
}

public class Backup
{
    public string BackupId { get; set; }          // Unique identifier
    public string DatabaseId { get; set; }        // Source database
    public BackupType BackupType { get; set; }    // Full/Incremental/Differential
    public BackupStatus Status { get; set; }      // Pending/Completed/Failed
    public long SizeBytes { get; set; }           // File size
    public DateTime ExpiresAt { get; set; }       // Retention deadline
    public bool IsVerified { get; set; }          // Integrity checked
    public string BackupPath { get; set; }        // Storage location
}
```

## Cross-Cutting Concerns

### 1. Exception Handling

Custom exception hierarchy for precise error handling:

```
Exception
├── TenantNotFoundException
├── DatabaseAccessException
├── MigrationException
└── BackupException
```

Handled by `ExceptionProcessor` which maps to HTTP status codes.

### 2. Validation

Fluent validation framework:
- **TenantValidator** - Tenant name, email, status
- **DataValidator** - Generic validation rules
- **ValidationRuleBuilder** - DSL for custom rules

### 3. Logging & Monitoring

**Logging Levels:**
- **Fatal** - System failures
- **Error** - Operation failures
- **Warning** - Unexpected but recoverable
- **Information** - Key operations (tenant creation, migrations)
- **Debug** - Detailed diagnostics
- **Trace** - Very detailed tracing

**Monitoring:**
- **RequestResponseLogger** - HTTP audit trail
- **PerformanceMonitor** - Execution timing
- **AuditLogger** - Security audit trail
- **MetricsService** - KPI tracking

### 4. Caching Strategy

```
Application Logic
      ↓
Cache Service (In-Memory LRU)
      ↓
Repository Layer
      ↓
SQLite Database
```

**Cache Configuration:**
- TTL: Configurable (default 15 minutes)
- Max Items: Configurable (default 1000)
- Eviction: LRU (Least Recently Used)
- Keys: Entity ID + operation type

### 5. Event System

Event-driven architecture for decoupled operations:

```
Domain Events (TenantCreated, BackupCompleted, etc.)
      ↓
Event Bus (In-Process Queue)
      ↓
Event Handlers (Async Processing)
      ↓
Webhooks & External Systems
```

### 6. Security

**Components:**
- **EncryptionService** - AES-256 encryption
- **RateLimiter** - Token bucket algorithm
- **AuthenticationInterceptor** - JWT/Bearer validation
- **RequestInterceptor** - Security headers

## Dependency Injection

Services are registered in `DependencyInjectionSetup.cs`:

```csharp
public static IServiceCollection AddSqliteMultiTenant(
    this IServiceCollection services,
    string masterConnectionString,
    Action<MultiTenantOptions> configure)
{
    // Register repositories
    services.AddScoped<ITenantRepository, TenantRepository>();
    services.AddScoped<IMigrationRepository, MigrationRepository>();
    services.AddScoped<IBackupRepository, BackupRepository>();

    // Register services
    services.AddScoped<ITenantService, TenantService>();
    services.AddScoped<IMigrationService, MigrationService>();
    services.AddScoped<IBackupService, BackupService>();

    // Register infrastructure
    services.AddSingleton<IEventBus, EventBusImpl>();
    services.AddSingleton<ICacheService, CacheService>();
    services.AddSingleton<IHealthCheckService, HealthCheckService>();

    // ... more registrations
}
```

## Data Flow Examples

### Creating a Tenant

```
User Request
    ↓
TenantController.CreateTenant()
    ↓
TenantService.CreateTenantAsync()
    - Validate input
    - Check for duplicates
    - Generate ID
    ↓
TenantRepository.CreateAsync()
    - Insert into master.db
    ↓
EventBus.PublishAsync(TenantCreatedEvent)
    - Notify subscribers
    - Send webhooks
    ↓
CacheService.Set()
    - Cache result
    ↓
Response to User
```

### Applying a Migration

```
CLI: "migration apply --database-id db1"
    ↓
CommandExecutor.ExecuteAsync()
    ↓
MigrationService.GetPendingMigrationsAsync()
    ↓
MigrationRepository.GetByStatusAsync(Pending)
    ↓
For each pending migration:
    - Create backup (optional)
    - Execute UpScript on tenant database
    - Call MarkMigrationAsCompletedAsync()
    - MigrationRepository.UpdateAsync()
    ↓
EventBus.PublishAsync(MigrationCompletedEvent)
    ↓
Response to CLI
```

### Creating a Backup

```
User Request: POST /api/backups
    ↓
BackupController.CreateBackupAsync()
    ↓
BackupService.CreateBackupAsync()
    - Validate database exists
    - Generate backup ID
    - Determine backup path
    ↓
BackupRepository.CreateAsync()
    - Insert metadata record
    ↓
[External] Copy database file to backup location
    ↓
BackupService.MarkBackupAsCompletedAsync()
    ↓
BackupRepository.UpdateAsync()
    ↓
EventBus.PublishAsync(BackupCompletedEvent)
    ↓
Response: Backup ID & location
```

## Design Patterns Used

### 1. Repository Pattern

Abstracts data access behind interfaces:
```csharp
public interface ITenantRepository
{
    Task<Tenant> CreateAsync(Tenant entity);
    Task<Tenant> GetByIdAsync(string id);
    Task<List<Tenant>> GetAllAsync();
    Task UpdateAsync(Tenant entity);
    Task DeleteAsync(string id);
}
```

### 2. Dependency Injection

Loose coupling via constructor injection:
```csharp
public class TenantService
{
    private readonly ITenantRepository _repository;
    private readonly ILogger<TenantService> _logger;

    public TenantService(ITenantRepository repository, ILogger<TenantService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
}
```

### 3. Async/Await

All I/O operations are asynchronous:
```csharp
public async Task<Tenant> GetTenantAsync(string tenantId)
{
    return await _repository.GetByIdAsync(tenantId);
}
```

### 4. Pub/Sub (Observer Pattern)

Event-driven notifications:
```csharp
public interface IEventBus
{
    void Subscribe<TEvent>(Func<TEvent, Task> handler);
    Task PublishAsync<TEvent>(TEvent @event);
}
```

### 5. Strategy Pattern

Pluggable implementations:
```csharp
public interface IOutputFormatter
{
    string Format(object data, FormatType type);
}

public class JsonFormatter : IOutputFormatter { }
public class CsvFormatter : IOutputFormatter { }
```

## Performance Considerations

### Connection Pooling

SQLite connections are pooled per tenant:
```csharp
// Configuration
options.MaxConnections = 20; // Per tenant

// Usage
var connection = await connectionManager.GetConnectionAsync(tenantId);
// ... use connection ...
// Connection returned to pool automatically
```

### Query Optimization

Repositories use efficient queries:
- Index-based lookups
- Projection (SELECT specific columns)
- Pagination (LIMIT/OFFSET)
- Prepared statements

### Caching Strategy

```
Hot Data (Frequently Accessed)
├── Tenants
├── Database Metadata
└── Recent Backups (TTL: 15 min)

Cold Data
├── Migration History
├── Archived Backups
└── Audit Logs
```

### Batch Operations

Support for bulk operations:
```csharp
public class BatchProcessor
{
    public async Task<BatchResult> ProcessBatchAsync<T>(
        IEnumerable<T> items,
        Func<T, Task> processor,
        int batchSize = 100,
        int maxDegreeOfParallelism = 4)
    {
        // Process items in parallel batches
    }
}
```

## Scalability Strategy

### Horizontal Scaling

1. **Stateless Services** - All services are stateless
2. **Shared Master DB** - Single master database for all tenants
3. **Per-Tenant DBs** - Independent SQLite files (can be distributed)
4. **Load Balancing** - API nodes behind load balancer

### Vertical Scaling

1. **Caching** - In-memory cache reduces DB hits
2. **Connection Pooling** - Reuses connections efficiently
3. **Batch Operations** - Processes large datasets in chunks
4. **Async I/O** - Non-blocking operations

## Extension Points

### Custom Services

```csharp
public interface ICustomService
{
    Task<T> DoSomethingAsync<T>(string tenantId);
}

services.AddScoped<ICustomService, CustomService>();
```

### Event Handlers

```csharp
var eventBus = serviceProvider.GetRequiredService<IEventBus>();
eventBus.Subscribe<TenantCreatedEvent>(async @event =>
{
    // Handle tenant creation
});
```

### Custom Repositories

```csharp
public class CustomRepository<T> : GenericRepository<T>
{
    // Extend with custom queries
}
```

## Testing Strategy

### Unit Testing

Mock repositories and external dependencies:
```csharp
var mockRepository = new Mock<ITenantRepository>();
var service = new TenantService(mockRepository.Object, logger);
```

### Integration Testing

Use in-memory SQLite for testing:
```csharp
var connection = new SqliteConnection("Data Source=:memory:");
var repository = new TenantRepository(connection);
```

## Deployment Architecture

```
┌─────────────────────────────────────┐
│     Load Balancer / API Gateway     │
└──────────────┬──────────────────────┘
               ↓
    ┌──────────┴──────────┐
    ↓                     ↓
┌─────────┐          ┌─────────┐
│API Node1│          │API Node2│
└────┬────┘          └────┬────┘
     │                    │
     └──────────┬─────────┘
                ↓
     ┌──────────────────────┐
     │  Master Database     │
     │  (master.db - shared)│
     └──────────────────────┘

Per-Tenant Databases:
     ┌──────────────────────┐
     │ Distributed Storage  │
     ├──────────────────────┤
     │ tenant1.db (Node1)   │
     │ tenant2.db (Node2)   │
     │ tenant3.db (Node1)   │
     │ ...                  │
     └──────────────────────┘
```

## Conclusion

The architecture is designed for:
- **Scalability** - Horizontal and vertical scaling
- **Maintainability** - Clear separation of concerns
- **Testability** - Dependency injection and mocking
- **Extensibility** - Plugin interfaces and event system
- **Reliability** - Error handling and monitoring
- **Performance** - Caching and optimization

For implementation details, see the source code with detailed comments.
