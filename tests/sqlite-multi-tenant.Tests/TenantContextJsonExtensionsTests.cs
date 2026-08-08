using Xunit;
using SqliteMultiTenant.Models;
using System;
using System.Text.Json;

namespace SqliteMultiTenant.Tests
{
    public class TenantContextJsonExtensionsTests
    {
        [Fact]
        public void ToJson_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            TenantContext? value = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => value!.ToJson());
        }

        [Fact]
        public void ToJson_Value_ReturnsValidJson()
        {
            // Arrange
            var context = new TenantContext
            {
                TenantId = "tenant-1",
                TenantName = "Test Tenant",
                UserId = "user-1",
                UserEmail = "user@test.com",
                EstablishedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                RequestId = "req-1",
                ConnectionId = "conn-1",
                DatabasePath = "/path/to/db.sqlite",
                IsValid = true
            };
            context.SetContextData("key1", "value1");
            context.SetContextData("key2", 42);
            context.SetContextData("key3", true);
            context.SetContextData("key4", null);

            // Act
            var json = context.ToJson();

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\"tenantId\":\"tenant-1\"", json);
            Assert.Contains("\"tenantName\":\"Test Tenant\"", json);
            Assert.Contains("\"userId\":\"user-1\"", json);
            Assert.Contains("\"userEmail\":\"user@test.com\"", json);
            Assert.Contains("\"key1\":\"value1\"", json);
            Assert.Contains("\"key2\":42", json);
            Assert.Contains("\"key3\":true", json);
            Assert.Contains("\"key4\":null", json);
        }

        [Fact]
        public void ToJson_IndentedTrue_ReturnsIndentedJson()
        {
            // Arrange
            var context = new TenantContext { TenantId = "t1" };

            // Act
            var json = context.ToJson(indented: true);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\r\n  ", json);
        }

        [Fact]
        public void FromJson_NullOrEmpty_ReturnsNull()
        {
            // Arrange
            string? json = null;

            // Act
            var result = TenantContextJsonExtensions.FromJson(json);

            // Assert
            Assert.Null(result);

            // Empty string
            result = TenantContextJsonExtensions.FromJson(string.Empty);
            Assert.Null(result);

            // Whitespace
            result = TenantContextJsonExtensions.FromJson("   ");
            Assert.Null(result);
        }

        [Fact]
        public void FromJson_ValidJson_ReturnsTenantContext()
        {
            // Arrange
            var json = @"{
                ""tenantId"": ""tenant-1"",
                ""tenantName"": ""Test Tenant"",
                ""userId"": ""user-1"",
                ""userEmail"": ""user@test.com"",
                ""establishedAt"": ""2024-01-01T00:00:00Z"",
                ""createdAt"": ""2024-01-02T00:00:00Z"",
                ""requestId"": ""req-1"",
                ""connectionId"": ""conn-1"",
                ""databasePath"": ""/path/to/db.sqlite"",
                ""isValid"": true,
                ""contextData"": {
                    ""key1"": ""value1"",
                    ""key2"": 42,
                    ""key3"": true,
                    ""key4"": null
                }
            }";

            // Act
            var result = TenantContextJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("tenant-1", result.TenantId);
            Assert.Equal("Test Tenant", result.TenantName);
            Assert.Equal("user-1", result.UserId);
            Assert.Equal("user@test.com", result.UserEmail);
            Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.EstablishedAt);
            Assert.Equal(new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc), result.CreatedAt);
            Assert.Equal("req-1", result.RequestId);
            Assert.Equal("conn-1", result.ConnectionId);
            Assert.Equal("/path/to/db.sqlite", result.DatabasePath);
            Assert.True(result.IsValid);
            Assert.NotNull(result.ContextData);
            Assert.Equal(4, result.ContextData.Count);
            Assert.Equal("value1", result.ContextData["key1"]);
            Assert.Equal(42L, result.ContextData["key2"]);
            Assert.Equal(true, result.ContextData["key3"]);
            Assert.Null(result.ContextData["key4"]);
        }

        [Fact]
        public void FromJson_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            var json = @"{ invalid json }";

            // Act & Assert
            Assert.Throws<JsonException>(() => TenantContextJsonExtensions.FromJson(json));
        }

        [Fact]
        public void TryFromJson_NullOrEmpty_ReturnsFalse()
        {
            // Arrange
            string? json = null;

            // Act
            var success = TenantContextJsonExtensions.TryFromJson(json, out TenantContext? value);

            // Assert
            Assert.False(success);
            Assert.Null(value);

            // Empty
            success = TenantContextJsonExtensions.TryFromJson(string.Empty, out value);
            Assert.False(success);
            Assert.Null(value);

            // Whitespace
            success = TenantContextJsonExtensions.TryFromJson("   ", out value);
            Assert.False(success);
            Assert.Null(value);
        }

        [Fact]
        public void TryFromJson_ValidJson_ReturnsTrueAndValue()
        {
            // Arrange
            var json = @"{""tenantId"":""t1""}";

            // Act
            var success = TenantContextJsonExtensions.TryFromJson(json, out TenantContext? value);

            // Assert
            Assert.True(success);
            Assert.NotNull(value);
            Assert.Equal("t1", value.TenantId);
        }

        [Fact]
        public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
        {
            // Arrange
            var json = @"{ invalid }";

            // Act
            var success = TenantContextJsonExtensions.TryFromJson(json, out TenantContext? value);

            // Assert
            Assert.False(success);
            Assert.Null(value);
        }
    }
}