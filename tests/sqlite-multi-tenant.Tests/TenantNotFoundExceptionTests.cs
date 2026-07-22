using FluentAssertions;
using SqliteMultiTenant.Exceptions;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class TenantNotFoundExceptionTests
{
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