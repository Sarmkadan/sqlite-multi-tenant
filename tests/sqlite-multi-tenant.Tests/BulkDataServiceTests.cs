#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SqliteMultiTenant.BulkOperations;
using SqliteMultiTenant.DataOperations;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Operations;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Tests for <see cref="BulkDataService"/> against real SQLite files in a temporary directory.
/// </summary>
public sealed class BulkDataServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"bulk_svc_{Guid.NewGuid():N}");

    public BulkDataServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        SQLiteConnection.ClearAllPools();
        Directory.Delete(_dir, true);
    }

    [Fact]
    public async Task ExportTableAsync_CsvTable_WritesFileAndReportsRowCount()
    {
        // Arrange
        CreateDatabase("tenant1",
            "CREATE TABLE items (id INTEGER, name TEXT)",
            "INSERT INTO items VALUES (1, 'alpha')",
            "INSERT INTO items VALUES (2, 'beta')",
            "INSERT INTO items VALUES (3, 'gamma')");
        var outputPath = Path.Combine(_dir, "items.csv");

        // Act
        var result = await CreateService().ExportTableAsync("tenant1", "items", BulkDataFormat.Csv, new ExportOptions { OutputFilePath = outputPath });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TotalRowsExported.Should().Be(3);
        result.TablesProcessed.Should().Equal("items");
        File.ReadAllText(outputPath).Should().Contain("gamma");
    }

    [Fact]
    public async Task ExportDatabaseAsync_TwoTables_AggregatesRowsAndPublishesStartedEvent()
    {
        // Arrange
        CreateDatabase("tenant1",
            "CREATE TABLE a (id INTEGER)",
            "INSERT INTO a VALUES (1)",
            "INSERT INTO a VALUES (2)",
            "CREATE TABLE b (id INTEGER)",
            "INSERT INTO b VALUES (3)");
        var eventBus = Substitute.For<IEventBus>();

        // Act
        var result = await CreateService(eventBus).ExportDatabaseAsync("tenant1", BulkDataFormat.Csv, new ExportOptions { OutputFilePath = Path.Combine(_dir, "all.csv") });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TablesProcessed.Should().Equal("a", "b");
        result.TotalRowsExported.Should().Be(3);
        await eventBus.Received(1).PublishAsync(Arg.Any<BulkExportStartedEvent>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, "items")]
    [InlineData("tenant1", null)]
    [InlineData("", "items")]
    [InlineData("tenant1", "   ")]
    public async Task ExportTableAsync_NullOrBlankArguments_ThrowsArgumentException(string? databaseId, string? tableName)
    {
        // Act
        var act = () => CreateService().ExportTableAsync(databaseId!, tableName!, BulkDataFormat.Json);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(2, new[] { 2, 2, 1 })]
    [InlineData(0, new[] { 5 })]
    public async Task StreamExportAsync_RowsSplitIntoBatches_EndsWithLastBatchFlag(int batchSize, int[] expectedRowCounts)
    {
        // Arrange
        var sql = new[] { "CREATE TABLE nums (n INTEGER)" }
            .Concat(Enumerable.Range(1, 5).Select(n => $"INSERT INTO nums VALUES ({n})"))
            .ToArray();
        CreateDatabase("tenant1", sql);
        var batches = new List<ExportBatch>();

        // Act
        await foreach (var batch in CreateService().StreamExportAsync("tenant1", "nums", BulkDataFormat.Csv, batchSize))
        {
            batches.Add(batch);
        }

        // Assert
        batches.Select(b => b.RowCount).Should().Equal(expectedRowCounts);
        batches.Select(b => b.SequenceNumber).Should().Equal(Enumerable.Range(0, expectedRowCounts.Length));
        batches.Where(b => b.IsLastBatch).Should().ContainSingle().Which.Should().BeSameAs(batches.Last());
    }

    [Fact]
    public async Task ImportTableAsync_CsvPayload_InsertsRowsAndReportsCount()
    {
        // Arrange
        CreateDatabase("tenant1", "CREATE TABLE items (id INTEGER, name TEXT)");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("id,name\n1,alpha\n2,beta\n"));

        // Act
        var result = await CreateService().ImportTableAsync("tenant1", "items", stream, BulkDataFormat.Csv);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TotalRowsImported.Should().Be(2);
        CountRows("tenant1", "items").Should().Be(2);
    }

    [Fact]
    public async Task StreamImportAsync_FailedBatch_IsSkippedAndRemainingRowsImported()
    {
        // Arrange
        CreateDatabase("tenant1", "CREATE TABLE items (id INTEGER, name TEXT)");
        var batches = ToAsync(
            new ImportBatch("items", "id,name\n1,alpha\n", BulkDataFormat.Csv, 0, false),
            new ImportBatch("ghost", "id,name\n9,x\n", BulkDataFormat.Csv, 1, false),
            new ImportBatch("items", "id,name\n2,beta\n", BulkDataFormat.Csv, 2, true));

        // Act
        var result = await CreateService().StreamImportAsync("tenant1", batches);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TotalRowsImported.Should().Be(2);
        result.TotalRowsFailed.Should().Be(1);
        result.TablesProcessed.Should().BeEquivalentTo("items", "ghost");
        result.Warnings.Should().ContainSingle().Which.Should().Contain("ghost");
        CountRows("tenant1", "items").Should().Be(2);
    }

    private BulkDataService CreateService(IEventBus? eventBus = null) =>
        new(new DataExporter(NullLogger<DataExporter>.Instance),
            new DataImporter(NullLogger<DataImporter>.Instance),
            new BatchProcessor(NullLogger<BatchProcessor>.Instance),
            eventBus ?? Substitute.For<IEventBus>(),
            NullLogger<BulkDataService>.Instance,
            new BulkDataOptions { BaseDatabasePath = _dir });

    private void CreateDatabase(string databaseId, params string[] statements)
    {
        using var connection = new SQLiteConnection(ConnectionString(databaseId));
        connection.Open();
        foreach (var sql in statements)
        {
            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }

    private long CountRows(string databaseId, string tableName)
    {
        using var connection = new SQLiteConnection(ConnectionString(databaseId));
        connection.Open();
        using var command = new SQLiteCommand($"SELECT COUNT(*) FROM {tableName}", connection);
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private string ConnectionString(string databaseId) =>
        $"Data Source={Path.Combine(_dir, databaseId + ".db")};Version=3;";

    private static async IAsyncEnumerable<ImportBatch> ToAsync(params ImportBatch[] batches)
    {
        foreach (var batch in batches)
        {
            await Task.Yield();
            yield return batch;
        }
    }
}
