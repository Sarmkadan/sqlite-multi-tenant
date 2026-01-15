#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.BulkOperations;

/// <summary>
/// Global configuration for the async bulk import/export subsystem.
/// Register via <c>services.Configure&lt;BulkDataOptions&gt;(config.GetSection("BulkData"))</c>
/// or supply an <c>Action&lt;BulkDataOptions&gt;</c> to
/// <see cref="BulkDataExtensions.AddBulkDataServices"/>.
/// Individual operations can override these values through
/// <see cref="ExportOptions"/> or <see cref="ImportOptions"/>.
/// </summary>
public sealed class BulkDataOptions
{
    /// <summary>
    /// Default number of rows included in each streaming batch.
    /// Larger values reduce round-trips; smaller values reduce peak memory usage.
    /// </summary>
    public int DefaultBatchSize { get; set; } = 1_000;

    /// <summary>
    /// Maximum number of tables processed concurrently during a multi-table export or import.
    /// Increasing this value trades higher memory and I/O pressure for lower total wall-clock time.
    /// </summary>
    public int MaxConcurrentTables { get; set; } = 3;

    /// <summary>
    /// Maximum in-memory serialisation buffer in bytes before the service flushes to the output stream.
    /// Prevents unbounded memory growth on very wide tables.
    /// </summary>
    public int MaxBufferSizeBytes { get; set; } = 10_000_000;

    /// <summary>
    /// Maximum wall-clock time allowed for a single bulk operation before it is cancelled.
    /// </summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// When <c>true</c>, the service publishes <c>BulkExport*</c> and <c>BulkImport*</c>
    /// domain events via the registered <c>IEventBus</c>.
    /// Disable to reduce overhead in scenarios where event handling is not required.
    /// </summary>
    public bool PublishDomainEvents { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, the service calls <see cref="IProgress{T}"/> callbacks after each batch.
    /// Disable to eliminate the (minimal) overhead in fire-and-forget pipelines.
    /// </summary>
    public bool EnableProgressReporting { get; set; } = true;

    /// <summary>
    /// Base directory used as the output location when no explicit file path is supplied
    /// to an export operation. Relative paths are resolved from the application base directory.
    /// </summary>
    public string DefaultExportDirectory { get; set; } = "./exports";

    /// <summary>
    /// Base directory containing tenant SQLite database files.
    /// The service resolves a database connection string as
    /// <c>{BaseDatabasePath}/{databaseId}.db</c>.
    /// Must match the value in <c>MultiTenantOptions.BasePath</c>.
    /// </summary>
    public string BaseDatabasePath { get; set; } = "./databases";
}
