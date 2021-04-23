# Frequently Asked Questions

## General Questions

### Q: What is SQLite Multi-Tenant?
**A:** SQLite Multi-Tenant is a .NET library for managing multi-tenant SQLite databases with per-tenant isolation, automated migrations, comprehensive backup management, and advanced monitoring. It's designed for SaaS platforms and multi-tenant applications.

### Q: Why SQLite instead of SQL Server or PostgreSQL?
**A:** SQLite offers:
- **Zero setup** - No separate database server needed
- **Per-tenant databases** - True isolation and independent scaling
- **Embedded** - Deploy without infrastructure
- **Lightweight** - Minimal resource requirements
- **Portable** - Single file databases easy to backup/restore

### Q: Can I use this with existing databases?
**A:** Yes. The library manages tenant lifecycle and migrations, but can work with existing database files. You'll need to create tenant and database entries for existing databases.

### Q: Is this production-ready?
**A:** Yes. The library is production-grade with comprehensive error handling, logging, security features, and monitoring capabilities.

### Q: What's the maximum number of tenants supported?
**A:** Technically unlimited. Performance depends on:
- Available disk space (each tenant has own database)
- Available memory (connection pooling per tenant)
- Network I/O bandwidth (if using network storage)

Tested with 1000+ tenants without issues.

## Installation & Setup

### Q: Which .NET versions are supported?
**A:** .NET 8.0 and higher. Download from https://dotnet.microsoft.com/download

### Q: Do I need to install SQLite separately?
**A:** No. SQLite is included as a NuGet dependency (System.Data.SQLite).

### Q: How do I handle database path configuration?
**A:** Use MultiTenantOptions:

```csharp
services.AddSqliteMultiTenant(connectionString, options =>
{
    options.DatabaseDirectory = "/var/databases";  // Absolute path
    options.BackupDirectory = "/var/backups";
});
```

### Q: Can databases be on a network share?
**A:** Yes, but with caveats:
- Network latency affects performance
- File locking can cause issues
- Use local SSD for production when possible

## Database Operations

### Q: How do I execute custom SQL?
**A:** The library manages schema and backups. For custom queries:

```csharp
using var connection = new SqliteConnection("Data Source=tenant.db");
await connection.OpenAsync();

using var command = connection.CreateCommand();
command.CommandText = "SELECT * FROM Users WHERE Id = @id";
command.Parameters.AddWithValue("@id", 1);

var result = await command.ExecuteScalarAsync();
```

### Q: How do migrations work?
**A:** Migrations are version-controlled SQL scripts:

1. Create migration definition with up/down scripts
2. View pending migrations
3. Apply migrations (execute up script)
4. System tracks applied migrations
5. Can rollback (execute down script)

### Q: What if a migration fails halfway?
**A:** The migration is marked as failed in the system. You can:
1. Fix the issue
2. Manually correct the database state
3. Mark migration as completed
4. Or rollback and retry

### Q: Can I have multiple databases per tenant?
**A:** Yes. The system supports multiple TenantDatabase entries per tenant:

```csharp
var db1 = new TenantDatabase { DatabaseId = "primary", TenantId = "tenant1" };
var db2 = new TenantDatabase { DatabaseId = "analytics", TenantId = "tenant1" };
```

## Backups & Recovery

### Q: How often should I backup?
**A:** Depends on data criticality:
- **Critical systems**: Every hour
- **Normal systems**: Daily
- **Archive data**: Weekly

Configure retention:
```csharp
options.BackupRetentionDays = 30;
```

### Q: What's the difference between backup types?
**A:**
- **Full**: Complete copy of database (slowest, largest)
- **Incremental**: Only changes since last full backup (faster, smaller)
- **Differential**: Only changes since last any backup (medium)

### Q: How do I restore a backup?
**A:** Backup verification creates proof of recovery capability. To actually restore:

```csharp
// 1. Get backup
var backup = await backupService.GetBackupAsync(backupId);

// 2. Replace database file
File.Copy(backup.BackupPath, tenantDatabasePath, overwrite: true);

// 3. Verify restored database
using var connection = new SqliteConnection($"Data Source={tenantDatabasePath}");
await connection.OpenAsync();
```

### Q: Can I backup to cloud storage?
**A:** Yes. Implement custom backup handler or use cloud CLI:

```bash
# AWS S3
aws s3 cp backups/tenant1.db s3://backups/tenant1.db

# Azure Blob Storage
az storage blob upload --file backups/tenant1.db \
  --container backups --name tenant1.db

# Google Cloud Storage
gsutil cp backups/tenant1.db gs://backups/tenant1.db
```

### Q: How do I backup all tenants?
**A:** Use batch operations:

```csharp
var backupService = provider.GetRequiredService<IBackupService>();
var tenantService = provider.GetRequiredService<ITenantService>();

var tenants = await tenantService.GetAllTenantsAsync();

var backupTasks = tenants.Select(t => 
    backupService.CreateBackupAsync(
        databaseId: t.TenantId,
        backupType: BackupType.Full,
        createdBy: "system"));

await Task.WhenAll(backupTasks);
```

## Tenant Management

### Q: How do I rename a tenant?
**A:** Use UpdateTenantAsync:

```csharp
await tenantService.UpdateTenantAsync(
    tenantId: "tenant-id",
    name: "New Name",
    description: "Updated description");
```

### Q: What happens when I delete a tenant?
**A:** Default behavior is soft delete (marked as deleted). To hard delete:

1. Soft delete the tenant: `await tenantService.DeleteTenantAsync(tenantId)`
2. Delete the database file: `File.Delete(databasePath)`
3. Clear backups if desired

### Q: How do I search for tenants?
**A:**

```csharp
var results = await tenantService.SearchTenantsAsync("search term");
// Searches name, description, and contact email
```

### Q: Can I archive old tenants?
**A:** Yes:

```csharp
await tenantService.ArchiveTenantAsync(tenantId);
// Tenant becomes read-only

// Later, if needed:
await tenantService.ActivateTenantAsync(tenantId);
```

### Q: How do I track tenant metadata?
**A:**

```csharp
// Set metadata
await tenantService.SetTenantMetadataAsync(
    tenantId: "tenant-id",
    key: "subscription_plan",
    value: "enterprise");

// Get metadata
var plan = await tenantService.GetTenantMetadataAsync(
    tenantId: "tenant-id",
    key: "subscription_plan");
```

## Performance & Optimization

### Q: Why are queries slow?
**A:** Possible causes:
1. **Missing indexes** - Add indexes to frequently queried columns
2. **Large dataset** - Use pagination or filter criteria
3. **Cache disabled** - Enable caching: `options.EnableCaching = true`
4. **Network latency** - If using network storage, consider local cache

### Q: How do I improve performance?
**A:**

```csharp
services.AddSqliteMultiTenant(connectionString, options =>
{
    // Increase connection pool
    options.MaxConnections = 50;
    
    // Enable caching with longer TTL
    options.EnableCaching = true;
    options.CacheExpirationMinutes = 30;
    
    // Batch operations
    options.EnableBatchOperations = true;
    options.BatchSize = 500;
    options.MaxDegreeOfParallelism = Environment.ProcessorCount;
});
```

### Q: What's the cache hit rate?
**A:** Check metrics:

```csharp
var metricsService = provider.GetService<MetricsService>();
var metrics = metricsService.GetMetrics();
Console.WriteLine($"Cache Hit Rate: {metrics.CacheHitRate:P}");
```

Aim for 70%+ cache hit rate in production.

### Q: How many connections do I need?
**A:** Formula: `RequestsPerSecond * AverageQueryTimeSeconds + Buffer`

Example:
- 100 requests/sec
- 100ms average query time (0.1 seconds)
- = 100 * 0.1 + 10 buffer = 20 connections

## Error Handling

### Q: What exceptions can be thrown?
**A:**
- `TenantNotFoundException` - Tenant not found
- `DatabaseAccessException` - Database error
- `MigrationException` - Migration failure
- `BackupException` - Backup operation failure

### Q: How do I handle "Database is locked" errors?
**A:**
1. **Increase timeout**: `options.ConnectionTimeoutSeconds = 60`
2. **Enable connection pooling**: Automatic with MaxConnections
3. **Reduce write contention**: Batch operations
4. **Verify SQLite version**: Use SQLite 3.36+

### Q: Why is my migration failing?
**A:** Common causes:
1. **Syntax error** in SQL script - Test with `sqlite3` CLI first
2. **Column already exists** - Check migration history
3. **Foreign key constraint** - Check dependencies
4. **Insufficient permissions** - Verify file/directory permissions

Debug:

```csharp
try
{
    await migrationService.ExecuteMigrationAsync(migrationId);
}
catch (MigrationException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Migration: {ex.MigrationVersion}");
}
```

## Security

### Q: Should I enable encryption?
**A:** Yes for sensitive data:

```csharp
options.EnableEncryption = true;
options.EncryptionKey = "minimum-32-character-encryption-key";
```

### Q: How strong should the encryption key be?
**A:** 
- Minimum: 32 characters (256 bits)
- Recommended: Use a random key from key management service
- Store key securely (not in code)

### Q: How do I set rate limiting?
**A:** Configure in middleware:

```csharp
app.UseRateLimitingMiddleware(options =>
{
    options.RequestsPerSecond = 100;
    options.BurstSize = 200;
});
```

### Q: Is the system vulnerable to SQL injection?
**A:** No. The library uses:
- Parameterized queries
- ORM abstraction
- Input validation

Use parameterized queries for custom SQL:

```csharp
command.CommandText = "SELECT * FROM Users WHERE Id = @id";
command.Parameters.AddWithValue("@id", userId);
```

## Monitoring & Logging

### Q: How do I enable debug logging?
**A:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "SqliteMultiTenant": "Debug"
    }
  }
}
```

### Q: What metrics are available?
**A:**
- Total requests
- Average response time
- Error rate
- Cache hit rate
- Database operation counts
- System resource usage

### Q: How do I get health status?
**A:**

```csharp
var healthService = provider.GetService<HealthCheckService>();
var health = await healthService.CheckHealthAsync();

Console.WriteLine($"Status: {health.Status}");
Console.WriteLine($"Uptime: {health.UptimeMinutes} min");
Console.WriteLine($"Active Tenants: {health.ActiveTenants}");
```

### Q: Can I export audit logs?
**A:** Yes, if audit logging is enabled:

```csharp
var auditLogger = provider.GetService<AuditLogger>();
var logs = await auditLogger.GetLogsAsync(
    startDate: DateTime.UtcNow.AddDays(-30),
    endDate: DateTime.UtcNow);
```

## Integration

### Q: Can I use this with ASP.NET Core?
**A:** Yes. Inject services in controllers:

```csharp
[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTenant(string id)
    {
        var tenant = await _tenantService.GetTenantAsync(id);
        return Ok(tenant);
    }
}
```

### Q: How do I use this with Entity Framework?
**A:** The library manages database structure independently. For EF:

```csharp
var options = new DbContextOptionsBuilder<MyDbContext>()
    .UseSqlite($"Data Source={tenantDatabase.FilePath}")
    .Options;

using var dbContext = new MyDbContext(options);
var users = await dbContext.Users.ToListAsync();
```

### Q: Can I use with dependency injection containers?
**A:** Yes:

```csharp
// Autofac
var builder = new ContainerBuilder();
builder.RegisterModule<SqliteMultiTenantModule>();

// Ninject
kernel.Load(new SqliteMultiTenantModule());

// Custom container
var services = new ServiceCollection();
services.AddSqliteMultiTenant(...);
```

## Licensing & Contributing

### Q: What license is this?
**A:** MIT License. Free for commercial and personal use.

### Q: How do I contribute?
**A:** See CONTRIBUTING.md or:
1. Fork the repository
2. Create feature branch
3. Commit changes
4. Submit pull request

### Q: Where do I report bugs?
**A:** GitHub Issues: https://github.com/Sarmkadan/sqlite-multi-tenant/issues

### Q: Who maintains this?
**A:** Vladyslav Zaiets (CTO & Software Architect)

---

For more information, see the [full documentation](../README.md).
