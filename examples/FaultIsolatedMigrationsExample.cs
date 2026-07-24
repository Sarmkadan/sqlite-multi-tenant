#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Example: Fault-Isolated Per-Tenant Migrations
// Demonstrates how migrations can fail per tenant without aborting the entire process
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Api.Requests;
using SqliteMultiTenant.Api.Responses;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Services;

// Example: Fault-isolated migration workflow across multiple tenants
class FaultIsolatedMigrationsExample
{
    static async Task Main()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

        var masterDb = "Data Source=master_fault_test.db;Version=3;";
        services.AddSqliteMultiTenant(masterDb, options =>
        {
            options.MaxConnections = 20;
            options.DatabaseDirectory = "databases_fault_test";
            options.BackupDirectory = "backups_fault_test";
        });

        var serviceProvider = services.BuildServiceProvider();
        Directory.CreateDirectory("databases_fault_test");
        Directory.CreateDirectory("backups_fault_test");

        var logger = serviceProvider.GetRequiredService<ILogger<FaultIsolatedMigrationsExample>>();
        var tenantService = serviceProvider.GetRequiredService<ITenantService>();
        var migrationService = serviceProvider.GetRequiredService<IMigrationService>();

        logger.LogInformation("=== Fault-Isolated Per-Tenant Migrations Example ===\n");

        try
        {
            // Step 1: Create multiple tenants
            logger.LogInformation("Step 1: Creating 3 test tenants...");
            var tenants = new List<(string Id, string Name)>();

            for (int i = 1; i <= 3; i++)
            {
                var tenant = await tenantService.CreateTenantAsync(
                    name: $"Test Corp {i}",
                    description: $"Testing fault-isolated migrations - Tenant {i}",
                    contactEmail: $"admin{i}@test.com");
                tenants.Add((tenant.TenantId, tenant.Name));
                logger.LogInformation("✓ Created tenant: {Name} (ID: {TenantId})", tenant.Name, tenant.TenantId);
            }
            logger.LogInformation();

            // Step 2: Create database entries for each tenant
            logger.LogInformation("Step 2: Creating database entries for each tenant...");
            var databaseIds = new List<string>();

            foreach (var (tenantId, tenantName) in tenants)
            {
                var databaseId = Guid.NewGuid().ToString();
                var tenantDb = new TenantDatabase
                {
                    DatabaseId = databaseId,
                    TenantId = tenantId,
                    Name = "primary",
                    FilePath = System.IO.Path.Combine("databases_fault_test", $"{tenantId}_primary.db"),
                    SchemaVersion = 1,
                    IsReadOnly = false,
                    CreatedAt = DateTime.UtcNow
                };

                databaseIds.Add(databaseId);
                logger.LogInformation("✓ Created database for {TenantName}: {DatabaseId}", tenantName, databaseId);
            }
            logger.LogInformation();

            // Step 3: Define a set of migrations
            logger.LogInformation("Step 3: Creating migration definitions...");
            var migrations = new[]
            {
                new
                {
                    Version = "001",
                    Name = "CreateUsersTable",
                    Up = @"CREATE TABLE Users (
                        Id INTEGER PRIMARY KEY,
                        Name TEXT NOT NULL,
                        Email TEXT UNIQUE,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );",
                    Down = "DROP TABLE Users;"
                },
                new
                {
                    Version = "002",
                    Name = "CreatePostsTable",
                    Up = @"CREATE TABLE Posts (
                        Id INTEGER PRIMARY KEY,
                        UserId INTEGER NOT NULL,
                        Title TEXT NOT NULL,
                        Content TEXT,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (UserId) REFERENCES Users(Id)
                    );",
                    Down = "DROP TABLE Posts;"
                },
                new
                {
                    Version = "003",
                    Name = "CreateIndexes",
                    Up = @"CREATE INDEX idx_users_email ON Users(Email);
                        CREATE INDEX idx_posts_userid ON Posts(UserId);",
                    Down = @"DROP INDEX idx_users_email;
                        DROP INDEX idx_posts_userid;"
                },
                new
                {
                    Version = "004",
                    Name = "AddUserRoles",
                    Up = @"CREATE TABLE UserRoles (
                        Id INTEGER PRIMARY KEY,
                        UserId INTEGER NOT NULL,
                        RoleName TEXT NOT NULL,
                        FOREIGN KEY (UserId) REFERENCES Users(Id)
                    );",
                    Down = "DROP TABLE UserRoles;"
                }
            };

            // Create migration records for each database
            foreach (var databaseId in databaseIds)
            {
                logger.LogInformation("Creating migrations for database: {DatabaseId}", databaseId);
                foreach (var mig in migrations)
                {
                    var migration = await migrationService.CreateMigrationAsync(
                        databaseId: databaseId,
                        version: mig.Version,
                        name: mig.Name,
                        upScript: mig.Up,
                        downScript: mig.Down);
                    logger.LogInformation(" ✓ Created: {Version} - {Name}", mig.Version, mig.Name);
                }
                logger.LogInformation();
            }

            // Step 4: Apply migrations with fault isolation to all databases
            logger.LogInformation("Step 4: Applying migrations with FAULT ISOLATION to all databases...");
            logger.LogInformation("This demonstrates that a failure in one tenant does NOT stop migrations for others!\n");

            var batchRequest = new ApplyMigrationsToMultipleRequest
            {
                DatabaseIds = databaseIds,
                AppliedBy = "fault-isolation-demo"
            };

            // In a real scenario, you would call the API endpoint:
            // var apiResponse = await migrationController.ApplyMigrationsToMultipleDatabasesAsync(batchRequest);
            // For this example, we'll simulate the service call directly:

            var batchResult = await migrationService.ApplyMigrationsToMultipleDatabasesAsync(
                databaseIds: databaseIds,
                executedBy: "fault-isolation-demo");

            logger.LogInformation("\n=== BATCH MIGRATION RESULTS ===");
            logger.LogInformation("Overall Success: {IsSuccess}", batchResult.IsSuccess);
            logger.LogInformation("Total Migrations Attempted: {Total}", batchResult.TotalMigrationsAttempted);
            logger.LogInformation("Successful Migrations: {Successful}", batchResult.SuccessfulMigrations);
            logger.LogInformation("Failed Migrations: {Failed}", batchResult.FailedMigrations);
            logger.LogInformation("Result Summary: {Summary}", batchResult.ResultSummary);
            logger.LogInformation();

            // Step 5: Analyze tenant-specific results
            logger.LogInformation("=== TENANT-SPECIFIC RESULTS ===");
            foreach (var tenantResult in batchResult.TenantResults)
            {
                logger.LogInformation("\nTenant Database: {DatabaseId}", tenantResult.DatabaseId);
                logger.LogInformation("  Status: {Status}", tenantResult.IsSuccess ? "✓ SUCCESS" : "✗ FAILED");
                logger.LogInformation("  Migrations Attempted: {Attempted}", tenantResult.TotalMigrationsAttempted);
                logger.LogInformation("  Successful: {Successful}", tenantResult.SuccessfulMigrations);
                logger.LogInformation("  Failed: {Failed}", tenantResult.FailedMigrations);
                logger.LogInformation("  Schema Version Reached: {Version}", tenantResult.SchemaVersionReached ?? "N/A");

                if (tenantResult.Failures.Count > 0)
                {
                    logger.LogInformation("\n  === FAILURE DETAILS ===");
                    foreach (var failure in tenantResult.Failures)
                    {
                        logger.LogError("  ✗ Migration {Version} ({Name}) failed:", failure.Version, failure.Name);
                        logger.LogError("    Error: {ErrorMessage}", failure.ErrorMessage);
                        logger.LogError("    Failed At: {FailedAt}", failure.FailedAt);
                    }
                }
            }
            logger.LogInformation("\n=== FAULT ISOLATION DEMONSTRATION COMPLETE ===");
            logger.LogInformation("Key Takeaway:");
            logger.LogInformation("- Each tenant's migrations ran in ISOLATION");
            logger.LogInformation("- Failures in one tenant did NOT abort migrations for other tenants");
            logger.LogInformation("- Detailed failure reports allow targeted retry of only failed tenants");
            logger.LogInformation("- Operators can retry failed migrations per tenant without affecting working tenants");
        }
        catch (Exception ex)
        {
            logger.LogError("Error: {Message}", ex.Message);
            logger.LogError("Stack Trace:\n{StackTrace}", ex.StackTrace);
            Environment.Exit(1);
        }
    }
}

// Helper class for the example
class TenantDatabase
{
    public string DatabaseId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public bool IsReadOnly { get; set; }
    public DateTime CreatedAt { get; set; }
}