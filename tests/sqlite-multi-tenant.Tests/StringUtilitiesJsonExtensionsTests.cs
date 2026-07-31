using System;
using System.Text.Json;
using Xunit;
using SqliteMultiTenant.Utilities;

namespace SqliteMultiTenant.Tests;

public class StringUtilitiesJsonExtensionsTests
{
    [Fact]
    public void ToJson_HappyPath_ReturnsJsonString()
    {
        string input = "hello";
        string json = StringUtilitiesJsonExtensions.ToJson(input);
        Assert.Equal("\"hello\"", json);
    }

    [Fact]
    public void ToJson_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => StringUtilitiesJsonExtensions.ToJson(null!));
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsString()
    {
        string json = "\"world\"";
        string? result = StringUtilitiesJsonExtensions.FromJson(json);
        Assert.Equal("world", result);
    }

    [Fact]
    public void FromJson_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => StringUtilitiesJsonExtensions.FromJson(string.Empty));
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsJsonException()
    {
        string badJson = "notjson";
        Assert.Throws<JsonException>(() => StringUtilitiesJsonExtensions.FromJson(badJson));
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndValue()
    {
        string json = "\"test\"";
        bool success = StringUtilitiesJsonExtensions.TryFromJson(json, out var value);
        Assert.True(success);
        Assert.Equal("test", value);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalse()
    {
        string json = "invalid";
        bool success = StringUtilitiesJsonExtensions.TryFromJson(json, out var value);
        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void ToJsonWithHash_ReturnsJsonWithValueAndHash()
    {
        string input = "abc";
        string json = StringUtilitiesJsonExtensions.ToJsonWithHash(input);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(input, root.GetProperty("Value").GetString());

        string? hash = root.GetProperty("Hash").GetString();
        Assert.NotNull(hash);
        // SHA256 hash in hex is 64 characters
        Assert.Equal(64, hash!.Length);
    }

    [Fact]
    public void ToJsonWithSnakeCase_ReturnsJsonWithOriginalAndSnakeCase()
    {
        string input = "HelloWorldTest";
        string json = StringUtilitiesJsonExtensions.ToJsonWithSnakeCase(input);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(input, root.GetProperty("Original").GetString());

        string? snake = root.GetProperty("SnakeCase").GetString();
        Assert.NotNull(snake);
        // Basic sanity: snake case should contain underscores and be lower‑case
        Assert.Contains("_", snake!);
        Assert.Equal(snake, snake.ToLowerInvariant());
    }
}
