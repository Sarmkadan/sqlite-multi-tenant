#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Utilities
{
    // Common database utility functions for SQLite operations
    public class DatabaseUtilities
    {
        // Enables all pragmas for optimal multi-tenant performance
        public static async Task ConfigureOptimalSettingsAsync(SQLiteConnection connection)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    PRAGMA foreign_keys = ON;
                    PRAGMA journal_mode = WAL;
                    PRAGMA synchronous = NORMAL;
                    PRAGMA cache_size = 10000;
                    PRAGMA temp_store = MEMORY;
                    PRAGMA query_only = OFF;
                    PRAGMA busy_timeout = 5000;
                ";

                await command.ExecuteNonQueryAsync();
            }
        }

        // Gets database file size in bytes
        public static long GetDatabaseSize(string databasePath)
        {
            if (string.IsNullOrEmpty(databasePath))
                throw new ArgumentException("Database path cannot be empty", nameof(databasePath));

            try
            {
                var fileInfo = new System.IO.FileInfo(databasePath);
                return fileInfo.Exists ? fileInfo.Length : 0;
            }
            catch
            {
                return 0;
            }
        }

        // Gets human-readable database size
        public static string GetDatabaseSizeFormatted(string databasePath)
        {
            var bytes = GetDatabaseSize(databasePath);
            return FormatBytes(bytes);
        }

        // Compacts database by removing deleted space
        public static async Task CompactDatabaseAsync(SQLiteConnection connection)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "VACUUM";
                await command.ExecuteNonQueryAsync();
            }
        }

        // Analyzes query performance
        public static async Task AnalyzeQueryPerformanceAsync(SQLiteConnection connection, string query)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrEmpty(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"EXPLAIN QUERY PLAN {query}";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        // Read explain output
                    }
                }
            }
        }

        // Gets current database statistics
        public static async Task<DatabaseStatistics> GetDatabaseStatisticsAsync(SQLiteConnection connection)
        {
            var stats = new DatabaseStatistics();

            if (connection is null)
                return stats;

            try
            {
                // Get table count
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table'";
                    stats.TableCount = (long)await command.ExecuteScalarAsync();
                }

                // Get index count
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='index'";
                    stats.IndexCount = (long)await command.ExecuteScalarAsync();
                }

                // Get page count
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA page_count";
                    stats.PageCount = (long)await command.ExecuteScalarAsync();
                }

                // Get page size
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA page_size";
                    stats.PageSize = (long)await command.ExecuteScalarAsync();
                }

                stats.EstimatedSize = stats.PageCount * stats.PageSize;
            }
            catch { /* Ignored */ }

            return stats;
        }

        // Checks if a table exists
        public static async Task<bool> TableExistsAsync(SQLiteConnection connection, string tableName)
        {
            if (connection is null || string.IsNullOrEmpty(tableName))
                return false;

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name";
                    command.Parameters.AddWithValue("@name", tableName);

                    var result = await command.ExecuteScalarAsync();
                    return (long)result > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        // Checks if a column exists in a table
        public static async Task<bool> ColumnExistsAsync(SQLiteConnection connection,
            string tableName, string columnName)
        {
            if (connection is null || string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(columnName))
                return false;

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
            catch { /* Ignored */ }

            return false;
        }

        // Gets all columns for a table
        public static async Task<List<ColumnInfo>> GetTableColumnsAsync(SQLiteConnection connection,
            string tableName)
        {
            var columns = new List<ColumnInfo>();

            if (connection is null || string.IsNullOrEmpty(tableName))
                return columns;

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"PRAGMA table_info({tableName})";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            columns.Add(new ColumnInfo
                            {
                                Name = reader.GetString(1),
                                Type = reader.GetString(2),
                                NotNull = reader.GetBoolean(3),
                                DefaultValue = reader.IsDBNull(4) ? null : reader.GetValue(4).ToString(),
                                PrimaryKey = reader.GetBoolean(5)
                            });
                        }
                    }
                }
            }
            catch { /* Ignored */ }

            return columns;
        }

        private static string FormatBytes(long bytes)
        {
            var sizes = new[] { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }

    public sealed class DatabaseStatistics {
        public long TableCount { get; set; }
        public long IndexCount { get; set; }
        public long PageCount { get; set; }
        public long PageSize { get; set; }
        public long EstimatedSize { get; set; }
    }

    public sealed class ColumnInfo {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool NotNull { get; set; }
        public string DefaultValue { get; set; }
        public bool PrimaryKey { get; set; }
    }
}
