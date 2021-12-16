#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using SqliteMultiTenant.Utilities;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Contains unit tests for <see cref="TenantNameValidator"/> tenant name validation and generation functionality.
/// </summary>
public sealed class TenantNameValidatorTests
{
	/// <summary>
	/// Validates that a properly formatted tenant ID returns a valid validation result.
	/// </summary>
	[Fact]
	public void ValidateTenantId_WithValidId_ReturnsValidResult()
	{
		// Act
		var result = TenantNameValidator.ValidateTenantId("my-tenant");

		// Assert
		result.IsValid.Should().BeTrue();
		result.Error.Should().BeNullOrEmpty();
	}

	/// <summary>
	/// Validates that a tenant ID below the minimum length (3 characters) returns an invalid result with a length error.
	/// </summary>
	[Fact]
	public void ValidateTenantId_BelowMinimumLength_ReturnsInvalidWithLengthError()
	{
		// Arrange
		// Minimum is 3 characters; "ab" is only 2

		// Act
		var result = TenantNameValidator.ValidateTenantId("ab");

		// Assert
		result.IsValid.Should().BeFalse();
		result.Error.Should().Contain("3");
	}

	/// <summary>
	/// Validates that a tenant ID using a reserved word returns an invalid result with a reserved word error.
	/// </summary>
	[Fact]
	public void ValidateTenantId_UsingReservedWord_ReturnsReservedError()
	{
		// Act
		var result = TenantNameValidator.ValidateTenantId("admin");

		// Assert
		result.IsValid.Should().BeFalse();
		result.Error.Should().Contain("reserved");
	}

	/// <summary>
	/// Validates that a tenant name with special characters and spaces is properly normalized to a tenant ID slug.
	/// </summary>
	/// <remarks>
	/// The normalization process converts spaces to hyphens, removes special characters like '!', and converts to lowercase.
	/// </remarks>
	[Fact]
	public void GenerateTenantId_FromNameWithSpecialCharactersAndSpaces_ReturnsNormalizedSlug()
	{
		// Arrange
		// Spaces become hyphens, "!" is stripped, everything is lowercased

		// Act
		var result = TenantNameValidator.GenerateTenantId("Acme Corporation 2024!");

		// Assert
		result.Should().Be("acme-corporation-2024");
	}
}