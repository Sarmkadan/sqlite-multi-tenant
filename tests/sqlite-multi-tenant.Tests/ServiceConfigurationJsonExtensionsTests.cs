using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Tests;

namespace SqliteMultiTenant.Tests
{
    public class ServiceConfigurationJsonExtensionsTests
    {
        [Fact]
        public async Task Test_ToJson_Happy_Path()
        {
            // Arrange
            var configuration = new AppConfiguration();

            // Act
            var json = ServiceConfigurationJsonExtensions.ToJson(configuration);

            // Assert
            Assert.NotNull(json);
            Assert.False(string.IsNullOrWhiteSpace(json));
        }

        [Fact]
        public async Task Test_FromJson_Happy_Path()
        {
            // Arrange
            var json = "{}";

            // Act
            var configuration = ServiceConfigurationJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(configuration);
        }

        [Fact]
        public async Task Test_TryFromJson_Happy_Path()
        {
            // Arrange
            var json = "{}";

            // Act
            var result = ServiceConfigurationJsonExtensions.TryFromJson(json, out var configuration);

            // Assert
            Assert.True(result);
            Assert.NotNull(configuration);
        }
    }
}