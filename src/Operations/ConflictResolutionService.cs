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

namespace SqliteMultiTenant.Operations
{
    /// <summary>
    /// Handles data conflict resolution in multi-tenant scenarios
    /// Useful for merge operations, data synchronization, and concurrent updates
    /// </summary>
    public sealed class ConflictResolutionService {
        private readonly ILogger<ConflictResolutionService> _logger;

        /// <summary>
        /// Initializes a new instance of the ConflictResolutionService class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public ConflictResolutionService(ILogger<ConflictResolutionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Returns a concise, informative representation of the service
        public override string ToString()
        {
            return $"ConflictResolutionService {{ Field = {nameof(DataConflict.Field)}, ConflictType = {nameof(DataConflict.ConflictType)}, LocalValue = {nameof(DataConflict.LocalValue)}, RemoteValue = {nameof(DataConflict.RemoteValue)}, IsSuccessful = {nameof(ConflictResolutionResult.IsSuccessful)}, Error = {nameof(ConflictResolutionResult.Error)} }}";
        }

        /// <summary>
        /// Detects conflicts between two data versions
        /// </summary>
        /// <param name="localVersion">The local version of the data</param>
        /// <param name="remoteVersion">The remote version of the data</param>
        /// <returns>A ConflictDetectionResult containing any detected conflicts</returns>
        public ConflictDetectionResult DetectConflicts(Dictionary<string, object> localVersion,
            Dictionary<string, object> remoteVersion)
        {
            var result = new ConflictDetectionResult();

            if (localVersion is null || remoteVersion is null)
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

        /// <summary>
        /// Resolves conflicts using a specified strategy
        /// </summary>
        /// <param name="conflicts">The conflicts to resolve</param>
        /// <param name="strategy">The resolution strategy to apply</param>
        /// <returns>A ConflictResolutionResult containing the resolved values</returns>
        public async Task<ConflictResolutionResult> ResolveConflictsAsync(
            ConflictDetectionResult conflicts, ConflictResolutionStrategy strategy)
        {
            var result = new ConflictResolutionResult();

            if (conflicts is null || conflicts.Conflicts.Count == 0)
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

        /// <summary>
        /// Applies conflict resolutions to database
        /// </summary>
        /// <param name="connection">The SQLite connection</param>
        /// <param name="tableName">The name of the table to update</param>
        /// <param name="keyColumn">The name of the key column</param>
        /// <param name="keyValue">The value of the key</param>
        /// <param name="resolution">The conflict resolution result</param>
        /// <returns>True if the resolution was applied successfully; otherwise, false</returns>
        public async Task<bool> ApplyResolutionAsync(SQLiteConnection connection,
            string tableName, string keyColumn, object keyValue,
            ConflictResolutionResult resolution)
        {
            if (connection is null || string.IsNullOrEmpty(tableName))
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
            if (local is null) return remote;
            if (remote is null) return local;

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

    /// <summary>
    /// Represents the result of conflict detection
    /// </summary>
    public sealed class ConflictDetectionResult {
        /// <summary>
        /// Gets the list of detected conflicts
        /// </summary>
        public List<DataConflict> Conflicts { get; } = new List<DataConflict>();

        /// <summary>
        /// Gets a value indicating whether any conflicts were detected
        /// </summary>
        public bool HasConflicts => Conflicts.Count > 0;

        /// <summary>
        /// Adds a conflict to the detection result
        /// </summary>
        /// <param name="conflict">The conflict to add</param>
        public void AddConflict(DataConflict conflict)
        {
            if (conflict is not null)
            {
                Conflicts.Add(conflict);
            }
        }
    }

    /// <summary>
    /// Represents a data conflict between local and remote values
    /// </summary>
    public sealed class DataConflict {
        /// <summary>
        /// Gets or sets the name of the field in conflict
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the type of conflict
        /// </summary>
        public ConflictType ConflictType { get; set; }

        /// <summary>
        /// Gets or sets the local value
        /// </summary>
        public object LocalValue { get; set; }

        /// <summary>
        /// Gets or sets the remote value
        /// </summary>
        public object RemoteValue { get; set; }
    }

    /// <summary>
    /// Specifies the type of conflict detected
    /// </summary>
    public enum ConflictType
    {
        /// <summary>
        /// Indicates that the local and remote values are different
        /// </summary>
        ValueDifference,

        /// <summary>
        /// Indicates that the field was created in the remote version
        /// </summary>
        CreatedRemotely,

        /// <summary>
        /// Indicates that the field was deleted in the remote version
        /// </summary>
        DeletedRemotely,

        /// <summary>
        /// Indicates that the field was modified in both versions
        /// </summary>
        ModifiedBoth
    }

    /// <summary>
    /// Specifies the strategy to use for resolving conflicts
    /// </summary>
    public enum ConflictResolutionStrategy
    {
        /// <summary>
        /// Prefer the local value when resolving conflicts
        /// </summary>
        PreferLocal,

        /// <summary>
        /// Prefer the remote value when resolving conflicts
        /// </summary>
        PreferRemote,

        /// <summary>
        /// Keep both values by concatenating them with a separator
        /// </summary>
        KeepBoth,

        /// <summary>
        /// Discard both values, setting the field to null
        /// </summary>
        DiscardBoth,

        /// <summary>
        /// Merge the values using a default merge strategy (average for numbers, concatenation for strings)
        /// </summary>
        Merge
    }

    /// <summary>
    /// Represents the result of conflict resolution
    /// </summary>
    public sealed class ConflictResolutionResult {
        /// <summary>
        /// Gets the dictionary of resolved field values
        /// </summary>
        public Dictionary<string, object> ResolvedValues { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets a value indicating whether the resolution was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the error message if the resolution failed
        /// </summary>
        public string Error { get; set; }
    }
}