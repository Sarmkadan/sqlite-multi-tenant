using System;
using System.Text.Json;
using Xunit;
using SqliteMultiTenant.Middleware;

namespace SqliteMultiTenant.Tests;

public sealed class CorrelationIdMiddlewareJsonExtensionsTests
{
    [Fact]
    public void ToJson_HappyPath_ReturnsJsonString()
    {
        // Arrange: create a default instance via deserialization (works even without a public ctor)
        var instance = JsonSerializer.Deserialize<CorrelationIdMiddleware>("{}");
        Assert.NotNull(instance);

        // Act
        var json = instance!.ToJson();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        // Deserializing the produced JSON should yield an equivalent instance
        var roundTrip = JsonSerializer.Deserialize<CorrelationIdMiddleware>(json);
        Assert.NotNull(roundTrip);
        // The JSON representation should be identical
        Assert.Equal(json, roundTrip!.ToJson());
    }

    [Fact]
    public void ToJson_NullValue_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((CorrelationIdMiddleware?)null)!.ToJson());
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsInstance()
    {
        // Act
        var result = CorrelationIdMiddlewareJsonExtensions.FromJson("{}");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void FromJson_Null_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CorrelationIdMiddlewareJsonExtensions.FromJson(null!));
    }

    [Fact]
    public void FromJson_EmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => CorrelationIdMiddlewareJsonExtensions.FromJson(""));
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsJsonException()
    {
        // Act & Assert
        Assert.Throws<JsonException>(() => CorrelationIdMiddlewareJsonExtensions.FromJson("{invalid}"));
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndInstance()
    {
        // Act
        var success = CorrelationIdMiddlewareJsonExtensions.TryFromJson("{}", out var value);

        // Assert
        Assert.True(success);
        Assert.NotNull(value);
    }

    [Fact]
    public void TryFromJson_Null_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CorrelationIdMiddlewareJsonExtensions.TryFromJson(null!, out _));
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalse()
    {
        // Act
        var success = CorrelationIdMiddlewareJsonExtensions.TryFromJson("{invalid}", out var value);

        // Assert
        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_EmptyString_ReturnsFalse()
    {
        // Act
        var success = CorrelationIdMiddlewareJsonExtensions.TryFromJson("", out var value);

        // Assert
        Assert.False(success);
        Assert.Null(value);
    }
}
