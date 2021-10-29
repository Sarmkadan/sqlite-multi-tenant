# DatabaseUtilities

The `DatabaseUtilities` class provides a comprehensive suite of static methods for managing, inspecting, and optimizing SQLite databases in multi-tenant applications. It offers functionality ranging from performance configuration and database maintenance tasks, such as compaction and analysis, to schema introspection, enabling robust database lifecycle management.

## API

### Methods

*   **`ConfigureOptimalSettingsAsync`**
    *   Purpose: Applies recommended SQLite configuration settings to improve performance and reliability.
    *   Returns: A `Task` representing the asynchronous operation.
    *   Throws: May throw database-related exceptions if the connection is inaccessible.

*   **`GetDatabaseSize`**
    *   Purpose: Retrieves the raw size of the database file in bytes.
    *   Returns: A `long` representing the size in bytes.

*   **`GetDatabaseSizeFormatted`**
    *   Purpose: Retrieves the database size as a human-readable string (e.g., "KB", "MB", "GB").
    *   Returns: A `string` containing the formatted size.

*   **`CompactDatabaseAsync`**
    *   Purpose: Performs a VACUUM operation to defragment the database and reclaim unused space.
    *   Returns: A `Task` representing the asynchronous operation.
    *   Throws: May throw exceptions if the database is locked or disk space is insufficient.

*   **`AnalyzeQueryPerformanceAsync`**
    *   Purpose: Executes internal SQLite analysis to update query planner statistics.
    *   Returns: A `Task` representing the asynchronous operation.

*   **`GetDatabaseStatisticsAsync`**
    *   Purpose: Gathers comprehensive metrics regarding the database structure and storage.
    *   Returns: A `Task<DatabaseStatistics>` containing the compiled statistics.

*   **`TableExistsAsync`**
    *   Purpose: Verifies the existence of a specific table within the database.
    *   Returns: A `Task<bool>` that is `true` if the table exists, otherwise `false`.

*   **`ColumnExistsAsync`**
    *   Purpose: Verifies the existence of a specific column within a given table.
    *   Returns: A `Task<bool>` that is `true` if the column exists, otherwise `false`.

*   **`GetTableColumnsAsync`**
    *   Purpose: Retrieves schema information for all columns in a specified table.
    *   Returns: A `Task<List<ColumnInfo>>` containing details for each column.

### Classes

#### `DatabaseStatistics`
Represents the current state and metrics of the SQLite database.
*   `TableCount` (long): Total number of tables in the database.
*   `IndexCount` (long): Total number of indexes.
*   `PageCount` (long): Total number of pages in the database file.
*   `PageSize` (long): The size of a single page in bytes.
*   `EstimatedSize` (long): The total estimated size of the database in bytes.

#### `ColumnInfo`
Represents metadata for a specific database column.
*   `Name` (string): The name of the column.
*   `Type` (string): The data type of the column.
*   `NotNull` (bool): Indicates if the column enforces a NOT NULL constraint.
*   `DefaultValue` (string): The default value defined for the column, if any.

## Usage

### Example 1: Inspecting Database Statistics
```csharp
// Retrieve and display database metrics
var stats = await DatabaseUtilities.GetDatabaseStatisticsAsync();
Console.WriteLine($"Total Tables: {stats.TableCount}");
Console.WriteLine($"Estimated Size: {DatabaseUtilities.GetDatabaseSizeFormatted()}");
```

### Example 2: Schema Verification and Maintenance
```csharp
// Ensure table structure and perform maintenance
if (await DatabaseUtilities.TableExistsAsync("Tenants"))
{
    var columns = await DatabaseUtilities.GetTableColumnsAsync("Tenants");
    if (!columns.Any(c => c.Name == "LastActive"))
    {
        // Handle missing column logic
    }
}

// Compact the database to reclaim space
await DatabaseUtilities.CompactDatabaseAsync();
```

## Notes

*   **Thread Safety:** While `DatabaseUtilities` consists of static methods, they are designed to be thread-safe regarding internal state. However, SQLite limitations concerning concurrent write operations still apply.
*   **Performance Impact:** Operations like `CompactDatabaseAsync` and `AnalyzeQueryPerformanceAsync` may cause temporary locking of the database file. These should be executed during maintenance windows or when low traffic is anticipated.
*   **Database Locking:** If the database is busy (locked by another process), some methods may throw exceptions. Ensure appropriate retry logic is implemented in the calling application.
