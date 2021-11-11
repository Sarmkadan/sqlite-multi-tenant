# BulkDataServiceExtensions

`BulkDataServiceExtensions` provides high-performance extension methods for transferring table data between SQLite databases in the `sqlite-multi-tenant` system. These methods operate at the raw data level, bypassing ORM overhead to achieve maximum throughput for bulk export, import, backup, and cross-database streaming scenarios. All operations are asynchronous and return structured result objects that report row counts and elapsed time.

## API

### ExportTableToNewDatabaseAsync

```csharp
public static async Task<BulkExportResult> ExportTableToNewDatabaseAsync(
    this IBulkDataService service,
    string sourceConnectionString,
    string tableName,
    string targetDatabasePath,
    CancellationToken cancellationToken = default)
```

Exports a single table from the source database into a newly created SQLite database file at the specified path. The target database is created if it does not exist; if it already exists, the table is created or replaced within it. Only the schema and data for the named table are transferred.

**Parameters:**
- `service`: The `IBulkDataService` instance being extended.
- `sourceConnectionString`: Connection string for the source database.
- `tableName`: Name of the table to export. Must exist in the source database.
- `targetDatabasePath`: File system path where the new database will be created.
- `cancellationToken`: Token to cancel the operation.

**Returns:** A `BulkExportResult` containing the number of rows exported and the elapsed wall-clock time.

**Throws:**
- `ArgumentNullException` if `sourceConnectionString`, `tableName`, or `targetDatabasePath` is null or empty.
- `DatabaseAccessException` if the source database cannot be opened or the specified table does not exist.
- `IOException` if the target path is inaccessible or cannot be written to.

---

### ImportFromDatabaseFileAsync

```csharp
public static async Task<BulkImportResult> ImportFromDatabaseFileAsync(
    this IBulkDataService service,
    string targetConnectionString,
    string tableName,
    string sourceDatabasePath,
    CancellationToken cancellationToken = default)
```

Imports a table from an external SQLite database file into the target database. The source file is opened in read-only mode. The table must exist in the source file; it is created in the target database if absent, or its data is appended/replaced according to the implementation's conflict behavior.

**Parameters:**
- `service`: The `IBulkDataService` instance being extended.
- `targetConnectionString`: Connection string for the destination database.
- `tableName`: Name of the table to import.
- `sourceDatabasePath`: File system path to the source SQLite database file.
- `cancellationToken`: Token to cancel the operation.

**Returns:** A `BulkImportResult` containing the number of rows imported and the elapsed time.

**Throws:**
- `ArgumentNullException` if any required string argument is null or empty.
- `DatabaseAccessException` if either database cannot be opened or the source table is missing.
- `FileNotFoundException` if `sourceDatabasePath` does not point to an existing file.

---

### CreateDatabaseBackupAsync

```csharp
public static async Task<BulkExportResult> CreateDatabaseBackupAsync(
    this IBulkDataService service,
    string sourceConnectionString,
    string backupDatabasePath,
    CancellationToken cancellationToken = default)
```

Creates a full backup of the source database by copying all tables and their data to a new database file. This is a logical backup (table-by-table) rather than a file-level copy, making it suitable for multi-tenant scenarios where per-tenant filtering or transformation may be applied during the backup process.

**Parameters:**
- `service`: The `IBulkDataService` instance being extended.
- `sourceConnectionString`: Connection string for the database to back up.
- `backupDatabasePath`: File system path for the resulting backup database.
- `cancellationToken`: Token to cancel the operation.

**Returns:** A `BulkExportResult` with the total number of rows copied across all tables and the elapsed time.

**Throws:**
- `ArgumentNullException` if `sourceConnectionString` or `backupDatabasePath` is null or empty.
- `DatabaseAccessException` if the source database cannot be opened or is empty.
- `IOException` if the backup path is not writable.

---

### StreamBetweenDatabasesAsync

```csharp
public static async Task<BulkImportResult> StreamBetweenDatabasesAsync(
    this IBulkDataService service,
    string sourceConnectionString,
    string targetConnectionString,
    string tableName,
    CancellationToken cancellationToken = default)
```

Streams a table directly from one open database to another without materializing the entire dataset in memory. Both databases must be accessible via their connection strings. This method is optimized for large tables where memory pressure would otherwise be prohibitive.

**Parameters:**
- `service`: The `IBulkDataService` instance being extended.
- `sourceConnectionString`: Connection string for the source database.
- `targetConnectionString`: Connection string for the destination database.
- `tableName`: Name of the table to stream.
- `cancellationToken`: Token to cancel the operation.

**Returns:** A `BulkImportResult` with the row count transferred and elapsed time.

**Throws:**
- `ArgumentNullException` if any required string argument is null or empty.
- `DatabaseAccessException` if either database cannot be opened or the source table does not exist.

## Usage

### Example 1: Exporting a Tenant Table to an Archive

```csharp
var bulkService = serviceProvider.GetRequiredService<IBulkDataService>();
var sourceConnStr = "Data Source=tenant_abc.db";

// Export the 'Orders' table to a timestamped archive file
var archivePath = $"/backups/orders_{DateTime.UtcNow:yyyyMMddHHmmss}.db";

BulkExportResult result = await bulkService.ExportTableToNewDatabaseAsync(
    sourceConnStr,
    "Orders",
    archivePath,
    CancellationToken.None);

Console.WriteLine($"Exported {result.RowsExported} rows in {result.Elapsed.TotalSeconds:F2}s");
```

### Example 2: Streaming Between Tenant and Aggregator Databases

```csharp
var bulkService = serviceProvider.GetRequiredService<IBulkDataService>();
var tenantConnStr = "Data Source=tenant_xyz.db";
var aggregatorConnStr = "Data Source=aggregator.db";

// Stream the 'Events' table from a tenant database into the central aggregator
BulkImportResult result = await bulkService.StreamBetweenDatabasesAsync(
    tenantConnStr,
    aggregatorConnStr,
    "Events",
    new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token);

Console.WriteLine($"Streamed {result.RowsImported} rows in {result.Elapsed.TotalSeconds:F2}s");
```

## Notes

- **Thread Safety:** These extension methods delegate to the underlying `IBulkDataService` implementation. Callers should consult that implementation's documentation for specific thread-safety guarantees. In general, SQLite connections are not thread-safe; concurrent calls targeting the same database file should use distinct connection strings or external synchronization.
- **Table Schema Matching:** When importing or streaming between databases, the source and target table schemas must be compatible. Behavior on schema mismatch (e.g., extra columns, type differences) depends on the `IBulkDataService` implementation and may result in `DatabaseAccessException` or data truncation.
- **Cancellation:** All methods accept a `CancellationToken`. Cancellation mid-operation leaves the target database in an undefined state; partial data may have been written. Callers should implement their own cleanup or transactional wrapping if atomicity is required.
- **Large Tables:** `StreamBetweenDatabasesAsync` is specifically designed for tables that exceed available memory. Prefer it over `ExportTableToNewDatabaseAsync` followed by `ImportFromDatabaseFileAsync` when both databases are simultaneously accessible, as it avoids the intermediate file and double I/O cost.
- **Backup Scope:** `CreateDatabaseBackupAsync` performs a logical backup of all tables. It does not include SQLite-specific artifacts such as WAL files, indexes not tied to tables, or triggers unless the implementation explicitly handles them. For disaster recovery requiring byte-for-byte fidelity, consider file-level backup in addition to this method.
