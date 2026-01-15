#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Example 3: Backup Creation and Management
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Constants;

// Example: Complete backup workflow
class BackupRestoreExample
{
    static async Task Main()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

        var masterDb = "Data Source=master.db;Version=3;";
        services.AddSqliteMultiTenant(masterDb, options =>
        {
            options.MaxConnections = 20;
            options.DatabaseDirectory = "databases";
            options.BackupDirectory = "backups";
            options.BackupRetentionDays = 30;
        });

        var serviceProvider = services.BuildServiceProvider();
        Directory.CreateDirectory("databases");
        Directory.CreateDirectory("backups");

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var tenantService = serviceProvider.GetRequiredService<ITenantService>();
        var backupService = serviceProvider.GetRequiredService<IBackupService>();

        logger.LogInformation("=== Backup and Restore Example ===\n");

        try
        {
            // Create tenant
            logger.LogInformation("Step 1: Creating tenant...");
            var tenant = await tenantService.CreateTenantAsync(
                name: "BackupTest Corp",
                description: "Testing backups",
                contactEmail: "admin@backup-test.com");
            logger.LogInformation($"✓ Tenant: {tenant.TenantId}\n");

            // Create database entry
            var databaseId = Guid.NewGuid().ToString();
            var tenantDb = new TenantDatabase
            {
                DatabaseId = databaseId,
                TenantId = tenant.TenantId,
                Name = "primary",
                FilePath = Path.Combine("databases", $"{tenant.TenantId}.db"),
                CreatedAt = DateTime.UtcNow
            };

            // Create multiple backups of different types
            logger.LogInformation("Step 2: Creating backups...");

            var backups = new List<string>();

            // Full backup
            logger.LogInformation("  Creating full backup...");
            var fullBackup = await backupService.CreateBackupAsync(
                databaseId: databaseId,
                backupType: BackupType.Full,
                createdBy: "admin");
            await backupService.MarkBackupAsCompletedAsync(
                fullBackup.BackupId,
                sizeBytes: 1024000,
                durationMs: 2500);
            backups.Add(fullBackup.BackupId);
            logger.LogInformation($"    ✓ Full: {fullBackup.BackupId}");

            // Incremental backup
            logger.LogInformation("  Creating incremental backup...");
            var incrBackup = await backupService.CreateBackupAsync(
                databaseId: databaseId,
                backupType: BackupType.Incremental,
                createdBy: "admin");
            await backupService.MarkBackupAsCompletedAsync(
                incrBackup.BackupId,
                sizeBytes: 204800,
                durationMs: 800);
            backups.Add(incrBackup.BackupId);
            logger.LogInformation($"    ✓ Incremental: {incrBackup.BackupId}");

            // Differential backup
            logger.LogInformation("  Creating differential backup...");
            var diffBackup = await backupService.CreateBackupAsync(
                databaseId: databaseId,
                backupType: BackupType.Differential,
                createdBy: "admin");
            await backupService.MarkBackupAsCompletedAsync(
                diffBackup.BackupId,
                sizeBytes: 512000,
                durationMs: 1500);
            backups.Add(diffBackup.BackupId);
            logger.LogInformation($"    ✓ Differential: {diffBackup.BackupId}\n");

            // Add tags to backups
            logger.LogInformation("Step 3: Adding backup tags...");
            foreach (var backupId in backups)
            {
                await backupService.AddBackupTagAsync(backupId, "production");
                await backupService.AddBackupTagAsync(backupId, "daily");
                logger.LogInformation($"  ✓ Tagged: {backupId}");
            }
            logger.LogInformation();

            // Verify backups
            logger.LogInformation("Step 4: Verifying backups...");
            foreach (var backupId in backups)
            {
                await backupService.VerifyBackupAsync(backupId, "admin");
                var backup = await backupService.GetBackupAsync(backupId);
                logger.LogInformation($"  ✓ Verified: {backupId}");
                logger.LogInformation($"    Status: {backup.Status}, IsVerified: {backup.IsVerified}");
            }
            logger.LogInformation();

            // List all backups
            logger.LogInformation("Step 5: Listing all backups...");
            var allBackups = await backupService.GetDatabaseBackupsAsync(
                databaseId: databaseId,
                pageSize: 50);
            logger.LogInformation($"✓ Total backups: {allBackups.Count}");
            foreach (var b in allBackups.OrderByDescending(x => x.CreatedAt))
            {
                var tags = await backupService.GetBackupTagsAsync(b.BackupId);
                logger.LogInformation($"  - {b.BackupType}");
                logger.LogInformation($"    ID: {b.BackupId}");
                logger.LogInformation($"    Size: {b.SizeBytes / 1024.0:F2} KB");
                logger.LogInformation($"    Created: {b.CreatedAt:O}");
                logger.LogInformation($"    Status: {b.Status}, Verified: {b.IsVerified}");
                logger.LogInformation($"    Tags: {string.Join(", ", tags)}");
            }
            logger.LogInformation();

            // Get completed backups only
            logger.LogInformation("Step 6: Getting completed backups...");
            var completedBackups = await backupService.GetCompletedBackupsAsync(databaseId);
            logger.LogInformation($"✓ Completed backups: {completedBackups.Count}\n");

            // Get backup count
            logger.LogInformation("Step 7: Getting backup statistics...");
            var backupCount = await backupService.GetBackupCountAsync(databaseId);
            logger.LogInformation($"✓ Total backups for database: {backupCount}");

            // Calculate total backup size
            var totalSize = allBackups.Sum(b => b.SizeBytes);
            var totalSizeMB = totalSize / (1024.0 * 1024.0);
            logger.LogInformation($"✓ Total backup size: {totalSizeMB:F2} MB");

            // Set backup expiration
            logger.LogInformation("Step 8: Setting backup expiration...");
            var oldestBackup = allBackups.OrderBy(x => x.CreatedAt).First();
            var expirationDate = DateTime.UtcNow.AddDays(7);
            await backupService.SetBackupExpirationAsync(
                backupId: oldestBackup.BackupId,
                expiresAt: expirationDate);
            logger.LogInformation($"✓ Set expiration for {oldestBackup.BackupId}");
            logger.LogInformation($"  Expires: {expirationDate:O}\n");

            logger.LogInformation("✓ Backup example completed successfully!");
            logger.LogInformation("\nSummary:");
            logger.LogInformation($"  Database: {databaseId}");
            logger.LogInformation($"  Total Backups: {backupCount}");
            logger.LogInformation($"  Backup Types: Full, Incremental, Differential");
            logger.LogInformation($"  All Verified: {allBackups.All(b => b.IsVerified)}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
