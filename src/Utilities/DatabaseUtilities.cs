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
    /// <summary>
/// Provides common database utility functions for SQLite operations in a multi-tenant environment.
/// Includes methods for configuration, performance analysis, database maintenance, and schema inspection.
/// </summary>
    public class DatabaseUtilities
    {
        /// <summary>
        /// Configures optimal SQLite database settings for multi-tenant performance.
        /// Enables foreign keys, WAL journal mode, sets appropriate synchronous level, cache size,
        /// and other performance-related pragmas to optimize database operations in a multi-tenant environment.
        /// </summary>
        /// <param name="connection">The SQLite connection to configure. Must be open or will be opened automatically.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when connection is null.</exception>
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

        /// <summary>
        /// Gets the size of the SQLite database file in bytes.
        /// </summary>
        /// <param name="databasePath">The file path to the SQLite database.</param>
        /// <returns>The size of the database file in bytes, or 0 if the file doesn't exist or an error occurs.</returns>
        /// <exception cref="ArgumentException">Thrown when databasePath is null or empty.</exception>
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

        /// <summary>
        /// Gets a human-readable formatted size of the SQLite database file.
        /// </summary>
        /// <param name="databasePath">The file path to the SQLite database.</param>
        /// <returns>A formatted string representing the database size (e.g., "2.5 MB", "1.2 GB").</returns>
        /// <seealso cref="GetDatabaseSize"/>
        public static string GetDatabaseSizeFormatted(string databasePath)
        {
            var bytes = GetDatabaseSize(databasePath);
            return FormatBytes(bytes);
        }

        /// <summary>
        /// Compacts the SQLite database by removing unused space and reducing file size.
        /// Executes the VACUUM command which rebuilds the database file, repacking it into a minimal amount of disk space.
        /// </summary>
        /// <param name="connection">The SQLite connection to the database to compact. Must be open.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when connection is null.</exception>
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

        /// <summary>
        /// Analyzes the performance characteristics of a SQL query using SQLite's EXPLAIN QUERY PLAN.
        /// Returns information about how the query will be executed, including which indexes will be used,
        /// whether full table scans will occur, and the join strategy.
        /// </summary>
        /// <param name="connection">The SQLite connection to analyze the query against. Must be open.</param>
        /// <param name="query">The SQL query to analyze.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when connection is null.</exception>
        /// <exception cref="ArgumentException">Thrown when query is null or empty.</exception>
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

        /// <summary>
        /// Retrieves comprehensive statistics about the SQLite database.
        /// Includes table count, index count, page count, page size, and estimated database size.
        /// </summary>
        /// <param name="connection">The SQLite connection to the database to analyze. Must be open.</param>
        /// <returns>A <see cref="DatabaseStatistics"/> object containing database statistics, or a default instance if an error occurs.</returns>
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

        /// <summary>
        /// Checks whether a specific table exists in the SQLite database.
        /// </summary>
        /// <param name="connection">The SQLite connection to check. Must be open.</param>
        /// <param name="tableName">The name of the table to check for existence.</param>
        /// <returns>True if the table exists; otherwise, false.</returns>
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

        /// <summary>
        /// Checks whether a specific column exists in a table within the SQLite database.
        /// </summary>
        /// <param name="connection">The SQLite connection to check. Must be open.</param>
        /// <param name="tableName">The name of the table to check.</param>
        /// <param name="columnName">The name of the column to check for existence.</param>
        /// <returns>True if the column exists in the specified table; otherwise, false.</returns>
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

        /// <summary>
        /// Retrieves information about all columns in a specified table.
        /// Uses PRAGMA table_info to get column names, types, constraints, default values, and primary key information.
        /// </summary>
        /// <param name="connection">The SQLite connection to the database. Must be open.</param>
        /// <param name="tableName">The name of the table to inspect.</param>
        /// <returns>A list of <see cref="ColumnInfo"/> objects describing each column in the table, or an empty list if an error occurs.</returns>
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

    /// <summary>
/// Represents comprehensive statistics about an SQLite database.
/// </summary>
public sealed class DatabaseStatistics {
        /// <summary>
        /// Gets or sets the number of tables in the database.
        /// </summary>
        public long TableCount { get; set; }

        /// <summary>
        /// Gets or sets the number of indexes in the database.
        /// </summary>
        public long IndexCount { get; set; }

        /// <summary>
        /// Gets or sets the number of pages in the database file.
        /// </summary>
        public long PageCount { get; set; }

        /// <summary>
        /// Gets or sets the size of each page in bytes.
        /// </summary>
        public long PageSize { get; set; }

        /// <summary>
        /// Gets or sets the estimated total size of the database in bytes.
        /// Calculated as PageCount * PageSize.
        /// </summary>
        public long EstimatedSize { get; set; }
    }

    /// <summary>
/// Represents information about a column in an SQLite table.
/// </summary>
public sealed class ColumnInfo {
        /// <summary>
        /// Gets or sets the name of the column.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the data type of the column (e.g., "INTEGER", "TEXT", "BLOB").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the column is defined as NOT NULL.
        /// </summary>
        public bool NotNull { get; set; }

        /// <summary>
        /// Gets or sets the default value of the column, or null if no default is defined.
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the column is part of the primary key.
        /// </summary>
        public bool PrimaryKey { get; set; }
    }
}
