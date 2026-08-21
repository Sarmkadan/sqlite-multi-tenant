#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using FluentAssertions;
using NSubstitute;
using Xunit;
using SqliteMultiTenant.DataOperations;
using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;
using System.Collections.Generic; // Added for Dictionary<string, object>

namespace SqliteMultiTenant.Tests
{
    /// <summary>
    /// Contains unit tests for the <see cref="DataExporter"/> class.
    /// Tests the functionality of exporting data from SQLite database tables in various formats (JSON, CSV, SQL).
    /// </summary>
    public sealed class DataExporterTests : IDisposable {
        /// <summary>
        /// Gets the mock logger instance used for testing.
        /// </summary>
        private readonly ILogger<DataExporter> _mockLogger;

        /// <summary>
        /// Gets the system under test - an instance of <see cref="DataExporter"/> being tested.
        /// </summary>
        private readonly DataExporter _sut;

        /// <summary>
        /// Gets the SQLite database connection used for testing.
        /// </summary>
        private SQLiteConnection _connection;

        /// <summary>
        /// Gets the name of the test table containing sample user data.
        /// </summary>
        private string _testTableName = "TestUsers";

        /// <summary>
        /// Gets the name of an empty test table used for testing edge cases.
        /// </summary>
        private string _emptyTableName = "EmptyTable";

        /// <summary>
        /// Initializes a new instance of the <see cref="DataExporterTests"/> class.
        /// Sets up the test environment with mock logger, test data exporter instance,
        /// and initializes an in-memory SQLite database with sample test data.
        /// </summary>
        public DataExporterTests()
        {
            _mockLogger = Substitute.For<ILogger<DataExporter>>();
            _mockLogger.LogInformation("Initializing DataExporterTests with mock logger");
            _sut = new DataExporter(_mockLogger);

            InitializeInMemoryDatabase();
        }

        private void InitializeInMemoryDatabase()
        {
            _connection = new SQLiteConnection("Data Source=:memory:");
            _connection.Open();

            using (var command = _connection.CreateCommand())
            {
                // Create test table and insert data
                command.CommandText = $"CREATE TABLE {_testTableName} (Id INTEGER PRIMARY KEY, Name TEXT, Email TEXT);";
                command.ExecuteNonQuery();

                command.CommandText = $"INSERT INTO {_testTableName} (Id, Name, Email) VALUES (1, 'Alice', 'alice@example.com');";
                command.ExecuteNonQuery();
                command.CommandText = $"INSERT INTO {_testTableName} (Id, Name, Email) VALUES (2, 'Bob', 'bob@example.com');";
                command.ExecuteNonQuery();
                command.CommandText = $"INSERT INTO {_testTableName} (Id, Name, Email) VALUES (3, 'Charlie', NULL);";
                command.ExecuteNonQuery();

                // Create an empty table
                command.CommandText = $"CREATE TABLE {_emptyTableName} (Id INTEGER PRIMARY KEY, Value TEXT);";
                command.ExecuteNonQuery();
            }
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsJsonAsync"/> returns correct JSON output
        /// when the table contains sample data with proper metadata inclusion.
        /// </summary>
        public async Task ExportAsJsonAsync_ShouldReturnCorrectJson_WhenTableHasData()
        {
            _mockLogger.LogInformation("ExportAsJsonAsync_ShouldReturnCorrectJson_WhenTableHasData called with {TableName}, {IncludeMeta}", _testTableName, true);

            // Act
            var result = await _sut.ExportAsJsonAsync(_connection, _testTableName, true);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("\"Table\": \"" + _testTableName + "\"");
            result.Should().Contain("\"RowCount\": 3");
            result.Should().Contain("\"Id\": 1");
            result.Should().Contain("\"Name\": \"Alice\"");
            result.Should().Contain("\"Email\": \"alice@example.com\"");
            result.Should().Contain("\"Name\": \"Bob\"");
            result.Should().Contain("\"Email\": \"bob@example.com\"");
            result.Should().Contain("\"Name\": \"Charlie\"");
            result.Should().Contain("\"Email\": null");
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsJsonAsync"/> excludes metadata from the output
        /// when the includeMeta parameter is set to false.
        /// </summary>
        public async Task ExportAsJsonAsync_ShouldExcludeMetadata_WhenIncludeMetaIsFalse()
        {
            _mockLogger.LogInformation("ExportAsJsonAsync_ShouldExcludeMetadata_WhenIncludeMetaIsFalse called with {TableName}, {IncludeMeta}", _testTableName, false);

            // Act
            var result = await _sut.ExportAsJsonAsync(_connection, _testTableName, false);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().NotContain("meta");
            result.Should().NotContain("Table");
            result.Should().Contain("\"Id\": 1");
            result.Should().Contain("\"Name\": \"Alice\"");
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsJsonAsync"/> throws an <see cref="ArgumentNullException"/>
        /// when the database connection parameter is null.
        /// </summary>
        public async Task ExportAsJsonAsync_ShouldThrowArgumentNullException_WhenConnectionIsNull()
        {
            // Act & Assert
            await _sut.Awaiting(s => s.ExportAsJsonAsync(null, _testTableName))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("connection");
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsJsonAsync"/> throws an <see cref="ArgumentNullException"/>
        /// when the table name parameter is an empty string.
        /// </summary>
        public async Task ExportAsJsonAsync_ShouldThrowArgumentNullException_WhenTableNameIsEmpty()
        {
            // Act & Assert
            await _sut.Awaiting(s => s.ExportAsJsonAsync(_connection, ""))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("tableName");
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsJsonAsync"/> throws an <see cref="ArgumentNullException"/>
        /// when the table name parameter is null.
        /// </summary>
        public async Task ExportAsJsonAsync_ShouldThrowArgumentNullException_WhenTableNameIsNull()
        {
            // Act & Assert
            await _sut.Awaiting(s => s.ExportAsJsonAsync(_connection, null))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("tableName");
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsJsonAsync"/> returns JSON with empty data array
        /// when the table is empty.
        /// </summary>
        public async Task ExportAsJsonAsync_ShouldReturnEmptyDataArray_WhenTableIsEmpty()
        {
            // Act
            var result = await _sut.ExportAsJsonAsync(_connection, _emptyTableName, true);

            // Assert
            result.Should().Contain("\"RowCount\": 0");
            result.Should().Contain("\"data\": []");
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsCsvAsync"/> returns correct CSV output
        /// when the table contains sample data with headers included.
        /// </summary>
        public async Task ExportAsCsvAsync_ShouldReturnCorrectCsv_WhenTableHasData()
        {
            // Arrange
            var expectedCsv =
            "Id,Name,Email\n" +
            "1,Alice,alice@example.com\n" +
            "2,Bob,bob@example.com\n" +
            "3,Charlie,\"\"\n";

            // Act
            var result = await _sut.ExportAsCsvAsync(_connection, _testTableName, true);

            // Assert
            result.Should().Be(expectedCsv);
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsCsvAsync"/> excludes headers from the output
        /// when the includeHeaders parameter is set to false.
        /// </summary>
        public async Task ExportAsCsvAsync_ShouldExcludeHeaders_WhenIncludeHeadersIsFalse()
        {
            _mockLogger.LogInformation("ExportAsCsvAsync_ShouldExcludeHeaders_WhenIncludeHeadersIsFalse called with {TableName}, {IncludeHeaders}", _testTableName, false);

            // Arrange
            var expectedCsv =
            "1,Alice,alice@example.com\n" +
            "2,Bob,bob@example.com\n" +
            "3,Charlie,\"\"\n";

            // Act
            var result = await _sut.ExportAsCsvAsync(_connection, _testTableName, false);

            // Assert
            result.Should().Be(expectedCsv);
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsCsvAsync"/> properly handles special characters and commas
        /// in CSV fields by properly escaping them according to CSV standards.
        /// </summary>
        public async Task ExportAsCsvAsync_ShouldHandleSpecialCharactersAndCommas_InCsvFields()
        {
            // Arrange
            var specialTableName = "SpecialTable";
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"CREATE TABLE {specialTableName} (Id INTEGER PRIMARY KEY, Description TEXT);";
                command.ExecuteNonQuery();

                command.CommandText = $"INSERT INTO {specialTableName} (Id, Description) VALUES (1, 'Value with \"quotes\" and, commas');";
                command.ExecuteNonQuery();
                command.CommandText = $"INSERT INTO {specialTableName} (Id, Description) VALUES (2, 'Multi\nLine');";
                command.ExecuteNonQuery();
            }

            var expectedCsv =
            "Id,Description\n" +
            "1,\"Value with \"\"quotes\"\" and, commas\"\n" +
            "2,\"Multi\nLine\"\n";

            // Act
            var result = await _sut.ExportAsCsvAsync(_connection, specialTableName, true);

            // Assert
            result.Should().Be(expectedCsv);
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsCsvAsync"/> throws an <see cref="ArgumentNullException"/>
        /// when the database connection parameter is null.
        /// </summary>
        public async Task ExportAsCsvAsync_ShouldThrowArgumentNullException_WhenConnectionIsNull()
        {
            // Act & Assert
            await _sut.Awaiting(s => s.ExportAsCsvAsync(null, _testTableName))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("connection");
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsCsvAsync"/> throws an <see cref="ArgumentNullException"/>
        /// when the table name parameter is an empty string.
        /// </summary>
        public async Task ExportAsCsvAsync_ShouldThrowArgumentNullException_WhenTableNameIsEmpty()
        {
            // Act & Assert
            await _sut.Awaiting(s => s.ExportAsCsvAsync(_connection, ""))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("tableName");
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsCsvAsync"/> returns only headers (column names)
        /// when the table is empty and headers are included.
        /// </summary>
        public async Task ExportAsCsvAsync_ShouldReturnOnlyHeaders_WhenTableIsEmptyAndHeadersIncluded()
        {
            // Arrange
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"CREATE TABLE IF NOT EXISTS AnotherEmptyTable (Col1 TEXT, Col2 TEXT);";
                command.ExecuteNonQuery();
            }
            var expectedCsv = "Col1,Col2\n";

            // Act
            var result = await _sut.ExportAsCsvAsync(_connection, "AnotherEmptyTable", true);

            // Assert
            result.Should().Be(expectedCsv);
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsCsvAsync"/> returns an empty string
        /// when the table is empty and headers are excluded.
        /// </summary>
        public async Task ExportAsCsvAsync_ShouldReturnEmptyString_WhenTableIsEmptyAndHeadersExcluded()
        {
            // Arrange
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"CREATE TABLE IF NOT EXISTS YetAnotherEmptyTable (Col1 TEXT, Col2 TEXT);";
                command.ExecuteNonQuery();
            }
            var expectedCsv = "";

            // Act
            var result = await _sut.ExportAsCsvAsync(_connection, "YetAnotherEmptyTable", false);

            // Assert
            result.Should().Be(expectedCsv);
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsSqlAsync"/> returns correct SQL INSERT statements
        /// when the table contains sample data.
        /// </summary>
        public async Task ExportAsSqlAsync_ShouldReturnCorrectSql_WhenTableHasData()
        {
            _mockLogger.LogInformation("ExportAsSqlAsync_ShouldReturnCorrectSql_WhenTableHasData called with {TableName}", _testTableName);

            // Act
            var result = await _sut.ExportAsSqlAsync(_connection, _testTableName);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("-- Export of TestUsers from SQLite");
            result.Should().Contain("-- Generated at"); // Date and time will be dynamic
            result.Should().Contain("INSERT INTO [TestUsers] ([Id], [Name], [Email]) VALUES (1, 'Alice', 'alice@example.com');");
            result.Should().Contain("INSERT INTO [TestUsers] ([Id], [Name], [Email]) VALUES (2, 'Bob', 'bob@example.com');");
            result.Should().Contain("INSERT INTO [TestUsers] ([Id], [Name], [Email]) VALUES (3, 'Charlie', NULL);");
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsSqlAsync"/> throws an <see cref="ArgumentNullException"/>
        /// when the database connection parameter is null.
        /// </summary>
        public async Task ExportAsSqlAsync_ShouldThrowArgumentNullException_WhenConnectionIsNull()
        {
            // Act & Assert
            await _sut.Awaiting(s => s.ExportAsSqlAsync(null, _testTableName))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("connection");
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsSqlAsync"/> throws an <see cref="ArgumentNullException"/>
        /// when the table name parameter is an empty string.
        /// </summary>
        public async Task ExportAsSqlAsync_ShouldThrowArgumentNullException_WhenTableNameIsEmpty()
        {
            // Act & Assert
            await _sut.Awaiting(s => s.ExportAsSqlAsync(_connection, ""))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("tableName");
        }

        [Fact]
        /// <summary>
        /// Tests that <see cref="DataExporter.ExportAsSqlAsync"/> returns only comments (no INSERT statements)
        /// when the table is empty.
        /// </summary>
        public async Task ExportAsSqlAsync_ShouldReturnOnlyComments_WhenTableIsEmpty()
        {
            // Act
            var result = await _sut.ExportAsSqlAsync(_connection, _emptyTableName);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain($"-- Export of {_emptyTableName} from SQLite");
            result.Should().Contain("-- Generated at");
            result.Should().NotContain("INSERT INTO");
        }

        /// <summary>
        /// Disposes the test resources by closing and disposing the database connection.
        /// </summary>

    [Fact]
    /// <summary>
    /// Tests that <see cref="DataExporter.ExportAsJsonLinesAsync"/> creates a valid JSON Lines (.jsonl) file
    /// with one JSON object per row when the table contains sample data.
    /// </summary>
    public async Task ExportAsJsonLinesAsync_ShouldCreateValidJsonLinesFile_WhenTableHasData()
    {
        // Arrange
        var outputPath = "/tmp/test_export.jsonl";
        if (System.IO.File.Exists(outputPath))
        {
            System.IO.File.Delete(outputPath);
        }

        // Act
        await _sut.ExportAsJsonLinesAsync(_connection, _testTableName, outputPath);

        // Assert
        System.IO.File.Exists(outputPath).Should().BeTrue();

        var lines = await System.IO.File.ReadAllLinesAsync(outputPath);
        lines.Should().HaveCount(3); // 3 rows in test table

        // Verify each line is valid JSON
        foreach (var line in lines)
        {
            var jsonDoc = JsonDocument.Parse(line);
            jsonDoc.RootElement.TryGetProperty("Id", out var idProp).Should().BeTrue();
            jsonDoc.RootElement.TryGetProperty("Name", out var nameProp).Should().BeTrue();
            jsonDoc.RootElement.TryGetProperty("Email", out var emailProp).Should().BeTrue();
        }

        // Verify first row content
        var firstLine = JsonDocument.Parse(lines[0]);
        firstLine.RootElement.GetProperty("Id").GetInt32().Should().Be(1);
        firstLine.RootElement.GetProperty("Name").GetString().Should().Be("Alice");
        firstLine.RootElement.GetProperty("Email").GetString().Should().Be("alice@example.com");

        // Verify second row content
        var secondLine = JsonDocument.Parse(lines[1]);
        secondLine.RootElement.GetProperty("Id").GetInt32().Should().Be(2);
        secondLine.RootElement.GetProperty("Name").GetString().Should().Be("Bob");
        secondLine.RootElement.GetProperty("Email").GetString().Should().Be("bob@example.com");

        // Verify third row with NULL value
        var thirdLine = JsonDocument.Parse(lines[2]);
        thirdLine.RootElement.GetProperty("Id").GetInt32().Should().Be(3);
        thirdLine.RootElement.GetProperty("Name").GetString().Should().Be("Charlie");
        thirdLine.RootElement.GetProperty("Email").ValueKind.Should().Be(JsonValueKind.Null);

        // Cleanup
        System.IO.File.Delete(outputPath);
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="DataExporter.ExportAsJsonLinesAsync"/> creates an empty JSON Lines file
    /// when the table is empty.
    /// </summary>
    public async Task ExportAsJsonLinesAsync_ShouldCreateEmptyFile_WhenTableIsEmpty()
    {
        // Arrange
        var outputPath = "/tmp/test_export_empty.jsonl";
        if (System.IO.File.Exists(outputPath))
        {
            System.IO.File.Delete(outputPath);
        }

        // Act
        await _sut.ExportAsJsonLinesAsync(_connection, _emptyTableName, outputPath);

        // Assert
        System.IO.File.Exists(outputPath).Should().BeTrue();

        var lines = await System.IO.File.ReadAllLinesAsync(outputPath);
        lines.Should().BeEmpty(); // Empty table = empty file

        // Cleanup
        System.IO.File.Delete(outputPath);
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="DataExporter.ExportAsJsonLinesAsync"/> throws an <see cref="ArgumentNullException"/>
    /// when the database connection parameter is null.
    /// </summary>
    public async Task ExportAsJsonLinesAsync_ShouldThrowArgumentNullException_WhenConnectionIsNull()
    {
        // Arrange
        var outputPath = "/tmp/test_export_null.jsonl";

        // Act & Assert
        await _sut.Awaiting(s => s.ExportAsJsonLinesAsync(null, _testTableName, outputPath))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("connection");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="DataExporter.ExportAsJsonLinesAsync"/> throws an <see cref="ArgumentNullException"/>
    /// when the table name parameter is null.
    /// </summary>
    public async Task ExportAsJsonLinesAsync_ShouldThrowArgumentNullException_WhenTableNameIsNull()
    {
        // Arrange
        var outputPath = "/tmp/test_export_null.jsonl";

        // Act & Assert
        await _sut.Awaiting(s => s.ExportAsJsonLinesAsync(_connection, null, outputPath))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("tableName");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="DataExporter.ExportAsJsonLinesAsync"/> throws an <see cref="ArgumentNullException"/>
    /// when the output path parameter is null.
    /// </summary>
    public async Task ExportAsJsonLinesAsync_ShouldThrowArgumentNullException_WhenOutputPathIsNull()
    {
        // Act & Assert
        await _sut.Awaiting(s => s.ExportAsJsonLinesAsync(_connection, _testTableName, null))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("outputPath");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="DataExporter.ExportAsJsonLinesAsync"/> throws an <see cref="ArgumentNullException"/>
    /// when the output path parameter is empty.
    /// </summary>
    public async Task ExportAsJsonLinesAsync_ShouldThrowArgumentNullException_WhenOutputPathIsEmpty()
    {
        // Arrange
        var outputPath = "";

        // Act & Assert
        await _sut.Awaiting(s => s.ExportAsJsonLinesAsync(_connection, _testTableName, outputPath))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("outputPath");
    }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}