#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Unit tests for LoggingExtensionsJsonExtensions
// =====================================================================

using System;
using System.Collections.Generic;
using FluentAssertions;
using SqliteMultiTenant.Logging;
using Xunit;

/// <summary>
/// Tests for LoggingExtensionsJsonExtensions - JSON serialization/deserialization extensions.
/// </summary>
public sealed class LoggingExtensionsJsonExtensionsTests
{
    /// <summary>
    /// Test data for various object types to serialize/deserialize.
    /// </summary>
    public static IEnumerable<object[]> TestData => new object[][]
    {
        new object[] { new { Name = "test", Value = 42 }, typeof(object) },
        new object[] { new Dictionary<string, int> { { "a", 1 }, { "b", 2 } }, typeof(Dictionary<string, int>) },
        new object[] { new List<string> { "item1", "item2", "item3" }, typeof(List<string>) },
        new object[] { new SimpleLogModel { Id = "log-123", Message = "Test message", Level = "Info" }, typeof(SimpleLogModel) },
        new object[] { new { Text = "hello", Number = 123, Flag = true }, typeof(object) },
    };

    /// <summary>
    /// Simple model for testing serialization/deserialization.
    /// </summary>
    private sealed class SimpleLogModel
    {
        public string? Id { get; set; }
        public string? Message { get; set; }
        public string? Level { get; set; }
    }

    /// <summary>
    /// Tests ToJson with a simple object.
    /// </summary>
    [Fact]
    public void ToJson_WithSimpleObject_ReturnsValidJson()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 42 };

        // Act
        var json = testObject.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("Test");
        json.Should().Contain("42");
    }

    /// <summary>
    /// Tests ToJson with null input throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void ToJson_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        object? nullObject = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullObject!.ToJson());
    }

    /// <summary>
    /// Tests FromJson with valid JSON.
    /// </summary>
    [Fact]
    public void FromJson_WithValidJson_ReturnsDeserializedObject()
    {
        // Arrange
        var json = "{\"id\":\"log-123\",\"message\":\"Test message\",\"level\":\"Info\"}";

        // Act
        var result = LoggingExtensionsJsonExtensions.FromJson<SimpleLogModel>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("log-123");
        result.Message.Should().Be("Test message");
        result.Level.Should().Be("Info");
    }

    /// <summary>
    /// Tests FromJson with null input throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void FromJson_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        string? nullJson = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => LoggingExtensionsJsonExtensions.FromJson<SimpleLogModel>(nullJson!));
    }

    /// <summary>
    /// Tests FromJson with empty string throws ArgumentException.
    /// </summary>
    [Fact]
    public void FromJson_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var emptyJson = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => LoggingExtensionsJsonExtensions.FromJson<SimpleLogModel>(emptyJson));
    }

    /// <summary>
    /// Tests FromJson with invalid JSON throws JsonException.
    /// </summary>
    [Fact]
    public void FromJson_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "{invalid json}";

        // Act & Assert
        Assert.Throws<global::System.Text.Json.JsonException>(() => LoggingExtensionsJsonExtensions.FromJson<SimpleLogModel>(invalidJson));
    }

    /// <summary>
    /// Tests TryFromJson with valid JSON returns true and deserializes correctly.
    /// </summary>
    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndDeserializes()
    {
        // Arrange
        var json = "{\"id\":\"log-456\",\"message\":\"Another test\",\"level\":\"Warning\"}";

        // Act
        var result = LoggingExtensionsJsonExtensions.TryFromJson<SimpleLogModel>(json, out var deserialized);

        // Assert
        result.Should().BeTrue();
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be("log-456");
        deserialized.Message.Should().Be("Another test");
        deserialized.Level.Should().Be("Warning");
    }

    /// <summary>
    /// Tests TryFromJson with invalid JSON returns false and sets value to null.
    /// </summary>
    [Fact]
    public void TryFromJson_WithInvalidJson_ReturnsFalseAndSetsValueToNull()
    {
        // Arrange
        var invalidJson = "{invalid json}";

        SimpleLogModel? value = null;

        // Act
        var result = LoggingExtensionsJsonExtensions.TryFromJson<SimpleLogModel>(invalidJson, out value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeNull();
    }

    /// <summary>
    /// Tests TryFromJson with null input throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void TryFromJson_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        string? nullJson = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => LoggingExtensionsJsonExtensions.TryFromJson<object>(nullJson!, out _));
    }

    /// <summary>
    /// Tests TryFromJson with empty string throws ArgumentException.
    /// </summary>
    [Fact]
    public void TryFromJson_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var emptyJson = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => LoggingExtensionsJsonExtensions.TryFromJson<object>(emptyJson, out _));
    }

    /// <summary>
    /// Tests round-trip serialization and deserialization preserves data.
    /// </summary>
    [Fact]
    public void RoundTrip_ToJsonAndFromJson_PreservesData()
    {
        // Arrange
        var original = new SimpleLogModel { Id = "log-789", Message = "Round trip test", Level = "Error" };

        // Act
        var json = original.ToJson();
        var deserialized = LoggingExtensionsJsonExtensions.FromJson<SimpleLogModel>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(original.Id);
        deserialized.Message.Should().Be(original.Message);
        deserialized.Level.Should().Be(original.Level);
    }

    /// <summary>
    /// Tests TryFromJson with complex object returns true.
    /// </summary>
    [Fact]
    public void TryFromJson_WithComplexObject_ReturnsTrue()
    {
        // Arrange
        var json = "{\"user\":{\"id\":\"user-2\",\"name\":\"Jane Doe\"},\"action\":\"logout\",\"count\":5}";

        // Act
        var result = LoggingExtensionsJsonExtensions.TryFromJson<SimpleLogModel>(json, out var deserialized);

        // Assert
        result.Should().BeTrue();
        deserialized.Should().NotBeNull();
    }

    /// <summary>
    /// Tests ToJson with empty collection.
    /// </summary>
    [Fact]
    public void ToJson_WithEmptyCollection_ReturnsValidJson()
    {
        // Arrange
        var emptyList = new List<string>();

        // Act
        var json = emptyList.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Be("[]");
    }

    /// <summary>
    /// Tests FromJson with empty object returns default instance (not null).
    /// </summary>
    [Fact]
    public void FromJson_WithEmptyObject_ReturnsDefaultInstance()
    {
        // Arrange
        var emptyJson = "{}";

        // Act
        var result = LoggingExtensionsJsonExtensions.FromJson<SimpleLogModel>(emptyJson);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().BeNull();
        result.Message.Should().BeNull();
        result.Level.Should().BeNull();
    }

    /// <summary>
    /// Tests TryFromJson with empty object returns true with default instance.
    /// </summary>
    [Fact]
    public void TryFromJson_WithEmptyObject_ReturnsTrueWithDefaultInstance()
    {
        // Arrange
        var emptyJson = "{}";

        // Act
        var result = LoggingExtensionsJsonExtensions.TryFromJson<SimpleLogModel>(emptyJson, out var deserialized);

        // Assert
        result.Should().BeTrue();
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().BeNull();
        deserialized.Message.Should().BeNull();
        deserialized.Level.Should().BeNull();
    }

    /// <summary>
    /// Tests ToJson with indented parameter set to true returns pretty-printed JSON.
    /// </summary>
    [Fact]
    public void ToJson_WithIndentedTrue_ReturnsPrettyPrintedJson()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 42 };

        // Act
        var json = testObject.ToJson(indented: true);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("Test");
        json.Should().Contain("42");
        json.Should().Contain("\n"); // Pretty-printed JSON contains newlines
    }

    /// <summary>
    /// Tests ToJson with indented parameter set to false returns compact JSON.
    /// </summary>
    [Fact]
    public void ToJson_WithIndentedFalse_ReturnsCompactJson()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 42 };

        // Act
        var json = testObject.ToJson(indented: false);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("Test");
        json.Should().Contain("42");
        json.Should().NotContain("\n"); // Compact JSON should not contain newlines
    }

}