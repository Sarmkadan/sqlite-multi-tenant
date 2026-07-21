# TenantSizeReportRecord

A data transfer object that encapsulates size and layout statistics for a single tenant’s SQLite database within a multi‑tenant environment. It aggregates metrics such as total size, page counts, WAL size, and free‑list information, and provides helpers for rendering the data as plain‑text tables or summaries.

## API

### TenantId  
**Type:** `string`  
**Purpose:** Identifier of the tenant whose database statistics are recorded.  
**Return Value:** The tenant ID string; may be `null` if not set.  
**Throws:** None.

### TenantName  
**Type:** `string`  
**Purpose:** Human‑readable name of the tenant.  
**Return Value:** The tenant name string; may be `null` if not set.  
**Throws:** None.

### DatabasePath  
**Type:** `string`  
**Purpose:** Filesystem path to the tenant’s SQLite database file.  
**Return Value:** The absolute or relative path string; may be `null` if unknown.  
**Throws:** None.

### SizeBytes  
**Type:** `long`  
**Purpose:** Total allocated size of the database file in bytes.  
**Return Value:** Non‑negative byte count.  
**Throws:** None.

### PageCount  
**Type:** `long`  
**Purpose:** Number of pages in the database file.  
**Return Value:** Non‑negative page count.  
**Throws:** None.

### PageSize  
**Type:** `int`  
**Purpose:** Size of a single database page in bytes (typically 1024, 2048, or 4096).  
**Return Value:** Positive page size.  
**Throws:** None.

### FreeListCount  
**Type:** `long`  
**Purpose:** Number of pages currently on the free‑list (available for reuse).  
**Return Value:** Non‑negative count of free pages.  
**Throws:** None.

### WalSizeBytes  
**Type:** `long`  
**Purpose:** Size of the Write‑Ahead Log (WAL) file associated with the database, in bytes.  
**Return Value:** Non‑negative byte count; zero if WAL mode is not used.  
**Throws:** None.

### FileSizeBytes  
**Type:** `long`  
**Purpose:** Combined size of the main database file and any auxiliary files (e.g., WAL, SHM).  
**Return Value:** Non‑negative byte count.  
**Throws:** None.

### ToTextTableRow  
**Signature:** `public string ToTextTableRow()`  
**Purpose:** Produces a single line of formatted text suitable for inclusion in a plain‑text table report.  
**Parameters:** None.  
**Return Value:** A string where each column is separated by a pipe (`|`) character, containing the values of `TenantId`, `TenantName`, `SizeBytes`, `PageCount`, `PageSize`, `FreeListCount`, `WalSizeBytes`, and `FileSizeBytes`.  
**Throws:** `InvalidOperationException` if any required field (`TenantId` or `DatabasePath`) is `null`, rendering the row meaningless.

### GetTextTableHeader  
**Signature:** `public static string GetTextTableHeader()`  
**Purpose:** Returns the header line for the text table produced by `ToTextTableRow`.  
**Parameters:** None.  
**Return Value:** A string containing column names separated by pipes (`|`), e.g., `TenantId|TenantName|SizeBytes|...`.  
**Throws:** None.

### GetTextTableFooter  
**Signature:** `public static string GetTextTableFooter()`  
**Purpose:** Returns the footer line for the text table (often a separator or summary line).  
**Parameters:** None.  
**Return Value:** A string suitable for appending after the last data row; commonly a series of dashes matching column widths.  
**Throws:** None.

### GetSummaryReport  
**Signature:** `public static string GetSummaryReport()`  
**Purpose:** Generates a concise summary report across all tenant records known to the caller (the method relies on external state populated prior to invocation).  
**Parameters:** None.  
**Return Value:** A multi‑line string containing aggregate totals such as total size, average page size, and counts of tenants with non‑zero WAL files.  
**Throws:** `InvalidOperationException` if the internal collection of records has not been initialized or is empty.

### CompareTo  
**Signature:** `public int CompareTo(TenantSizeReportRecord other)`  
**Purpose:** Implements ordering logic for `TenantSizeReportRecord`, typically comparing by `SizeBytes` descending.  
**Parameters:**  
- `other`: The `TenantSizeReportRecord` instance to compare against; must not be `null`.  
**Return Value:**  
- A negative integer if this instance is smaller than `other`.  
- Zero if they are considered equal.  
- A positive integer if this instance is larger than `other`.  
**Throws:** `ArgumentNullException` if `other` is `null`.

## Usage

### Example 1: Building a text table report
```csharp
using System;
using System.Collections.Generic;
using System.Linq;

// Assume a list of TenantSizeReportRecord objects has been filled elsewhere.
List<TenantSizeReportRecord> records = GetTenantSizeRecords();

Console.WriteLine(TenantSizeReportRecord.GetTextTableHeader());
foreach (var rec in records)
{
    Console.WriteLine(rec.ToTextTableRow());
}
Console.WriteLine(TenantSizeReportRecord.GetTextTableFooter());
```

### Example 2: Obtaining a summary and sorting records
```csharp
using System;
using System.Collections.Generic;

List<TenantSizeReportRecord> records = GetTenantSizeRecords();

// Sort records by size (largest first) using the implemented CompareTo.
records.Sort((x, y) => y.CompareTo(x)); // descending order

string summary = TenantSizeReportRecord.GetSummaryReport();
Console.WriteLine("=== Tenant Size Summary ===");
Console.WriteLine(summary);

Console.WriteLine("\nSorted records:");
foreach (var rec in records)
{
    Console.WriteLine($"{rec.TenantId}: {rec.SizeBytes} bytes");
}
```

## Notes

- The `ToTextTableRow` method expects `TenantId` and `DatabasePath` to be non‑`null`; otherwise it throws an `InvalidOperationException` to prevent malformed table output.  
- `GetSummaryReport` relies on external state (e.g., a static list or a service) that must be populated before the method is called; invoking it with no data available results in an `InvalidOperationException`.  
- The `CompareTo` implementation assumes that `other` is a valid `TenantSizeReportRecord`; passing `null` is not permitted and will trigger an `ArgumentNullException`.  
- All numeric properties (`SizeBytes`, `PageCount`, `PageSize`, `FreeListCount`, `WalSizeBytes`, `FileSizeBytes`) are intended to be non‑negative; negative values indicate a programming error and are not guarded by the type itself.  
- The type contains only mutable fields; therefore, instances are **not** thread‑safe for concurrent writes. Concurrent read‑only access after initialization is safe, but any thread that modifies a record should synchronize access or use immutable copies.  
- No inheritance or interfaces are indicated by the supplied members; the type is a plain data class with utility methods.  

---
