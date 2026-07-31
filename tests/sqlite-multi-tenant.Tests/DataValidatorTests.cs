using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Validation;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class DataValidatorTests
{
    private readonly DataValidator _validator;

    public DataValidatorTests()
    {
        // Use a NullLogger to avoid needing a real logging implementation.
        _validator = new DataValidator(NullLogger<DataValidator>.Instance);
    }

    [Fact]
    public void RequireString_HappyPath_ReturnsValidator()
    {
        // Arrange
        string value = "Hello";
        string fieldName = "Name";

        // Act
        var result = _validator.RequireString(value, fieldName);

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequireString_NullInput_ReturnsValidator()
    {
        // Arrange
        string value = null;
        string fieldName = "Name";

        // Act
        var result = _validator.RequireString(value, fieldName);

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequireString_EmptyString_ReturnsValidator()
    {
        // Arrange
        string value = "";
        string fieldName = "Name";

        // Act
        var result = _validator.RequireString(value, fieldName);

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequireString_InvalidString_ThrowsArgumentException()
    {
        // Arrange
        string value = "Hello";
        string fieldName = "Name";

        // Act and Assert
        Assert.Throws<ArgumentException>(() => _validator.RequireString(value, fieldName, 5));
    }

    [Fact]
    public void RequireRange_HappyPath_ReturnsValidator()
    {
        // Arrange
        int value = 10;
        int minValue = 5;
        int maxValue = 15;
        string fieldName = "Age";

        // Act
        var result = _validator.RequireRange(value, minValue, maxValue, fieldName);

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequireRange_NullInput_ReturnsValidator()
    {
        // Arrange
        int? value = null;
        int minValue = 5;
        int maxValue = 15;
        string fieldName = "Age";

        // Act
        var result = _validator.RequireRange(value, minValue, maxValue, fieldName);

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequireRange_OutOfRange_ThrowsArgumentException()
    {
        // Arrange
        int value = 20;
        int minValue = 5;
        int maxValue = 15;
        string fieldName = "Age";

        // Act and Assert
        Assert.Throws<ArgumentException>(() => _validator.RequireRange(value, minValue, maxValue, fieldName));
    }

    [Fact]
    public void RequireValidEmail_HappyPath_ReturnsValidator()
    {
        // Arrange
        string value = "test@example.com";
        string fieldName = "Email";

        // Act
        var result = _validator.RequireValidEmail(value, fieldName);

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequireValidEmail_NullInput_ReturnsValidator()
    {
        // Arrange
        string value = null;
        string fieldName = "Email";

        // Act
        var result = _validator.RequireValidEmail(value, fieldName);

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequireValidEmail_InvalidEmail_ThrowsArgumentException()
    {
        // Arrange
        string value = "invalid email";
        string fieldName = "Email";

        // Act and Assert
        Assert.Throws<ArgumentException>(() => _validator.RequireValidEmail(value, fieldName));
    }

    [Fact]
    public void RequireValidUrl_HappyPath_ReturnsValidator()
    {
        // Arrange
        string value = "https://example.com";
        string fieldName = "Url";

        // Act
        var result = _validator.RequireValidUrl(value, fieldName);

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequireValidUrl_NullInput_ReturnsValidator()
    {
        // Arrange
        string value = null;
        string fieldName = "Url";

        // Act
        var result = _validator.RequireValidUrl(value, fieldName);

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequireValidUrl_InvalidUrl_ThrowsArgumentException()
    {
        // Arrange
        string value = "invalid url";
        string fieldName = "Url";

        // Act and Assert
        Assert.Throws<ArgumentException>(() => _validator.RequireValidUrl(value, fieldName));
    }

    [Fact]
    public void RequireValidGuid_HappyPath_ReturnsValidator()
    {
        // Arrange
        string value = "12345678-1234-1234-1234-123456789012";
        string fieldName = "Guid";

        // Act
        var result = _validator.RequireValidGuid(value, fieldName);

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequireValidGuid_NullInput_ReturnsValidator()
    {
        // Arrange
        string value = null;
        string fieldName = "Guid";

        // Act
        var result = _validator.RequireValidGuid(value, fieldName);

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequireValidGuid_InvalidGuid_ThrowsArgumentException()
    {
        // Arrange
        string value = "invalid guid";
        string fieldName = "Guid";

        // Act and Assert
        Assert.Throws<ArgumentException>(() => _validator.RequireValidGuid(value, fieldName));
    }

    [Fact]
    public void RequirePattern_HappyPath_ReturnsValidator()
    {
        // Arrange
        string value = "Hello";
        string pattern = "^[a-zA-Z]+$";
        string fieldName = "Name";

        // Act
        var result = _validator.RequirePattern(value, pattern, fieldName, "Invalid name");

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequirePattern_NullInput_ReturnsValidator()
    {
        // Arrange
        string value = null;
        string pattern = "^[a-zA-Z]+$";
        string fieldName = "Name";

        // Act
        var result = _validator.RequirePattern(value, pattern, fieldName, "Invalid name");

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequirePattern_InvalidPattern_ThrowsArgumentException()
    {
        // Arrange
        string value = "Hello";
        string pattern = "invalid pattern";
        string fieldName = "Name";

        // Act and Assert
        Assert.Throws<ArgumentException>(() => _validator.RequirePattern(value, pattern, fieldName, "Invalid name"));
    }

    [Fact]
    public void Require_HappyPath_ReturnsValidator()
    {
        // Arrange
        int value = 10;
        Func<int?, bool> predicate = x => x > 5;
        string fieldName = "Age";

        // Act
        var result = _validator.Require(value, predicate, fieldName, "Invalid age");

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void Require_NullInput_ReturnsValidator()
    {
        // Arrange
        int? value = null;
        Func<int?, bool> predicate = x => x > 5;
        string fieldName = "Age";

        // Act
        var result = _validator.Require(value, predicate, fieldName, "Invalid age");

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void Require_InvalidPredicate_ThrowsArgumentException()
    {
        // Arrange
        int value = 10;
        Func<int?, bool> predicate = x => x < 5;
        string fieldName = "Age";

        // Act and Assert
        Assert.Throws<ArgumentException>(() => _validator.Require(value, predicate, fieldName, "Invalid age"));
    }

    [Fact]
    public void RequireNotEmpty_HappyPath_ReturnsValidator()
    {
        // Arrange
        List<int> value = new List<int> { 1, 2, 3 };
        string fieldName = "Numbers";

        // Act
        var result = _validator.RequireNotEmpty(value, fieldName);

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequireNotEmpty_NullInput_ReturnsValidator()
    {
        // Arrange
        List<int>? value = null;
        string fieldName = "Numbers";

        // Act
        var result = _validator.RequireNotEmpty(value, fieldName);

        // Assert
        Assert.Same(_validator, result);
    }

    [Fact]
    public void RequireNotEmpty_EmptyCollection_ThrowsArgumentException()
    {
        // Arrange
        List<int> value = new List<int>();
        string fieldName = "Numbers";

        // Act and Assert
        Assert.Throws<ArgumentException>(() => _validator.RequireNotEmpty(value, fieldName));
    }
}
