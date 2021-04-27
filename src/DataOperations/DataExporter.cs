#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.DataOperations
{
    // Exports tenant data in multiple formats (JSON, CSV, SQL)
    // Enables data portability and integration with external systems
    public sealed class DataExporter {
        private readonly ILogger<DataExporter> _logger;

        public DataExporter(ILogger<DataExporter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Exports table data as JSON with configurable formatting
        public async Task<string> ExportAsJsonAsync(SQLiteConnection connection, string tableName,
            bool includeMeta = true)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            try
            {
                var rows = new List<Dictionary<string, object>>();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM {tableName}";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var fieldCount = reader.FieldCount;
                        var fieldNames = new string[fieldCount];

                        for (int i = 0; i < fieldCount; i++)
                        {
                            fieldNames[i] = reader.GetName(i);
                        }

                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < fieldCount; i++)
                            {
                                row[fieldNames[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            rows.Add(row);
                        }
                    }
                }

                var metadata = new { Table = tableName, RowCount = rows.Count, ExportedAt = DateTime.UtcNow };
                var output = includeMeta
                    ? new { meta = metadata, data = rows }
                    : (object)rows;

                return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export table {TableName} as JSON", tableName);
                throw;
            }
        }

        // Exports table data as CSV with proper escaping and quoting
        public async Task<string> ExportAsCsvAsync(SQLiteConnection connection, string tableName,
            bool includeHeaders = true)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            try
            {
                var csv = new StringBuilder();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM {tableName}";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var fieldCount = reader.FieldCount;

                        // Write headers
                        if (includeHeaders)
                        {
                            for (int i = 0; i < fieldCount; i++)
                            {
                                if (i > 0) csv.Append(",");
                                csv.Append(EscapeCsvField(reader.GetName(i)));
                            }
                            csv.AppendLine();
                        }

                        // Write data rows
                        while (await reader.ReadAsync())
                        {
                            for (int i = 0; i < fieldCount; i++)
                            {
                                if (i > 0) csv.Append(",");

                                var value = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i).ToString();
                                csv.Append(EscapeCsvField(value));
                            }
                            csv.AppendLine();
                        }
                    }
                }

                return csv.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export table {TableName} as CSV", tableName);
                throw;
            }
        }

        // Exports entire database as SQL INSERT statements
        public async Task<string> ExportAsSqlAsync(SQLiteConnection connection, string tableName)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            try
            {
                var sql = new StringBuilder();
                sql.AppendLine($"-- Export of {tableName} from SQLite");
                sql.AppendLine($"-- Generated at {DateTime.UtcNow:O}");
                sql.AppendLine();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM {tableName}";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var fieldCount = reader.FieldCount;
                        var fieldNames = new string[fieldCount];

                        for (int i = 0; i < fieldCount; i++)
                        {
                            fieldNames[i] = reader.GetName(i);
                        }

                        while (await reader.ReadAsync())
                        {
                            var fields = new StringBuilder();
                            var values = new StringBuilder();

                            for (int i = 0; i < fieldCount; i++)
                            {
                                if (i > 0)
                                {
                                    fields.Append(", ");
                                    values.Append(", ");
                                }

                                fields.Append($"[{fieldNames[i]}]");

                                if (reader.IsDBNull(i))
                                    values.Append("NULL");
                                else
                                    values.Append($"'{reader.GetValue(i).ToString().Replace("'", "''")}'");
                            }

                            sql.AppendLine(
                                $"INSERT INTO [{tableName}] ({fields}) VALUES ({values});");
                        }
                    }
                }

                return sql.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export table {TableName} as SQL", tableName);
                throw;
            }
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            if (field.Contains("\"") || field.Contains(",") || field.Contains("\n"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }
    }
}
