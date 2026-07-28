// tests/sqlite-multi-tenant.Tests/DataExporterJsonExtensionsTests.cs
using System;
using System.Text.Json;
using SqliteMultiTenant.DataOperations;
using Xunit;

namespace SqliteMultiTenant.Tests;

public sealed class DataExporterJsonExtensionsTests
{
    private static DataExporter CreateSampleExporter()
    {
        // The DataExporter class is part of the production code.
        // We only need a valid instance; we do not rely on any specific properties.
        // If the type has a parameterless constructor, this will succeed.
        // Otherwise, adjust this method to match the actual constructor signature.
        return Activator.CreateInstance<DataExporter>()!;
    }

    [Fact]
    public void ToJson_WithValidExporter_ReturnsJsonString()
    {
        // Arrange
        var exporter = CreateSampleExporter();

        // Act
        var json = exporter.ToJson();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        // The result should be a valid JSON that can be deserialized back.
        var roundTrip = JsonSerializer.Deserialize<DataExporter>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(roundTrip);
    }

    [Fact]
    public void ToJson_WithIndentation_ProducesIndentedJson()
    {
        // Arrange
        var exporter = CreateSampleExporter();

        // Act
        var json = exporter.ToJson(indented: true);

        // Assert
        Assert.Contains("\n", json); // indented JSON contains line breaks
    }

    [Fact]
    public void ToJson_NullExporter_ThrowsArgumentNullException()
    {
        // Arrange
        DataExporter? exporter = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exporter!.ToJson());
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsExporterInstance()
    {
        // Arrange
        var exporter = CreateSampleExporter();
        var json = exporter.ToJson();

        // Act
        var result = DataExporterJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void FromJson_EmptyOrWhiteSpace_ReturnsNull()
    {
        // Arrange
        var empty = "";
        var whitespace = "   ";

        // Act
        var resultEmpty = DataExporterJsonExtensions.FromJson(empty);
        var resultWhite = DataExporterJsonExtensions.FromJson(whitespace);

        // Assert
        Assert.Null(resultEmpty);
        Assert.Null(resultWhite);
    }

    [Fact]
    public void FromJson_NullOrEmpty_ThrowsArgumentException()
    {
        // Arrange
        string? nullString = null;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DataExporterJsonExtensions.FromJson(nullString!));
        Assert.Throws<ArgumentException>(() => DataExporterJsonExtensions.FromJson(string.Empty));
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndValue()
    {
        // Arrange
        var exporter = CreateSampleExporter();
        var json = exporter.ToJson();

        // Act
        var success = DataExporterJsonExtensions.TryFromJson(json, out var result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
    {
        // Arrange
        var invalidJson = "{ this is not valid json }";

        // Act
        var success = DataExporterJsonExtensions.TryFromJson(invalidJson, out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_NullOrEmpty_ThrowsArgumentException()
    {
        // Arrange
        string? nullString = null;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DataExporterJsonExtensions.TryFromJson(nullString!, out _));
        Assert.Throws<ArgumentException>(() => DataExporterJsonExtensions.TryFromJson(string.Empty, out _));
    }
}
