// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Events;

/// <summary>
/// Raised when a bulk export operation begins.
/// Enables monitoring dashboards and audit trails to record operation start times.
/// </summary>
public class BulkExportStartedEvent : DomainEvent
{
    /// <summary>Identifier of the source database being exported.</summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>Names of the tables included in the export job.</summary>
    public IReadOnlyList<string> TableNames { get; set; } = [];

    /// <summary>Requested output format (Json, Csv, Sql).</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>Unique correlation token linking start, complete, and failed events.</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <inheritdoc />
    public BulkExportStartedEvent() : base(nameof(BulkExportStartedEvent)) { }
}

/// <summary>
/// Raised when a bulk export operation finishes successfully.
/// Carries final statistics for metrics collection and audit logging.
/// </summary>
public class BulkExportCompletedEvent : DomainEvent
{
    /// <summary>Identifier of the exported database.</summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>Total rows written across all exported tables.</summary>
    public long RowsExported { get; set; }

    /// <summary>Number of tables included in this export.</summary>
    public int TablesExported { get; set; }

    /// <summary>Wall-clock duration of the operation in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>Optional file system path to the export artifact.</summary>
    public string? OutputPath { get; set; }

    /// <summary>Correlation token matching <see cref="BulkExportStartedEvent.OperationId"/>.</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <inheritdoc />
    public BulkExportCompletedEvent() : base(nameof(BulkExportCompletedEvent)) { }
}

/// <summary>
/// Raised when a bulk export operation terminates with an unrecoverable error.
/// Triggers alerting and allows downstream systems to initiate recovery.
/// </summary>
public class BulkExportFailedEvent : DomainEvent
{
    /// <summary>Identifier of the database that failed to export.</summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>Human-readable error description.</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Correlation token matching <see cref="BulkExportStartedEvent.OperationId"/>.</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <inheritdoc />
    public BulkExportFailedEvent() : base(nameof(BulkExportFailedEvent)) { }
}

/// <summary>
/// Raised when a bulk import operation begins.
/// Enables monitoring systems to track in-progress write operations.
/// </summary>
public class BulkImportStartedEvent : DomainEvent
{
    /// <summary>Identifier of the target database receiving the import.</summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>Names of the tables that will be written during this import.</summary>
    public IReadOnlyList<string> TableNames { get; set; } = [];

    /// <summary>Input data format (Json, Csv, Sql).</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>Unique correlation token linking start, complete, and failed events.</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <inheritdoc />
    public BulkImportStartedEvent() : base(nameof(BulkImportStartedEvent)) { }
}

/// <summary>
/// Raised when a bulk import operation finishes successfully.
/// Carries final row counts for metrics collection and audit logging.
/// </summary>
public class BulkImportCompletedEvent : DomainEvent
{
    /// <summary>Identifier of the database that received the import.</summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>Total rows successfully inserted across all tables.</summary>
    public long RowsImported { get; set; }

    /// <summary>Total rows rejected or skipped due to errors.</summary>
    public long RowsFailed { get; set; }

    /// <summary>Wall-clock duration of the operation in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>Correlation token matching <see cref="BulkImportStartedEvent.OperationId"/>.</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <inheritdoc />
    public BulkImportCompletedEvent() : base(nameof(BulkImportCompletedEvent)) { }
}

/// <summary>
/// Raised when a bulk import operation terminates with an unrecoverable error.
/// Triggers alerting and allows downstream systems to initiate recovery or retry.
/// </summary>
public class BulkImportFailedEvent : DomainEvent
{
    /// <summary>Identifier of the target database where the import failed.</summary>
    public string DatabaseId { get; set; } = string.Empty;

    /// <summary>Human-readable error description.</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Rows that were rejected before the fatal error occurred.</summary>
    public long RowsFailedBeforeAbort { get; set; }

    /// <summary>Correlation token matching <see cref="BulkImportStartedEvent.OperationId"/>.</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <inheritdoc />
    public BulkImportFailedEvent() : base(nameof(BulkImportFailedEvent)) { }
}
