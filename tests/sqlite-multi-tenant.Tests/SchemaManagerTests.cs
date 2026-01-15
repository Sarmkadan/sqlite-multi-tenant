#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Database;
using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using Xunit;
using System.Linq;

namespace SqliteMultiTenant.Tests
{
    public sealed class SchemaManagerTests : IDisposable {
        private readonly ILogger<SchemaManager> _mockLogger;
        private SQLiteConnection _connection;
        private string _connectionString = "Data Source=:memory:";
        private SchemaManager _sut;

        public SchemaManagerTests()
        {
            _mockLogger = Substitute.For<ILogger<SchemaManager>>();
            _sut = new SchemaManager(_mockLogger, _connectionString);

            // Initialize a connection for direct assertions
            _connection = new SQLiteConnection(_connectionString);
            _connection.Open();
        }

        private async Task<bool> TableExists(string tableName)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";
                var result = await command.ExecuteScalarAsync();
                return result is not null;
            }
        }

        private async Task<bool> ColumnExists(string tableName, string columnName)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"PRAGMA table_info({tableName});";
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader.GetString(1) == columnName) // Column name is at index 1
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        private async Task<bool> IndexExists(string indexName)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"SELECT name FROM sqlite_master WHERE type='index' AND name='{indexName}';";
                var result = await command.ExecuteScalarAsync();
                return result is not null;
            }
        }

        [Fact]
        public void SchemaManager_Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            this.Invoking(_ => new SchemaManager(null, _connectionString))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void SchemaManager_Constructor_ThrowsArgumentNullException_WhenConnectionStringIsNull()
        {
            // Act & Assert
            this.Invoking(_ => new SchemaManager(_mockLogger, null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("connectionString");
        }

        [Fact]
        public async Task InitializeSchemaAsync_CreatesTablesAndIndexes()
        {
            // Arrange
            var tenantId = "testTenant";

            // Act
            await _sut.InitializeSchemaAsync(tenantId);

            // Assert
            (await TableExists("Tenants")).Should().BeTrue();
            (await TableExists("AuditLog")).Should().BeTrue();
            (await IndexExists("idx_AuditLog_TenantId")).Should().BeTrue();
            (await IndexExists("idx_AuditLog_CreatedAt")).Should().BeTrue();

            _mockLogger.Received(1).LogInformation("Schema initialized for tenant: {TenantId}", tenantId);
        }

        [Fact]
        public async Task InitializeSchemaAsync_ShouldNotRecreateExistingTablesAndIndexes()
        {
            // Arrange
            var tenantId = "testTenant";
            await _sut.InitializeSchemaAsync(tenantId); // First initialization

            // Act (Call again)
            await _sut.InitializeSchemaAsync(tenantId);

            // Assert (should not throw, tables and indexes should still exist)
            (await TableExists("Tenants")).Should().BeTrue();
            _mockLogger.Received(2).LogInformation("Schema initialized for tenant: {TenantId}", tenantId);
        }

        [Fact]
        public async Task AddColumnAsync_AddsNewColumnToTable()
        {
            // Arrange
            var tenantId = "testTenant";
            await _sut.InitializeSchemaAsync(tenantId);
            var tableName = "Tenants";
            var columnName = "NewColumn";
            var columnDefinition = "TEXT DEFAULT 'default_value'";

            // Act
            var result = await _sut.AddColumnAsync(tenantId, tableName, columnName, columnDefinition);

            // Assert
            result.Should().BeTrue();
            (await ColumnExists(tableName, columnName)).Should().BeTrue();
            _mockLogger.Received(1).LogInformation("Column {ColumnName} added to table {TableName} for tenant {TenantId}",
                columnName, tableName, tenantId);
        }

        [Fact]
        public async Task AddColumnAsync_ReturnsFalse_WhenColumnAlreadyExists()
        {
            // Arrange
            var tenantId = "testTenant";
            await _sut.InitializeSchemaAsync(tenantId);
            var tableName = "Tenants";
            var columnName = "Name"; // Existing column
            var columnDefinition = "TEXT";

            // Act
            var result = await _sut.AddColumnAsync(tenantId, tableName, columnName, columnDefinition);

            // Assert
            result.Should().BeFalse();
            _mockLogger.Received(1).LogWarning("Column {ColumnName} already exists in table {TableName}",
                columnName, tableName);
        }

        [Fact]
        public async Task AddColumnAsync_ThrowsExceptionAndLogs_WhenTableNameDoesNotExist()
        {
            // Arrange
            var tenantId = "testTenant";
            var nonExistentTable = "NonExistentTable";
            var columnName = "TestCol";
            var columnDefinition = "TEXT";

            // Act
            await _sut.Awaiting(s => s.AddColumnAsync(tenantId, nonExistentTable, columnName, columnDefinition))
                .Should().ThrowAsync<SQLiteException>();

            // Assert
            _mockLogger.Received(1).LogError(Arg.Any<SQLiteException>(),
                "Failed to add column {ColumnName} to table {TableName}", columnName, nonExistentTable);
        }

        [Fact]
        public async Task RenameTableAsync_RenamesTableSuccessfully()
        {
            // Arrange
            var tenantId = "testTenant";
            await _sut.InitializeSchemaAsync(tenantId); // Creates Tenants table
            var oldName = "Tenants";
            var newName = "Customers";

            // Act
            await _sut.RenameTableAsync(oldName, newName);

            // Assert
            (await TableExists(oldName)).Should().BeFalse();
            (await TableExists(newName)).Should().BeTrue();
            _mockLogger.Received(1).LogInformation("Table renamed from {OldName} to {NewName}", oldName, newName);
        }

        [Fact]
        public async Task RenameTableAsync_ThrowsExceptionAndLogs_WhenOldTableDoesNotExist()
        {
            // Arrange
            var nonExistentTable = "NonExistent";
            var newName = "NewTable";

            // Act
            await _sut.Awaiting(s => s.RenameTableAsync(nonExistentTable, newName))
                .Should().ThrowAsync<SQLiteException>();

            // Assert
            _mockLogger.Received(1).LogError(Arg.Any<SQLiteException>(),
                "Failed to rename table from {OldName} to {NewName}", nonExistentTable, newName);
        }

        [Fact]
        public async Task CreateIndexAsync_CreatesNewIndex()
        {
            // Arrange
            var tenantId = "testTenant";
            await _sut.InitializeSchemaAsync(tenantId);
            var tableName = "Tenants";
            var indexName = "idx_Tenants_Name";
            var columns = new[] { "Name" };

            // Act
            var result = await _sut.CreateIndexAsync(tableName, indexName, columns);

            // Assert
            result.Should().BeTrue();
            (await IndexExists(indexName)).Should().BeTrue();
            _mockLogger.Received(1).LogInformation("Index {IndexName} created on table {TableName}", indexName, tableName);
        }

        [Fact]
        public async Task CreateIndexAsync_ReturnsFalse_WhenIndexAlreadyExists()
        {
            // Arrange
            var tenantId = "testTenant";
            await _sut.InitializeSchemaAsync(tenantId);
            var tableName = "AuditLog";
            var indexName = "idx_AuditLog_TenantId"; // Existing index
            var columns = new[] { "TenantId" };

            // Act
            var result = await _sut.CreateIndexAsync(tableName, indexName, columns);

            // Assert
            result.Should().BeFalse();
            _mockLogger.Received(1).LogWarning("Index {IndexName} already exists", indexName);
        }

        [Fact]
        public async Task GetTablesAsync_ReturnsAllUserTables()
        {
            // Arrange
            var tenantId = "testTenant";
            await _sut.InitializeSchemaAsync(tenantId); // Creates Tenants and AuditLog

            // Act
            var tables = await _sut.GetTablesAsync();

            // Assert
            tables.Should().Contain("Tenants");
            tables.Should().Contain("AuditLog");
            tables.Should().NotContain("sqlite_sequence"); // Internal SQLite table
            tables.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTablesAsync_ReturnsEmptyList_WhenNoTablesExist()
        {
            // Arrange - use a fresh connection string without initialization
            var newConnectionString = "Data Source=:memory:;Mode=Memory;Cache=Shared";
            var newSut = new SchemaManager(_mockLogger, newConnectionString);

            // Act
            var tables = await newSut.GetTablesAsync();

            // Assert
            tables.Should().BeEmpty();
        }


        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
