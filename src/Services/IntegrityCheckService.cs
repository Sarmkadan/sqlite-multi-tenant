#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Data.SQLite;
using System.Text;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service implementation for performing SQLite database integrity checks on tenant databases.
/// Executes PRAGMA integrity_check per tenant and returns ok/failed status with messages.
/// Supports batch operations with configurable parallelism limits.
/// </summary>
public sealed class IntegrityCheckService : IIntegrityCheckService
{
    private const int DefaultMaxDegreeOfParallelism = 4;
    private const int DefaultTimeoutSeconds = 60;
    private const string IntegrityCheckPragma = "PRAGMA integrity_check;";

    private readonly ITenantService _tenantService;
    private readonly ILogger<IntegrityCheckService> _logger;
    private readonly TimeSpan _defaultTimeout;

    public IntegrityCheckService(
        ITenantService tenantService,
        ILogger<IntegrityCheckService> logger)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultTimeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds); // 1 minute per database for integrity check
    }

    /// <summary>
    /// Performs integrity check on a specific tenant database.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Integrity check result with ok/failed status and messages.</returns>
    public async Task<TenantIntegrityCheckResult> CheckTenantIntegrityAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken);
        if (tenant is null)
            throw new TenantNotFoundException(tenantId);

        if (string.IsNullOrWhiteSpace(tenant.DatabasePath) || !File.Exists(tenant.DatabasePath))
            throw new InvalidOperationException($"Tenant {tenantId} database file not found at {tenant.DatabasePath}");

        return await CheckDatabaseIntegrityAsync(tenant, cancellationToken);
    }

    /// <summary>
    /// Performs integrity check on multiple tenant databases with configurable parallelism.
    /// </summary>
    /// <param name="tenantIds">List of tenant identifiers to check.</param>
    /// <param name="maxDegreeOfParallelism">Maximum parallel operations (0 or 1 for sequential).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of integrity check results for all specified tenants.</returns>
    public async Task<List<TenantIntegrityCheckResult>> CheckTenantsIntegrityAsync(
        IEnumerable<string> tenantIds,
        int maxDegreeOfParallelism = DefaultMaxDegreeOfParallelism,
        CancellationToken cancellationToken = default)
    {
        if (tenantIds is null)
            throw new ArgumentNullException(nameof(tenantIds));

        var tenantList = tenantIds.ToList();
        _logger.LogInformation("Starting integrity checks for {Count} tenants with parallelism {Parallelism}",
            tenantList.Count, maxDegreeOfParallelism);

        var results = await CheckManyAsync(tenantList, maxDegreeOfParallelism, cancellationToken);
        _logger.LogInformation("Completed integrity checks for {Count} tenants", results.Count);
        return results;
    }

    /// <summary>
    /// Performs integrity check on all tenant databases with configurable parallelism.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Maximum parallel operations (0 or 1 for sequential).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of integrity check results for all tenants.</returns>
    public async Task<List<TenantIntegrityCheckResult>> CheckAllTenantsIntegrityAsync(
        int maxDegreeOfParallelism = DefaultMaxDegreeOfParallelism,
        CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantService.GetAllTenantsAsync(cancellationToken);
        _logger.LogInformation("Starting integrity checks for all {Count} tenants with parallelism {Parallelism}",
            tenants.Count, maxDegreeOfParallelism);

        var tenantIds = tenants.Select(t => t.TenantId).ToList();
        var results = await CheckManyAsync(tenantIds, maxDegreeOfParallelism, cancellationToken);
        _logger.LogInformation("Completed integrity checks for {Count} tenants", results.Count);
        return results;
    }

    /// <summary>
    /// Performs integrity check on all active tenant databases with configurable parallelism.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Maximum parallel operations (0 or 1 for sequential).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of integrity check results for active tenants only.</returns>
    public async Task<List<TenantIntegrityCheckResult>> CheckActiveTenantsIntegrityAsync(
        int maxDegreeOfParallelism = DefaultMaxDegreeOfParallelism,
        CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantService.GetActiveTenantsAsync(cancellationToken);
        _logger.LogInformation("Starting integrity checks for {Count} active tenants with parallelism {Parallelism}",
            tenants.Count, maxDegreeOfParallelism);

        var tenantIds = tenants.Select(t => t.TenantId).ToList();
        var results = await CheckManyAsync(tenantIds, maxDegreeOfParallelism, cancellationToken);
        _logger.LogInformation("Completed integrity checks for {Count} tenants", results.Count);
        return results;
    }

    /// <summary>
    /// Internal method to check integrity of a single tenant database.
    /// </summary>
    private async Task<TenantIntegrityCheckResult> CheckDatabaseIntegrityAsync(
        Tenant tenant,
        CancellationToken cancellationToken)
    {
        var result = new TenantIntegrityCheckResult
        {
            TenantId = tenant.TenantId,
            TenantName = tenant.Name,
            CheckedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting integrity check on tenant {TenantId} ({TenantName})",
                tenant.TenantId, tenant.Name);

            if (string.IsNullOrWhiteSpace(tenant.DatabasePath) || !File.Exists(tenant.DatabasePath))
            {
                var errorMsg = $"Tenant {tenant.TenantId} database file not found at {tenant.DatabasePath}";
                _logger.LogError(errorMsg);
                result.IsOk = false;
                result.Error = errorMsg;
                return result;
            }

            // Execute PRAGMA integrity_check
            var integrityOutput = await ExecutePragmaIntegrityCheckAsync(tenant, cancellationToken);
            result.IntegrityOutput = integrityOutput;

            // Parse the output to determine if it's OK or contains errors
            result.IsOk = ParseIntegrityOutput(integrityOutput, out var errorMessage);

            if (result.IsOk)
            {
                _logger.LogInformation("Integrity check passed for tenant {TenantId} ({TenantName})",
                    tenant.TenantId, tenant.Name);
            }
            else
            {
                _logger.LogWarning("Integrity check failed for tenant {TenantId} ({TenantName}): {Error}",
                    tenant.TenantId, tenant.Name, errorMessage);
                result.Error = errorMessage;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during integrity check on tenant {TenantId}", tenant.TenantId);
            result.IsOk = false;
            result.Error = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Internal method to execute PRAGMA integrity_check on a tenant database.
    /// </summary>
    private async Task<string> ExecutePragmaIntegrityCheckAsync(
        Tenant tenant,
        CancellationToken cancellationToken)
    {
        if (tenant is null)
            throw new ArgumentNullException(nameof(tenant));

        if (string.IsNullOrWhiteSpace(tenant.DatabasePath) || !File.Exists(tenant.DatabasePath))
            throw new InvalidOperationException($"Tenant {tenant.TenantId} database file not found");

        await using var connection = new SQLiteConnection($"Data Source={tenant.DatabasePath};");
        await connection.OpenAsync(cancellationToken);

        using var command = new SQLiteCommand(IntegrityCheckPragma, connection);
        command.CommandTimeout = (int)_defaultTimeout.TotalSeconds;

        // Execute the command and read the result
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new StringBuilder();
        while (await reader.ReadAsync(cancellationToken))

        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (!reader.IsDBNull(i))
                {
                    var value = reader.GetString(i);
                    result.AppendLine(value);
                }
            }
        }

        return result.ToString().Trim();
    }

    /// <summary>
    /// Parses the integrity check output to determine if it's OK or contains errors.
    /// SQLite returns "ok" (case-insensitive) for success, or error messages otherwise.
    /// </summary>
    private bool ParseIntegrityOutput(string output, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(output))
        {
            errorMessage = "No output from integrity check";
            return false;
        }

        // Normalize the output for comparison
        var normalizedOutput = output.Trim().ToLowerInvariant();

        // SQLite returns "ok" when database is OK
        if (normalizedOutput == "ok" || normalizedOutput == "ok\nok")
        {
            return true;
        }

        // If output contains "ok" but also has other text, it's likely OK with warnings
        // Only return false if we see actual error indicators
        if (normalizedOutput.Contains("ok"))
        {
            // Check if there are actual error messages
            var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var lowerLine = line.Trim().ToLowerInvariant();
                if (lowerLine.Contains("error") ||
                    lowerLine.Contains("corrupt") ||
                    lowerLine.Contains("malformed") ||
                    lowerLine.Contains("database disk image is malformed"))
                {
                    errorMessage = line.Trim();
                    return false;
                }
            }

            // If we get here, it's OK with some warnings
            return true;
        }

        // Any other output indicates a problem
        errorMessage = output.Trim();
        return false;
    }

    /// <summary>
    /// Executes integrity checks on a batch of tenants with configurable parallelism.
    /// </summary>
    private async Task<List<TenantIntegrityCheckResult>> CheckManyAsync(
        IReadOnlyCollection<string> tenantIds,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        // Validate and adjust maxDegreeOfParallelism to be at least 1
        if (maxDegreeOfParallelism < 1)
        {
            maxDegreeOfParallelism = 1;
        }

        var results = new List<TenantIntegrityCheckResult>(tenantIds.Count);

        if (maxDegreeOfParallelism <= 1)
        {
            // Sequential processing
            foreach (var tenantId in tenantIds)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var result = await CheckTenantIntegrityAsync(tenantId, cancellationToken);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check integrity for tenant {TenantId}", tenantId);
                    results.Add(new TenantIntegrityCheckResult
                    {
                        TenantId = tenantId,
                        TenantName = "Unknown",
                        IsOk = false,
                        Error = ex.Message,
                        CheckedAt = DateTime.UtcNow
                    });
                }
            }
        }
        else
        {
            // Parallel processing using Task.WhenAll
            var options = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            var tenantTasks = tenantIds.Select(async tenantId =>
            {
                try
                {
                    return await CheckTenantIntegrityAsync(tenantId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check integrity for tenant {TenantId}", tenantId);
                    return new TenantIntegrityCheckResult
                    {
                        TenantId = tenantId,
                        TenantName = "Unknown",
                        IsOk = false,
                        Error = ex.Message,
                        CheckedAt = DateTime.UtcNow
                    };
                }
            }).ToList();

            await Task.WhenAll(tenantTasks);
            results.AddRange(tenantTasks.Select(t => t.Result));
        }

        return results;
    }
}
