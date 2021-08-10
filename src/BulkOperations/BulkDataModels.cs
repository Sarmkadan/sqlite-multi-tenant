// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.BulkOperations;

/// <summary>
/// Supported serialisation formats for bulk import and export operations.
/// </summary>
public enum BulkDataFormat
{
    /// <summary>JSON array or newline-delimited JSON objects.</summary>
    Json = 0,

    /// <summary>Comma-separated values with an optional header row.</summary>
    Csv = 1,

    /// <summary>Raw SQL INSERT statements compatible with SQLite.</summary>
    Sql = 2
}

/// <summary>
/// Strategy applied when an incoming record's primary key already exists in the target table.
/// </summary>
public enum DuplicateStrategy
{
    /// <summary>Abort the whole batch immediately on the first duplicate.</summary>
    Abort = 0,

    /// <summary>Skip the conflicting row and continue processing remaining records.</summary>
    Skip = 1,

    /// <summary>Overwrite the existing row with the incoming data.</summary>
    Replace = 2
}

/// <summary>
/// Snapshot of export progress emitted at the end of each processed batch.
/// Consumers may forward this to UI progress bars or monitoring dashboards.
/// </summary>
/// <param name="TableName">Table currently being exported.</param>
/// <param name="RowsProcessed">Cumulative rows exported for this table so far.</param>
/// <param name="TotalRowsEstimate">
/// Estimated total row count for this table; <c>-1</c> when the exact count is unavailable.
/// </param>
/// <param name="BatchSequence">Zero-based index of the batch just completed.</param>
public sealed record ExportProgress(
    string TableName,
    long RowsProcessed,
    long TotalRowsEstimate,
    int BatchSequence)
{
    /// <summary>
    /// Completion percentage in the range [0, 100].
    /// Returns <c>-1</c> when <see cref="TotalRowsEstimate"/> is unknown.
    /// </summary>
    public double PercentComplete => TotalRowsEstimate > 0
        ? Math.Min(100.0, RowsProcessed / (double)TotalRowsEstimate * 100.0)
        : -1;
}

/// <summary>
/// Snapshot of import progress emitted at the end of each processed batch.
/// </summary>
/// <param name="TableName">Table currently being written.</param>
/// <param name="RowsImported">Cumulative rows successfully inserted so far.</param>
/// <param name="RowsFailed">Cumulative rows rejected or skipped so far.</param>
/// <param name="BatchSequence">Zero-based index of the batch just completed.</param>
public sealed record ImportProgress(
    string TableName,
    long RowsImported,
    long RowsFailed,
    int BatchSequence)
{
    /// <summary>Total rows attempted in this operation so far.</summary>
    public long TotalAttempted => RowsImported + RowsFailed;

    /// <summary>
    /// Success rate as a value in [0, 1]; <c>1</c> when no rows have been attempted.
    /// </summary>
    public double SuccessRate => TotalAttempted > 0
        ? (double)RowsImported / TotalAttempted
        : 1.0;
}

/// <summary>
/// A single batch of serialised rows produced by the streaming export pipeline.
/// Batches are emitted in sequence and can be persisted, forwarded, or re-assembled
/// by the consumer without buffering the full result set.
/// </summary>
/// <param name="TableName">Source table name.</param>
/// <param name="Data">Serialised payload for this batch (format is caller-chosen).</param>
/// <param name="RowCount">Number of data rows encoded in <paramref name="Data"/>.</param>
/// <param name="SequenceNumber">Zero-based monotonically increasing index.</param>
/// <param name="IsLastBatch">
/// <c>true</c> for the terminal batch of a table, allowing consumers to finalise
/// the target file or channel.
/// </param>
public sealed record ExportBatch(
    string TableName,
    string Data,
    int RowCount,
    int SequenceNumber,
    bool IsLastBatch);

/// <summary>
/// A single batch of serialised rows submitted to the streaming import pipeline.
/// Produced by external callers (e.g., network readers or file parsers) and consumed
/// by <see cref="IBulkDataService.StreamImportAsync"/>.
/// </summary>
/// <param name="TableName">Target table for this batch.</param>
/// <param name="Data">Serialised payload containing the rows to insert.</param>
/// <param name="Format">Data format of <paramref name="Data"/>.</param>
/// <param name="SequenceNumber">Zero-based ordering index.</param>
/// <param name="IsLastBatch"><c>true</c> signals no more batches will follow for this table.</param>
public sealed record ImportBatch(
    string TableName,
    string Data,
    BulkDataFormat Format,
    int SequenceNumber,
    bool IsLastBatch);

/// <summary>
/// Aggregated statistics returned after a bulk export operation completes or fails.
/// </summary>
public sealed record BulkExportResult
{
    /// <summary>Indicates that the export completed without a fatal error.</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>Ordered list of tables that were processed during the export.</summary>
    public required IReadOnlyList<string> TablesProcessed { get; init; }

    /// <summary>Total rows written across all tables.</summary>
    public required long TotalRowsExported { get; init; }

    /// <summary>Optional path to the persisted export artifact on disk.</summary>
    public string? OutputPath { get; init; }

    /// <summary>Wall-clock time consumed by the entire export operation.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Non-fatal warnings collected during export (e.g., null column values).</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>Error message populated when <see cref="IsSuccess"/> is <c>false</c>.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Aggregated statistics returned after a bulk import operation completes or fails.
/// </summary>
public sealed record BulkImportResult
{
    /// <summary>Indicates that the import completed without a fatal error.</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>Ordered list of tables that received data during the import.</summary>
    public required IReadOnlyList<string> TablesProcessed { get; init; }

    /// <summary>Total rows successfully inserted across all tables.</summary>
    public required long TotalRowsImported { get; init; }

    /// <summary>Total rows rejected or skipped due to validation or constraint errors.</summary>
    public required long TotalRowsFailed { get; init; }

    /// <summary>Wall-clock time consumed by the entire import operation.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Non-fatal warnings collected during import.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>Error message populated when <see cref="IsSuccess"/> is <c>false</c>.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Per-call export tuning parameters layered on top of global <see cref="BulkDataOptions"/>.
/// All properties have sensible defaults; override only what differs from the global policy.
/// </summary>
public sealed class ExportOptions
{
    /// <summary>Number of rows to include in each streamed batch.</summary>
    public int BatchSize { get; set; } = 1_000;

    /// <summary>
    /// Optional SQL WHERE predicate appended to the SELECT statement for filtered exports.
    /// Example: <c>"status = 'active' AND created_at &gt; '2024-01-01'"</c>.
    /// </summary>
    public string? WhereClause { get; set; }

    /// <summary>Prepend CREATE TABLE DDL to SQL-format exports.</summary>
    public bool IncludeSchema { get; set; }

    /// <summary>Wrap JSON output in a metadata envelope (table name, row count, timestamp).</summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>Emit column headers in the first row of CSV exports.</summary>
    public bool IncludeCsvHeaders { get; set; } = true;

    /// <summary>
    /// Absolute or relative path where the export file is persisted.
    /// When <c>null</c> the service uses <see cref="BulkDataOptions.DefaultExportDirectory"/>.
    /// </summary>
    public string? OutputFilePath { get; set; }
}

/// <summary>
/// Per-call import tuning parameters layered on top of global <see cref="BulkDataOptions"/>.
/// </summary>
public sealed class ImportOptions
{
    /// <summary>Delete all existing rows in the target table before inserting incoming data.</summary>
    public bool TruncateBeforeImport { get; set; }

    /// <summary>Log and skip individual rows that fail validation or constraint checks.</summary>
    public bool SkipFailedRows { get; set; } = true;

    /// <summary>Behaviour when an incoming row's primary key already exists in the target.</summary>
    public DuplicateStrategy DuplicateHandling { get; set; } = DuplicateStrategy.Abort;

    /// <summary>
    /// Commit the open transaction after every N rows.
    /// Set to <c>0</c> to use a single transaction for the entire batch.
    /// </summary>
    public int CommitEveryNRows { get; set; }

    /// <summary>Field separator used when parsing CSV payloads.</summary>
    public string CsvDelimiter { get; set; } = ",";

    /// <summary>Whether the first line of a CSV payload contains column names.</summary>
    public bool CsvHasHeaders { get; set; } = true;
}
