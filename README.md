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

## HealthCheckService

`src/Health/HealthCheckService.cs` provides system and component health monitoring through the `IHealthCheckService` interface. It checks database connectivity, disk space and memory usage, returning structured results for load balancer heartbeats and alerting systems.

**Key members:**

| Method | Returns | Description |
|--------|---------|-------------|
| `GetHealthStatusAsync()` | `HealthCheckResponse` | Aggregated status with per-component breakdown (database, disk, memory) and response times |
| `IsDatabaseHealthyAsync()` | `bool` | Tests master database accessibility |
| `IsDiskSpaceHealthyAsync(long)` | `bool` | Checks free disk space against a threshold (default 1 GB) |
| `IsSystemHealthyAsync()` | `bool` | Single boolean combining all component checks |
| `GetDetailedStatusAsync()` | `string` | Returns `"healthy"` or `"unhealthy"` |
| `CheckLivenessAsync()` | `HealthStatusResult` | Cheap process-level check (no external deps) for orchestrators |
| `CheckReadinessAsync()` | `HealthStatusResult` | Database + disk check confirming the service can accept traffic |

**`HealthStatusResult`** - structured result returned by liveness/readiness checks:

| Property | Type | Description |
|----------|------|-------------|
| `Status` | `string` | `"healthy"` or `"unhealthy"` |
| `IsHealthy` | `bool` | Boolean health flag |
| `CheckedAt` | `DateTime` | UTC timestamp of the check |
| `DurationMs` | `long` | Total check duration in milliseconds |
| `Checks` | `Dictionary<string, ComponentHealth>` | Per-component results |

`DetailedHealthCheckService` extends `HealthCheckService` with a `GetDetailedDiagnostics()` method returning OS version, processor count, .NET version, memory working set, CPU time and thread count.

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Health;

// Create the service (typically resolved from DI)
var logger = loggerFactory.CreateLogger<HealthCheckService>();
var healthService = new HealthCheckService(logger, databasePath: "./databases");

// Full component breakdown
var response = await healthService.GetHealthStatusAsync();
Console.WriteLine($"Overall: {response.Status}");
foreach (var (name, component) in response.Components)
{
    Console.WriteLine($"  {name}: {component.Status} ({component.ResponseTimeMs} ms)");
}

// Kubernetes-style probes
var liveness = await healthService.CheckLivenessAsync();
Console.WriteLine($"Liveness: {liveness.Status} (took {liveness.DurationMs} ms)");

var readiness = await healthService.CheckReadinessAsync();
Console.WriteLine($"Readiness: {readiness.Status}");

// Quick boolean for conditional logic
if (!await healthService.IsSystemHealthyAsync())
{
    Console.WriteLine("System is unhealthy - check components for details");
}
```

## TenantService

`src/Services/TenantService.cs` implements `ITenantService` and is the main entry point for tenant lifecycle management. It creates, reads, updates and deletes tenants, controls their status (active, inactive, suspended), manages per-tenant metadata and reports tenant database storage usage. It is registered as a scoped service by `AddSqliteMultiTenant`.

**Key members:**

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateTenantAsync(name, description?, contactEmail?)` | `Tenant` | Creates an active tenant with a generated GUID identifier. Throws `InvalidOperationException` if the name is already in use |
| `GetTenantAsync(tenantId)` | `Tenant?` | Returns the tenant or `null`. Records the access time on the tenant as a side effect |
| `UpdateTenantAsync(tenant)` | `Task` | Validates and saves changes. Throws `TenantNotFoundException` if the tenant does not exist |
| `DeleteTenantAsync(tenantId)` | `Task` | Deletes a tenant. Throws `TenantNotFoundException` if the tenant does not exist |
| `GetAllTenantsAsync()` / `GetActiveTenantsAsync()` | `List<Tenant>` | Lists all tenants, or only those currently active |
| `ActivateTenantAsync(tenantId)` / `DeactivateTenantAsync(tenantId)` | `Task` | Changes the tenant's active state |
| `SuspendTenantAsync(tenantId)` | `Task` | Sets the status to `TenantStatus.Suspended` |
| `TenantExistsAsync(tenantId)` | `bool` | Returns `false` for blank identifiers instead of throwing |
| `GetTenantCountAsync()` | `int` | Total number of tenants |
| `SearchTenantsAsync(searchTerm)` | `List<Tenant>` | Searches tenants by a term; the term must not be blank |
| `SetTenantMetadataAsync(tenantId, key, value)` | `Task` | Stores a metadata key/value pair on the tenant |
| `GetTenantDatabaseSizeAsync(tenantId)` | `TenantStorageInfo` | Reads `PRAGMA page_count` and `page_size` from the tenant database and the size of its `-wal` file. Requires `DatabasePath` to be set on the tenant |

Argument validation failures throw `ArgumentException`. Every method logs failures with `ILogger<TenantService>` and rethrows the original exception.

### Usage Example

```csharp
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Services;

// Assume 'serviceProvider' is the application's IServiceProvider
var tenantService = serviceProvider.GetRequiredService<ITenantService>();

// Create a tenant
var tenant = await tenantService.CreateTenantAsync(
    name: "Globex Industries",
    description: "Enterprise customer",
    contactEmail: "ops@globex.example.com");

// Store a metadata value and read the tenant back
await tenantService.SetTenantMetadataAsync(tenant.TenantId, "plan", "enterprise");
var loaded = await tenantService.GetTenantAsync(tenant.TenantId);
Console.WriteLine($"{loaded?.Name} is active: {loaded?.Status == TenantStatus.Active}");

// Suspend and later reactivate the tenant
await tenantService.SuspendTenantAsync(tenant.TenantId);
await tenantService.ActivateTenantAsync(tenant.TenantId);

// Inspect storage usage (requires DatabasePath to be set on the tenant)
var storage = await tenantService.GetTenantDatabaseSizeAsync(tenant.TenantId);
Console.WriteLine($"Size: {storage.SizeBytes} bytes, WAL: {storage.WalSizeBytes} bytes");

// Handle missing tenants explicitly
try
{
    await tenantService.DeleteTenantAsync("does-not-exist");
}
catch (TenantNotFoundException ex)
{
    Console.WriteLine($"Tenant not found: {ex.TenantId}");
}
```

## BackupService

`src/Services/BackupService.cs` implements `IBackupService` and manages backup records and backup files. It creates backup records in the `Pending` state, marks them as completed or failed, verifies backup files with SQLite's `PRAGMA integrity_check`, sets retention expiration dates, tags backups and deletes them. `BackupWithProgressAsync` copies a SQLite database file with the SQLite online backup API and reports progress. It is registered as a scoped service by `AddSqliteMultiTenant`.

**Key members:**

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateBackupAsync(databaseId, backupType, createdBy, backupPath?)` | `Backup` | Inserts a `Pending` backup record with a new GUID identifier. If `backupPath` is omitted, a path is generated under the default backup directory. The record expires after `TenantConstants.BackupRetentionDays`. Does not write a backup file |
| `GetBackupAsync(backupId)` | `Backup?` | Returns the backup or `null` |
| `GetDatabaseBackupsAsync(databaseId)` | `List<Backup>` | Lists all backups of a database |
| `GetCompletedBackupsAsync(databaseId)` | `List<Backup>` | Lists the completed backups of a database |
| `GetLatestBackupAsync(databaseId)` | `Backup?` | Returns the most recent backup of a database, or `null` |
| `GetBackupCountAsync(databaseId)` | `int` | Number of backups recorded for a database |
| `GetExpiredBackupsAsync()` | `List<Backup>` | Lists backups whose expiration date has passed |
| `MarkBackupAsCompletedAsync(backupId, sizeBytes, durationMs)` | `Task` | Stores the size and duration and sets the status to completed. Throws `BackupException` if the backup does not exist |
| `MarkBackupAsFailedAsync(backupId, errorMessage)` | `Task` | Stores the error message and sets the status to failed. Throws `BackupException` if the backup does not exist |
| `VerifyBackupAsync(backupId, verifiedBy)` | `BackupVerificationResult` | Opens the backup file read-only, runs `PRAGMA integrity_check` and, if the check passes, marks the backup as verified. Missing files, SQLite errors and a missing backup record are returned as a failed result (`IsValid == false`) rather than thrown |
| `SetBackupExpirationAsync(backupId, expirationDate)` | `Task` | Changes the expiration date. Throws `BackupException` if the backup does not exist |
| `AddBackupTagAsync(backupId, tag)` | `Task` | Adds a tag to the backup. Throws `BackupException` if the backup does not exist |
| `DeleteBackupAsync(backupId)` | `Task` | Deletes the backup record. Throws `BackupException` if the backup does not exist. The backup file on disk is not removed by this method |
| `BackupWithProgressAsync(sourceDatabasePath, destinationPath, progress?, pagesPerStep?)` | `Task` | Copies a database file with `SQLiteConnection.BackupDatabase`. `IProgress<BackupProgress>` receives the pages copied and remaining. `pagesPerStep` defaults to `-1` (copy all pages in one step). Cancellation is honored between steps |

Argument validation failures throw `ArgumentException` or `ArgumentNullException`. Methods that touch the repository log failures with `ILogger<BackupService>` and rethrow the original exception, except `VerifyBackupAsync`, which returns a failed result.

### Usage Example

```csharp
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Services;

// Assume 'serviceProvider' is the application's IServiceProvider
var backupService = serviceProvider.GetRequiredService<IBackupService>();

// Create a backup record for a tenant database and copy the file
var backup = await backupService.CreateBackupAsync(
    databaseId: "tenant-database-id",
    backupType: BackupType.Full,
    createdBy: "nightly-job",
    backupPath: "/var/backups/tenant-database-id.db");

var progress = new Progress<BackupProgress>(p =>
    Console.WriteLine($"{p.PagesCopied}/{p.TotalPages} pages copied"));

await backupService.BackupWithProgressAsync(
    sourceDatabasePath: "/var/data/tenant-database-id.db",
    destinationPath: backup.BackupPath,
    progress: progress);

// Record the outcome of the copy
await backupService.MarkBackupAsCompletedAsync(backup.BackupId, sizeBytes: 1048576, durationMs: 250);

// Verify the file and check the result
var verification = await backupService.VerifyBackupAsync(backup.BackupId, verifiedBy: "nightly-job");
if (!verification.IsValid)
{
    Console.WriteLine($"Backup is not usable: {verification.ErrorMessage}");
}

// Keep the backup for longer, tag it and look up the latest one
await backupService.SetBackupExpirationAsync(backup.BackupId, DateTime.UtcNow.AddYears(1));
await backupService.AddBackupTagAsync(backup.BackupId, "monthly");
var latest = await backupService.GetLatestBackupAsync("tenant-database-id");
Console.WriteLine($"Latest backup: {latest?.BackupId}");

// Handle missing backup records explicitly
try
{
    await backupService.MarkBackupAsFailedAsync("does-not-exist", "disk full");
}
catch (BackupException ex)
{
    Console.WriteLine($"Backup not found: {ex.Message}");
}
```

## MigrationService

`src/Services/MigrationService.cs` implements `IMigrationService` and manages migration definitions and their state for each database. It creates migrations in the `Pending` state, tracks them through `Running`, `Completed`, `Failed` and `RolledBack`, and applies pending migrations in `ExecutionOrder`. The batch methods isolate failures: a failing migration is recorded and the batch moves on to the next one. It is registered as a scoped service by `AddSqliteMultiTenant`.

**Status lifecycle:** `Pending` → `Running` (set by `ExecuteMigrationAsync` or by the apply flow) → `Completed` or `Failed`. A `Completed` migration that has a down script can move to `RolledBack`.

**Apply flow (`ApplyMigrationsWithFaultIsolationAsync`):**

1. Loads the pending migrations of the database. If there are none, it returns a successful result with zero migrations attempted.
2. For each migration in `ExecutionOrder`, marks it `Running`, saves it, runs the migration script, then marks it `Completed` with the execution time and saves it.
3. If a migration throws, the error is stored in `ErrorMessage`, the migration is set to `Failed` and saved, and the next migration runs. Migrations that already completed are not rolled back.
4. Returns a `MigrationBatchResult`. `IsSuccess` is `false` when any migration failed. Exceptions raised outside the per-migration loop are returned as a failure result instead of being thrown.

**Multi-database flow (`ApplyMigrationsToMultipleDatabasesAsync`):** runs the apply flow for each database ID in the list, in order. An exception for one database is recorded as a `MigrationFailure` (migration ID and version `system`) and the loop continues with the next database. Totals are summed across all databases.

**Rollback flow (`RollbackMigrationAsync`):** the migration is rolled back only when `Migration.CanRollback()` returns `true`: it is rollbackable, has a non-empty `DownScript`, is `Completed`, and has not been rolled back yet. Otherwise a failure result is returned. On success the status is set to `RolledBack` and saved. The method does not execute the `DownScript`; it updates the migration record only.

> **Note:** the script execution step is currently a simulation (`ExecuteMigrationScriptAsync` in the source). It waits 50 ms, fails for any version that contains `fail` or `error` (case-insensitive), fails randomly about 5% of the time, and otherwise reports a random execution time between 10 and 200 ms. The `UpScript` is not run against the tenant database by this service yet.

**Key members:**

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateMigrationAsync(databaseId, version, name, upScript, downScript?)` | `Migration` | Creates a `Pending` migration. `ExecutionOrder` is the existing migration count plus one, and `IsRollbackable` is `true` when a down script is supplied. Throws `MigrationException` when the version already exists for the database |
| `GetMigrationAsync(migrationId)` | `Migration?` | Returns the migration or `null` |
| `GetDatabaseMigrationsAsync(databaseId)` | `List<Migration>` | Lists all migrations of a database in execution order |
| `GetPendingMigrationsAsync(databaseId)` | `List<Migration>` | Lists the pending migrations |
| `GetAppliedMigrationsAsync(databaseId)` | `List<Migration>` | Lists the applied migrations |
| `GetFailedMigrationsAsync(databaseId)` | `List<Migration>` | Lists the failed migrations |
| `GetMigrationCountAsync(databaseId)` | `int` | Number of migrations recorded for a database |
| `IsMigrationAppliedAsync(databaseId, version)` | `bool` | `true` only when the migration with that version is `Completed` |
| `ExecuteMigrationAsync(migrationId, executedBy)` | `MigrationResult` | Sets the migration to `Running` and records the executor. Does not run the script. Returns a failure result when the migration is not found |
| `MarkMigrationAsCompletedAsync(migrationId, executionTimeMs)` | `MigrationResult` | Sets the migration to `Completed` and stores the execution time. Throws `MigrationException` when the migration is not found |
| `MarkMigrationAsFailedAsync(migrationId, errorMessage)` | `MigrationResult` | Sets the migration to `Failed`, stores the message and returns a failure result with it. Returns a failure result when the migration is not found |
| `RollbackMigrationAsync(migrationId, executedBy)` | `MigrationResult` | Sets a rollbackable migration to `RolledBack`. See the rollback flow above. `executedBy` is validated but not stored |
| `ApplyMigrationsWithFaultIsolationAsync(databaseId, executedBy)` | `MigrationBatchResult` | Applies the pending migrations of one database (apply flow above) |
| `ApplyMigrationsToMultipleDatabasesAsync(databaseIds, executedBy)` | `MigrationBatchResult` | Applies the pending migrations of several databases (multi-database flow above). Throws `ArgumentException` when the list is `null` or empty |

Empty or white-space arguments throw `ArgumentException`. Repository failures are logged with `ILogger<MigrationService>` and rethrown, except in the two batch methods, which return a failure result.

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Services;

// Assume 'serviceProvider' is the application's IServiceProvider
var migrationService = serviceProvider.GetRequiredService<IMigrationService>();

// Define a migration. The version must be unique within the database
var migration = await migrationService.CreateMigrationAsync(
    databaseId: "tenant-database-id",
    version: "2026.10.25.1",
    name: "add-invoices-table",
    upScript: "CREATE TABLE Invoices (Id INTEGER PRIMARY KEY, Total REAL NOT NULL);",
    downScript: "DROP TABLE Invoices;");

// Apply all pending migrations. A failing migration does not stop the batch
var batch = await migrationService.ApplyMigrationsWithFaultIsolationAsync(
    databaseId: "tenant-database-id",
    executedBy: "startup-job");

if (!batch.IsSuccess)
{
    foreach (var tenant in batch.FailedTenantResults)
    {
        foreach (var failure in tenant.Failures)
        {
            Console.WriteLine($"{failure.Version} ({failure.Name}) failed: {failure.ErrorMessage}");
        }
    }
}

// Apply pending migrations to several databases. Each database is isolated
var multi = await migrationService.ApplyMigrationsToMultipleDatabasesAsync(
    databaseIds: new List<string> { "tenant-a-db", "tenant-b-db" },
    executedBy: "startup-job");
Console.WriteLine(multi.ResultSummary);

// Check the state of a version
var applied = await migrationService.IsMigrationAppliedAsync("tenant-database-id", "2026.10.25.1");
var pending = await migrationService.GetPendingMigrationsAsync("tenant-database-id");
Console.WriteLine($"Applied: {applied}, pending: {pending.Count}");

// Rollback is only allowed for a completed migration that has a down script.
// The migration created above is still pending, so this call returns a failure result
var rollback = await migrationService.RollbackMigrationAsync(migration.MigrationId, executedBy: "ops-engineer");
if (!rollback.IsSuccess)
{
    Console.WriteLine($"Rollback refused: {rollback.Error}");
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

## Caching

The library ships two caching layers and two pluggable cache strategies.

### CacheService

`src/Caching/CacheService.cs` implements `ICacheService` and wraps `IMemoryCache` for thread-safe in-process caching. It tracks keys internally so it can remove entries by prefix pattern or clear the entire cache.

| Method | Description |
|--------|-------------|
| `Get<T>(string key)` | Returns the cached value or `default(T)` on a miss. Null/whitespace keys return `default`. |
| `Set<T>(string key, T value, TimeSpan?)` | Stores a value with an optional sliding expiration (default 1 hour). Null values are silently ignored. |
| `Remove(string key)` | Removes a single key from the cache. Safe to call if the key does not exist. |
| `RemoveByPattern(string pattern)` | Removes all keys whose prefix matches the pattern (e.g. `"tenant:*"`). |
| `Clear()` | Removes every tracked entry without disposing the underlying `IMemoryCache`. |

Consistent cache keys are generated via the static `CacheKeys` helper, which prefixes every key with `sqlmt:` and offers methods such as `TenantKey(id)`, `AllTenantsKey()`, `BackupKey(id)`, `PendingMigrationsKey(dbId)` and `HealthCheckKey()`.

`CacheInvalidationService` provides event-driven invalidation - call `InvalidateTenant(id)`, `InvalidateBackups(dbId)`, `InvalidateMigrations(dbId)` or `InvalidateHealthCheck()` after mutations.

### DistributedCacheService

`src/Caching/DistributedCacheService.cs` implements `IDistributedCache` with a fully async API, LRU eviction and TTL support. Unlike `CacheService`, it does not depend on `IMemoryCache` and manages its own dictionary protected by a `SemaphoreSlim`.

| Method | Description |
|--------|-------------|
| `GetAsync<T>(string key)` | Returns the cached value or `null`. Expired entries are evicted on read. |
| `SetAsync<T>(string key, T value, TimeSpan?)` | Stores a value. When the cache is full the least-recently-used entry is evicted. Default TTL is 1 hour. |
| `RemoveAsync(string key)` | Removes a single entry. |
| `RemoveByPatternAsync(string pattern)` | Removes entries whose key starts with the pattern (case-insensitive). |
| `ClearAsync()` | Removes all entries and resets hit/miss counters. |
| `GetStatisticsAsync()` | Returns `DistributedCacheStatistics` with item count, total size, hits, misses and hit rate. |
| `CleanupExpiredAsync()` | Explicitly purges all expired entries. |

### Cache Strategies

`src/Caching/CacheStrategy.cs` defines the `ICacheStrategy` interface with `GetAsync`, `SetAsync`, `RemoveAsync` and `ClearAsync`. Two implementations are provided:

- **LruCacheStrategy** - evicts the least-recently-used entry when the cache reaches `maxSize` (default 1000). Entries can optionally have a TTL; expired entries are removed on read. Exposes `GetStatistics()` for per-key access counts and timestamps.
- **TimeBasedCacheStrategy** - pure time-based expiration with a configurable default TTL (default 1 hour). Expired entries are removed on read.

### Usage Example

```csharp
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Caching;

// In-memory cache via CacheService
var memoryCache = new MemoryCache(new MemoryCacheOptions());
var logger = loggerFactory.CreateLogger<CacheService>();
var cache = new CacheService(memoryCache, logger);

cache.Set("tenant:t1", tenantObject, TimeSpan.FromMinutes(30));
var tenant = cache.Get<Tenant>("tenant:t1");

// Pattern-based invalidation
cache.RemoveByPattern(CacheKeys.TenantPattern()); // removes all "sqlmt:tenant:*" keys

// Distributed cache with LRU eviction
var distLogger = loggerFactory.CreateLogger<DistributedCacheService>();
var distCache = new DistributedCacheService(distLogger, maxItems: 500);

await distCache.SetAsync("session:abc", sessionData, TimeSpan.FromMinutes(15));
var session = await distCache.GetAsync<SessionData>("session:abc");
var stats = await distCache.GetStatisticsAsync();
Console.WriteLine($"Hit rate: {stats.HitRate:P1}");

// Pluggable strategy (LRU)
var lruLogger = loggerFactory.CreateLogger<LruCacheStrategy>();
ICacheStrategy strategy = new LruCacheStrategy(lruLogger, maxSize: 2000);

await strategy.SetAsync("key", value, TimeSpan.FromMinutes(10));
var cached = await strategy.GetAsync<MyType>("key");
```

## Validation

The library provides validators for tenant requests, migration scripts, connection strings, backups, and arbitrary data, plus a fluent rule builder for reusable validation logic.

### TenantValidator

`src/Validation/TenantValidator.cs` validates tenant create and update requests at the API boundary. Each method returns a `Dictionary<string, string>` of field-name-to-error-message pairs (empty when valid).

| Method | Description |
|--------|-------------|
| `ValidateCreateRequest(CreateTenantRequest)` | Checks that `Name` is present and passes `IsValidTenantName()`, and that `ContactEmail` is present and passes `IsValidEmail()`. |
| `ValidateUpdateRequest(UpdateTenantRequest)` | Validates only the fields that are provided (all fields are optional on update). |
| `ValidateNameUniqueness(string, string?)` | Placeholder for database-backed uniqueness check. |

The file also contains `MigrationValidator` (version format, script content, dangerous-pattern detection), `ConnectionStringValidator` (SQLite connection string format and path safety), and `BackupValidator` (tag format, retention days).

### DataValidator

`src/Validation/DataValidator.cs` is an imperative, fluent validator. Chain validation calls and collect results:

| Method | Description |
|--------|-------------|
| `RequireString(value, fieldName, maxLength?)` | Non-null/non-whitespace check with optional max length. |
| `RequireRange(value, min, max, fieldName)` | Integer range validation. |
| `RequireValidEmail(email, fieldName)` | Email format check via `System.Net.Mail.MailAddress`. |
| `RequireValidUrl(url, fieldName)` | Absolute HTTP(S) URL check. |
| `RequireValidGuid(guid, fieldName)` | GUID format check. |
| `RequirePattern(value, pattern, fieldName, message)` | Regex pattern match. |
| `Require<T>(value, predicate, fieldName, message)` | Custom predicate. |
| `RequireNotEmpty<T>(collection, fieldName)` | Collection must have at least one item. |
| `RequireEqual<T>` / `RequireNotEqual<T>` | Equality / inequality checks. |
| `GetResult()` | Returns a `ValidationResult` with `IsValid` and a list of `ValidationError` objects. |

### ValidationRuleBuilder

`src/Validation/ValidationRuleBuilder.cs` provides a generic fluent API for building reusable, composable validation rule sets.

| Method | Description |
|--------|-------------|
| `Required(fieldName, message?)` | Field must have a non-null, non-whitespace value. |
| `StringLength(fieldName, minLength?, maxLength?, message?)` | String length constraints. |
| `Email(fieldName, message?)` | Email format via regex. |
| `Range(fieldName, minValue?, maxValue?, message?)` | Numeric range (converts via `decimal`). |
| `Pattern(fieldName, pattern, message?)` | Regex pattern match. |
| `Custom(fieldName, predicate, message?)` | Arbitrary predicate on the object. |
| `MustMatch(field1, field2, message?)` | Cross-field equality. |
| `Validate(T obj)` | Runs all rules and returns `RuleValidationResult` with `IsValid` and `Errors`. |

### Usage Example

```csharp
using SqliteMultiTenant.Validation;

// Validate a tenant create request
var validator = new TenantValidator();
var errors = validator.ValidateCreateRequest(new CreateTenantRequest
{
    Name = "Acme Corp",
    ContactEmail = "admin@acme.example.com"
});

if (errors.Count > 0)
{
    foreach (var (field, message) in errors)
        Console.WriteLine($"{field}: {message}");
}

// Fluent data validation
var dataValidator = new DataValidator(loggerFactory.CreateLogger<DataValidator>());
var result = dataValidator
    .RequireString(name, "Name", maxLength: 255)
    .RequireValidEmail(email, "Email")
    .RequireRange(age, 1, 150, "Age")
    .GetResult();

if (!result.IsValid)
    Console.WriteLine(result.GetErrorMessage());

// Reusable rule builder
var rules = new ValidationRuleBuilder<CreateTenantRequest>()
    .Required("Name")
    .StringLength("Name", minLength: 3, maxLength: 255)
    .Email("ContactEmail");

var ruleResult = rules.Validate(request);
if (!ruleResult.IsValid)
{
    foreach (var error in ruleResult.Errors)
        Console.WriteLine($"{error.FieldName}: {error.Message}");
}
```

## EncryptionService

`src/Security/EncryptionService.cs` implements `IEncryptionService`. It encrypts and decrypts sensitive strings and byte arrays with AES-256-CBC, and hashes passwords with PBKDF2-SHA256. The AES key is derived from a single secret in configuration (`Encryption:Key`). `src/Security/EncryptionOptions.cs` holds the tunable parameters. `src/Security/EncryptionKeyManager.cs` is a separate component: it generates, stores, rotates and deletes a random AES-256 key per tenant.

`EncryptionService` is not registered by `AddSqliteMultiTenant`; create it yourself or register it in your host.

**Configuration**

The constructor reads `Encryption:Key` and throws `InvalidOperationException` if the value is missing or shorter than 32 characters. Supply it from a secret store or environment variable, never from source control:

```json
{
  "Encryption": {
    "Key": "<at least 32 characters, from a secret store>"
  }
}
```

**`EncryptionOptions`**

| Property | Default | Description |
|----------|---------|-------------|
| `KeySize` | `256` | AES key size in bits |
| `IvSize` | `128` | IV length in bits. A fresh random IV is prepended to every ciphertext |
| `SaltSize` | `128` | Salt length in bits for password hashes |
| `Iterations` | `10000` | PBKDF2 iteration count |
| `DerivationSalt` | `"SqliteMultiTenant"` | Salt used to derive the AES key from `Encryption:Key` |

Changing `KeySize`, `Iterations`, `DerivationSalt` or the configured key makes existing ciphertext undecryptable. Changing `SaltSize` makes existing password hashes fail verification. Set these once, before you store data.

**`EncryptionService` methods**

| Method | Returns | Description |
|--------|---------|-------------|
| `Encrypt(string)` | `string` | AES-256-CBC encryption. Output is Base64 of `IV \|\| ciphertext`. Null or empty input returns an empty string |
| `Decrypt(string)` | `string` | Reverses `Encrypt`. Throws on malformed or tampered input |
| `EncryptBytes(byte[])` | `byte[]` | Raw-byte variant with the same `IV \|\| ciphertext` layout |
| `DecryptBytes(byte[])` | `byte[]` | Reverses `EncryptBytes`. Throws on malformed input |
| `HashPassword(string)` | `string` | Random salt plus PBKDF2-SHA256 derived key, returned as Base64 of `salt \|\| hash` |
| `VerifyHash(string, string)` | `bool` | Recomputes the hash from the stored salt. Returns `false` for empty, malformed or non-matching input instead of throwing |

Every `Encrypt`, `Decrypt` and `EncryptBytes` call re-runs PBKDF2 to derive the key. In hot paths, batch values or cache the result rather than calling per field.

### EncryptionKeyManager

`EncryptionKeyManager` stores one AES-256 key per tenant as JSON under `keyStorePath`, which is created if it does not exist. Active keys are written to `{tenantId}_key.json`. Rotated keys are archived to `{tenantId}_key_v{version}.json`. Keys are cached in memory after the first read.

| Method | Returns | Description |
|--------|---------|-------------|
| `GenerateKeyAsync(tenantId, masterPassword?)` | `EncryptionKey` | Creates a random 256-bit key at version 1. **Overwrites the active key file if one exists.** Use `RotateKeyAsync` for existing tenants |
| `GetActiveKeyAsync(tenantId)` | `EncryptionKey?` | Returns the active key, or `null` if none exists or the file cannot be read |
| `RotateKeyAsync(tenantId, masterPassword?)` | `EncryptionKey` | Archives the current key as `v{n}`, then creates a new key at version `n + 1` with `PreviousKeyId` set |
| `GetKeyVersionAsync(tenantId, version)` | `EncryptionKey?` | Loads an archived key version, for example to re-encrypt data after a rotation |
| `DeleteTenantKeysAsync(tenantId)` | `bool` | Deletes all key files for the tenant and removes the cache entry. Returns `false` on error |

`EncryptionKey` exposes `KeyId`, `TenantId`, `KeyMaterial` (raw key bytes), `CreatedAt`, `DeactivatedAt`, `IsActive`, `Version` and `PreviousKeyId`.

The optional `masterPassword` is XORed with PBKDF2-derived bytes at generation time. The stored `KeyMaterial` is the final key, so the password is not needed to read the key later. Anyone with read access to the key file can use the key. On Unix, key files are set to mode `600` after they are written, so protect the key directory accordingly.

### Usage Example

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Security;

// IConfiguration is normally injected; it must contain Encryption:Key
var encryption = new EncryptionService(
    config,
    loggerFactory.CreateLogger<EncryptionService>());

// Field-level encryption of a sensitive string
string cipher = encryption.Encrypt("123-45-6789");
string plain = encryption.Decrypt(cipher);

// Password hashing and verification
string stored = encryption.HashPassword("correct horse battery staple");
bool ok = encryption.VerifyHash("correct horse battery staple", stored);   // true
bool bad = encryption.VerifyHash("wrong password", stored);                // false

// Per-tenant keys with rotation
var keyManager = new EncryptionKeyManager(
    loggerFactory.CreateLogger<EncryptionKeyManager>(),
    keyStorePath: "./keys");

var key = await keyManager.GenerateKeyAsync("tenant-42");
Console.WriteLine($"Created key v{key.Version} ({key.KeyId})");

var rotated = await keyManager.RotateKeyAsync("tenant-42");
Console.WriteLine($"Rotated to v{rotated.Version}, previous key {rotated.PreviousKeyId}");

// Archived key, needed to decrypt data written before the rotation
var previous = await keyManager.GetKeyVersionAsync("tenant-42", 1);

// Remove all key material when the tenant is deprovisioned
await keyManager.DeleteTenantKeysAsync("tenant-42");
```

## RateLimiter

`src/Security/RateLimiter.cs` implements `IRateLimiter` and limits how many requests an identifier (an IP address, user ID or API key) can make within a time window. `src/Security/RateLimiterOptions.cs` holds the cleanup settings. All state is kept in memory, so limits are per process and reset on restart.

`RateLimiter` is not registered by `AddSqliteMultiTenant`; create it yourself or register it in your host.

**Buckets**

Each identifier has one `RateLimitBucket` with `Identifier`, `CreatedAt`, `LastAccessedAt` and `Requests`. `Requests` is the list of UTC timestamps of accepted requests. On every `CheckLimitAsync` call the limiter first removes timestamps older than the window, then:

- if fewer than `maxRequests` timestamps remain, the request is accepted and its timestamp is added;
- otherwise the request is denied and nothing is recorded.

This is a sliding-window log. A denied request does not count against the limit, so a client that keeps retrying is only blocked by the window, not extended by its own attempts. `maxRequests` and `window` are passed per call and the bucket is keyed by identifier only, so the values of the latest call apply to that check.

**Cleanup**

A timer runs every `CleanupInterval` and removes buckets whose `LastAccessedAt` is older than `ExpirationTime`. The timer starts in the constructor, so set the options before creating the limiter. `RateLimiter` has a `Dispose()` method but does not implement `IDisposable`, so a DI container will not dispose it. Call `Dispose()` on the instance at shutdown to stop the timer.

**`RateLimiterOptions`**

| Property | Default | Description |
|----------|---------|-------------|
| `CleanupInterval` | `5 minutes` | How often the cleanup timer runs |
| `ExpirationTime` | `1 hour` | How long a bucket may go unused before it is removed |

Passing `null` to the constructor uses the defaults.

**`RateLimiter` methods**

| Method | Returns | Description |
|--------|---------|-------------|
| `CheckLimitAsync(identifier, maxRequests, window)` | `RateLimitResult` | Accepts or denies one request (see Buckets). Throws `ArgumentException` when `identifier` is null or empty |
| `GetStatusAsync(identifier)` | `RateLimitStatus` | Returns the bucket state without recording a request. An unknown identifier returns `CurrentCount` 0 and default timestamps |
| `ResetAsync(identifier)` | `Task` | Removes the bucket, so the identifier starts again with an empty window |
| `GetStatisticsAsync()` | `RateLimiterStatistics` | Available on the concrete class only. Returns the bucket count, the total tracked requests and the oldest and newest bucket creation times. Throws `InvalidOperationException` when there are no buckets |

**`RateLimitResult`** (returned by `CheckLimitAsync`)

| Property | Description |
|----------|-------------|
| `IsAllowed` | `true` when the request was recorded |
| `CurrentCount` | Requests in the window after this check. When denied, equals `MaxCount` |
| `MaxCount` | The `maxRequests` value used for the check |
| `ResetTime` | UTC time when the oldest tracked request leaves the window. When nothing is tracked, the current time plus `window` |
| `RemainingRequests` | `MaxCount - CurrentCount`, never below 0 |
| `TimeUntilReset` | `ResetTime` minus the current UTC time, evaluated when read |

**`RateLimitStatus`** (returned by `GetStatusAsync`)

| Property | Description |
|----------|-------------|
| `Identifier` | The identifier that was queried |
| `CurrentCount` | Number of timestamps stored in the bucket. Timestamps are only pruned by `CheckLimitAsync`, so this can include requests that are already outside the window |
| `CreatedAt` | When the bucket was created (UTC) |
| `LastAccessedAt` | When the bucket was last used by `CheckLimitAsync` (UTC). `GetStatusAsync` does not update it |

**Helpers in `RateLimitResultExtensions`**

| Method | Returns | Description |
|--------|---------|-------------|
| `ToRetryAfterHeaderValue()` | `string?` | Seconds until reset, rounded up and at least 1, for a denied result. `null` when the request was allowed |
| `ToRateLimitHeaders()` | `IReadOnlyDictionary<string, string>` | `X-RateLimit-Limit`, `X-RateLimit-Remaining` and `X-RateLimit-Reset` (Unix epoch seconds) |
| `ToDisplayString()` | `string` | Human-readable summary, for example `Denied: 10/10 requests, resets in 42s`. Overloads exist for `RateLimitResult` and `RateLimitStatus` |

### Usage Example

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Security;

// Assume 'loggerFactory' is the application's ILoggerFactory
var limiter = new RateLimiter(
    loggerFactory.CreateLogger<RateLimiter>(),
    new RateLimiterOptions
    {
        CleanupInterval = TimeSpan.FromMinutes(5),
        ExpirationTime = TimeSpan.FromHours(1)
    });

// Allow 10 requests per client IP per minute
string clientIp = "203.0.113.7";
RateLimitResult result = await limiter.CheckLimitAsync(
    identifier: clientIp,
    maxRequests: 10,
    window: TimeSpan.FromMinutes(1));

if (!result.IsAllowed)
{
    // 'httpContext' is the current HttpContext in an ASP.NET Core endpoint
    httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

    foreach (var header in result.ToRateLimitHeaders())
    {
        httpContext.Response.Headers[header.Key] = header.Value;
    }

    string? retryAfter = result.ToRetryAfterHeaderValue();
    if (retryAfter is not null)
    {
        httpContext.Response.Headers["Retry-After"] = retryAfter;
    }

    Console.WriteLine(result.ToDisplayString());
    return;
}

// Inspect the current state without recording a request
RateLimitStatus status = await limiter.GetStatusAsync(clientIp);
Console.WriteLine(status.ToDisplayString());

// Clear the window for an identifier, for example after a manual unblock
await limiter.ResetAsync(clientIp);

// Stop the cleanup timer when the application shuts down
limiter.Dispose();
```

## TenantProvisioner

`src/Tenants/TenantProvisioner.cs` is a sealed class that handles the lifecycle of tenant databases on disk: provisioning, encrypted provisioning, cloning, renaming, deprovisioning and validation. Each tenant gets its own directory under `basePath`, named after the tenant ID, containing `{tenantId}.db`. The class is not registered by `AddSqliteMultiTenant`; create it yourself or register it in your host.

**Constructor**

`TenantProvisioner(ITenantRepository tenantRepository, SchemaManager schemaManager, ILogger<TenantProvisioner> logger, string basePath, ILoggerFactory? loggerFactory = null, IEventBus? eventBus = null)`

- `loggerFactory` is used to create the logger for the per-tenant `SchemaManager`. When it is `null`, a null logger factory is used.
- `eventBus` receives `TenantClonedEvent` and `TenantRenamedEvent`. `RenameTenantAsync` skips publishing when it is `null`, but `CloneTenantAsync` does not check for it, so pass an event bus if you use cloning.

**Key members**

| Method | Returns | Description |
|--------|---------|-------------|
| `ProvisionTenantAsync(tenantId, tenantName, settings?)` | `Tenant` | Creates `{basePath}/{tenantId}/{tenantId}.db`, initializes the schema for that tenant and stores the `Tenant` record with status `Active`. `settings` is accepted but not used by this method. Throws `ArgumentException` when the ID or name is empty |
| `ProvisionEncryptedTenantAsync(tenantId, tenantName, encryptionKey, settings?)` | `Tenant` | Same as above, but the database is created with the SQLCipher connection string built by `SqlCipherConnectionBuilder`. Requires the `SQLitePCLRaw.bundle_sqlcipher` package. The key is never stored by this method; keep it in your own key store, for example `EncryptionKeyManager` |
| `CloneTenantAsync(sourceTenantId, targetTenantId)` | `string` | Creates a new database for `targetTenantId`, copies every user table (schema and rows) with `ATTACH` and `INSERT ... SELECT`, stores a tenant named `Clone of {source name}`, publishes `TenantClonedEvent` and returns the new database path. Throws `InvalidOperationException` when the source does not exist. Only tables are copied: indexes, triggers, views and constraints such as primary keys are not, because `CREATE TABLE ... AS SELECT` does not carry them over |
| `RenameTenantAsync(oldTenantId, newTenantId, cancellationToken = default)` | `Tenant` | Renames a tenant (see the flow below). Throws `ArgumentException` for empty or invalid IDs, or when the new ID differs only by case from the old one. Throws `TenantNotFoundException` when the old tenant does not exist. Throws `InvalidOperationException` when the new ID is taken or a file already exists at the target. Throws `IOException` when the database is busy or verification fails |
| `DeprovisionTenantAsync(tenantId, deleteBackups = false)` | `bool` | Deletes the database file, the tenant directory (recursively) and the tenant record. Returns `false` when the tenant does not exist. The `deleteBackups` parameter is accepted but the current implementation does not delete any backup files |
| `ValidateTenantDatabaseAsync(tenantId)` | `bool` | Returns `true` only when the tenant record exists, the database file exists, it opens, and the tables `Tenants` and `AuditLog` are present. Any exception is logged and returns `false`. Throws `ArgumentNullException` or `ArgumentException` for a null or blank ID |

Failures in provisioning, cloning, renaming and deprovisioning are logged with `ILogger<TenantProvisioner>` and rethrown.

**Rename flow (`RenameTenantAsync`)**

1. Validates the new ID with `TenantNameValidator.ValidateTenantId` after trimming it, and checks that it is not already registered.
2. Runs a `PRAGMA wal_checkpoint(TRUNCATE)` on the source. If the database is busy or a non-empty WAL file remains, the rename fails with `IOException` and can be retried later.
3. Copies the file to `{newId}.db.rename-tmp` in the new directory, compares SHA-256 hashes of the source and the copy, and runs `PRAGMA quick_check` on the copy.
4. Moves the copy to its final name. If any step before this fails, the temporary file is deleted and the tenant is unchanged.
5. Inserts the new tenant record, then deletes the old one. If deleting the old record fails, the new record and its file are removed.
6. Deletes the original database file and its directory when that directory is empty. A failure here is logged as a warning and the rename is kept.

> **Note:** callers must evict pooled connections to the old database before renaming. `TenantProvisioner` does not manage connection pools.

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Database;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Tenants;

// Assume 'loggerFactory' is the application's ILoggerFactory
const string registryConnectionString = "Data Source=registry.db;";
const string tenantsBasePath = "/var/lib/app/tenants";

var tenantRepository = new TenantRepository(
    registryConnectionString,
    loggerFactory.CreateLogger<TenantRepository>());

var provisioner = new TenantProvisioner(
    tenantRepository: tenantRepository,
    schemaManager: new SchemaManager(loggerFactory.CreateLogger<SchemaManager>(), registryConnectionString),
    logger: loggerFactory.CreateLogger<TenantProvisioner>(),
    basePath: tenantsBasePath,
    loggerFactory: loggerFactory,
    eventBus: new EventBus(loggerFactory.CreateLogger<EventBus>()));

// Create a tenant. Creates /var/lib/app/tenants/acme/acme.db with the schema
Tenant acme = await provisioner.ProvisionTenantAsync("acme", "Acme Corporation");
Console.WriteLine($"Provisioned {acme.TenantId} at {acme.DatabasePath}");

// Create an encrypted tenant. The key comes from your own secret store
string encryptionKey = Environment.GetEnvironmentVariable("ACME_DB_KEY")
    ?? throw new InvalidOperationException("ACME_DB_KEY is not set");
await provisioner.ProvisionEncryptedTenantAsync("globex", "Globex", encryptionKey);

// Copy an existing tenant for testing or replication. Returns the new database path
string clonePath = await provisioner.CloneTenantAsync("acme", "acme-staging");
Console.WriteLine($"Clone written to {clonePath}");

// Rename a tenant. The old database is removed only after the copy is verified
// Evict any pooled connections to "acme" before calling this
try
{
    Tenant renamed = await provisioner.RenameTenantAsync("acme", "acme-corp");
    Console.WriteLine($"Renamed to {renamed.TenantId}");
}
catch (IOException ex)
{
    // The database was busy or the copy failed verification. The tenant is unchanged; retry later
    Console.WriteLine($"Rename deferred: {ex.Message}");
}

// Check that the database is present and has the expected tables
bool healthy = await provisioner.ValidateTenantDatabaseAsync("acme-corp");

// Remove a tenant. Deletes its database and directory. This cannot be undone
bool removed = await provisioner.DeprovisionTenantAsync("acme-staging");
Console.WriteLine($"Validated: {healthy}, removed staging: {removed}");
```

## TenantQuotaEnforcer

`src/Tenants/TenantQuotaEnforcer.cs` is a sealed class that enforces a storage limit per tenant. The limit is stored in the tenant's metadata under the key `quota.maxBytes` (`TenantQuotaEnforcer.QuotaMetadataKey`), as an invariant-culture integer string. Usage is measured with `ITenantService.GetTenantDatabaseSizeAsync`, using `TotalSizeBytes`, which is the database file size plus the WAL file size. The class is not registered by `AddSqliteMultiTenant`; create it yourself or register it in your host.

**Constructor**

`TenantQuotaEnforcer(ITenantService tenantService)` throws `ArgumentNullException` when `tenantService` is null.

**`WarningThreshold`**

A fraction between 0 and 1 (default `0.9`). A tenant is near its quota when its usage is at or above this fraction and it is not over quota. Setting a value of 0 or less, or greater than 1, throws `ArgumentOutOfRangeException`.

**Key members**

| Method | Returns | Description |
|--------|---------|-------------|
| `SetQuotaAsync(tenantId, maxBytes, cancellationToken = default)` | `Task` | Stores the quota through `SetTenantMetadataAsync`, replacing any previous value. Throws `ArgumentException` for a blank ID or when `maxBytes` is not positive |
| `GetQuotaAsync(tenantId, cancellationToken = default)` | `long?` | Reads the quota. Returns `null` when the tenant does not exist, has no quota, or the stored value cannot be parsed |
| `CheckQuotaAsync(tenantId, cancellationToken = default)` | `QuotaCheckResult` | Compares current size with the quota. Throws `TenantNotFoundException` when the tenant does not exist. Throws `InvalidOperationException` when the stored quota is zero or negative |
| `EnforceAsync(tenantId, autoSuspend = true, cancellationToken = default)` | `QuotaCheckResult` | Runs `CheckQuotaAsync`. When the tenant is over quota and `autoSuspend` is `true`, calls `SuspendTenantAsync`. The result is returned in both cases |
| `CheckAllTenantsAsync(maxDegreeOfParallelism = 4, cancellationToken = default)` | `List<QuotaCheckResult>` | Checks every tenant with bounded parallelism and returns the results ordered by `UsagePercent`, highest first. Throws `ArgumentOutOfRangeException` when `maxDegreeOfParallelism` is below 1 |
| `GetTenantsOverQuotaAsync(cancellationToken = default)` | `List<QuotaCheckResult>` | Runs `CheckAllTenantsAsync` and returns only the results with `IsOverQuota` set |
| `ScanAllAsync(cancellationToken = default)` | `List<QuotaCheckResult>` | Checks active tenants only (`GetActiveTenantsAsync`) and returns those that are near or over quota, worst first. Tenants that are not found while checking are skipped |

Tenants without a quota are treated as unlimited. Their `QuotaCheckResult` has `QuotaBytes` set to `null`, `UsagePercent` set to `0`, and both flags set to `false`.

`CheckAllTenantsAsync` and `GetTenantsOverQuotaAsync` do not skip tenants that disappear during the scan, so a concurrent delete can make them throw `TenantNotFoundException`. `ScanAllAsync` does skip them.

`CheckQuotaAsync` calls `GetTenantAsync`, which updates the tenant's access time as a side effect.

**`QuotaCheckResult`**

A `sealed record` returned by the check methods.

| Property | Description |
|----------|-------------|
| `TenantId` | The tenant that was checked |
| `CurrentSizeBytes` | Database size plus WAL size, in bytes |
| `QuotaBytes` | The configured quota in bytes, or `null` when the tenant has no quota |
| `UsagePercent` | `CurrentSizeBytes / QuotaBytes * 100`, rounded to two decimals. `0` when unlimited. Can be above 100 |
| `IsOverQuota` | `true` when `UsagePercent` is 100 or more |
| `IsNearQuota` | `true` when `UsagePercent` is at or above `WarningThreshold * 100` and the tenant is not over quota |

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Tenants;

// Assume 'loggerFactory' is the application's ILoggerFactory
const string registryConnectionString = "Data Source=registry.db;";

var tenantService = new TenantService(
    new TenantRepository(registryConnectionString, loggerFactory.CreateLogger<TenantRepository>()),
    loggerFactory.CreateLogger<TenantService>());

var enforcer = new TenantQuotaEnforcer(tenantService)
{
    // Report "near quota" from 80% usage instead of the default 90%
    WarningThreshold = 0.8
};

// Limit "acme" to 50 MB
await enforcer.SetQuotaAsync("acme", 50L * 1024 * 1024);

// Read the usage without changing anything
QuotaCheckResult check = await enforcer.CheckQuotaAsync("acme");
Console.WriteLine($"{check.TenantId}: {check.CurrentSizeBytes} bytes, {check.UsagePercent}% of quota");

if (check.IsNearQuota)
{
    Console.WriteLine("Warning: acme is close to its storage limit");
}

// Check and suspend the tenant if it is over quota (autoSuspend defaults to true)
QuotaCheckResult enforced = await enforcer.EnforceAsync("acme");
if (enforced.IsOverQuota)
{
    Console.WriteLine("acme exceeded its quota and has been suspended");
}

// Periodic report: every tenant that is near or over quota, worst first
List<QuotaCheckResult> atRisk = await enforcer.ScanAllAsync();
foreach (QuotaCheckResult result in atRisk)
{
    Console.WriteLine($"{result.TenantId}: {result.UsagePercent}% (over quota: {result.IsOverQuota})");
}
```

## Background Workers

The classes in `src/BackgroundWorkers/` run maintenance, backup and retention work. `ScheduledTaskService` runs delegates on an interval. `BackupSchedulerService`, `BackupCleanupService` and `DatabaseMaintenanceWorker` are `BackgroundService` implementations. `BackupRotationManager` and `DataRetentionPolicy` are helpers you call yourself. `AddBackgroundWorkers()` already registers the three hosted workers with their defaults, so do not register a second copy of any of them.

### ScheduledTaskService

`src/BackgroundWorkers/ScheduledTaskService.cs` runs `Func<Task>` delegates at a fixed interval. A task can be tied to a tenant with `tenantId`, in which case it runs inside that tenant's context. The constructor takes an `ILogger<ScheduledTaskService>` and a `TenantContextHelper`. The class implements both `IScheduledTaskService` and `IHostedService`.

- Register tasks before calling `StartAsync`. A task registered after the service has started is not picked up.
- The first run happens one interval after registration.
- A task does not start a new run while its previous run is still executing.
- After a failure, the interval is multiplied by 2 for each failure, up to 32 times the base interval and 24 hours. `FailureCount` is never reset on success, so a task that has failed at any point keeps the longer interval.

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.BackgroundWorkers;
using SqliteMultiTenant.Utilities;

// Assume 'loggerFactory' is the application's ILoggerFactory
var tenantContextHelper = new TenantContextHelper(loggerFactory.CreateLogger<TenantContextHelper>());
var scheduler = new ScheduledTaskService(
    loggerFactory.CreateLogger<ScheduledTaskService>(),
    tenantContextHelper);

// Run every 15 minutes inside the "acme" tenant context
scheduler.RegisterTask(
    "acme-cleanup",
    () => CleanupAcmeAsync(), // Your Func<Task>
    TimeSpan.FromMinutes(15),
    tenantId: "acme");

await scheduler.StartAsync();

TaskExecutionStatus status = await scheduler.GetTaskStatusAsync("acme-cleanup");
Console.WriteLine($"Runs: {status.ExecutionCount}, failures: {status.FailureCount}, next: {status.NextExecutionAt:O}");

await scheduler.StopAsync();
```

`GetTaskStatusAsync` throws `KeyNotFoundException` for an unknown task ID.

### BackupSchedulerService

`src/BackgroundWorkers/BackupScheduler.cs` is a `BackgroundService` that runs a backup job once per interval (default 24 hours). It checks the clock every minute. The constructor is `(IBackupService, ITenantService, ILogger<BackupSchedulerService>, TimeSpan? interval = null)`. See [Scheduled Backup of All Tenants via BackgroundService](#scheduled-backup-of-all-tenants-via-backgroundservice) for the full host setup.

- The first run is scheduled two hours after startup. The class summary's "2 AM UTC" is not what the code does.
- The job lists tenants and logs each one, but it does not call `IBackupService` yet. Treat it as a placeholder until that step is implemented.

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.BackgroundWorkers;
using SqliteMultiTenant.Services;

// Run the backup job every 6 hours instead of the default 24
builder.Services.AddHostedService(sp => new BackupSchedulerService(
    sp.GetRequiredService<IBackupService>(),
    sp.GetRequiredService<ITenantService>(),
    sp.GetRequiredService<ILogger<BackupSchedulerService>>(),
    TimeSpan.FromHours(6)));
```

`BackupCleanupService` in the same file follows the same pattern. Its cleanup pass also only logs and does not delete anything yet.

### BackupRotationManager

`src/BackgroundWorkers/BackupRotationManager.cs` rotates and checks the `*.db` files in a backup directory. It is not a hosted service. The constructor takes an `ILogger<BackupRotationManager>`.

`RotateBackupsAsync` deletes files older than `MaxBackupAge` first. It then deletes the oldest files beyond `MaxBackupCount`, ordering by `CreationTimeUtc`. Point it at a directory that holds only backups, because it deletes files.

- `MaxDiskUsage` is not enforced. `RotateBackupsAsync` ignores it.

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.BackgroundWorkers;

var rotation = new BackupRotationManager(loggerFactory.CreateLogger<BackupRotationManager>());

var policy = new BackupRotationPolicy
{
    MaxBackupAge = TimeSpan.FromDays(14),
    MaxBackupCount = 10
};

const string backupDirectory = "/var/backups/tenants";

BackupRotationResult result = await rotation.RotateBackupsAsync(backupDirectory, policy);
Console.WriteLine($"Deleted {result.DeletedByAge} by age and {result.DeletedByCount} over the count limit; {result.RemainingBackups} remain");

List<BackupVerificationResult> checks = await rotation.VerifyBackupsAsync(backupDirectory);
BackupStatistics stats = rotation.GetBackupStatistics(backupDirectory);
Console.WriteLine($"{stats.TotalBackups} backups, {stats.TotalDiskUsage} bytes in total");
```

### DatabaseMaintenanceWorker

`src/BackgroundWorkers/DatabaseMaintenanceWorker.cs` is a `BackgroundService`. It runs `ITenantDatabaseMaintenanceService.PerformFullMaintenanceOnAllAsync` (VACUUM, ANALYZE and `PRAGMA optimize`) on startup and then once per interval. The default interval is 24 hours. After an unexpected error it waits one hour before retrying.

- `DatabaseMaintenanceOptions`, defined in the same file, is not read by the worker yet. It always runs the full maintenance pass.

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.BackgroundWorkers;
using SqliteMultiTenant.Services;

// Run maintenance every 12 hours instead of the default 24
builder.Services.AddHostedService(sp => new DatabaseMaintenanceWorker(
    sp.GetRequiredService<ILogger<DatabaseMaintenanceWorker>>(),
    sp.GetRequiredService<ITenantDatabaseMaintenanceService>(),
    TimeSpan.FromHours(12)));
```

### DataRetentionPolicy

`src/BackgroundWorkers/DataRetentionPolicy.cs` applies retention rules to one tenant's SQLite connection. It is not registered in dependency injection, so create it yourself with an `ILogger<DataRetentionPolicy>`. Each `RetentionRule` deletes rows whose `DateColumn` is older than `RetentionValue` days, months or years. `GetDefaultPolicy(tenantId)` returns one rule that deletes `AuditLog.CreatedAt` values older than 7 years.

- `SimulateRetentionPolicyAsync` counts the rows that would be deleted without deleting them. Use it before `ApplyRetentionPolicyAsync`.
- Table and column names are written into the SQL text, so take them from trusted configuration, never from user input.
- Do not set `ArchiveBeforeDelete` to `true` yet. The archive statement starts with a stray `@`, so the rule fails with `Failed: ...` and deletes nothing.
- Errors from SQL execution are returned in `RetentionResult.Error` and each rule's `Status`. They are not thrown.

```csharp
using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.BackgroundWorkers;

var retention = new DataRetentionPolicy(loggerFactory.CreateLogger<DataRetentionPolicy>());

// Start from the default AuditLog rule, then add a rule for another table
RetentionPolicyConfig policy = retention.GetDefaultPolicy("acme");
policy.Rules.Add(new RetentionRule
{
    TableName = "Sessions",
    DateColumn = "LastSeenAt",
    RetentionType = RetentionType.DaysOld,
    RetentionValue = 90,
    IsEnabled = true,
    ArchiveBeforeDelete = false
});

using var connection = new SQLiteConnection("Data Source=acme.db;Version=3;");
connection.Open();

// Preview first: counts rows without deleting anything
DryRunResult preview = await retention.SimulateRetentionPolicyAsync(connection, policy);
Console.WriteLine($"{preview.TotalRecordsWouldDelete} rows would be deleted");

RetentionResult result = await retention.ApplyRetentionPolicyAsync(connection, policy);
Console.WriteLine($"Deleted {result.TotalRecordsDeleted} rows: {(result.IsSuccessful ? "ok" : result.Error)}");
```

## BatchProcessor

`src/Operations/BatchProcessor.cs` runs an asynchronous operation over a collection of items with a bounded concurrency level. Each item is isolated: a failure is recorded in `BatchProcessResult<T>.Errors` and does not stop the other items. The class implements `IBatchProcessor`.

**Key members:**

| Member | Returns | Description |
|--------|---------|-------------|
| `ProcessAsync<TItem, TResult>(items, Func<TItem, Task<TResult>>, maxConcurrency = 4)` | `BatchProcessResult<TResult>` | Collects each successful result |
| `ProcessAsync<TItem>(items, Func<TItem, Task>, maxConcurrency = 4)` | `BatchProcessResult<object>` | Fire-and-forget style; successes are stored as placeholder `object` values |
| `BatchProcessResult<T>.SuccessCount`, `ErrorCount`, `SuccessRate` | `int` / `double` | Summary counters |

Notes:
- `SuccessfulResults` is in completion order, not input order. Errors carry the zero-based input index as `ItemId`, so map failures back to inputs through that index.
- `maxConcurrency` is passed directly to `SemaphoreSlim`, so it must be at least 1.
- `items` and `operation` throw `ArgumentNullException` when null.

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Operations;

// 'logger' is an ILogger<BatchProcessor> obtained from your logging setup
var processor = new BatchProcessor(logger);

var tenantIds = new List<string> { "tenant-a", "tenant-b", "tenant-c" };

// Run at most two operations at a time; each item's failure is isolated
BatchProcessResult<int> result = await processor.ProcessAsync(
    tenantIds,
    async tenantId => await CountOrdersAsync(tenantId), // your Func<string, Task<int>>
    maxConcurrency: 2);

Console.WriteLine(result); // BatchProcessResult: 2 success, 1 errors, Success Rate: 66.67%

foreach (var error in result.Errors)
{
    // ItemId is the input index of the failed item, e.g. "2"
    Console.WriteLine($"Item {error.ItemId} failed: {error.Exception} - {error.Message}");
}
```

## BulkInsertBuilder

`src/Operations/BulkInsertBuilder.cs` collects records as `Dictionary<string, object>` and inserts them into one table through a fluent API. Records are written in batches inside a single SQLite transaction. The file also contains `BulkUpdateBuilder`, which runs one `UPDATE` for a set of column assignments and a caller-supplied `WHERE` clause.

**Key members:**

| Member | Returns | Description |
|--------|---------|-------------|
| `new BulkInsertBuilder(connection, logger, tableName, batchSize = 1000)` | `BulkInsertBuilder` | `batchSize` values of 0 or less fall back to 1000 |
| `AddRecord(record)` / `AddRecords(records)` | `BulkInsertBuilder` | Queue records for insertion |
| `ExecuteAsync()` | `Task<BulkInsertResult>` | Inserts all records in one transaction |
| `GenerateSqlStatements()` | `string` | Returns the `INSERT` statements as text without executing them |

Notes:
- Column names are taken from the first record of each batch. Records missing a column receive `DBNull`, and extra keys in later records are ignored.
- The connection must be open before `ExecuteAsync()` is called.
- On failure the transaction is rolled back, the error is logged, and the exception is rethrown. `BulkInsertResult.Error` is set as well.
- `GenerateSqlStatements()` escapes values but not table or column names. Use it for inspection and logging only.
- `BulkUpdateBuilder.ExecuteAsync()` inserts the `WHERE` clause verbatim. Never pass untrusted input as that clause. It catches exceptions and reports them through `BulkUpdateResult.Error` instead of rethrowing.

### Usage Example

```csharp
using System.Data.SQLite;
using SqliteMultiTenant.Operations;

using var connection = new SQLiteConnection("Data Source=tenant-a.db;Version=3;");
await connection.OpenAsync();

var newOrders = new List<Dictionary<string, object>>
{
    new() { ["Id"] = 101, ["CustomerId"] = 7, ["Total"] = 19.99m },
    new() { ["Id"] = 102, ["CustomerId"] = 9, ["Total"] = 5.00m }
};

// 'logger' is an ILogger<BulkInsertBuilder>; insert in batches of 500 rows
var builder = new BulkInsertBuilder(connection, logger, "Orders", batchSize: 500)
    .AddRecord(new Dictionary<string, object> { ["Id"] = 100, ["CustomerId"] = 3, ["Total"] = 42.50m })
    .AddRecords(newOrders);

BulkInsertResult result = await builder.ExecuteAsync();
Console.WriteLine($"Inserted {result.InsertedRecords}/{result.TotalRecords} rows, successful: {result.IsSuccessful}");

// Inspect the generated SQL without executing it
string preview = builder.GenerateSqlStatements();
Console.WriteLine(preview);
```

## BatchOperationHandler

`src/Operations/BatchOperationHandler.cs` executes a `BatchOperation` against a list of resource (tenant) IDs and returns a `BatchOperationResult` with per-resource outcomes. It enforces size limits before any work starts, supports two atomicity modes, and tracks progress by operation ID so callers can poll `GetStatusAsync`.

**Key members:**

| Member | Returns | Description |
|--------|---------|-------------|
| `ExecuteAsync(operation, cancellationToken)` | `Task<BatchOperationResult>` | Runs the operation, one resource group at a time |
| `GetStatusAsync(operationId)` | `Task<BatchOperationStatus?>` | Progress for an operation tracked by this handler instance, or `null` |
| `BatchOperation.AtomicityMode` | `BatchAtomicityMode` | `CrossTenant` (default, best-effort) or `SingleTenant` |
| `BatchOperationResult.GetOverallStatus()` | `BatchOperationAggregateStatus` | `AllSucceeded`, `AllFailed`, `PartialSuccess`, or `Empty` |

Notes:
- The handler constructor takes `ILogger<BatchOperationHandler>`, a `TenantContextHelper`, and an optional `SqliteMultiTenantOptions`. The options supply `MaxBatchItems` (default 500) and `MaxBatchPayloadSizeBytes` (default 1 MB). Exceeding either limit throws `BatchTooLargeException` before any resource is processed.
- In `SingleTenant` mode, if a tenant context is set, every resource ID must match the current tenant. Otherwise `UnauthorizedAccessException` is thrown.
- The per-resource operation is not implemented yet. `ExecuteOperationAsync` only logs, and `SingleTenant` mode returns a simulated transactional result without touching the database. Treat the results as a contract for the future implementation, not as proof that data changed.
- Status is stored in memory per handler instance. It is lost on restart and not shared between instances.

### Usage Example

```csharp
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Operations;
using SqliteMultiTenant.Utilities;

// 'logger' is an ILogger<BatchOperationHandler>; 'tenantContextHelper' comes from DI
var options = new SqliteMultiTenantOptions
{
    MaxBatchItems = 100,
    MaxBatchPayloadSizeBytes = 256 * 1024
};
var handler = new BatchOperationHandler(logger, tenantContextHelper, options);

var operation = new BatchOperation
{
    OperationType = "apply-migration",
    ResourceIds = new List<string> { "tenant-a", "tenant-b", "tenant-c" },
    Parameters = new Dictionary<string, object> { ["Version"] = "2026.10.1" },
    AtomicityMode = BatchAtomicityMode.CrossTenant, // best-effort: one failure does not stop the rest
    ContinueOnError = true
};

BatchOperationResult result = await handler.ExecuteAsync(operation, cancellationToken);
Console.WriteLine($"{result.SuccessCount} succeeded, {result.FailureCount} failed ({result.GetOverallStatus()})");

foreach (var resource in result.ResourceResults.Where(r => !r.Success))
{
    Console.WriteLine($"{resource.ResourceId}: {resource.Message}");
}

// Progress can be read by operation ID after (or during) execution
BatchOperationStatus? status = await handler.GetStatusAsync(operation.OperationId);
Console.WriteLine($"Progress: {status?.ProgressPercent ?? 0}%");
```

## ConflictResolutionService

`src/Operations/ConflictResolutionService.cs` compares two versions of a record (`Dictionary<string, object>`), reports the differences as `DataConflict` items, resolves them with a `ConflictResolutionStrategy`, and can write the resolved values to a table. Extension methods in `ConflictResolutionServiceExtensions.cs` add helpers for filtering and retrying, and `ConflictResolutionServiceValidation.cs` adds `Validate`, `IsValid`, and `EnsureValid` checks.

**Key members:**

| Member | Returns | Description |
|--------|---------|-------------|
| `DetectConflicts(local, remote)` | `ConflictDetectionResult` | Finds `ValueDifference`, `DeletedRemotely` (in local only), and `CreatedRemotely` (in remote only) fields |
| `ResolveConflictsAsync(conflicts, strategy)` | `Task<ConflictResolutionResult>` | Applies one `ConflictResolutionStrategy` to every conflict |
| `ApplyResolutionAsync(connection, table, keyColumn, keyValue, resolution)` | `Task<bool>` | Runs one `UPDATE` in a transaction. Returns `false` on any failure instead of throwing |
| `GetConflictingFields(local, remote)` (extension) | `Dictionary<string, ConflictResolutionStrategy>` | Suggested strategy per conflicting field |
| `ResolveConflictsAsync(conflicts, customResolver)` (extension) | `Task<ConflictResolutionResult>` | Resolves each field with your own function |
| `ApplyResolutionWithRetryAsync(...)` (extension) | `Task<bool>` | Wraps `ApplyResolutionAsync` with retries |

Strategy behavior:

| Strategy | Result for each field |
|----------|-----------------------|
| `PreferLocal` / `PreferRemote` | The local or remote value |
| `KeepBoth` | `"{local}|{remote}"` |
| `DiscardBoth` | `null` |
| `Merge` | Average for two `int` or two `long` values, `"{local}; {remote}"` for other values, or whichever side is non-null |

Notes:
- `ApplyResolutionAsync` uses resolved field names directly as column names in the SQL. Only pass field names that come from a trusted schema.
- `ApplyResolutionAsync` catches every exception and returns `false`. Because of that, the `SQLiteException` retry in `ApplyResolutionWithRetryAsync` never fires. Check the boolean result, and do not rely on the retry for transient SQLite errors.
- `ResolveConflictsAsync` with a strategy records any failure in `ConflictResolutionResult.Error` and does not throw.

### Usage Example

```csharp
using System.Data.SQLite;
using SqliteMultiTenant.Operations;

// 'logger' is an ILogger<ConflictResolutionService>
var service = new ConflictResolutionService(logger);

var local = new Dictionary<string, object> { ["Name"] = "Acme", ["Credits"] = 10 };
var remote = new Dictionary<string, object> { ["Name"] = "Acme Corp", ["Credits"] = 14, ["Region"] = "EU" };

// Name and Credits differ; Region exists only in the remote version
ConflictDetectionResult conflicts = service.DetectConflicts(local, remote);
Console.WriteLine($"Conflicts found: {conflicts.Conflicts.Count}");

// Merge: Name -> "Acme; Acme Corp", Credits -> 12 (average), Region -> "EU"
ConflictResolutionResult resolution =
    await service.ResolveConflictsAsync(conflicts, ConflictResolutionStrategy.Merge);

if (resolution.IsSuccessful)
{
    using var connection = new SQLiteConnection("Data Source=tenant-a.db;Version=3;");
    await connection.OpenAsync();

    bool applied = await service.ApplyResolutionWithRetryAsync(
        connection, "Customers", "Id", 42, resolution);
    Console.WriteLine(applied ? "Resolution written" : "Resolution failed");
}

// Custom per-field rule: keep the remote value for Region, otherwise the local value
ConflictResolutionResult custom = await service.ResolveConflictsAsync(
    conflicts,
    (field, type, localValue, remoteValue) =>
        field == "Region" ? remoteValue : localValue);
```

## BulkDataService

`src/BulkOperations/BulkDataService.cs` implements `IBulkDataService` for importing and exporting tenant SQLite data. It supports whole-database and single-table exports, streaming export as `IAsyncEnumerable<ExportBatch>`, single-table import from a `Stream`, and streaming import from an `IAsyncEnumerable<ImportBatch>`. Tenant files are resolved as `{BaseDatabasePath}/{databaseId}.db`. Supported formats are `BulkDataFormat.Json`, `Csv` and `Sql`. Configuration lives in `BulkDataOptions` (`src/BulkOperations/BulkDataOptions.cs`). Request and result types live in `src/BulkOperations/BulkDataModels.cs`.

Register the service with `AddBulkDataServices`, which is defined in `src/BulkOperations/BulkDataExtensions.cs`. It registers `IBulkDataService` as scoped, `BulkDataOptions` as a singleton, and `IEventBus` as a singleton only if one is not already registered.

**Key members:**

| Member | Returns | Description |
|--------|---------|-------------|
| `ExportDatabaseAsync(databaseId, format, options?, progress?, ct)` | `Task<BulkExportResult>` | Exports every user table (up to `MaxConcurrentTables` at a time) into one buffer and writes one artifact |
| `ExportTableAsync(databaseId, tableName, format, options?, progress?, ct)` | `Task<BulkExportResult>` | Exports one table and writes one artifact |
| `StreamExportAsync(databaseId, tableName, format, batchSize = 1000, ct)` | `IAsyncEnumerable<ExportBatch>` | Yields rows in batches; the last batch has `IsLastBatch = true` |
| `ImportTableAsync(databaseId, tableName, dataStream, format, options?, progress?, ct)` | `Task<BulkImportResult>` | Reads the whole stream into memory and imports it into one table |
| `StreamImportAsync(databaseId, batches, options?, progress?, ct)` | `Task<BulkImportResult>` | Imports each `ImportBatch` on one shared connection; with `SkipFailedRows`, failed batches are counted and skipped |

**Configuration (`BulkDataOptions`):**

| Property | Default | Applied by the service |
|----------|---------|------------------------|
| `DefaultBatchSize` | `1000` | Default for `StreamExportAsync` when `batchSize <= 0` |
| `MaxConcurrentTables` | `3` | Concurrency for `ExportDatabaseAsync` |
| `OperationTimeout` | `1 hour` | No (validated only) |
| `MaxBufferSizeBytes` | `10_000_000` | No (validated only) |
| `PublishDomainEvents` | `true` | Publishes `BulkExport*` and `BulkImport*` events on `IEventBus` |
| `EnableProgressReporting` | `true` | Calls `IProgress<T>` callbacks |
| `DefaultExportDirectory` | `./exports` | Artifact directory when `ExportOptions.OutputFilePath` is `null` |
| `BaseDatabasePath` | `./databases` | Directory containing `{databaseId}.db` files |

**Result and progress types (`BulkDataModels.cs`):**

| Type | Key members |
|------|-------------|
| `BulkDataFormat` | `Json` (0), `Csv` (1), `Sql` (2) |
| `DuplicateStrategy` | `Abort`, `Skip`, `Replace`. Not read by the service yet (see notes) |
| `ExportOptions` | `IncludeMetadata` (JSON envelope), `IncludeCsvHeaders`, `OutputFilePath` |
| `ImportOptions` | `TruncateBeforeImport`, `SkipFailedRows` (streaming import), `CsvDelimiter`, `CsvHasHeaders` |
| `ExportBatch` | `TableName`, `Data`, `RowCount`, `SequenceNumber`, `IsLastBatch` |
| `ImportBatch` | `TableName`, `Data`, `Format`, `SequenceNumber`, `IsLastBatch` |
| `ExportProgress` | `TableName`, `RowsProcessed`, `TotalRowsEstimate`, `BatchSequence`, `PercentComplete` |
| `ImportProgress` | `TableName`, `RowsImported`, `RowsFailed`, `BatchSequence`, `TotalAttempted`, `SuccessRate` |
| `BulkExportResult` | `IsSuccess`, `TablesProcessed`, `TotalRowsExported`, `OutputPath`, `Duration`, `Warnings`, `ErrorMessage` |
| `BulkImportResult` | `IsSuccess`, `TablesProcessed`, `TotalRowsImported`, `TotalRowsFailed`, `Duration`, `Warnings`, `ErrorMessage` |

Notes:
- Every method takes a `databaseId` and a `tableName`, but the service does not validate table names. `StreamExportAsync` builds `SELECT * FROM [{tableName}]` by string interpolation, so never pass untrusted table names.
- `ExportDatabaseAsync` returns `IsSuccess = true` even when some tables fail. Per-table failures are added to `Warnings` as `Table export failed [index]: message`.
- Exceptions raised before the `try` block are thrown, not returned. This includes opening the database in `ExportDatabaseAsync`, `ExportTableAsync`, `StreamExportAsync` and `StreamImportAsync`. Other failures are returned as `IsSuccess = false` with `ErrorMessage` set.
- `ExportOptions.BatchSize`, `WhereClause` and `IncludeSchema`, and `ImportOptions.DuplicateHandling` and `CommitEveryNRows`, are declared but not read by the current implementation. `OperationTimeout` and `MaxBufferSizeBytes` are validated but not enforced. Do not rely on them.
- `StreamExportAsync` with `BulkDataFormat.Sql` emits `INSERT INTO [...]` using the first column name as the table name. Use `ExportTableAsync` for SQL export.
- `TotalRowsEstimate` in export progress equals the number of rows in the batch, so `PercentComplete` reaches 100 once a table has been reported. It does not reflect the table's real total.
- `ImportTableAsync` always reports `TotalRowsFailed = 0`. Only `StreamImportAsync` counts failed rows.

### Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.BulkOperations;

// Register the service; options are applied through the configure delegate
services.AddBulkDataServices(opts =>
{
    opts.BaseDatabasePath = "./databases";
    opts.DefaultExportDirectory = "./exports";
    opts.MaxConcurrentTables = 2;
});

using var scope = provider.CreateScope();
var bulk = scope.ServiceProvider.GetRequiredService<IBulkDataService>();

// Export one tenant table to CSV with a progress callback
var export = await bulk.ExportTableAsync(
    "tenant-a",
    "Orders",
    BulkDataFormat.Csv,
    new ExportOptions { IncludeCsvHeaders = true, OutputFilePath = "./exports/tenant-a-orders.csv" },
    new Progress<ExportProgress>(p => Console.WriteLine($"{p.TableName}: {p.RowsProcessed} rows")));

if (export.IsSuccess)
{
    Console.WriteLine($"Exported {export.TotalRowsExported} rows to {export.OutputPath}");
}

// Stream a table in 500-row JSON batches without holding the whole result in memory
await foreach (ExportBatch batch in bulk.StreamExportAsync("tenant-a", "Orders", BulkDataFormat.Json, batchSize: 500))
{
    Console.WriteLine($"Batch {batch.SequenceNumber}: {batch.RowCount} rows (last: {batch.IsLastBatch})");
}

// Import streamed JSON batches into another tenant; failed batches are skipped and counted
static async IAsyncEnumerable<ImportBatch> ReadBatchesAsync()
{
    yield return new ImportBatch("Orders", "[{\"Id\":101,\"CustomerId\":7,\"Total\":19.99}]", BulkDataFormat.Json, 0, IsLastBatch: false);
    yield return new ImportBatch("Orders", "[{\"Id\":102,\"CustomerId\":9,\"Total\":5.00}]", BulkDataFormat.Json, 1, IsLastBatch: true);
}

BulkImportResult import = await bulk.StreamImportAsync(
    "tenant-b",
    ReadBatchesAsync(),
    new ImportOptions { SkipFailedRows = true });

if (!import.IsSuccess)
{
    Console.WriteLine($"Import failed: {import.ErrorMessage}");
}
else
{
    Console.WriteLine($"Imported {import.TotalRowsImported} rows, failed {import.TotalRowsFailed}");
}
```

## DataExporter

`src/DataOperations/DataExporter.cs` exports every row of one table from an open `SQLiteConnection` into JSON, CSV, raw SQL `INSERT` statements or JSON Lines. It is `sealed` and has two constructors: a parameterless one (used for JSON deserialization, logs to a `NullLogger`) and one that takes `ILogger<DataExporter>`. Failures are logged and rethrown.

**Key members:**

| Member | Returns | Description |
|--------|---------|-------------|
| `ExportAsJsonAsync(connection, tableName, includeMeta = true)` | `Task<string>` | Indented JSON. With `includeMeta`, the output is `{ "meta": { "Table", "RowCount", "ExportedAt" }, "data": [...] }`; otherwise a bare array |
| `ExportAsCsvAsync(connection, tableName, includeHeaders = true)` | `Task<string>` | CSV with `\n` line endings. Fields containing quotes, commas or newlines are quoted |
| `ExportAsSqlAsync(connection, tableName)` | `Task<string>` | `INSERT INTO [table] ([col], ...) VALUES (...);` per row, preceded by a two-line comment header |
| `ExportAsJsonLinesAsync(connection, tableName, outputPath)` | `Task` | Streams one JSON object per line to `outputPath` (UTF-8, overwrites an existing file) |

Notes:
- `tableName` is interpolated into `SELECT * FROM {tableName}` without quoting or validation. Never pass an untrusted table name.
- The string-returning methods buffer the whole table in memory. `ExportAsJsonLinesAsync` is the only one that writes rows as it reads them.
- NULL and an empty string both export as `""` in CSV. The importer reads empty CSV fields back as NULL, so the two are not distinguishable after a round trip.
- CSV and SQL output use `ToString()` for BLOB values, so byte arrays come out as `System.Byte[]` rather than their contents. JSON serializes them as base64.
- Booleans are written as `1`/`0` in SQL output. Other non-numeric values are written as single-quoted strings with `'` doubled.

### Usage Example

```csharp
using System.Data.SQLite;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.DataOperations;

using var connection = new SQLiteConnection("Data Source=./databases/tenant-a_primary.db");
await connection.OpenAsync();

var exporter = new DataExporter(NullLogger<DataExporter>.Instance);
Directory.CreateDirectory("./exports");

// JSON with the metadata envelope: { "meta": { ... }, "data": [ ... ] }
string json = await exporter.ExportAsJsonAsync(connection, "Orders");
await File.WriteAllTextAsync("./exports/orders.json", json);

// CSV without a header row
string csv = await exporter.ExportAsCsvAsync(connection, "Orders", includeHeaders: false);

// INSERT statements that recreate the rows in another SQLite database
string sql = await exporter.ExportAsSqlAsync(connection, "Orders");
await File.WriteAllTextAsync("./exports/orders.sql", sql);

// Streamed JSON Lines, written row by row straight to disk
await exporter.ExportAsJsonLinesAsync(connection, "Orders", "./exports/orders.jsonl");
```

## DataImporter

`src/DataOperations/DataImporter.cs` imports JSON, CSV and raw SQL into an open `SQLiteConnection`. Each import runs in one transaction, so a failure rolls back the whole batch. The class is `sealed` and has no parameterless constructor; it requires an `ILogger<DataImporter>`. The class also has a tenant-scoped CSV overload that opens the tenant database itself.

**Key members:**

| Member | Returns | Description |
|--------|---------|-------------|
| `ImportFromJsonAsync(connection, tableName, jsonData, truncateTable = false)` | `Task<int>` | Accepts a top-level array of objects, or an object with a `data` array. An object without `data` imports 0 rows |
| `ImportFromCsvAsync(connection, tableName, csvData, hasHeaders = true, delimiter = ",", truncateTable = false)` | `Task<int>` | Parses the first line as column names when `hasHeaders` is true. Empty fields are inserted as NULL |
| `ImportFromSqlAsync(connection, sqlStatements)` | `Task<int>` | Splits the input on `;`, runs each non-empty statement and returns the total rows affected |
| `ImportFromCsvAsync(tenantId, tableName, csvText)` | `Task<int>` | Opens `databases/{tenantId}_primary.db` relative to the working directory and imports with headers, comma delimiter and no truncation |

Notes:
- **JSON only imports string and null values.** A number or boolean property throws `InvalidOperationException`, because the value is read with `GetString()`.
- **Every JSON row is written with the values of the first record.** The column names and values both come from the first object, so a multi-record array inserts the first record repeatedly. Until this is fixed, import one record per call.
- An empty JSON input returns 0 without running `truncateTable`, so the target table is left as it is.
- CSV rows whose column count differs from the header are skipped with a warning and are not counted. Lines are split on `\r\n`, `\r` or `\n` before parsing, so a quoted field cannot contain a line break.
- `ImportFromSqlAsync` splits on every `;`, including ones inside string literals. Do not use it with values that contain semicolons.
- `ImportFromSqlAsync` runs any statement it receives, not only `INSERT`. Only pass trusted SQL.
- The tenant overload requires `tenantId` to be a positive integer made of digits only (for example `"42"`). It does not accept names such as `tenant-a`, and it does not truncate the table.

### Usage Example

```csharp
using System.Data.SQLite;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.DataOperations;

using var connection = new SQLiteConnection("Data Source=./databases/tenant-b_primary.db");
await connection.OpenAsync();

var importer = new DataImporter(NullLogger<DataImporter>.Instance);

// CSV with a header row; the empty Email field is stored as NULL
string csv = "Id,Name,Email\n1,Ada,ada@example.com\n2,Grace,\n";
int imported = await importer.ImportFromCsvAsync(connection, "Customers", csv, truncateTable: true);
Console.WriteLine($"Imported {imported} rows from CSV");

// Raw INSERT statements, all applied in one transaction
string sql = "INSERT INTO Customers (Id, Name, Email) VALUES (3, 'Linus', NULL); " +
             "INSERT INTO Customers (Id, Name, Email) VALUES (4, 'Ken', 'ken@example.com');";
int affected = await importer.ImportFromSqlAsync(connection, sql);
Console.WriteLine($"SQL statements affected {affected} rows");

// JSON: a single object, with every value written as a JSON string (numbers would throw)
string json = "[{\"Id\":\"5\",\"Name\":\"Barbara\",\"Email\":\"barbara@example.com\"}]";
int fromJson = await importer.ImportFromJsonAsync(connection, "Customers", json);
Console.WriteLine($"JSON imported {fromJson} rows");

// Tenant overload: opens databases/42_primary.db itself and appends the CSV rows
int tenantRows = await importer.ImportFromCsvAsync("42", "Customers", csv);
Console.WriteLine($"Tenant 42 received {tenantRows} rows");
```

## DataConsistencyChecker

`src/DataOperations/DataConsistencyChecker.cs` checks a tenant database for integrity problems, foreign key violations, a hard-coded set of expected indexes, and table row counts. It also finds likely duplicate rows and validates expected row counts. The class is `sealed`, implements `IEquatable<DataConsistencyChecker>`, and requires an `ILogger<DataConsistencyChecker>`. `CheckDatabaseIntegrityAsync` returns its results in a new `ConsistencyCheckResult`; the checker's own properties (`IsHealthy`, `OrphanedRecords`, `CheckedAt` and so on) are never set by any method.

**Key members:**

| Member | Returns | Description |
|--------|---------|-------------|
| `CheckDatabaseIntegrityAsync(connection)` | `Task<ConsistencyCheckResult>` | Runs all five checks and sets `IsHealthy` |
| `FindDuplicatesAsync(connection, tableName, keyColumns, similarityThreshold = 0.95)` | `Task<List<DuplicateRecord>>` | Compares every pair of rows on the key columns |
| `ValidateRecordCountsAsync(connection, expectedCounts)` | `Task<bool>` | `true` only if every `COUNT(*)` matches its expected value |

`ConsistencyCheckResult` holds `IsHealthy`, `IntegrityCheckPassed`, `OrphanedRecords`, `ForeignKeyViolations`, `MissingIndexes`, `TableStatistics` (keyed by table name) and `CheckedAt`. `ConstraintViolation`, `TableStatistics` and `DuplicateRecord` are declared in the same file.

Notes:
- `CheckDatabaseIntegrityAsync` throws `InvalidOperationException` if the connection is not open.
- `IsHealthy` is true only when the integrity check returns `ok`, and there are no orphaned records and no foreign key violations. `OrphanedRecords` and `ForeignKeyViolations` both come from `PRAGMA foreign_key_check`.
- Errors inside the individual checks are logged and swallowed. A failed foreign key check reports no violations, and a failed orphan check reports no orphans. A failed integrity check makes `IsHealthy` false; a failed orphan or foreign key check does not.
- `MissingIndexes` checks a hard-coded pair, `AuditLog.TenantId` and `AuditLog.CreatedAt`. A database without an `AuditLog` table reports both as missing. `MissingIndexes` does not affect `IsHealthy`.
- `TableStatistics` counts every user table with `SELECT COUNT(*) FROM {name}`. Table names are not quoted.
- `FindDuplicatesAsync` loads the whole table and compares every pair of rows, so it is O(n²) in memory and time. Similarity is the number of matching key columns divided by the number of key columns, and values are compared with `object.Equals`, so `1` and `"1"` do not match. With the default threshold of 0.95 every key column must match, unless there are 20 or more of them.
- `FindDuplicatesAsync` reports 0-based positions in the `SELECT *` result, not row IDs. Any error, including an unknown table name, is logged and returns an empty list.
- `ValidateRecordCountsAsync` stops at the first mismatch and returns `false`. Any exception, including a `null` argument, is logged and also returns `false`. Its XML documentation says it throws `ArgumentNullException`, but it does not.
- `Equals` compares every result property, including `CheckedAt`, so two separate runs are practically never equal.

### Usage Example

```csharp
using System.Collections.Generic;
using System.Data.SQLite;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.DataOperations;

using var connection = new SQLiteConnection("Data Source=./databases/tenant-a_primary.db");
await connection.OpenAsync();

var checker = new DataConsistencyChecker(NullLogger<DataConsistencyChecker>.Instance);

// Runs the integrity, orphan, foreign key, index and table statistics checks
ConsistencyCheckResult result = await checker.CheckDatabaseIntegrityAsync(connection);
Console.WriteLine(result);

foreach (var violation in result.ForeignKeyViolations)
{
    Console.WriteLine($"Foreign key violation: {violation}");
}

foreach (var stats in result.TableStatistics.Values)
{
    Console.WriteLine($"{stats.TableName}: {stats.RowCount} rows");
}

// Customers whose Name and Email both match (similarity 1.0, at or above the 0.95 threshold)
List<DuplicateRecord> duplicates =
    await checker.FindDuplicatesAsync(connection, "Customers", new[] { "Name", "Email" });
Console.WriteLine($"Found {duplicates.Count} duplicate pairs");

// Expect exactly two rows in Customers; returns false on the first mismatch or any error
bool countsMatch = await checker.ValidateRecordCountsAsync(
    connection,
    new Dictionary<string, int> { ["Customers"] = 2 });
Console.WriteLine($"Counts match: {countsMatch}");
```

## Middleware Pipeline

The five ASP.NET Core middleware classes in `src/Middleware/` run inside an `IApplicationBuilder` pipeline, for example a `WebApplication`. `AddSqliteMultiTenant` does not add any of them, so add the ones you need to your pipeline yourself.

**Recommended registration order**

1. `CorrelationIdMiddleware`: first, so every later log entry and response carries the correlation ID.
2. `ErrorHandlingMiddleware`: outside every middleware that can throw, so it catches their exceptions and writes the JSON error response.
3. `RateLimitingMiddleware`: before the logging and performance middleware, so rejected requests cost little work. It rethrows exceptions from the rest of the pipeline, which `ErrorHandlingMiddleware` then handles.
4. `LoggingMiddleware`: wraps the endpoint, so it logs the status and duration of the real response.
5. `PerformanceMiddleware`: innermost, so it measures the endpoint itself.

The `UseRequestResponseLogging()` extension in `src/Configuration/ServiceCollectionExtensions.cs` registers only `CorrelationIdMiddleware` and `PerformanceMiddleware`. It does not replace the full order above.

### CorrelationIdMiddleware

Reads the correlation ID from the `X-Correlation-Id` request header. If the header is missing or blank, it reads the `correlationId` query string value. If neither is present, it generates a new GUID in `N` format. The ID is stored in `HttpContext.Items` and written to the `X-Correlation-Id` response header when the response starts, unless the header is already set. The middleware logs the request and the response status with the ID.

Read the ID later with the `HttpContext.GetCorrelationId()` extension, which returns an empty string when no ID is stored.

### ErrorHandlingMiddleware

Catches exceptions from the rest of the pipeline and writes a JSON `ErrorResponse` with the camelCase properties `code`, `message` and `traceId`. `traceId` is `HttpContext.TraceIdentifier`. Exceptions are mapped as follows:

| Exception | Status | `code` | `message` returned to the client |
|-----------|--------|--------|----------------------------------|
| `TenantNotFoundException` | 404 | `TENANT_NOT_FOUND` | The exception message |
| `DatabaseAccessException` | 500 | `DATABASE_ERROR` | `Database operation failed` |
| `MigrationException` | 400 | `MIGRATION_FAILED` | The exception message |
| `BackupException` | 500 | `BACKUP_FAILED` | The exception message |
| `ArgumentException` | 400 | `INVALID_ARGUMENT` | The exception message |
| `UnauthorizedAccessException` | 401 | `UNAUTHORIZED` | The exception message |
| Any other exception | 500 | `INTERNAL_SERVER_ERROR` | `An unexpected error occurred` |

For the exceptions that return their own message, that message is sent to the client unchanged, so keep internal details out of those exception messages. The stack trace is not sent to the client. The middleware logs only the message for the mapped exceptions, and the type, message and stack trace for any other exception.

The middleware has no `Use...` extension method; register it with `UseMiddleware`.

### LoggingMiddleware

Logs each request with its method, path, `TraceIdentifier` as the request ID and the remote IP address. After the response it logs the status code and the duration in milliseconds. Responses with status 400 or higher are logged at Warning. Responses slower than 5000 ms are logged at Warning as `Slow HTTP Response`. Exceptions are logged at Error and rethrown.

The middleware replaces the response body with a `MemoryStream` while the rest of the pipeline runs, so each in-flight response is held in memory until it completes.

### PerformanceMiddleware

Measures the elapsed time of each request and the change in `GC.GetTotalMemory` during it. The memory figure is process-wide, so it is only an estimate when requests run concurrently. Requests slower than `slowRequestThresholdMs` (default 1000 ms) are logged at Warning, and the others at Information.

Each request's `RequestMetrics` is stored in `HttpContext.Items`. Read it with the `HttpContext.GetRequestMetrics()` extension.

The middleware also sets the `X-Response-Time-Ms` and `X-Memory-Used-Kb` response headers. It sets them after the response body has been copied back to the client, so the headers can only be added if the response has not started. Do not rely on them until this is fixed.

The middleware has a `UsePerformanceTracking(thresholdMs)` extension method. It is equivalent to `UseMiddleware<PerformanceMiddleware>()` with a different threshold.

### RateLimitingMiddleware

Applies one token bucket per key. The key is the value of the `X-Tenant-Id` request header if it is present. Otherwise it is the remote IP address, or `unknown` if there is none. Each bucket holds `RequestsPerMinute` tokens and refills at `RequestsPerMinute / 60` tokens per second. A request with no token available gets status 429, a `Retry-After: 60` header and the JSON body `{ "error": "Rate limit exceeded. Try again later." }`.

Requests to paths that start with `/health` or `/admin` are not limited.

The middleware reads its settings from a `RateLimitingOptions` instance in DI. Only `RequestsPerMinute` (default 300) is used. `BurstCapacity` and `CleanupIntervalSeconds` are defined but have no effect. `AddSqliteMultiTenant` does not register `RateLimitingOptions`, so register it yourself.

The limiter has these limitations:

- The `X-Tenant-Id` header is not authenticated. A client can send a new value for each request to get a fresh bucket, so the limit does not apply to it. Use this middleware for abuse protection, not for enforcing per-tenant quotas.
- Buckets are never removed, so the dictionary grows with each distinct key.

### Usage Example

```csharp
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqliteMultiTenant("Data Source=master.db;Version=3;");
builder.Services.AddSingleton(new RateLimitingOptions { RequestsPerMinute = 120 });
builder.Services.AddControllers();

var app = builder.Build();

// Recommended order: correlation ID, error handling, rate limiting, logging, performance
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<PerformanceMiddleware>();

app.MapControllers();
app.Run();
```

## Exception Hierarchy

All library exceptions are in the `SqliteMultiTenant.Exceptions` namespace. `MultiTenantException` is an abstract base class with a `TenantId` property, which is `null` when the failure is not tied to a tenant. Four of its subclasses inherit from it. `QuotaExceededException` and `BatchTooLargeException` derive directly from `Exception`, so a `catch (MultiTenantException)` block does not catch them.

| Exception | Base class | Thrown when | Extra properties |
|-----------|------------|-------------|------------------|
| `MultiTenantException` | `Exception` (abstract) | Never thrown directly. It is the base type for the subclasses below | `TenantId` |
| `TenantNotFoundException` | `MultiTenantException` | A tenant lookup or update fails because the tenant does not exist. Raised by `TenantService`, `TenantManager`, `TenantProvisioner`, `TenantQuotaEnforcer`, `TenantDatabaseMaintenanceService`, `IntegrityCheckService` and `TenantSizeReportService` | `TenantId` |
| `MigrationException` | `MultiTenantException` | A migration fails. `MigrationService` raises it when a migration script fails during execution, and `MigrationResultExtensions` raises it for a failed migration result | `MigrationId`, `MigrationVersion` |
| `BackupException` | `MultiTenantException` | A backup verification fails. `BackupVerificationResultExtensions` raises it when a verification result is a failure | `BackupId`, `DatabaseId` |
| `DatabaseAccessException` | `MultiTenantException` | A database operation fails. No code in `src/` throws it at present. `ErrorHandlingMiddleware` maps it to HTTP 500 with the code `DATABASE_ERROR` | `DatabaseId`, `OperationType` |
| `QuotaExceededException` | `Exception` | A tenant database is over its configured quota. Raised by `QuotaCheckResultExtensions.ThrowIfExceeded()`, which sets `DeltaBytes` to `0` because the check result has no pending size change | `TenantId`, `QuotaBytes`, `CurrentSizeBytes`, `DeltaBytes` |
| `BatchTooLargeException` | `Exception` | A batch operation has more items than the configured maximum, or a payload larger than the configured maximum size. Raised by `BatchOperationHandler` | `MaxItemCount`, `ActualItemCount`, `MaxPayloadSizeBytes`, `ActualPayloadSizeBytes` |

`ErrorHandlingMiddleware` maps `TenantNotFoundException`, `DatabaseAccessException`, `MigrationException` and `BackupException` to HTTP responses. See [Middleware Pipeline](#middleware-pipeline).

### Usage Example

Catch the most specific types first. Catch `MultiTenantException` last, and catch `QuotaExceededException` and `BatchTooLargeException` separately because they are not part of that hierarchy.

```csharp
using SqliteMultiTenant.Exceptions;

static async Task RunSafelyAsync(Func<Task> operation)
{
    try
    {
        await operation();
    }
    catch (TenantNotFoundException ex)
    {
        Console.WriteLine($"Tenant '{ex.TenantId}' does not exist.");
    }
    catch (QuotaExceededException ex)
    {
        Console.WriteLine($"Tenant '{ex.TenantId}' is over its quota of {ex.QuotaBytes} bytes.");
    }
    catch (BatchTooLargeException ex)
    {
        Console.WriteLine($"Batch of {ex.ActualItemCount} items exceeds the limit of {ex.MaxItemCount}.");
    }
    catch (MigrationException ex)
    {
        Console.WriteLine($"Migration {ex.MigrationId} (v{ex.MigrationVersion}) failed: {ex.Message}");
    }
    catch (BackupException ex)
    {
        Console.WriteLine($"Backup {ex.BackupId} failed: {ex.Message}");
    }
    catch (DatabaseAccessException ex)
    {
        Console.WriteLine($"Database {ex.DatabaseId} failed during {ex.OperationType}.");
    }
    catch (MultiTenantException ex)
    {
        // Any other library error that carries a tenant ID (may be null)
        Console.WriteLine($"Tenant error for '{ex.TenantId}': {ex.Message}");
    }
}

await RunSafelyAsync(() => tenantService.DeleteTenantAsync("acme"));
```

## WebhookService

`WebhookService` (`src/Integration/WebhookService.cs`) keeps subscriptions in memory, grouped by event type. `TriggerWebhooksAsync` POSTs a JSON envelope (`EventType`, `Timestamp`, `Data`, `Attempt`) to every active subscriber for that event type. Each delivery gets up to three attempts with a five-second pause between them. When a subscription has a secret, the request carries an `X-Signature` header with the hex-encoded HMAC-SHA256 of the request body. Receivers check it with the static `WebhookService.VerifySignature`, passing the raw body they received. A subscription is deactivated after more than 10 consecutive failed deliveries.

Subscriptions are not persisted, so they are lost when the process restarts. The service creates its own `HttpClient` (30-second timeout) and is not `IDisposable`, so create one instance and reuse it.

`WebhookHandler` (`src/Integration/WebhookHandler.cs`) is a separate, DI-friendly delivery component built on the `IWebhookHandler` interface. It takes an injected `HttpClient` and stores `WebhookHandlerSubscription` objects. A subscription with `EventType = "*"` matches every event. `GetSubscriptions(eventType)` returns the enabled subscriptions that match an event type. `DeliverAsync` posts a `WebhookDelivery` (the target `Url`, the `DomainEvent` payload and custom `Headers`), with `MaxRetries` (default 3) retries after the first attempt and delays of 2, 4 and 8 seconds. Each attempt times out after 30 seconds. `WebhookHandler` does not sign requests itself, so set `X-Signature` in `Headers` yourself if receivers need one.

`WebhookEventHandler<T>` is a placeholder. Its `HandleAsync` only logs at debug level and does not deliver anything yet, so do not rely on it for delivery.

### Usage Example

`WebhookService`: subscribe, trigger, and verify on the receiver side.

```csharp
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Integration;

var webhooks = new WebhookService(NullLogger<WebhookService>.Instance);

// Subscribe a receiver to tenant events; the secret signs every request body
string subscriptionId = await webhooks.SubscribeAsync(
    "TenantCreated",
    "https://hooks.example.com/tenants",
    headers: new Dictionary<string, string> { ["X-Api-Source"] = "sqlite-multi-tenant" },
    secret: "replace-with-shared-secret");

// Delivers to every active subscriber of this event type
await webhooks.TriggerWebhooksAsync("TenantCreated", new { TenantId = "acme", TenantName = "Acme Corp" });

List<WebhookSubscription> active = await webhooks.GetSubscriptionsAsync("TenantCreated");
Console.WriteLine($"Active subscribers: {active.Count}");

bool removed = await webhooks.UnsubscribeAsync(subscriptionId);
```

Receiver side: verify the signature against the raw body before acting on it.

```csharp
static bool IsTrustedWebhook(string rawBody, string? signature) =>
    !string.IsNullOrEmpty(signature) &&
    WebhookService.VerifySignature(rawBody, signature, "replace-with-shared-secret");
```

`WebhookHandler`: register a wildcard subscription and deliver one event with a signature header.

```csharp
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Integration;

var handler = new WebhookHandler(new HttpClient(), NullLogger<WebhookHandler>.Instance);

// "*" matches every event type
await handler.RegisterAsync(new WebhookHandlerSubscription
{
    Url = "https://hooks.example.com/events",
    EventType = "*",
    Headers = new Dictionary<string, string> { ["X-Api-Source"] = "sqlite-multi-tenant" }
});

var domainEvent = new TenantCreatedEvent
{
    TenantId = "acme",
    TenantName = "Acme Corp",
    ContactEmail = "ops@acme.example"
};

foreach (WebhookHandlerSubscription subscription in handler.GetSubscriptions(domainEvent.EventType))
{
    var delivery = new WebhookDelivery
    {
        WebhookId = subscription.WebhookId,
        Url = subscription.Url,
        Event = domainEvent,
        Headers = new Dictionary<string, string>(subscription.Headers),
        MaxRetries = 3
    };

    // Sign the same serialization that DeliverAsync sends, so WebhookDelivery.VerifySignature can check it
    string body = JsonSerializer.Serialize(delivery.Event);
    byte[] hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes("replace-with-shared-secret"), Encoding.UTF8.GetBytes(body));
    delivery.Headers["X-Signature"] = Convert.ToHexString(hash);

    await handler.DeliverAsync(delivery, CancellationToken.None);
    Console.WriteLine($"Delivered to {subscription.Url}, signature valid: {delivery.VerifySignature("replace-with-shared-secret")}");
}
```

## MultiTenantHttpClientFactory

`MultiTenantHttpClientFactory` (`src/Integration/MultiTenantHttpClientFactory.cs`) creates `HttpClient` instances that carry tenant context. Each client gets an `X-Tenant-Id` header, a User-Agent, a fixed `X-Request-Id` and `X-Timestamp`, and a Bearer `Authorization` header when an API key is given. Clients are cached by tenant ID, API key and base address, so repeating the same arguments returns the same instance.

Every non-empty `baseAddress` passes an SSRF check. The address must be HTTP or HTTPS, and loopback, link-local, private (RFC 1918, RFC 6598, RFC 4193), `0.0.0.0` and `::` addresses are rejected with `ArgumentException`. The exception is a host listed in `allowedHosts` or matching `allowedHostRegex`, which skips the address-range checks. The same validation applies to `TenantHttpClientBuilder` (in the same file), which builds an uncached client with a fluent API.

`HttpClientWrapper` (`src/Integration/HttpClientWrapper.cs`) wraps an `HttpClient` and implements `IHttpClientWrapper`. `GetAsync<T>` and `PostAsync<T>` deserialize the response body with case-insensitive property names. They return `null` on a non-success status or an exception. `PutAsync` and `DeleteAsync` return `true` only for a success status code. `maxRetries` (default 3) is the total number of attempts, not the number of extra retries, and a value of 0 sends no request. The delay starts at `retryDelayMs` (default 1000 ms) and doubles after each attempt. Retries happen on 5xx responses and on `HttpRequestException` with a `TimeoutException` inner exception. Requests that hit `HttpClient.Timeout` surface as `TaskCanceledException` and are not retried.

Known limitations to account for:

- `GetCachedClient(tenantId)` returns the first cached client that has an `X-Tenant-Id` header. It does not filter by tenant, so use `CreateClientForTenant` to get a specific tenant's client.
- `InvalidateClient(tenantId)` matches cache keys by prefix, so invalidating `"acme"` also removes the clients cached for `"acme-corp"`.

### Usage Example

`MultiTenantHttpClientFactory` and `TenantHttpClientBuilder`:

```csharp
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Integration;

var factory = new MultiTenantHttpClientFactory(
    NullLogger<MultiTenantHttpClientFactory>.Instance,
    defaultUserAgent: "MyApp/1.0",
    allowedHosts: new[] { "api.partner.example" });

// The same tenant, API key and base address return the cached instance
HttpClient client = factory.CreateClientForTenant(
    "acme",
    apiKey: "tenant-api-key",
    timeoutSeconds: 10,
    baseAddress: "https://api.partner.example/v2/");

HttpResponseMessage response = await client.GetAsync("customers");
Console.WriteLine($"Status: {response.StatusCode}");

// Private addresses are rejected unless the host is in allowedHosts
try
{
    factory.CreateClientForTenant("acme", baseAddress: "http://10.0.0.5/");
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
}

// The builder returns a new, uncached client each time
HttpClient builtClient = new TenantHttpClientBuilder()
    .ForTenant("acme")
    .WithApiKey("tenant-api-key")
    .WithBaseAddress("https://api.partner.example/v2/")
    .WithAllowedHosts(new[] { "api.partner.example" })
    .AddHeader("X-Client", "my-app")
    .Build();

// Disposes all cached clients
factory.Dispose();
```

`HttpClientWrapper`: retries, typed responses, and success flags.

```csharp
using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Integration;

var httpClient = new HttpClient { BaseAddress = new Uri("https://api.partner.example/v2/") };

// Up to 3 attempts in total, waiting 1 s and then 2 s between attempts
var wrapper = new HttpClientWrapper(
    httpClient,
    NullLogger<HttpClientWrapper>.Instance,
    maxRetries: 3,
    retryDelayMs: 1000);

wrapper.SetBearerToken("tenant-api-key");
wrapper.AddDefaultHeader("X-Tenant-Id", "acme");

// Null when the request fails or the body cannot be deserialized
Customer? customer = await wrapper.GetAsync<Customer>("customers/42");
Console.WriteLine(customer is null ? "Not found or failed" : $"Loaded {customer.Name}");

// The payload is serialized as JSON; the response is deserialized
Order? order = await wrapper.PostAsync<Order>("orders", new { CustomerId = 42, Total = 99.90m });

// PUT and DELETE report only success or failure
bool updated = await wrapper.PutAsync("customers/42", new { Name = "Acme Corp" });
bool deleted = await wrapper.DeleteAsync("customers/41");

public sealed record Customer(int Id, string Name);

public sealed record Order(int Id, decimal Total);
```

## Domain Events

The `src/Events/` folder contains two in-memory publishers and the event types they carry. Both work inside one process only; nothing is sent to other machines.

`EventBusImpl` (`src/Events/EventBusImpl.cs`) is a standalone bus. `Subscribe<TEvent>(handler, priority)` takes a `Func<TEvent, Task>` and returns an `IDisposable` that removes the handler when disposed. Higher priorities are placed first in the subscriber list. `PublishAsync` starts every handler for the event type, and they run concurrently; `Publish` blocks on `PublishAsync`. The bus keeps a publish history (`historySize`, default 1000, oldest entries evicted first), per-event-type counters from `GetEventStatistics()`, and a dead letter queue. The constructor takes an `ILogger<EventBusImpl>`, and it is not registered in the DI container, so create it yourself. It is `IDisposable`; `Dispose` removes all subscriptions and clears the history and dead letter queue.

`EventPublisher` (`src/Events/EventPublisher.cs`) implements `IEventPublisher` and is registered as a singleton by `AddEventServices` (`src/Configuration/DependencyInjectionSetup.cs`). Handlers implement `IEventHandler<T>` and are added with `Subscribe`. `PublishAsync(event, cancellationToken)` runs all handlers for the event's type with `Task.WhenAll`. A handler that throws is logged and does not stop the other handlers. `GetHandlerCount<T>()` returns the number of handlers registered for a type. Its handler list is a plain `Dictionary`, so subscribe during startup, before publishing from several threads. `EventPublisherOptions` is defined in the same file, but nothing reads it: no handler timeout is applied, and `ContinueOnHandlerException` and `EnableAsyncPublishing` have no effect.

All event types are in `src/Events/DomainEvent.cs`. They derive from `DomainEvent`, which provides `EventId` (a new GUID string), `OccurredAt` (UTC), `EventType` (the type name) and `TenantId`.

| Event | Meaning | Payload properties |
|-------|---------|--------------------|
| `TenantCreatedEvent` | A tenant was created | `TenantName`, `ContactEmail` |
| `TenantUpdatedEvent` | A tenant was updated | `OldName`, `NewName` |
| `TenantSuspendedEvent` | A tenant was suspended | `SuspendedBy`, `Reason` |
| `BackupStartedEvent` | A backup started | `BackupId`, `DatabaseId`, `BackupType` |
| `BackupCompletedEvent` | A backup completed | `BackupId`, `DatabaseId`, `SizeBytes`, `DurationMilliseconds` |
| `BackupFailedEvent` | A backup failed | `BackupId`, `DatabaseId`, `ErrorMessage` |
| `MigrationAppliedEvent` | A migration was applied | `MigrationId`, `DatabaseId`, `Version`, `MigrationName` |
| `MigrationFailedEvent` | A migration failed | `MigrationId`, `DatabaseId`, `Version`, `ErrorMessage` |
| `MigrationRolledBackEvent` | A migration was rolled back | `MigrationId`, `DatabaseId`, `Version`, `RolledBackBy` |
| `TenantClonedEvent` | A tenant was cloned from another tenant | `SourceTenantId`, `TargetTenantId`, `DatabasePath` |
| `HealthCheckFailedEvent` | A health check failed | `ComponentName`, `ErrorMessage`, `ResponseTimeMs` |
| `CustomDomainEvent` | Application-defined event | `Data` (`Dictionary<string, object>`) |

`CustomDomainEvent` takes its event name in the constructor. Every custom event has the same .NET type, so both buses route by type: a handler subscribed to `CustomDomainEvent` receives all custom events, and it must check `EventType` itself.

Known limitations to account for:

- `TenantCreatedEvent`, `TenantUpdatedEvent` and `TenantSuspendedEvent` declare their own `TenantId`, which hides the base property. `EventBusImpl` reads the base `DomainEvent.TenantId` to choose the tenant scope for handlers, so a value set through the derived property (for example in an object initializer) is ignored by the bus. Set it on the base property with `((DomainEvent)evt).TenantId = "acme"`. A handler that reads the derived property still sees the value set there, but the bus does not use it to choose the tenant scope.
- `EventBusImpl` catches handler exceptions and logs them; `PublishAsync` does not throw for them. Failures are counted per event type, not per handler, and any successful handler invocation of that type resets the count to 0. When the count goes above 3, the failing event is copied to the dead letter queue, which keeps at most 1000 entries per event type. There is no retry.
- `Publish` blocks the calling thread until all handlers finish. From async code, use `PublishAsync`.

### Usage Example

`EventBusImpl`: subscribe with a priority, publish with a tenant, and unsubscribe. `EventPublisher`: implement `IEventHandler<T>`, subscribe it, and publish.

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Events;

using var bus = new EventBusImpl(NullLogger<EventBusImpl>.Instance, historySize: 500);

// The returned IDisposable removes this handler when disposed
IDisposable subscription = bus.Subscribe<TenantCreatedEvent>(async evt =>
{
    Console.WriteLine($"Tenant created: {evt.TenantName} <{evt.ContactEmail}>");
    await Task.CompletedTask;
}, priority: 10);

var created = new TenantCreatedEvent
{
    TenantName = "Acme Corp",
    ContactEmail = "ops@acme.example"
};

// The bus reads the base DomainEvent.TenantId, so set it there (see limitations above)
((DomainEvent)created).TenantId = "acme";

await bus.PublishAsync(created);

// Statistics are kept per event type that has a subscriber
Dictionary<string, EventStatistics> stats = bus.GetEventStatistics();
Console.WriteLine($"Successful invocations: {stats[nameof(TenantCreatedEvent)].SuccessfulHandlerInvocations}");

subscription.Dispose();

var publisher = new EventPublisher(NullLogger<EventPublisher>.Instance);
publisher.Subscribe(new TenantSuspendedHandler());

// Subscribe before publishing; the handler list is not thread-safe
await publisher.PublishAsync(new TenantSuspendedEvent
{
    TenantId = "acme",
    SuspendedBy = "admin",
    Reason = "Payment overdue"
});

// Type declarations must follow the top-level statements
public sealed class TenantSuspendedHandler : IEventHandler<TenantSuspendedEvent>
{
    public Task HandleAsync(TenantSuspendedEvent @event, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Suspended {@event.TenantId} by {@event.SuspendedBy}: {@event.Reason}");
        return Task.CompletedTask;
    }
}
```

## Command Line Interface

The `src/Cli/` folder holds a small command-line front end. Four classes take part in a command run, and a fifth, `CommandLineParser`, is a separate builder-style parser that the front end does not use.

`CliApplication` (`src/Cli/CliApplication.cs`) is the host. Its constructor takes a `CommandParser`, a `CommandExecutor`, an `ILogger<CliApplication>` and an `IConsoleWriter`. `RunAsync(string[] args)` parses the arguments, executes the command, writes the result and returns an exit code: `0` on success and `1` for a failed command or an unhandled exception. The file also defines `IConsoleWriter` and `ConsoleWriter`, which prints success, error, warning and info lines in colour with `✓`, `✗`, `⚠` and `ℹ` prefixes.

`CommandParser` (`src/Cli/CommandParser.cs`) turns the arguments into a `ParsedCommand`. Its constructor takes an `ILogger<CommandParser>`. The command and subcommand names are case-insensitive. It validates the required arguments itself, and it returns help text or an error in place of a command when the arguments do not name a runnable subcommand. The command table is built in code (`RegisterDefaultCommands`), so there is no way to add commands from outside the class.

`CommandExecutor` (`src/Cli/CommandExecutor.cs`) runs a `ParsedCommand` through `ExecuteAsync(ParsedCommand, CancellationToken)`. Its constructor takes `ITenantService`, `IBackupService`, `IMigrationService`, `HealthCheckService`, `ConnectionManager`, an `ILogger<CommandExecutor>` and an `OutputFormatter`. `ExecuteAsync` does not throw: cancellation and other exceptions become a `CommandResult` with `Success = false`.

`CommandLineParser` (`src/Cli/CommandLineParser.cs`) is a general-purpose parser with registered commands, value options and flags. Its constructor takes the arguments as `params string[]`. `RegisterCommand` returns the parser for chaining, and `RegisterOption` and `RegisterFlag` attach an option or flag to the most recently registered command. `Parse()` returns a `LegacyParsedCommand` and does not call the registered handler, so the caller has to invoke it. `GetHelpText()` lists the registered commands. This parser has limitations to account for:

- `CliApplication` does not use it, and it registers no commands of its own.
- Long flags (`--name`) are never recognised, because the long-option branch only checks options. Use the short name for a flag.
- Values in the `--name=value` form are not supported. A value must be the next argument, and it cannot start with `-`.
- Flags and options attach to `_commands.Values.Last()`, which depends on dictionary enumeration order rather than registration order.

Nothing in the repository registers `CliApplication` or `CommandParser` in the DI container, and `src/Program.cs` does not call them, so the host program has to construct and call them itself.

### Supported Commands

These are the commands and subcommands that `CommandParser` accepts and `CommandExecutor` runs. Arguments in `<angle brackets>` are required and are checked by the parser. The `tenant create` description and email are optional, and the parser passes any extra arguments through to the executor.

| Command | Subcommand | Arguments | Behaviour |
|---------|------------|-----------|-----------|
| `tenant` | `create` | `<name>` `[description]` `[email]` | Calls `CreateTenantAsync` and prints the new tenant's details |
| `tenant` | `list` | none | Prints the tenant count, then each tenant's ID, name, status and creation time |
| `tenant` | `get` | `<tenantId>` | Prints the tenant's details, or fails with "not found" |
| `tenant` | `status` | `<tenantId>` | Prints `Tenant <tenantId>: <status>` |
| `tenant` | `delete` | `<tenantId>` | Calls `DeleteTenantAsync`. There is no confirmation prompt |
| `backup` | `create` | `<databaseId>` | Creates a full backup with the creator `cli` and prints its backup ID |
| `backup` | `list` | `<databaseId>` | Prints how many backups the database has |
| `backup` | `verify` | `<backupId>` | Calls `VerifyBackupAsync` and reports success |
| `backup` | `restore` | `<backupId>` `<targetPath>` | Placeholder: reports success without restoring anything |
| `backup` | `delete` | `<backupId>` | Placeholder: reports success without deleting anything |
| `migration` | `pending` | `<databaseId>` | Lists the pending migrations and their count |
| `migration` | `apply` | `<databaseId>` | Placeholder: reports success without applying anything |
| `migration` | `rollback` | `<databaseId>` | Placeholder: reports success without rolling anything back |
| `migration` | `history` | `<databaseId>` | Placeholder: prints a one-line message without history |
| `health` | `check` | none | Placeholder: reports completion without calling `HealthCheckService` |
| `health` | `status` | none | Placeholder: always reports `System is operational` |
| `explain` | `query` | `<sqlQuery>` | Runs `EXPLAIN QUERY PLAN` on the arguments joined with spaces, against the first tenant from `GetAllTenantsAsync` |

The help command is handled by the parser and needs no service. `help`, `-h`, `--help` and running with no arguments print the full command list. A command name without a subcommand, such as `backup`, prints the help for that command. An unknown command or subcommand, or a missing required argument, fails with exit code `1` and a message. The message for a missing argument lists the arguments that were expected.

### Usage Example

`CliApplication` is constructed by hand here, because nothing in the repository registers it. The services come from your container or from your own setup.

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Cli;

// The services below come from your DI container or are built by hand
CommandParser parser = new(NullLogger<CommandParser>.Instance);
CommandExecutor executor = new(
    tenantService, backupService, migrationService, healthService,
    connectionManager, NullLogger<CommandExecutor>.Instance, formatter);

var app = new CliApplication(parser, executor, NullLogger<CliApplication>.Instance, new ConsoleWriter());

// args are the process arguments from the top-level program
int exitCode = await app.RunAsync(args);
return exitCode;
```

With that host, the arguments are typed as ordinary shell words. Quote any value that contains spaces or characters the shell would expand, such as the `*` in the SQL query. The tenant ID is the one printed by `tenant create` or `tenant list`, so `acme` below stands for that value.

```bash
# Create a tenant with a description and a contact email
tenant create "Acme Corp" "Production tenant" ops@acme.example

# Look up a tenant and print only its status
tenant get acme
tenant status acme

# Create a full backup, then count the backups for the database
backup create acme
backup list acme

# Explain a query against the first tenant
explain query "SELECT * FROM Users WHERE Id = 1"

# Print the command list
help

# Print the help for one command
backup
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for prerequisites, build and test commands, coding conventions, and the pull request process.

## License

Released under the [MIT License](LICENSE). Copyright (c) 2026 Vladyslav Zaiets.
