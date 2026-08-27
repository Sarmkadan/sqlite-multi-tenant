using SqliteMultiTenant.Operations;
using FluentAssertions;
using Xunit;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace SqliteMultiTenant.Tests.Operations
{
    /// <summary>
    /// Contains unit tests for the <see cref="BulkInsertBuilder"/> class.
    /// </summary>
    public class BulkInsertBuilderTests
    {
        private readonly SQLiteConnection _connection;
        private readonly ILogger<BulkInsertBuilder> _logger;
        private const string TestTable = "TestTable";

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkInsertBuilderTests"/> class.
        /// Sets up an in-memory SQLite connection and a mock logger for testing.
        /// </summary>
        public BulkInsertBuilderTests()
        {
            _connection = new SQLiteConnection("Data Source=:memory:");
            _logger = Substitute.For<ILogger<BulkInsertBuilder>>();
        }

        /// <summary>
        /// Tests that the BulkInsertBuilder constructor throws an ArgumentNullException when the connection parameter is null.
        /// </summary>
        [Fact]
        public void BulkInsertBuilder_Constructor_WithNullConnection_ThrowsArgumentNullException()
        {
            _logger.LogInformation("Executing test: {TestName}", nameof(BulkInsertBuilder_Constructor_WithNullConnection_ThrowsArgumentNullException));

            // Act
            Action act = () => new BulkInsertBuilder(null!, _logger, TestTable);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("connection");

            _logger.LogInformation("Test completed: {TestName}", nameof(BulkInsertBuilder_Constructor_WithNullConnection_ThrowsArgumentNullException));
        }

        /// <summary>
        /// Tests that the BulkInsertBuilder constructor throws an ArgumentNullException when the logger parameter is null.
        /// </summary>
        [Fact]
        public void BulkInsertBuilder_Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            _logger.LogInformation("Executing test: {TestName}", nameof(BulkInsertBuilder_Constructor_WithNullLogger_ThrowsArgumentNullException));

            // Act
            Action act = () => new BulkInsertBuilder(_connection, null!, TestTable);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");

            _logger.LogInformation("Test completed: {TestName}", nameof(BulkInsertBuilder_Constructor_WithNullLogger_ThrowsArgumentNullException));
        }

        /// <summary>
        /// Tests that the BulkInsertBuilder constructor throws an ArgumentException when the table name is empty.
        /// </summary>
        [Fact]
        public void BulkInsertBuilder_Constructor_WithEmptyTableName_ThrowsArgumentException()
        {
            _logger.LogInformation("Executing test: {TestName}", nameof(BulkInsertBuilder_Constructor_WithEmptyTableName_ThrowsArgumentException));

            // Act
            Action act = () => new BulkInsertBuilder(_connection, _logger, "");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty*");

            _logger.LogInformation("Test completed: {TestName}", nameof(BulkInsertBuilder_Constructor_WithEmptyTableName_ThrowsArgumentException));
        }

        /// <summary>
        /// Tests that the BulkInsertBuilder constructor throws an ArgumentException when the table name is null.
        /// </summary>
        [Fact]
        public void BulkInsertBuilder_Constructor_WithNullTableName_ThrowsArgumentException()
        {
            _logger.LogInformation("Executing test: {TestName}", nameof(BulkInsertBuilder_Constructor_WithNullTableName_ThrowsArgumentException));

            // Act
            Action act = () => new BulkInsertBuilder(_connection, _logger, null!);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty*");

            _logger.LogInformation("Test completed: {TestName}", nameof(BulkInsertBuilder_Constructor_WithNullTableName_ThrowsArgumentException));
        }

        /// <summary>
        /// Tests that the BulkInsertBuilder constructor throws an ArgumentException when the table name consists only of whitespace.
        /// </summary>
        [Fact]
        public void BulkInsertBuilder_Constructor_WithWhitespaceTableName_ThrowsArgumentException()
        {
            _logger.LogInformation("Executing test: {TestName}", nameof(BulkInsertBuilder_Constructor_WithWhitespaceTableName_ThrowsArgumentException));

            // Act
            Action act = () => new BulkInsertBuilder(_connection, _logger, "   ");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty*");

            _logger.LogInformation("Test completed: {TestName}", nameof(BulkInsertBuilder_Constructor_WithWhitespaceTableName_ThrowsArgumentException));
        }

        /// <summary>
        /// Tests that the AddRecord method throws an ArgumentNullException when the record parameter is null.
        /// </summary>
        [Fact]
        public void BulkInsertBuilder_AddRecord_WithNullRecord_ThrowsArgumentNullException()
        {
            _logger.LogInformation("Executing test: {TestName}", nameof(BulkInsertBuilder_AddRecord_WithNullRecord_ThrowsArgumentNullException));

            // Arrange
            var builder = new BulkInsertBuilder(_connection, _logger, TestTable);

            // Act
            Action act = () => builder.AddRecord(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("record");

            _logger.LogInformation("Test completed: {TestName}", nameof(BulkInsertBuilder_AddRecord_WithNullRecord_ThrowsArgumentNullException));
        }

        /// <summary>
        /// Tests that the AddRecords method throws an ArgumentNullException when the records parameter is null.
        /// </summary>
        [Fact]
        public void BulkInsertBuilder_AddRecords_WithNullRecords_ThrowsArgumentNullException()
        {
            _logger.LogInformation("Executing test: {TestName}", nameof(BulkInsertBuilder_AddRecords_WithNullRecords_ThrowsArgumentNullException));

            // Arrange
            var builder = new BulkInsertBuilder(_connection, _logger, TestTable);

            // Act
            Action act = () => builder.AddRecords(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("records");

            _logger.LogInformation("Test completed: {TestName}", nameof(BulkInsertBuilder_AddRecords_WithNullRecords_ThrowsArgumentNullException));
        }

        /// <summary>
        /// Tests that the GenerateSqlStatements method returns an empty string when there are no records.
        /// </summary>
        [Fact]
        public void BulkInsertBuilder_GenerateSqlStatements_WithEmptyRecords_ReturnsEmptyString()
        {
            _logger.LogInformation("Executing test: {TestName}", nameof(BulkInsertBuilder_GenerateSqlStatements_WithEmptyRecords_ReturnsEmptyString));

            // Arrange
            var builder = new BulkInsertBuilder(_connection, _logger, TestTable);

            // Act
            var sql = builder.GenerateSqlStatements();

            // Assert
            sql.Should().BeEmpty();

            _logger.LogInformation("Test completed: {TestName}", nameof(BulkInsertBuilder_GenerateSqlStatements_WithEmptyRecords_ReturnsEmptyString));
        }

        /// <summary>
        /// Tests that the GenerateSqlStatements method generates correct SQL for a single record.
        /// </summary>
        [Fact]
        public void BulkInsertBuilder_GenerateSqlStatements_WithSingleRecord_GeneratesCorrectSql()
        {
            _logger.LogInformation("Executing test: {TestName}", nameof(BulkInsertBuilder_GenerateSqlStatements_WithSingleRecord_GeneratesCorrectSql));

            // Arrange
            var builder = new BulkInsertBuilder(_connection, _logger, TestTable);
            var record = new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", "Test" },
                { "Value", 42.5 }
            };
            builder.AddRecord(record);

            // Act
            var sql = builder.GenerateSqlStatements();

            // Assert
            sql.Should().StartWith($"INSERT INTO [{TestTable}] ([Id], [Name], [Value]) VALUES");
            sql.Should().Contain("'1'");
            sql.Should().Contain("'Test'");
            sql.Should().Contain("'42.5'");

            _logger.LogInformation("Test completed: {TestName}", nameof(BulkInsertBuilder_GenerateSqlStatements_WithSingleRecord_GeneratesCorrectSql));
        }

        /// <summary>
        /// Tests that the GenerateSqlStatements method generates correct SQL for multiple records.
        /// </summary>
        [Fact]
        public void BulkInsertBuilder_GenerateSqlStatements_WithMultipleRecords_GeneratesCorrectSql()
        {
            _logger.LogInformation("Executing test: {TestName}", nameof(BulkInsertBuilder_GenerateSqlStatements_WithMultipleRecords_GeneratesCorrectSql));

            // Arrange
            var builder = new BulkInsertBuilder(_connection, _logger, TestTable);
            builder.AddRecord(new Dictionary<string, object> { { "Id", 1 }, { "Name", "Test1" } });
            builder.AddRecord(new Dictionary<string, object> { { "Id", 2 }, { "Name", "Test2" } });
            builder.AddRecord(new Dictionary<string, object> { { "Id", 3 }, { "Name", "Test3" } });

            // Act
            var sql = builder.GenerateSqlStatements();

            // Assert
            sql.Should().Contain("'1', 'Test1'");
            sql.Should().Contain("'2', 'Test2'");
            sql.Should().Contain("'3', 'Test3'");

            _logger.LogInformation("Test completed: {TestName}", nameof(BulkInsertBuilder_GenerateSqlStatements_WithMultipleRecords_GeneratesCorrectSql));
        }

        /// <summary>
        /// Tests that the GenerateSqlStatements method correctly handles null and DBNull values in records.
        /// </summary>
        [Fact]
        public void BulkInsertBuilder_GenerateSqlStatements_WithNullValues_HandlesCorrectly()
        {
            _logger.LogInformation("Executing test: {TestName}", nameof(BulkInsertBuilder_GenerateSqlStatements_WithNullValues_HandlesCorrectly));

            // Arrange
            var builder = new BulkInsertBuilder(_connection, _logger, TestTable);
            var record = new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", null },
                { "Value", DBNull.Value }
            };
            builder.AddRecord(record);

            // Act
            var sql = builder.GenerateSqlStatements();

            // Assert
            sql.Should().StartWith($"INSERT INTO [{TestTable}] ([Id], [Name], [Value]) VALUES");
            sql.Should().Contain("'1'");
            sql.Should().Contain("NULL");

            _logger.LogInformation("Test completed: {TestName}", nameof(BulkInsertBuilder_GenerateSqlStatements_WithNullValues_HandlesCorrectly));
        }

        /// <summary>
        /// Tests that the GenerateSqlStatements method correctly escapes special characters in record values.
        /// </summary>
        [Fact]
        public void BulkInsertBuilder_GenerateSqlStatements_WithSpecialCharactersInValues_EscapesCorrectly()
        {
            _logger.LogInformation("Executing test: {TestName}", nameof(BulkInsertBuilder_GenerateSqlStatements_WithSpecialCharactersInValues_EscapesCorrectly));

            // Arrange
            var builder = new BulkInsertBuilder(_connection, _logger, TestTable);
            var record = new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", "Test'O''Leary" },
                { "Description", "It's a \"test\"" }
            };
            builder.AddRecord(record);

            // Act
            var sql = builder.GenerateSqlStatements();

            // Assert
            sql.Should().Contain("'Test''O''''Leary'");
            sql.Should().Contain("It''s");

            _logger.LogInformation("Test completed: {TestName}", nameof(BulkInsertBuilder_GenerateSqlStatements_WithSpecialCharactersInValues_EscapesCorrectly));
        }

        /// <summary>
        /// Tests that the AddRecord method returns the same builder instance to support fluent interface.
        /// </summary>
        [Fact]
        public void BulkInsertBuilder_AddRecord_ReturnsBuilderForFluentInterface()
        {
            _logger.LogInformation("Executing test: {TestName}", nameof(BulkInsertBuilder_AddRecord_ReturnsBuilderForFluentInterface));

            // Arrange
            var builder = new BulkInsertBuilder(_connection, _logger, TestTable);
            var record = new Dictionary<string, object> { { "Id", 1 } };

            // Act
            var result = builder.AddRecord(record);

            // Assert
            result.Should().BeSameAs(builder);

            _logger.LogInformation("Test completed: {TestName}", nameof(BulkInsertBuilder_AddRecord_ReturnsBuilderForFluentInterface));
        }

        /// <summary>
        /// Tests that the AddRecords method returns the same builder instance to support fluent interface.
        /// </summary>
        [Fact]
        public void BulkInsertBuilder_AddRecords_ReturnsBuilderForFluentInterface()
        {
            _logger.LogInformation("Executing test: {TestName}", nameof(BulkInsertBuilder_AddRecords_ReturnsBuilderForFluentInterface));

            // Arrange
            var builder = new BulkInsertBuilder(_connection, _logger, TestTable);
            var records = new List<Dictionary<string, object>>
            {
                new() { { "Id", 1 } },
                new() { { "Id", 2 } }
            };

            // Act
            var result = builder.AddRecords(records);

            // Assert
            result.Should().BeSameAs(builder);

            _logger.LogInformation("Test completed: {TestName}", nameof(BulkInsertBuilder_AddRecords_ReturnsBuilderForFluentInterface));
        }
    }
}