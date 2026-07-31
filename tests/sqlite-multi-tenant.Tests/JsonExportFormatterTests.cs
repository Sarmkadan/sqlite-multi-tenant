using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Formatters;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class JsonExportFormatterTests
{
    private readonly JsonExportFormatter _formatter;

    public JsonExportFormatterTests()
    {
        // Use a NullLogger to avoid needing a real logging implementation.
        _formatter = new JsonExportFormatter(NullLogger<JsonExportFormatter>.Instance);
    }

    private class SimpleDto
    {
        public string Name { get; set; } = "Alice";
        public int Age { get; set; } = 30;
    }

    private class FaultyDto
    {
        public string GoodProp => "ok";

        // This getter throws – used to test the error‑handling path.
        public string BadProp => throw new InvalidOperationException("boom");
    }

    [Fact]
    public void Format_Object_ReturnsExpectedJson()
    {
        var dto = new SimpleDto();
        string json = _formatter.Format(dto);

        // The formatter includes the JSON object; we only assert the core structure.
        Assert.Contains("\"name\":\"Alice\"", json);
        Assert.Contains("\"age\":30", json);
    }

    [Fact]
    public void Format_Collection_ReturnsExpectedJson()
    {
        var list = new List<SimpleDto>
        {
            new SimpleDto { Name = "Bob", Age = 25 },
            new SimpleDto { Name = "Carol", Age = 40 }
        };

        string json = _formatter.Format(list);

        Assert.Contains("[", json);
        Assert.Contains("{\"name\":\"Bob\",\"age\":25}", json);
        Assert.Contains("{\"name\":\"Carol\",\"age\":40}", json);
        Assert.Contains("]", json);
    }

    [Fact]
    public void Format_NullInput_ReturnsNull()
    {
        string json = _formatter.Format<object>(null);

        Assert.Equal("null", json);
    }

    [Fact]
    public void Format_EmptyCollection_ReturnsEmptyArray()
    {
        var emptyList = new List<SimpleDto>();
        string json = _formatter.Format(emptyList);

        Assert.Equal("[]", json);
    }

    [Fact]
    public void Format_ObjectWithThrowingProperty_ReturnsErrorElement()
    {
        var faulty = new FaultyDto();

        string json = _formatter.Format(faulty);

        // The formatter catches the exception and returns an error element.
        Assert.StartsWith("{\"error\":\"boom\"}", json);
    }

    [Fact]
    public void Parse_NullInput_ReturnsNull()
    {
        string json = null;
        object? result = _formatter.Parse<object>(json);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsNull()
    {
        string json = "";
        object? result = _formatter.Parse<object>(json);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsFormatException()
    {
        string json = "invalid json";
        Assert.Throws<FormatException>(() => _formatter.Parse<object>(json));
    }

    [Fact]
    public void GetMinimalOptions_ReturnsMinimalOptions()
    {
        JsonSerializerOptions options = JsonExportFormatter.GetMinimalOptions();

        Assert.Equal(false, options.WriteIndented);
        Assert.Equal(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
        Assert.Equal(JsonIgnoreCondition.WhenWritingNull, options.DefaultIgnoreCondition);
    }

    [Fact]
    public void GetVerboseOptions_ReturnsVerboseOptions()
    {
        JsonSerializerOptions options = JsonExportFormatter.GetVerboseOptions();

        Assert.Equal(true, options.WriteIndented);
        Assert.Equal(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
        Assert.Equal(JsonIgnoreCondition.Never, options.DefaultIgnoreCondition);
    }
}
