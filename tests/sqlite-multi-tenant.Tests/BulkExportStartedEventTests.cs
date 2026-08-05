using System;
using System.Collections.Generic;
using Xunit;
using SqliteMultiTenant.Events;

namespace SqliteMultiTenant.Tests;

public sealed class BulkExportStartedEventTests
{
    [Fact]
    public void Constructor_SetsEventTypeToClassName()
    {
        var evt = new BulkExportStartedEvent();

        Assert.Equal(nameof(BulkExportStartedEvent), evt.EventType);
    }

    [Fact]
    public void Constructor_SetsDefaultPropertyValues()
    {
        var evt = new BulkExportStartedEvent();

        Assert.Equal(string.Empty, evt.DatabaseId);
        Assert.Equal(string.Empty, evt.Format);
        Assert.Equal(string.Empty, evt.OperationId);
        Assert.NotNull(evt.TableNames);
        Assert.Empty(evt.TableNames);
        Assert.NotEmpty(evt.EventId);
        Assert.True(evt.OccurredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved_HappyPath()
    {
        var tableNames = new List<string> { "Users", "Orders" };

        var evt = new BulkExportStartedEvent
        {
            DatabaseId = "db-123",
            TableNames = tableNames,
            Format = "Json",
            OperationId = "op-456"
        };

        Assert.Equal("db-123", evt.DatabaseId);
        Assert.Same(tableNames, evt.TableNames);
        Assert.Equal("Json", evt.Format);
        Assert.Equal("op-456", evt.OperationId);
    }

    [Fact]
    public void TableNames_AcceptsEmptyCollection()
    {
        var evt = new BulkExportStartedEvent
        {
            TableNames = Array.Empty<string>()
        };

        Assert.Empty(evt.TableNames);
    }

    [Fact]
    public void TableNames_AcceptsSingleTableName_BoundaryValue()
    {
        var evt = new BulkExportStartedEvent
        {
            TableNames = new List<string> { "OnlyTable" }
        };

        Assert.Single(evt.TableNames);
        Assert.Equal("OnlyTable", evt.TableNames[0]);
    }

    [Fact]
    public void EachInstance_GetsUniqueEventId()
    {
        var first = new BulkExportStartedEvent();
        var second = new BulkExportStartedEvent();

        Assert.NotEqual(first.EventId, second.EventId);
    }

    [Fact]
    public void TenantId_DefaultsToNull_AndCanBeAssigned()
    {
        var evt = new BulkExportStartedEvent();

        Assert.Null(evt.TenantId);

        evt.TenantId = "tenant-1";

        Assert.Equal("tenant-1", evt.TenantId);
    }

    [Fact]
    public void BulkExportCompletedEvent_Properties_CanBeSetAndRetrieved()
    {
        var evt = new BulkExportCompletedEvent
        {
            DatabaseId = "db-789",
            RowsExported = 100000L,
            TablesExported = 5,
            DurationMs = 2500,
            OutputPath = "/tmp/export.json",
            OperationId = "op-456"
        };

        Assert.Equal(nameof(BulkExportCompletedEvent), evt.EventType);
        Assert.Equal("db-789", evt.DatabaseId);
        Assert.Equal(100000L, evt.RowsExported);
        Assert.Equal(5, evt.TablesExported);
        Assert.Equal(2500, evt.DurationMs);
        Assert.Equal("/tmp/export.json", evt.OutputPath);
        Assert.Equal("op-456", evt.OperationId);
    }
}
