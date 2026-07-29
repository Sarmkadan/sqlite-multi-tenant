using System;
using System.Runtime.Serialization;
using System.Text.Json;
using Xunit;
using SqliteMultiTenant.Tenants;

namespace SqliteMultiTenant.Tests
{
    public class TenantRecoveryServiceJsonExtensionsTests
    {
        private static TenantRecoveryService CreateUninitializedTenantRecoveryService()
        {
            return (TenantRecoveryService)FormatterServices.GetUninitializedObject(typeof(TenantRecoveryService));
        }

        [Fact]
        public void ToJson_HappyPath_ReturnsJsonString()
        {
            var service = CreateUninitializedTenantRecoveryService();

            string json = service.ToJson();

            Assert.False(string.IsNullOrWhiteSpace(json));
        }

        [Fact]
        public void ToJson_Null_ThrowsArgumentNullException()
        {
            TenantRecoveryService? service = null;
            Assert.Throws<ArgumentNullException>(() => service!.ToJson());
        }

        [Fact]
        public void ToJson_Indented_ReturnsIndentedJson()
        {
            var service = CreateUninitializedTenantRecoveryService();

            string json = service.ToJson(indented: true);

            Assert.Contains("\n", json);
        }

        [Fact]
        public void FromJson_HappyPath_ReturnsTenantRecoveryService()
        {
            var service = CreateUninitializedTenantRecoveryService();
            string json = service.ToJson();

            TenantRecoveryService? result = TenantRecoveryServiceJsonExtensions.FromJson(json);

            Assert.NotNull(result);
        }

        [Fact]
        public void FromJson_NullOrEmpty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => TenantRecoveryServiceJsonExtensions.FromJson(null!));
            Assert.Throws<ArgumentException>(() => TenantRecoveryServiceJsonExtensions.FromJson(string.Empty));
        }

        [Fact]
        public void FromJson_InvalidJson_ReturnsNull()
        {
            string json = "{ invalid json }";
            TenantRecoveryService? result = TenantRecoveryServiceJsonExtensions.FromJson(json);

            Assert.Null(result);
        }

        [Fact]
        public void TryFromJson_ValidJson_ReturnsTrueAndValue()
        {
            var service = CreateUninitializedTenantRecoveryService();
            string json = service.ToJson();

            bool success = TenantRecoveryServiceJsonExtensions.TryFromJson(json, out TenantRecoveryService? result);

            Assert.True(success);
            Assert.NotNull(result);
        }

        [Fact]
        public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
        {
            string json = "{ invalid json }";

            bool success = TenantRecoveryServiceJsonExtensions.TryFromJson(json, out TenantRecoveryService? result);

            Assert.False(success);
            Assert.Null(result);
        }
    }
}
