#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;
using FluentAssertions;
using SqliteMultiTenant.Api.Requests;
using SqliteMultiTenant.Validation;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public sealed class TenantValidatorTests {
        private readonly TenantValidator _validator;

        public TenantValidatorTests()
        {
            _validator = new TenantValidator();
        }

        [Fact]
        public void ValidateCreateRequest_ShouldReturnNoErrors_WithValidRequest()
        {
            // Arrange
            var request = new CreateTenantRequest
            {
                Name = "ValidTenantName",
                ContactEmail = "test@example.com"
            };

            // Act
            var errors = _validator.ValidateCreateRequest(request);

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateCreateRequest_ShouldReturnError_WhenNameIsEmpty()
        {
            // Arrange
            var request = new CreateTenantRequest
            {
                Name = "",
                ContactEmail = "test@example.com"
            };

            // Act
            var errors = _validator.ValidateCreateRequest(request);

            // Assert
            errors.Should().ContainSingle()
                  .And.ContainKey(nameof(request.Name))
                  .And.ContainValue("Tenant name is required");
        }

        [Fact]
        public void ValidateCreateRequest_ShouldReturnError_WhenNameIsTooShort()
        {
            // Arrange
            var request = new CreateTenantRequest
            {
                Name = "ab", // Less than 3 characters
                ContactEmail = "test@example.com"
            };

            // Act
            var errors = _validator.ValidateCreateRequest(request);

            // Assert
            errors.Should().ContainSingle()
                  .And.ContainKey(nameof(request.Name))
                  .And.ContainValue("Tenant name must be between 3 and 255 characters");
        }

        [Fact]
        public void ValidateCreateRequest_ShouldReturnError_WhenNameIsTooLong()
        {
            // Arrange
            var request = new CreateTenantRequest
            {
                Name = new string('a', 256), // More than 255 characters
                ContactEmail = "test@example.com"
            };

            // Act
            var errors = _validator.ValidateCreateRequest(request);

            // Assert
            errors.Should().ContainSingle()
                  .And.ContainKey(nameof(request.Name))
                  .And.ContainValue("Tenant name must be between 3 and 255 characters");
        }

        [Fact]
        public void ValidateCreateRequest_ShouldReturnError_WhenContactEmailIsEmpty()
        {
            // Arrange
            var request = new CreateTenantRequest
            {
                Name = "ValidTenant",
                ContactEmail = ""
            };

            // Act
            var errors = _validator.ValidateCreateRequest(request);

            // Assert
            errors.Should().ContainSingle()
                  .And.ContainKey(nameof(request.ContactEmail))
                  .And.ContainValue("Contact email is required");
        }

        [Fact]
        public void ValidateCreateRequest_ShouldReturnError_WhenContactEmailIsInvalid()
        {
            // Arrange
            var request = new CreateTenantRequest
            {
                Name = "ValidTenant",
                ContactEmail = "invalid-email"
            };

            // Act
            var errors = _validator.ValidateCreateRequest(request);

            // Assert
            errors.Should().ContainSingle()
                  .And.ContainKey(nameof(request.ContactEmail))
                  .And.ContainValue("Contact email must be valid");
        }

        [Fact]
        public void ValidateUpdateRequest_ShouldReturnNoErrors_WithValidRequest()
        {
            // Arrange
            var request = new UpdateTenantRequest
            {
                Name = "UpdatedTenantName",
                ContactEmail = "updated@example.com"
            };

            // Act
            var errors = _validator.ValidateUpdateRequest(request);

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateUpdateRequest_ShouldReturnNoErrors_WhenOnlyOneFieldIsProvidedAndValid()
        {
            // Arrange
            var request1 = new UpdateTenantRequest { Name = "NewName" };
            var request2 = new UpdateTenantRequest { ContactEmail = "new@example.com" };

            // Act
            var errors1 = _validator.ValidateUpdateRequest(request1);
            var errors2 = _validator.ValidateUpdateRequest(request2);

            // Assert
            errors1.Should().BeEmpty();
            errors2.Should().BeEmpty();
        }

        [Fact]
        public void ValidateUpdateRequest_ShouldReturnError_WhenNameIsTooShort()
        {
            // Arrange
            var request = new UpdateTenantRequest
            {
                Name = "ab"
            };

            // Act
            var errors = _validator.ValidateUpdateRequest(request);

            // Assert
            errors.Should().ContainSingle()
                  .And.ContainKey(nameof(request.Name))
                  .And.ContainValue("Tenant name must be between 3 and 255 characters");
        }

        [Fact]
        public void ValidateUpdateRequest_ShouldReturnError_WhenContactEmailIsInvalid()
        {
            // Arrange
            var request = new UpdateTenantRequest
            {
                ContactEmail = "invalid-update-email"
            };

            // Act
            var errors = _validator.ValidateUpdateRequest(request);

            // Assert
            errors.Should().ContainSingle()
                  .And.ContainKey(nameof(request.ContactEmail))
                  .And.ContainValue("Contact email must be valid");
        }
    }
}
