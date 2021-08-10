// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.BulkOperations;

/// <summary>
/// High-level contract for async streaming bulk import and export operations on tenant databases.
/// </summary>
/// <remarks>
/// <para>
/// All export overloads support an <see cref="IProgress{T}"/> callback so callers can relay
/// progress to UI components or monitoring systems without blocking the pipeline.
/// </para>
/// <para>
/// The streaming variants (<see cref="StreamExportAsync"/> and <see cref="StreamImportAsync"/>)
/// use <see cref="IAsyncEnumerable{T}"/> to avoid materialising full result sets in memory,
/// making them suitable for arbitrarily large tables.
/// </para>
/// <para>
/// All operations honour the supplied <see cref="CancellationToken"/> and can be safely
/// cancelled at any batch boundary without leaving the database in an inconsistent state.
/// </para>
/// </remarks>
public interface IBulkDataService
{
    // ── Export ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Exports every user table in the specified database to a single serialised artifact.
    /// Tables are processed in parallel up to <see cref="BulkDataOptions.MaxConcurrentTables"/>.
    /// </summary>
    /// <param name="databaseId">Logical identifier of the source database.</param>
    /// <param name="format">Output serialisation format.</param>
    /// <param name="options">Optional per-call overrides; uses global defaults when <c>null</c>.</param>
    /// <param name="progress">
    /// Optional callback invoked after each batch.  Receives an <see cref="ExportProgress"/>
    /// snapshot with cumulative statistics for the table currently being exported.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation at the next safe boundary.</param>
    /// <returns>
    /// A <see cref="BulkExportResult"/> containing aggregate statistics and,
    /// when an output path was configured, the path to the persisted artifact.
    /// </returns>
    Task<BulkExportResult> ExportDatabaseAsync(
        string databaseId,
        BulkDataFormat format,
        ExportOptions? options = null,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a single table from the specified database.
    /// </summary>
    /// <param name="databaseId">Logical identifier of the source database.</param>
    /// <param name="tableName">Name of the table to export.</param>
    /// <param name="format">Output serialisation format.</param>
    /// <param name="options">Optional per-call overrides.</param>
    /// <param name="progress">Optional per-batch progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="BulkExportResult"/> with statistics for the exported table.</returns>
    Task<BulkExportResult> ExportTableAsync(
        string databaseId,
        string tableName,
        BulkDataFormat format,
        ExportOptions? options = null,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams an export as a lazy sequence of <see cref="ExportBatch"/> objects.
    /// Each batch contains at most <paramref name="batchSize"/> serialised rows.
    /// The final batch for a table always has <see cref="ExportBatch.IsLastBatch"/> set to
    /// <c>true</c>, allowing the consumer to finalise the output channel.
    /// </summary>
    /// <param name="databaseId">Logical identifier of the source database.</param>
    /// <param name="tableName">Name of the table to stream.</param>
    /// <param name="format">Output serialisation format for batch payloads.</param>
    /// <param name="batchSize">Maximum rows per emitted batch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async sequence of batches in ascending <see cref="ExportBatch.SequenceNumber"/> order.</returns>
    IAsyncEnumerable<ExportBatch> StreamExportAsync(
        string databaseId,
        string tableName,
        BulkDataFormat format,
        int batchSize = 1_000,
        CancellationToken cancellationToken = default);

    // ── Import ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Imports data from a <see cref="Stream"/> into a single target table.
    /// The entire stream is read and applied within a single database transaction
    /// unless <see cref="ImportOptions.CommitEveryNRows"/> is configured.
    /// </summary>
    /// <param name="databaseId">Logical identifier of the target database.</param>
    /// <param name="tableName">Name of the table receiving the data.</param>
    /// <param name="dataStream">Readable stream containing the serialised payload.</param>
    /// <param name="format">Serialisation format of <paramref name="dataStream"/>.</param>
    /// <param name="options">Optional per-call overrides.</param>
    /// <param name="progress">Optional per-batch progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="BulkImportResult"/> with row-level statistics.</returns>
    Task<BulkImportResult> ImportTableAsync(
        string databaseId,
        string tableName,
        Stream dataStream,
        BulkDataFormat format,
        ImportOptions? options = null,
        IProgress<ImportProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes an async sequence of pre-partitioned <see cref="ImportBatch"/> objects,
    /// writing each to its designated target table.
    /// Suitable for multi-table imports delivered by an upstream producer (e.g., a network
    /// relay or a transformation pipeline) without requiring full in-memory buffering.
    /// </summary>
    /// <param name="databaseId">Logical identifier of the target database.</param>
    /// <param name="batches">
    /// Async sequence of import batches.  Batches for different tables may be interleaved;
    /// each batch carries its own <see cref="ImportBatch.TableName"/> and
    /// <see cref="ImportBatch.Format"/>.
    /// </param>
    /// <param name="options">Optional per-call overrides applied to all batches.</param>
    /// <param name="progress">Optional per-batch progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="BulkImportResult"/> aggregating statistics across all consumed batches.
    /// </returns>
    Task<BulkImportResult> StreamImportAsync(
        string databaseId,
        IAsyncEnumerable<ImportBatch> batches,
        ImportOptions? options = null,
        IProgress<ImportProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
