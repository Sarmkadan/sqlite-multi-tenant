# ExportProgress

Represents the outcome of a database export operation in `sqlite-multi-tenant`. This record provides detailed metrics about the export process, including success status, tables processed, row counts, duration, warnings, and errors. It is used to report progress and results to calling code, enabling programmatic handling of export outcomes.

## API

### `IsSuccess`
- **Purpose**: Indicates whether the export operation completed successfully.
- **Type**: `bool` (required)
- **Remarks**: `true` if the export completed without critical errors; `false` if the operation failed or encountered unrecoverable issues. Does not reflect partial success (e.g., some tables exported while others failed).

### `TablesProcessed`
- **Purpose**: Lists the names of all tables included in the export operation.
- **Type**: `IReadOnlyList<string>` (required)
- **Remarks**: Contains the names of tables attempted during export, regardless of success. Empty if no tables were processed.

### `TotalRowsExported`
- **Purpose**: Reports the total number of rows successfully exported across all tables.
- **Type**: `long` (required)
- **Remarks**: Includes only rows written to the output without errors. Does not account for rows skipped due to filtering or failures.

### `OutputPath`
- **Purpose**: Specifies the filesystem path where the exported data was written.
- **Type**: `string?`
- **Remarks**: `null` if the export failed before writing output or if the operation did not generate a file (e.g., in-memory exports). Path format is platform-dependent.

### `Duration`
- **Purpose**: Measures the total time taken to complete the export operation.
- **Type**: `TimeSpan` (required)
- **Remarks**: Includes time spent on all phases of the export (e.g., querying, writing). Does not account for delays introduced by external factors (e.g., filesystem latency).

### `Warnings`
- **Purpose**: Lists non-critical issues encountered during the export.
- **Type**: `IReadOnlyList<string>`
- **Remarks**: Warnings may include skipped rows, schema mismatches, or recoverable errors. Empty if no warnings were generated.

### `ErrorMessage`
- **Purpose**: Provides a human-readable description of the primary error, if the export failed.
- **Type**: `string?`
- **Remarks**: `null` if `IsSuccess` is `true`. May contain technical details (e.g., exception messages) for debugging.

---

### Related Types

#### `ImportProgress`
Represents the outcome of a database import operation, with analogous members to `ExportProgress`:
- `IsSuccess`: `bool` (required)
- `TablesProcessed`: `IReadOnlyList<string>` (required)
- `TotalRowsImported`: `long` (required)
- `TotalRowsFailed`: `long` (required)
- `Duration`: `TimeSpan` (required)
- `Warnings`: `IReadOnlyList<string>`
- `ErrorMessage`: `string?`

#### `ExportBatch`
Represents a batch of rows during export, used for chunked operations:
- `BatchSize`: `int` – Number of rows in the batch.
- `WhereClause`: `string?` – SQL filter applied to the batch (e.g., `WHERE id BETWEEN 100 AND 200`).

#### `ImportBatch`
Represents a batch of rows during import, with the same members as `ExportBatch`.

## Usage

### Example 1: Basic Export Handling
