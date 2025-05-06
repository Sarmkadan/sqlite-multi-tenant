# SQLite Multi-Tenant Examples

This directory contains complete, runnable example applications demonstrating various features of the SQLite Multi-Tenant library.

## Quick Start

Each example is a standalone C# program that can be run independently. They all demonstrate real-world usage patterns.

### Running Examples

1. **Create a test project:**
   ```bash
   cd examples
   dotnet new console -n MyExample
   cd MyExample
   ```

2. **Copy the example file:**
   ```bash
   cp ../<example-file>.cs Program.cs
   ```

3. **Add the NuGet package:**
   ```bash
   dotnet add package SqliteMultiTenant
   ```

4. **Run the example:**
   ```bash
   dotnet run
   ```

## Examples Overview

### 1-basic-setup.cs - Basic Setup & Tenant Creation

**Purpose**: Learn how to set up the library and create tenants.

**Topics:**
- Service registration and configuration
- Creating tenants
- Retrieving tenant information
- Listing all tenants

**Key Learnings:**
- How to configure `AddSqliteMultiTenant`
- The basic tenant lifecycle
- Error handling basics

**Use Cases:**
- SaaS onboarding flow
- Multi-tenant application initialization
- User signup and tenant creation

**Duration**: ~5 minutes

```bash
# To run:
cd examples && dotnet new console -n Example1
cp 1-basic-setup.cs Program.cs
dotnet add package SqliteMultiTenant
dotnet run
```

### 2-migrations-example.cs - Database Migrations

**Purpose**: Understand database schema versioning and migrations.

**Topics:**
- Creating migration definitions
- Viewing pending migrations
- Simulating migration execution
- Viewing applied migrations
- Rollback capabilities

**Key Learnings:**
- How migrations track schema changes
- Migration versioning and ordering
- Rollback scripts for safety
- Migration execution workflow

**Use Cases:**
- Schema evolution for growing features
- Multi-tenant schema synchronization
- Database version tracking
- Zero-downtime deployments

**Duration**: ~10 minutes

```bash
# To run:
cd examples && dotnet new console -n Example2
cp 2-migrations-example.cs Program.cs
dotnet add package SqliteMultiTenant
dotnet run
```

### 3-backup-restore.cs - Backup & Recovery

**Purpose**: Master backup creation, verification, and management.

**Topics:**
- Creating different backup types (Full, Incremental, Differential)
- Marking backups complete
- Verifying backup integrity
- Adding backup tags
- Listing and managing backups
- Setting expiration policies
- Backup statistics

**Key Learnings:**
- Different backup strategies
- Backup lifecycle and states
- Verification and integrity checking
- Tag-based organization
- Retention policies
- Recovery readiness

**Use Cases:**
- Automated daily backups
- Point-in-time recovery
- Disaster recovery planning
- Backup retention compliance
- Multi-backup organization

**Duration**: ~10 minutes

```bash
# To run:
cd examples && dotnet new console -n Example3
cp 3-backup-restore.cs Program.cs
dotnet add package SqliteMultiTenant
dotnet run
```

### 4-error-handling.cs - Exception Handling

**Purpose**: Implement robust error handling in production applications.

**Topics:**
- Custom exception types
- Try-catch patterns
- Specific vs generic exception handling
- Retry logic with exponential backoff
- Batch operations with error isolation
- Logging errors with context

**Key Learnings:**
- How to catch library-specific exceptions
- Error recovery patterns
- Retry strategies
- Graceful degradation
- Error logging best practices

**Use Cases:**
- API error responses
- Batch import validation
- Data synchronization
- Resilient background jobs
- User-facing error messages

**Duration**: ~8 minutes

```bash
# To run:
cd examples && dotnet new console -n Example4
cp 4-error-handling.cs Program.cs
dotnet add package SqliteMultiTenant
dotnet run
```

### 5-advanced-operations.cs - Advanced Multi-Tenant Operations

**Purpose**: Implement complex, real-world scenarios at scale.

**Topics:**
- Batch tenant creation (parallel)
- Metadata management
- Search operations
- Tenant status transitions
- Multi-database per tenant setup
- Statistics and reporting
- Bulk operations
- Performance optimization

**Key Learnings:**
- Parallel operations for performance
- Custom metadata storage
- Search and filtering
- Lifecycle management patterns
- Batch processing
- Performance metrics

**Use Cases:**
- Bulk customer imports
- Multi-database architectures
- Custom tenant attributes
- Advanced tenant search
- Analytics and reporting
- Performance-critical operations

**Duration**: ~12 minutes

```bash
# To run:
cd examples && dotnet new console -n Example5
cp 5-advanced-operations.cs Program.cs
dotnet add package SqliteMultiTenant
dotnet run
```

## Complete Scenario: Multi-Tenant SaaS Platform

Here's a realistic workflow combining multiple examples:

```csharp
// 1. Initialize system (Example 1)
var tenantService = provider.GetRequiredService<ITenantService>();
var migrationService = provider.GetRequiredService<IMigrationService>();
var backupService = provider.GetRequiredService<IBackupService>();

// 2. Create tenant during signup
var tenant = await tenantService.CreateTenantAsync(
    name: "Customer Inc",
    description: "Enterprise customer",
    contactEmail: "admin@customer.com");

// 3. Set metadata for billing/configuration
await tenantService.SetTenantMetadataAsync(tenant.TenantId, "plan", "enterprise");
await tenantService.SetTenantMetadataAsync(tenant.TenantId, "billing_email", "billing@customer.com");

// 4. Setup database with migrations (Example 2)
var databaseId = Guid.NewGuid().ToString();
var db = new TenantDatabase { /* ... */ };

var migration = await migrationService.CreateMigrationAsync(
    databaseId: databaseId,
    version: "001",
    name: "InitialSchema",
    upScript: /* schema SQL */,
    downScript: /* cleanup SQL */);

// 5. Create baseline backup before going live (Example 3)
var backup = await backupService.CreateBackupAsync(
    databaseId: databaseId,
    backupType: BackupType.Full,
    createdBy: "system");

await backupService.MarkBackupAsCompletedAsync(backup.BackupId, sizeBytes: 512000, durationMs: 1500);
await backupService.VerifyBackupAsync(backup.BackupId, "system");

// 6. Handle errors during operations (Example 4)
try
{
    // Various operations
}
catch (TenantNotFoundException ex)
{
    logger.LogError($"Tenant not found: {ex.TenantId}");
}

// 7. Bulk operations for scale (Example 5)
var customers = await GetNewCustomersAsync();
var tasks = customers.Select(c => 
    tenantService.CreateTenantAsync(c.Name, c.Email));
await Task.WhenAll(tasks);
```

## Common Patterns

### Pattern 1: Safe Tenant Creation with Verification

```csharp
var tenant = await tenantService.CreateTenantAsync(name, email);
var verified = await tenantService.GetTenantAsync(tenant.TenantId);
if (verified != null && verified.Status == TenantStatus.Active)
{
    // Safe to proceed
}
```

### Pattern 2: Backup-Migrate-Verify

```csharp
// Create backup
var backup = await backupService.CreateBackupAsync(dbId, BackupType.Full, "system");
await backupService.MarkBackupAsCompletedAsync(backup.BackupId, size, duration);

// Apply migration
var migration = (await migrationService.GetPendingMigrationsAsync(dbId)).First();
// Execute migration...
await migrationService.MarkMigrationAsCompletedAsync(migration.MigrationId, execTime);

// Verify backup
await backupService.VerifyBackupAsync(backup.BackupId, "system");
```

### Pattern 3: Batch with Error Handling

```csharp
var results = new List<(string id, bool success, string error)>();

foreach (var item in items)
{
    try
    {
        var result = await operation(item);
        results.Add((item.Id, true, null));
    }
    catch (Exception ex)
    {
        results.Add((item.Id, false, ex.Message));
    }
}

var successCount = results.Count(r => r.success);
logger.LogInformation($"Batch completed: {successCount}/{items.Count} succeeded");
```

## Testing Your Code

Each example includes realistic data and error scenarios. Enhance them by:

1. **Adding assertions** to verify results
2. **Adding error scenarios** (invalid inputs, missing resources)
3. **Measuring performance** with `Stopwatch`
4. **Testing concurrency** with `Task.WhenAll`
5. **Verifying side effects** (files created, databases modified)

## Files Generated

After running examples, you'll see:

```
databases/
  master.db              # System metadata
  <tenant-id>.db        # Per-tenant database
  
backups/
  <backup-id>.db        # Backup files
```

## Next Steps

After exploring these examples:

1. **Read** [Getting Started Guide](../docs/getting-started.md)
2. **Review** [Architecture Guide](../docs/architecture.md)
3. **Check** [Deployment Guide](../docs/deployment.md)
4. **Build** your own application using these patterns

## Production Considerations

When moving from examples to production:

- Enable encryption for sensitive data
- Implement comprehensive error logging
- Add health checks and monitoring
- Configure appropriate backup retention
- Use environment variables for configuration
- Implement rate limiting
- Add request logging
- Setup audit trails
- Monitor disk space
- Plan for scaling

See [Deployment Guide](../docs/deployment.md) for details.

## Questions?

- Check [FAQ](../docs/faq.md) for common questions
- Review [Architecture](../docs/architecture.md) for design details
- Open [GitHub Issues](https://github.com/Sarmkadan/sqlite-multi-tenant/issues) for bugs

---

Happy learning! These examples are production-grade code. Feel free to use them as templates for your own applications.
