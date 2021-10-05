# SchemaManager

SchemaManager provides asynchronous helpers for evolving the SQLite database schema in a multi‑tenant application. It encapsulates common DDL operations such as ensuring baseline tables exist, adding columns, renaming tables, creating indexes, and enumerating existing tables, all while surfacing errors through exceptions and boolean success flags where appropriate.

## API

### SchemaManager()
Parameterless constructor that creates a new SchemaManager instance. The instance must be associated with a valid SQLite connection before any schema operations are invoked (typically via dependency injection or property configuration).

### InitializeSchemaAsync()
Ensures that the baseline schema required by the application is present in the connected database.  
- **Return value:** Completes when the schema has been verified or created.  
- **Exceptions:** Throws `SQLiteException` if the connection fails, or `InvalidOperationException` if the schema cannot be initialized due to conflicting objects or insufficient privileges.

### AddColumnAsync(...)
Attempts to add a column to a specified table.  
- **Parameters:** Identifiers for the target table, column name, column type, and any additional constraints (exact signature defined in the source).  
- **Return value:** `true` if the column was added, `false` if the column already existed or no change was made.  
- **Exceptions:** Throws `SQLiteException` on syntax errors, missing table, or other execution failures.

### RenameTableAsync(...)
Renames an existing table to a new name.  
- **Parameters:** Current table name and desired new table name.  
- **Return value:** Completes when the rename operation has been executed.  
- **Exceptions:** Throws `SQLiteException` if the source table does not exist, the target name is already in use, or the operation is not supported by the SQLite version.

### CreateIndexAsync(...)
Creates an index on one or more columns of a table.  
- **Parameters:** Table name, index name, column list, and options such as uniqueness (exact signature defined in the source).  
- **Return value:** `true` if the index was created, `false` if an index with the same definition already existed.  
- **Exceptions:** Throws `SQLiteException` for invalid table/column names, malformed index definition, or execution errors.

### GetTablesAsync()
Retrieves the names of all user‑defined tables in the database.  
- **Return value:** A `Task<List<string>>` that yields a list of table names.  
- **Exceptions:** Throws `SQLiteException` if the connection is unavailable or the query fails.

## Usage

```csharp
using var connection = new SqliteConnection("Data Source=tenant.db");
var schemaMgr = new SchemaManager();
// Assume connection is opened and assigned internally via DI or property setup.

// Ensure baseline tables exist before any tenant‑specific work.
await schemaMgr.InitializeSchemaAsync();

// Add a nullable TEXT column named 'Description' to the 'Orders' table if it does not already exist.
bool added = await schemaMgr.AddColumnAsync(
    tableName: "Orders",
    columnName: "Description",
    columnType: "TEXT",
    nullable: true);
if (added)
{
    Console.WriteLine("Column added.");
}
else
{
    Console.WriteLine("Column already present.");
}
```

```csharp
using var connection = new SqliteConnection("Data Source=tenant.db");
var schemaMgr = new SchemaManager();
// Connection handling of connection setup omitted for brevity

// Rename a legacy table to conform to a new naming convention.
await schemaMgr.RenameTableAsync(
    oldName: "LegacyUsers",
    newName: "Users");

// Create a unique index on the email column to enforce uniqueness per tenant.
bool created = await schemaMgr.CreateIndexAsync(
    tableName: "Users",
    indexName: "IX_Users_Email",
    columns: new[] { "Email" },
    isUnique: true);
if (created)
{
    Console.WriteLine("Unique index created.");
}

// List all tables to verify schema state.
IReadOnlyList<string> tables = await schemaMgr.GetTablesAsync();
foreach (var t in tables)
{
    Console.WriteLine(t);
}
```

## Notes

- The class does **not** manage the lifetime of the underlying `IDbConnection`; callers must open, close, or dispose the connection as appropriate for their application’s lifecycle.  
- Schema modification methods are **not thread‑safe**; concurrent invocations against the same instance may lead to race conditions or SQLite locking errors. Serialize access externally if needed.  
- Certain ALTER TABLE operations (e.g., dropping a column) are not supported by SQLite; attempting to emulate such changes via this API will result in exceptions.  
- `InitializeSchemaAsync` should be called once per connection before any other schema method; invoking it repeatedly is safe but may incur unnecessary overhead.  
- Return values of `false` from `AddColumnAsync` and `CreateIndexAsync` indicate that the requested object already matched the desired definition, not that an error occurred.  
- Errors related to foreign key constraints, missing tables, or invalid SQL are propagated as `SQLiteException`; callers should handle or log them according to their error‑handling policy.
