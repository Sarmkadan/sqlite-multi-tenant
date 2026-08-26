#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =========================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.BackgroundWorkers
{
    /// <summary>
    /// Implements data retention policies for managing old records.
    /// Automatically archives or deletes data based on configurable rules.
    /// </summary>
    public sealed class DataRetentionPolicy
    {
        private readonly ILogger<DataRetentionPolicy> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRetentionPolicy"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public DataRetentionPolicy(ILogger<DataRetentionPolicy> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override string ToString() => $"DataRetentionPolicy";

        /// <summary>
        /// Applies retention policy to a tenant's database.
        /// </summary>
        /// <param name="connection">The SQLite connection to the database.</param>
        /// <param name="policy">The retention policy configuration.</param>
        /// <returns>A <see cref="RetentionResult"/> containing the execution result.</returns>
        public async Task<RetentionResult> ApplyRetentionPolicyAsync(SQLiteConnection connection,
            RetentionPolicyConfig policy)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (policy is null)
                throw new ArgumentNullException(nameof(policy));

            var result = new RetentionResult();

            try
            {
                foreach (var rule in policy.Rules)
                {
                    var ruleResult = await ApplyRetentionRuleAsync(connection, rule);
                    result.ProcessedRules.Add(rule.TableName, ruleResult);
                    result.TotalRecordsDeleted += ruleResult.RecordsDeleted;
                }

                result.ExecutedAt = DateTime.UtcNow;
                result.IsSuccessful = true;

                _logger.LogInformation(
                    "Retention policy executed: {DeletedRecords} records deleted from {RuleCount} tables",
                    result.TotalRecordsDeleted, result.ProcessedRules.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply retention policy");
                result.IsSuccessful = false;
                result.Error = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Applies a single retention rule.
        /// </summary>
        /// <param name="connection">The SQLite connection to the database.</param>
        /// <param name="rule">The retention rule to apply.</param>
        /// <returns>A <see cref="RuleExecutionResult"/> containing the execution result.</returns>
        private async Task<RuleExecutionResult> ApplyRetentionRuleAsync(SQLiteConnection connection, RetentionRule rule)
        {
            var result = new RuleExecutionResult { TableName = rule.TableName };

            try
            {
                if (!rule.IsEnabled)
                {
                    result.Status = "Skipped - disabled";
                    return result;
                }

                // Determine cutoff date
                var cutoffDate = rule.RetentionType switch
                {
                    RetentionType.DaysOld => DateTime.UtcNow.AddDays(-rule.RetentionValue),
                    RetentionType.MonthsOld => DateTime.UtcNow.AddMonths(-rule.RetentionValue),
                    RetentionType.YearsOld => DateTime.UtcNow.AddYears(-rule.RetentionValue),
                    _ => DateTime.UtcNow.AddDays(-30)
                };

                // Build DELETE query based on rule
                string deleteQuery;
                if (rule.ArchiveBeforeDelete)
                {
                    // Archive records first
                    deleteQuery = $"@"
                    + $"INSERT INTO {rule.ArchiveTableName} "
                    + $"SELECT * FROM {rule.TableName} "
                    + $"WHERE {rule.DateColumn} < @cutoffDate";

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = deleteQuery;
                        command.Parameters.AddWithValue("@cutoffDate", cutoffDate);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Delete the records
                deleteQuery = $"DELETE FROM {rule.TableName} WHERE {rule.DateColumn} < @cutoffDate";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = deleteQuery;
                    command.Parameters.AddWithValue("@cutoffDate", cutoffDate);
                    result.RecordsDeleted = await command.ExecuteNonQueryAsync();
                }

                result.Status = "Completed";
                result.CutoffDate = cutoffDate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply retention rule for table {TableName}",
                    rule.TableName);
                result.Status = $"Failed: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Simulates applying retention policy without deleting any data.
        /// Returns a report of which rows WOULD be purged (counts per tenant, oldest/newest timestamps).
        /// </summary>
        /// <param name="connection">The SQLite connection to the database.</param>
        /// <param name="policy">The retention policy configuration.</param>
        /// <returns>A <see cref="DryRunResult"/> containing the dry-run report.</returns>
        public async Task<DryRunResult> SimulateRetentionPolicyAsync(SQLiteConnection connection,
            RetentionPolicyConfig policy)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (policy is null)
                throw new ArgumentNullException(nameof(policy));

            var result = new DryRunResult { TenantId = policy.TenantId };

            try
            {
                foreach (var rule in policy.Rules)
                {
                    var ruleResult = await SimulateRetentionRuleAsync(connection, rule);
                    result.ProcessedRules.Add(rule.TableName, ruleResult);
                    result.TotalRecordsWouldDelete += ruleResult.RecordsWouldDelete;
                }

                result.ExecutedAt = DateTime.UtcNow;
                result.IsSuccessful = true;

                _logger.LogInformation(
                    "Dry-run retention policy completed: {WouldDeleteRecords} records would be deleted from {RuleCount} tables",
                    result.TotalRecordsWouldDelete, result.ProcessedRules.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run dry-run retention policy");
                result.IsSuccessful = false;
                result.Error = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Simulates applying a single retention rule without deleting any data.
        /// </summary>
        /// <param name="connection">The SQLite connection to the database.</param>
        /// <param name="rule">The retention rule to simulate.</param>
        /// <returns>A <see cref="DryRunRuleResult"/> containing the simulation result.</returns>
        private async Task<DryRunRuleResult> SimulateRetentionRuleAsync(SQLiteConnection connection, RetentionRule rule)
        {
            var result = new DryRunRuleResult { TableName = rule.TableName };

            try
            {
                if (!rule.IsEnabled)
                {
                    result.Status = "Skipped - disabled";
                    return result;
                }

                // Determine cutoff date
                var cutoffDate = rule.RetentionType switch
                {
                    RetentionType.DaysOld => DateTime.UtcNow.AddDays(-rule.RetentionValue),
                    RetentionType.MonthsOld => DateTime.UtcNow.AddMonths(-rule.RetentionValue),
                    RetentionType.YearsOld => DateTime.UtcNow.AddYears(-rule.RetentionValue),
                    _ => DateTime.UtcNow.AddDays(-30)
                };

                // Query to count records that would be deleted
                string countQuery = $"SELECT COUNT(*) FROM {rule.TableName} WHERE {rule.DateColumn} < @cutoffDate";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = countQuery;
                    command.Parameters.AddWithValue("@cutoffDate", cutoffDate);
                    result.RecordsWouldDelete = Convert.ToInt32(await command.ExecuteScalarAsync());
                }

                // Get oldest and newest timestamps for reporting
                if (result.RecordsWouldDelete > 0)
                {
                    string oldestQuery = $"SELECT MIN({rule.DateColumn}) FROM {rule.TableName} WHERE {rule.DateColumn} < @cutoffDate";
                    string newestQuery = $"SELECT MAX({rule.DateColumn}) FROM {rule.TableName} WHERE {rule.DateColumn} < @cutoffDate";

                    using (var oldestCommand = connection.CreateCommand())
                    {
                        oldestCommand.CommandText = oldestQuery;
                        oldestCommand.Parameters.AddWithValue("@cutoffDate", cutoffDate);
                        var oldestResult = await oldestCommand.ExecuteScalarAsync();
                        result.OldestWouldDelete = oldestResult != DBNull.Value ? (DateTime?)oldestResult : null;
                    }

                    using (var newestCommand = connection.CreateCommand())
                    {
                        newestCommand.CommandText = newestQuery;
                        newestCommand.Parameters.AddWithValue("@cutoffDate", cutoffDate);
                        var newestResult = await newestCommand.ExecuteScalarAsync();
                        result.NewestWouldDelete = newestResult != DBNull.Value ? (DateTime?)newestResult : null;
                    }
                }

                result.Status = "Completed";
                result.CutoffDate = cutoffDate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to simulate retention rule for table {TableName}", rule.TableName);
                result.Status = $"Failed: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Gets the current retention configuration.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <returns>A <see cref="RetentionPolicyConfig"/> containing the default policy configuration.</returns>
        public RetentionPolicyConfig GetDefaultPolicy(string tenantId)
        {
            return new RetentionPolicyConfig
            {
                TenantId = tenantId,
                Rules = new List<RetentionRule>
                {
                    new RetentionRule
                    {
                        TableName = "AuditLog",
                        DateColumn = "CreatedAt",
                        RetentionType = RetentionType.YearsOld,
                        RetentionValue = 7,
                        IsEnabled = true,
                        ArchiveBeforeDelete = false
                    }
                }
            };
        }
    }

    /// <summary>
    /// Represents a retention policy configuration.
    /// </summary>
    public sealed class RetentionPolicyConfig
    {
        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the list of retention rules.
        /// </summary>
        public List<RetentionRule> Rules { get; set; } = new List<RetentionRule>();

        /// <summary>
        /// Gets or sets a value indicating whether to auto-execute the policy.
        /// </summary>
        public bool AutoExecute { get; set; }

        public override string ToString() => $"RetentionPolicyConfig {{ TenantId = {TenantId}, Rules = {Rules}, AutoExecute = {AutoExecute} }}";
    }

    /// <summary>
    /// Represents a retention rule.
    /// </summary>
    public sealed class RetentionRule
    {
        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the date column name.
        /// </summary>
        public string DateColumn { get; set; }

        /// <summary>
        /// Gets or sets the retention type.
        /// </summary>
        public RetentionType RetentionType { get; set; }

        /// <summary>
        /// Gets or sets the retention value.
        /// </summary>
        public int RetentionValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the rule is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to archive before deleting.
        /// </summary>
        public bool ArchiveBeforeDelete { get; set; }

        /// <summary>
        /// Gets or sets the archive table name.
        /// </summary>
        public string ArchiveTableName { get; set; }
    }

    /// <summary>
    /// Represents a retention type.
    /// </summary>
    public enum RetentionType
    {
        /// <summary>
        /// Days old.
        /// </summary>
        DaysOld,
        /// <summary>
        /// Months old.
        /// </summary>
        MonthsOld,
        /// <summary>
        /// Years old.
        /// </summary>
        YearsOld
    }

    /// <summary>
    /// Represents a retention result.
    /// </summary>
    public sealed class RetentionResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the policy was executed successfully.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the total number of records deleted.
        /// </summary>
        public int TotalRecordsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the execution date and time.
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// Gets or sets the error message (if any).
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of processed rules.
        /// </summary>
        public Dictionary<string, RuleExecutionResult> ProcessedRules { get; set; } =
            new Dictionary<string, RuleExecutionResult>();
    }

    /// <summary>
    /// Represents a rule execution result.
    /// </summary>
    public sealed class RuleExecutionResult
    {
        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the number of records deleted.
        /// </summary>
        public int RecordsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the cutoff date (if any).
        /// </summary>
        public DateTime? CutoffDate { get; set; }
    }

    /// <summary>
    /// Represents a dry-run retention result.
    /// </summary>
    public sealed class DryRunResult
    {
        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the simulation was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the total number of records that would be deleted.
        /// </summary>
        public int TotalRecordsWouldDelete { get; set; }

        /// <summary>
        /// Gets or sets the execution date and time.
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// Gets or sets the error message (if any).
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of processed rules.
        /// </summary>
        public Dictionary<string, DryRunRuleResult> ProcessedRules { get; set; } =
            new Dictionary<string, DryRunRuleResult>();
    }

    /// <summary>
    /// Represents a dry-run rule result.
    /// </summary>
    public sealed class DryRunRuleResult
    {
        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the number of records that would be deleted.
        /// </summary>
        public int RecordsWouldDelete { get; set; }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the cutoff date (if any).
        /// </summary>
        public DateTime? CutoffDate { get; set; }

        /// <summary>
        /// Gets or sets the oldest timestamp that would be deleted (if any).
        /// </summary>
        public DateTime? OldestWouldDelete { get; set; }

        /// <summary>
        /// Gets or sets the newest timestamp that would be deleted (if any).
        /// </summary>
        public DateTime? NewestWouldDelete { get; set; }
    }
}
