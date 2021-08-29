#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using SqliteMultiTenant.Events;

namespace SqliteMultiTenant.BulkOperations;

/// <summary>
/// Extension methods for <see cref="BulkDataService"/> providing additional
/// convenience operations for bulk data operations.
/// </summary>
public static class BulkDataServiceExtensions
{
    /// <summary>
    /// Exports a single table to a SQLite database file, creating the database
    /// and table structure if they don't exist.
    /// </summary>
    /// <param name="service">The bulk data service instance</param>
    /// <param name="databaseId">The target database identifier</param>
    /// <param name="tableName">The table to export</param>
    /// <param name="format">The export format</param>
    /// <param name="options">Optional export options</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk export result containing the exported data</returns>
    public static async Task<BulkExportResult> ExportTableToNewDatabaseAsync(
        this BulkDataService service,
        string databaseId,
        string tableName,
        BulkDataFormat format,
        ExportOptions? options = null,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        var result = await service.ExportTableAsync(databaseId, tableName, format, options, progress, cancellationToken);

        if (!result.IsSuccess)
            return result;

        // Create a new database file and import the exported data
        var newDatabasePath = Path.Combine(service.GetOptions().BaseDatabasePath, $"{databaseId}_exported.db");

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(newDatabasePath)!);

        // Create empty database file
        SQLiteConnection.CreateFile(newDatabasePath);

        // Import the exported data into the new database
        await using var fileStream = File.OpenRead(result.OutputPath!);
        var importResult = await service.ImportTableAsync(
            Path.GetFileNameWithoutExtension(newDatabasePath),
            tableName,
            fileStream,
            format,
            new ImportOptions { TruncateBeforeImport = true },
            null,
            cancellationToken);

        return new BulkExportResult
        {
            IsSuccess = importResult.IsSuccess,
            TablesProcessed = importResult.IsSuccess ? [tableName] : [],
            TotalRowsExported = result.TotalRowsExported,
            OutputPath = newDatabasePath,
            Duration = result.Duration,
            Warnings = importResult.Warnings
        };
    }

    /// <summary>
    /// Imports data from a SQLite database file into the specified table.
    /// </summary>
    /// <param name="service">The bulk data service instance</param>
    /// <param name="databaseId">The target database identifier</param>
    /// <param name="sourceDatabasePath">Path to the source SQLite database file</param>
    /// <param name="tableName">The target table name</param>
    /// <param name="format">The data format in the source file</param>
    /// <param name="options">Optional import options</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk import result</returns>
    public static async Task<BulkImportResult> ImportFromDatabaseFileAsync(
        this BulkDataService service,
        string databaseId,
        string sourceDatabasePath,
        string tableName,
        BulkDataFormat format,
        ImportOptions? options = null,
        IProgress<ImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDatabasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        await using var fileStream = File.OpenRead(sourceDatabasePath);
        return await service.ImportTableAsync(databaseId, tableName, fileStream, format, options, progress, cancellationToken);
    }

    /// <summary>
    /// Creates a backup of the specified database by exporting all tables and
    /// importing them into a new database file.
    /// </summary>
    /// <param name="service">The bulk data service instance</param>
    /// <param name="databaseId">The database to backup</param>
    /// <param name="backupDatabaseId">The backup database identifier</param>
    /// <param name="format">The export format to use for backup</param>
    /// <param name="options">Optional export/import options</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk export result with backup information</returns>
    public static async Task<BulkExportResult> CreateDatabaseBackupAsync(
        this BulkDataService service,
        string databaseId,
        string backupDatabaseId,
        BulkDataFormat format = BulkDataFormat.Sql,
        ExportOptions? options = null,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(backupDatabaseId);

        // Export the entire database
        var exportResult = await service.ExportDatabaseAsync(databaseId, format, options, progress, cancellationToken);

        if (!exportResult.IsSuccess)
            return exportResult;

        // Create backup database path
        var backupPath = Path.Combine(service.GetOptions().BaseDatabasePath, $"{backupDatabaseId}.db");

        // Import all tables into the backup database
        await using var fileStream = File.OpenRead(exportResult.OutputPath!);
        var importResult = await service.ImportTableAsync(
            backupDatabaseId,
            "__all__", // Special marker to import all tables
            fileStream,
            format,
            new ImportOptions { TruncateBeforeImport = true },
            null,
            cancellationToken);

        return new BulkExportResult
        {
            IsSuccess = importResult.IsSuccess,
            TablesProcessed = importResult.IsSuccess ? exportResult.TablesProcessed : [],
            TotalRowsExported = exportResult.TotalRowsExported,
            OutputPath = backupPath,
            Duration = exportResult.Duration,
            Warnings = importResult.Warnings
        };
    }

    /// <summary>
    /// Streams data from one database table to another with optional transformation.
    /// </summary>
    /// <param name="service">The bulk data service instance</param>
    /// <param name="sourceDatabaseId">Source database identifier</param>
    /// <param name="targetDatabaseId">Target database identifier</param>
    /// <param name="tableName">Table name to stream</param>
    /// <param name="format">Data format</param>
    /// <param name="transform">Optional row transformation function</param>
    /// <param name="batchSize">Batch size for streaming</param>
    /// <param name="options">Optional import options for target</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk import result</returns>
    public static async Task<BulkImportResult> StreamBetweenDatabasesAsync(
        this BulkDataService service,
        string sourceDatabaseId,
        string targetDatabaseId,
        string tableName,
        BulkDataFormat format = BulkDataFormat.Json,
        Func<Dictionary<string, object?>, Dictionary<string, object?>>? transform = null,
        int batchSize = 1000,
        ImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDatabaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDatabaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        var batches = service.StreamExportAsync(
            sourceDatabaseId,
            tableName,
            format,
            batchSize,
            cancellationToken);

        var importBatches = TransformBatchesAsync(batches, format, transform);

        return await service.StreamImportAsync(
            targetDatabaseId,
            importBatches,
            options,
            null,
            cancellationToken);
    }

    private static async IAsyncEnumerable<ImportBatch> TransformBatchesAsync(
        IAsyncEnumerable<ExportBatch> batches,
        BulkDataFormat format,
        Func<Dictionary<string, object?>, Dictionary<string, object?>>? transform,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var batch in batches.WithCancellation(cancellationToken))
        {
            if (transform == null)
            {
                yield return new ImportBatch(batch.TableName, batch.Data, format, batch.SequenceNumber, batch.IsLastBatch);
                continue;
            }

            // Parse and transform rows
            var rows = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(batch.Data);
            var transformedRows = rows?.Select(transform).ToList() ?? [];
            var transformedData = JsonSerializer.Serialize(transformedRows);

            yield return new ImportBatch(batch.TableName, transformedData, format, batch.SequenceNumber, batch.IsLastBatch);
        }
    }

    /// <summary>
    /// Gets the BulkDataOptions from the BulkDataService for external use.
    /// </summary>
    /// <param name="service">The bulk data service instance</param>
    /// <returns>The BulkDataOptions configuration</returns>
    private static BulkDataOptions GetOptions(this BulkDataService service)
    {
        var optionsField = typeof(BulkDataService).GetField("_options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (BulkDataOptions)(optionsField?.GetValue(service) ?? throw new InvalidOperationException("Could not access BulkDataOptions"));
    }
}
