// =============================================================================
// Example: Basic Usage
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Services;

class BasicUsage
{
    static async Task Main()
    {
        var services = new ServiceCollection();

        // Configure basic logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        // Configure SQLite Multi-Tenant
        var masterDb = "Data Source=master.db;Version=3;";
        services.AddSqliteMultiTenant(masterDb, options =>
        {
            options.DatabaseDirectory = "tenants";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Ensure directory exists
        Directory.CreateDirectory("tenants");

        var tenantService = serviceProvider.GetRequiredService<ITenantService>();

        // Create a tenant
        var tenant = await tenantService.CreateTenantAsync("Client A", "Description", "contact@clienta.com");
        Console.WriteLine($"Tenant '{tenant.Name}' created with ID: {tenant.TenantId}");
    }
}
