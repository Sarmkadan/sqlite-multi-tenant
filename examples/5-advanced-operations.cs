#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Example 5: Advanced Operations - Batch Processing, Metadata, Search
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;

// Example: Advanced multi-tenant operations
class AdvancedOperationsExample
{
    static async Task Main()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

        var masterDb = "Data Source=master.db;Version=3;";
        services.AddSqliteMultiTenant(masterDb, options =>
        {
            options.MaxConnections = 50;
            options.DatabaseDirectory = "databases";
            options.BackupDirectory = "backups";
            options.EnableCaching = true;
            options.CacheExpirationMinutes = 30;
        });

        var serviceProvider = services.BuildServiceProvider();
        Directory.CreateDirectory("databases");
        Directory.CreateDirectory("backups");

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var tenantService = serviceProvider.GetRequiredService<ITenantService>();
        var migrationService = serviceProvider.GetRequiredService<IMigrationService>();
        var backupService = serviceProvider.GetRequiredService<IBackupService>();

        logger.LogInformation("=== Advanced Operations Example ===\n");

        // Part 1: Batch Tenant Creation
        logger.LogInformation("Part 1: Batch Tenant Creation");
        logger.LogInformation("  Creating multiple tenants in parallel...");

        var tenantNames = new[]
        {
            "TechStart Inc",
            "Finance Plus",
            "Retail Solutions",
            "Healthcare Pro",
            "Manufacturing Co"
        };

        var createdTenants = new List<Tenant>();
        var tenantTasks = tenantNames.Select(async name =>
        {
            try
            {
                var tenant = await tenantService.CreateTenantAsync(
                    name: name,
                    description: $"Batch created tenant: {name}",
                    contactEmail: $"admin@{name.ToLower().Replace(" ", "-")}.com");
                return tenant;
            }
            catch (Exception ex)
            {
                logger.LogError($"  Failed to create {name}: {ex.Message}");
                return null;
            }
        });

        var tenantResults = await Task.WhenAll(tenantTasks);
        createdTenants = tenantResults.Where(t => t is not null).ToList();

        logger.LogInformation($"  ✓ Created {createdTenants.Count} tenants\n");

        // Part 2: Metadata Management
        logger.LogInformation("Part 2: Tenant Metadata Management");

        foreach (var tenant in createdTenants)
        {
            // Set various metadata
            await tenantService.SetTenantMetadataAsync(
                tenant.TenantId, "subscription_plan", "enterprise");
            await tenantService.SetTenantMetadataAsync(
                tenant.TenantId, "region", "us-east-1");
            await tenantService.SetTenantMetadataAsync(
                tenant.TenantId, "industry", DetermineIndustry(tenant.Name));
            await tenantService.SetTenantMetadataAsync(
                tenant.TenantId, "employee_count", "500");

            logger.LogInformation($"  ✓ Set metadata for: {tenant.Name}");
        }

        logger.LogInformation();

        // Part 3: Search Operations
        logger.LogInformation("Part 3: Search Operations");

        // Search for specific tenant
        var searchTerm = "Tech";
        var searchResults = await tenantService.SearchTenantsAsync(searchTerm);
        logger.LogInformation($"  Search for '{searchTerm}': {searchResults.Count} results");
        foreach (var result in searchResults)
        {
            logger.LogInformation($"    - {result.Name}");
        }

        logger.LogInformation();

        // Part 4: Status Management
        logger.LogInformation("Part 4: Tenant Status Management");

        if (createdTenants.Count > 0)
        {
            var testTenant = createdTenants[0];

            // Activate
            logger.LogInformation($"  Activating: {testTenant.Name}");
            await tenantService.ActivateTenantAsync(testTenant.TenantId);
            var activated = await tenantService.GetTenantAsync(testTenant.TenantId);
            logger.LogInformation($"    Status: {activated.Status}");

            // Suspend
            logger.LogInformation($"  Suspending: {testTenant.Name}");
            await tenantService.SuspendTenantAsync(testTenant.TenantId);
            var suspended = await tenantService.GetTenantAsync(testTenant.TenantId);
            logger.LogInformation($"    Status: {suspended.Status}");

            // Reactivate
            logger.LogInformation($"  Reactivating: {testTenant.Name}");
            await tenantService.ActivateTenantAsync(testTenant.TenantId);
            var reactivated = await tenantService.GetTenantAsync(testTenant.TenantId);
            logger.LogInformation($"    Status: {reactivated.Status}");

            logger.LogInformation();
        }

        // Part 5: Create Databases with Migrations
        logger.LogInformation("Part 5: Multi-Database Setup per Tenant");

        foreach (var tenant in createdTenants.Take(2))
        {
            var databaseId = Guid.NewGuid().ToString();
            var db = new TenantDatabase
            {
                DatabaseId = databaseId,
                TenantId = tenant.TenantId,
                Name = "primary",
                FilePath = Path.Combine("databases", $"{tenant.TenantId}_primary.db"),
                CreatedAt = DateTime.UtcNow
            };

            logger.LogInformation($"  Created database for {tenant.Name}: {databaseId}");

            // Create initial migration
            var migration = await migrationService.CreateMigrationAsync(
                databaseId: databaseId,
                version: "001",
                name: "InitialSchema",
                upScript: @"CREATE TABLE Customers (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Email TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );",
                downScript: "DROP TABLE Customers;");

            logger.LogInformation($"    Created migration: {migration.Version}");
        }

        logger.LogInformation();

        // Part 6: Statistics and Reporting
        logger.LogInformation("Part 6: Statistics and Reporting");

        var allTenants = await tenantService.GetAllTenantsAsync();
        logger.LogInformation($"  Total Tenants: {allTenants.Count}");

        // Group by status
        var byStatus = allTenants
            .GroupBy(t => t.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        logger.LogInformation("  Tenants by Status:");
        foreach (var kvp in byStatus)
        {
            logger.LogInformation($"    {kvp.Key}: {kvp.Value}");
        }

        // Analyze created dates
        var oldestTenant = allTenants.OrderBy(t => t.CreatedAt).FirstOrDefault();
        var newestTenant = allTenants.OrderByDescending(t => t.CreatedAt).FirstOrDefault();

        if (oldestTenant is not null)
        {
            logger.LogInformation($"  Oldest Tenant: {oldestTenant.Name} ({oldestTenant.CreatedAt:O})");
        }

        if (newestTenant is not null)
        {
            logger.LogInformation($"  Newest Tenant: {newestTenant.Name} ({newestTenant.CreatedAt:O})");
        }

        logger.LogInformation();

        // Part 7: Bulk Backup Operations
        logger.LogInformation("Part 7: Bulk Backup Operations");

        int backupCount = 0;
        foreach (var tenant in createdTenants)
        {
            var databaseId = tenant.TenantId;

            try
            {
                var backup = await backupService.CreateBackupAsync(
                    databaseId: databaseId,
                    backupType: Constants.BackupType.Full,
                    createdBy: "system");

                await backupService.MarkBackupAsCompletedAsync(
                    backup.BackupId,
                    sizeBytes: 512000,
                    durationMs: 1500);

                await backupService.VerifyBackupAsync(backup.BackupId, "system");

                backupCount++;
                logger.LogInformation($"  ✓ Backed up: {tenant.Name}");
            }
            catch (Exception ex)
            {
                logger.LogError($"  ✗ Backup failed for {tenant.Name}: {ex.Message}");
            }
        }

        logger.LogInformation($"  Total Backups Created: {backupCount}\n");

        // Summary
        logger.LogInformation("✓ Advanced operations example completed!");
        logger.LogInformation($"\nSummary:");
        logger.LogInformation($"  Tenants Created: {createdTenants.Count}");
        logger.LogInformation($"  Total Tenants (including existing): {allTenants.Count}");
        logger.LogInformation($"  Backups Created: {backupCount}");
        logger.LogInformation($"  Caching: Enabled");
        logger.LogInformation($"  Ready for multi-tenant operations!");
    }

    static string DetermineIndustry(string tenantName)
    {
        return tenantName.ToLower() switch
        {
            string n when n.Contains("tech") => "Technology",
            string n when n.Contains("finance") => "Finance",
            string n when n.Contains("retail") => "Retail",
            string n when n.Contains("health") => "Healthcare",
            string n when n.Contains("manufacturing") => "Manufacturing",
            _ => "Other"
        };
    }
}
