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
    /// Extension methods for <see cref="ConflictResolutionService"/> providing additional conflict resolution utilities
    /// </summary>
    public static class ConflictResolutionServiceExtensions
    {
        /// <summary>
        /// Creates a conflict detection result with a single conflict pre-populated
        /// </summary>
        /// <param name="service">The conflict resolution service</param>
        /// <param name="field">The field name with conflict</param>
        /// <param name="conflictType">Type of conflict</param>
        /// <param name="localValue">Local value</param>
        /// <param name="remoteValue">Remote value</param>
        /// <returns>ConflictDetectionResult with the specified conflict</returns>
        /// <exception cref="ArgumentException"><paramref name="field"/> is null or empty</exception>
        public static ConflictDetectionResult CreateConflictDetectionResult(
            this ConflictResolutionService service,
            string field,
            ConflictType conflictType,
            object localValue,
            object remoteValue)
        {
            ArgumentException.ThrowIfNullOrEmpty(field);

            var result = new ConflictDetectionResult();
            result.AddConflict(new DataConflict
            {
                Field = field,
                ConflictType = conflictType,
                LocalValue = localValue,
                RemoteValue = remoteValue
            });

            return result;
        }

        /// <summary>
        /// Detects conflicts between two data versions and returns only the conflicting fields with their resolution strategies
        /// </summary>
        /// <param name="service">The conflict resolution service</param>
        /// <param name="localVersion">Local data version</param>
        /// <param name="remoteVersion">Remote data version</param>
        /// <returns>Dictionary containing only the conflicting field names and their resolution strategy</returns>
        /// <exception cref="ArgumentNullException"><paramref name="localVersion"/> or <paramref name="remoteVersion"/> is null</exception>
        public static Dictionary<string, ConflictResolutionStrategy> GetConflictingFields(
            this ConflictResolutionService service,
            Dictionary<string, object> localVersion,
            Dictionary<string, object> remoteVersion)
        {
            ArgumentNullException.ThrowIfNull(localVersion);
            ArgumentNullException.ThrowIfNull(remoteVersion);

            var conflicts = service.DetectConflicts(localVersion, remoteVersion);
            var result = new Dictionary<string, ConflictResolutionStrategy>();

            foreach (var conflict in conflicts.Conflicts)
            {
                result[conflict.Field] = conflict.ConflictType switch
                {
                    ConflictType.ValueDifference => ConflictResolutionStrategy.PreferLocal,
                    ConflictType.CreatedRemotely => ConflictResolutionStrategy.PreferRemote,
                    ConflictType.DeletedRemotely => ConflictResolutionStrategy.PreferLocal,
                    ConflictType.ModifiedBoth => ConflictResolutionStrategy.Merge,
                    _ => ConflictResolutionStrategy.PreferLocal
                };
            }

            return result;
        }

        /// <summary>
        /// Resolves conflicts using a custom resolution function for each field
        /// </summary>
        /// <param name="service">The conflict resolution service</param>
        /// <param name="conflicts">Detected conflicts</param>
        /// <param name="customResolver">Function that resolves each conflict based on field name and conflict type</param>
        /// <returns>ConflictResolutionResult with resolved values</returns>
        /// <exception cref="ArgumentNullException"><paramref name="customResolver"/> is null</exception>
        public static async Task<ConflictResolutionResult> ResolveConflictsAsync(
            this ConflictResolutionService service,
            ConflictDetectionResult conflicts,
            Func<string, ConflictType, object, object, object> customResolver)
        {
            ArgumentNullException.ThrowIfNull(customResolver);

            var result = new ConflictResolutionResult();

            if (conflicts?.Conflicts.Count > 0)
            {
                try
                {
                    foreach (var conflict in conflicts.Conflicts)
                    {
                        var resolvedValue = customResolver(
                            conflict.Field,
                            conflict.ConflictType,
                            conflict.LocalValue,
                            conflict.RemoteValue);

                        result.ResolvedValues[conflict.Field] = resolvedValue;
                    }

                    result.IsSuccessful = true;
                }
                catch (Exception ex)
                {
                    result.Error = ex.Message;
                }
            }

            return result;
        }

        /// <summary>
        /// Applies conflict resolution to database with retry logic for transient failures
        /// </summary>
        /// <param name="service">The conflict resolution service</param>
        /// <param name="connection">Database connection</param>
        /// <param name="tableName">Table name</param>
        /// <param name="keyColumn">Primary key column name</param>
        /// <param name="keyValue">Primary key value</param>
        /// <param name="resolution">Conflict resolution result</param>
        /// <param name="maxRetries">Maximum retry attempts</param>
        /// <returns>True if successful, false otherwise</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="tableName"/> or <paramref name="keyColumn"/> is null or empty</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRetries"/> is negative</exception>
        public static async Task<bool> ApplyResolutionWithRetryAsync(
            this ConflictResolutionService service,
            SQLiteConnection connection,
            string tableName,
            string keyColumn,
            object keyValue,
            ConflictResolutionResult resolution,
            int maxRetries = 3)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentException.ThrowIfNullOrEmpty(tableName);
            ArgumentException.ThrowIfNullOrEmpty(keyColumn);
            ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

            int attempt = 0;

            while (attempt <= maxRetries)
            {
                try
                {
                    return await service.ApplyResolutionAsync(
                        connection,
                        tableName,
                        keyColumn,
                        keyValue,
                        resolution);
                }
                catch (SQLiteException) when (attempt < maxRetries)
                {
                    attempt++;

                    // Exponential backoff
                    var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 50);
                    await Task.Delay(delay);
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a conflict detection result from a collection of conflicts
        /// </summary>
        /// <param name="service">The conflict resolution service</param>
        /// <param name="conflicts">Collection of conflicts</param>
        /// <returns>ConflictDetectionResult populated with conflicts</returns>
        public static ConflictDetectionResult CreateConflictDetectionResult(
            this ConflictResolutionService service,
            IEnumerable<DataConflict> conflicts)
        {
            var result = new ConflictDetectionResult();

            if (conflicts is not null)
            {
                foreach (var conflict in conflicts)
                {
                    result.AddConflict(conflict);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if any conflicts have a specific type
        /// </summary>
        /// <param name="service">The conflict resolution service</param>
        /// <param name="detectionResult">Conflict detection result</param>
        /// <param name="conflictType">Conflict type to check for</param>
        /// <returns>True if any conflicts match the specified type</returns>
        /// <exception cref="ArgumentNullException"><paramref name="detectionResult"/> is null</exception>
        public static bool HasConflictType(
            this ConflictResolutionService service,
            ConflictDetectionResult detectionResult,
            ConflictType conflictType)
        {
            ArgumentNullException.ThrowIfNull(detectionResult);

            return detectionResult.Conflicts.Exists(c => c.ConflictType == conflictType);
        }

        /// <summary>
        /// Gets the first conflict of a specific type
        /// </summary>
        /// <param name="service">The conflict resolution service</param>
        /// <param name="detectionResult">Conflict detection result</param>
        /// <param name="conflictType">Conflict type to find</param>
        /// <returns>First conflict of the specified type, or null if not found</returns>
        /// <exception cref="ArgumentNullException"><paramref name="detectionResult"/> is null</exception>
        public static DataConflict? GetFirstConflictOfType(
            this ConflictResolutionService service,
            ConflictDetectionResult detectionResult,
            ConflictType conflictType)
        {
            ArgumentNullException.ThrowIfNull(detectionResult);

            return detectionResult.Conflicts.Find(c => c.ConflictType == conflictType);
        }
    }
}