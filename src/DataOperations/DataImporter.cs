// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.DataOperations
{
    // Imports data into tenant databases from various formats
    // Includes validation, transaction support, and rollback capability
    public class DataImporter
    {
        private readonly ILogger<DataImporter> _logger;

        public DataImporter(ILogger<DataImporter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Imports JSON data into a table with transaction support and validation
        public async Task<int> ImportFromJsonAsync(SQLiteConnection connection, string tableName,
            string jsonData, bool truncateTable = false)
        {
            if (connection == null)
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

        // Imports CSV data with configurable delimiter and header row
        public async Task<int> ImportFromCsvAsync(SQLiteConnection connection, string tableName,
            string csvData, bool hasHeaders = true, string delimiter = ",", bool truncateTable = false)
        {
            if (connection == null)
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

        // Imports SQL INSERT statements
        public async Task<int> ImportFromSqlAsync(SQLiteConnection connection, string sqlStatements)
        {
            if (connection == null)
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
    }
}
