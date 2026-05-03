# SQLite Multi-Tenant Database Manager

A comprehensive .NET library for managing multi-tenant SQLite databases with per-tenant isolation, automated migrations, and backup management.

## Features

- **Multi-Tenant Isolation**: Secure per-tenant database management with complete data isolation
- **Tenant Management**: Full lifecycle management of tenants (create, update, delete, activate, suspend)
- **Database Migrations**: Track and execute database migrations with rollback capabilities
- **Backup Management**: Create, verify, and restore backups with expiration policies
- **Connection Management**: Handle tenant-specific database connections with pooling
- **Async/Await Support**: Fully asynchronous API for high-performance applications
- **Comprehensive Logging**: Built-in logging for all operations via ILogger
- **Validation**: Entity validation with detailed error messages
- **Custom Exceptions**: Specialized exception types for better error handling

## Project Structure

```
src/
├── Models/                          # Domain models
│   ├── Tenant.cs                   # Tenant entity
│   ├── TenantDatabase.cs           # Database configuration per tenant
│   ├── Migration.cs                # Migration tracking
│   ├── Backup.cs                   # Backup information
│   ├── TenantSettings.cs           # Tenant-specific settings
│   └── TenantContext.cs            # Runtime tenant context
├── Services/                        # Business logic layer
│   ├── ITenantService.cs           # Tenant service interface
│   ├── TenantService.cs            # Tenant service implementation
│   ├── IMigrationService.cs        # Migration service interface
│   ├── MigrationService.cs         # Migration service implementation
│   ├── IBackupService.cs           # Backup service interface
│   └── BackupService.cs            # Backup service implementation
├── Repositories/                    # Data access layer
│   ├── ITenantRepository.cs        # Tenant repository interface
│   ├── TenantRepository.cs         # Tenant SQLite implementation
│   ├── IMigrationRepository.cs     # Migration repository interface
│   ├── MigrationRepository.cs      # Migration SQLite implementation
│   ├── IBackupRepository.cs        # Backup repository interface
│   └── BackupRepository.cs         # Backup SQLite implementation
├── Exceptions/                      # Custom exception types
│   ├── TenantNotFoundException.cs
│   ├── DatabaseAccessException.cs
│   ├── MigrationException.cs
│   └── BackupException.cs
├── Constants/                       # Configuration constants and enums
│   ├── TenantConstants.cs          # Constants
│   └── Enums.cs                    # Enum definitions
├── Configuration/                   # Dependency injection setup
│   └── ServiceConfiguration.cs     # Service registration
└── Program.cs                       # Demo application entry point
```

## Installation

Clone the repository and build the project:

```bash
git clone https://github.com/vladyslav-zaiets/sqlite-multi-tenant.git
cd sqlite-multi-tenant
dotnet build
```

## Quick Start

### 1. Configure Services

```csharp
var services = new ServiceCollection();

services.AddLogging(builder => builder.AddConsole());

var connectionString = "Data Source=master.db;Version=3;";
services.AddSqliteMultiTenant(connectionString, options =>
{
    options.MaxConnections = 20;
    options.BackupRetentionDays = 30;
    options.EnableLogging = true;
});

var serviceProvider = services.BuildServiceProvider();
```

### 2. Create a Tenant

```csharp
var tenantService = serviceProvider.GetRequiredService<ITenantService>();

var tenant = await tenantService.CreateTenantAsync(
    name: "Acme Corporation",
    description: "Main customer",
    contactEmail: "admin@acme.com");

Console.WriteLine($"Tenant created: {tenant.TenantId}");
```

### 3. Manage Migrations

```csharp
var migrationService = serviceProvider.GetRequiredService<IMigrationService>();

var migration = await migrationService.CreateMigrationAsync(
    databaseId: "db-123",
    version: "001",
    name: "InitialSchema",
    upScript: "CREATE TABLE Users (Id INTEGER PRIMARY KEY, Name TEXT);",
    downScript: "DROP TABLE Users;");

var pending = await migrationService.GetPendingMigrationsAsync("db-123");
```

### 4. Create and Verify Backups

```csharp
var backupService = serviceProvider.GetRequiredService<IBackupService>();

var backup = await backupService.CreateBackupAsync(
    databaseId: "db-123",
    backupType: BackupType.Full,
    createdBy: "admin@acme.com");

await backupService.MarkBackupAsCompletedAsync(backup.BackupId, sizeBytes: 1024000, durationMs: 2500);
await backupService.VerifyBackupAsync(backup.BackupId, "admin@acme.com");
```

## Core Entities

### Tenant
Represents a customer or organization in the system.
- Lifecycle management (Active, Inactive, Suspended, Archived, Deleted)
- Metadata storage for custom key-value pairs
- Connection pooling configuration

### TenantDatabase
Represents a SQLite database associated with a tenant.
- Per-tenant database isolation
- Schema versioning
- Size tracking
- Encryption support

### Migration
Represents a database migration script.
- Up/Down script tracking
- Execution history
- Rollback capabilities
- Error tracking and recovery

### Backup
Represents a point-in-time backup of a tenant database.
- Multiple backup types (Full, Incremental, Differential)
- Verification and integrity checking
- Expiration policies
- Tagging system for organization

## Service Layer

### ITenantService
- GetTenantAsync() - Retrieve tenant details
- CreateTenantAsync() - Create new tenant
- UpdateTenantAsync() - Update tenant information
- DeleteTenantAsync() - Remove tenant
- GetAllTenantsAsync() - List all tenants
- ActivateTenantAsync() / DeactivateTenantAsync() - Manage status
- SearchTenantsAsync() - Search by name or email
- SetTenantMetadataAsync() - Store custom metadata

### IMigrationService
- CreateMigrationAsync() - Define new migration
- GetMigrationAsync() - Retrieve migration details
- GetPendingMigrationsAsync() - Get pending migrations
- GetAppliedMigrationsAsync() - Get applied migrations
- ExecuteMigrationAsync() - Start execution
- RollbackMigrationAsync() - Rollback migration
- MarkMigrationAsCompletedAsync() - Mark as complete
- MarkMigrationAsFailedAsync() - Mark as failed

### IBackupService
- CreateBackupAsync() - Create new backup
- GetBackupAsync() - Retrieve backup details
- GetDatabaseBackupsAsync() - List all backups for database
- GetCompletedBackupsAsync() - List completed backups
- MarkBackupAsCompletedAsync() - Mark backup complete
- VerifyBackupAsync() - Verify backup integrity
- SetBackupExpirationAsync() - Set expiration date
- AddBackupTagAsync() - Add tags for organization

## Repository Layer

All repositories follow the same pattern:
- **ITenantRepository** - Full CRUD + queries for tenants
- **IMigrationRepository** - Full CRUD + queries for migrations
- **IBackupRepository** - Full CRUD + queries for backups

Each repository provides:
- Async methods with CancellationToken support
- Paging support
- Search capabilities
- Status filtering
- Existence checks

## Exception Handling

```csharp
try
{
    await tenantService.GetTenantAsync(tenantId);
}
catch (TenantNotFoundException ex)
{
    Console.WriteLine($"Tenant not found: {ex.TenantId}");
}
catch (DatabaseAccessException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}
catch (MigrationException ex)
{
    Console.WriteLine($"Migration error: {ex.MigrationVersion}");
}
catch (BackupException ex)
{
    Console.WriteLine($"Backup error: {ex.BackupId}");
}
```

## Configuration Options

```csharp
services.AddSqliteMultiTenant(connectionString, options =>
{
    options.MaxConnections = 20;                    // Max connections per tenant
    options.ConnectionTimeoutSeconds = 30;         // Connection timeout
    options.BackupRetentionDays = 30;              // Backup retention period
    options.EnableEncryption = false;              // Enable database encryption
    options.BackupDirectory = "backups";           // Backup location
    options.DatabaseDirectory = "databases";       // Database location
    options.EnableLogging = true;                  // Enable ILogger integration
});
```

## Development

The project demonstrates:
- **Architecture**: Layered architecture with Models, Services, and Repositories
- **Patterns**: Repository pattern, Dependency Injection, async/await
- **Best Practices**: Entity validation, custom exceptions, comprehensive logging
- **Testing**: Entities with validation methods, service layer abstractions

## Requirements

- .NET 8.0 or higher
- SQLite 3
- System.Data.SQLite NuGet package

## License

This project is licensed under the MIT License - see LICENSE file for details.

## Author

**Vladyslav Zaiets**  
CTO & Software Architect  
https://sarmkadan.com

---

**Version**: 1.0.0  
**Last Updated**: 2026-05-03
