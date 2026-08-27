using FluentAssertions;
using SqliteMultiTenant.Exceptions;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Contains unit tests for the <see cref="TenantNotFoundException"/> class.
/// Tests cover various constructor overloads and edge cases for tenant ID handling.
/// </summary>
public class TenantNotFoundExceptionTests
{
    /// <summary>
    /// Verifies that the constructor with a tenant ID sets the expected message,
    /// the tenant ID, and a null inner exception.
    /// </summary>
    [Fact]
    public void Constructor_WithTenantId_ShouldSetMessageAndTenantId()
    {
        // Arrange
        var tenantId = "tenant-123";

        // Act
        var exception = new TenantNotFoundException(tenantId);

        // Assert
        exception.Message.Should().Be($"Tenant with ID '{tenantId}' was not found.");
        exception.TenantId.Should().Be(tenantId);
        exception.InnerException.Should().BeNull();
    }

    /// <summary>
/// Verifies that the constructor with a null tenant ID sets the expected message,
/// a null tenant ID, and a null inner exception.
/// </summary>
    [Fact]
    public void Constructor_WithNullTenantId_ShouldSetMessageAndNullTenantId()
    {
        // Arrange
        string? tenantId = null;

        // Act
        var exception = new TenantNotFoundException(tenantId);

        // Assert
        exception.Message.Should().Be("Tenant with ID '' was not found.");
        exception.TenantId.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    /// <summary>
/// Verifies that the constructor with an empty tenant ID sets the expected message,
/// an empty tenant ID, and a null inner exception.
/// </summary>
    [Fact]
    public void Constructor_WithEmptyTenantId_ShouldSetMessageAndEmptyTenantId()
    {
        // Arrange
        var tenantId = string.Empty;

        // Act
        var exception = new TenantNotFoundException(tenantId);

        // Assert
        exception.Message.Should().Be("Tenant with ID '' was not found.");
        exception.TenantId.Should().BeEmpty();
        exception.InnerException.Should().BeNull();
    }

    /// <summary>
/// Verifies that the constructor with a tenant ID and inner exception sets all properties correctly.
/// </summary>
    [Fact]
    public void Constructor_WithTenantIdAndInnerException_ShouldSetProperties()
    {
        // Arrange
        var tenantId = "tenant-456";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new TenantNotFoundException(tenantId, innerException);

        // Assert
        exception.Message.Should().Be($"Tenant with ID '{tenantId}' was not found.");
        exception.TenantId.Should().Be(tenantId);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    /// <summary>
/// Verifies that the constructor with a null tenant ID and inner exception sets all properties correctly.
/// </summary>
    [Fact]
    public void Constructor_WithNullTenantIdAndInnerException_ShouldSetProperties()
    {
        // Arrange
        string? tenantId = null;
        var innerException = new Exception("Inner error");

        // Act
        var exception = new TenantNotFoundException(tenantId, innerException);

        // Assert
        exception.Message.Should().Be("Tenant with ID '' was not found.");
        exception.TenantId.Should().BeNull();
        exception.InnerException.Should().BeSameAs(innerException);
    }

    /// <summary>
/// Verifies that the constructor with a custom message, tenant ID, and inner exception
/// sets all properties to the provided values.
/// </summary>
    [Fact]
    public void Constructor_WithMessageTenantIdAndInnerException_ShouldSetAllProperties()
    {
        // Arrange
        var message = "Custom error message for tenant";
        var tenantId = "tenant-789";
        var innerException = new Exception("Inner error");

        // Act
        var exception = new TenantNotFoundException(message, tenantId, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.TenantId.Should().Be(tenantId);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    /// <summary>
/// Verifies that the constructor with a custom message, null tenant ID, and null inner exception
/// sets the message and leaves the tenant ID and inner exception null.
/// </summary>
    [Fact]
    public void Constructor_WithCustomMessageNullTenantIdAndNullInnerException_ShouldSetProperties()
    {
        // Arrange
        var message = "Custom error message";
        string? tenantId = null;
        Exception? innerException = null;

        // Act
        var exception = new TenantNotFoundException(message, tenantId, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.TenantId.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    /// <summary>
/// Verifies that the constructor with a whitespace-padded tenant ID sets the expected message
/// and preserves the tenant ID as provided.
/// </summary>
    [Fact]
    public void Constructor_WithWhitespaceTenantId_ShouldSetMessageAndTenantId()
    {
        // Arrange
        var tenantId = "  tenant-123  ";

        // Act
        var exception = new TenantNotFoundException(tenantId);

        // Assert
        exception.Message.Should().Be($"Tenant with ID '{tenantId}' was not found.");
        exception.TenantId.Should().Be(tenantId);
    }

    /// <summary>
/// Verifies that the constructor with a tenant ID containing special characters
/// sets the expected message and preserves the tenant ID as provided.
/// </summary>
    [Fact]
    public void Constructor_WithSpecialCharactersInTenantId_ShouldSetMessageAndTenantId()
    {
        // Arrange
        var tenantId = "tenant-123-@#$%";

        // Act
        var exception = new TenantNotFoundException(tenantId);

        // Assert
        exception.Message.Should().Be($"Tenant with ID '{tenantId}' was not found.");
        exception.TenantId.Should().Be(tenantId);
    }

    /// <summary>
/// Verifies that the constructor with a very long tenant ID sets the expected message
/// and preserves the tenant ID as provided.
/// </summary>
    [Fact]
    public void Constructor_WithLongTenantId_ShouldSetMessageAndTenantId()
    {
        // Arrange
        var tenantId = new string('x', 1000);

        // Act
        var exception = new TenantNotFoundException(tenantId);

        // Assert
        exception.Message.Should().Be($"Tenant with ID '{tenantId}' was not found.");
        exception.TenantId.Should().Be(tenantId);
        exception.Message.Length.Should().BeGreaterThan(100);
    }
}