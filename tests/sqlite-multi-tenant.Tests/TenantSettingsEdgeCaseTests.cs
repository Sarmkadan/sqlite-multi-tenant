#nullable enable
using FluentAssertions;
using SqliteMultiTenant.Models;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Edge-case tests for TenantSettings model - type conversion, validation boundaries, and error handling.
/// </summary>
public sealed class TenantSettingsEdgeCaseTests
{
    [Fact]
    public void Validate_EmptySettingId_ReturnsError()
    {
        var settings = new TenantSettings
        {
            SettingId = "",
            TenantId = "t1",
            SettingKey = "key",
            SettingValue = "val"
        };

        var isValid = settings.Validate(out var errors);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("SettingId"));
    }

    [Fact]
    public void Validate_EmptyTenantId_ReturnsError()
    {
        var settings = new TenantSettings
        {
            SettingId = "s1",
            TenantId = "",
            SettingKey = "key",
            SettingValue = "val"
        };

        var isValid = settings.Validate(out var errors);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("TenantId"));
    }

    [Fact]
    public void Validate_SettingKeyExceedsMaxLength_ReturnsError()
    {
        var settings = new TenantSettings
        {
            SettingId = "s1",
            TenantId = "t1",
            SettingKey = new string('k', 257),
            SettingValue = "val"
        };

        var isValid = settings.Validate(out var errors);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("SettingKey"));
    }

    [Fact]
    public void Validate_SettingKeyExactly256Chars_IsValid()
    {
        var settings = new TenantSettings
        {
            SettingId = "s1",
            TenantId = "t1",
            SettingKey = new string('k', 256),
            SettingValue = "val"
        };

        var isValid = settings.Validate(out var errors);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void GetValue_ValidIntString_ReturnsInt()
    {
        var settings = new TenantSettings { SettingValue = "42" };

        var result = settings.GetValue<int>();

        result.Should().Be(42);
    }

    [Fact]
    public void GetValue_InvalidConversion_ThrowsInvalidOperationException()
    {
        var settings = new TenantSettings { SettingValue = "not-a-number" };

        var act = () => settings.GetValue<int>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot convert*");
    }

    [Fact]
    public void GetValue_EmptyString_ToInt_ThrowsInvalidOperationException()
    {
        var settings = new TenantSettings { SettingValue = "" };

        var act = () => settings.GetValue<int>();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetValue_BoolConversion_Works()
    {
        var settings = new TenantSettings { SettingValue = "True" };

        var result = settings.GetValue<bool>();

        result.Should().BeTrue();
    }

    [Fact]
    public void SetValue_SetsDataTypeToTypeName()
    {
        var settings = new TenantSettings();

        settings.SetValue(123, "admin");

        settings.SettingValue.Should().Be("123");
        settings.DataType.Should().Be("Int32");
        settings.LastModifiedBy.Should().Be("admin");
    }

    [Fact]
    public void UpdateValue_UpdatesTimestampAndModifiedBy()
    {
        var settings = new TenantSettings();
        var before = DateTime.UtcNow;

        settings.UpdateValue("new-value", "user1");

        settings.SettingValue.Should().Be("new-value");
        settings.LastModifiedBy.Should().Be("user1");
        settings.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void UpdateValue_NullModifiedBy_SetsToNull()
    {
        var settings = new TenantSettings { LastModifiedBy = "previous" };

        settings.UpdateValue("val");

        settings.LastModifiedBy.Should().BeNull();
    }

    [Fact]
    public void SetActive_ToFalse_DeactivatesSetting()
    {
        var settings = new TenantSettings { IsActive = true };

        settings.SetActive(false);

        settings.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SetActive_ToTrue_ActivatesSetting()
    {
        var settings = new TenantSettings { IsActive = false };

        settings.SetActive(true);

        settings.IsActive.Should().BeTrue();
    }
}
