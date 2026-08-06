#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Database
{
    /// <summary>
    /// Manages SQLite schema modifications, constraints, and migrations at the database level.
    /// Provides methods for creating tables, adding columns, renaming tables, and creating indexes
    /// with built-in validation and error recovery.
    /// </summary>
    public sealed class SchemaManager : IEquatable<SchemaManager>
    {
        private readonly ILogger<SchemaManager> _logger;
        private readonly string _connectionString;

        /// <summary>
        /// Gets the connection string used by this <see cref="SchemaManager"/> instance.
        /// </summary>
        public string ConnectionString => _connectionString;

        /// <summary>
        /// Initializes a new instance of <see cref="SchemaManager"/>.
        /// </summary>
        /// <param name="logger">Logger instance for diagnostic output.</param>
        /// <param name="connectionString">SQLite connection string for the target database.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> or <paramref name="connectionString"/> is null.</exception>
        public SchemaManager(ILogger<SchemaManager> logger, string connectionString)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public bool Equals(SchemaManager? other)
        {
            if (other is null) return false;
            return ConnectionString == other.ConnectionString;
        }

        public override bool Equals(object? obj) => obj is SchemaManager other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(ConnectionString);

        public static bool operator ==(SchemaManager? left, SchemaManager? right) => Equals(left, right);

        public static bool operator !=(SchemaManager? left, SchemaManager? right) => !Equals(left, right);

        /// <summary>
        /// Creates the standard multi-tenant schema including Tenants and AuditLog tables
        /// with foreign key constraints enabled. Safe to call multiple times (uses CREATE IF NOT EXISTS).
        /// </summary>
        /// <param name="tenantId">The unique identifier of the tenant being initialized.</param>
        public async Task InitializeSchemaAsync(string tenantId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Tenants (
TenantId TEXT PRIMARY KEY,
Name TEXT NOT NULL UNIQUE,
IsActive INTEGER NOT NULL DEFAULT 1,
CreatedAt TEXT NOT NULL,
UpdatedAt TEXT NOT NULL,
DatabasePath TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS AuditLog (
Id INTEGER PRIMARY KEY AUTOINCREMENT,
TenantId TEXT NOT NULL,
Action TEXT NOT NULL,
EntityType TEXT NOT NULL,
EntityId TEXT NOT NULL,
OldValues TEXT,
NewValues TEXT,
CreatedAt TEXT NOT NULL,
CreatedBy TEXT NOT NULL,
FOREIGN KEY(TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_AuditLog_TenantId ON AuditLog(TenantId);
CREATE INDEX IF NOT EXISTS idx_AuditLog_CreatedAt ON AuditLog(CreatedAt);
";

                        await command.ExecuteNonQueryAsync();
                    }
                }

                _logger.LogInformation("Schema initialized for tenant: {TenantId}", tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize schema for tenant: {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// Adds a new column to an existing table with pre-validation to prevent duplicates.
        /// </summary>
        /// <param name="tenantId">Tenant identifier for logging context.</param>
        /// <param name="tableName">Name of the table to modify.</param>
        /// <param name="columnName">Name of the new column.</param>
        /// <param name="columnDefinition">SQLite column type and constraints (e.g., "TEXT NOT NULL DEFAULT ''").</param>
        /// <returns><c>true</c> if the column was added; <c>false</c> if it already exists.</returns>
        /// <exception cref="SQLiteException">Thrown when the ALTER TABLE statement fails.</exception>
        public async Task<bool> AddColumnAsync(string tenantId, string tableName,
            string columnName, string columnDefinition)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check if column already exists
                    if (await ColumnExistsAsync(connection, tableName, columnName))
                    {
                        _logger.LogWarning("Column {ColumnName} already exists in table {TableName}",
                            columnName, tableName);
                        return false;
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}";
                        await command.ExecuteNonQueryAsync();
                    }
                }

                _logger.LogInformation("Column {ColumnName} added to table {TableName} for tenant {TenantId}",
                    columnName, tableName, tenantId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add column {ColumnName} to table {TableName}",
                    columnName, tableName);
                throw;
            }
        }

        /// <summary>
        /// Renames a table using ALTER TABLE RENAME. Foreign key references are not automatically updated
        /// by SQLite, so callers must handle constraint adjustments separately.
        /// </summary>
        /// <param name="oldTableName">Current name of the table.</param>
        /// <param name="newTableName">New name for the table.</param>
        public async Task RenameTableAsync(string oldTableName, string newTableName)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"ALTER TABLE {oldTableName} RENAME TO {newTableName}";
                        await command.ExecuteNonQueryAsync();
                    }
                }

                _logger.LogInformation("Table renamed from {OldName} to {NewName}", oldTableName, newTableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rename table from {OldName} to {NewName}",
                    oldTableName, newTableName);
                throw;
            }
        }

        /// <summary>
        /// Creates a non-unique index on the specified columns. Skips creation if the index already exists.
        /// </summary>
        /// <param name="tableName">Table to create the index on.</param>
        /// <param name="indexName">Unique name for the index.</param>
        /// <param name="columns">One or more column names to include in the index.</param>
        /// <returns><c>true</c> if the index was created; <c>false</c> if it already exists.</returns>
        public async Task<bool> CreateIndexAsync(string tableName, string indexName, params string[] columns)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    if (await IndexExistsAsync(connection, indexName))
                    {
                        _logger.LogWarning("Index {IndexName} already exists", indexName);
                        return false;
                    }

                    var columnList = string.Join(", ", columns);
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"CREATE INDEX {indexName} ON {tableName} ({columnList})\n";
                        await command.ExecuteNonQueryAsync();
                    }
                }

                _logger.LogInformation("Index {IndexName} created on table {TableName}", indexName, tableName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create index {IndexName}", indexName);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the names of all user-created tables in the database,
        /// excluding SQLite internal tables (sqlite_*).
        /// </summary>
        /// <returns>A list of table names. Returns an empty list if an error occurs.</returns>
        public async Task<List<string>> GetTablesAsync()
        {
            var tables = new List<string>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                tables.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve tables");
            }

            return tables;
        }

        private async Task<bool> ColumnExistsAsync(SQLiteConnection connection,
            string tableName, string columnName)
        {
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"PRAGMA table_info({tableName})\n";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader.GetString(1) == columnName)
                                return true;
                        }
                    }
                }
            }
            catch { /* Ignore */ }

            return false;
        }

        private async Task<bool> IndexExistsAsync(SQLiteConnection connection, string indexName)
        {
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='index' AND name=@name";
                    command.Parameters.AddWithValue("@name", indexName);

                    var result = await command.ExecuteScalarAsync();
                    return result is not null;
                }
            }
            catch { /* Ignore */ }

            return false;
        }

        /// <summary>
        /// Schema comparison result for a single table.
        /// </summary>
        public sealed class TableSchemaDiff
        {
            /// <summary>
            /// Gets or sets the name of the table.
            /// </summary>
            public string TableName { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets a value indicating whether the table is missing in the target database.
            /// </summary>
            public bool IsMissing { get; set; }

            /// <summary>
            /// Gets a value indicating whether the table exists in both databases.
            /// </summary>
            public bool ExistsInBoth => !IsMissing && TargetColumns != null;

            /// <summary>
            /// Gets the list of columns that are missing in the target database.
            /// </summary>
            public List<string> MissingColumns { get; } = new List<string>();

            /// <summary>
            /// Gets the list of columns with type mismatches between source and target.
            /// </summary>
            public List<(string ColumnName, string SourceType, string TargetType)> TypeMismatches { get; } = new List<(string, string, string)>();

            /// <summary>
            /// Gets the list of columns that exist in target but not in source (extra columns).
            /// </summary>
            public List<string> ExtraColumns { get; } = new List<string>();

            /// <summary>
            /// Gets the list of indexes that are missing in the target database.
            /// </summary>
            public List<string> MissingIndexes { get; } = new List<string>();

            /// <summary>
            /// Gets the list of indexes that exist in target but not in source (extra indexes).
            /// </summary>
            public List<string> ExtraIndexes { get; } = new List<string>();

            /// <summary>
            /// Gets or sets the source database columns (if table exists in source).
            /// </summary>
            public Dictionary<string, string>? SourceColumns { get; set; }

            /// <summary>
            /// Gets or sets the target database columns (if table exists in source).
            /// </summary>
            public Dictionary<string, string>? TargetColumns { get; set; }

            /// <summary>
            /// Gets or sets the source database indexes (if table exists in source).
            /// </summary>
            public HashSet<string>? SourceIndexes { get; set; }

            /// <summary>
            /// Gets or sets the target database indexes (if table exists in source).
            /// </summary>
            public HashSet<string>? TargetIndexes { get; set; }

            /// <summary>
            /// Gets a value indicating whether this table schema is identical in both databases.
            /// </summary>
            public bool IsIdentical =>
                !IsMissing &&
                MissingColumns.Count == 0 &&
                TypeMismatches.Count == 0 &&
                ExtraColumns.Count == 0 &&
                MissingIndexes.Count == 0 &&
                ExtraIndexes.Count == 0;
        }

        /// <summary>
        /// Schema comparison result for the entire database.
        /// </summary>
        public sealed class DatabaseSchemaDiff
        {
            /// <summary>
            /// Gets the list of tables that are missing in the target database.
            /// </summary>
            public List<string> MissingTables { get; } = new List<string>();

            /// <summary>
            /// Gets the list of tables with schema differences.
            /// </summary>
            public List<TableSchemaDiff> TablesWithDifferences { get; } = new List<TableSchemaDiff>();

            /// <summary>
            /// Gets the list of tables that exist in both databases but have no differences.
            /// </summary>
            public List<TableSchemaDiff> IdenticalTables { get; } = new List<TableSchemaDiff>();

            /// <summary>
            /// Gets a value indicating whether all tables are identical in both databases.
            /// </summary>
            public bool IsIdentical => MissingTables.Count == 0 && TablesWithDifferences.Count == 0;

            /// <summary>
            /// Gets the total number of tables compared.
            /// </summary>
            public int TotalTables => MissingTables.Count + IdenticalTables.Count + TablesWithDifferences.Count;
        }

        /// <summary>
        /// Compares the schema of the current database with another database.
        /// </summary>
        /// <param name="otherConnectionString">Connection string to the other database to compare against.</param>
        /// <returns>A structured diff showing differences between the two database schemas.</returns>
        /// <exception cref="ArgumentNullException">Thrown when connection string is null.</exception>
        public async Task<DatabaseSchemaDiff> CompareSchemasAsync(string otherConnectionString)
        {
            if (string.IsNullOrWhiteSpace(otherConnectionString))
            {
                throw new ArgumentNullException(nameof(otherConnectionString));
            }

            var diff = new DatabaseSchemaDiff();

            // Get tables from both databases
            var sourceTables = await GetTablesAsync();
            var targetTables = await GetTablesFromConnectionAsync(otherConnectionString);

            // Find missing tables in target
            foreach (var tableName in sourceTables)
            {
                if (!targetTables.Contains(tableName))
                {
                    diff.MissingTables.Add(tableName);
                }
            }

            // Compare tables that exist in both databases
            var commonTables = sourceTables.Intersect(targetTables);
            foreach (var tableName in commonTables)
            {
                var tableDiff = await CompareTableSchemasAsync(tableName, otherConnectionString);

                if (tableDiff.IsMissing)
                {
                    diff.MissingTables.Add(tableName);
                }
                else if (tableDiff.IsIdentical)
                {
                    diff.IdenticalTables.Add(tableDiff);
                }
                else
                {
                    diff.TablesWithDifferences.Add(tableDiff);
                }
            }

        // Track tables that exist only in target (extra tables)
        var extraTables = targetTables.Except(sourceTables);
        foreach (var tableName in extraTables)
        {
            var tableDiff = new TableSchemaDiff
            {
                TableName = tableName,
                IsMissing = true
            };
            diff.TablesWithDifferences.Add(tableDiff);
        }

            _logger.LogInformation("Schema comparison completed: {TotalTables} total, {Missing} missing, {Identical} identical, {Differences} with differences",
                diff.TotalTables, diff.MissingTables.Count, diff.IdenticalTables.Count, diff.TablesWithDifferences.Count);

            return diff;
        }

        /// <summary>
        /// Compares the schema of a specific table between this database and another database.
        /// </summary>
        /// <param name="tableName">Name of the table to compare.</param>
        /// <param name="otherConnectionString">Connection string to the other database.</param>
        /// <returns>Detailed comparison of the table schemas.</returns>
        /// <exception cref="ArgumentNullException">Thrown when table name or connection string is null.</exception>
        public async Task<TableSchemaDiff> CompareTableSchemasAsync(string tableName, string otherConnectionString)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            if (string.IsNullOrWhiteSpace(otherConnectionString))
            {
                throw new ArgumentNullException(nameof(otherConnectionString));
            }

            var tableDiff = new TableSchemaDiff();
            tableDiff.TableName = tableName;

            try
            {
                // Get source table schema
                tableDiff.SourceColumns = await GetTableColumnsAsync(_connectionString, tableName);
                tableDiff.SourceIndexes = await GetTableIndexesAsync(_connectionString, tableName);

                // Get target table schema
                tableDiff.TargetColumns = await GetTableColumnsAsync(otherConnectionString, tableName);
                tableDiff.TargetIndexes = await GetTableIndexesAsync(otherConnectionString, tableName);

                // Determine if table is missing (both should be null if missing)
                if (tableDiff.SourceColumns == null || tableDiff.TargetColumns == null)
                {
                    tableDiff.IsMissing = true;
                    return tableDiff;
                }

                // Compare columns
                CompareColumns(tableDiff);
                CompareIndexes(tableDiff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing table schemas for table {TableName}", tableName);
                tableDiff.IsMissing = true;
            }

            return tableDiff;
        }

        private async Task<Dictionary<string, string>> GetTableColumnsAsync(string connectionString, string tableName)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"PRAGMA table_info({tableName})\n";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            while (await reader.ReadAsync())
                            {
                                var columnName = reader.GetString(1);
                                var columnType = reader.IsDBNull(2) ? "TEXT" : reader.GetString(2);
                                columns[columnName] = columnType;
                            }
                            return columns.Count > 0 ? columns : null;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private async Task<HashSet<string>> GetTableIndexesAsync(string connectionString, string tableName)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT name FROM sqlite_master WHERE type='index' AND tbl_name=@tableName";
                        command.Parameters.AddWithValue("@tableName", tableName);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var indexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            while (await reader.ReadAsync())
                            {
                                indexes.Add(reader.GetString(0));
                            }
                            return indexes.Count > 0 ? indexes : null;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private async Task<HashSet<string>> GetTablesFromConnectionAsync(string connectionString)
        {
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'\n";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                tables.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve tables from connection");
            }

            return tables;
        }

        private void CompareColumns(TableSchemaDiff tableDiff)
        {
            if (tableDiff.SourceColumns == null || tableDiff.TargetColumns == null)
            {
                return;
            }

            // Find missing columns in target
            foreach (var columnName in tableDiff.SourceColumns.Keys)
            {
                if (!tableDiff.TargetColumns.ContainsKey(columnName))
                {
                    tableDiff.MissingColumns.Add(columnName);
                }
                else
                {
                    // Compare column types
                    var sourceType = tableDiff.SourceColumns[columnName];
                    var targetType = tableDiff.TargetColumns[columnName];

                    if (!string.Equals(sourceType, targetType, StringComparison.OrdinalIgnoreCase))
                    {
                        tableDiff.TypeMismatches.Add((columnName, sourceType, targetType));
                    }
                }
            }

            // Find extra columns in target
            foreach (var columnName in tableDiff.TargetColumns.Keys)
            {
                if (!tableDiff.SourceColumns.ContainsKey(columnName))
                {
                    tableDiff.ExtraColumns.Add(columnName);
                }
            }
        }

        private void CompareIndexes(TableSchemaDiff tableDiff)
        {
            if (tableDiff.SourceIndexes == null || tableDiff.TargetIndexes == null)
            {
                return;
            }

            // Find missing indexes in target
            foreach (var indexName in tableDiff.SourceIndexes)
            {
                if (!tableDiff.TargetIndexes.Contains(indexName))
                {
                    tableDiff.MissingIndexes.Add(indexName);
                }
            }

            // Find extra indexes in target
            foreach (var indexName in tableDiff.TargetIndexes)
            {
                if (!tableDiff.SourceIndexes.Contains(indexName))
                {
                    tableDiff.ExtraIndexes.Add(indexName);
                }
            }
        }
    }
}