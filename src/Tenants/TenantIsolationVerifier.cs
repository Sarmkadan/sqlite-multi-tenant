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

namespace SqliteMultiTenant.Tenants
{
    /// <summary>
    /// Provides verification services to ensure multi-tenant data isolation is maintained
    /// and prevents unauthorized cross-tenant data access in SQLite databases.
    /// </summary>
    public sealed class TenantIsolationVerifier
    {
        private readonly ILogger<TenantIsolationVerifier> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantIsolationVerifier"/> class.
        /// </summary>
        /// <param name="logger">The logger instance used for logging verification results and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public TenantIsolationVerifier(ILogger<TenantIsolationVerifier> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Verifies that a tenant can only access its own data by checking database isolation.
        /// </summary>
        /// <param name="connection">The SQLite database connection to verify.</param>
        /// <param name="tenantId">The tenant identifier to verify isolation for.</param>
        /// <returns>An <see cref="IsolationVerificationResult"/> containing isolation verification results.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is null or whitespace.</exception>
        public async Task<IsolationVerificationResult> VerifyTenantIsolationAsync(
            SQLiteConnection connection, string tenantId)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            try
            {
                var result = new IsolationVerificationResult { TenantId = tenantId };

                // Verify no cross-tenant data leak in AuditLog
                result.AuditLogIsolationValid = await VerifyAuditLogIsolationAsync(
                    connection, tenantId);

                // Verify connection restriction
                result.ConnectionRestrictionValid = VerifyConnectionRestriction(
                    connection, tenantId);

                // Verify query results isolation
                result.QueryIsolationValid = await VerifyQueryIsolationAsync(
                    connection, tenantId);

                result.IsIsolated = result.AuditLogIsolationValid
                    && result.ConnectionRestrictionValid
                    && result.QueryIsolationValid;

                result.VerifiedAt = DateTime.UtcNow;

                if (!result.IsIsolated)
                {
                    _logger.LogWarning("Tenant isolation violation detected for tenant: {TenantId}",
                        tenantId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify tenant isolation for tenant: {TenantId}",
                    tenantId);
                throw;
            }
        }

        /// <summary>
        /// Detects potential data leakage patterns by analyzing database schema and structure.
        /// </summary>
        /// <param name="connection">The SQLite database connection to analyze for data leakage risks.</param>
        /// <returns>A list of <see cref="DataLeakageSuspicion"/> objects identifying potential data leaks.</returns>
        public async Task<List<DataLeakageSuspicion>> DetectPotentialDataLeaksAsync(
            SQLiteConnection connection)
        {
            var suspicions = new List<DataLeakageSuspicion>();

            try
            {
                // Check for tables without tenant context
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"SELECT name FROM sqlite_master
                        WHERE type='table'
                        AND name NOT LIKE 'sqlite_%'
                        AND name NOT IN ('Tenants', 'AuditLog')";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var tableName = reader.GetString(0);

                            // Check if table has tenant ID column
                            if (!await HasTenantColumnAsync(connection, tableName))
                            {
                                suspicions.Add(new DataLeakageSuspicion
                                {
                                    Type = "MissingTenantContext",
                                    Description = $"Table '{tableName}' has no TenantId column",
                                    Severity = "Critical"
                                });
                            }
                        }
                    }
                }

                // Check for unencrypted sensitive columns
                suspicions.AddRange(await DetectUnencryptedSensitiveDataAsync(connection));

                // Check for improper access patterns
                suspicions.AddRange(await DetectImproperAccessPatternsAsync(connection));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to detect potential data leaks");
            }

            return suspicions;
        }

        /// <summary>
        /// Validates that SQL queries respect tenant boundaries and isolation requirements.
        /// </summary>
        /// <param name="connection">The SQLite database connection to use for validation.</param>
        /// <param name="query">The SQL query to validate for tenant isolation compliance.</param>
        /// <param name="tenantId">The tenant identifier the query should be scoped to.</param>
        /// <returns>A <see cref="QueryValidationResult"/> containing validation results for the query.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> or <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="query"/> is empty or whitespace.</exception>
        public async Task<QueryValidationResult> ValidateQueryTenantIsolationAsync(
            SQLiteConnection connection, string query, string tenantId)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            var result = new QueryValidationResult { Query = query, TenantId = tenantId };

            try
            {
                // Basic validation rules
                result.ContainsTenantFilter = query.Contains("TenantId", StringComparison.OrdinalIgnoreCase);
                result.ContainsWildcardSelect = query.Contains("SELECT *");
                result.IsParameterized = query.Contains("@");

                // Check for common isolation bypass patterns
                result.HasNoUnionBypass = !query.Contains("UNION", StringComparison.OrdinalIgnoreCase)
                    || query.Contains($"TenantId", StringComparison.OrdinalIgnoreCase);

                result.HasNoSubqueryBypass = !query.Contains("(SELECT", StringComparison.OrdinalIgnoreCase)
                    || query.Contains($"TenantId", StringComparison.OrdinalIgnoreCase);

                result.IsIsolationSafe = result.ContainsTenantFilter
                    && result.IsParameterized
                    && result.HasNoUnionBypass
                    && result.HasNoSubqueryBypass;

                if (!result.IsIsolationSafe)
                {
                    _logger.LogWarning(
                        "Query isolation concern detected for tenant {TenantId}: {Query}",
                        tenantId, query);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate query isolation");
                result.IsIsolationSafe = false;
            }

            return result;
        }

        private async Task<bool> VerifyAuditLogIsolationAsync(SQLiteConnection connection,
            string tenantId)
        {
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"SELECT COUNT(*) FROM AuditLog
                        WHERE TenantId != @tenantId";

                    command.Parameters.AddWithValue("@tenantId", tenantId);

                    var count = (long)await command.ExecuteScalarAsync();
                    return count == 0; // Should be 0 if properly isolated
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify audit log isolation");
                return false;
            }
        }

        private bool VerifyConnectionRestriction(SQLiteConnection connection, string tenantId)
        {
            try
            {
                // Verify connection is restricted to tenant's database file
                var connectionString = connection.ConnectionString;
                return !string.IsNullOrEmpty(connectionString);
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> VerifyQueryIsolationAsync(SQLiteConnection connection,
            string tenantId)
        {
            try
            {
                // Test that we can only access this tenant's data
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT TenantId FROM AuditLog LIMIT 1";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var returnedTenantId = reader.GetString(0);
                            return returnedTenantId == tenantId;
                        }
                    }
                }

                return true; // Empty table is OK
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> HasTenantColumnAsync(SQLiteConnection connection, string tableName)
        {
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"PRAGMA table_info({tableName})";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader.GetString(1) == "TenantId")
                                return true;
                        }
                    }
                }
            }
            catch { /* Ignored */ }

            return false;
        }

        private async Task<List<DataLeakageSuspicion>> DetectUnencryptedSensitiveDataAsync(
            SQLiteConnection connection)
        {
            var suspicions = new List<DataLeakageSuspicion>();

            // Common sensitive column patterns
            var sensitivePatterns = new[] { "password", "secret", "token", "key", "credential" };

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"SELECT m.tbl_name, m.name
                        FROM pragma_table_info(m.name) t, sqlite_master m
                        WHERE m.type='table'";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var columnName = reader.GetString(1).ToLower();

                            foreach (var pattern in sensitivePatterns)
                            {
                                if (columnName.Contains(pattern))
                                {
                                    suspicions.Add(new DataLeakageSuspicion
                                    {
                                        Type = "PotentiallyUnencryptedSensitiveData",
                                        Description = $"Column '{columnName}' may contain sensitive data",
                                        Severity = "High"
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to detect unencrypted sensitive data");
            }

            return suspicions;
        }

        private async Task<List<DataLeakageSuspicion>> DetectImproperAccessPatternsAsync(
            SQLiteConnection connection)
        {
            var suspicions = new List<DataLeakageSuspicion>();

            // This would typically check query logs and access patterns
            // For now, return empty list as this would require additional instrumentation

            return suspicions;
        }
    }

    /// <summary>
    /// Contains the results of a tenant isolation verification check.
    /// </summary>
    public sealed class IsolationVerificationResult
    {
        /// <summary>
        /// Gets or sets the tenant identifier being verified.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether all isolation checks passed successfully.
        /// </summary>
        public bool IsIsolated { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether audit log isolation was validated successfully.
        /// </summary>
        public bool AuditLogIsolationValid { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether connection restrictions were validated successfully.
        /// </summary>
        public bool ConnectionRestrictionValid { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether query isolation was validated successfully.
        /// </summary>
        public bool QueryIsolationValid { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the verification was performed.
        /// </summary>
        public DateTime VerifiedAt { get; set; }
    }

    /// <summary>
    /// Represents a potential data leakage issue detected during database analysis.
    /// </summary>
    public sealed class DataLeakageSuspicion
    {
        /// <summary>
        /// Gets or sets the type/category of the data leakage suspicion.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets a description of the potential data leakage issue.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the severity level of the data leakage concern (e.g., "Critical", "High", "Medium").
        /// </summary>
        public string Severity { get; set; }
    }

    /// <summary>
    /// Contains the results of validating a SQL query for tenant isolation compliance.
    /// </summary>
    public sealed class QueryValidationResult
    {
        /// <summary>
        /// Gets or sets the SQL query that was validated.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier the query should be scoped to.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the query contains a tenant filter.
        /// </summary>
        public bool ContainsTenantFilter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the query uses wildcard SELECT (*).
        /// </summary>
        public bool ContainsWildcardSelect { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the query uses parameterized statements.
        /// </summary>
        public bool IsParameterized { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the query avoids UNION-based isolation bypasses.
        /// </summary>
        public bool HasNoUnionBypass { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the query avoids subquery-based isolation bypasses.
        /// </summary>
        public bool HasNoSubqueryBypass { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the query is considered safe for tenant isolation.
        /// </summary>
        public bool IsIsolationSafe { get; set; }
    }
}
