using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using SqliteMultiTenant.Services;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class TenantServiceValidationTests
    {
        private static TenantService CreateTenantService()
        {
            // TenantService likely has no public parameterless ctor.
            // Use FormatterServices to create an uninitialized instance.
            return (TenantService)FormatterServices.GetUninitializedObject(typeof(TenantService));
        }

        [Fact]
        public void Validate_WithValidTenantService_ReturnsEmpty()
        {
            // Arrange
            var service = CreateTenantService();

            // Act
            IReadOnlyList<string> problems = service.Validate();

            // Assert
            Assert.Empty(problems);
        }

        [Fact]
        public void IsValid_WithValidTenantService_ReturnsTrue()
        {
            // Arrange
            var service = CreateTenantService();

            // Act
            bool result = service.IsValid();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void EnsureValid_WithValidTenantService_DoesNotThrow()
        {
            // Arrange
            var service = CreateTenantService();

            // Act / Assert
            var exception = Record.Exception(() => service.EnsureValid());
            Assert.Null(exception);
        }

        [Fact]
        public void Validate_Null_ThrowsArgumentNullException()
        {
            // Arrange
            TenantService? service = null;

            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => service!.Validate());
        }

        [Fact]
        public void IsValid_Null_ThrowsArgumentNullException()
        {
            // Arrange
            TenantService? service = null;

            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => service!.IsValid());
        }

        [Fact]
        public void EnsureValid_Null_ThrowsArgumentNullException()
        {
            // Arrange
            TenantService? service = null;

            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => service!.EnsureValid());
        }
    }
}
