# DataConsistencyChecker

`DataConsistencyChecker` is a sealed utility class that performs integrity and structural validation across a multi-tenant SQLite database. It verifies physical database integrity, detects duplicate records, validates expected row counts, and surfaces orphaned records, foreign key violations, missing indexes, and per-table statistics in a single consolidated result.

## API

### DataConsistencyChecker

```csharp
public DataConsistencyChecker()
```

Default constructor. Creates a new instance of the checker. No initialization parameters are required; connection or context details are expected to be supplied externally or resolved internally by the implementation.

### CheckDatabaseIntegrityAsync

```csharp
public async Task<ConsistencyCheckResult> CheckDatabaseIntegrityAsync()
```

Runs a full consistency check across the database. This includes a physical integrity check (equivalent to SQLite’s `PRAGMA integrity_check`), detection of orphaned records, foreign key constraint violations, missing indexes, and collection of per-table row counts and statistics.

**Returns** a `ConsistencyCheckResult` containing all findings, with `CheckedAt` set to the UTC timestamp at completion.

**Exceptions** may be thrown if the underlying database connection is unavailable, the database file is locked, or a query fails unexpectedly. Specific exception types depend on the data access layer in use.

### FindDuplicatesAsync

```csharp
public async Task<List<DuplicateRecord>> FindDuplicatesAsync()
```

Scans the database for duplicate records based on predefined or configured uniqueness criteria. The exact columns and tables examined are determined by the implementation’s internal rules.

**Returns** a list of `DuplicateRecord` instances, each describing a set of rows considered duplicates. Returns an empty list if no duplicates are found.

**Exceptions** may be thrown under the same conditions as `CheckDatabaseIntegrityAsync`.

### ValidateRecordCountsAsync

```csharp
public async Task<bool> ValidateRecordCountsAsync()
```

Compares actual row counts in monitored tables against expected thresholds or reference counts. The definition of “expected” is implementation-specific (e.g., configuration-driven or derived from tenant metadata).

**Returns** `true` if all monitored tables meet their expected counts; `false` if one or more tables deviate.

**Exceptions** may be thrown under the same conditions as `CheckDatabaseIntegrityAsync`.

### ConsistencyCheckResult

```csharp
public sealed class ConsistencyCheckResult
```

Aggregates the outcome of a full consistency check.

| Member | Type | Description |
|---|---|---|
| `IsHealthy` | `bool` | Composite flag indicating whether the database is considered healthy. Typically `true` when `IntegrityCheckPassed` is `true` and no violations, orphans, or missing indexes are present. |
| `IntegrityCheckPassed` | `bool` | Result of the physical integrity check. `true` if `PRAGMA integrity_check` returned no errors. |
| `OrphanedRecords` | `List<string>` | Descriptions of orphaned rows (child records with no corresponding parent). Each string identifies the table and row involved. |
| `ForeignKeyViolations` | `List<ConstraintViolation>` | Detailed list of foreign key constraint violations found. |
| `MissingIndexes` | `List<string>` | Names or descriptions of indexes expected but not present in the schema. |
| `TableStatistics` | `Dictionary<string, TableStatistics>` | Per-table statistics keyed by table name. |
| `CheckedAt` | `DateTime` | UTC timestamp when the check completed. |

### ConstraintViolation

```csharp
public sealed class ConstraintViolation
```

Describes a single foreign key violation.

| Member | Type | Description |
|---|---|---|
| `Table` | `string` | The child table containing the violating row. |
| `Rowid` | `long` | The `rowid` of the violating row. |
| `ParentTable` | `string` | The referenced parent table. |
| `ParentRowid` | `long` | The expected `rowid` in the parent table that was not found. |

### TableStatistics

```csharp
public sealed class TableStatistics
```

Holds basic row-count information for a single table.

| Member | Type | Description |
|---|---|---|
| `TableName` | `string` | The name of the table. |

Additional members (e.g., row count) are not exposed in the public surface documented here; the implementation may compute and expose them through internal pathways.

## Usage

### Example 1: Full Integrity Check

```csharp
var checker = new DataConsistencyChecker();
ConsistencyCheckResult result = await checker.CheckDatabaseIntegrityAsync();

if (!result.IsHealthy)
{
    Console.WriteLine($"Integrity check failed at {result.CheckedAt:O}");

    foreach (var orphan in result.OrphanedRecords)
        Console.WriteLine($"Orphan: {orphan}");

    foreach (var violation in result.ForeignKeyViolations)
        Console.WriteLine($"FK violation: {violation.Table} rowid {violation.Rowid} -> {violation.ParentTable} rowid {violation.ParentRowid}");

    foreach (var missingIndex in result.MissingIndexes)
        Console.WriteLine($"Missing index: {missingIndex}");
}
else
{
    Console.WriteLine("Database is healthy.");
    foreach (var kvp in result.TableStatistics)
        Console.WriteLine($"Table {kvp.Key}: statistics recorded.");
}
```

### Example 2: Duplicate Detection and Count Validation

```csharp
var checker = new DataConsistencyChecker();

List<DuplicateRecord> duplicates = await checker.FindDuplicatesAsync();
if (duplicates.Any())
{
    Console.WriteLine($"Found {duplicates.Count} duplicate groups.");
    // Process or report duplicates
}

bool countsValid = await checker.ValidateRecordCountsAsync();
if (!countsValid)
{
    Console.WriteLine("Record count mismatch detected. Initiating full consistency check.");
    var fullResult = await checker.CheckDatabaseIntegrityAsync();
    // Inspect fullResult.TableStatistics for per-table details
}
```

## Notes

- **Thread safety:** `DataConsistencyChecker` is a sealed class with no exposed mutable state. Individual async methods are assumed to be safe for concurrent calls only if the underlying database connection and access layer support concurrent read operations. No internal synchronization is implied by the public signatures.
- **Orphaned records:** The `OrphanedRecords` list contains human-readable strings. Parsing logic should not rely on a fixed format; treat these as diagnostic messages.
- **Missing indexes:** The criteria for “missing” indexes are implementation-defined. An empty list does not guarantee an optimal indexing strategy—only that no known required indexes are absent.
- **`TableStatistics`:** Only `TableName` is publicly visible. Row counts and other metrics are stored internally and surfaced through the dictionary’s presence; consumers needing exact counts should consult the implementation’s extended surface or logs.
- **`DuplicateRecord`:** The return type of `FindDuplicatesAsync` is `List<DuplicateRecord>`, but `DuplicateRecord` itself is not documented in the public surface shown here. Its structure is defined elsewhere in the project.
- **`ValidateRecordCountsAsync`:** Returns a simple boolean. When `false`, follow up with `CheckDatabaseIntegrityAsync` to obtain per-table statistics and diagnose which tables deviated.
- **Timestamps:** `CheckedAt` is set at the end of the check operation, not the start. It reflects the completion time in UTC.
