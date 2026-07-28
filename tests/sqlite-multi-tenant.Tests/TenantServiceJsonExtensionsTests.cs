using System;
using System.Runtime.Serialization;
using System.Text.Json;
using Xunit;
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Tests
{
    public class TenantServiceJsonExtensionsTests
    {
        private static TenantService CreateUninitializedTenantService()
        {
            return (TenantService)FormatterServices.GetUninitializedObject(typeof(TenantService));
        }

        [Fact]
        public void ToJson_HappyPath_ReturnsJsonString()
        {
            var tenant = CreateUninitializedTenantService();

            string json = tenant.ToJson();

            Assert.False(string.IsNullOrWhiteSpace(json));
            // Round‑trip to ensure the JSON can be deserialized back
            var roundTrip = JsonSerializer.Deserialize<TenantService>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            Assert.NotNull(roundTrip);
        }

        [Fact]
        public void ToJson_Null_ThrowsArgumentNullException()
        {
            TenantService? tenant = null;
            Assert.Throws<ArgumentNullException>(() => tenant!.ToJson());
        }

        [Fact]
        public void ToJson_Indented_ReturnsIndentedJson()
        {
            var tenant = CreateUninitializedTenantService();

            string json = tenant.ToJson(indented: true);

            Assert.Contains("\n", json);
        }

        [Fact]
        public void FromJson_HappyPath_ReturnsTenantService()
        {
            var tenant = CreateUninitializedTenantService();
            string json = tenant.ToJson();

            TenantService? result = TenantServiceJsonExtensions.FromJson(json);

            Assert.NotNull(result);
        }

        [Fact]
        public void FromJson_Null_ThrowsArgumentNullException()
        {
            string? json = null;
            Assert.Throws<ArgumentNullException>(() => TenantServiceJsonExtensions.FromJson(json!));
        }

        [Fact]
        public void FromJson_Empty_ReturnsNull()
        {
            string json = "";
            TenantService? result = TenantServiceJsonExtensions.FromJson(json);
            Assert.Null(result);
        }

        [Fact]
        public void FromJson_InvalidJson_ThrowsJsonException()
        {
            string json = "{ invalid json }";
            Assert.Throws<JsonException>(() => TenantServiceJsonExtensions.FromJson(json));
        }

        [Fact]
        public void TryFromJson_ValidJson_ReturnsTrueAndValue()
        {
            var tenant = CreateUninitializedTenantService();
            string json = tenant.ToJson();

            bool success = TenantServiceJsonExtensions.TryFromJson(json, out TenantService? result);

            Assert.True(success);
            Assert.NotNull(result);
        }

        [Fact]
        public void TryFromJson_Null_ThrowsArgumentNullException()
        {
            string? json = null;
            Assert.Throws<ArgumentNullException>(() => TenantServiceJsonExtensions.TryFromJson(json!, out _));
        }

        [Fact]
        public void TryFromJson_Empty_ThrowsArgumentException()
        {
            string json = "";
            Assert.Throws<ArgumentException>(() => TenantServiceJsonExtensions.TryFromJson(json, out _));
        }

        [Fact]
        public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
        {
            string json = "{ invalid json }";

            bool success = TenantServiceJsonExtensions.TryFromJson(json, out TenantService? result);

            Assert.False(success);
            Assert.Null(result);
        }
    }
}
