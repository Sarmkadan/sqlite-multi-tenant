#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===========================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Operations
{
    /// <summary>
    /// Builder for efficient bulk insert operations with batching support.
    /// Provides fluent interface for adding records and executing bulk insert operations
    /// with transaction management and batch processing capabilities.
    /// </summary>
    public sealed class BulkInsertBuilder
    {
        private readonly SQLiteConnection _connection;
        private readonly ILogger<BulkInsertBuilder> _logger;
        private readonly string _tableName;
        private readonly List<Dictionary<string, object>> _records;
        private readonly int _batchSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkInsertBuilder"/> class.
        /// </summary>
        /// <param name="connection">The SQLite database connection to use for bulk insert operations.</param>
        /// <param name="logger">The logger instance for recording operation details and errors.</param>
        /// <param name="tableName">Name of the table where records will be inserted.</param>
        /// <param name="batchSize">Maximum number of records to process in each batch. Defaults to 1000 if not specified.</param>
        /// <exception cref="ArgumentNullException">Thrown when connection or logger is null.</exception>
        /// <exception cref="ArgumentException">Thrown when tableName is null or whitespace.</exception>
        public BulkInsertBuilder(SQLiteConnection connection, ILogger<BulkInsertBuilder> logger,
            string tableName, int batchSize = 1000)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be empty", nameof(tableName));

            _connection = connection;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tableName = tableName;
            _records = new List<Dictionary<string, object>>();
            _batchSize = batchSize > 0 ? batchSize : 1000;
        }

        // Adds a record to the bulk insert
        public BulkInsertBuilder AddRecord(Dictionary<string, object> record)
        {
            if (record is null)
                throw new ArgumentNullException(nameof(record));

            _records.Add(record);
            return this;
        }

        // Adds multiple records
        public BulkInsertBuilder AddRecords(IEnumerable<Dictionary<string, object>> records)
        {
            if (records is null)
                throw new ArgumentNullException(nameof(records));

            _records.AddRange(records);
            return this;
        }

        // Executes the bulk insert in batches
        public async Task<BulkInsertResult> ExecuteAsync()
        {
            var result = new BulkInsertResult { TotalRecords = _records.Count };

            if (_records.Count == 0)
            {
                _logger.LogWarning("No records to insert");
                return result;
            }

            try
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        // Process in batches
                        for (int i = 0; i < _records.Count; i += _batchSize)
                        {
                            var batch = _records.Skip(i).Take(_batchSize).ToList();
                            var insertedCount = await InsertBatchAsync(batch);
                            result.InsertedRecords += insertedCount;
                        }

                        transaction.Commit();
                        result.IsSuccessful = true;

                        _logger.LogInformation(
                            "Bulk insert completed: {Inserted}/{Total} records",
                            result.InsertedRecords, result.TotalRecords);
                    }
                    catch
                    {
                        transaction.Rollback();
                        result.IsSuccessful = false;
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk insert operation failed");
                result.Error = ex.Message;
                throw;
            }

            return result;
        }

        private async Task<int> InsertBatchAsync(List<Dictionary<string, object>> batch)
        {
            if (batch.Count == 0)
                return 0;

            var insertedCount = 0;
            var columnNames = batch[0].Keys.ToList();

            foreach (var record in batch)
            {
                var columnList = string.Join(", ", columnNames.Select(c => $"[{c}]"));
                var paramList = string.Join(", ", columnNames.Select((_, i) => $"@p{i}"));

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = $"INSERT INTO [{_tableName}] ({columnList}) VALUES ({paramList})\n";

                    for (int i = 0; i < columnNames.Count; i++)
                    {
                        var value = record.ContainsKey(columnNames[i])
                            ? (object)record[columnNames[i]] ?? DBNull.Value
                            : DBNull.Value;

                        command.Parameters.AddWithValue($"@p{i}", value);
                    }

                    insertedCount += await command.ExecuteNonQueryAsync();
                }
            }

            return insertedCount;
        }

        // Generates SQL INSERT statements without executing
        public string GenerateSqlStatements()
        {
            var sql = new StringBuilder();

            var columnNames = _records.FirstOrDefault()?.Keys.ToList();
            if (columnNames is null || columnNames.Count == 0)
                return "";

            foreach (var record in _records)
            {
                var columnList = string.Join(", ", columnNames.Select(c => $"[{c}]"));
                var values = new List<string>();

                foreach (var columnName in columnNames)
                {
                    if (record.ContainsKey(columnName))
                    {
                        var value = record[columnName];
                        values.Add(value is null ? "NULL" : $"'{value.ToString().Replace("'", "''")}'");
                    }
                    else
                    {
                        values.Add("NULL");
                    }
                }

                var valueList = string.Join(", ", values);
                sql.AppendLine($"INSERT INTO [{_tableName}] ({columnList}) VALUES ({valueList});");
            }

            return sql.ToString();
        }
    }

    public sealed class BulkInsertResult
    {
        public int TotalRecords { get; set; }
        public int InsertedRecords { get; set; }
        public bool IsSuccessful { get; set; }
        public string Error { get; set; }
    }

    // Builder for bulk update operations
    public sealed class BulkUpdateBuilder
    {
        private readonly SQLiteConnection _connection;
        private readonly ILogger<BulkUpdateBuilder> _logger;
        private readonly string _tableName;
        private readonly string _whereClause;
        private readonly List<KeyValuePair<string, object>> _updates;

        public BulkUpdateBuilder(SQLiteConnection connection, ILogger<BulkUpdateBuilder> logger,
            string tableName, string whereClause)
        {
            _connection = connection;
            _logger = logger;
            _tableName = tableName;
            _whereClause = whereClause;
            _updates = new List<KeyValuePair<string, object>>();
        }

        // Adds an update
        public BulkUpdateBuilder Set(string column, object value)
        {
            _updates.Add(new KeyValuePair<string, object>(column, value));
            return this;
        }

        // Executes the bulk update
        public async Task<BulkUpdateResult> ExecuteAsync()
        {
            var result = new BulkUpdateResult();

            if (_updates.Count == 0)
            {
                _logger.LogWarning("No updates specified");
                return result;
            }

            try
            {
                var setClause = string.Join(", ",
                    _updates.Select((u, i) => $"[{u.Key}] = @update{i}"));

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = $"UPDATE [{_tableName}] SET {setClause} WHERE {_whereClause}";

                    for (int i = 0; i < _updates.Count; i++)
                    {
                        command.Parameters.AddWithValue($"@update{i}",
                            _updates[i].Value ?? DBNull.Value);
                    }

                    result.AffectedRows = await command.ExecuteNonQueryAsync();
                    result.IsSuccessful = true;

                    _logger.LogInformation("Bulk update completed: {AffectedRows} rows updated",
                        result.AffectedRows);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk update operation failed");
                result.Error = ex.Message;
            }

            return result;
        }
    }

    public sealed class BulkUpdateResult
    {
        public int AffectedRows { get; set; }
        public bool IsSuccessful { get; set; }
        public string Error { get; set; }
    }
}