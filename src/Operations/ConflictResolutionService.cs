// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Operations
{
    // Handles data conflict resolution in multi-tenant scenarios
    // Useful for merge operations, data synchronization, and concurrent updates
    public class ConflictResolutionService
    {
        private readonly ILogger<ConflictResolutionService> _logger;

        public ConflictResolutionService(ILogger<ConflictResolutionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Detects conflicts between two data versions
        public ConflictDetectionResult DetectConflicts(Dictionary<string, object> localVersion,
            Dictionary<string, object> remoteVersion)
        {
            var result = new ConflictDetectionResult();

            if (localVersion == null || remoteVersion == null)
            {
                return result;
            }

            foreach (var key in localVersion.Keys)
            {
                if (!remoteVersion.ContainsKey(key))
                {
                    result.AddConflict(new DataConflict
                    {
                        Field = key,
                        ConflictType = ConflictType.DeletedRemotely,
                        LocalValue = localVersion[key],
                        RemoteValue = null
                    });
                }
                else if (!Equals(localVersion[key], remoteVersion[key]))
                {
                    result.AddConflict(new DataConflict
                    {
                        Field = key,
                        ConflictType = ConflictType.ValueDifference,
                        LocalValue = localVersion[key],
                        RemoteValue = remoteVersion[key]
                    });
                }
            }

            foreach (var key in remoteVersion.Keys)
            {
                if (!localVersion.ContainsKey(key))
                {
                    result.AddConflict(new DataConflict
                    {
                        Field = key,
                        ConflictType = ConflictType.CreatedRemotely,
                        LocalValue = null,
                        RemoteValue = remoteVersion[key]
                    });
                }
            }

            return result;
        }

        // Resolves conflicts using a specified strategy
        public async Task<ConflictResolutionResult> ResolveConflictsAsync(
            ConflictDetectionResult conflicts, ConflictResolutionStrategy strategy)
        {
            var result = new ConflictResolutionResult();

            if (conflicts == null || conflicts.Conflicts.Count == 0)
            {
                return result;
            }

            try
            {
                foreach (var conflict in conflicts.Conflicts)
                {
                    var resolvedValue = strategy switch
                    {
                        ConflictResolutionStrategy.PreferLocal => conflict.LocalValue,
                        ConflictResolutionStrategy.PreferRemote => conflict.RemoteValue,
                        ConflictResolutionStrategy.KeepBoth =>
                            $"{conflict.LocalValue}|{conflict.RemoteValue}",
                        ConflictResolutionStrategy.DiscardBoth => null,
                        ConflictResolutionStrategy.Merge =>
                            MergeValues(conflict.LocalValue, conflict.RemoteValue),
                        _ => conflict.LocalValue
                    };

                    result.ResolvedValues[conflict.Field] = resolvedValue;
                }

                result.IsSuccessful = true;
                _logger.LogInformation("Resolved {Count} conflicts using {Strategy} strategy",
                    conflicts.Conflicts.Count, strategy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve conflicts");
                result.Error = ex.Message;
            }

            return result;
        }

        // Applies conflict resolutions to database
        public async Task<bool> ApplyResolutionAsync(SQLiteConnection connection,
            string tableName, string keyColumn, object keyValue,
            ConflictResolutionResult resolution)
        {
            if (connection == null || string.IsNullOrEmpty(tableName))
                return false;

            try
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var updates = new List<string>();
                        foreach (var kvp in resolution.ResolvedValues)
                        {
                            updates.Add($"[{kvp.Key}] = @val_{kvp.Key}");
                        }

                        if (updates.Count == 0)
                        {
                            return true;
                        }

                        var setClause = string.Join(", ", updates);

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText =
                                $"UPDATE [{tableName}] SET {setClause} WHERE [{keyColumn}] = @keyValue";

                            foreach (var kvp in resolution.ResolvedValues)
                            {
                                command.Parameters.AddWithValue($"@val_{kvp.Key}",
                                    kvp.Value ?? DBNull.Value);
                            }

                            command.Parameters.AddWithValue("@keyValue", keyValue);
                            await command.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                        _logger.LogInformation("Applied conflict resolution for {Table}:{Key}",
                            tableName, keyValue);

                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply conflict resolution");
                return false;
            }
        }

        private object MergeValues(object local, object remote)
        {
            if (local == null) return remote;
            if (remote == null) return local;

            // For numeric values, use the average
            if (local is int l && remote is int r)
            {
                return (l + r) / 2;
            }

            if (local is long ll && remote is long rl)
            {
                return (ll + rl) / 2;
            }

            // For strings, concatenate
            return $"{local}; {remote}";
        }
    }

    public class ConflictDetectionResult
    {
        public List<DataConflict> Conflicts { get; } = new List<DataConflict>();
        public bool HasConflicts => Conflicts.Count > 0;

        public void AddConflict(DataConflict conflict)
        {
            if (conflict != null)
            {
                Conflicts.Add(conflict);
            }
        }
    }

    public class DataConflict
    {
        public string Field { get; set; }
        public ConflictType ConflictType { get; set; }
        public object LocalValue { get; set; }
        public object RemoteValue { get; set; }
    }

    public enum ConflictType
    {
        ValueDifference,
        CreatedRemotely,
        DeletedRemotely,
        ModifiedBoth
    }

    public enum ConflictResolutionStrategy
    {
        PreferLocal,
        PreferRemote,
        KeepBoth,
        DiscardBoth,
        Merge
    }

    public class ConflictResolutionResult
    {
        public Dictionary<string, object> ResolvedValues { get; } = new Dictionary<string, object>();
        public bool IsSuccessful { get; set; }
        public string Error { get; set; }
    }
}
