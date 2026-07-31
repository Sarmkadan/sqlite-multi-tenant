using Xunit;
using System.Text.Json;
using System.Text.Json.Serialization;
using SqliteMultiTenant.Utilities;

namespace SqliteMultiTenant.Tests
{
    public class DatabaseUtilitiesJsonExtensionsTests
    {
        [Fact]
        public void ToJson_HappyPath_ReturnsJsonString()
        {
            // Arrange
            var databaseUtilities = new DatabaseUtilities();
            var expectedJson = "{\"key\":\"value\"}";

            // Act
            var actualJson = databaseUtilities.ToJson();

            // Assert
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void ToJson_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => DatabaseUtilitiesJsonExtensions.ToJson(null));
        }

        [Fact]
        public void FromJson_HappyPath_ReturnsDatabaseUtilitiesInstance()
        {
            // Arrange
            var json = "{\"key\":\"value\"}";
            var expectedDatabaseUtilities = new DatabaseUtilities();

            // Act
            var actualDatabaseUtilities = DatabaseUtilitiesJsonExtensions.FromJson(json);

            // Assert
            Assert.Equal(expectedDatabaseUtilities, actualDatabaseUtilities);
        }

        [Fact]
        public void FromJson_NullInput_ReturnsNull()
        {
            // Act
            var actualDatabaseUtilities = DatabaseUtilitiesJsonExtensions.FromJson(null);

            // Assert
            Assert.Null(actualDatabaseUtilities);
        }

        [Fact]
        public void FromJson_EmptyJson_ReturnsNull()
        {
            // Act
            var actualDatabaseUtilities = DatabaseUtilitiesJsonExtensions.FromJson("");

            // Assert
            Assert.Null(actualDatabaseUtilities);
        }

        [Fact]
        public void TryFromJson_HappyPath_ReturnsTrue()
        {
            // Arrange
            var json = "{\"key\":\"value\"}";

            // Act
            var result = DatabaseUtilitiesJsonExtensions.TryFromJson(json, out var databaseUtilities);

            // Assert
            Assert.True(result);
            Assert.NotNull(databaseUtilities);
        }

        [Fact]
        public void TryFromJson_NullInput_ReturnsFalse()
        {
            // Act
            var result = DatabaseUtilitiesJsonExtensions.TryFromJson(null, out var databaseUtilities);

            // Assert
            Assert.False(result);
            Assert.Null(databaseUtilities);
        }

        [Fact]
        public void TryFromJson_EmptyJson_ReturnsFalse()
        {
            // Act
            var result = DatabaseUtilitiesJsonExtensions.TryFromJson("", out var databaseUtilities);

            // Assert
            Assert.False(result);
            Assert.Null(databaseUtilities);
        }
    }
}
