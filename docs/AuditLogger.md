# AuditLogger

`AuditLogger` is the in-memory implementation of [`IAuditLogger`](IAuditLogger.md). It records audit events, supports filtered queries and retention cleanup, and exposes summary statistics. Access to its internal entry collection is serialized with a semaphore.

The logger retains at most 10,000 entries. When that limit is exceeded, it removes the oldest entries first. Entries are not persisted and are lost when the application process ends.

## Construction

```csharp
var auditLogger = new AuditLogger(logger);
```

The constructor requires an `ILogger<AuditLogger>` used to report logged events and purge results.

## Methods

### `LogAsync(AuditLogEntry entry)`

Adds an entry to the audit log. Before storing it, the method replaces `Id` with a new GUID string and sets `Timestamp` to the current UTC time. If the in-memory limit is exceeded, the oldest entries are removed.

### `GetEntriesAsync(AuditLogFilter filter)`

Returns matching entries ordered from newest to oldest, limited by `filter.Limit`.

The method applies these optional filters:

- `EventType`, `Actor`, and `ResourceId` use exact, case-sensitive equality.
- `StartTime` is inclusive (`Timestamp >= StartTime`).
- `EndTime` is inclusive (`Timestamp <= EndTime`).
- `SearchTerm` performs a case-insensitive substring search on `Description`.

### `GetEntryCountAsync(AuditLogFilter filter)`

Returns the number of entries matching `EventType`, `Actor`, `StartTime`, and `EndTime`. Unlike `GetEntriesAsync`, this method does not apply `ResourceId`, `SearchTerm`, or `Limit`.

### `PurgeOldEntriesAsync(TimeSpan retentionPeriod)`

Removes entries whose timestamps are earlier than `DateTime.UtcNow - retentionPeriod`. Entries exactly at the cutoff are retained. The number removed is written to the application logger.

### `GetStatisticsAsync()`

Returns an `AuditLogStatistics` snapshot for the current in-memory collection. This method is available on `AuditLogger`, but is not currently declared by `IAuditLogger`.

## AuditLogEntry

`AuditLogEntry` represents a recorded audit event.

| Field | Type | Description |
| --- | --- | --- |
| `Id` | `string` | Entry identifier. `LogAsync` assigns a new GUID string. |
| `Timestamp` | `DateTime` | Time the entry was recorded. `LogAsync` assigns the current UTC time. |
| `EventType` | `string` | Application-defined event category. |
| `Actor` | `string` | User, service, or process responsible for the event. |
| `ResourceId` | `string` | Identifier of the affected resource. |
| `ResourceType` | `string` | Application-defined type of the affected resource. |
| `Description` | `string` | Human-readable event description. |
| `Action` | `AuditAction` | Standard action performed by the event. |
| `Changes` | `Dictionary<string, object>` | Application-defined changed values or other event details. |
| `IpAddress` | `string` | Originating IP address, when known. |
| `TenantId` | `string` | Identifier of the tenant associated with the event. |

String fields default to `string.Empty`, and `Changes` defaults to an empty dictionary.

## AuditAction

`AuditAction` identifies the general operation represented by an entry:

- `Create`
- `Read`
- `Update`
- `Delete`
- `Execute`
- `Export`
- `Import`

## AuditLogFilter

`AuditLogFilter` supplies criteria for entry queries.

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `EventType` | `string?` | `null` | Exact event type to match. |
| `Actor` | `string?` | `null` | Exact actor to match. |
| `ResourceId` | `string?` | `null` | Exact resource identifier to match in `GetEntriesAsync`. |
| `StartTime` | `DateTime?` | `null` | Inclusive lower timestamp bound. |
| `EndTime` | `DateTime?` | `null` | Inclusive upper timestamp bound. |
| `SearchTerm` | `string?` | `null` | Case-insensitive text to find in descriptions in `GetEntriesAsync`. |
| `Limit` | `int` | `100` | Maximum number of entries returned by `GetEntriesAsync`. |

Null or empty string filters are ignored. `Limit` does not affect `GetEntryCountAsync`.

## AuditLogStatistics

`AuditLogStatistics` summarizes the current entries.

| Field | Type | Description |
| --- | --- | --- |
| `TotalEntries` | `int` | Total number of retained entries. |
| `UniqueActors` | `int` | Number of distinct actor strings. |
| `UniqueEventTypes` | `int` | Number of distinct event type strings. |
| `OldestEntry` | `DateTime?` | Timestamp of the first retained entry, or `null` when empty. |
| `NewestEntry` | `DateTime?` | Timestamp of the last retained entry, or `null` when empty. |

Because entries are appended in logging order and timestamps are assigned by `LogAsync`, the first and last retained entries ordinarily represent the oldest and newest records.
