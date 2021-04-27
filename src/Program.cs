// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Configuration;

namespace SqliteMultiTenant;

/// <summary>
/// Main program entry point for SQLite Multi-Tenant demonstration
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(builder =>
            builder.AddConsole()
                   .SetMinimumLevel(LogLevel.Information));

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

        try
        {
            await RunDemonstration(serviceProvider);
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError($"Application error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static async Task RunDemonstration(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("=== SQLite Multi-Tenant Database Manager ===");
        logger.LogInformation("Starting demonstration...\n");

        // Get services
        var tenantService = serviceProvider.GetRequiredService<Services.ITenantService>();
        var migrationService = serviceProvider.GetRequiredService<Services.IMigrationService>();
        var backupService = serviceProvider.GetRequiredService<Services.IBackupService>();

        // Create a tenant
        logger.LogInformation("Creating tenant...");
        var tenant = await tenantService.CreateTenantAsync(
            name: "Acme Corporation",
            description: "Main tenant for demonstration",
            contactEmail: "admin@acme.example.com");

        logger.LogInformation($"✓ Tenant created: {tenant.TenantId}");
        logger.LogInformation($"  Name: {tenant.Name}");
        logger.LogInformation($"  Status: {tenant.Status}");
        logger.LogInformation($"  Created: {tenant.CreatedAt:O}\n");

        // Retrieve the tenant
        logger.LogInformation("Retrieving tenant...");
        var retrievedTenant = await tenantService.GetTenantAsync(tenant.TenantId);
        if (retrievedTenant != null)
        {
            logger.LogInformation($"✓ Tenant retrieved: {retrievedTenant.Name}");
            logger.LogInformation($"  Last accessed: {retrievedTenant.LastAccessedAt:O}\n");
        }

        // Create a database entry for the tenant
        logger.LogInformation("Creating tenant database entry...");
        var tenantDb = new Models.TenantDatabase
        {
            DatabaseId = Guid.NewGuid().ToString(),
            TenantId = tenant.TenantId,
            Name = "primary_db",
            FilePath = Path.Combine("databases", $"{tenant.TenantId}_primary.db"),
            SizeBytes = 0,
            SchemaVersion = 1,
            IsReadOnly = false
        };

        logger.LogInformation($"✓ Database entry created: {tenantDb.DatabaseId}");
        logger.LogInformation($"  Tenant: {tenantDb.TenantId}");
        logger.LogInformation($"  Path: {tenantDb.FilePath}\n");

        // Create migrations
        logger.LogInformation("Creating migrations...");
        var migration1 = await migrationService.CreateMigrationAsync(
            databaseId: tenantDb.DatabaseId,
            version: "001",
            name: "InitialSchema",
            upScript: "CREATE TABLE Users (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);",
            downScript: "DROP TABLE Users;");

        logger.LogInformation($"✓ Migration 1 created: {migration1.GetDisplayName()}");
        logger.LogInformation($"  Status: {migration1.Status}");
        logger.LogInformation($"  Rollbackable: {migration1.IsRollbackable}\n");

        var migration2 = await migrationService.CreateMigrationAsync(
            databaseId: tenantDb.DatabaseId,
            version: "002",
            name: "AddEmailColumn",
            upScript: "ALTER TABLE Users ADD COLUMN Email TEXT;",
            downScript: "ALTER TABLE Users DROP COLUMN Email;");

        logger.LogInformation($"✓ Migration 2 created: {migration2.GetDisplayName()}\n");

        // Get pending migrations
        logger.LogInformation("Retrieving pending migrations...");
        var pendingMigrations = await migrationService.GetPendingMigrationsAsync(tenantDb.DatabaseId);
        logger.LogInformation($"✓ Found {pendingMigrations.Count} pending migrations:");
        foreach (var mig in pendingMigrations)
        {
            logger.LogInformation($"  - {mig.GetDisplayName()} (Order: {mig.ExecutionOrder})");
        }
        logger.LogInformation();

        // Create a backup
        logger.LogInformation("Creating backup...");
        var backup = await backupService.CreateBackupAsync(
            databaseId: tenantDb.DatabaseId,
            backupType: Constants.BackupType.Full,
            createdBy: "admin@acme.example.com",
            backupPath: null);

        logger.LogInformation($"✓ Backup created: {backup.BackupId}");
        logger.LogInformation($"  Type: {backup.BackupType}");
        logger.LogInformation($"  Status: {backup.Status}");
        logger.LogInformation($"  Path: {backup.BackupPath}");
        logger.LogInformation($"  Expires: {backup.ExpiresAt:O}\n");

        // Complete the backup
        logger.LogInformation("Completing backup...");
        await backupService.MarkBackupAsCompletedAsync(backup.BackupId, sizeBytes: 1024000, durationMs: 2500);
        logger.LogInformation($"✓ Backup completed\n");

        // Verify the backup
        logger.LogInformation("Verifying backup...");
        await backupService.VerifyBackupAsync(backup.BackupId, "admin@acme.example.com");
        logger.LogInformation($"✓ Backup verified\n");

        // Add tag to backup
        logger.LogInformation("Adding tag to backup...");
        await backupService.AddBackupTagAsync(backup.BackupId, "production");
        logger.LogInformation($"✓ Tag added: production\n");

        // Get backup details
        logger.LogInformation("Retrieving backup details...");
        var backupDetails = await backupService.GetBackupAsync(backup.BackupId);
        if (backupDetails != null)
        {
            logger.LogInformation($"✓ Backup details retrieved:");
            logger.LogInformation($"  Status: {backupDetails.Status}");
            logger.LogInformation($"  Size: {backupDetails.SizeBytes} bytes");
            logger.LogInformation($"  Verified: {backupDetails.IsVerified}");
            logger.LogInformation($"  Tags: {string.Join(", ", backupDetails.GetTags())}\n");
        }

        // Get all tenants
        logger.LogInformation("Retrieving all tenants...");
        var allTenants = await tenantService.GetAllTenantsAsync();
        logger.LogInformation($"✓ Total tenants: {allTenants.Count}\n");

        // Get backup count
        logger.LogInformation("Getting backup count...");
        var backupCount = await backupService.GetBackupCountAsync(tenantDb.DatabaseId);
        logger.LogInformation($"✓ Backups for this database: {backupCount}\n");

        // Summary
        logger.LogInformation("=== Demonstration Complete ===");
        logger.LogInformation($"Tenant ID: {tenant.TenantId}");
        logger.LogInformation($"Database ID: {tenantDb.DatabaseId}");
        logger.LogInformation($"Backup ID: {backup.BackupId}");
        logger.LogInformation("\nAll multi-tenant operations completed successfully!");
    }
}
