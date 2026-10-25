#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Tests;

using System;
using SqliteMultiTenant.BackgroundWorkers;
using Xunit;

/// <summary>
/// Tests for the DatabaseMaintenanceWorkerValidation class.
/// </summary>
public sealed class DatabaseMaintenanceWorkerValidationTests
{
    [Fact]
    public void Validate_DefaultOptions_ReturnsNoProblems()
    {
        // Arrange
        var options = new DatabaseMaintenanceOptions();

        // Act
        var problems = options.Validate();

        // Assert
        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        DatabaseMaintenanceOptions? options = null;

        // Act
        Action act = () => DatabaseMaintenanceWorkerValidation.Validate(options);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(168, true)]
    [InlineData(169, false)]
    public void Validate_IntervalHoursBoundaries_ReportsProblemOnlyOutsideRange(int hours, bool expectedValid)
    {
        // Arrange
        var options = new DatabaseMaintenanceOptions { IntervalHours = hours };

        // Act
        var problems = options.Validate();

        // Assert
        Assert.Equal(expectedValid, problems.Count == 0);
        if (!expectedValid)
        {
            Assert.Single(problems);
            Assert.Contains("IntervalHours", problems[0]);
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(3600, true)]
    [InlineData(3601, false)]
    public void Validate_TimeoutSecondsBoundaries_ReportsProblemOnlyOutsideRange(int seconds, bool expectedValid)
    {
        // Arrange
        var options = new DatabaseMaintenanceOptions { TimeoutSeconds = seconds };

        // Act
        var problems = options.Validate();

        // Assert
        Assert.Equal(expectedValid, problems.Count == 0);
        if (!expectedValid)
        {
            Assert.Single(problems);
            Assert.Contains("TimeoutSeconds", problems[0]);
        }
    }

    [Fact]
    public void Validate_DegreeOfParallelismBoundaries_AllowsZeroAndProcessorCountTimesFour()
    {
        // Arrange
        int maxAllowed = Environment.ProcessorCount * 4;

        // Act & Assert
        Assert.Empty(new DatabaseMaintenanceOptions { DegreeOfParallelism = 0 }.Validate());
        Assert.Empty(new DatabaseMaintenanceOptions { DegreeOfParallelism = maxAllowed }.Validate());

        var negative = new DatabaseMaintenanceOptions { DegreeOfParallelism = -1 }.Validate();
        Assert.Single(negative);
        Assert.Contains("DegreeOfParallelism", negative[0]);

        var tooHigh = new DatabaseMaintenanceOptions { DegreeOfParallelism = maxAllowed + 1 }.Validate();
        Assert.Single(tooHigh);
        Assert.Contains("DegreeOfParallelism", tooHigh[0]);
    }

    [Fact]
    public void Validate_AllFieldsInvalid_ReportsOneProblemPerField()
    {
        // Arrange
        var options = new DatabaseMaintenanceOptions
        {
            IntervalHours = 0,
            TimeoutSeconds = -5,
            DegreeOfParallelism = -1
        };

        // Act
        var problems = options.Validate();

        // Assert
        Assert.Equal(3, problems.Count);
        Assert.Contains(problems, p => p.Contains("IntervalHours"));
        Assert.Contains(problems, p => p.Contains("TimeoutSeconds"));
        Assert.Contains(problems, p => p.Contains("DegreeOfParallelism"));
    }

    [Fact]
    public void IsValid_NullAndInvalidOptions_ReturnsFalse()
    {
        // Arrange
        DatabaseMaintenanceOptions? nullOptions = null;
        var invalidOptions = new DatabaseMaintenanceOptions { TimeoutSeconds = 0 };

        // Act & Assert
        Assert.False(DatabaseMaintenanceWorkerValidation.IsValid(nullOptions));
        Assert.False(invalidOptions.IsValid());
        Assert.True(new DatabaseMaintenanceOptions().IsValid());
    }

    [Fact]
    public void EnsureValid_InvalidOptions_ThrowsArgumentExceptionListingProblems()
    {
        // Arrange
        var options = new DatabaseMaintenanceOptions { IntervalHours = 500 };

        // Act
        Action act = () => options.EnsureValid();

        // Assert
        var ex = Assert.Throws<ArgumentException>(act);
        Assert.Contains("DatabaseMaintenanceOptions is invalid", ex.Message);
        Assert.Contains("IntervalHours 500 exceeds maximum", ex.Message);
    }
}
