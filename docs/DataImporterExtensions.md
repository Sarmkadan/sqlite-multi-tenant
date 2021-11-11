# DataImporterExtensions

Provides asynchronous extension methods for importing structured data from files into a SQLite multi-tenant database. These utilities handle common import formats—JSON, CSV, and SQL—while offering table validation and dynamic schema creation to ensure the target structure exists before data is written.

## API

### ImportFromJsonFileAsync

```csharp
public static async Task<int> ImportFromJsonFileAsync(this DbConnection connection, string filePath, string tableName, CancellationToken cancellationToken = default)
```

Reads a JSON array from the specified file and inserts each element as a row into the named table. The JSON structure is expected to be a flat array of objects whose property names match the target table's column names.

**Parameters:**
- `connection` — the open database connection to operate on.
- `filePath` — absolute or relative path to the JSON file.
- `tableName` — the target table into which rows are inserted.
- `cancellationToken` — token to observe for cancellation requests.

**Returns:** the number of rows successfully inserted.

**Throws:**
- `FileNotFoundException` when `filePath` does not exist.
- `InvalidOperationException` when the JSON file contains malformed content or the array elements are not objects.
- `DatabaseAccessException` when the table does not exist or a column mismatch occurs during insertion.

---

### ImportFromCsvFileAsync

```csharp
public static async Task<int> ImportFromCsvFileAsync(this DbConnection connection, string filePath, string tableName, bool hasHeader = true, CancellationToken cancellationToken = default)
```

Parses a CSV file and bulk-inserts the rows into the specified table. When `hasHeader` is `true`, the first line is treated as column names and must correspond to the table's columns; otherwise, columns are mapped positionally.

**Parameters:**
- `connection` — the open database connection.
- `filePath` — path to the CSV file.
- `tableName` — target table for the imported rows.
- `hasHeader` — whether the first row contains column headers (default `true`).
- `cancellationToken` — token to observe for cancellation.

**Returns:** the number of rows inserted.

**Throws:**
- `FileNotFoundException` when the CSV file is missing.
- `InvalidOperationException` when the CSV structure is inconsistent (e.g., varying column counts).
- `DatabaseAccessException` when the table does not exist or a column mismatch occurs.

---

### ImportFromSqlFileAsync

```csharp
public static async Task<int> ImportFromSqlFileAsync(this DbConnection connection, string filePath, CancellationToken cancellationToken = default)
```

Executes a file containing one or more SQL statements separated by semicolons. Statements are executed sequentially within a single transaction. The file is expected to contain only valid SQL compatible with the underlying SQLite provider.

**Parameters:**
- `connection` — the open database connection.
- `filePath` — path to the SQL file.
- `cancellationToken` — token to observe for cancellation.

**Returns:** the total number of rows affected across all statements.

**Throws:**
- `FileNotFoundException` when the SQL file does not exist.
- `DatabaseAccessException` when any statement fails to execute, rolling back the entire import.

---

### ValidateTableExistsAsync

```csharp
public static async Task<bool> ValidateTableExistsAsync(this DbConnection connection, string tableName, CancellationToken cancellationToken = default)
```

Checks whether a table with the given name exists in the database. This is a lightweight existence check that queries the schema metadata.

**Parameters:**
- `connection` — the open database connection.
- `tableName` — the table name to verify.
- `cancellationToken` — token to observe for cancellation.

**Returns:** `true` if the table exists; otherwise `false`.

**Throws:** does not throw under normal circumstances. Exceptions may propagate from the underlying connection if it is closed or disposed.

---

### CreateTableIfNotExistsAsync

```csharp
public static async Task<bool> CreateTableIfNotExistsAsync(this DbConnection connection, string tableName, string columnDefinitions, CancellationToken cancellationToken = default)
```

Creates a table with the specified column definitions if it does not already exist. The `columnDefinitions` parameter is a raw SQL fragment containing the column names, types, and constraints (e.g., `"Id INTEGER PRIMARY KEY, Name TEXT NOT NULL"`).

**Parameters:**
- `connection` — the open database connection.
- `tableName` — the name of the table to create.
- `columnDefinitions` — SQL fragment defining the columns.
- `cancellationToken` — token to observe for cancellation.

**Returns:** `true` if the table was created by this call; `false` if it already existed.

**Throws:**
- `ArgumentException` when `columnDefinitions` is null, empty, or whitespace.
- `DatabaseAccessException` when the SQL syntax is invalid or the connection is in an unusable state.

## Usage

### Example 1: Importing JSON after ensuring the table exists

```csharp
using var connection = new SqliteConnection("Data Source=tenant.db");
await connection.OpenAsync();

string tableName = "Customers";
bool exists = await connection.ValidateTableExistsAsync(tableName);

if (!exists)
{
    await connection.CreateTableIfNotExistsAsync(
        tableName,
        "Id INTEGER PRIMARY KEY, Name TEXT, Email TEXT");
}

int rows = await connection.ImportFromJsonFileAsync(
    "customers.json",
    tableName);

Console.WriteLine($"Imported {rows} customer records.");
```

### Example 2: Importing CSV with a header row

```csharp
using var connection = new SqliteConnection("Data Source=tenant.db");
await connection.OpenAsync();

// Ensure the target schema is present before importing.
await connection.CreateTableIfNotExistsAsync(
    "Orders",
    "OrderId INTEGER PRIMARY KEY, Product TEXT, Quantity INTEGER, Price REAL");

int rows = await connection.ImportFromCsvFileAsync(
    "orders.csv",
    "Orders",
    hasHeader: true);

Console.WriteLine($"Imported {rows} orders from CSV.");
```

## Notes

- All import methods operate on an already-open connection. The caller is responsible for opening and disposing the connection; these extensions do not manage connection lifetime.
- `ImportFromSqlFileAsync` wraps all statements in a single transaction. If any statement fails, the entire import is rolled back, leaving the database unchanged.
- `ImportFromJsonFileAsync` and `ImportFromCsvFileAsync` do not automatically create tables. Use `CreateTableIfNotExistsAsync` beforehand to avoid `DatabaseAccessException` due to missing tables.
- `CreateTableIfNotExistsAsync` uses the `CREATE TABLE IF NOT EXISTS` SQLite syntax, making it idempotent and safe to call multiple times with the same definition.
- These methods are not thread-safe by themselves. Concurrent calls on the same connection may interfere with each other. If multiple imports must run in parallel, use separate connections or serialize access externally.
- Large files are read and processed asynchronously, but the underlying SQLite library serializes writes. For very large imports, consider batching or using a single dedicated connection to avoid contention.
