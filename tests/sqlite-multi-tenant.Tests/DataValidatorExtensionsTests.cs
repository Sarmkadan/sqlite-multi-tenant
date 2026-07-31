using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Validation;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class DataValidatorExtensionsTests
{
    private readonly DataValidator _validator;

    public DataValidatorExtensionsTests()
    {
        _validator = new DataValidator(NullLogger<DataValidator>.Instance);
    }

    [Fact]
    public void RequireString_ValidValue_IsValid()
    {
        _validator.RequireString("value", "field", "Error");
        Assert.True(_validator.GetResult().IsValid);
    }

    [Fact]
    public void RequireMinLength_TooShort_IsInvalid()
    {
        _validator.RequireMinLength("sh", 3, "field");
        var result = _validator.GetResult();
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "field");
    }

    [Fact]
    public void RequireLengthBetween_Valid_IsValid()
    {
        _validator.RequireLengthBetween("test", 3, 5, "field");
        Assert.True(_validator.GetResult().IsValid);
    }

    [Fact]
    public void RequireValidPhoneNumber_Invalid_IsInvalid()
    {
        _validator.RequireValidPhoneNumber("123", "phone");
        Assert.False(_validator.GetResult().IsValid);
    }

    [Fact]
    public void RequireValidDate_InvalidFormat_IsInvalid()
    {
        _validator.RequireValidDate("invalid date", "date");
        Assert.False(_validator.GetResult().IsValid);
    }

    [Fact]
    public void RequireValidDateTime_Valid_IsValid()
    {
        _validator.RequireValidDateTime("2023-12-12T10:00:00", "dt");
        Assert.True(_validator.GetResult().IsValid);
    }

    [Fact]
    public void RequireValidIPv4_Invalid_IsInvalid()
    {
        _validator.RequireValidIPv4("123.456.789.0", "ip");
        Assert.False(_validator.GetResult().IsValid);
    }

    [Fact]
    public void RequireCollectionCount_WrongCount_IsInvalid()
    {
        _validator.RequireCollectionCount(new[] { 1, 2 }, 3, "list");
        Assert.False(_validator.GetResult().IsValid);
    }

    [Fact]
    public void RequireMaxItems_TooMany_IsInvalid()
    {
        _validator.RequireMaxItems(new[] { 1, 2, 3 }, 2, "list");
        Assert.False(_validator.GetResult().IsValid);
    }

    [Fact]
    public void RequireGreaterThan_NotGreater_IsInvalid()
    {
        _validator.RequireGreaterThan(5, 10, "value");
        Assert.False(_validator.GetResult().IsValid);
    }
}
