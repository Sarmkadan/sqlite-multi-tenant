#nullable enable
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

namespace SqliteMultiTenant.DataOperations
{
    /// <summary>
/// Provides functionality to verify data integrity and consistency across tenant databases.
/// Detects orphaned records, constraint violations, missing indexes, and duplicate records.
/// </summary>
// Verifies data integrity and consistency across tenant databases
    // Detects orphaned records, constraint violations, missing indexes, and duplicate records
    public sealed class DataConsistencyChecker {
        private readonly ILogger<DataConsistencyChecker> _logger;

        /// <summary>
/// Initializes a new instance of the <see cref="DataConsistencyChecker"/> class.
/// </summary>
/// <param name="logger">The logger instance to use for logging operations.</param>
/// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
public DataConsistencyChecker(ILogger<DataConsistencyChecker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
/// Runs a complete consistency check on the database.
/// </summary>
/// <param name="connection">The SQLite database connection to check.</param>
/// <returns>A <see cref="ConsistencyCheckResult"/> containing the results of all consistency checks.</returns>
/// <exception cref="ArgumentNullException">Thrown when connection is null.</exception>
        public async Task<ConsistencyCheckResult> CheckDatabaseIntegrityAsync(SQLiteConnection connection)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            try
            {
                var result = new ConsistencyCheckResult();

                // Run PRAGMA integrity_check
                result.IntegrityCheckPassed = await VerifyDatabaseIntegrityAsync(connection);

                // Check for orphaned records
                result.OrphanedRecords = await FindOrphanedRecordsAsync(connection);

                // Verify foreign keys
                result.ForeignKeyViolations = await CheckForeignKeyConstraintsAsync(connection);

                // Check indexes
                result.MissingIndexes = await FindMissingIndexesAsync(connection);

                // Verify row counts
                result.TableStatistics = await GetTableStatisticsAsync(connection);

                result.CheckedAt = DateTime.UtcNow;
                result.IsHealthy = result.IntegrityCheckPassed
                    && !result.OrphanedRecords.Any()
                    && !result.ForeignKeyViolations.Any();

                _logger.LogInformation("Database consistency check completed. Healthy: {IsHealthy}",
                    result.IsHealthy);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database consistency check failed");
                throw;
            }
        }

        /// <summary>
/// Finds duplicate records in a table using fuzzy matching on specified key columns.
/// </summary>
/// <param name="connection">The SQLite database connection to use.</param>
/// <param name="tableName">Name of the table to search for duplicates.</param>
/// <param name="keyColumns">Array of column names to use for comparison.</param>
/// <param name="similarityThreshold">Minimum similarity score (0-1) to consider records as duplicates. Default is 0.95.</param>
/// <returns>A list of <see cref="DuplicateRecord"/> objects representing found duplicates.</returns>
/// <exception cref="ArgumentNullException">Thrown when connection, tableName, or keyColumns is null.</exception>
        public async Task<List<DuplicateRecord>> FindDuplicatesAsync(SQLiteConnection connection,
            string tableName, string[] keyColumns, double similarityThreshold = 0.95)
        {
            var duplicates = new List<DuplicateRecord>();

            try
            {
                var records = new List<Dictionary<string, object>>();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM {tableName}";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var fieldCount = reader.FieldCount;

                        while (await reader.ReadAsync())
                        {
                            var record = new Dictionary<string, object>();
                            for (int i = 0; i < fieldCount; i++)
                            {
                                record[reader.GetName(i)] = reader.GetValue(i);
                            }
                            records.Add(record);
                        }
                    }
                }

                // Find potential duplicates by comparing key columns
                for (int i = 0; i < records.Count; i++)
                {
                    for (int j = i + 1; j < records.Count; j++)
                    {
                        var similarity = CalculateSimilarity(records[i], records[j], keyColumns);

                        if (similarity >= similarityThreshold)
                        {
                            duplicates.Add(new DuplicateRecord
                            {
                                TableName = tableName,
                                RecordIndex1 = i,
                                RecordIndex2 = j,
                                Similarity = similarity
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to find duplicates in table {TableName}", tableName);
            }

            return duplicates;
        }

        /// <summary>
/// Validates that table record counts match expected values.
/// </summary>
/// <param name="connection">The SQLite database connection to use.</param>
/// <param name="expectedCounts">Dictionary mapping table names to expected record counts.</param>
/// <returns>True if all record counts match expected values; otherwise false.</returns>
/// <exception cref="ArgumentNullException">Thrown when connection or expectedCounts is null.</exception>
        public async Task<bool> ValidateRecordCountsAsync(SQLiteConnection connection,
            Dictionary<string, int> expectedCounts)
        {
            try
            {
                foreach (var kvp in expectedCounts)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT COUNT(*) FROM {kvp.Key}";
                        var actualCount = (long)await command.ExecuteScalarAsync();

                        if (actualCount != kvp.Value)
                        {
                            _logger.LogWarning(
                                "Record count mismatch for table {Table}: expected {Expected}, got {Actual}",
                                kvp.Key, kvp.Value, actualCount);

                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate record counts");
                return false;
            }
        }

        private async Task<bool> VerifyDatabaseIntegrityAsync(SQLiteConnection connection)
        {
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA integrity_check";
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString() == "ok";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database integrity check failed");
                return false;
            }
        }

        private async Task<List<string>> FindOrphanedRecordsAsync(SQLiteConnection connection)
        {
            var orphaned = new List<string>();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    // Check for foreign key constraint violations
                    command.CommandText = "PRAGMA foreign_key_check";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            orphaned.Add(
                                $"Table: {reader.GetValue(0)}, " +
                                $"Rowid: {reader.GetValue(1)}, " +
                                $"Parent Table: {reader.GetValue(2)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to find orphaned records");
            }

            return orphaned;
        }

        private async Task<List<ConstraintViolation>> CheckForeignKeyConstraintsAsync(SQLiteConnection connection)
        {
            var violations = new List<ConstraintViolation>();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA foreign_key_check";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            violations.Add(new ConstraintViolation
                            {
                                Table = reader.GetString(0),
                                Rowid = reader.GetInt64(1),
                                ParentTable = reader.GetString(2),
                                ParentRowid = reader.GetInt64(3)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check foreign key constraints");
            }

            return violations;
        }

        private async Task<List<string>> FindMissingIndexesAsync(SQLiteConnection connection)
        {
            var missing = new List<string>();

            // This would typically check against a known set of recommended indexes
            // For now, it returns commonly expected indexes that are missing

            var commonIndexes = new[]
            {
                ("AuditLog", "TenantId"),
                ("AuditLog", "CreatedAt")
            };

            try
            {
                foreach (var (table, column) in commonIndexes)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText =
                            $@"SELECT COUNT(*) FROM sqlite_master
                               WHERE type='index' AND tbl_name='{table}' AND sql LIKE '%{column}%'";

                        var exists = (long)await command.ExecuteScalarAsync();
                        if (exists == 0)
                        {
                            missing.Add($"Missing index on {table}.{column}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to find missing indexes");
            }

            return missing;
        }

        private async Task<Dictionary<string, TableStatistics>> GetTableStatisticsAsync(SQLiteConnection connection)
        {
            var stats = new Dictionary<string, TableStatistics>();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var tableName = reader.GetString(0);

                            using (var countCmd = connection.CreateCommand())
                            {
                                countCmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
                                var rowCount = (long)await countCmd.ExecuteScalarAsync();

                                stats[tableName] = new TableStatistics
                                {
                                    TableName = tableName,
                                    RowCount = rowCount
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get table statistics");
            }

            return stats;
        }

        private double CalculateSimilarity(Dictionary<string, object> record1,
            Dictionary<string, object> record2, string[] keyColumns)
        {
            int matches = 0;

            foreach (var column in keyColumns)
            {
                if (record1.TryGetValue(column, out var val1) &&
                    record2.TryGetValue(column, out var val2))
                {
                    if (Equals(val1, val2))
                    {
                        matches++;
                    }
                }
            }

            return (double)matches / keyColumns.Length;
        }
    }

    /// <summary>
/// Represents the results of a database consistency check.
/// </summary>
public sealed class ConsistencyCheckResult {
        /// <summary>
/// Gets or sets a value indicating whether the database is healthy (all checks passed).
/// </summary>
public bool IsHealthy { get; set; }
        /// <summary>
/// Gets or sets a value indicating whether the database integrity check passed.
/// </summary>
public bool IntegrityCheckPassed { get; set; }
        /// <summary>
/// Gets or sets a list of descriptions of orphaned records found during the check.
/// </summary>
public List<string> OrphanedRecords { get; set; } = new List<string>();
        /// <summary>
/// Gets or sets a list of foreign key constraint violations found during the check.
/// </summary>
public List<ConstraintViolation> ForeignKeyViolations { get; set; } = new List<ConstraintViolation>();
        /// <summary>
/// Gets or sets a list of missing indexes that were expected but not found.
/// </summary>
public List<string> MissingIndexes { get; set; } = new List<string>();
        /// <summary>
/// Gets or sets a dictionary containing statistics for each table in the database.
/// </summary>
public Dictionary<string, TableStatistics> TableStatistics { get; set; } = new Dictionary<string, TableStatistics>();
        /// <summary>
/// Gets or sets the timestamp when the consistency check was performed.
/// </summary>
public DateTime CheckedAt { get; set; }
    }

    /// <summary>
/// Represents a foreign key constraint violation in the database.
/// </summary>
public sealed class ConstraintViolation {
        /// <summary>
/// Gets or sets the name of the table that contains the violating record.
/// </summary>
public string Table { get; set; }
        /// <summary>
/// Gets or sets the row ID of the violating record.
/// </summary>
public long Rowid { get; set; }
        /// <summary>
/// Gets or sets the name of the parent table that should have a valid reference.
/// </summary>
public string ParentTable { get; set; }
        /// <summary>
/// Gets or sets the row ID of the parent record that should have a valid reference.
/// </summary>
public long ParentRowid { get; set; }
    }

    /// <summary>
/// Contains statistics for a specific table in the database.
/// </summary>
public sealed class TableStatistics {
        public string TableName { get; set; }
        public long RowCount { get; set; }
    }

    public sealed class DuplicateRecord {
        public string TableName { get; set; }
        public int RecordIndex1 { get; set; }
        public int RecordIndex2 { get; set; }
        public double Similarity { get; set; }
    }
}
