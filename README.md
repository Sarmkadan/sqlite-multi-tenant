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
