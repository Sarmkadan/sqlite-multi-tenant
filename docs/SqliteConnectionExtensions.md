# SqliteConnectionExtensions

`SqliteConnectionExtensions` provides synchronous helpers for inspecting and updating SQLite database metadata.

Despite the class name, these are **static helper methods**, not C# extension methods. Each method takes a `System.Data.SQLite.SQLiteConnection` as its first parameter; the parameter is not declared with `this`. Call them through the class, for example:

```csharp
long pageCount = SqliteConnectionExtensions.GetPageCount(connection);
```

The caller must provide a usable connection. Every method throws `ArgumentNullException` when `conn` is `null`.

## Methods and commands

### `TableExists(SQLiteConnection conn, string tableName)`

Returns `true` when `sqlite_master` contains a table whose name exactly matches the trimmed `tableName`; otherwise, it returns `false`. A null, empty, or whitespace-only table name causes an `ArgumentException`.

This method does not run a PRAGMA. It runs this parameterized query:

```sql
SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName;
```

### `GetUserVersion(SQLiteConnection conn)`

Returns the database's application-defined user version as a `long`. If the scalar result is `null`, it returns `0`.

```sql
PRAGMA user_version;
```

### `GetUserVersionInt(SQLiteConnection conn)`

Returns the same application-defined user version as an `int`. If the scalar result is `null`, it returns `0`.

```sql
PRAGMA user_version;
```

### `SetUserVersion(SQLiteConnection conn, long version)`

Sets the database's application-defined user version from a `long` value. The value is supplied through the `@version` command parameter.

```sql
PRAGMA user_version = @version;
```

### `SetUserVersion(SQLiteConnection conn, int version)`

Sets the database's application-defined user version from an `int` value. The value is supplied through the `@version` command parameter.

```sql
PRAGMA user_version = @version;
```

### `GetPageCount(SQLiteConnection conn)`

Returns the total number of pages in the database file as a `long`. If the scalar result is `null`, it returns `0`.

```sql
PRAGMA page_count;
```

### `GetFreelistCount(SQLiteConnection conn)`

Returns the number of unused pages on the database freelist as a `long`. If the scalar result is `null`, it returns `0`.

```sql
PRAGMA freelist_count;
```

### `GetTableNames(SQLiteConnection conn)`

Returns a read-only list of non-null table names ordered by name. This method does not run a PRAGMA; it runs:

```sql
SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;
```

## Example

```csharp
if (SqliteConnectionExtensions.TableExists(connection, "Orders"))
{
    long version = SqliteConnectionExtensions.GetUserVersion(connection);
    long pages = SqliteConnectionExtensions.GetPageCount(connection);
    long freePages = SqliteConnectionExtensions.GetFreelistCount(connection);
    IReadOnlyList<string> tables = SqliteConnectionExtensions.GetTableNames(connection);
}

SqliteConnectionExtensions.SetUserVersion(connection, 2);
```
