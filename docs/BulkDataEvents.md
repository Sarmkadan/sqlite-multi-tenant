# Bulk data events

[`BulkDataEvents.cs`](../src/Events/BulkDataEvents.cs) declares six sealed domain-event classes used to report the lifecycle of bulk exports and imports. All six inherit from `DomainEvent`; there are no record declarations in this file.

`BulkDataService` publishes the events only when `BulkDataOptions.PublishDomainEvents` is `true`. Every operation creates one `OperationId` and copies it to its started, completed, and failed events so consumers can correlate them.

## Properties shared through `DomainEvent`

Each event also exposes these inherited properties:

| Property | Type | Description |
| --- | --- | --- |
| `EventId` | `string` | Read-only GUID string generated when the event instance is created. |
| `OccurredAt` | `DateTime` | Read-only UTC time captured when the event instance is created. |
| `EventType` | `string` | Read-only event class name supplied by the event's parameterless constructor. |
| `TenantId` | `string?` | Optional tenant identifier. `BulkDataService` does not set it when publishing these events. |

## Export events

### `BulkExportStartedEvent`

Signals that a database export is about to process its tables. See the dedicated [BulkExportStartedEvent documentation](BulkExportStartedEvent.md) for additional usage notes.

| Property | Type | Description |
| --- | --- | --- |
| `DatabaseId` | `string` | Identifier of the source database. |
| `TableNames` | `IReadOnlyList<string>` | Names discovered for export. |
| `Format` | `string` | Requested `BulkDataFormat`, converted with `ToString()`. |
| `OperationId` | `string` | Correlation identifier for this export. |

`ExportDatabaseAsync` publishes this event after it opens the database and discovers the table names, but before it exports any table data. The publication occurs before the method's export `try` block and uses the caller's cancellation token.

### `BulkExportCompletedEvent`

Signals that a database export completed successfully.

| Property | Type | Description |
| --- | --- | --- |
| `DatabaseId` | `string` | Identifier of the exported database. |
| `RowsExported` | `long` | Estimated total number of rows in successful table results. |
| `TablesExported` | `int` | Number of tables successfully processed by the batch processor. |
| `DurationMs` | `long` | Elapsed wall-clock time in milliseconds. |
| `OutputPath` | `string?` | Path returned after the export artifact is persisted. |
| `OperationId` | `string` | Correlation identifier matching the started event. |

`ExportDatabaseAsync` publishes this event after table processing and artifact persistence, immediately before logging and returning a successful `BulkExportResult`. It uses the caller's cancellation token. Individual table failures collected by the batch processor become warnings and do not prevent this event from being published for the remaining successful work.

### `BulkExportFailedEvent`

Signals that a fatal error terminated a database export.

| Property | Type | Description |
| --- | --- | --- |
| `DatabaseId` | `string` | Identifier of the database being exported. |
| `ErrorMessage` | `string` | Message from the exception caught by the service. |
| `OperationId` | `string` | Correlation identifier matching the started event. |

`ExportDatabaseAsync` publishes this event from its catch block when table processing, artifact persistence, or another operation inside the export `try` block throws. It publishes with `CancellationToken.None`, then returns a failed `BulkExportResult`. Errors opening the initial connection or discovering table names occur before that `try` block, so they do not cause this event to be published.

`ExportTableAsync` and `StreamExportAsync` do not publish any of the export events declared in this file.

## Import events

### `BulkImportStartedEvent`

Signals that an import is about to begin.

| Property | Type | Description |
| --- | --- | --- |
| `DatabaseId` | `string` | Identifier of the target database. |
| `TableNames` | `IReadOnlyList<string>` | Names of the tables expected at publication time. |
| `Format` | `string` | Input format description. |
| `OperationId` | `string` | Correlation identifier for this import. |

The service publishes this event in two methods, using the caller's cancellation token:

- `ImportTableAsync` publishes it before reading the input stream or opening the database. `TableNames` contains the requested table name and `Format` is the requested `BulkDataFormat` converted with `ToString()`.
- `StreamImportAsync` publishes it before opening the database or enumerating any batches. Because no batches have been observed yet, `TableNames` is empty and `Format` is `"Streaming"`.

In both methods, started-event publication occurs before the method's processing `try` block.

### `BulkImportCompletedEvent`

Signals that an import completed successfully.

| Property | Type | Description |
| --- | --- | --- |
| `DatabaseId` | `string` | Identifier of the database that received the import. |
| `RowsImported` | `long` | Total rows successfully imported. |
| `RowsFailed` | `long` | Total failed or skipped rows tracked by the operation. |
| `DurationMs` | `long` | Elapsed wall-clock time in milliseconds. |
| `OperationId` | `string` | Correlation identifier matching the started event. |

The service publishes this event in two methods, using the caller's cancellation token:

- `ImportTableAsync` publishes it after importing the payload and reporting progress, before returning success. `RowsFailed` is always `0` on this path.
- `StreamImportAsync` publishes it after all batches have been enumerated. `RowsImported` and `RowsFailed` contain the accumulated totals; when `SkipFailedRows` is enabled, skipped batch failures contribute to `RowsFailed` without preventing completion.

### `BulkImportFailedEvent`

Signals that a fatal error terminated an import.

| Property | Type | Description |
| --- | --- | --- |
| `DatabaseId` | `string` | Identifier of the target database. |
| `ErrorMessage` | `string` | Message from the exception caught by the service. |
| `RowsFailedBeforeAbort` | `long` | Number of failures already accumulated before a streaming import aborts. Defaults to `0`. |
| `OperationId` | `string` | Correlation identifier matching the started event. |

The service publishes this event with `CancellationToken.None` in two catch blocks:

- `ImportTableAsync` publishes it when reading the input, opening the connection, or importing the payload throws inside its `try` block. It leaves `RowsFailedBeforeAbort` at its default value of `0`.
- `StreamImportAsync` publishes it when batch enumeration or processing terminates with an exception not handled by `SkipFailedRows`. It sets `RowsFailedBeforeAbort` to the accumulated failure count.

`StreamImportAsync` opens its database connection after publishing the started event but before entering its `try` block. Consequently, a connection-open failure does not publish `BulkImportFailedEvent`.

## Publication summary

| Service method | Started | Completed | Failed |
| --- | --- | --- | --- |
| `ExportDatabaseAsync` | `BulkExportStartedEvent` | `BulkExportCompletedEvent` | `BulkExportFailedEvent` |
| `ExportTableAsync` | None | None | None |
| `StreamExportAsync` | None | None | None |
| `ImportTableAsync` | `BulkImportStartedEvent` | `BulkImportCompletedEvent` | `BulkImportFailedEvent` |
| `StreamImportAsync` | `BulkImportStartedEvent` | `BulkImportCompletedEvent` | `BulkImportFailedEvent` |
