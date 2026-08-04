using System;
using Xunit;
using SqliteMultiTenant.Exceptions;

namespace SqliteMultiTenant.Tests
{
    public class TenantNotFoundExceptionExtensionsTests
    {
        [Fact]
        public void IsMatchingTenantId_ReturnsTrue_WhenTenantIdsMatch()
        {
            // Arrange
            var exception = new TenantNotFoundException("tenant123");
            string tenantId = "tenant123";

            // Act
            bool result = TenantNotFoundExceptionExtensions.IsMatchingTenantId(exception, tenantId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsMatchingTenantId_ReturnsFalse_WhenTenantIdsDoNotMatch()
        {
            // Arrange
            var exception = new TenantNotFoundException("tenant123");
            string tenantId = "different";

            // Act
            bool result = TenantNotFoundExceptionExtensions.IsMatchingTenantId(exception, tenantId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMatchingTenantId_ReturnsFalse_WhenTenantIdIsNull()
        {
            // Arrange
            var exception = new TenantNotFoundException("tenant123");
            string? tenantId = null;

            // Act
            bool result = TenantNotFoundExceptionExtensions.IsMatchingTenantId(exception, tenantId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMatchingTenantId_ReturnsFalse_WhenTenantIdIsEmpty()
        {
            // Arrange
            var exception = new TenantNotFoundException("tenant123");
            string tenantId = "";

            // Act
            bool result = TenantNotFoundExceptionExtensions.IsMatchingTenantId(exception, tenantId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMatchingTenantId_ThrowsArgumentNullException_WhenExceptionIsNull()
        {
            // Arrange
            TenantNotFoundException exception = null!;
            string? tenantId = "any";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => TenantNotFoundExceptionExtensions.IsMatchingTenantId(exception, tenantId));
        }

        [Fact]
        public void GetErrorMessage_ReturnsFormattedMessage()
        {
            // Arrange
            var exception = new TenantNotFoundException("tenant456");

            // Act
            string result = TenantNotFoundExceptionExtensions.GetErrorMessage(exception);

            // Assert
            Assert.Equal("Tenant with ID tenant456 not found.", result);
        }

        [Fact]
        public void GetErrorMessage_ThrowsArgumentNullException_WhenExceptionIsNull()
        {
            // Arrange
            TenantNotFoundException exception = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => TenantNotFoundExceptionExtensions.GetErrorMessage(exception));
        }

        [Fact]
        public void AsInnerException_ReturnsInvalidOperationException_WithCorrectMessageAndInnerException()
        {
            // Arrange
            var exception = new TenantNotFoundException("tenant789");

            // Act
            Exception result = TenantNotFoundExceptionExtensions.AsInnerException(exception);

            // Assert
            Assert.IsType<InvalidOperationException>(result);
            Assert.Equal("Tenant not found", result.Message);
            Assert.Same(exception, result.InnerException);
        }

        [Fact]
        public void AsInnerException_ThrowsArgumentNullException_WhenExceptionIsNull()
        {
            // Arrange
            TenantNotFoundException exception = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => TenantNotFoundExceptionExtensions.AsInnerException(exception));
        }
    }
}