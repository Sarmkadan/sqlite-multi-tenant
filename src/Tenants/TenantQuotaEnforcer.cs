using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Exceptions;

namespace SqliteMultiTenant.Tenants;

/// <summary>Snapshot of a tenant's quota usage.</summary>
public sealed record QuotaCheckResult
{
    public required string TenantId { get; init; }
    public required long CurrentSizeBytes { get; init; }
    /// <summary>Configured quota, or null when the tenant has no quota metadata (unlimited).</summary>
    public long? QuotaBytes { get; init; }
    /// <summary>0-100+; 0 when unlimited.</summary>
    public double UsagePercent { get; init; }
    public bool IsOverQuota { get; init; }
    /// <summary>True when usage is at or above the warning threshold but not over quota.</summary>
    public bool IsNearQuota { get; init; }
}

/// <summary>Enforces per-tenant storage quotas stored in tenant metadata under key "quota.maxBytes".</summary>
public sealed class TenantQuotaEnforcer
{
    /// <summary>Metadata key holding the quota in bytes as an invariant-culture long string.</summary>
    public const string QuotaMetadataKey = "quota.maxBytes";

    private readonly ITenantService _tenantService;
    /// <summary>Fraction (0-1) of quota at which IsNearQuota becomes true. Default 0.9.</summary>
    public double WarningThreshold { get; set; } = 0.9;

    public TenantQuotaEnforcer(ITenantService tenantService)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
    }

    /// <summary>Sets (or updates) the quota for a tenant via SetTenantMetadataAsync. maxBytes must be positive.</summary>
    public async Task SetQuotaAsync(string tenantId, long maxBytes, CancellationToken cancellationToken = default)
    {
        if (maxBytes <= 0)
        {
            throw new ArgumentException("Quota must be positive", nameof(maxBytes));
        }

        await _tenantService.SetTenantMetadataAsync(
            tenantId,
            QuotaMetadataKey,
            maxBytes.ToString(CultureInfo.InvariantCulture),
            cancellationToken);
    }

    /// <summary>Reads the tenant's quota from metadata; null when absent or unparsable.</summary>
    public async Task<long?> GetQuotaAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        var quotaStr = tenant.GetMetadata(QuotaMetadataKey);
        if (string.IsNullOrEmpty(quotaStr) || !long.TryParse(quotaStr, out var quota))
        {
            return null;
        }

        return quota;
    }

    /// <summary>Compares GetTenantDatabaseSizeAsync (use its TotalSizeBytes/size property) against the quota and returns a QuotaCheckResult. Throws TenantNotFoundException when the tenant does not exist.</summary>
    public async Task<QuotaCheckResult> CheckQuotaAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new TenantNotFoundException($"Tenant with ID '{tenantId}' was not found.", tenantId);
        }

        var sizeInfo = await _tenantService.GetTenantDatabaseSizeAsync(tenantId, cancellationToken);
        var quota = await GetQuotaAsync(tenantId, cancellationToken);

        double usagePercent = 0;
        bool isOverQuota = false;
        bool isNearQuota = false;

        if (quota.HasValue)
        {
            if (quota.Value <= 0)
            {
                throw new InvalidOperationException("Quota must be positive");
            }

            usagePercent = (double)sizeInfo.TotalSizeBytes / quota.Value * 100;
            isOverQuota = usagePercent >= 100;
            isNearQuota = usagePercent >= WarningThreshold * 100 && !isOverQuota;
        }

        return new QuotaCheckResult
        {
            TenantId = tenantId,
            CurrentSizeBytes = sizeInfo.TotalSizeBytes,
            QuotaBytes = quota,
            UsagePercent = Math.Round(usagePercent, 2),
            IsOverQuota = isOverQuota,
            IsNearQuota = isNearQuota
        };
    }

    /// <summary>Checks the quota and, when exceeded and autoSuspend is true, calls SuspendTenantAsync. Returns the check result.</summary>
    public async Task<QuotaCheckResult> EnforceAsync(string tenantId, bool autoSuspend = true, CancellationToken cancellationToken = default)
    {
        var result = await CheckQuotaAsync(tenantId, cancellationToken);

        if (autoSuspend && result.IsOverQuota)
        {
            await _tenantService.SuspendTenantAsync(tenantId, cancellationToken);
        }

        return result;
    }

    /// <summary>Scans all active tenants and returns results for tenants that are near or over quota, worst first.</summary>
    public async Task<List<QuotaCheckResult>> ScanAllAsync(CancellationToken cancellationToken = default)
    {
        var activeTenants = await _tenantService.GetActiveTenantsAsync(cancellationToken);
        var results = new List<QuotaCheckResult>();

        foreach (var tenant in activeTenants)
        {
            try
            {
                var result = await CheckQuotaAsync(tenant.TenantId, cancellationToken);
                if (result.IsNearQuota || result.IsOverQuota)
                {
                    results.Add(result);
                }
            }
            catch (TenantNotFoundException)
            {
                // Skip non-existent tenants
            }
        }

        // Sort by worst offenders first
        return results
            .OrderByDescending(r => r.UsagePercent)
            .ThenByDescending(r => r.IsOverQuota ? 1 : 0)
            .ToList();
    }
}
