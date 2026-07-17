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
    public sealed class SchemaManager {
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
                        command.CommandText = $"CREATE INDEX {indexName} ON {tableName} ({columnList})";
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
                    command.CommandText = $"PRAGMA table_info({tableName})";

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
    }
}
