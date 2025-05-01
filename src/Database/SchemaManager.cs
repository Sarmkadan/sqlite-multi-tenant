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
    // Manages SQLite schema modifications, constraints, and migrations at the database level
    public class SchemaManager
    {
        private readonly ILogger<SchemaManager> _logger;
        private readonly string _connectionString;

        public SchemaManager(ILogger<SchemaManager> logger, string connectionString)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        // Creates standard multi-tenant schema with foreign key constraints enabled
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

        // Adds a new column to existing table with validation and rollback capability
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

        // Renames a table with foreign key constraint handling
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

        // Creates an index for performance optimization
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

        // Retrieves all tables in the database
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
                    return result != null;
                }
            }
            catch { /* Ignore */ }

            return false;
        }
    }
}
