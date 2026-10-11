# SqliteMultiTenant

A .NET 10 class library providing per-tenant SQLite database isolation, migrations, backups, connection pooling and a demo ASP.NET/console host.

## Getting Started

### 1. Registering the Library in Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Services;

// In your application startup (e.g., Program.cs)
var services = new ServiceCollection();

// Configure logging
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

// Configure SQLite Multi-Tenant services
string masterConnectionString = "Data Source=master.db;Version=3;";
services.AddSqliteMultiTenant(masterConnectionString, options =>
{
    options.MaxConnections = 20;
    options.ConnectionTimeoutSeconds = 30;
    options.BackupRetentionDays = 30;
    options.EnableEncryption = false;
    options.BackupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "backups");
    options.DatabaseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "databases");
    options.EnableLogging = true;
});

var serviceProvider = services.BuildServiceProvider();

// Get required services
var tenantService = serviceProvider.GetRequiredService<ITenantService>();
var migrationService = serviceProvider.GetRequiredService<IMigrationService>();
var backupService = serviceProvider.GetRequiredService<IBackupService>();
```

### 2. Creating a Tenant, Getting a Connection, and Running a Query

```csharp
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;
using System.Data.SQLite;
using System.Threading.Tasks;

// Assume 'tenantService' and 'connectionPool' are obtained from DI
var tenantService = serviceProvider.GetRequiredService<ITenantService>();
var connectionPool = serviceProvider.GetRequiredService<IConnectionPoolManager>();

// Create a new tenant
var tenant = await tenantService.CreateTenantAsync(
    name: "Acme Corporation",
    description: "Main tenant for demonstration",
    contactEmail: "admin@acme.example.com");

// Create a database entry for the tenant
var tenantDb = new TenantDatabase
{
    DatabaseId = Guid.NewGuid().ToString(),
    TenantId = tenant.TenantId,
    Name = "primary_db",
    FilePath = Path.Combine("databases", $"{tenant.TenantId}_primary.db"),
    SizeBytes = 0,
    SchemaVersion = 1,
    IsReadOnly = false
};

// Get a connection for the tenant and run a query
string connectionString = $"Data Source={tenantDb.FilePath};Version=3;";
using (var connection = await connectionPool.AcquireAsync(tenant.TenantId, connectionString))
{
    // Ensure the database file exists and create tables if needed
    if (!File.Exists(tenantDb.FilePath))
    {
        Directory.CreateDirectory(Path.GetDirectoryName(tenantDb.FilePath)!);
        
        // Create initial schema
        using (var initCmd = connection.CreateCommand())
        {
            initCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Tenants (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Email TEXT
                );
                
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY,
                    TenantId TEXT NOT NULL,
                    Username TEXT NOT NULL,
                    Email TEXT NOT NULL
                );";
            await initCmd.ExecuteNonQueryAsync();
        }
    }
    
    // Insert sample data
    using (var insertCmd = connection.CreateCommand())
    {
        insertCmd.CommandText = "INSERT INTO Users (TenantId, Username, Email) VALUES (@tid, @user, @email)";
        insertCmd.Parameters.AddWithValue("@tid", tenant.TenantId);
        insertCmd.Parameters.AddWithValue("@user", "john_doe");
        insertCmd.Parameters.AddWithValue("@email", "john@example.com");
        await insertCmd.ExecuteNonQueryAsync();
    }
    
    // Query the data
    using (var selectCmd = connection.CreateCommand())
    {
        selectCmd.CommandText = "SELECT Id, Username, Email FROM Users WHERE TenantId = @tid";
        selectCmd.Parameters.AddWithValue("@tid", tenant.TenantId);
        
        using (var reader = await selectCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                Console.WriteLine($"User: {reader.GetString(1)} ({reader.GetString(2)})");
            }
        }
    }
}

// Release the connection back to the pool
await connectionPool.ReleaseAsync(tenant.TenantId, connection);
```

### 3. Running Migrations for All Tenants at Startup

```csharp
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;
using System.Threading.Tasks;

// Assume 'migrationService' and 'tenantService' are obtained from DI
var migrationService = serviceProvider.GetRequiredService<IMigrationService>();
var tenantService = serviceProvider.GetRequiredService<ITenantService>();

// Get all tenants
var tenants = await tenantService.GetAllTenantsAsync();

// Apply migrations to each tenant's database
foreach (var tenant in tenants)
{
    // Get the tenant's primary database (in a real app, you might have multiple databases per tenant)
    var tenantDb = tenant.Databases.FirstOrDefault();
    if (tenantDb == null) continue;
    
    Console.WriteLine($"Applying migrations for tenant: {tenant.Name}");
    
    // Apply pending migrations with fault isolation
    var result = await migrationService.ApplyMigrationsWithFaultIsolationAsync(
        databaseId: tenantDb.DatabaseId,
        executedBy: "system-startup");
    
    if (result.IsSuccess)
    {
        Console.WriteLine($"✓ Successfully applied {result.SuccessfulMigrations}/{result.TotalMigrationsAttempted} migrations");
    }
    else
    {
        Console.WriteLine($"✗ Migration batch failed: {string.Join("; ", result.Failures.Select(f => f.ErrorMessage))}");
    }
}
```

### 4. Resolving the Current Tenant in an ASP.NET Core Request

```csharp
using Microsoft.AspNetCore.Http;
using SqliteMultiTenant.Utilities;
using System.Threading.Tasks;

// In your ASP.NET Core middleware or controller
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenantContextHelper _tenantContextHelper;
    
    public TenantResolutionMiddleware(RequestDelegate next, TenantContextHelper tenantContextHelper)
    {
        _next = next;
        _tenantContextHelper = tenantContextHelper;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get tenant ID from header first
        if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader))
        {
            var tenantId = tenantIdHeader.ToString();
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                using (_tenantContextHelper.CreateScope(tenantId))
                {
                    await _next(context);
                }
                return;
            }
        }
        
        // Try to get tenant ID from subdomain
        var host = context.Request.Host.Host;
        var subdomain = host.Split('.').FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(subdomain) && 
            subdomain != "www" && 
            !subdomain.Contains("localhost"))
        {
            // In a real app, you would look up the tenant ID from the subdomain
            // For this example, we'll use the subdomain as the tenant ID
            using (_tenantContextHelper.CreateScope(subdomain))
            {
                await _next(context);
            }
            return;
        }
        
        // No tenant context - proceed without tenant resolution
        await _next(context);
    }
}

// Extension method to add the middleware
public static class TenantResolutionMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantResolutionMiddleware>();
    }
}

// Usage in Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddLogging();
builder.Services.AddSqliteMultiTenant("Data Source=master.db;Version=3;");
builder.Services.AddSingleton<TenantContextHelper>();

var app = builder.Build();

// Add the tenant resolution middleware
app.UseTenantResolution();

// Your endpoints will now have access to the tenant context
app.MapGet("/users", (TenantContextHelper tenantContextHelper) => 
{
    var tenantId = tenantContextHelper.GetCurrentTenantId();
    if (tenantId == null)
    {
        return Results.BadRequest("No tenant context");
    }
    
    // In a real app, you would use the tenantId to query the correct database
    return Results.Ok(new { Message = $"Users for tenant: {tenantId}" });
});

app.Run();
```

## Build

Requires .NET SDK 10.0 (`global.json` pins 10.0.100, rollForward latestMinor).

```bash
dotnet restore
dotnet build SqliteMultiTenant.sln -c Release          # whole solution (lib + tests + benchmarks)
dotnet build src/SqliteMultiTenant.csproj -c Release   # library only (same as `make build`)
dotnet pack src/SqliteMultiTenant.csproj -c Release -o ./nupkg
```

Build currently produces ~1200 warnings and 0 errors; `TreatWarningsAsErrors=false` in `Directory.Build.props`. Do not introduce new errors; reducing warnings is welcome but not required.

## Test

```bash
dotnet test tests/sqlite-multi-tenant.Tests -c Release
dotnet test tests/sqlite-multi-tenant.Tests -c Release --filter "FullyQualifiedName~TenantServiceTests"
```

- Framework: xUnit 2.9, FluentAssertions 7, NSubstitute 5.3.
- Single test project: `tests/sqlite-multi-tenant.Tests/` (flat layout, plus `Operations/`, `Security/`, `Utilities/`, `Validation/` subfolders).
- File naming: `<ClassUnderTest>Tests.cs`; integration tests use `<Class>IntegrationTests.cs` and hit real temp SQLite files.
- Method naming: `Method_Scenario_ExpectedResult` (e.g. `GetTenantAsync_WithBlankId_ThrowsArgumentException`), Arrange/Act/Assert comments.
- Test classes are `public sealed`, dependencies mocked with `Substitute.For<T>()` in the constructor.
- Known state: not all tests pass (as of 2026-09: ~174 failing of ~1692). Run the relevant subset before and after a change and do not make the failing count grow. `make test` swallows failures with `|| true`; prefer plain `dotnet test`.
- CI (`.github/workflows/ci.yml`, `build.yml`): `dotnet restore && dotnet build -c Release && dotnet test -c Release`.

## Lint / Format

```bash
dotnet format src/SqliteMultiTenant.csproj                                   # apply .editorconfig
dotnet build src/SqliteMultiTenant.csproj -c Release /p:EnforceCodeStyleInBuild=true
```

`.editorconfig` is the source of truth: 4-space indent, UTF-8, final newline, trim trailing whitespace, Allman braces (`csharp_new_line_before_open_brace = all`), `var` preferred everywhere. Note that many existing files (especially tests) use K&R braces; follow the style of the file you are editing rather than reformatting whole files.

## Operations

### Scheduled Backup of All Tenants via BackgroundService

The library includes `BackupSchedulerService` for automated tenant backups. Register it in your ASP.NET Core application:

```csharp
using Microsoft.Extensions.Hosting;
using SqliteMultiTenant.BackgroundWorkers;

// In Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add SQLite Multi-Tenant services
builder.Services.AddSqliteMultiTenant("Data Source=master.db;Version=3;");

// Add background backup service (runs daily at 2 AM UTC by default)
builder.Services.AddHostedService<BackupSchedulerService>();

var app = builder.Build();
app.Run();
```

To customize the backup interval (e.g., every 6 hours):

```csharp
builder.Services.AddHostedService(sp => 
    new BackupSchedulerService(
        sp.GetRequiredService<IBackupService>(),
        sp.GetRequiredService<ITenantService>(),
        sp.GetRequiredService<ILogger<BackupSchedulerService>>(),
        TimeSpan.FromHours(6) // Custom interval
    ));
```

### Restoring One Tenant from a Backup File

To restore a tenant database from a backup file:

```csharp
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;
using System.Data.SQLite;
using System.IO;

// Assume 'backupService' and 'tenantService' are obtained from DI
var backupService = serviceProvider.GetRequiredService<IBackupService>();
var tenantService = serviceProvider.GetRequiredService<ITenantService>();

// Get the backup to restore from
var backup = await backupService.GetBackupAsync("backup-id-here");
if (backup == null)
{
    throw new Exception("Backup not found");
}

// Verify the backup file exists
if (!File.Exists(backup.BackupPath))
{
    throw new Exception($"Backup file not found: {backup.BackupPath}");
}

// Get tenant information (optional, for validation)
var tenant = await tenantService.GetTenantAsync(backup.TenantId);
if (tenant == null)
{
    throw new Exception("Tenant not found for backup");
}

// Define restore path (could be original location or new location)
string restorePath = Path.Combine("databases", $"{tenant.TenantId}_restored.db");

// Ensure directory exists
Directory.CreateDirectory(Path.GetDirectoryName(restorePath)!);

// Copy backup file to restore location
File.Copy(backup.BackupPath, restorePath, overwrite: true);

// Optionally, verify the restored database
using var connection = new SQLiteConnection($"Data Source={restorePath};");
await connection.OpenAsync();

using var command = connection.CreateCommand();
command.CommandText = "PRAGMA integrity_check;";
var result = await command.ExecuteScalarAsync();
bool isValid = string.Equals(result?.ToString(), "ok", StringComparison.OrdinalIgnoreCase);

if (!isValid)
{
    throw new Exception("Restored database failed integrity check");
}

// The restored database is now ready for use
// You can update the tenant's database record to point to the restored file
```

### Wiring HealthChecker into ASP.NET Core Health Checks

Integrate the `HealthChecker` service with ASP.NET Core's health check system:

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SqliteMultiTenant.Services;

// In Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add SQLite Multi-Tenant services
builder.Services.AddSqliteMultiTenant("Data Source=master.db;Version=3;");

// Add HealthChecker as a scoped service
builder.Services.AddScoped<HealthChecker>();

// Add custom health check for tenant databases
builder.Services.AddHealthChecks()
    .AddCheck("tenant-databases", async sp =>
    {
        var healthChecker = sp.GetRequiredService<HealthChecker>();
        var tenantService = sp.GetRequiredService<ITenantService>();
        var connectionPool = sp.GetRequiredService<IConnectionPoolManager>();
        
        try
        {
            // Get all tenants
            var tenants = await tenantService.GetAllTenantsAsync();
            
            // Check each tenant's health
            var unhealthyTenants = new List<string>();
            foreach (var tenant in tenants)
            {
                // Get tenant's database connection string (simplified example)
                var tenantDb = tenant.Databases.FirstOrDefault();
                if (tenantDb == null) continue;
                
                string connectionString = $"Data Source={tenantDb.FilePath};Version=3;";
                
                // Check tenant health
                var result = await healthChecker.CheckTenantAsync(
                    tenant.TenantId, 
                    connectionString);
                
                if (!result.IsHealthy)
                {
                    unhealthyTenants.Add($"{tenant.TenantId}: {result.ErrorMessage}");
                }
            }
            
            return unhealthyTenants.Any()
                ? HealthCheckResult.Unhealthy($"Unhealthy tenants: {string.Join(", ", unhealthyTenants)}")
                : HealthCheckResult.Healthy("All tenants are healthy");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                $"Health check failed: {ex.Message}");
        }
    });

var app = builder.Build();

// Map health check endpoint
app.MapHealthChecks("/health");

app.Run();
```

Sample `/health` JSON output when all tenants are healthy:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "tenant-databases": {
      "status": "Healthy",
      "description": "All tenants are healthy",
      "data": {}
    }
  }
}
```

Sample `/health` JSON output when some tenants are unhealthy:
```json
{
  "status": "Unhealthy",
  "totalDuration": "00:00:00.2345678",
  "entries": {
    "tenant-databases": {
      "status": "Unhealthy",
      "description": "Unhealthy tenants: tenant123: Database file not found, tenant456: Connection timeout",
      "data": {}
    }
  }
}
```

## Configuration

The following configuration options are available in `MultiTenantOptions` and related classes:

| Option | Type | Default | Meaning |
|--------|------|---------|---------|
| **MultiTenantOptions** |
| BasePath | string | `"./databases"` | The base path where tenant databases are stored |
| MaxConnectionsPerTenant | int | `10` | Maximum number of connections allowed per tenant |
| DefaultMaxConnections | int | `10` | Default maximum number of connections for a tenant if not overridden |
| MaxBackupCount | int | `20` | Maximum number of backup files to retain per tenant |
| BackupRetention | TimeSpan | `30 days` | Time span after which backups are deleted |
| EnableBackupScheduling | bool | `true` | Indicates whether backup scheduling is enabled |
| BackupInterval | TimeSpan | `1 hour` | Interval between automatic backups |
| EnableAuditLogging | bool | `true` | Indicates whether audit logging is enabled |
| EnablePerformanceMonitoring | bool | `true` | Indicates whether performance monitoring is enabled |
| EnableDataEncryption | bool | `false` | Indicates whether data encryption is enabled |
| MaxCacheSize | int | `1000` | Maximum number of cache entries |
| DefaultCacheTTL | TimeSpan | `1 hour` | Default time-to-live for cache entries |
| RateLimitRequestsPerMinute | int | `1000` | Number of rate limit requests allowed per minute |
| EncryptionKeyPath | string | `"./keys"` | Path to the encryption key file |
| VerboseLogging | bool | `false` | Indicates whether verbose logging is enabled |
| **BackupOptions** |
| BackupPath | string | `"./backups"` | The path where backups are stored |
| MaxConcurrentBackups | int | `3` | Maximum number of concurrent backup operations |
| BackupTimeoutSeconds | int | `300` | Timeout in seconds for backup operations |
| CompressBackups | bool | `true` | Indicates whether backups are compressed |
| VerifyBackupIntegrity | bool | `true` | Indicates whether backup integrity is verified after creation |
| RetainDifferentialBackups | bool | `true` | Indicates whether differential backups are retained |
| **MonitoringOptions** |
| Enabled | bool | `true` | Indicates whether monitoring is enabled |
| MetricsCollectionInterval | TimeSpan | `60 seconds` | Interval at which metrics are collected |
| MaxMetricsHistory | int | `1000` | Maximum number of metric history entries to retain |
| SlowQueryThresholdMs | long | `1000` | Threshold in milliseconds for a query to be considered slow |
| **AlertThreshold** (per metric) |
| MetricName | string | (required) | The name of the metric to monitor |
| WarningLevel | double | (required) | The value at which a warning alert is triggered |
| CriticalLevel | double | (required) | The value at which a critical alert is triggered |
| EnableEmail | bool | (required) | Indicates whether email alerts are enabled for this threshold |
| EnableLog | bool | (required) | Indicates whether log alerts are enabled for this threshold |
| **SecurityOptions** |
| RequireTenantIdValidation | bool | `true` | Indicates whether tenant ID validation is required |
| EnableQueryLogging | bool | `false` | Indicates whether query logging is enabled |
| EnableConnectionEncryption | bool | `false` | Indicates whether connection encryption is enabled |
| SessionTimeout | TimeSpan | `1 hour` | Timeout for user sessions |
| MaxFailedLoginAttempts | int | `5` | Maximum number of failed login attempts before lockout |
| LockoutDuration | TimeSpan | `15 minutes` | Duration of account lockout after too many failed attempts |
| RequireStrongPasswords | bool | `false` | Indicates whether strong passwords are required |
| **MaintenanceOptions** |
| EnableAutomaticVacuum | bool | `true` | Indicates whether automatic vacuum is enabled |
| VacuumInterval | TimeSpan | `1 day` | Interval at which automatic vacuum runs |
| EnableIndexRebuild | bool | `true` | Indicates whether index rebuild is enabled |
| IndexRebuildInterval | TimeSpan | `7 days` | Interval at which index rebuild runs |
| EnableStatisticsUpdate | bool | `true` | Indicates whether statistics update is enabled |
| StatisticsUpdateInterval | TimeSpan | `1 day` | Interval at which statistics update runs |
| **FeatureFlags** |
| EnableCaching | bool | `true` | Indicates whether caching is enabled |
| EnableBatching | bool | `true` | Indicates whether batching is enabled |
| EnableAsyncOperations | bool | `true` | Indicates whether asynchronous operations are enabled |
| EnableWebhooks | bool | `false` | Indicates whether webhooks are enabled |
| EnableExternalIntegrations | bool | `false` | Indicates whether external integrations are enabled |
| EnableAdvancedAnalytics | bool | `false` | Indicates whether advanced analytics are enabled |
