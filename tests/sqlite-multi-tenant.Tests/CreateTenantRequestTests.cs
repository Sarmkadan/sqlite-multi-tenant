using System;
using SqliteMultiTenant.Api.Requests;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class CreateTenantRequestTests
    {
        [Fact]
        public void DefaultConstructor_InitializesPropertiesToEmptyStrings()
        {
            // Arrange & Act
            var request = new CreateTenantRequest();

            // Assert
            Assert.Equal(string.Empty, request.Name);
            Assert.Equal(string.Empty, request.Description);
            Assert.Equal(string.Empty, request.ContactEmail);
        }

        [Fact]
        public void PropertySetters_AssignValues_ReturnsSameValues()
        {
            // Arrange
            var request = new CreateTenantRequest
            {
                Name = "Acme Corp",
                Description = "A sample tenant",
                ContactEmail = "admin@acme.example.com"
            };

            // Assert
            Assert.Equal("Acme Corp", request.Name);
            Assert.Equal("A sample tenant", request.Description);
            Assert.Equal("admin@acme.example.com", request.ContactEmail);
        }

        [Fact]
        public void PropertySetters_AssignNullValues_AllowsNull()
        {
            // Suppress nullable warnings for the purpose of this test
#pragma warning disable CS8625 // Cannot convert null literal to non‑nullable reference type.
            var request = new CreateTenantRequest
            {
                Name = null,
                Description = null,
                ContactEmail = null
            };
#pragma warning restore CS8625

            // Assert
            Assert.Null(request.Name);
            Assert.Null(request.Description);
            Assert.Null(request.ContactEmail);
        }

        [Fact]
        public void PropertySetters_AssignVeryLongString_DoesNotThrowAndStoresValue()
        {
            // Arrange
            var longString = new string('x', 10_000);
            var request = new CreateTenantRequest();

            // Act
            var exception = Record.Exception(() =>
            {
                request.Name = longString;
                request.Description = longString;
                request.ContactEmail = longString;
            });

            // Assert
            Assert.Null(exception);
            Assert.Equal(longString, request.Name);
            Assert.Equal(longString, request.Description);
            Assert.Equal(longString, request.ContactEmail);
        }
    }
}
