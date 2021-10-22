# ReportGenerator

The `ReportGenerator` class provides a simple façade for producing textual reports related to the health, performance, usage, errors, and capacity of a SQLite‑based multi‑tenant system. Each method generates a report string that can be logged, displayed, or persisted without requiring any configuration parameters.

## API

### `public ReportGenerator()`
Creates a new instance of the report generator. The constructor does not take any parameters and does not throw exceptions under normal circumstances.

### `public string GenerateHealthReport()`
**Purpose:** Returns a summary of the overall health of the tenant database connections, including connectivity status and basic diagnostics.  
**Parameters:** None.  
**Return value:** A non‑empty string containing the health report.  
**Throws:**  
- `InvalidOperationException` if the underlying data source cannot be accessed at the time of the call.  
- `ObjectDisposedException` if the instance has been used after being disposed (if a dispose pattern is added in the future).

### `public string GeneratePerformanceReport()`
**Purpose:** Returns a report detailing query execution times, throughput, and resource utilization metrics for the tenant databases.  
**Parameters:** None.  
**Return value:** A non‑empty string containing the performance report.  
**Throws:**  
- `InvalidOperationException` when performance counters are unavailable or the database is offline.  
- `NotSupportedException` if the runtime environment does not expose the required performance information.

### `public string GenerateTenantUsageReport()`
**Purpose:** Returns a breakdown of storage and connection usage per tenant, useful for capacity planning and billing.  
**Parameters:** None.  
**Return value:** A non‑empty string containing the tenant usage report.  
**Throws:**  
- `InvalidOperationException` if tenant metadata cannot be read.  
- `UnauthorizedAccessException` when the caller lacks permission to access tenant‑specific data.

### `public string GenerateErrorReport()`
**Purpose:** Returns a formatted list of recent errors and exceptions captured by the system, including timestamps and error codes.  
**Parameters:** None.  
**Return value:** A non‑empty string containing the error report.  
**Throws:**  
- `InvalidOperationException` if the error log storage is inaccessible.  
- `IOException` when there is a problem reading the error log file.

### `public string GenerateCapacityReport()`
**Purpose:** Returns an assessment of remaining storage capacity, connection pool availability, and projected growth trends for each tenant.  
**Parameters:** None.  
**Return value:** A non‑empty string containing the capacity report.  
**Throws:**  
- `InvalidOperationException` when capacity metrics cannot be computed (e.g., missing schema tables).  
- `OverflowException` if the calculated values exceed the representable range of the underlying numeric type.

## Usage

```csharp
using System;
using SqliteMultiTenant.Reports; // adjust namespace as needed

class Program
{
    static void Main()
    {
        var generator = new ReportGenerator();

        string health = generator.GenerateHealthReport();
        Console.WriteLine("Health Report:");
        Console.WriteLine(health);

        string usage = generator.GenerateTenantUsageReport();
        Console.WriteLine("\nTenant Usage Report:");
        Console.WriteLine(usage);
    }
}
```

```csharp
using System.IO;
using SqliteMultiTenant.Reports;

class ReportingService
{
    private readonly ReportGenerator _generator = new ReportGenerator();

    public void SaveAllReports(string directory)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(Path.Combine(directory, "health.txt"), _generator.GenerateHealthReport());
        File.WriteAllText(Path.Combine(directory, "performance.txt"), _generator.GeneratePerformanceReport());
        File.WriteAllText(Path.Combine(directory, "tenant-usage.txt"), _generator.GenerateTenantUsageReport());
        File.WriteAllText(Path.Combine(directory, "errors.txt"), _generator.GenerateErrorReport());
        File.WriteAllText(Path.Combine(directory, "capacity.txt"), _generator.GenerateCapacityReport());
    }
}
```

## Notes

- The class holds no mutable state; therefore, instances are inherently thread‑safe for concurrent invocation of any of the report‑generation methods, assuming the underlying data sources themselves are thread‑safe or properly synchronized.
- If any method throws an exception, the returned string is undefined; callers should handle exceptions appropriately and not rely on a partial or empty report.
- The methods are designed to be idempotent with respect to their inputs (none), but successive calls may yield different results as the underlying system state changes.
- In the current implementation, none of the methods accept parameters; adding parameters in a future version would be a breaking change.
- Consumers should consider throttling calls if the report generation involves expensive queries or file I/O, to avoid impacting the performance of the tenant workload.
