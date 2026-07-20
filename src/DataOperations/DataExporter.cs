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
    /// <summary>
    /// Provides functionality to export data from a SQLite database table into
    /// various portable formats such as JSON, CSV, and raw SQL INSERT statements.
    /// </summary>
    public sealed class DataExporter {
        private readonly ILogger<DataExporter> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataExporter"/> class.
        /// </summary>
        /// <param name="logger">
        /// The <see cref="ILogger{DataExporter}"/> used to record diagnostic information
        /// and errors that occur during export operations.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        public DataExporter(ILogger<DataExporter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Exports all rows from the specified <paramref name="tableName"/> as a JSON string.
        /// </summary>
        /// <param name="connection">
        /// An open <see cref="SQLiteConnection"/> used to query the table.
        /// </param>
        /// <param name="tableName">
        /// The name of the table whose data should be exported.
        /// </param>
        /// <param name="includeMeta">
        /// When <c>true</c>, the returned JSON includes a <c>meta</c> object containing the
        /// table name, row count, and export timestamp; otherwise only the data array is returned.
        /// </param>
        /// <returns>
        /// A JSON-formatted <see cref="string"/> representing the table rows (and optional metadata).
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="connection"/> or <paramref name="tableName"/> is <c>null</c>
        /// or empty.
        /// </exception>
        /// <exception cref="Exception">
        /// Propagates any exception that occurs while reading from the database or serializing the result.
        /// </exception>
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

        /// <summary>
        /// Exports all rows from the specified <paramref name="tableName"/> as a CSV string.
        /// </summary>
        /// <param name="connection">
        /// An open <see cref="SQLiteConnection"/> used to query the table.
        /// </param>
        /// <param name="tableName">
        /// The name of the table whose data should be exported.
        /// </param>
        /// <param name="includeHeaders">
        /// When <c>true</c>, the first line of the CSV contains column names; otherwise only data rows are emitted.
        /// </param>
        /// <returns>
        /// A CSV-formatted <see cref="string"/> representing the table rows.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="connection"/> or <paramref name="tableName"/> is <c>null</c>
        /// or empty.
        /// </exception>
        /// <exception cref="Exception">
        /// Propagates any exception that occurs while reading from the database.
        /// </exception>
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

        /// <summary>
        /// Exports all rows from the specified <paramref name="tableName"/> as a series of
        /// SQL <c>INSERT</c> statements suitable for recreating the data in another SQLite database.
        /// </summary>
        /// <param name="connection">
        /// An open <see cref="SQLiteConnection"/> used to query the table.
        /// </param>
        /// <param name="tableName">
        /// The name of the table whose data should be exported.
        /// </param>
        /// <returns>
        /// A string containing SQL statements that insert each row from the source table.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="connection"/> or <paramref name="tableName"/> is <c>null</c>
        /// or empty.
        /// </exception>
        /// <exception cref="Exception">
        /// Propagates any exception that occurs while reading from the database.
        /// </exception>
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
                                {
                                    values.Append("NULL");
                                }
                                else
                                {
                                    var value = reader.GetValue(i);
                                    if (value is bool boolValue)
                                    {
                                        values.Append(boolValue ? "1" : "0");
                                    }
                                    else if (value is sbyte or byte or short or ushort or int or uint
                                        or long or ulong or float or double or decimal)
                                    {
                                        values.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                                    }
                                    else
                                    {
                                        values.Append($"'{value.ToString().Replace("'", "''")}'");
                                    }
                                }
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

    /// <summary>
    /// Exports all rows from the specified <paramref name="tableName"/> as a JSON Lines (.jsonl) file.
    /// Each row is serialized as a separate JSON object on its own line.
    /// </summary>
    /// <param name="connection">
    /// An open <see cref="SQLiteConnection"/> used to query the table.
    /// </param>
    /// <param name="tableName">
    /// The name of the table whose data should be exported.
    /// </param>
    /// <param name="outputPath">
    /// The file path where the JSON Lines output will be written.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous export operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="connection"/>, <paramref name="tableName"/>, or <paramref name="outputPath"/> is <c>null</c>
    /// or empty.
    /// </exception>
    /// <exception cref="Exception">
    /// Propagates any exception that occurs while reading from the database or writing to the file.
    /// </exception>
    public async Task ExportAsJsonLinesAsync(SQLiteConnection connection, string tableName, string outputPath)
    {
        if (connection is null)
            throw new ArgumentNullException(nameof(connection));

        if (string.IsNullOrEmpty(tableName))
            throw new ArgumentNullException(nameof(tableName));

        if (string.IsNullOrEmpty(outputPath))
            throw new ArgumentNullException(nameof(outputPath));

        try
        {
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

                    // Stream directly to file instead of materializing all rows in memory
                    await using (var fileStream = System.IO.File.Create(outputPath))
                    await using (var writer = new System.IO.StreamWriter(fileStream, Encoding.UTF8))
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object?>();
                            for (int i = 0; i < fieldCount; i++)
                            {
                                row[fieldNames[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }

                            var json = JsonSerializer.Serialize(row, new JsonSerializerOptions { WriteIndented = false });
                            await writer.WriteLineAsync(json);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export table {TableName} as JSON Lines", tableName);
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
