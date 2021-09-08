#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using SqliteMultiTenant.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Extension methods for <see cref="TenantNameValidatorTests"/> that provide fluent assertions
/// and additional test utilities for tenant name validation scenarios.
/// </summary>
public static class TenantNameValidatorTestsExtensions
{
    /// <summary>
    /// Asserts that a tenant ID validation result is valid and has no errors.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="tenantId">The tenant ID being validated.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tenantId"/> is null.</exception>
    public static void ShouldBeValidTenantId(this TenantNameValidatorTests test, string tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        var result = TenantNameValidator.ValidateTenantId(tenantId);
        result.IsValid.Should().BeTrue($"Expected tenant ID '{tenantId}' to be valid");
        result.Error.Should().BeNullOrEmpty($"Expected no error for valid tenant ID '{tenantId}'");
    }

    /// <summary>
    /// Asserts that a tenant ID validation result is invalid with a specific error message.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="tenantId">The tenant ID being validated.</param>
    /// <param name="expectedErrorSubstring">The substring that must appear in the error message.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tenantId"/> or <paramref name="expectedErrorSubstring"/> is null.</exception>
    public static void ShouldBeInvalidTenantIdWithError(this TenantNameValidatorTests test, string tenantId, string expectedErrorSubstring)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(expectedErrorSubstring);

        var result = TenantNameValidator.ValidateTenantId(tenantId);
        result.IsValid.Should().BeFalse($"Expected tenant ID '{tenantId}' to be invalid");
        result.Error.Should().NotBeNullOrEmpty($"Expected error message for invalid tenant ID '{tenantId}'");
        result.Error.Should().Contain(expectedErrorSubstring);
    }

    /// <summary>
    /// Asserts that a tenant ID is generated correctly from a tenant name.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="tenantName">The original tenant name with special characters.</param>
    /// <param name="expectedTenantId">The expected normalized tenant ID.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tenantName"/> or <paramref name="expectedTenantId"/> is null.</exception>
    public static void ShouldGenerateTenantId(this TenantNameValidatorTests test, string tenantName, string expectedTenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantName);
        ArgumentNullException.ThrowIfNull(expectedTenantId);

        var result = TenantNameValidator.GenerateTenantId(tenantName);
        result.Should().Be(expectedTenantId, $"Expected tenant ID for '{tenantName}' to be '{expectedTenantId}'");
    }

    /// <summary>
    /// Creates a collection of invalid tenant IDs for parameterized testing.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <returns>An enumerable of invalid tenant ID test cases.</returns>
    public static IEnumerable<(string TenantId, string ExpectedError)> GetInvalidTenantIds(this TenantNameValidatorTests test)
    {
        yield return ("ab", "3"); // Below minimum length
        yield return ("admin", "reserved"); // Reserved word
        yield return ("my tenant", "invalid"); // Contains spaces
        yield return ("MyTenant", "invalid"); // Contains uppercase
        yield return ("", "empty"); // Empty string
        yield return (null!, "null"); // Null (will throw ArgumentNullException)
    }

    /// <summary>
    /// Creates a collection of valid tenant name to ID mappings for testing.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <returns>An enumerable of valid tenant name mappings.</returns>
    public static IEnumerable<(string TenantName, string ExpectedTenantId)> GetValidTenantNameMappings(this TenantNameValidatorTests test)
    {
        yield return ("acme-corp", "acme-corp");
        yield return ("My Company 2024", "my-company-2024");
        yield return ("test-tenant-123", "test-tenant-123");
        yield return ("a-b-c", "a-b-c");
        yield return ("single", "single");
    }
}