#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.BackgroundWorkers
{
    /// <summary>
    /// Implements data retention policies for managing old records.
    /// Automatically archives or deletes data based on configurable rules.
    /// </summary>
    public sealed class DataRetentionPolicy {
        private readonly ILogger<DataRetentionPolicy> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRetentionPolicy"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public DataRetentionPolicy(ILogger<DataRetentionPolicy> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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
        private async Task<RuleExecutionResult> ApplyRetentionRuleAsync(SQLiteConnection connection,
            RetentionRule rule)
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
                    deleteQuery = $@"
                        INSERT INTO {rule.ArchiveTableName}
                        SELECT * FROM {rule.TableName}
                        WHERE {rule.DateColumn} < @cutoffDate";

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
    public sealed class RetentionPolicyConfig {
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
    }

    /// <summary>
    /// Represents a retention rule.
    /// </summary>
    public sealed class RetentionRule {
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
    public sealed class RetentionResult {
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
    public sealed class RuleExecutionResult {
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
}
