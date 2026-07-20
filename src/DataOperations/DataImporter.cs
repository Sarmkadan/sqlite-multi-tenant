#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.DataOperations
{
    /// <summary>
/// Provides functionality for importing data into tenant databases from various formats.
/// Supports JSON, CSV, and raw SQL INSERT statements with transaction support, validation, and rollback capability.
/// </summary>
        public sealed class DataImporter {
        private readonly ILogger<DataImporter> _logger;

        /// <summary>
/// Initializes a new instance of the <see cref="DataImporter"/> class.
/// </summary>
/// <param name="logger">The logger instance used for logging import operations and errors.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
public DataImporter(ILogger<DataImporter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
/// Imports JSON data into a specified table with transaction support and validation.
/// </summary>
/// <param name="connection">The SQLite database connection to use for the import operation.</param>
/// <param name="tableName">Name of the table where data will be imported.</param>
/// <param name="jsonData">JSON string containing the data to import. Can be either an array of objects or an object with a "data" property containing an array.</param>
/// <param name="truncateTable">If true, truncates the target table before importing data.</param>
/// <returns>Number of rows successfully imported.</returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/>, <paramref name="tableName"/>, or <paramref name="jsonData"/> is null.</exception>
/// <exception cref="Exception">Thrown when JSON parsing fails or database operation encounters an error.</exception>
        public async Task<int> ImportFromJsonAsync(SQLiteConnection connection, string tableName,
            string jsonData, bool truncateTable = false)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            if (string.IsNullOrEmpty(jsonData))
                throw new ArgumentNullException(nameof(jsonData));

            try
            {
                using (var doc = JsonDocument.Parse(jsonData))
                {
                    var root = doc.RootElement;

                    List<JsonElement> records = new List<JsonElement>();

                    // Handle both array and {data: []} formats
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        records = root.EnumerateArray().ToList();
                    }
                    else if (root.TryGetProperty("data", out var dataArray))
                    {
                        records = dataArray.EnumerateArray().ToList();
                    }

                    return await ImportRecordsAsync(connection, tableName, records, truncateTable);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import JSON data into table {TableName}", tableName);
                throw;
            }
        }

        /// <summary>
/// Imports CSV data into a specified table with configurable delimiter and header row support.
/// </summary>
/// <param name="connection">The SQLite database connection to use for the import operation.</param>
/// <param name="tableName">Name of the table where data will be imported.</param>
/// <param name="csvData">CSV string containing the data to import.</param>
/// <param name="hasHeaders">If true, treats the first row as column headers.</param>
/// <param name="delimiter">The delimiter character used to separate values in the CSV data.</param>
/// <param name="truncateTable">If true, truncates the target table before importing data.</param>
/// <returns>Number of rows successfully imported.</returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/>, <paramref name="tableName"/>, or <paramref name="csvData"/> is null.</exception>
/// <exception cref="Exception">Thrown when CSV parsing fails or database operation encounters an error.</exception>
        public async Task<int> ImportFromCsvAsync(SQLiteConnection connection, string tableName,
            string csvData, bool hasHeaders = true, string delimiter = ",", bool truncateTable = false)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            if (string.IsNullOrEmpty(csvData))
                throw new ArgumentNullException(nameof(csvData));

            try
            {
                var lines = csvData.Split(new[] { "\r\n", "\r", "\n" },
                    StringSplitOptions.RemoveEmptyEntries);

                var columnNames = new string[0];
                var startIndex = 0;

                // Parse header row
                if (hasHeaders && lines.Length > 0)
                {
                    columnNames = ParseCsvLine(lines[0], delimiter);
                    startIndex = 1;
                }

                if (columnNames.Length == 0)
                {
                    _logger.LogWarning("No columns defined for CSV import into {TableName}", tableName);
                    return 0;
                }

                var rowsImported = 0;

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Truncate if requested
                        if (truncateTable)
                        {
                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.CommandText = $"DELETE FROM {tableName}";
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        // Insert data rows
                        for (int i = startIndex; i < lines.Length; i++)
                        {
                            var values = ParseCsvLine(lines[i], delimiter);

                            if (values.Length != columnNames.Length)
                            {
                                _logger.LogWarning("Row {RowIndex} has mismatched column count", i);
                                continue;
                            }

                            using (var command = connection.CreateCommand())
                            {
                                var columnList = string.Join(", ", columnNames.Select(c => $"[{c}]"));
                                var paramList = string.Join(", ", Enumerable.Range(0, columnNames.Length)
                                    .Select(j => $"@p{j}"));

                                command.CommandText = $"INSERT INTO [{tableName}] ({columnList}) VALUES ({paramList})";

                                for (int j = 0; j < columnNames.Length; j++)
                                {
                                    var value = string.IsNullOrEmpty(values[j]) ? (object)DBNull.Value : values[j];
                                    command.Parameters.AddWithValue($"@p{j}", value);
                                }

                                await command.ExecuteNonQueryAsync();
                                rowsImported++;
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }

                _logger.LogInformation("Imported {RowCount} rows into table {TableName}",
                    rowsImported, tableName);

                return rowsImported;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import CSV data into table {TableName}", tableName);
                throw;
            }
        }

        /// <summary>
/// Imports raw SQL INSERT statements into the database.
/// </summary>
/// <param name="connection">The SQLite database connection to use for the import operation.</param>
/// <param name="sqlStatements">SQL statements string containing INSERT statements separated by semicolons.</param>
/// <returns>Total number of rows affected by all INSERT statements.</returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> or <paramref name="sqlStatements"/> is null.</exception>
/// <exception cref="Exception">Thrown when SQL parsing fails or database operation encounters an error.</exception>
        public async Task<int> ImportFromSqlAsync(SQLiteConnection connection, string sqlStatements)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrEmpty(sqlStatements))
                throw new ArgumentNullException(nameof(sqlStatements));

            try
            {
                var statements = sqlStatements.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                var rowsAffected = 0;

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var statement in statements)
                        {
                            var trimmed = statement.Trim();
                            if (string.IsNullOrEmpty(trimmed))
                                continue;

                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = trimmed;
                                rowsAffected += await command.ExecuteNonQueryAsync();
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }

                return rowsAffected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import SQL statements");
                throw;
            }
        }

        /// <summary>
/// Imports a collection of JSON records into a specified table with transaction support.
/// This is an internal method used by <see cref="ImportFromJsonAsync"/>.
/// </summary>
/// <param name="connection">The SQLite database connection to use for the import operation.</param>
/// <param name="tableName">Name of the table where data will be imported.</param>
/// <param name="records">List of JSON elements representing the records to import.</param>
/// <param name="truncateTable">If true, truncates the target table before importing data.</param>
/// <returns>Number of rows successfully imported.</returns>
/// <exception cref="Exception">Thrown when database operation encounters an error.</exception>
private async Task<int> ImportRecordsAsync(SQLiteConnection connection, string tableName,
            List<JsonElement> records, bool truncateTable)
        {
            var rowsImported = 0;

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    if (truncateTable)
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = $"DELETE FROM {tableName}";
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    foreach (var record in records)
                    {
                        if (record.ValueKind != JsonValueKind.Object)
                            continue;

                        var properties = record.EnumerateObject().ToList();
                        var columns = properties.Select(p => p.Name).ToList();
                        var values = new List<object>();

                        foreach (var prop in properties)
                        {
                            values.Add(prop.Value.ValueKind == JsonValueKind.Null
                                ? DBNull.Value
                                : (object)prop.Value.GetString());
                        }

                        using (var command = connection.CreateCommand())
                        {
                            var columnList = string.Join(", ", columns.Select(c => $"[{c}]"));
                            var paramList = string.Join(", ", Enumerable.Range(0, columns.Count)
                                .Select(i => $"@p{i}"));

                            command.CommandText = $"INSERT INTO [{tableName}] ({columnList}) VALUES ({paramList})";

                            for (int i = 0; i < columns.Count; i++)
                            {
                                command.Parameters.AddWithValue($"@p{i}", values[i]);
                            }

                            await command.ExecuteNonQueryAsync();
                            rowsImported++;
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return rowsImported;
        }

        /// <summary>
/// Parses a single line of CSV data into an array of field values.
/// Handles quoted fields and escaped quotes according to CSV format specifications.
/// </summary>
/// <param name="line">The CSV line to parse.</param>
/// <param name="delimiter">The delimiter character used to separate values in the CSV data.</param>
/// <returns>Array of parsed field values.</returns>
private string[] ParseCsvLine(string line, string delimiter)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (line.Substring(i).StartsWith(delimiter) && !inQuotes)
                {
                    fields.Add(current.ToString().Trim('"'));
                    current.Clear();
                    i += delimiter.Length - 1;
                }
                else
                {
                    current.Append(ch);
                }
            }

            fields.Add(current.ToString().Trim('"'));
            return fields.ToArray();
        }

        /// <summary>
        /// Imports CSV data into a specified table for a tenant with transaction support and validation.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="tableName">Name of the table where data will be imported.</param>
        /// <param name="csvText">CSV string containing the data to import.</param>
        /// <returns>Number of rows successfully imported.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="tenantId"/> is less than or equal to 0.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tableName"/> or <paramref name="csvText"/> is null.</exception>
        /// <exception cref="Exception">Thrown when CSV parsing fails or database operation encounters an error.</exception>
        public async Task<int> ImportFromCsvAsync(string tenantId, string tableName, string csvText)
        {
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));

            if (tenantId.All(char.IsDigit) == false)
                throw new ArgumentException("TenantId must be numeric", nameof(tenantId));

            if (int.TryParse(tenantId, out var tenantIdInt) == false || tenantIdInt <= 0)
                throw new ArgumentOutOfRangeException(nameof(tenantId), "TenantId must be a positive integer");

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            if (string.IsNullOrEmpty(csvText))
                throw new ArgumentNullException(nameof(csvText));

            // Construct tenant database path (databases/{tenantId}_*.db)
            var tenantDbPath = Path.Combine("databases", $"{tenantId}_primary.db");

            // Get connection for the specified tenant
            await using var connection = new SQLiteConnection(
                new SQLiteConnectionStringBuilder
                {
                    DataSource = tenantDbPath,
                    Pooling = false
                }.ToString());

            await connection.OpenAsync();

            // Use existing CSV import logic with default parameters
            return await ImportFromCsvAsync(connection, tableName, csvText, hasHeaders: true, delimiter: ",", truncateTable: false);
        }
    }
}
