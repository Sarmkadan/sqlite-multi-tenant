# BulkDataService

The `BulkDataService` class provides high-performance data transfer capabilities for the `sqlite-multi-tenant` project, facilitating efficient bulk export and import operations against SQLite databases. It supports both batch-oriented processing via task-based asynchronous patterns and streaming scenarios using asynchronous enumerables, enabling scalable data migration, backup, and restoration workflows across multi-tenant environments.

## API

### `public BulkDataService`
Initializes a new instance of the `BulkDataService` class. This constructor prepares the service for performing data operations, typically requiring necessary dependencies such as database connection factories or tenant context resolvers to be injected or configured prior to use.

### `public async Task<BulkExportResult> ExportDatabaseAsync`
Exports the entire contents of the current tenant's database into a single bulk package.
*   **Purpose**: Creates a comprehensive snapshot of all tables and schema within the active database context.
*   **Return Value**: Returns a `Task<BulkExportResult>` containing metadata about the export operation, including total rows processed, byte size, and a reference to the exported data payload.
*   **Exceptions**: Throws if the database connection cannot be established, if the user lacks permissions to read specific system tables, or if disk space is insufficient for the temporary export buffer.

### `public async Task<BulkExportResult> ExportTableAsync`
Exports data from a specific table within the current tenant's database.
*   **Purpose**: Extracts all rows from a named table, preserving column order and data types.
*   **Parameters**: Requires the name of the table to export (typically passed as a string argument).
*   **Return Value**: Returns a `Task<BulkExportResult>` detailing the count of exported rows and the resulting data stream or file reference.
*   **Exceptions**: Throws `ArgumentException` if the table name is null or empty; throws an database-specific exception if the table does not exist.

### `public async IAsyncEnumerable<ExportBatch> StreamExportAsync`
Streams export data in incremental batches rather than loading the entire result set into memory.
*   **Purpose**: Enables memory-efficient processing of large datasets by yielding `ExportBatch` objects as they become available.
*   **Return Value**: Returns an `IAsyncEnumerable<ExportBatch>` allowing the caller to iterate over data chunks asynchronously.
*   **Exceptions**: Throws if the underlying data reader fails during iteration or if the connection is lost mid-stream.

### `public async Task<BulkImportResult> ImportTableAsync`
Imports data into a specific table, typically replacing or appending to existing records based on configuration.
*   **Purpose**: Performs a high-volume insert operation from a provided data source into a target table.
*   **Parameters**: Requires the target table name and the source data payload (format depends on implementation, often a stream or collection).
*   **Return Value**: Returns a `Task<BulkImportResult>` indicating the number of rows successfully inserted, skipped, or failed.
*   **Exceptions**: Throws if the target table schema does not match the incoming data, if constraint violations occur (e.g., primary key conflicts), or if the transaction log fills up.

### `public async Task<BulkImportResult> StreamImportAsync`
Imports data from an asynchronous stream, processing records as they are received.
*   **Purpose**: Facilitates low-memory footprint imports by consuming an `IAsyncEnumerable` or similar stream of data records.
*   **Parameters**: Accepts a data stream and the target table identifier.
*   **Return Value**: Returns a `Task<BulkImportResult>` summarizing the ingestion statistics upon completion of the stream.
*   **Exceptions**: Throws if the stream yields malformed data, if the database connection drops during the write phase, or if a batch commit fails.

## Usage

### Example 1: Full Database Backup
The following example demonstrates how to perform a full database export for backup purposes, awaiting the final result to log statistics.

```csharp
public async Task BackupTenantDatabaseAsync(BulkDataService service, string tenantId)
{
    // Assume context switch to specific tenant has occurred
    var result = await service.ExportDatabaseAsync();
    
    Console.WriteLine($"Backup completed for tenant {tenantId}");
    Console.WriteLine($"Total rows exported: {result.TotalRows}");
    Console.WriteLine($"Payload size: {result.SizeBytes} bytes");
    
    // Persist result.Stream or result.Payload to storage
}
```

### Example 2: Streaming Large Table Import
This example illustrates importing a large dataset using the streaming API to minimize memory pressure, processing batches as they are read from an external source.

```csharp
public async Task ImportLargeDatasetAsync(BulkDataService service, IAsyncEnumerable<DataRecord> sourceStream)
{
    // StreamImportAsync consumes the sourceStream and writes to the "Events" table
    var importResult = await service.StreamImportAsync("Events", sourceStream);
    
    if (importResult.FailedRows > 0)
    {
        Console.WriteLine($"Import finished with {importResult.FailedRows} errors.");
        // Handle error logging or retry logic
    }
    else
    {
        Console.WriteLine($"Successfully imported {importResult.InsertedRows} records.");
    }
}
```

## Notes

*   **Memory Management**: When dealing with very large datasets, prefer `StreamExportAsync` and `StreamImportAsync` over their `Task`-based counterparts (`ExportTableAsync`, `ImportTableAsync`) to prevent `OutOfMemoryException` scenarios, as the streaming methods process data in incremental `ExportBatch` units.
*   **Transaction Scope**: Import operations (`ImportTableAsync`, `StreamImportAsync`) typically execute within a database transaction. If the operation fails midway, all changes for that specific call should be rolled back automatically, ensuring data consistency.
*   **Thread Safety**: Instances of `BulkDataService` are not guaranteed to be thread-safe for concurrent execution of methods on the same instance. While multiple instances can operate in parallel (e.g., for different tenants), a single instance should not have `Export` and `Import` methods invoked simultaneously from different threads without external synchronization.
*   **Schema Compatibility**: The service assumes the target table schema exists and matches the structure of the data being imported. It does not automatically create tables or alter schemas; mismatched columns will result in runtime exceptions during import.
*   **Cancellation**: As all public members are asynchronous, they support standard .NET cancellation tokens if propagated through the call chain, allowing long-running bulk operations to be gracefully terminated.
