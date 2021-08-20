#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Health;

namespace SqliteMultiTenant;

/// <summary>
/// Main program entry point for SQLite Multi-Tenant demonstration
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Check if running in web mode (for Docker health checks)
        var isWebMode = args.Contains("--web") || Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != null;

        if (isWebMode)
        {
            await RunWebApplicationAsync(args);
        }
        else
        {
            await RunConsoleApplicationAsync(args);
        }
    }

    static async Task RunConsoleApplicationAsync(string[] args)
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
            logger.LogError("Application error: {Message}", ex.Message);
            Environment.Exit(1);
        }
    }

    static async Task RunWebApplicationAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure logging
        builder.Logging.AddConsole();

        // Configure SQLite Multi-Tenant services
        string masterConnectionString = "Data Source=master.db;Version=3;";
        builder.Services.AddSqliteMultiTenant(masterConnectionString, options =>
        {
            options.MaxConnections = 20;
            options.ConnectionTimeoutSeconds = 30;
            options.BackupRetentionDays = 30;
            options.EnableEncryption = false;
            options.BackupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "backups");
            options.DatabaseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "databases");
            options.EnableLogging = true;
        });

        // Add health check services
        builder.Services.AddHealthCheckServices();

        var app = builder.Build();

        // Health check endpoint
        app.MapGet("/health", async (IHealthCheckService healthCheck) =>
        {
            var healthStatus = await healthCheck.GetHealthStatusAsync();
            return Results.Ok(healthStatus);
        });

        // API controllers
        app.MapControllers();

        app.Run();
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

        logger.LogInformation("✓ Tenant created: {TenantId}", tenant.TenantId);
        logger.LogInformation(" Name: {Name}", tenant.Name);
        logger.LogInformation(" Status: {Status}", tenant.Status);
        logger.LogInformation(" Created: {CreatedAt}\n", tenant.CreatedAt);

        // Retrieve the tenant
        logger.LogInformation("Retrieving tenant...");
        var retrievedTenant = await tenantService.GetTenantAsync(tenant.TenantId);
        if (retrievedTenant is not null)
        {
            logger.LogInformation("✓ Tenant retrieved: {Name}", retrievedTenant.Name);
            logger.LogInformation(" Last accessed: {LastAccessedAt}\n", retrievedTenant.LastAccessedAt);
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

        logger.LogInformation("✓ Database entry created: {DatabaseId}", tenantDb.DatabaseId);
        logger.LogInformation(" Tenant: {TenantId}", tenantDb.TenantId);
        logger.LogInformation(" Path: {FilePath}\n", tenantDb.FilePath);

        // Create migrations
        logger.LogInformation("Creating migrations...");
        var migration1 = await migrationService.CreateMigrationAsync(
            databaseId: tenantDb.DatabaseId,
            version: "001",
            name: "InitialSchema",
            upScript: "CREATE TABLE Users (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);",
            downScript: "DROP TABLE Users;");

        logger.LogInformation($"✓ Migration 1 created: {migration1.GetDisplayName()}");
        logger.LogInformation(" Status: {Status}", migration1.Status);
        logger.LogInformation(" Rollbackable: {IsRollbackable}\n", migration1.IsRollbackable);

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
        logger.LogInformation("✓ Found {Count} pending migrations:", pendingMigrations.Count);
        foreach (var mig in pendingMigrations)
        {
            logger.LogInformation($" - {mig.GetDisplayName()} (Order: {mig.ExecutionOrder})");
        }
        logger.LogInformation();

        // Create a backup
        logger.LogInformation("Creating backup...");
        var backup = await backupService.CreateBackupAsync(
            databaseId: tenantDb.DatabaseId,
            backupType: Constants.BackupType.Full,
            createdBy: "admin@acme.example.com",
            backupPath: null);

        logger.LogInformation("✓ Backup created: {BackupId}", backup.BackupId);
        logger.LogInformation(" Type: {BackupType}", backup.BackupType);
        logger.LogInformation(" Status: {Status}", backup.Status);
        logger.LogInformation(" Path: {BackupPath}", backup.BackupPath);
        logger.LogInformation(" Expires: {ExpiresAt}\n", backup.ExpiresAt);

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
        if (backupDetails is not null)
        {
            logger.LogInformation($"✓ Backup details retrieved:");
            logger.LogInformation(" Status: {Status}", backupDetails.Status);
            logger.LogInformation(" Size: {SizeBytes} bytes", backupDetails.SizeBytes);
            logger.LogInformation(" Verified: {IsVerified}", backupDetails.IsVerified);
            logger.LogInformation($" Tags: {string.Join(", ", backupDetails.GetTags())}\n");
        }

        // Get all tenants
        logger.LogInformation("Retrieving all tenants...");
        var allTenants = await tenantService.GetAllTenantsAsync();
        logger.LogInformation("✓ Total tenants: {Count}\n", allTenants.Count);

        // Get backup count
        logger.LogInformation("Getting backup count...");
        var backupCount = await backupService.GetBackupCountAsync(tenantDb.DatabaseId);
        logger.LogInformation("✓ Backups for this database: {BackupCount}\n", backupCount);

        // Summary
        logger.LogInformation("=== Demonstration Complete ===");
        logger.LogInformation("Tenant ID: {TenantId}", tenant.TenantId);
        logger.LogInformation("Database ID: {DatabaseId}", tenantDb.DatabaseId);
        logger.LogInformation("Backup ID: {BackupId}", backup.BackupId);
        logger.LogInformation("\nAll multi-tenant operations completed successfully!");
    }
}
