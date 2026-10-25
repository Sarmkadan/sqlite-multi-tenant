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

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for prerequisites, build and test commands, coding conventions, and the pull request process.

## License

Released under the [MIT License](LICENSE). Copyright (c) 2026 Vladyslav Zaiets.
