// =============================================================================
// Example: ASP.NET Core Integration
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqliteMultiTenant.Configuration;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Integration: Adding to ASP.NET Core DI
        var masterDb = "Data Source=master.db;Version=3;";
        
        services.AddSqliteMultiTenant(masterDb, options =>
        {
            options.DatabaseDirectory = "App_Data/Tenants";
            options.BackupDirectory = "App_Data/Backups";
        });
        
        // The library services are now available for injection into controllers/services
        // Example: public MyController(ITenantService tenantService) { ... }
    }
}
