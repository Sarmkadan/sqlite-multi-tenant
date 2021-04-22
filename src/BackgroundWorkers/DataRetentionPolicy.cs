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
    // Implements data retention policies for managing old records
    // Automatically archives or deletes data based on configurable rules
    public class DataRetentionPolicy
    {
        private readonly ILogger<DataRetentionPolicy> _logger;

        public DataRetentionPolicy(ILogger<DataRetentionPolicy> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Applies retention policy to a tenant's database
        public async Task<RetentionResult> ApplyRetentionPolicyAsync(SQLiteConnection connection,
            RetentionPolicyConfig policy)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (policy == null)
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

        // Applies a single retention rule
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

        // Gets current retention configuration
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

    public class RetentionPolicyConfig
    {
        public string TenantId { get; set; }
        public List<RetentionRule> Rules { get; set; } = new List<RetentionRule>();
        public bool AutoExecute { get; set; }
    }

    public class RetentionRule
    {
        public string TableName { get; set; }
        public string DateColumn { get; set; }
        public RetentionType RetentionType { get; set; }
        public int RetentionValue { get; set; }
        public bool IsEnabled { get; set; }
        public bool ArchiveBeforeDelete { get; set; }
        public string ArchiveTableName { get; set; }
    }

    public enum RetentionType
    {
        DaysOld,
        MonthsOld,
        YearsOld
    }

    public class RetentionResult
    {
        public bool IsSuccessful { get; set; }
        public int TotalRecordsDeleted { get; set; }
        public DateTime ExecutedAt { get; set; }
        public string Error { get; set; }
        public Dictionary<string, RuleExecutionResult> ProcessedRules { get; set; } =
            new Dictionary<string, RuleExecutionResult>();
    }

    public class RuleExecutionResult
    {
        public string TableName { get; set; }
        public int RecordsDeleted { get; set; }
        public string Status { get; set; }
        public DateTime? CutoffDate { get; set; }
    }
}
