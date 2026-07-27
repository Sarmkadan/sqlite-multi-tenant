using Xunit;
using SqliteMultiTenant.Models;
using System;

namespace SqliteMultiTenant.Tests
{
    public class TenantContextTests
    {
        [Fact]
        public void Constructor_InitializesWithDefaultValues()
        {
            var context = new TenantContext();

            Assert.Equal(string.Empty, context.TenantId);
            Assert.Null(context.TenantName);
            Assert.Null(context.UserId);
            Assert.Null(context.UserEmail);
            Assert.Null(context.RequestId);
            Assert.Null(context.ConnectionId);
            Assert.Null(context.DatabasePath);
            Assert.Null(context.ContextData);
            Assert.NotNull(context.AllowedTenants);
            Assert.Empty(context.AllowedTenants);
            Assert.True(context.IsValid);
            Assert.True(context.EstablishedAt <= DateTime.UtcNow);
            Assert.True(context.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Validate_WithValidTenantId_ReturnsTrueAndNullError()
        {
            var context = new TenantContext { TenantId = "valid-tenant" };

            var isValid = context.Validate(out string? errorMessage);

            Assert.True(isValid);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void Validate_WithEmptyTenantId_ReturnsFalseAndErrorMessage()
        {
            var context = new TenantContext { TenantId = string.Empty };

            var isValid = context.Validate(out string? errorMessage);

            Assert.False(isValid);
            Assert.Equal("TenantId is required", errorMessage);
        }

        [Fact]
        public void Validate_WithWhitespaceTenantId_ReturnsFalseAndErrorMessage()
        {
            var context = new TenantContext { TenantId = "   " };

            var isValid = context.Validate(out string? errorMessage);

            Assert.False(isValid);
            Assert.Equal("TenantId is required", errorMessage);
        }

        [Fact]
        public void Validate_WhenContextMarkedInvalid_ReturnsFalseAndErrorMessage()
        {
            var context = new TenantContext { TenantId = "valid-tenant", IsValid = false };

            var isValid = context.Validate(out string? errorMessage);

            Assert.False(isValid);
            Assert.Equal("Context is marked as invalid", errorMessage);
        }

        [Fact]
        public void SetContextData_ThenGetContextData_ReturnsCorrectValue()
        {
            var context = new TenantContext();
            const string key = "session-id";
            const string value = "abc-123";

            context.SetContextData(key, value);
            var result = context.GetContextData(key);

            Assert.Equal(value, result);
            Assert.NotNull(context.ContextData);
        }

        [Fact]
        public void GetContextData_WhenKeyDoesNotExist_ReturnsNull()
        {
            var context = new TenantContext();

            var result = context.GetContextData("missing-key");

            Assert.Null(result);
        }

        [Fact]
        public void Invalidate_SetsIsValidToFalse()
        {
            var context = new TenantContext();

            context.Invalidate();

            Assert.False(context.IsValid);
        }

        [Fact]
        public void ToString_ReturnsFormattedStringWithProperties()
        {
            var context = new TenantContext
            {
                TenantId = "tenant-1",
                UserEmail = "admin@test.com",
                EstablishedAt = new DateTime(2024, 5, 20, 10, 30, 0, DateTimeKind.Utc)
            };

            var result = context.ToString();

            Assert.Contains("TenantId=tenant-1", result);
            Assert.Contains("User=admin@test.com", result);
            Assert.Contains("Established=2024-05-20T10:30:00.0000000Z", result);
        }
    }
}
