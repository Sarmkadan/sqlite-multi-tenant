// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using SqliteMultiTenant.DataOperations;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Operations;

namespace SqliteMultiTenant.BulkOperations;

/// <summary>
/// Production implementation of <see cref="IBulkDataService"/>.
/// Orchestrates streaming reads and writes over per-tenant SQLite files,
/// integrates with the domain event bus, and delegates concurrent table
/// processing to <see cref="IBatchProcessor"/>.
/// </summary>
public sealed class BulkDataService : IBulkDataService
{
    private readonly DataExporter _exporter;
    private readonly DataImporter _importer;
    private readonly IBatchProcessor _batchProcessor;
    private readonly IEventBus _eventBus;
    private readonly ILogger<BulkDataService> _logger;
    private readonly BulkDataOptions _options;

    /// <summary>
    /// Initialises a new <see cref="BulkDataService"/> with all required dependencies.
    /// </summary>
    public BulkDataService(
        DataExporter exporter,
        DataImporter importer,
        IBatchProcessor batchProcessor,
        IEventBus eventBus,
        ILogger<BulkDataService> logger,
        BulkDataOptions options)
    {
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _importer = importer ?? throw new ArgumentNullException(nameof(importer));
        _batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<BulkExportResult> ExportDatabaseAsync(
        string databaseId,
        BulkDataFormat format,
        ExportOptions? options = null,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

        var operationId = Guid.NewGuid().ToString();
        var started = DateTime.UtcNow;
        var opts = options ?? new ExportOptions();
        var warnings = new List<string>();
        var allRows = 0L;
        var tables = new List<string>();

        _logger.LogInformation("BulkExport starting: database={DatabaseId}, format={Format}, op={OperationId}",
            databaseId, format, operationId);

        using var connection = await OpenConnectionAsync(databaseId, cancellationToken);
        tables = await GetTableNamesAsync(connection, cancellationToken);

        if (_options.PublishDomainEvents)
            await _eventBus.PublishAsync(new BulkExportStartedEvent
            {
                DatabaseId = databaseId,
                TableNames = tables,
                Format = format.ToString(),
                OperationId = operationId
            }, cancellationToken);

        try
        {
            var output = new StringBuilder();

            // Process tables with bounded concurrency via IBatchProcessor.
            // Each table result contributes its serialised data to the output buffer.
            var tableResults = await _batchProcessor.ProcessAsync(
                tables,
                async tableName =>
                {
                    using var tableConn = await OpenConnectionAsync(databaseId, cancellationToken);
                    var data = await ExportTableDataAsync(tableConn, tableName, format, opts, cancellationToken);
                    return (tableName, data);
                },
                maxConcurrency: _options.MaxConcurrentTables);

            foreach (var item in tableResults.SuccessfulResults)
            {
                output.AppendLine(item.data);
                var rowCount = EstimateRowCount(item.data, format);
                allRows += rowCount;

                if (_options.EnableProgressReporting)
                    progress?.Report(new ExportProgress(item.tableName, rowCount, rowCount, 0));
            }

            foreach (var error in tableResults.Errors)
                warnings.Add($"Table export failed [{error.ItemId}]: {error.Message}");

            var artifact = await PersistArtifactAsync(output.ToString(), databaseId, format, opts);
            var duration = DateTime.UtcNow - started;

            if (_options.PublishDomainEvents)
                await _eventBus.PublishAsync(new BulkExportCompletedEvent
                {
                    DatabaseId = databaseId,
                    RowsExported = allRows,
                    TablesExported = tableResults.SuccessCount,
                    DurationMs = (long)duration.TotalMilliseconds,
                    OutputPath = artifact,
                    OperationId = operationId
                }, cancellationToken);

            _logger.LogInformation(
                "BulkExport completed: database={DatabaseId}, rows={Rows}, tables={Tables}, ms={Ms}",
                databaseId, allRows, tableResults.SuccessCount, (long)duration.TotalMilliseconds);

            return new BulkExportResult
            {
                IsSuccess = true,
                TablesProcessed = tables,
                TotalRowsExported = allRows,
                OutputPath = artifact,
                Duration = duration,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - started;

            if (_options.PublishDomainEvents)
                await _eventBus.PublishAsync(new BulkExportFailedEvent
                {
                    DatabaseId = databaseId,
                    ErrorMessage = ex.Message,
                    OperationId = operationId
                }, CancellationToken.None);

            _logger.LogError(ex, "BulkExport failed: database={DatabaseId}, op={OperationId}",
                databaseId, operationId);

            return new BulkExportResult
            {
                IsSuccess = false,
                TablesProcessed = tables,
                TotalRowsExported = allRows,
                Duration = duration,
                ErrorMessage = ex.Message,
                Warnings = warnings
            };
        }
    }

    /// <inheritdoc />
    public async Task<BulkExportResult> ExportTableAsync(
        string databaseId,
        string tableName,
        BulkDataFormat format,
        ExportOptions? options = null,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        var opts = options ?? new ExportOptions();
        var started = DateTime.UtcNow;

        using var connection = await OpenConnectionAsync(databaseId, cancellationToken);
        var data = await ExportTableDataAsync(connection, tableName, format, opts, cancellationToken);
        var rowCount = EstimateRowCount(data, format);
        var artifact = await PersistArtifactAsync(data, databaseId, format, opts);
        var duration = DateTime.UtcNow - started;

        if (_options.EnableProgressReporting)
            progress?.Report(new ExportProgress(tableName, rowCount, rowCount, 0));

        _logger.LogInformation("ExportTable completed: table={Table}, rows={Rows}, ms={Ms}",
            tableName, rowCount, (long)duration.TotalMilliseconds);

        return new BulkExportResult
        {
            IsSuccess = true,
            TablesProcessed = [tableName],
            TotalRowsExported = rowCount,
            OutputPath = artifact,
            Duration = duration
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ExportBatch> StreamExportAsync(
        string databaseId,
        string tableName,
        BulkDataFormat format,
        int batchSize = 1_000,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        if (batchSize <= 0) batchSize = _options.DefaultBatchSize;

        using var connection = await OpenConnectionAsync(databaseId, cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM [{tableName}]";

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var fieldCount = reader.FieldCount;
        var fieldNames = Enumerable.Range(0, fieldCount)
            .Select(i => reader.GetName(i))
            .ToArray();

        var buffer = new List<Dictionary<string, object?>>(batchSize);
        var sequence = 0;

        while (await reader.ReadAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = new Dictionary<string, object?>(fieldCount);
            for (var i = 0; i < fieldCount; i++)
                row[fieldNames[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);

            buffer.Add(row);

            if (buffer.Count < batchSize) continue;

            yield return new ExportBatch(
                tableName,
                SerialiseRows(buffer, format, fieldNames),
                buffer.Count,
                sequence++,
                IsLastBatch: false);

            buffer.Clear();
        }

        // Terminal batch — always emitted so consumers can detect end-of-stream.
        yield return new ExportBatch(
            tableName,
            buffer.Count > 0 ? SerialiseRows(buffer, format, fieldNames) : string.Empty,
            buffer.Count,
            sequence,
            IsLastBatch: true);
    }

    /// <inheritdoc />
    public async Task<BulkImportResult> ImportTableAsync(
        string databaseId,
        string tableName,
        Stream dataStream,
        BulkDataFormat format,
        ImportOptions? options = null,
        IProgress<ImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentNullException.ThrowIfNull(dataStream);

        var opts = options ?? new ImportOptions();
        var operationId = Guid.NewGuid().ToString();
        var started = DateTime.UtcNow;

        if (_options.PublishDomainEvents)
            await _eventBus.PublishAsync(new BulkImportStartedEvent
            {
                DatabaseId = databaseId,
                TableNames = [tableName],
                Format = format.ToString(),
                OperationId = operationId
            }, cancellationToken);

        try
        {
            using var reader = new StreamReader(dataStream, leaveOpen: true);
            var payload = await reader.ReadToEndAsync(cancellationToken);

            using var connection = await OpenConnectionAsync(databaseId, cancellationToken);
            var rowsImported = await ImportPayloadAsync(connection, tableName, payload, format, opts, cancellationToken);
            var duration = DateTime.UtcNow - started;

            if (_options.EnableProgressReporting)
                progress?.Report(new ImportProgress(tableName, rowsImported, 0, 0));

            if (_options.PublishDomainEvents)
                await _eventBus.PublishAsync(new BulkImportCompletedEvent
                {
                    DatabaseId = databaseId,
                    RowsImported = rowsImported,
                    RowsFailed = 0,
                    DurationMs = (long)duration.TotalMilliseconds,
                    OperationId = operationId
                }, cancellationToken);

            _logger.LogInformation("ImportTable completed: table={Table}, rows={Rows}, ms={Ms}",
                tableName, rowsImported, (long)duration.TotalMilliseconds);

            return new BulkImportResult
            {
                IsSuccess = true,
                TablesProcessed = [tableName],
                TotalRowsImported = rowsImported,
                TotalRowsFailed = 0,
                Duration = duration
            };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - started;

            if (_options.PublishDomainEvents)
                await _eventBus.PublishAsync(new BulkImportFailedEvent
                {
                    DatabaseId = databaseId,
                    ErrorMessage = ex.Message,
                    OperationId = operationId
                }, CancellationToken.None);

            _logger.LogError(ex, "ImportTable failed: database={DatabaseId}, table={Table}",
                databaseId, tableName);

            return new BulkImportResult
            {
                IsSuccess = false,
                TablesProcessed = [tableName],
                TotalRowsImported = 0,
                TotalRowsFailed = 0,
                Duration = duration,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<BulkImportResult> StreamImportAsync(
        string databaseId,
        IAsyncEnumerable<ImportBatch> batches,
        ImportOptions? options = null,
        IProgress<ImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentNullException.ThrowIfNull(batches);

        var opts = options ?? new ImportOptions();
        var operationId = Guid.NewGuid().ToString();
        var started = DateTime.UtcNow;
        var totalImported = 0L;
        var totalFailed = 0L;
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();

        _logger.LogInformation("StreamImport starting: database={DatabaseId}, op={OperationId}",
            databaseId, operationId);

        if (_options.PublishDomainEvents)
            await _eventBus.PublishAsync(new BulkImportStartedEvent
            {
                DatabaseId = databaseId,
                TableNames = [],
                Format = "Streaming",
                OperationId = operationId
            }, cancellationToken);

        using var connection = await OpenConnectionAsync(databaseId, cancellationToken);

        try
        {
            await foreach (var batch in batches.WithCancellation(cancellationToken))
            {
                tables.Add(batch.TableName);

                try
                {
                    var imported = await ImportPayloadAsync(
                        connection, batch.TableName, batch.Data, batch.Format, opts, cancellationToken);

                    totalImported += imported;

                    if (_options.EnableProgressReporting)
                        progress?.Report(new ImportProgress(
                            batch.TableName, totalImported, totalFailed, batch.SequenceNumber));

                    _logger.LogDebug(
                        "StreamImport batch {Seq} processed: table={Table}, rows={Rows}",
                        batch.SequenceNumber, batch.TableName, imported);
                }
                catch (Exception ex) when (opts.SkipFailedRows)
                {
                    totalFailed++;
                    var warning = $"Batch {batch.SequenceNumber} for table '{batch.TableName}' failed: {ex.Message}";
                    warnings.Add(warning);
                    _logger.LogWarning(ex, "StreamImport: skipping failed batch {Seq} for {Table}",
                        batch.SequenceNumber, batch.TableName);
                }
            }

            var duration = DateTime.UtcNow - started;

            if (_options.PublishDomainEvents)
                await _eventBus.PublishAsync(new BulkImportCompletedEvent
                {
                    DatabaseId = databaseId,
                    RowsImported = totalImported,
                    RowsFailed = totalFailed,
                    DurationMs = (long)duration.TotalMilliseconds,
                    OperationId = operationId
                }, cancellationToken);

            _logger.LogInformation(
                "StreamImport completed: database={DatabaseId}, rows={Rows}, failed={Failed}, ms={Ms}",
                databaseId, totalImported, totalFailed, (long)duration.TotalMilliseconds);

            return new BulkImportResult
            {
                IsSuccess = true,
                TablesProcessed = [.. tables],
                TotalRowsImported = totalImported,
                TotalRowsFailed = totalFailed,
                Duration = duration,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - started;

            if (_options.PublishDomainEvents)
                await _eventBus.PublishAsync(new BulkImportFailedEvent
                {
                    DatabaseId = databaseId,
                    ErrorMessage = ex.Message,
                    RowsFailedBeforeAbort = totalFailed,
                    OperationId = operationId
                }, CancellationToken.None);

            _logger.LogError(ex, "StreamImport failed: database={DatabaseId}", databaseId);

            return new BulkImportResult
            {
                IsSuccess = false,
                TablesProcessed = [.. tables],
                TotalRowsImported = totalImported,
                TotalRowsFailed = totalFailed,
                Duration = duration,
                ErrorMessage = ex.Message,
                Warnings = warnings
            };
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<SQLiteConnection> OpenConnectionAsync(string databaseId, CancellationToken ct)
    {
        var path = Path.Combine(_options.BaseDatabasePath, $"{databaseId}.db");
        var connStr = $"Data Source={path};Version=3;";
        var connection = new SQLiteConnection(connStr);
        await connection.OpenAsync(ct);
        return connection;
    }

    private static async Task<List<string>> GetTableNamesAsync(SQLiteConnection connection, CancellationToken ct)
    {
        var tables = new List<string>();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            tables.Add(reader.GetString(0));
        return tables;
    }

    private async Task<string> ExportTableDataAsync(
        SQLiteConnection connection,
        string tableName,
        BulkDataFormat format,
        ExportOptions opts,
        CancellationToken ct) =>
        format switch
        {
            BulkDataFormat.Csv => await _exporter.ExportAsCsvAsync(connection, tableName, opts.IncludeCsvHeaders),
            BulkDataFormat.Sql => await _exporter.ExportAsSqlAsync(connection, tableName),
            _ => await _exporter.ExportAsJsonAsync(connection, tableName, opts.IncludeMetadata)
        };

    private async Task<long> ImportPayloadAsync(
        SQLiteConnection connection,
        string tableName,
        string payload,
        BulkDataFormat format,
        ImportOptions opts,
        CancellationToken ct) =>
        format switch
        {
            BulkDataFormat.Csv => await _importer.ImportFromCsvAsync(
                connection, tableName, payload,
                opts.CsvHasHeaders, opts.CsvDelimiter, opts.TruncateBeforeImport),
            BulkDataFormat.Sql => await _importer.ImportFromSqlAsync(connection, payload),
            _ => await _importer.ImportFromJsonAsync(connection, tableName, payload, opts.TruncateBeforeImport)
        };

    private static string SerialiseRows(
        List<Dictionary<string, object?>> rows,
        BulkDataFormat format,
        string[] fieldNames) =>
        format switch
        {
            BulkDataFormat.Csv => SerialiseCsv(rows, fieldNames),
            BulkDataFormat.Sql => SerialiseSql(rows, fieldNames),
            _ => JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = false })
        };

    private static string SerialiseCsv(List<Dictionary<string, object?>> rows, string[] fieldNames)
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            for (var i = 0; i < fieldNames.Length; i++)
            {
                if (i > 0) sb.Append(',');
                var val = row.TryGetValue(fieldNames[i], out var v) ? v?.ToString() ?? string.Empty : string.Empty;
                if (val.Contains(',') || val.Contains('"') || val.Contains('\n'))
                    val = $"\"{val.Replace("\"", "\"\"")}\"";
                sb.Append(val);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string SerialiseSql(List<Dictionary<string, object?>> rows, string[] fieldNames)
    {
        var sb = new StringBuilder();
        var cols = string.Join(", ", fieldNames.Select(f => $"[{f}]"));
        foreach (var row in rows)
        {
            var vals = string.Join(", ", fieldNames.Select(f =>
            {
                var v = row.TryGetValue(f, out var val) ? val : null;
                return v is null ? "NULL" : $"'{v.ToString()!.Replace("'", "''")}'";
            }));
            sb.AppendLine($"INSERT INTO [{rows[0].Keys.First()}] ({cols}) VALUES ({vals});");
        }
        return sb.ToString();
    }

    private static long EstimateRowCount(string data, BulkDataFormat format) =>
        format switch
        {
            BulkDataFormat.Csv => Math.Max(0, data.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length - 1),
            BulkDataFormat.Sql => data.Split("INSERT INTO", StringSplitOptions.RemoveEmptyEntries).Length - 1,
            _ => data.Count(c => c == '{')
        };

    private async Task<string?> PersistArtifactAsync(
        string content, string databaseId, BulkDataFormat format, ExportOptions opts)
    {
        if (string.IsNullOrWhiteSpace(opts.OutputFilePath) &&
            string.IsNullOrWhiteSpace(_options.DefaultExportDirectory))
            return null;

        var ext = format switch { BulkDataFormat.Csv => "csv", BulkDataFormat.Sql => "sql", _ => "json" };
        var dir = opts.OutputFilePath is not null
            ? Path.GetDirectoryName(opts.OutputFilePath)!
            : _options.DefaultExportDirectory;

        Directory.CreateDirectory(dir);

        var path = opts.OutputFilePath
            ?? Path.Combine(dir, $"{databaseId}_{DateTime.UtcNow:yyyyMMddHHmmss}.{ext}");

        await File.WriteAllTextAsync(path, content);
        _logger.LogInformation("Export artifact persisted: {Path}", path);
        return path;
    }
}
