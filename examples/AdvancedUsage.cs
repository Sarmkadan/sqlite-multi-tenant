// =============================================================================
// Example: Advanced Usage
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Exceptions;

class AdvancedUsage
{
    static async Task Main()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        var masterDb = "Data Source=master.db;Version=3;";
        services.AddSqliteMultiTenant(masterDb, options =>
        {
            options.DatabaseDirectory = "tenants";
            options.BackupDirectory = "backups";
            options.MaxConnections = 50;
            options.EnableEncryption = true; // Requires proper setup
        });

        var serviceProvider = services.BuildServiceProvider();
        var tenantService = serviceProvider.GetRequiredService<ITenantService>();

        try
        {
            // Advanced: Create tenant with specific configuration/settings if applicable
            var tenant = await tenantService.CreateTenantAsync("Advanced Client", "High traffic", "admin@advanced.com");
            
            // Perform operations with error handling
            await tenantService.DeactivateTenantAsync(tenant.TenantId);
            Console.WriteLine("Tenant deactivated successfully.");
        }
        catch (TenantNotFoundException)
        {
            Console.WriteLine("Tenant not found.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}
