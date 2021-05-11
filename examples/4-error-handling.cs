#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Example 4: Error Handling and Exception Handling
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Exceptions;

// Example: Comprehensive error handling
class ErrorHandlingExample
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

        logger.LogInformation("=== Error Handling Example ===\n");

        // Example 1: Handle TenantNotFoundException
        logger.LogInformation("Example 1: Handling TenantNotFoundException");
        logger.LogInformation("  Attempting to get non-existent tenant...");

        try
        {
            var tenant = await tenantService.GetTenantAsync("non-existent-id");
            if (tenant is null)
            {
                logger.LogWarning("  Tenant not found");
            }
        }
        catch (TenantNotFoundException ex)
        {
            logger.LogWarning($"  ✓ Caught TenantNotFoundException");
            logger.LogWarning("    Tenant ID: {TenantId}", ex.TenantId);
            logger.LogWarning("    Message: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError($"  Unexpected error: {ex.GetType().Name} - {ex.Message}");
        }

        logger.LogInformation();

        // Example 2: Handle DatabaseAccessException
        logger.LogInformation("Example 2: Handling DatabaseAccessException");
        logger.LogInformation("  Creating tenant in read-only mode (simulated)...");

        try
        {
            // This would fail if the master database is corrupted or inaccessible
            var tenant = await tenantService.CreateTenantAsync(
                name: "Test Tenant",
                description: "Test",
                contactEmail: "test@example.com");

            logger.LogInformation("  ✓ Tenant created successfully: {TenantId}", tenant.TenantId);
        }
        catch (DatabaseAccessException ex)
        {
            logger.LogError($"  ✓ Caught DatabaseAccessException");
            logger.LogError("    Message: {Message}", ex.Message);
            logger.LogError($"    Recommendation: Check database connectivity and permissions");
        }
        catch (Exception ex)
        {
            logger.LogError($"  Unexpected error: {ex.GetType().Name}");
        }

        logger.LogInformation();

        // Example 3: Validation Errors
        logger.LogInformation("Example 3: Handling validation errors");
        logger.LogInformation("  Attempting to create tenant with invalid email...");

        try
        {
            // Invalid email format
            var tenant = await tenantService.CreateTenantAsync(
                name: "Bad Email Corp",
                description: "Test",
                contactEmail: "not-an-email");

            logger.LogInformation("  Tenant created: {TenantId}", tenant.TenantId);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning($"  ✓ Caught validation error");
            logger.LogWarning("    Message: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError($"  Unexpected error: {ex.GetType().Name}");
        }

        logger.LogInformation();

        // Example 4: Handling Multiple Exception Types
        logger.LogInformation("Example 4: Handling multiple exception types");

        await SafeExecuteOperation(async () =>
        {
            var tenant = await tenantService.GetTenantAsync("test-id");
            return tenant?.TenantId ?? "none";
        }, logger);

        logger.LogInformation();

        // Example 5: Retry Logic
        logger.LogInformation("Example 5: Implementing retry logic");
        var result = await RetryOperation(
            async () => await tenantService.CreateTenantAsync(
                "RetryTest Corp",
                "Retry test",
                "retry@test.com"),
            maxRetries: 3,
            logger);

        if (result is not null)
        {
            logger.LogInformation("  ✓ Operation succeeded: {TenantId}", result.TenantId);
        }
        else
        {
            logger.LogError("  Operation failed after retries");
        }

        logger.LogInformation();

        // Example 6: Batch Operations with Error Handling
        logger.LogInformation("Example 6: Batch operations with error handling");

        var tenantNames = new[] { "Batch1", "Batch2", "Batch3" };
        var results = new List<(string Name, string TenantId, string Error)>();

        foreach (var name in tenantNames)
        {
            try
            {
                var tenant = await tenantService.CreateTenantAsync(
                    name: name,
                    description: $"Batch tenant {name}",
                    contactEmail: $"{name}@example.com");

                results.Add((name, tenant.TenantId, null));
                logger.LogInformation("  ✓ {Name}: {TenantId}", name, tenant.TenantId);
            }
            catch (Exception ex)
            {
                results.Add((name, null, ex.Message));
                logger.LogError("  ✗ {Name}: {Message}", name, ex.Message);
            }
        }

        logger.LogInformation($"\nBatch Results: {results.Count(r => r.Error == null)} succeeded, " +
                            $"{results.Count(r => r.Error != null)} failed");

        logger.LogInformation("\n✓ Error handling example completed!");
    }

    // Helper method: Safe operation wrapper
    static async Task SafeExecuteOperation(
        Func<Task<string>> operation,
        ILogger logger)
    {
        try
        {
            logger.LogInformation("  Executing operation with comprehensive error handling...");
            var result = await operation();
            logger.LogInformation("  ✓ Operation succeeded: {Result}", result);
        }
        catch (TenantNotFoundException ex)
        {
            logger.LogWarning("  ✓ Tenant not found: {TenantId}", ex.TenantId);
        }
        catch (DatabaseAccessException ex)
        {
            logger.LogError("  ✓ Database error: {Message}", ex.Message);
        }
        catch (MigrationException ex)
        {
            logger.LogError("  ✓ Migration error: {MigrationVersion}", ex.MigrationVersion);
        }
        catch (BackupException ex)
        {
            logger.LogError("  ✓ Backup error: {BackupId}", ex.BackupId);
        }
        catch (Exception ex)
        {
            logger.LogError($"  ✓ Unexpected error: {ex.GetType().Name} - {ex.Message}");
        }
    }

    // Helper method: Retry logic
    static async Task<T> RetryOperation<T>(
        Func<Task<T>> operation,
        int maxRetries,
        ILogger logger) where T : class
    {
        int attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                attempt++;
                logger.LogInformation("  Attempt {Attempt} of {MaxRetries}...", attempt, maxRetries);
                var result = await operation();
                return result;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                var delay = Math.Pow(2, attempt - 1) * 100; // Exponential backoff
                logger.LogWarning("  Attempt {Attempt} failed: {Message}", attempt, ex.Message);
                logger.LogInformation("  Retrying in {Delay}ms...", delay);
                await Task.Delay((int)delay);
            }
            catch (Exception ex)
            {
                logger.LogError("  All {MaxRetries} attempts failed: {Message}", maxRetries, ex.Message);
                return null;
            }
        }

        return null;
    }
}
