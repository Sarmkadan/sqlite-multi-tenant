using System;
using System.Text.Json;
using SqliteMultiTenant.Utilities;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class ReflectionExtensionsJsonExtensionsTests
    {
        [Fact]
        public void ToJson_HappyPath_ReturnsJson()
        {
            var type = typeof(object);
            var json = ReflectionExtensionsJsonExtensions.ToJson(type);
            Assert.NotNull(json);
            Assert.NotEmpty(json);
        }

        [Fact]
        public void ToJson_NullType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ReflectionExtensionsJsonExtensions.ToJson(null));
        }

        [Fact]
        public void FromJson_EmptyJson_ReturnsNull()
        {
            var json = "";
            var type = ReflectionExtensionsJsonExtensions.FromJson(json);
            Assert.Null(type);
        }

        [Fact]
        public void FromJson_ValidJson_ReturnsType()
        {
            var json = "System.String";
            var type = ReflectionExtensionsJsonExtensions.FromJson(json);
            Assert.NotNull(type);
            Assert.Equal(typeof(string), type);
        }

        [Fact]
        public void TryFromJson_EmptyJson_ReturnsFalse()
        {
            var json = "";
            var success = ReflectionExtensionsJsonExtensions.TryFromJson(json, out _);
            Assert.False(success);
        }

        [Fact]
        public void TryFromJson_ValidJson_ReturnsTrue()
        {
            var json = "System.String";
            var success = ReflectionExtensionsJsonExtensions.TryFromJson(json, out _);
            Assert.True(success);
        }

        [Fact]
        public void TryFromJson_InvalidJson_ReturnsFalse()
        {
            var json = "InvalidJson";
            var success = ReflectionExtensionsJsonExtensions.TryFromJson(json, out _);
            Assert.False(success);
        }
    }
}
