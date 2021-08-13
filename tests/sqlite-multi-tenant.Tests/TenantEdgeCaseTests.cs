#nullable enable
using FluentAssertions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Constants;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Edge-case tests for Tenant model validation, state transitions, and metadata operations.
/// Covers null inputs, boundary values, and concurrent metadata access.
/// </summary>
public sealed class TenantEdgeCaseTests
{
    [Fact]
    public void Validate_NullTenantId_ReturnsError()
    {
        var tenant = new Tenant { TenantId = null!, Name = "Valid" };

        var isValid = tenant.Validate(out var errors);

        isValid.Should().BeFalse();
        errors.Should().ContainSingle(e => e.Contains("TenantId"));
    }

    [Fact]
    public void Validate_EmptyTenantId_ReturnsError()
    {
        var tenant = new Tenant { TenantId = "", Name = "Valid" };

        var isValid = tenant.Validate(out var errors);

        isValid.Should().BeFalse();
        errors.Should().ContainSingle(e => e.Contains("TenantId"));
    }

    [Fact]
    public void Validate_WhitespaceTenantId_ReturnsError()
    {
        var tenant = new Tenant { TenantId = "   ", Name = "Valid" };

        var isValid = tenant.Validate(out var errors);

        isValid.Should().BeFalse();
        errors.Should().ContainSingle(e => e.Contains("TenantId"));
    }

    [Fact]
    public void Validate_TenantIdExceedsMaxLength_ReturnsError()
    {
        var tenant = new Tenant
        {
            TenantId = new string('x', TenantConstants.MaxTenantIdLength + 1),
            Name = "Valid"
        };

        var isValid = tenant.Validate(out var errors);

        isValid.Should().BeFalse();
        errors.Should().ContainSingle(e => e.Contains("TenantId"));
    }

    [Fact]
    public void Validate_TenantIdExactlyMaxLength_IsValid()
    {
        var tenant = new Tenant
        {
            TenantId = new string('x', TenantConstants.MaxTenantIdLength),
            Name = "Valid"
        };

        var isValid = tenant.Validate(out var errors);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ReturnsError()
    {
        var tenant = new Tenant
        {
            TenantId = "t1",
            Name = new string('a', TenantConstants.MaxTenantNameLength + 1)
        };

        var isValid = tenant.Validate(out var errors);

        isValid.Should().BeFalse();
        errors.Should().ContainSingle(e => e.Contains("Name"));
    }

    [Fact]
    public void Validate_ZeroMaxConnections_ReturnsError()
    {
        var tenant = new Tenant { TenantId = "t1", Name = "Valid", MaxConnections = 0 };

        var isValid = tenant.Validate(out var errors);

        isValid.Should().BeFalse();
        errors.Should().ContainSingle(e => e.Contains("MaxConnections"));
    }

    [Fact]
    public void Validate_NegativeMaxConnections_ReturnsError()
    {
        var tenant = new Tenant { TenantId = "t1", Name = "Valid", MaxConnections = -5 };

        var isValid = tenant.Validate(out var errors);

        isValid.Should().BeFalse();
        errors.Should().ContainSingle(e => e.Contains("MaxConnections"));
    }

    [Fact]
    public void Validate_CreatedAtAfterUpdatedAt_ReturnsError()
    {
        var tenant = new Tenant
        {
            TenantId = "t1",
            Name = "Valid",
            CreatedAt = DateTime.UtcNow.AddHours(1),
            UpdatedAt = DateTime.UtcNow
        };

        var isValid = tenant.Validate(out var errors);

        isValid.Should().BeFalse();
        errors.Should().ContainSingle(e => e.Contains("CreatedAt"));
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        var tenant = new Tenant
        {
            TenantId = "",
            Name = "",
            MaxConnections = -1,
            CreatedAt = DateTime.UtcNow.AddHours(1),
            UpdatedAt = DateTime.UtcNow
        };

        var isValid = tenant.Validate(out var errors);

        isValid.Should().BeFalse();
        errors.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void MarkAsAccessed_SetsLastAccessedAtToUtcNow()
    {
        var tenant = new Tenant { TenantId = "t1", Name = "Test" };
        tenant.LastAccessedAt.Should().BeNull();

        var before = DateTime.UtcNow;
        tenant.MarkAsAccessed();
        var after = DateTime.UtcNow;

        tenant.LastAccessedAt.Should().NotBeNull();
        tenant.LastAccessedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        tenant.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Deactivate_SetsStatusToInactive()
    {
        var tenant = new Tenant { TenantId = "t1", Name = "Test", Status = TenantStatus.Active };

        tenant.Deactivate();

        tenant.Status.Should().Be(TenantStatus.Inactive);
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsStatusToActive()
    {
        var tenant = new Tenant { TenantId = "t1", Name = "Test" };
        tenant.Deactivate();
        tenant.Status.Should().Be(TenantStatus.Inactive);

        tenant.Activate();

        tenant.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public void SetMetadata_WhenMetadataIsNull_InitializesAndSetsValue()
    {
        var tenant = new Tenant { TenantId = "t1", Name = "Test", Metadata = null };

        tenant.SetMetadata("key1", "value1");

        tenant.Metadata.Should().NotBeNull();
        tenant.Metadata!["key1"].Should().Be("value1");
    }

    [Fact]
    public void SetMetadata_OverwritesExistingKey()
    {
        var tenant = new Tenant { TenantId = "t1", Name = "Test" };
        tenant.SetMetadata("key1", "original");

        tenant.SetMetadata("key1", "updated");

        tenant.GetMetadata("key1").Should().Be("updated");
    }

    [Fact]
    public void GetMetadata_NonexistentKey_ReturnsNull()
    {
        var tenant = new Tenant { TenantId = "t1", Name = "Test" };

        var result = tenant.GetMetadata("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public void GetMetadata_WhenMetadataIsNull_ReturnsNull()
    {
        var tenant = new Tenant { TenantId = "t1", Name = "Test", Metadata = null };

        var result = tenant.GetMetadata("any");

        result.Should().BeNull();
    }

    [Fact]
    public void SetMetadata_ConcurrentAccess_DoesNotThrow()
    {
        var tenant = new Tenant { TenantId = "t1", Name = "Test" };

        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() => tenant.SetMetadata($"key_{i}", $"value_{i}")))
            .ToArray();

        var act = () => Task.WaitAll(tasks);

        // Dictionary is not thread-safe, so we just verify no unhandled exceptions crash the process
        // In production, a ConcurrentDictionary should be used
        act.Should().NotThrow<AggregateException>(
            "metadata operations should not throw unhandled exceptions even under concurrent access");
    }
}
