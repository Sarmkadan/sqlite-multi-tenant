#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.DataOperations
{
    // Extension methods for DataImporter to provide additional import functionality
    public static class DataImporterExtensions
    {
        // Imports JSON data from a file path asynchronously
        // Automatically reads the file content and delegates to ImportFromJsonAsync
        public static async Task<int> ImportFromJsonFileAsync(this DataImporter importer,
            SQLiteConnection connection, string tableName, string filePath, bool truncateTable = false)
        {
            if (importer is null)
                throw new ArgumentNullException(nameof(importer));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var jsonData = await System.IO.File.ReadAllTextAsync(filePath);
            return await importer.ImportFromJsonAsync(connection, tableName, jsonData, truncateTable);
        }

        // Imports CSV data from a file path asynchronously
        // Automatically reads the file content and delegates to ImportFromCsvAsync
        public static async Task<int> ImportFromCsvFileAsync(this DataImporter importer,
            SQLiteConnection connection, string tableName, string filePath,
            bool hasHeaders = true, string delimiter = ",", bool truncateTable = false)
        {
            if (importer is null)
                throw new ArgumentNullException(nameof(importer));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var csvData = await System.IO.File.ReadAllTextAsync(filePath);
            return await importer.ImportFromCsvAsync(connection, tableName, csvData, hasHeaders, delimiter, truncateTable);
        }

        // Imports SQL statements from a file path asynchronously
        // Automatically reads the file content and delegates to ImportFromSqlAsync
        public static async Task<int> ImportFromSqlFileAsync(this DataImporter importer,
            SQLiteConnection connection, string filePath)
        {
            if (importer is null)
                throw new ArgumentNullException(nameof(importer));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var sqlStatements = await System.IO.File.ReadAllTextAsync(filePath);
            return await importer.ImportFromSqlAsync(connection, sqlStatements);
        }

        // Validates that a table exists before attempting import
        // Returns true if table exists, false otherwise
        // Can optionally create the table if it doesn't exist using a provided schema
        public static async Task<bool> ValidateTableExistsAsync(this DataImporter importer,
            SQLiteConnection connection, string tableName, string? schema = null)
        {
            if (importer is null)
                throw new ArgumentNullException(nameof(importer));

            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName";
                command.Parameters.AddWithValue("@tableName", tableName);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
        }

        // Creates a table with the specified schema if it doesn't exist
        // Returns true if table was created, false if it already existed
        public static async Task<bool> CreateTableIfNotExistsAsync(this DataImporter importer,
            SQLiteConnection connection, string tableName, string schema)
        {
            if (importer is null)
                throw new ArgumentNullException(nameof(importer));

            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            if (string.IsNullOrEmpty(schema))
                throw new ArgumentNullException(nameof(schema));

            var tableExists = await importer.ValidateTableExistsAsync(connection, tableName);

            if (tableExists)
                return false;

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = schema;
                        await command.ExecuteNonQueryAsync();
                    }

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}
