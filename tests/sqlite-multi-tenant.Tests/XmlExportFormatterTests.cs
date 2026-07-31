using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Formatters;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class XmlExportFormatterTests
{
    private readonly XmlExportFormatter _formatter;

    public XmlExportFormatterTests()
    {
        // Use a NullLogger to avoid needing a real logging implementation.
        _formatter = new XmlExportFormatter(NullLogger<XmlExportFormatter>.Instance);
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
    public void Format_Object_ReturnsExpectedXml()
    {
        var dto = new SimpleDto();
        string xml = _formatter.Format(dto, "person");

        // The formatter includes the XML declaration; we only assert the core structure.
        Assert.Contains("<person>", xml);
        Assert.Contains("<name>Alice</name>", xml);
        Assert.Contains("<age>30</age>", xml);
        Assert.Contains("</person>", xml);
    }

    [Fact]
    public void Format_Collection_ReturnsExpectedXml()
    {
        var list = new List<SimpleDto>
        {
            new SimpleDto { Name = "Bob", Age = 25 },
            new SimpleDto { Name = "Carol", Age = 40 }
        };

        string xml = _formatter.Format(list, "people");

        Assert.Contains("<people>", xml);
        Assert.Contains("<item>", xml); // each element is wrapped in <item>
        Assert.Contains("<name>Bob</name>", xml);
        Assert.Contains("<age>25</age>", xml);
        Assert.Contains("<name>Carol</name>", xml);
        Assert.Contains("<age>40</age>", xml);
        Assert.Contains("</people>", xml);
    }

    [Fact]
    public void Format_NullInput_ReturnsRootElementOnly()
    {
        string xml = _formatter.Format<object>(null, "empty");

        // Should be a root element with no children.
        Assert.Contains("<empty />", xml);
    }

    [Fact]
    public void Format_EmptyCollection_ReturnsRootElementOnly()
    {
        var emptyList = new List<SimpleDto>();
        string xml = _formatter.Format(emptyList, "emptyList");

        // No <item> elements should be present.
        Assert.Contains("<emptyList />", xml);
        Assert.DoesNotContain("<item>", xml);
    }

    [Fact]
    public void Format_ObjectWithThrowingProperty_ReturnsErrorElement()
    {
        var faulty = new FaultyDto();

        string xml = _formatter.Format(faulty, "faulty");

        // The formatter catches the exception and returns an <error> element.
        Assert.StartsWith("<error>", xml);
        Assert.Contains("boom", xml); // message should be escaped but still present
        Assert.EndsWith("</error>", xml);
    }
}
