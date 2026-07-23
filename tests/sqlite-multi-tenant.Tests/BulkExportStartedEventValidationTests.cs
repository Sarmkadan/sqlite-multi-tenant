using System;
using System.Collections.Generic;
using Xunit;
using SqliteMultiTenant.Events;

namespace SqliteMultiTenant.Tests;

public sealed class BulkExportStartedEventValidationTests
{
    private static BulkExportStartedEvent CreateValidEvent()
    {
        return new BulkExportStartedEvent
        {
            DatabaseId = "db-123",
            TableNames = new List<string> { "Users", "Orders" },
            Format = "Json",
            OperationId = "op-456"
        };
    }

    [Fact]
    public void Validate_ValidEvent_ReturnsEmptyList()
    {
        var evt = CreateValidEvent();

        var problems = evt.Validate();

        Assert.NotNull(problems);
        Assert.Empty(problems);
        Assert.IsAssignableFrom<IReadOnlyList<string>>(problems);
    }

    [Fact]
    public void Validate_NullEvent_ThrowsArgumentNullException()
    {
        BulkExportStartedEvent? evt = null;

        Assert.Throws<ArgumentNullException>(() => evt.Validate());
    }

    [Fact]
    public void Validate_EmptyTableNames_ReturnsProblem()
    {
        var evt = CreateValidEvent();
        evt.TableNames = new List<string>();

        var problems = evt.Validate();

        Assert.Contains("TableNames must contain at least one table name.", problems);
    }

    [Fact]
    public void Validate_InvalidFormat_ReturnsProblem()
    {
        var evt = CreateValidEvent();
        evt.Format = "Xml";

        var problems = evt.Validate();

        Assert.Contains("Format must be one of: Json, Csv, Sql.", problems);
    }

    [Fact]
    public void Validate_EmptyOperationId_ReturnsProblem()
    {
        var evt = CreateValidEvent();
        evt.OperationId = string.Empty;

        var problems = evt.Validate();

        Assert.Contains("OperationId must not be null or whitespace.", problems);
    }

    [Fact]
    public void IsValid_ValidEvent_ReturnsTrue()
    {
        var evt = CreateValidEvent();

        Assert.True(evt.IsValid());
    }

    [Fact]
    public void IsValid_InvalidEvent_ReturnsFalse()
    {
        var evt = CreateValidEvent();
        evt.Format = "Xml";

        Assert.False(evt.IsValid());
    }

    [Fact]
    public void IsValid_NullEvent_ThrowsNullReferenceException()
    {
        BulkExportStartedEvent? evt = null;

        Assert.Throws<NullReferenceException>(() => evt.IsValid());
    }

    [Fact]
    public void EnsureValid_ValidEvent_DoesNotThrow()
    {
        var evt = CreateValidEvent();

        var exception = Record.Exception(() => evt.EnsureValid());

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureValid_InvalidEvent_ThrowsArgumentException()
    {
        var evt = CreateValidEvent();
        evt.TableNames = new List<string> { "" };

        var ex = Assert.Throws<ArgumentException>(() => evt.EnsureValid());

        Assert.Contains("BulkExportStartedEvent is invalid. Problems:", ex.Message);
        Assert.Contains("TableNames[0] must not be null or whitespace.", ex.Message);
    }

    [Fact]
    public void EnsureValid_NullEvent_ThrowsArgumentNullException()
    {
        BulkExportStartedEvent? evt = null;

        Assert.Throws<ArgumentNullException>(() => evt.EnsureValid());
    }
}
