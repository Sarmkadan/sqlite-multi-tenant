// existing content ...

## DataImporterExtensions

The `DataImporterExtensions` class provides a set of extension methods for importing data into the database. It supports importing data from JSON, CSV, and SQL files, as well as validating and creating tables if they do not exist.

### Usage Example

```csharp
var jsonFilePath = "path/to/data.json";
var csvFilePath = "path/to/data.csv";
var sqlFilePath = "path/to/data.sql";

var jsonImportResult = await DataImporterExtensions.ImportFromJsonFileAsync(jsonFilePath);
var csvImportResult = await DataImporterExtensions.ImportFromCsvFileAsync(csvFilePath);
var sqlImportResult = await DataImporterExtensions.ImportFromSqlFileAsync(sqlFilePath);

var tableExists = await DataImporterExtensions.ValidateTableExistsAsync("TableName");
if (!tableExists)
{
    await DataImporterExtensions.CreateTableIfNotExistsAsync("TableName");
}
```

## QuotaCheckResult

`QuotaCheckResult` represents the outcome of a quota evaluation for a specific tenant. It contains the tenant identifier, the current size of the tenant's data, the configured quota (if any), and calculated usage metrics such as the percentage of quota used and whether the tenant is over or near its quota limit.

### Usage Example

```csharp
using SqliteMultiTenant.Tenants;
using System;
using System.Threading.Tasks;

public class QuotaDemo
{
    private readonly TenantQuotaEnforcer _enforcer;

    public QuotaDemo(TenantQuotaEnforcer enforcer)
    {
        _enforcer = enforcer;
    }

    public async Task RunAsync()
    {
        const string tenantId = "tenant-123";

        // Set a quota of 1 GB for the tenant
        await _enforcer.SetQuotaAsync(tenantId, 1L * 1024 * 1024 * 1024);

        // Retrieve the current quota (may be null if not set)
        long? quota = await _enforcer.GetQuotaAsync(tenantId);
        Console.WriteLine($"Current quota for {tenantId}: {(quota.HasValue ? $"{quota.Value} bytes" : "none")}");

        // Perform a quota check
        QuotaCheckResult check = await _enforcer.CheckQuotaAsync(tenantId);
        Console.WriteLine(
            $"Tenant {check.TenantId} uses {check.CurrentSizeBytes} bytes " +
            $"({check.UsagePercent:P2} of quota). " +
            $"Over quota: {check.IsOverQuota}, Near quota: {check.IsNearQuota}");

        // Enforce the quota (will take action if over the limit)
        QuotaCheckResult enforcementResult = await _enforcer.EnforceAsync(tenantId);
        Console.WriteLine($"Enforcement completed. Over quota: {enforcementResult.IsOverQuota}");

        // Scan all tenants and get their quota status
        var allResults = await _enforcer.ScanAllAsync();
        Console.WriteLine($"Scanned {allResults.Count} tenants.");
    }
}
```

// existing content ...
