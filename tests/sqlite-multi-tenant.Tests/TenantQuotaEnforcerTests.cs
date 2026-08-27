using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Tenants;
using Xunit;

namespace SqliteMultiTenant.Tests.Tenants;

/// <summary>
/// Contains unit tests for the TenantQuotaEnforcer class.
/// Tests cover quota checking, enforcement with auto-suspend, quota setting and retrieval, and scanning all tenants for quota usage.
/// </summary>
public class TenantQuotaEnforcerTests
{
    private readonly ITenantService _tenantService;
    private readonly TenantQuotaEnforcer _enforcer;
    private const string TestTenantId = "test-tenant";

    /// <summary>
    /// Initializes a new instance of the TenantQuotaEnforcerTests class with a mocked tenant service and the enforcer under test.
    /// </summary>
    public TenantQuotaEnforcerTests()
    {
        _tenantService = Substitute.For<ITenantService>();
        _enforcer = new TenantQuotaEnforcer(_tenantService);
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

        _tenantService.GetTenantAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(tenant);
        _tenantService.GetTenantDatabaseSizeAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(storageInfo);

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
    /// Tests that CheckQuotaAsync returns a result indicating the tenant is over quota when its current size exactly matches the configured quota.
    /// </summary>
    [Fact]
    public async Task CheckQuotaAsync_AtBoundaryQuota_ReturnsOverQuota()
    {
        // Arrange
        var tenant = new Tenant { TenantId = TestTenantId };
        tenant.SetMetadata(TenantQuotaEnforcer.QuotaMetadataKey, "1000"); // 1KB quota

        var storageInfo = new TenantStorageInfo
        {
            TenantId = TestTenantId,
            SizeBytes = 1000, // Exactly at quota
            PageCount = 25,
            PageSize = 40,
            WalSizeBytes = 0
        };

        _tenantService.GetTenantAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(tenant);
        _tenantService.GetTenantDatabaseSizeAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(storageInfo);

        // Act
        var result = await _enforcer.CheckQuotaAsync(TestTenantId);

        // Assert
        result.TenantId.Should().Be(TestTenantId);
        result.CurrentSizeBytes.Should().Be(1000);
        result.QuotaBytes.Should().Be(1000);
        result.UsagePercent.Should().Be(100.0); // 1000/1000 * 100
        result.IsOverQuota.Should().BeTrue(); // At boundary is considered over quota
        result.IsNearQuota.Should().BeFalse(); // Over quota takes precedence
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

        _tenantService.GetTenantAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(tenant);
        _tenantService.GetTenantDatabaseSizeAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(storageInfo);

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

        _tenantService.GetTenantAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(tenant);
        _tenantService.GetTenantDatabaseSizeAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(storageInfo);

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

        _tenantService.GetTenantAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(tenant);
        _tenantService.GetTenantDatabaseSizeAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(storageInfo);

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
    /// Tests that CheckQuotaAsync throws a TenantNotFoundException when the tenant service returns null for the requested tenant ID.
    /// </summary>
    [Fact]
    public async Task CheckQuotaAsync_TenantNotFound_ThrowsTenantNotFoundException()
    {
        // Arrange
        _tenantService.GetTenantAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        // Act
        Func<Task> act = async () => await _enforcer.CheckQuotaAsync(TestTenantId);

        // Assert
        await act.Should().ThrowAsync<TenantNotFoundException>()
            .WithMessage($"Tenant with ID '{TestTenantId}' was not found.");
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

        _tenantService.GetTenantAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(tenant);
        _tenantService.GetTenantDatabaseSizeAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(storageInfo);

        // Act
        var result = await _enforcer.EnforceAsync(TestTenantId, autoSuspend: true);

        // Assert
        await _tenantService.Received(1).SuspendTenantAsync(TestTenantId, Arg.Any<CancellationToken>());
        result.IsOverQuota.Should().BeTrue();
    }

    /// <summary>
    /// Tests that EnforceAsync does not call SuspendTenantAsync on the tenant service when the tenant is over quota but auto-suspend is disabled.
    /// </summary>
    [Fact]
    public async Task EnforceAsync_OverQuotaWithoutAutoSuspend_DoesNotCallSuspendTenant()
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

        _tenantService.GetTenantAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(tenant);
        _tenantService.GetTenantDatabaseSizeAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(storageInfo);

        // Act
        var result = await _enforcer.EnforceAsync(TestTenantId, autoSuspend: false);

        // Assert
        await _tenantService.DidNotReceive().SuspendTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        result.IsOverQuota.Should().BeTrue();
    }

    /// <summary>
    /// Tests that SetQuotaAsync correctly stores the quota as tenant metadata when given a positive byte value.
    /// </summary>
    [Fact]
    public async Task SetQuotaAsync_PositiveMaxBytes_SetsMetadataCorrectly()
    {
        // Arrange
        const long maxBytes = 5000;

        // Act
        await _enforcer.SetQuotaAsync(TestTenantId, maxBytes);

        // Assert
        await _tenantService.Received(1).SetTenantMetadataAsync(
            TestTenantId,
            TenantQuotaEnforcer.QuotaMetadataKey,
            maxBytes.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Arg.Any<CancellationToken>());
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
    /// Tests that GetQuotaAsync returns the parsed quota value when the tenant has valid quota metadata.
    /// </summary>
    [Fact]
    public async Task GetQuotaAsync_WithValidQuotaMetadata_ReturnsParsedValue()
    {
        // Arrange
        var tenant = new Tenant { TenantId = TestTenantId };
        tenant.SetMetadata(TenantQuotaEnforcer.QuotaMetadataKey, "2048");

        _tenantService.GetTenantAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(tenant);

        // Act
        var quota = await _enforcer.GetQuotaAsync(TestTenantId);

        // Assert
        quota.Should().Be(2048L);
    }

    /// <summary>
    /// Tests that GetQuotaAsync returns null when the tenant exists but has no quota metadata set.
    /// </summary>
    [Fact]
    public async Task GetQuotaAsync_WithMissingQuotaMetadata_ReturnsNull()
    {
        // Arrange
        var tenant = new Tenant { TenantId = TestTenantId };
        // No quota metadata set

        _tenantService.GetTenantAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(tenant);

        // Act
        var quota = await _enforcer.GetQuotaAsync(TestTenantId);

        // Assert
        quota.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetQuotaAsync returns null when the tenant's quota metadata cannot be parsed as a number.
    /// </summary>
    [Fact]
    public async Task GetQuotaAsync_WithInvalidQuotaMetadata_ReturnsNull()
    {
        // Arrange
        var tenant = new Tenant { TenantId = TestTenantId };
        tenant.SetMetadata(TenantQuotaEnforcer.QuotaMetadataKey, "invalid-number");

        _tenantService.GetTenantAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns(tenant);

        // Act
        var quota = await _enforcer.GetQuotaAsync(TestTenantId);

        // Assert
        quota.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetQuotaAsync returns null when the tenant service returns null for the requested tenant ID.
    /// </summary>
    [Fact]
    public async Task GetQuotaAsync_TenantNotFound_ReturnsNull()
    {
        // Arrange
        _tenantService.GetTenantAsync(TestTenantId, Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        // Act
        var quota = await _enforcer.GetQuotaAsync(TestTenantId);

        // Assert
        quota.Should().BeNull();
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

        _tenantService.GetActiveTenantsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Tenant> { tenant1, tenant2, tenant3 });

        _tenantService.GetTenantAsync("tenant1", Arg.Any<CancellationToken>())
            .Returns(tenant1);
        _tenantService.GetTenantAsync("tenant2", Arg.Any<CancellationToken>())
            .Returns(tenant2);
        _tenantService.GetTenantAsync("tenant3", Arg.Any<CancellationToken>())
            .Returns(tenant3);

        _tenantService.GetTenantDatabaseSizeAsync("tenant1", Arg.Any<CancellationToken>())
            .Returns(storageInfo1);
        _tenantService.GetTenantDatabaseSizeAsync("tenant2", Arg.Any<CancellationToken>())
            .Returns(storageInfo2);
        _tenantService.GetTenantDatabaseSizeAsync("tenant3", Arg.Any<CancellationToken>())
            .Returns(storageInfo3);

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
}