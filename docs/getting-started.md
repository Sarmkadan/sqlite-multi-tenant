# Getting Started with SQLite Multi-Tenant

This guide will help you set up and use SQLite Multi-Tenant in your .NET application.

## Table of Contents

1. [Installation](#installation)
2. [Basic Setup](#basic-setup)
3. [Creating Your First Tenant](#creating-your-first-tenant)
4. [Working with Databases](#working-with-databases)
5. [Managing Migrations](#managing-migrations)
6. [Creating Backups](#creating-backups)
7. [Next Steps](#next-steps)

## Installation

### Via NuGet Package Manager

```bash
dotnet add package SqliteMultiTenant
```

### From Source

```bash
git clone https://github.com/Sarmkadan/sqlite-multi-tenant.git
cd sqlite-multi-tenant
dotnet build
dotnet pack
```

Then add to your project:

```xml
<ItemGroup>
    <ProjectReference Include="path/to/SqliteMultiTenant.csproj" />
</ItemGroup>
```

## Basic Setup

### 1. Create a New Console Application

```bash
dotnet new console -n MyMultiTenantApp
cd MyMultiTenantApp
dotnet add package SqliteMultiTenant
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Logging.Console
```

### 2. Configure Services in Program.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Services;

var services = new ServiceCollection();

// 1. Add logging (required)
services.AddLogging(builder =>
    builder.AddConsole()
           .SetMinimumLevel(LogLevel.Information));

// 2. Configure SQLite Multi-Tenant
var masterConnection = "Data Source=master.db;Version=3;";
services.AddSqliteMultiTenant(masterConnection, options =>
{
    // Connection settings
    options.MaxConnections = 20;
    options.ConnectionTimeoutSeconds = 30;
    
    // Storage settings
    options.DatabaseDirectory = Path.Combine(
        Directory.GetCurrentDirectory(), 
        "databases");
    options.BackupDirectory = Path.Combine(
        Directory.GetCurrentDirectory(), 
        "backups");
    
    // Feature settings
    options.EnableLogging = true;
    options.EnableAuditing = true;
    options.BackupRetentionDays = 30;
});

// 3. Build service provider
var serviceProvider = services.BuildServiceProvider();

// Create directories if they don't exist
Directory.CreateDirectory("databases");
Directory.CreateDirectory("backups");

// Now you can use the services
await Main(serviceProvider);

async Task Main(IServiceProvider provider)
{
    // Your application code here
}
```

## Creating Your First Tenant

### Simple Tenant Creation

```csharp
var tenantService = serviceProvider.GetRequiredService<ITenantService>();

// Create a tenant
var tenant = await tenantService.CreateTenantAsync(
    name: "Acme Corporation",
    description: "Our first customer",
    contactEmail: "admin@acme.com");

Console.WriteLine($"✓ Tenant created!");
Console.WriteLine($"  ID: {tenant.TenantId}");
Console.WriteLine($"  Name: {tenant.Name}");
Console.WriteLine($"  Status: {tenant.Status}");
Console.WriteLine($"  Created: {tenant.CreatedAt:O}");
```

### Retrieve Tenant Information

```csharp
// Get a specific tenant
var tenantId = "your-tenant-id";
var tenant = await tenantService.GetTenantAsync(tenantId);

if (tenant != null)
{
    Console.WriteLine($"Name: {tenant.Name}");
    Console.WriteLine($"Status: {tenant.Status}");
    Console.WriteLine($"Databases: {tenant.Metadata.Count}");
}
else
{
    Console.WriteLine("Tenant not found");
}
```

### List All Tenants

```csharp
var allTenants = await tenantService.GetAllTenantsAsync();

Console.WriteLine($"Total tenants: {allTenants.Count}");
foreach (var t in allTenants)
{
    Console.WriteLine($"  - {t.Name} ({t.Status})");
}
```

### Manage Tenant Status

```csharp
var tenantId = "your-tenant-id";

// Activate a tenant
await tenantService.ActivateTenantAsync(tenantId);
Console.WriteLine("Tenant activated");

// Suspend a tenant (no new operations allowed)
await tenantService.SuspendTenantAsync(tenantId);
Console.WriteLine("Tenant suspended");

// Archive a tenant (read-only)
await tenantService.ArchiveTenantAsync(tenantId);
Console.WriteLine("Tenant archived");

// Deactivate a tenant
await tenantService.DeactivateTenantAsync(tenantId);
Console.WriteLine("Tenant deactivated");
```

## Working with Databases

### Create a Database Entry

```csharp
using SqliteMultiTenant.Models;

var tenantId = "your-tenant-id";
var databaseId = Guid.NewGuid().ToString();

var database = new TenantDatabase
{
    DatabaseId = databaseId,
    TenantId = tenantId,
    Name = "primary_db",
    FilePath = Path.Combine(
        "databases", 
        $"{tenantId}_primary.db"),
    SizeBytes = 0,
    SchemaVersion = 1,
    IsReadOnly = false,
    CreatedAt = DateTime.UtcNow
};

Console.WriteLine($"Database entry created: {databaseId}");
Console.WriteLine($"  File: {database.FilePath}");
```

### Query Database Statistics

```csharp
// Get database size
var sizeBytes = database.SizeBytes;
var sizeMB = sizeBytes / (1024.0 * 1024.0);
Console.WriteLine($"Database size: {sizeMB:F2} MB");

// Check if read-only
if (database.IsReadOnly)
{
    Console.WriteLine("Database is read-only (backup/restore in progress)");
}
```

## Managing Migrations

### Create Migrations

```csharp
var migrationService = serviceProvider.GetRequiredService<IMigrationService>();
var databaseId = "your-database-id";

// Migration 1: Initial schema
var migration1 = await migrationService.CreateMigrationAsync(
    databaseId: databaseId,
    version: "001",
    name: "InitialSchema",
    upScript: @"
        CREATE TABLE Users (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL,
            Email TEXT UNIQUE,
            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
        );
        
        CREATE TABLE Tenants (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL,
            Description TEXT,
            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
        );",
    downScript: @"
        DROP TABLE Users;
        DROP TABLE Tenants;");

Console.WriteLine($"✓ Created migration {migration1.Version}: {migration1.Name}");
```

### View Pending Migrations

```csharp
var pending = await migrationService.GetPendingMigrationsAsync(databaseId);

Console.WriteLine($"Pending migrations: {pending.Count}");
foreach (var migration in pending)
{
    Console.WriteLine($"  - {migration.Version}: {migration.Name}");
    Console.WriteLine($"    Status: {migration.Status}");
    Console.WriteLine($"    Rollbackable: {migration.IsRollbackable}");
}
```

### Apply Migrations

```csharp
// Note: This marks migration as applied in the system
// Actual SQL execution would be done by your application

var migration = pending.FirstOrDefault();
if (migration != null)
{
    // Execute the migration script on the actual database
    // ... your SQL execution code ...
    
    // Then mark as completed
    var startTime = DateTime.UtcNow;
    // ... execute SQL ...
    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
    
    await migrationService.MarkMigrationAsCompletedAsync(
        migrationId: migration.MigrationId,
        executionMs: (long)duration);
    
    Console.WriteLine($"✓ Migration applied: {migration.Name}");
}
```

### Rollback Migrations

```csharp
var applied = await migrationService.GetAppliedMigrationsAsync(databaseId);
var lastMigration = applied.OrderByDescending(m => m.Version).FirstOrDefault();

if (lastMigration?.IsRollbackable ?? false)
{
    // Execute down script on actual database
    // ... your SQL execution code ...
    
    await migrationService.RollbackMigrationAsync(lastMigration.MigrationId);
    Console.WriteLine($"✓ Rolled back: {lastMigration.Name}");
}
```

## Creating Backups

### Create a Full Backup

```csharp
var backupService = serviceProvider.GetRequiredService<IBackupService>();
var databaseId = "your-database-id";

var backup = await backupService.CreateBackupAsync(
    databaseId: databaseId,
    backupType: Constants.BackupType.Full,
    createdBy: "admin@acme.com");

Console.WriteLine($"✓ Backup created: {backup.BackupId}");
Console.WriteLine($"  Type: {backup.BackupType}");
Console.WriteLine($"  Status: {backup.Status}");
```

### Mark Backup as Complete

```csharp
// After backup file is written
var backupId = backup.BackupId;
var sizeBytes = 1024000; // Size of backup file
var durationMs = 2500;   // Time taken

await backupService.MarkBackupAsCompletedAsync(
    backupId: backupId,
    sizeBytes: sizeBytes,
    durationMs: durationMs);

Console.WriteLine($"✓ Backup completed");
Console.WriteLine($"  Size: {sizeBytes / 1024.0:F2} KB");
Console.WriteLine($"  Duration: {durationMs} ms");
```

### Verify Backup Integrity

```csharp
await backupService.VerifyBackupAsync(
    backupId: backup.BackupId,
    verifiedBy: "admin@acme.com");

var verified = await backupService.GetBackupAsync(backup.BackupId);
Console.WriteLine($"✓ Backup verified: {verified.IsVerified}");
```

### List Backups

```csharp
var backups = await backupService.GetDatabaseBackupsAsync(
    databaseId: databaseId,
    pageSize: 50);

Console.WriteLine($"Total backups: {backups.Count}");
foreach (var b in backups.OrderByDescending(b => b.CreatedAt))
{
    Console.WriteLine($"  - {b.BackupId}");
    Console.WriteLine($"    Created: {b.CreatedAt:O}");
    Console.WriteLine($"    Status: {b.Status}");
    Console.WriteLine($"    Size: {b.SizeBytes / 1024.0:F2} KB");
    Console.WriteLine($"    Verified: {b.IsVerified}");
}
```

### Add Tags to Backups

```csharp
// Tag backups for organization
await backupService.AddBackupTagAsync(backup.BackupId, "production");
await backupService.AddBackupTagAsync(backup.BackupId, "daily");
await backupService.AddBackupTagAsync(backup.BackupId, "archive");

var tags = await backupService.GetBackupTagsAsync(backup.BackupId);
Console.WriteLine($"Tags: {string.Join(", ", tags)}");
```

## Next Steps

Now that you understand the basics:

1. **Read the [Architecture Guide](architecture.md)** to understand how the system works
2. **Explore the [API Reference](api-reference.md)** for detailed method documentation
3. **Check out the [Examples](../examples/)** directory for complete sample applications
4. **Review [Configuration Options](../README.md#configuration)** to customize for your needs
5. **Learn about [Deployment](deployment.md)** for production setups

## Common Patterns

### Transaction-like Operations

```csharp
// Backup before migration
var backup = await backupService.CreateBackupAsync(databaseId, BackupType.Full, "admin");
await backupService.MarkBackupAsCompletedAsync(backup.BackupId, sizeBytes, duration);

// Apply migration
var migration = (await migrationService.GetPendingMigrationsAsync(databaseId)).First();
// ... execute SQL ...
await migrationService.MarkMigrationAsCompletedAsync(migration.MigrationId, duration);
```

### Error Handling

```csharp
try
{
    var tenant = await tenantService.GetTenantAsync(tenantId);
}
catch (TenantNotFoundException)
{
    Console.WriteLine("Tenant does not exist");
}
catch (DatabaseAccessException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}
```

### Pagination with Large Datasets

```csharp
const int pageSize = 100;
var allBackups = new List<Models.Backup>();

for (int skip = 0; ; skip += pageSize)
{
    var page = await backupService.GetDatabaseBackupsAsync(databaseId, pageSize);
    if (page.Count == 0) break;
    
    allBackups.AddRange(page);
    if (page.Count < pageSize) break;
}
```

## Troubleshooting

**Q: Getting "Database is locked" errors?**  
A: SQLite locks databases during writes. Ensure you're using proper connection management and increasing timeouts.

**Q: Migrations are failing?**  
A: Check your SQL scripts for syntax errors. Consider creating a backup before applying migrations.

**Q: Need to reset everything?**  
A: Delete the database files in the `databases` and `backups` directories and create new tenants.

For more help, see the [Troubleshooting section in the README](../README.md#troubleshooting).
