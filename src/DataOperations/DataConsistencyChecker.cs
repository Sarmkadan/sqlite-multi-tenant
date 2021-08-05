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
    // Verifies data integrity and consistency across tenant databases
    // Detects orphaned records, constraint violations, and missing indexes
    public class DataConsistencyChecker
    {
        private readonly ILogger<DataConsistencyChecker> _logger;

        public DataConsistencyChecker(ILogger<DataConsistencyChecker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Runs complete consistency check on database
        public async Task<ConsistencyCheckResult> CheckDatabaseIntegrityAsync(SQLiteConnection connection)
        {
            if (connection == null)
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

        // Checks for duplicate records using fuzzy matching
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

        // Validates record counts against expected sizes
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

    public class ConsistencyCheckResult
    {
        public bool IsHealthy { get; set; }
        public bool IntegrityCheckPassed { get; set; }
        public List<string> OrphanedRecords { get; set; } = new List<string>();
        public List<ConstraintViolation> ForeignKeyViolations { get; set; } = new List<ConstraintViolation>();
        public List<string> MissingIndexes { get; set; } = new List<string>();
        public Dictionary<string, TableStatistics> TableStatistics { get; set; } = new Dictionary<string, TableStatistics>();
        public DateTime CheckedAt { get; set; }
    }

    public class ConstraintViolation
    {
        public string Table { get; set; }
        public long Rowid { get; set; }
        public string ParentTable { get; set; }
        public long ParentRowid { get; set; }
    }

    public class TableStatistics
    {
        public string TableName { get; set; }
        public long RowCount { get; set; }
    }

    public class DuplicateRecord
    {
        public string TableName { get; set; }
        public int RecordIndex1 { get; set; }
        public int RecordIndex2 { get; set; }
        public double Similarity { get; set; }
    }
}
