#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Example 1: Basic Setup and Tenant Creation
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Services;

// Example: Basic setup with minimal configuration
class BasicSetupExample
{
    static async Task Main()
    {
        // Setup service collection
        var services = new ServiceCollection();

        // Add logging with console output
        services.AddLogging(builder =>
            builder.AddConsole()
                   .SetMinimumLevel(LogLevel.Information));

        // Configure SQLite Multi-Tenant with minimal options
        var masterDb = "Data Source=master.db;Version=3;";
        services.AddSqliteMultiTenant(masterDb, options =>
        {
            options.MaxConnections = 20;
            options.BackupRetentionDays = 30;
            options.DatabaseDirectory = "databases";
            options.BackupDirectory = "backups";
        });

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Create required directories
        Directory.CreateDirectory("databases");
        Directory.CreateDirectory("backups");

        // Get logger for output
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("=== SQLite Multi-Tenant - Basic Setup Example ===\n");

        try
        {
            // Get tenant service
            var tenantService = serviceProvider.GetRequiredService<ITenantService>();

            // Create first tenant
            logger.LogInformation("Creating first tenant...");
            var tenant1 = await tenantService.CreateTenantAsync(
                name: "Acme Corporation",
                description: "Our first customer",
                contactEmail: "admin@acme.com");

            logger.LogInformation($"✓ Created tenant: {tenant1.Name}");
            logger.LogInformation($"  Tenant ID: {tenant1.TenantId}");
            logger.LogInformation($"  Status: {tenant1.Status}");
            logger.LogInformation($"  Created: {tenant1.CreatedAt:O}\n");

            // Create second tenant
            logger.LogInformation("Creating second tenant...");
            var tenant2 = await tenantService.CreateTenantAsync(
                name: "TechVentures Inc",
                description: "Growing startup",
                contactEmail: "contact@techventures.com");

            logger.LogInformation($"✓ Created tenant: {tenant2.Name}");
            logger.LogInformation($"  Tenant ID: {tenant2.TenantId}\n");

            // Retrieve and display tenant
            logger.LogInformation("Retrieving tenant details...");
            var retrievedTenant = await tenantService.GetTenantAsync(tenant1.TenantId);
            if (retrievedTenant is not null)
            {
                logger.LogInformation($"✓ Retrieved: {retrievedTenant.Name}");
                logger.LogInformation($"  Email: {retrievedTenant.ContactEmail}");
                logger.LogInformation($"  Status: {retrievedTenant.Status}\n");
            }

            // List all tenants
            logger.LogInformation("Listing all tenants...");
            var allTenants = await tenantService.GetAllTenantsAsync();
            logger.LogInformation($"✓ Total tenants: {allTenants.Count}");
            foreach (var t in allTenants)
            {
                logger.LogInformation($"  - {t.Name} ({t.Status})");
            }

            logger.LogInformation("\n✓ Basic setup example completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
