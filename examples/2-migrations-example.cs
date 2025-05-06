// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Example 2: Database Migrations Management
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;

// Example: Complete migration workflow
class MigrationsExample
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
        });

        var serviceProvider = services.BuildServiceProvider();
        Directory.CreateDirectory("databases");
        Directory.CreateDirectory("backups");

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var tenantService = serviceProvider.GetRequiredService<ITenantService>();
        var migrationService = serviceProvider.GetRequiredService<IMigrationService>();
        var backupService = serviceProvider.GetRequiredService<IBackupService>();

        logger.LogInformation("=== Database Migrations Example ===\n");

        try
        {
            // Create tenant
            logger.LogInformation("Step 1: Creating tenant...");
            var tenant = await tenantService.CreateTenantAsync(
                name: "MigrationTest Corp",
                description: "Testing migrations",
                contactEmail: "admin@test.com");
            logger.LogInformation($"✓ Tenant created: {tenant.TenantId}\n");

            // Create database entry
            var databaseId = Guid.NewGuid().ToString();
            var tenantDb = new TenantDatabase
            {
                DatabaseId = databaseId,
                TenantId = tenant.TenantId,
                Name = "primary",
                FilePath = Path.Combine("databases", $"{tenant.TenantId}_primary.db"),
                SchemaVersion = 1,
                IsReadOnly = false,
                CreatedAt = DateTime.UtcNow
            };
            logger.LogInformation($"Step 2: Created database entry: {databaseId}\n");

            // Create backup before migrations
            logger.LogInformation("Step 3: Creating baseline backup...");
            var backup = await backupService.CreateBackupAsync(
                databaseId: databaseId,
                backupType: Constants.BackupType.Full,
                createdBy: "admin");
            await backupService.MarkBackupAsCompletedAsync(backup.BackupId, 0, 0);
            logger.LogInformation($"✓ Backup created: {backup.BackupId}\n");

            // Define migrations
            logger.LogInformation("Step 4: Creating migrations...");

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
                            CREATE INDEX idx_posts_userid ON Posts(UserId);
                            CREATE INDEX idx_posts_created ON Posts(CreatedAt);",
                    Down = @"DROP INDEX idx_users_email;
                             DROP INDEX idx_posts_userid;
                             DROP INDEX idx_posts_created;"
                }
            };

            // Create migration records
            foreach (var mig in migrations)
            {
                var migration = await migrationService.CreateMigrationAsync(
                    databaseId: databaseId,
                    version: mig.Version,
                    name: mig.Name,
                    upScript: mig.Up,
                    downScript: mig.Down);

                logger.LogInformation($"  ✓ Created: {mig.Version} - {mig.Name}");
            }
            logger.LogInformation();

            // Get pending migrations
            logger.LogInformation("Step 5: Viewing pending migrations...");
            var pending = await migrationService.GetPendingMigrationsAsync(databaseId);
            logger.LogInformation($"✓ Pending migrations: {pending.Count}");
            foreach (var m in pending.OrderBy(x => x.Version))
            {
                logger.LogInformation($"  - {m.Version}: {m.Name} (Rollbackable: {m.IsRollbackable})");
            }
            logger.LogInformation();

            // Simulate applying migrations
            logger.LogInformation("Step 6: Simulating migration execution...");
            foreach (var pending_mig in pending.OrderBy(x => x.Version))
            {
                var startTime = DateTime.UtcNow;

                // In real scenario, execute pending_mig.UpScript on actual database
                logger.LogInformation($"  Executing: {pending_mig.Version} - {pending_mig.Name}");

                // Simulate execution delay
                await Task.Delay(100);

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // Mark as completed
                await migrationService.MarkMigrationAsCompletedAsync(
                    migrationId: pending_mig.MigrationId,
                    executionMs: (long)duration);

                logger.LogInformation($"    ✓ Completed ({duration}ms)");
            }
            logger.LogInformation();

            // Get applied migrations
            logger.LogInformation("Step 7: Viewing applied migrations...");
            var applied = await migrationService.GetAppliedMigrationsAsync(databaseId);
            logger.LogInformation($"✓ Applied migrations: {applied.Count}");
            foreach (var m in applied.OrderBy(x => x.Version))
            {
                logger.LogInformation($"  - {m.Version}: {m.Name} (Applied: {m.ExecutedAt:O})");
            }
            logger.LogInformation();

            // Demonstrate rollback capability
            logger.LogInformation("Step 8: Testing rollback capability...");
            var lastMigration = applied.OrderByDescending(x => x.Version).FirstOrDefault();
            if (lastMigration?.IsRollbackable ?? false)
            {
                logger.LogInformation($"✓ Migration {lastMigration.Version} can be rolled back");
                logger.LogInformation($"  Down script available: {!string.IsNullOrEmpty(lastMigration.DownScript)}");
            }
            logger.LogInformation();

            logger.LogInformation("✓ Migrations example completed successfully!");
            logger.LogInformation("\nSummary:");
            logger.LogInformation($"  Total Migrations Created: {migrations.Length}");
            logger.LogInformation($"  Applied Migrations: {applied.Count}");
            logger.LogInformation($"  Database: {tenantDb.FilePath}");
            logger.LogInformation($"  Ready for production use!");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
