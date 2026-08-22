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
    public class BulkInsertBuilderTests
    {
        private readonly SQLiteConnection _connection;
        private readonly ILogger<BulkInsertBuilder> _logger;
        private const string TestTable = "TestTable";

        public BulkInsertBuilderTests()
        {
            _connection = new SQLiteConnection("Data Source=:memory:");
            _logger = Substitute.For<ILogger<BulkInsertBuilder>>();
        }

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
