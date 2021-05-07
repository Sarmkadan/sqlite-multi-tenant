#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using SqliteMultiTenant.Utilities;
using Xunit;

namespace SqliteMultiTenant.Tests;

public sealed class TenantNameValidatorTests {
    [Fact]
    public void ValidateTenantId_WithValidId_ReturnsValidResult()
    {
        var result = TenantNameValidator.ValidateTenantId("my-tenant");

        result.IsValid.Should().BeTrue();
        result.Error.Should().BeNullOrEmpty();
    }

    [Fact]
    public void ValidateTenantId_BelowMinimumLength_ReturnsInvalidWithLengthError()
    {
        // Minimum is 3 characters; "ab" is only 2
        var result = TenantNameValidator.ValidateTenantId("ab");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("3");
    }

    [Fact]
    public void ValidateTenantId_UsingReservedWord_ReturnsReservedError()
    {
        var result = TenantNameValidator.ValidateTenantId("admin");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("reserved");
    }

    [Fact]
    public void GenerateTenantId_FromNameWithSpecialCharactersAndSpaces_ReturnsNormalizedSlug()
    {
        // Spaces become hyphens, "!" is stripped, everything is lowercased
        var result = TenantNameValidator.GenerateTenantId("Acme Corporation 2024!");

        result.Should().Be("acme-corporation-2024");
    }
}
