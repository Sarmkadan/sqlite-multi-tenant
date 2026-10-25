#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using SqliteMultiTenant.BulkOperations;
using Xunit;

namespace SqliteMultiTenant.Tests;

public sealed class BulkDataOptionsValidationTests
{
    [Fact]
    public void Validate_DefaultOptions_ReturnsNoProblems()
    {
        // Arrange
        var options = new BulkDataOptions();

        // Act
        var problems = options.Validate();

        // Assert
        Assert.Empty(problems);
        Assert.True(options.IsValid());
        Assert.Null(Record.Exception(() => options.EnsureValid()));
    }

    [Fact]
    public void Validate_NonPositiveNumericValues_ReportsEachField()
    {
        // Arrange
        var options = new BulkDataOptions
        {
            DefaultBatchSize = 0,
            MaxConcurrentTables = -1,
            MaxBufferSizeBytes = 0,
            OperationTimeout = TimeSpan.Zero
        };

        // Act
        var problems = options.Validate();

        // Assert
        Assert.Equal(4, problems.Count);
        Assert.Contains(problems, p => p.StartsWith("DefaultBatchSize must be positive, but was 0.", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.StartsWith("MaxConcurrentTables must be positive, but was -1.", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.StartsWith("MaxBufferSizeBytes must be positive, but was 0.", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.StartsWith("OperationTimeout must be positive", StringComparison.Ordinal));
        Assert.False(options.IsValid());
    }

    [Fact]
    public void Validate_SmallestPositiveValues_AreValid()
    {
        // Arrange
        var options = new BulkDataOptions
        {
            DefaultBatchSize = 1,
            MaxConcurrentTables = 1,
            MaxBufferSizeBytes = 1,
            OperationTimeout = TimeSpan.FromTicks(1)
        };

        // Act
        var problems = options.Validate();

        // Assert
        Assert.Empty(problems);
        Assert.True(options.IsValid());
    }

    [Theory]
    [InlineData("./data", true)]
    [InlineData(".\\data", true)]
    [InlineData("/var/data", true)]
    [InlineData("data", false)]
    [InlineData("../data", false)]
    public void Validate_PathsWithoutRootOrDotPrefix_ReportBothPathFields(string path, bool expectedValid)
    {
        // Arrange
        var options = new BulkDataOptions
        {
            DefaultExportDirectory = path,
            BaseDatabasePath = path
        };

        // Act
        var problems = options.Validate();

        // Assert
        if (expectedValid)
        {
            Assert.Empty(problems);
            return;
        }

        Assert.Equal(2, problems.Count);
        Assert.Contains(problems, p => p.StartsWith("DefaultExportDirectory must be an absolute path", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.StartsWith("BaseDatabasePath must be an absolute path", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhitespacePath_ThrowsArgumentExceptionNamingParameter()
    {
        // Arrange
        var options = new BulkDataOptions { BaseDatabasePath = "   " };

        // Act
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());

        // Assert
        Assert.Equal(nameof(BulkDataOptions.BaseDatabasePath), ex.ParamName);
    }

    [Fact]
    public void Validate_NullInstance_ThrowsArgumentNullException()
    {
        // Arrange
        BulkDataOptions? options = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => options!.Validate());
        Assert.Throws<ArgumentNullException>(() => options!.IsValid());
        Assert.Throws<ArgumentNullException>(() => options!.EnsureValid());
    }

    [Fact]
    public void EnsureValid_InvalidOptions_ThrowsWithAllProblemsInMessage()
    {
        // Arrange
        var options = new BulkDataOptions
        {
            DefaultBatchSize = 0,
            BaseDatabasePath = "databases"
        };

        // Act
        var ex = Assert.Throws<ArgumentException>(() => options.EnsureValid());

        // Assert
        Assert.StartsWith("BulkDataOptions is invalid. Problems:", ex.Message, StringComparison.Ordinal);
        Assert.Contains("DefaultBatchSize must be positive, but was 0.", ex.Message);
        Assert.Contains("BaseDatabasePath must be an absolute path or start with './' or '/', but was 'databases'.", ex.Message);
        Assert.False(options.IsValid());
    }
}
