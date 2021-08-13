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

            logger.LogInformation("✓ Created tenant: {Name}", tenant1.Name);
            logger.LogInformation("  Tenant ID: {TenantId}", tenant1.TenantId);
            logger.LogInformation("  Status: {Status}", tenant1.Status);
            logger.LogInformation("  Created: {CreatedAt}\n", tenant1.CreatedAt);

            // Create second tenant
            logger.LogInformation("Creating second tenant...");
            var tenant2 = await tenantService.CreateTenantAsync(
                name: "TechVentures Inc",
                description: "Growing startup",
                contactEmail: "contact@techventures.com");

            logger.LogInformation("✓ Created tenant: {Name}", tenant2.Name);
            logger.LogInformation("  Tenant ID: {TenantId}\n", tenant2.TenantId);

            // Retrieve and display tenant
            logger.LogInformation("Retrieving tenant details...");
            var retrievedTenant = await tenantService.GetTenantAsync(tenant1.TenantId);
            if (retrievedTenant is not null)
            {
                logger.LogInformation("✓ Retrieved: {Name}", retrievedTenant.Name);
                logger.LogInformation("  Email: {ContactEmail}", retrievedTenant.ContactEmail);
                logger.LogInformation("  Status: {Status}\n", retrievedTenant.Status);
            }

            // List all tenants
            logger.LogInformation("Listing all tenants...");
            var allTenants = await tenantService.GetAllTenantsAsync();
            logger.LogInformation("✓ Total tenants: {Count}", allTenants.Count);
            foreach (var t in allTenants)
            {
                logger.LogInformation("  - {Name} ({Status})", t.Name, t.Status);
            }

            logger.LogInformation("\n✓ Basic setup example completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError("Error: {Message}", ex.Message);
            Environment.Exit(1);
        }
    }
}
