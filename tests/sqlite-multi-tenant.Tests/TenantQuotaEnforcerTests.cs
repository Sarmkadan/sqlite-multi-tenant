using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Tenants;
using Xunit;

namespace SqliteMultiTenant.Tests.Tenants;

/// <summary>
/// Contains unit tests for the TenantQuotaEnforcer class.
/// Tests cover quota checking, enforcement with auto-suspend, quota setting and retrieval, and scanning all tenants for quota usage.
/// Uses a hand-rolled fake ITenantService implementation.
/// </summary>
public class TenantQuotaEnforcerTests
{
    private readonly FakeTenantService _tenantService;
    private readonly TenantQuotaEnforcer _enforcer;
    private const string TestTenantId = "test-tenant";

    public TenantQuotaEnforcerTests()
    {
        _tenantService = new FakeTenantService();
        _enforcer = new TenantQuotaEnforcer(_tenantService);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when tenantService is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTenantService_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new TenantQuotaEnforcer(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tenantService");
    }

    /// <summary>
    /// Tests that SetQuotaAsync throws an ArgumentException when given a non-positive byte value.
    /// </summary>
    [Fact]
    public async Task SetQuotaAsync_NonPositiveMaxBytes_ThrowsArgumentException()
    {
        // Arrange
        const long maxBytes = 0; // Invalid quota

        // Act
        Func<Task> act = async () => await _enforcer.SetQuotaAsync(TestTenantId, maxBytes);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Quota must be positive (Parameter 'maxBytes')");
    }

    /// <summary>
    /// Tests that CheckQuotaAsync returns a result indicating the tenant is under quota when its current size is below the configured quota.
    /// </summary>
    [Fact]
    public async Task CheckQuotaAsync_UnderQuotaAllowed_ReturnsCorrectResult()
    {
        // Arrange
        var tenant = new Tenant { TenantId = TestTenantId };
        tenant.SetMetadata(TenantQuotaEnforcer.QuotaMetadataKey, "1000"); // 1KB quota

        var storageInfo = new TenantStorageInfo
        {
            TenantId = TestTenantId,
            SizeBytes = 400, // 400 bytes
            PageCount = 10,
            PageSize = 40,
            WalSizeBytes = 0
        };

        _tenantService.SetupGetTenant(TestTenantId, tenant);
        _tenantService.SetupGetTenantDatabaseSize(TestTenantId, storageInfo);

        // Act
        var result = await _enforcer.CheckQuotaAsync(TestTenantId);

        // Assert
        result.TenantId.Should().Be(TestTenantId);
        result.CurrentSizeBytes.Should().Be(400);
        result.QuotaBytes.Should().Be(1000);
        result.UsagePercent.Should().Be(40.0); // 400/1000 * 100
        result.IsOverQuota.Should().BeFalse();
        result.IsNearQuota.Should().BeFalse(); // 40% < 90% warning threshold
    }

    /// <summary>
    /// Tests that CheckQuotaAsync returns a result indicating the tenant is over quota when its current size exceeds the configured quota.
    /// </summary>
    [Fact]
    public async Task CheckQuotaAsync_OverQuotaRejected_ReturnsOverQuota()
    {
        // Arrange
        var tenant = new Tenant { TenantId = TestTenantId };
        tenant.SetMetadata(TenantQuotaEnforcer.QuotaMetadataKey, "1000"); // 1KB quota

        var storageInfo = new TenantStorageInfo
        {
            TenantId = TestTenantId,
            SizeBytes = 1200, // Over quota
            PageCount = 30,
            PageSize = 40,
            WalSizeBytes = 0
        };

        _tenantService.SetupGetTenant(TestTenantId, tenant);
        _tenantService.SetupGetTenantDatabaseSize(TestTenantId, storageInfo);

        // Act
        var result = await _enforcer.CheckQuotaAsync(TestTenantId);

        // Assert
        result.TenantId.Should().Be(TestTenantId);
        result.CurrentSizeBytes.Should().Be(1200);
        result.QuotaBytes.Should().Be(1000);
        result.UsagePercent.Should().Be(120.0); // 1200/1000 * 100
        result.IsOverQuota.Should().BeTrue();
        result.IsNearQuota.Should().BeFalse(); // Over quota takes precedence
    }

    /// <summary>
    /// Tests that CheckQuotaAsync returns a result indicating the tenant is near quota when its usage reaches the warning threshold.
    /// </summary>
    [Fact]
    public async Task CheckQuotaAsync_NearQuotaWarning_ReturnsNearQuotaTrue()
    {
        // Arrange
        var tenant = new Tenant { TenantId = TestTenantId };
        tenant.SetMetadata(TenantQuotaEnforcer.QuotaMetadataKey, "1000"); // 1KB quota

        var storageInfo = new TenantStorageInfo
        {
            TenantId = TestTenantId,
            SizeBytes = 900, // 90% of quota (at warning threshold)
            PageCount = 22,
            PageSize = 40,
            WalSizeBytes = 0
        };

        _tenantService.SetupGetTenant(TestTenantId, tenant);
        _tenantService.SetupGetTenantDatabaseSize(TestTenantId, storageInfo);

        // Act
        var result = await _enforcer.CheckQuotaAsync(TestTenantId);

        // Assert
        result.TenantId.Should().Be(TestTenantId);
        result.CurrentSizeBytes.Should().Be(900);
        result.QuotaBytes.Should().Be(1000);
        result.UsagePercent.Should().Be(90.0); // 900/1000 * 100
        result.IsOverQuota.Should().BeFalse();
        result.IsNearQuota.Should().BeTrue(); // At warning threshold
    }

    /// <summary>
    /// Tests that CheckQuotaAsync returns a result with null quota and zero usage percent when no quota metadata is set on the tenant (unlimited quota).
    /// </summary>
    [Fact]
    public async Task CheckQuotaAsync_UnlimitedQuota_ReturnsZeroUsage()
    {
        // Arrange
        var tenant = new Tenant { TenantId = TestTenantId };
        // No quota metadata set (unlimited)

        var storageInfo = new TenantStorageInfo
        {
            TenantId = TestTenantId,
            SizeBytes = 2048, // 2KB usage
            PageCount = 51,
            PageSize = 40,
            WalSizeBytes = 0
        };

        _tenantService.SetupGetTenant(TestTenantId, tenant);
        _tenantService.SetupGetTenantDatabaseSize(TestTenantId, storageInfo);

        // Act
        var result = await _enforcer.CheckQuotaAsync(TestTenantId);

        // Assert
        result.TenantId.Should().Be(TestTenantId);
        result.CurrentSizeBytes.Should().Be(2048);
        result.QuotaBytes.Should().BeNull(); // Unlimited quota
        result.UsagePercent.Should().Be(0.0); // 0% for unlimited
        result.IsOverQuota.Should().BeFalse();
        result.IsNearQuota.Should().BeFalse();
    }

    /// <summary>
    /// Tests that EnforceAsync calls SuspendTenantAsync on the tenant service when the tenant is over quota and auto-suspend is enabled.
    /// </summary>
    [Fact]
    public async Task EnforceAsync_OverQuotaWithAutoSuspend_CallsSuspendTenant()
    {
        // Arrange
        var tenant = new Tenant { TenantId = TestTenantId };
        tenant.SetMetadata(TenantQuotaEnforcer.QuotaMetadataKey, "1000"); // 1KB quota

        var storageInfo = new TenantStorageInfo
        {
            TenantId = TestTenantId,
            SizeBytes = 1200, // Over quota
            PageCount = 30,
            PageSize = 40,
            WalSizeBytes = 0
        };

        _tenantService.SetupGetTenant(TestTenantId, tenant);
        _tenantService.SetupGetTenantDatabaseSize(TestTenantId, storageInfo);

        // Act
        var result = await _enforcer.EnforceAsync(TestTenantId, autoSuspend: true);

        // Assert
        _tenantService.SuspendTenantCalledWith.Should().Be(TestTenantId);
        result.IsOverQuota.Should().BeTrue();
    }

    /// <summary>
    /// Tests that ScanAllAsync returns tenants that are near or over quota, sorted by usage percentage descending with over-quota tenants first.
    /// </summary>
    [Fact]
    public async Task ScanAllAsync_ReturnsTenantsNearOrOverQuota_SortedByUsage()
    {
        // Arrange
        var tenant1 = new Tenant { TenantId = "tenant1" };
        tenant1.SetMetadata(TenantQuotaEnforcer.QuotaMetadataKey, "1000"); // 1KB quota

        var tenant2 = new Tenant { TenantId = "tenant2" };
        tenant2.SetMetadata(TenantQuotaEnforcer.QuotaMetadataKey, "1000"); // 1KB quota

        var tenant3 = new Tenant { TenantId = "tenant3" };
        tenant3.SetMetadata(TenantQuotaEnforcer.QuotaMetadataKey, "1000"); // 1KB quota

        var storageInfo1 = new TenantStorageInfo
        {
            TenantId = "tenant1",
            SizeBytes = 950, // 95% - near quota
            PageCount = 23,
            PageSize = 40,
            WalSizeBytes = 0
        };

        var storageInfo2 = new TenantStorageInfo
        {
            TenantId = "tenant2",
            SizeBytes = 1050, // 105% - over quota
            PageCount = 26,
            PageSize = 40,
            WalSizeBytes = 0
        };

        var storageInfo3 = new TenantStorageInfo
        {
            TenantId = "tenant3",
            SizeBytes = 500, // 50% - under quota (should be excluded)
            PageCount = 12,
            PageSize = 40,
            WalSizeBytes = 0
        };

        _tenantService.SetupGetActiveTenants(new List<Tenant> { tenant1, tenant2, tenant3 });
        _tenantService.SetupGetTenant("tenant1", tenant1);
        _tenantService.SetupGetTenant("tenant2", tenant2);
        _tenantService.SetupGetTenant("tenant3", tenant3);
        _tenantService.SetupGetTenantDatabaseSize("tenant1", storageInfo1);
        _tenantService.SetupGetTenantDatabaseSize("tenant2", storageInfo2);
        _tenantService.SetupGetTenantDatabaseSize("tenant3", storageInfo3);

        // Act
        var results = await _enforcer.ScanAllAsync();

        // Assert
        results.Should().HaveCount(2); // tenant1 (near) and tenant2 (over), tenant3 excluded

        // Should be sorted by usage percent descending, then over quota first
        results[0].TenantId.Should().Be("tenant2"); // 105% usage, over quota (highest)
        results[0].UsagePercent.Should().Be(105.0);
        results[0].IsOverQuota.Should().BeTrue();

        results[1].TenantId.Should().Be("tenant1"); // 95% usage, near quota
        results[1].UsagePercent.Should().Be(95.0);
        results[1].IsOverQuota.Should().BeFalse();
        results[1].IsNearQuota.Should().BeTrue();
    }

    private class FakeTenantService : ITenantService
    {
        private readonly Dictionary<string, Tenant> _tenants = new();
        private readonly Dictionary<string, TenantStorageInfo> _storageInfos = new();
        private readonly Dictionary<string, Dictionary<string, string>> _metadata = new();
        private readonly List<string> _suspendedTenants = new();

        public string? SuspendTenantCalledWith { get; private set; }

        public void SetupGetTenant(string tenantId, Tenant? tenant)
        {
            if (tenant != null)
            {
                _tenants[tenantId] = tenant;
            }
            else
            {
                _tenants.Remove(tenantId);
            }
        }

        public void SetupGetTenantDatabaseSize(string tenantId, TenantStorageInfo storageInfo)
        {
            _storageInfos[tenantId] = storageInfo;
        }

        public void SetupGetActiveTenants(List<Tenant> tenants)
        {
            foreach (var tenant in tenants)
            {
                _tenants[tenant.TenantId] = tenant;
            }
        }

        public Task<Tenant?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            _tenants.TryGetValue(tenantId, out var tenant);
            return Task.FromResult<Tenant?>(tenant);
        }

        public Task<Tenant> CreateTenantAsync(string name, string? description = null, string? contactEmail = null, CancellationToken cancellationToken = default)
        {
            var tenant = new Tenant
            {
                TenantId = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                ContactEmail = contactEmail,
                Status = TenantStatus.Active
            };

            _tenants[tenant.TenantId] = tenant;
            return Task.FromResult(tenant);
        }

        public Task UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            if (_tenants.ContainsKey(tenant.TenantId))
            {
                _tenants[tenant.TenantId] = tenant;
            }

            return Task.CompletedTask;
        }

        public Task DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            _tenants.Remove(tenantId);
            _metadata.Remove(tenantId);
            return Task.CompletedTask;
        }

        public Task<List<Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<Tenant>(_tenants.Values));
        }

        public Task<List<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
        {
            var activeTenants = new List<Tenant>();
            foreach (var tenant in _tenants.Values)
            {
                if (tenant.Status == TenantStatus.Active)
                {
                    activeTenants.Add(tenant);
                }
            }

            return Task.FromResult(activeTenants);
        }

        public Task ActivateTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (_tenants.TryGetValue(tenantId, out var tenant))
            {
                tenant.Status = TenantStatus.Active;
            }

            return Task.CompletedTask;
        }

        public Task DeactivateTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (_tenants.TryGetValue(tenantId, out var tenant))
            {
                tenant.Status = TenantStatus.Inactive;
            }

            return Task.CompletedTask;
        }

        public Task SuspendTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            SuspendTenantCalledWith = tenantId;
            if (_tenants.TryGetValue(tenantId, out var tenant))
            {
                tenant.Status = TenantStatus.Suspended; // Simplified: suspended tenants have Suspended status
            }

            return Task.CompletedTask;
        }

        public Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tenants.ContainsKey(tenantId));
        }

        public Task<int> GetTenantCountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tenants.Count);
        }

        public Task<List<Tenant>> SearchTenantsAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var results = new List<Tenant>();
            foreach (var tenant in _tenants.Values)
            {
                if (tenant.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    tenant.TenantId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(tenant);
                }
            }

            return Task.FromResult(results);
        }

        public Task SetTenantMetadataAsync(string tenantId, string key, string value, CancellationToken cancellationToken = default)
        {
            if (!_metadata.ContainsKey(tenantId))
            {
                _metadata[tenantId] = new Dictionary<string, string>();
            }

            _metadata[tenantId][key] = value;
            return Task.CompletedTask;
        }

        public Task<TenantStorageInfo> GetTenantDatabaseSizeAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            _storageInfos.TryGetValue(tenantId, out var storageInfo);
            return Task.FromResult(storageInfo ?? new TenantStorageInfo());
        }
    }
}