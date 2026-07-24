using System;
using SqliteMultiTenant.Models;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class TenantIntegrityCheckResultTests
    {
        [Fact]
        public void DefaultConstructor_InitializesWithDefaultValues()
        {
            // Act
            var result = new TenantIntegrityCheckResult();

            // Assert
            Assert.Equal(string.Empty, result.TenantId);
            Assert.Equal(string.Empty, result.TenantName);
            Assert.False(result.IsOk);
            Assert.Null(result.Error);
            Assert.Null(result.IntegrityOutput);
            Assert.InRange(result.CheckedAt, DateTime.UtcNow.AddSeconds(-2), DateTime.UtcNow.AddSeconds(2));
            Assert.False(result.IsSuccess);
            Assert.Equal("FAILED", result.ResultSummary);
            Assert.Contains("Integrity Check for Tenant:  ()", result.DetailedResult);
            Assert.Contains("Status: FAILED", result.DetailedResult);
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var tenantId = "tenant123";
            var tenantName = "Test Tenant";
            var isOk = true;
            var error = "Some error";
            var integrityOutput = "integrity check output";
            var checkedAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            // Act
            var result = new TenantIntegrityCheckResult
            {
                TenantId = tenantId,
                TenantName = tenantName,
                IsOk = isOk,
                Error = error,
                IntegrityOutput = integrityOutput,
                CheckedAt = checkedAt
            };

            // Assert
            Assert.Equal(tenantId, result.TenantId);
            Assert.Equal(tenantName, result.TenantName);
            Assert.Equal(isOk, result.IsOk);
            Assert.Equal(error, result.Error);
            Assert.Equal(integrityOutput, result.IntegrityOutput);
            Assert.Equal(checkedAt, result.CheckedAt);
            Assert.False(result.IsSuccess); // Because Error is not null
            Assert.Contains($"FAILED: {error}", result.ResultSummary);
            Assert.Contains($"Integrity Check for Tenant: {tenantName} ({tenantId})", result.DetailedResult);
            Assert.Contains($"Status: FAILED: {error}", result.DetailedResult);
            Assert.Contains($"Error: {error}", result.DetailedResult);
            Assert.Contains($"Checked At: {checkedAt:yyyy-MM-dd HH:mm:ss UTC}", result.DetailedResult);
        }

        [Fact]
        public void IsSuccess_ReturnsTrue_WhenIsOkTrueAndErrorNullOrEmpty()
        {
            // Arrange
            var result = new TenantIntegrityCheckResult
            {
                TenantId = "t1",
                TenantName = "Tenant",
                IsOk = true,
                Error = null,
                IntegrityOutput = "ok",
                CheckedAt = DateTime.UtcNow
            };

            // Act & Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void IsSuccess_ReturnsFalse_WhenIsOkFalse()
        {
            // Arrange
            var result = new TenantIntegrityCheckResult
            {
                TenantId = "t1",
                TenantName = "Tenant",
                IsOk = false,
                Error = null,
                IntegrityOutput = null,
                CheckedAt = DateTime.UtcNow
            };

            // Act & Assert
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void IsSuccess_ReturnsFalse_WhenErrorNotEmpty()
        {
            // Arrange
            var result = new TenantIntegrityCheckResult
            {
                TenantId = "t1",
                TenantName = "Tenant",
                IsOk = true,
                Error = "error",
                IntegrityOutput = null,
                CheckedAt = DateTime.UtcNow
            };

            // Act & Assert
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void ResultSummary_ReturnsOk_WhenIsSuccessTrue()
        {
            // Arrange
            var result = new TenantIntegrityCheckResult
            {
                TenantId = "t1",
                TenantName = "Tenant",
                IsOk = true,
                Error = null,
                IntegrityOutput = null,
                CheckedAt = DateTime.UtcNow
            };

            // Act & Assert
            Assert.Equal("OK", result.ResultSummary);
        }

        [Fact]
        public void ResultSummary_IncludesError_WhenIsOkFalseAndErrorNotNull()
        {
            // Arrange
            var result = new TenantIntegrityCheckResult
            {
                TenantId = "t1",
                TenantName = "Tenant",
                IsOk = false,
                Error = "something went wrong",
                IntegrityOutput = null,
                CheckedAt = DateTime.UtcNow
            };

            // Act & Assert
            Assert.Equal("FAILED: something went wrong", result.ResultSummary);
        }

        [Fact]
        public void ResultSummary_IncludesIntegrityOutput_WhenProvided()
        {
            // Arrange
            var result = new TenantIntegrityCheckResult
            {
                TenantId = "t1",
                TenantName = "Tenant",
                IsOk = false,
                Error = null,
                IntegrityOutput = "integrity output line1\nintegrity output line2",
                CheckedAt = DateTime.UtcNow
            };

            // Act & Assert
            Assert.Contains("integrity output line1", result.ResultSummary);
            Assert.Contains("integrity output line2", result.ResultSummary);
            Assert.Contains("Integrity output:\nintegrity output line1\nintegrity output line2", result.ResultSummary);
        }

        [Fact]
        public void DetailedResult_IncludesAllFields_WhenAllSet()
        {
            // Arrange
            var tenantId = "t1";
            var tenantName = "Tenant";
            var isOk = true;
            var error = "error";
            var integrityOutput = "output";
            var checkedAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            var result = new TenantIntegrityCheckResult
            {
                TenantId = tenantId,
                TenantName = tenantName,
                IsOk = isOk,
                Error = error,
                IntegrityOutput = integrityOutput,
                CheckedAt = checkedAt
            };

            // Act & Assert
            Assert.Contains($"Integrity Check for Tenant: {tenantName} ({tenantId})", result.DetailedResult);
            Assert.Contains($"Status: FAILED: {error}", result.DetailedResult);
            Assert.Contains($"Error: {error}", result.DetailedResult);
            Assert.Contains($"Checked At: {checkedAt:yyyy-MM-dd HH:mm:ss UTC}", result.DetailedResult);
        }
    }
}