#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.DataOperations
{
    /// <summary>
    /// Extension methods for <see cref="DataImporter"/> to provide additional import functionality
    /// from file-based data sources.
    /// </summary>
    public static class DataImporterExtensions
    {
        /// <summary>
        /// Imports JSON data from a file asynchronously.
        /// </summary>
        /// <param name="importer">The <see cref="DataImporter"/> instance.</param>
        /// <param name="connection">The <see cref="SQLiteConnection"/> to use for import.</param>
        /// <param name="tableName">Name of the table to import into.</param>
        /// <param name="filePath">Path to the JSON file containing data to import.</param>
        /// <param name="truncateTable">Whether to truncate the table before import.</param>
        /// <returns>Number of rows imported.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="importer"/>, <paramref name="connection"/>, <paramref name="tableName"/>, or <paramref name="filePath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> or <paramref name="filePath"/> is empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="filePath"/> does not exist.</exception>
        /// <exception cref="IOException">Thrown when there is an error reading the file.</exception>
        public static async Task<int> ImportFromJsonFileAsync(this DataImporter importer,
            SQLiteConnection connection, string tableName, string filePath, bool truncateTable = false)
        {
            ArgumentNullException.ThrowIfNull(importer);
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentException.ThrowIfNullOrEmpty(tableName);
            ArgumentException.ThrowIfNullOrEmpty(filePath);

            var jsonData = await File.ReadAllTextAsync(filePath);
            return await importer.ImportFromJsonAsync(connection, tableName, jsonData, truncateTable);
        }

        /// <summary>
        /// Imports CSV data from a file asynchronously.
        /// </summary>
        /// <param name="importer">The <see cref="DataImporter"/> instance.</param>
        /// <param name="connection">The <see cref="SQLiteConnection"/> to use for import.</param>
        /// <param name="tableName">Name of the table to import into.</param>
        /// <param name="filePath">Path to the CSV file containing data to import.</param>
        /// <param name="hasHeaders">Whether the CSV file contains a header row.</param>
        /// <param name="delimiter">The delimiter character used in the CSV file.</param>
        /// <param name="truncateTable">Whether to truncate the table before import.</param>
        /// <returns>Number of rows imported.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="importer"/>, <paramref name="connection"/>, <paramref name="tableName"/>, or <paramref name="filePath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/>, <paramref name="filePath"/>, or <paramref name="delimiter"/> is empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="filePath"/> does not exist.</exception>
        /// <exception cref="IOException">Thrown when there is an error reading the file.</exception>
        public static async Task<int> ImportFromCsvFileAsync(this DataImporter importer,
            SQLiteConnection connection, string tableName, string filePath,
            bool hasHeaders = true, string delimiter = ",", bool truncateTable = false)
        {
            ArgumentNullException.ThrowIfNull(importer);
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentException.ThrowIfNullOrEmpty(tableName);
            ArgumentException.ThrowIfNullOrEmpty(filePath);
            ArgumentException.ThrowIfNullOrEmpty(delimiter);

            var csvData = await File.ReadAllTextAsync(filePath);
            return await importer.ImportFromCsvAsync(connection, tableName, csvData, hasHeaders, delimiter, truncateTable);
        }

        /// <summary>
        /// Imports SQL statements from a file asynchronously.
        /// </summary>
        /// <param name="importer">The <see cref="DataImporter"/> instance.</param>
        /// <param name="connection">The <see cref="SQLiteConnection"/> to use for import.</param>
        /// <param name="filePath">Path to the SQL file containing statements to execute.</param>
        /// <returns>Total number of rows affected by all SQL statements.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="importer"/>, <paramref name="connection"/>, or <paramref name="filePath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="filePath"/> does not exist.</exception>
        /// <exception cref="IOException">Thrown when there is an error reading the file.</exception>
        public static async Task<int> ImportFromSqlFileAsync(this DataImporter importer,
            SQLiteConnection connection, string filePath)
        {
            ArgumentNullException.ThrowIfNull(importer);
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentException.ThrowIfNullOrEmpty(filePath);

            var sqlStatements = await File.ReadAllTextAsync(filePath);
            return await importer.ImportFromSqlAsync(connection, sqlStatements);
        }

        /// <summary>
        /// Validates that a table exists before attempting import.
        /// </summary>
        /// <param name="importer">The <see cref="DataImporter"/> instance.</param>
        /// <param name="connection">The <see cref="SQLiteConnection"/> to check.</param>
        /// <param name="tableName">Name of the table to validate.</param>
        /// <param name="schema">Optional schema to use if creating the table.</param>
        /// <returns>True if table exists, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="importer"/>, <paramref name="connection"/>, or <paramref name="tableName"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> is empty.</exception>
        public static async Task<bool> ValidateTableExistsAsync(this DataImporter importer,
            SQLiteConnection connection, string tableName, string? schema = null)
        {
            ArgumentNullException.ThrowIfNull(importer);
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentException.ThrowIfNullOrEmpty(tableName);

            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName";
            command.Parameters.AddWithValue("@tableName", tableName);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// Creates a table with the specified schema if it doesn't exist.
        /// </summary>
        /// <param name="importer">The <see cref="DataImporter"/> instance.</param>
        /// <param name="connection">The <see cref="SQLiteConnection"/> to use.</param>
        /// <param name="tableName">Name of the table to create.</param>
        /// <param name="schema">SQL schema definition for the table.</param>
        /// <returns>True if table was created, false if it already existed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="importer"/>, <paramref name="connection"/>, <paramref name="tableName"/>, or <paramref name="schema"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> or <paramref name="schema"/> is empty.</exception>
        public static async Task<bool> CreateTableIfNotExistsAsync(this DataImporter importer,
            SQLiteConnection connection, string tableName, string schema)
        {
            ArgumentNullException.ThrowIfNull(importer);
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentException.ThrowIfNullOrEmpty(tableName);
            ArgumentException.ThrowIfNullOrEmpty(schema);

            var tableExists = await importer.ValidateTableExistsAsync(connection, tableName);

            if (tableExists)
                return false;

            await using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText = schema;
                    await command.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}
