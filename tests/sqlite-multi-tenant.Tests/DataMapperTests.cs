using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Utilities;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class DataMapperTests
{
    private readonly ILogger<DataMapper> _logger;
    private readonly DataMapper _mapper;

    public DataMapperTests()
    {
        _logger = Substitute.For<ILogger<DataMapper>>();
        _mapper = new DataMapper(_logger);
    }

    [Fact]
    public void Map_SimplePropertyMapping_ReturnsMappedObject()
    {
        _logger.LogInformation("Starting test {TestName}", nameof(Map_SimplePropertyMapping_ReturnsMappedObject));
        // Arrange
        var source = new SimpleSource
        {
            Id = 1,
            Name = "Test Name",
            Value = 42.5
        };

        // Act
        var result = _mapper.Map<SimpleSource, SimpleTarget>(source);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test Name");
        result.Value.Should().Be(42.5);
        _logger.LogInformation("Completed test {TestName}", nameof(Map_SimplePropertyMapping_ReturnsMappedObject));
        _logger.LogInformation("Completed test Map_SimplePropertyMapping_ReturnsMappedObject", nameof(Map_SimplePropertyMapping_ReturnsMappedObject));    }

    [Fact]
    public void Map_NullSource_ReturnsNewInstance()
    {
        // Arrange & Act
        var result = _mapper.Map<SimpleSource, SimpleTarget>(null!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SimpleTarget>();
        _logger.LogInformation("Completed test Map_NullSource_ReturnsNewInstance", nameof(Map_NullSource_ReturnsNewInstance));    }

    [Fact]
    public void Map_MissingColumns_SkipsNonExistentProperties()
    {
        // Arrange
        var source = new SimpleSource { Id = 1, Name = "Test" };

        // Act
        var result = _mapper.Map<SimpleSource, TargetWithExtraProperties>(source);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test");
        // Extra properties should have default values
        result.ExtraProperty.Should().Be(0);
        result.AnotherExtra.Should().BeNull();
        _logger.LogInformation("Completed test Map_MissingColumns_SkipsNonExistentProperties", nameof(Map_MissingColumns_SkipsNonExistentProperties));    }

    [Fact]
    public void Map_NullValues_HandlesNullsGracefully()
    {
        // Arrange
        var source = new SimpleSource
        {
            Id = 1,
            Name = null,
            Value = null
        };

        // Act
        var result = _mapper.Map<SimpleSource, SimpleTarget>(source);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().BeNull();
        result.Value.Should().Be(0); // Default for double
        _logger.LogInformation("Completed test Map_NullValues_HandlesNullsGracefully", nameof(Map_NullValues_HandlesNullsGracefully));    }

    [Fact]
    public void Map_BoolPropertyMapping_WorksCorrectly()
    {
        // Arrange
        var source = new BoolSource { BoolValue = true };

        // Act
        var result = _mapper.Map<BoolSource, BoolTarget>(source);

        // Assert
        result.Should().NotBeNull();
        result.BoolValue.Should().BeTrue();
        _logger.LogInformation("Completed test Map_BoolPropertyMapping_WorksCorrectly", nameof(Map_BoolPropertyMapping_WorksCorrectly));    }


    [Fact]
    public void Map_ListMapping_MapsAllItemsInList()
    {
        // Arrange
        var sources = new List<SimpleSource>
        {
            new() { Id = 1, Name = "First", Value = 10.5 },
            new() { Id = 2, Name = "Second", Value = 20.5 },
            new() { Id = 3, Name = "Third", Value = 30.5 }
        };

        // Act
        var results = _mapper.MapList<SimpleSource, SimpleTarget>(sources);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(3);

        results[0].Id.Should().Be(1);
        results[0].Name.Should().Be("First");
        results[0].Value.Should().Be(10.5);

        results[1].Id.Should().Be(2);
        results[1].Name.Should().Be("Second");
        results[1].Value.Should().Be(20.5);

        results[2].Id.Should().Be(3);
        results[2].Name.Should().Be("Third");
        results[2].Value.Should().Be(30.5);
        _logger.LogInformation("Completed test Map_ListMapping_MapsAllItemsInList", nameof(Map_ListMapping_MapsAllItemsInList));    }

    [Fact]
    public void Map_ListMapping_WithNullList_ReturnsEmptyList()
    {
        // Arrange & Act
        var result = _mapper.MapList<SimpleSource, SimpleTarget>(null!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _logger.LogInformation("Completed test Map_ListMapping_WithNullList_ReturnsEmptyList", nameof(Map_ListMapping_WithNullList_ReturnsEmptyList));    }

    [Fact]
    public void Map_ListMapping_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var sources = new List<SimpleSource>();

        // Act
        var result = _mapper.MapList<SimpleSource, SimpleTarget>(sources);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _logger.LogInformation("Completed test Map_ListMapping_WithEmptyList_ReturnsEmptyList", nameof(Map_ListMapping_WithEmptyList_ReturnsEmptyList));    }

    [Fact]
    public void Map_CaseInsensitivePropertyMatching_MapsCorrectly()
    {
        // Arrange
        var source = new CaseInsensitiveSource { id = 42, NAME = "Test", VaLuE = 99.9 };

        // Act
        var result = _mapper.Map<CaseInsensitiveSource, CaseInsensitiveTarget>(source);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(42);
        result.Name.Should().Be("Test");
        result.Value.Should().Be(99.9);
        _logger.LogInformation("Completed test Map_CaseInsensitivePropertyMatching_MapsCorrectly", nameof(Map_CaseInsensitivePropertyMatching_MapsCorrectly));    }

    [Fact]
    public void Map_ReadOnlyProperty_SkipsProperty()
    {
        // Arrange
        var source = new SourceWithReadOnly { Id = 1, Name = "Test" };

        // Act
        var result = _mapper.Map<SourceWithReadOnly, TargetWithReadOnly>(source);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test");
        _logger.LogInformation("Completed test Map_ReadOnlyProperty_SkipsProperty", nameof(Map_ReadOnlyProperty_SkipsProperty));    }


    // Test classes for mapping scenarios

    private class SimpleSource
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public double? Value { get; set; }
    }

    private class SimpleTarget
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public double Value { get; set; }
    }

    private class TargetWithExtraProperties
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int ExtraProperty { get; set; }
        public string? AnotherExtra { get; set; }
    }

    private class BoolSource
    {
        public bool BoolValue { get; set; }
    }

    private class BoolTarget
    {
        public bool BoolValue { get; set; }
    }

    private class CaseInsensitiveSource
    {
        public int id { get; set; }
        public string NAME { get; set; } = "";
        public double VaLuE { get; set; }
    }

    private class CaseInsensitiveTarget
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Value { get; set; }
    }

    private class SourceWithReadOnly
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Computed => $"Computed_{Name}";
    }

    private class TargetWithReadOnly
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Computed { get; set; } = "";
    }

    // Type coercion test classes
    private class IntSource
    {
        public int IntValue { get; set; }
    }

    private class DoubleTarget
    {
        public double DoubleValue { get; set; }
    }

    private class DoubleSource
    {
        public double DoubleValue { get; set; }
    }

    private class IntTarget
    {
        public int IntValue { get; set; }
    }

    private class StringSource
    {
        public string StringValue { get; set; } = "";
    }

    private class StringBoolSource
    {
        public string StringBoolValue { get; set; } = "";
    }

    private class StringDateTimeSource
    {
        public string DateTimeString { get; set; } = "";
    }

    private class DateTimeTarget
    {
        public DateTime DateTimeValue { get; set; }
    }
}