using System;
using System.ComponentModel;
using System.Collections.Generic;
using FluentAssertions;
using SqliteMultiTenant.Utilities;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class EnumExtensionsTests
{
    public enum TestEnum
    {
        [Description("First Value Description")]
        FirstValue,
        SecondValue,
        [Description("Third")]
        ThirdValue
    }

    [Fact]
    public void GetDisplayName_ShouldReturnTitleCase()
    {
        TestEnum.FirstValue.GetDisplayName().Should().Be("First Value");
        TestEnum.SecondValue.GetDisplayName().Should().Be("Second Value");
    }

    [Theory]
    [InlineData("FirstValue", TestEnum.FirstValue)]
    [InlineData("secondvalue", TestEnum.SecondValue)]
    [InlineData("Invalid", TestEnum.ThirdValue)]
    public void ParseSafe_ShouldParseCorrectly(string input, TestEnum expected)
    {
        input.ParseSafe(TestEnum.ThirdValue).Should().Be(expected);
    }

    [Fact]
    public void HasAttribute_ShouldReturnTrueIfAttributeExists()
    {
        TestEnum.FirstValue.HasAttribute<TestEnum, DescriptionAttribute>().Should().BeTrue();
        TestEnum.SecondValue.HasAttribute<TestEnum, DescriptionAttribute>().Should().BeFalse();
    }

    [Fact]
    public void GetAttribute_ShouldReturnAttributeIfExists()
    {
        var attr = TestEnum.FirstValue.GetAttribute<TestEnum, DescriptionAttribute>();
        attr.Should().NotBeNull();
        attr!.Description.Should().Be("First Value Description");
    }

    [Fact]
    public void GetAllValues_ShouldReturnAllValues()
    {
        var values = EnumExtensions.GetAllValues<TestEnum>();
        values.Should().BeEquivalentTo(new[] { TestEnum.FirstValue, TestEnum.SecondValue, TestEnum.ThirdValue });
    }

    [Theory]
    [InlineData("FirstValue", true)]
    [InlineData("firstvalue", true)]
    [InlineData("Invalid", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsValidEnumValue_ShouldValidateCorrectly(string? input, bool expected)
    {
        input.IsValidEnumValue<TestEnum>().Should().Be(expected);
    }

    [Fact]
    public void GetDescription_ShouldReturnDescriptionOrDisplayName()
    {
        TestEnum.FirstValue.GetDescription().Should().Be("First Value Description");
        TestEnum.SecondValue.GetDescription().Should().Be("Second Value");
    }
}
