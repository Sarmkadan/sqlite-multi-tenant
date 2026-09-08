# Bulk Data Models

The types in `src/BulkOperations/BulkDataModels.cs` describe formats, duplicate handling, progress updates, streaming batches, operation results, and per-export settings for bulk data operations. They are declared in the `SqliteMultiTenant.BulkOperations` namespace.

## `BulkDataFormat`

`BulkDataFormat` selects the serialized representation used by a bulk import or export.

| Value | Numeric value | Description |
| --- | ---: | --- |
| `Json` | `0` | A JSON array or newline-delimited JSON objects. |
| `Csv` | `1` | Comma-separated values, optionally including a header row. |
| `Sql` | `2` | Raw SQLite-compatible `INSERT` statements. |

## `DuplicateStrategy`

`DuplicateStrategy` controls how imports respond when an incoming primary key already exists in the target table.

| Value | Numeric value | Behavior |
| --- | ---: | --- |
| `Abort` | `0` | Stops the whole batch immediately at the first duplicate. |
| `Skip` | `1` | Skips the conflicting row and continues with the remaining records. |
| `Replace` | `2` | Overwrites the existing row with the incoming data. |

## Progress records

### `ExportProgress`

`ExportProgress` is an immutable snapshot emitted after an export batch has been processed.

```csharp
public sealed record ExportProgress(
    string TableName,
    long RowsProcessed,
    long TotalRowsEstimate,
    int BatchSequence)
```

- `TableName`: table currently being exported.
- `RowsProcessed`: cumulative number of rows exported for the table.
- `TotalRowsEstimate`: estimated total row count, or `-1` when an exact count is unavailable.
- `BatchSequence`: zero-based index of the batch that just completed.
- `PercentComplete`: computed as `RowsProcessed / TotalRowsEstimate * 100` when `TotalRowsEstimate` is greater than zero. The result is capped at `100`; when the estimate is zero or negative, it returns `-1`.

### `ImportProgress`

`ImportProgress` is an immutable snapshot emitted after an import batch has been processed.

```csharp
public sealed record ImportProgress(
    string TableName,
    long RowsImported,
    long RowsFailed,
    int BatchSequence)
```

- `TableName`: table currently being written.
- `RowsImported`: cumulative number of successfully inserted rows.
- `RowsFailed`: cumulative number of rejected or skipped rows.
- `BatchSequence`: zero-based index of the batch that just completed.
- `TotalAttempted`: computed as `RowsImported + RowsFailed`.
- `SuccessRate`: the fraction of attempted rows that were imported, in the range `0` to `1`. It is computed as `RowsImported / TotalAttempted`, or returns `1.0` when no rows have been attempted.

## Streaming batch records

### `ExportBatch`

`ExportBatch` represents one serialized chunk produced by the streaming export pipeline.

```csharp
public sealed record ExportBatch(
    string TableName,
    string Data,
    int RowCount,
    int SequenceNumber,
    bool IsLastBatch);
```

- `TableName`: source table name.
- `Data`: serialized payload in the caller-selected format.
- `RowCount`: number of data rows encoded in `Data`.
- `SequenceNumber`: zero-based, monotonically increasing batch index.
- `IsLastBatch`: `true` for the terminal batch of a table.

### `ImportBatch`

`ImportBatch` represents one serialized chunk submitted to the streaming import pipeline.

```csharp
public sealed record ImportBatch(
    string TableName,
    string Data,
    BulkDataFormat Format,
    int SequenceNumber,
    bool IsLastBatch);
```

- `TableName`: target table name.
- `Data`: serialized payload containing rows to import.
- `Format`: format used by `Data`.
- `SequenceNumber`: zero-based ordering index.
- `IsLastBatch`: `true` when no more batches will follow for the table.

## Result records

### `BulkExportResult`

`BulkExportResult` contains aggregate export statistics.

| Property | Type | Required/default | Description |
| --- | --- | --- | --- |
| `IsSuccess` | `bool` | Required | Whether the export completed without a fatal error. |
| `TablesProcessed` | `IReadOnlyList<string>` | Required | Ordered table names processed by the export. |
| `TotalRowsExported` | `long` | Required | Total rows written across all tables. |
| `OutputPath` | `string?` | `null` | Path to a persisted export artifact, when one exists. |
| `Duration` | `TimeSpan` | Required | Wall-clock duration of the operation. |
| `Warnings` | `IReadOnlyList<string>` | Empty list | Non-fatal warnings collected during export. |
| `ErrorMessage` | `string?` | `null` | Error detail when `IsSuccess` is `false`. |

### `BulkImportResult`

`BulkImportResult` contains aggregate import statistics.

| Property | Type | Required/default | Description |
| --- | --- | --- | --- |
| `IsSuccess` | `bool` | Required | Whether the import completed without a fatal error. |
| `TablesProcessed` | `IReadOnlyList<string>` | Required | Ordered table names that received data. |
| `TotalRowsImported` | `long` | Required | Total successfully inserted rows across all tables. |
| `TotalRowsFailed` | `long` | Required | Total rows rejected or skipped because of validation or constraint errors. |
| `Duration` | `TimeSpan` | Required | Wall-clock duration of the operation. |
| `Warnings` | `IReadOnlyList<string>` | Empty list | Non-fatal warnings collected during import. |
| `ErrorMessage` | `string?` | `null` | Error detail when `IsSuccess` is `false`. |

All result properties are init-only, so they are assigned during object initialization rather than changed afterward.

## `ExportOptions`

`ExportOptions` supplies per-call export settings layered over global `BulkDataOptions` configuration.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `BatchSize` | `int` | `1000` | Number of rows placed in each streamed batch. |
| `WhereClause` | `string?` | `null` | Optional SQL `WHERE` predicate used to filter exported rows. |
| `IncludeSchema` | `bool` | `false` | Prepends `CREATE TABLE` DDL to SQL-format exports when enabled. |
| `IncludeMetadata` | `bool` | `true` | Wraps JSON output in a metadata envelope when enabled. |
| `IncludeCsvHeaders` | `bool` | `true` | Emits column headers in the first CSV row when enabled. |
| `OutputFilePath` | `string?` | `null` | Export destination; when omitted, the service uses `BulkDataOptions.DefaultExportDirectory`. |

Example:

```csharp
var options = new ExportOptions
{
    BatchSize = 1000,
    WhereClause = "status = 'active'",
    IncludeMetadata = true
};
```
