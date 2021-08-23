#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public sealed class DataExporterTests : IDisposable {
        private readonly ILogger<DataExporter> _mockLogger;
        private readonly DataExporter _sut;
        private SQLiteConnection _connection;
        private string _testTableName = "TestUsers";
        private string _emptyTableName = "EmptyTable";

        public DataExporterTests()
        {
            _mockLogger = Substitute.For<ILogger<DataExporter>>();
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
        public async Task ExportAsJsonAsync_ShouldReturnCorrectJson_WhenTableHasData()
        {
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
        public async Task ExportAsJsonAsync_ShouldExcludeMetadata_WhenIncludeMetaIsFalse()
        {
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
        public async Task ExportAsJsonAsync_ShouldThrowArgumentNullException_WhenConnectionIsNull()
        {
            // Act & Assert
            await _sut.Awaiting(s => s.ExportAsJsonAsync(null, _testTableName))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("connection");
        }

        [Fact]
        public async Task ExportAsJsonAsync_ShouldThrowArgumentNullException_WhenTableNameIsEmpty()
        {
            // Act & Assert
            await _sut.Awaiting(s => s.ExportAsJsonAsync(_connection, ""))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("tableName");
        }

        [Fact]
        public async Task ExportAsJsonAsync_ShouldThrowArgumentNullException_WhenTableNameIsNull()
        {
            // Act & Assert
            await _sut.Awaiting(s => s.ExportAsJsonAsync(_connection, null))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("tableName");
        }

        [Fact]
        public async Task ExportAsJsonAsync_ShouldReturnEmptyDataArray_WhenTableIsEmpty()
        {
            // Act
            var result = await _sut.ExportAsJsonAsync(_connection, _emptyTableName, true);

            // Assert
            result.Should().Contain("\"RowCount\": 0");
            result.Should().Contain("\"data\": []");
        }

        [Fact]
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
        public async Task ExportAsCsvAsync_ShouldExcludeHeaders_WhenIncludeHeadersIsFalse()
        {
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
        public async Task ExportAsCsvAsync_ShouldThrowArgumentNullException_WhenConnectionIsNull()
        {
            // Act & Assert
            await _sut.Awaiting(s => s.ExportAsCsvAsync(null, _testTableName))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("connection");
        }

        [Fact]
        public async Task ExportAsCsvAsync_ShouldThrowArgumentNullException_WhenTableNameIsEmpty()
        {
            // Act & Assert
            await _sut.Awaiting(s => s.ExportAsCsvAsync(_connection, ""))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("tableName");
        }

        [Fact]
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
        public async Task ExportAsSqlAsync_ShouldReturnCorrectSql_WhenTableHasData()
        {
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
        public async Task ExportAsSqlAsync_ShouldThrowArgumentNullException_WhenConnectionIsNull()
        {
            // Act & Assert
            await _sut.Awaiting(s => s.ExportAsSqlAsync(null, _testTableName))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("connection");
        }

        [Fact]
        public async Task ExportAsSqlAsync_ShouldThrowArgumentNullException_WhenTableNameIsEmpty()
        {
            // Act & Assert
            await _sut.Awaiting(s => s.ExportAsSqlAsync(_connection, ""))
                .Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("tableName");
        }

        [Fact]
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

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
