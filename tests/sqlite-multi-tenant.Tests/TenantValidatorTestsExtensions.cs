#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using FluentAssertions;
using SqliteMultiTenant.Api.Requests;
using SqliteMultiTenant.Validation;

namespace SqliteMultiTenant.Tests
{
    public static class TenantValidatorTestsExtensions
    {
        /// <summary>
        /// Creates a valid create tenant request for testing purposes.
        /// </summary>
        /// <param name="name">The tenant name (3-255 characters).</param>
        /// <param name="email">The contact email address.</param>
        /// <returns>A valid <see cref="CreateTenantRequest"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when name or email is invalid.</exception>
        public static CreateTenantRequest CreateValidCreateRequest(this TenantValidatorTests _, string name = "ValidTenantName", string email = "test@example.com")
        {
            ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
            ArgumentException.ThrowIfNullOrEmpty(email, nameof(email));

            if (name.Length is < 3 or > 255)
            {
                throw new ArgumentException("Tenant name must be between 3 and 255 characters", nameof(name));
            }

            return new CreateTenantRequest
            {
                Name = name,
                ContactEmail = email
            };
        }

        /// <summary>
        /// Creates a valid update tenant request for testing purposes.
        /// </summary>
        /// <param name="name">The tenant name (3-255 characters).</param>
        /// <param name="email">The contact email address.</param>
        /// <returns>A valid <see cref="UpdateTenantRequest"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when name or email is invalid.</exception>
        public static UpdateTenantRequest CreateValidUpdateRequest(this TenantValidatorTests _, string name = "UpdatedTenantName", string email = "updated@example.com")
        {
            ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
            ArgumentException.ThrowIfNullOrEmpty(email, nameof(email));

            if (name.Length is < 3 or > 255)
            {
                throw new ArgumentException("Tenant name must be between 3 and 255 characters", nameof(name));
            }

            return new UpdateTenantRequest
            {
                Name = name,
                ContactEmail = email
            };
        }

        /// <summary>
        /// Asserts that a validation result contains exactly one error for the specified property.
        /// </summary>
        /// <param name="errors">The validation errors dictionary.</param>
        /// <param name="propertyName">The name of the property that should have an error.</param>
        /// <param name="expectedErrorMessage">The expected error message.</param>
        /// <exception cref="ArgumentNullException">Thrown when errors is null.</exception>
        /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
        public static void ShouldHaveSingleErrorFor(this IReadOnlyDictionary<string, string> errors, string propertyName, string expectedErrorMessage)
        {
            ArgumentNullException.ThrowIfNull(errors);
            ArgumentException.ThrowIfNullOrEmpty(propertyName);
            ArgumentException.ThrowIfNullOrEmpty(expectedErrorMessage);

            errors.Should().ContainSingle()
                .And.ContainKey(propertyName)
                .And.ContainValue(expectedErrorMessage);
        }

        /// <summary>
        /// Asserts that a validation result is empty (no errors).
        /// </summary>
        /// <param name="errors">The validation errors dictionary.</param>
        /// <exception cref="ArgumentNullException">Thrown when errors is null.</exception>
        public static void ShouldBeEmpty(this IReadOnlyDictionary<string, string> errors)
        {
            ArgumentNullException.ThrowIfNull(errors);
            errors.Should().BeEmpty();
        }
    }
}