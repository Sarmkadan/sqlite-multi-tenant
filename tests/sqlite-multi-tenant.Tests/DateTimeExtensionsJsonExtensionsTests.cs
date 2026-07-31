using System;
using System.Text.Json;
using Xunit;
using SqliteMultiTenant.Utilities;

namespace SqliteMultiTenant.Tests;

public sealed class DateTimeExtensionsJsonExtensionsTests
{
    [Fact]
    public void ToJson_SerializesUtcDateTime_WithDefaultFormatting()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;

        // Act
        var json = utcNow.ToJson();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        // JsonSerializer serializes DateTime as ISO 8601 with 'Z' for UTC
        Assert.Contains("Z", json);
        // Ensure no indentation by default
        Assert.DoesNotContain("\n", json);
    }

    [Fact]
    public void ToJson_SerializesUtcDateTime_WithIndentation()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;

        // Act
        var json = utcNow.ToJson(indented: true);

        // Assert
        Assert.Contains("\n", json);
    }

    [Fact]
    public void FromJson_DeserializesValidJson_ReturnsSameDateTime()
    {
        // Arrange
        var original = DateTime.UtcNow;
        var json = original.ToJson();

        // Act
        var result = DateTimeExtensionsJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(original, result.Value);
    }

    [Fact]
    public void FromJson_ThrowsArgumentException_WhenJsonIsNullOrWhiteSpace()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => DateTimeExtensionsJsonExtensions.FromJson(null!));
        Assert.Throws<ArgumentException>(() => DateTimeExtensionsJsonExtensions.FromJson(""));
        Assert.Throws<ArgumentException>(() => DateTimeExtensionsJsonExtensions.FromJson("   "));
    }

    [Fact]
    public void FromJson_ThrowsJsonException_WhenJsonIsInvalid()
    {
        // Arrange
        var invalidJson = "\"not-a-datetime\"";

        // Act & Assert
        Assert.Throws<JsonException>(() => DateTimeExtensionsJsonExtensions.FromJson(invalidJson));
    }

    [Fact]
    public void TryFromJson_ReturnsTrue_WhenJsonIsValid()
    {
        // Arrange
        var original = DateTime.UtcNow;
        var json = original.ToJson();

        // Act
        var success = DateTimeExtensionsJsonExtensions.TryFromJson(json, out var result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(original, result.Value);
    }

    [Fact]
    public void TryFromJson_ReturnsFalse_WhenJsonIsInvalid()
    {
        // Arrange
        var invalidJson = "\"invalid\"";

        // Act
        var success = DateTimeExtensionsJsonExtensions.TryFromJson(invalidJson, out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_ThrowsArgumentException_WhenJsonIsNullOrWhiteSpace()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => DateTimeExtensionsJsonExtensions.TryFromJson(null!, out _));
        Assert.Throws<ArgumentException>(() => DateTimeExtensionsJsonExtensions.TryFromJson("", out _));
        Assert.Throws<ArgumentException>(() => DateTimeExtensionsJsonExtensions.TryFromJson("   ", out _));
    }
}
