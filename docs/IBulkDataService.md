# IBulkDataService Documentation

## Overview
The `IBulkDataService` interface defines a high-level contract for asynchronous streaming bulk import and export operations on tenant databases. It provides methods for exporting and importing data in various formats, with support for progress reporting, cancellation, and streaming to handle large datasets efficiently.

## Relation to BulkDataService
This interface is implemented by the `BulkDataService` class defined in [BulkDataService.md](./BulkDataService.md). All interface methods are implemented with proper error handling, transaction management, and adherence to the configured bulk data options.

## Public Methods

### `ExportDatabaseAsync`
```csharp
Task<BulkExportResult> ExportDatabaseAsync(
    string databaseId,
    BulkDataFormat format,
    ExportOptions? options = null,
    IProgress<ExportProgress>? progress = null,
    CancellationToken cancellationToken = default)
```
Exports every user table in the specified database to a single serialised artifact. Tables are processed in parallel up to `BulkDataOptions.MaxConcurrentTables`.

- **Parameters**:
  - `databaseId`: Logical identifier of the source database.
  - `format`: Output serialisation format (see `BulkDataFormat` enum).
  - `options`: Optional per-call overrides; uses global defaults when `null` (see `ExportOptions` class).
  - `progress`: Optional callback invoked after each batch. Receives an `ExportProgress` snapshot with cumulative statistics for the table currently being exported.
  - `cancellationToken`: Token to cancel the operation at the next safe boundary.
- **Returns**: A `BulkExportResult` containing aggregate statistics and, when an output path was configured, the path to the persisted artifact.
- **Referenced Types**: 
  - `BulkDataFormat` (enum, defined in the BulkOperations namespace)
  - `ExportOptions` (class, defined in the BulkOperations namespace)
  - `BulkExportResult` (class, defined in the BulkOperations namespace)
  - `ExportProgress` (class, defined in the BulkOperations namespace)
  - For bulk operation configuration, see [BulkDataOptions.md](./BulkDataOptions.md)

### `ExportTableAsync`
```csharp
Task<BulkExportResult> ExportTableAsync(
    string databaseId,
    string tableName,
    BulkDataFormat format,
    ExportOptions? options = null,
    IProgress<ExportProgress>? progress = null,
    CancellationToken cancellationToken = default)
```
Exports a single table from the specified database.

- **Parameters**:
  - `databaseId`: Logical identifier of the source database.
  - `tableName`: Name of the table to export.
  - `format`: Output serialisation format.
  - `options`: Optional per-call overrides.
  - `progress`: Optional per-batch progress callback.
  - `cancellationToken`: Cancellation token.
- **Returns**: A `BulkExportResult` with statistics for the exported table.
- **Referenced Types**: Same as `ExportDatabaseAsync`.

### `StreamExportAsync`
```csharp
IAsyncEnumerable<ExportBatch> StreamExportAsync(
    string databaseId,
    string tableName,
    BulkDataFormat format,
    int batchSize = 1_000,
    CancellationToken cancellationToken = default)
```
Streams an export as a lazy sequence of `ExportBatch` objects. Each batch contains at most `batchSize` serialised rows. The final batch for a table always has `ExportBatch.IsLastBatch` set to `true`, allowing the consumer to finalise the output channel.

- **Parameters**:
  - `databaseId`: Logical identifier of the source database.
  - `tableName`: Name of the table to stream.
  - `format`: Output serialisation format for batch payloads.
  - `batchSize`: Maximum rows per emitted batch.
  - `cancellationToken`: Cancellation token.
- **Returns**: An async sequence of batches in ascending `ExportBatch.SequenceNumber` order.
- **Referenced Types**: 
  - `BulkDataFormat` (enum)
  - `ExportBatch` (class, defined in the BulkOperations namespace)
  - For bulk operation configuration, see [BulkDataOptions.md](./BulkDataOptions.md)

### `ImportTableAsync`
```csharp
Task<BulkImportResult> ImportTableAsync(
    string databaseId,
    string tableName,
    Stream dataStream,
    BulkDataFormat format,
    ImportOptions? options = null,
    IProgress<ImportProgress>? progress = null,
    CancellationToken cancellationToken = default)
```
Imports data from a `Stream` into a single target table. The entire stream is read and applied within a single database transaction unless `ImportOptions.CommitEveryNRows` is configured.

- **Parameters**:
  - `databaseId`: Logical identifier of the target database.
  - `tableName`: Name of the table receiving the data.
  - `dataStream`: Readable stream containing the serialised payload.
  - `format`: Serialisation format of `dataStream`.
  - `options`: Optional per-call overrides.
  - `progress`: Optional per-batch progress callback.
  - `cancellationToken`: Cancellation token.
- **Returns**: A `BulkImportResult` with row-level statistics.
- **Referenced Types**: 
  - `BulkDataFormat` (enum)
  - `ImportOptions` (class, defined in the BulkOperations namespace)
  - `BulkImportResult` (class, defined in the BulkOperations namespace)
  - `ImportProgress` (class, defined in the BulkOperations namespace)
  - For bulk operation configuration, see [BulkDataOptions.md](./BulkDataOptions.md)

### `StreamImportAsync`
```csharp
Task<BulkImportResult> StreamImportAsync(
    string databaseId,
    IAsyncEnumerable<ImportBatch> batches,
    ImportOptions? options = null,
    IProgress<ImportProgress>? progress = null,
    CancellationToken cancellationToken = default)
```
Consumes an async sequence of pre-partitioned `ImportBatch` objects, writing each to its designated target table. Suitable for multi-table imports delivered by an upstream producer (e.g., a network relay or a transformation pipeline) without requiring full in-memory buffering.

- **Parameters**:
  - `databaseId`: Logical identifier of the target database.
  - `batches`: Async sequence of import batches. Batches for different tables may be interleaved; each batch carries its own `ImportBatch.TableName` and `ImportBatch.Format`.
  - `options`: Optional per-call overrides applied to all batches.
  - `progress`: Optional per-batch progress callback.
  - `cancellationToken`: Cancellation token.
- **Returns**: A `BulkImportResult` aggregating statistics across all consumed batches.
- **Referenced Types**: 
  - `ImportBatch` (class, defined in the BulkOperations namespace)
  - `ImportOptions` (class)
  - `BulkImportResult` (class)
  - `ImportProgress` (class)
  - For bulk operation configuration, see [BulkDataOptions.md](./BulkDataOptions.md)

## Usage Example
```csharp
// Dependency injection
public class MyController : ControllerBase
{
    private readonly IBulkDataService _bulkDataService;
    
    public MyController(IBulkDataService bulkDataService)
    {
        _bulkDataService = bulkDataService;
    }
    
    public async Task<IActionResult> ExportTenantData(string tenantId)
    {
        var options = new ExportOptions
        {
            // Configure options as needed
            Format = BulkDataFormat.CSV,
            // ... other options
        };
        
        var progress = new Progress<ExportProgress>(p =>
        {
            // Report progress to UI or logging
            Console.WriteLine($"Exporting {p.TableName}: {p.RowsProcessed} rows processed");
        });
        
        var result = await _bulkDataService.ExportDatabaseAsync(
            databaseId: tenantId,
            format: BulkDataFormat.CSV,
            options: options,
            progress: progress);
        
        if (result.Success)
        {
            return File(result.OutputPath!, "text/csv", $"tenant-{tenantId}-export.csv");
        }
        
        return BadRequest(result.ErrorMessage);
    }
}
```