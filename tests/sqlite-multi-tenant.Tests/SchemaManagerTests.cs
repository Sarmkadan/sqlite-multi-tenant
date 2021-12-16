#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;

/// <summary>
/// Unit tests for <see cref="SchemaManager"/> class that verify schema initialization, table management,
/// column operations, and index creation functionality for multi-tenant SQLite databases.
/// </summary>
/// <remarks>
/// Tests use in-memory SQLite databases with shared cache to ensure isolation between test runs.
/// Each test creates its own uniquely named database instance to prevent state leakage.
/// </remarks>
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
    /// <summary>
    /// Test suite for <see cref="SchemaManager"/> functionality including schema initialization, table operations,
    /// column management, and index creation for multi-tenant SQLite databases.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IDisposable"/> to properly clean up database connections after each test.
    /// </remarks>
    public sealed class SchemaManagerTests : IDisposable
    {
        private readonly ILogger<SchemaManager> _mockLogger;
        private SQLiteConnection _connection;
        private string _connectionString;
        private SchemaManager _sut;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaManagerTests"/> class.
        /// </summary>
        /// <remarks>
        /// Each test instance gets its own uniquely named shared-cache memory database,
        /// since System.Data.SQLite pools connections and a fixed name would leak state
        /// (e.g. tables/indexes) across test methods.
        /// </remarks>
        public SchemaManagerTests()
        {
            // Each test instance gets its own uniquely named shared-cache memory database,
            // since System.Data.SQLite pools connections and a fixed name would leak state
            // (e.g. tables/indexes) across test methods.
            _connectionString = $"Data Source=:memory:schemamanagertests_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            _mockLogger = Substitute.For<ILogger<SchemaManager>>();
            _sut = new SchemaManager(_mockLogger, _connectionString);

            // Initialize a connection for direct assertions
            _connection = new SQLiteConnection(_connectionString);
            _connection.Open();
        }

        /// <summary>
        /// Determines whether a table with the specified name exists in the database.
        /// </summary>
        /// <param name="tableName">Name of the table to check.</param>
        /// <returns><see langword="true"/> if the table exists; otherwise, <see langword="false"/>.</returns>
        private async Task<bool> TableExists(string tableName)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";
                var result = await command.ExecuteScalarAsync();
                return result is not null;
            }
        }

        /// <summary>
        /// Determines whether a column with the specified name exists in the specified table.
        /// </summary>
        /// <param name="tableName">Name of the table to check.</param>
        /// <param name="columnName">Name of the column to check.</param>
        /// <returns><see langword="true"/> if the column exists; otherwise, <see langword="false"/>.</returns>
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

        /// <summary>
        /// Verifies that the <see cref="SchemaManager"/> constructor throws an <see cref="ArgumentNullException"/>
        /// when a null logger is provided.
        /// </summary>
        [Fact]
        public void SchemaManager_Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            this.Invoking(_ => new SchemaManager(null, _connectionString))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        /// <summary>
        /// Verifies that the <see cref="SchemaManager"/> constructor throws an <see cref="ArgumentNullException"/>
        /// when a null connection string is provided.
        /// </summary>
        [Fact]
        public void SchemaManager_Constructor_ThrowsArgumentNullException_WhenConnectionStringIsNull()
        {
            // Act & Assert
            this.Invoking(_ => new SchemaManager(_mockLogger, null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("connectionString");
        }

        /// <summary>
        /// Verifies that calling <see cref="SchemaManager.InitializeSchemaAsync"/> creates the required tables and indexes
        /// for a new tenant in the database.
        /// </summary>
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

            _mockLogger.AssertLogged(LogLevel.Information, 1, "Schema initialized for tenant: {TenantId}", tenantId);
        }

        /// <summary>
        /// Verifies that calling <see cref="SchemaManager.InitializeSchemaAsync"/> multiple times does not recreate existing tables and indexes
        /// and does not throw exceptions.
        /// </summary>
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
            _mockLogger.AssertLogged(LogLevel.Information, 2, "Schema initialized for tenant: {TenantId}", tenantId);
        }

        /// <summary>
        /// Verifies that calling <see cref="SchemaManager.AddColumnAsync"/> adds a new column to an existing table
        /// in the database for the specified tenant.
        /// </summary>
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
            _mockLogger.AssertLogged(LogLevel.Information, 1, "Column {ColumnName} added to table {TableName} for tenant {TenantId}",
                columnName, tableName, tenantId);
        }

        /// <summary>
        /// Verifies that calling <see cref="SchemaManager.AddColumnAsync"/> returns false when attempting to add a column that already exists
        /// in the specified table, without throwing an exception.
        /// </summary>
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
            _mockLogger.AssertLogged(LogLevel.Warning, 1, "Column {ColumnName} already exists in table {TableName}",
                columnName, tableName);
        }

        /// <summary>
        /// Verifies that calling <see cref="SchemaManager.AddColumnAsync"/> throws a <see cref="SQLiteException"/> when attempting to add a column to a non-existent table.
        /// </summary>
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
            _mockLogger.AssertLoggedWithException(LogLevel.Error, 1, typeof(SQLiteException),
                "Failed to add column {ColumnName} to table {TableName}", columnName, nonExistentTable);
        }

        /// <summary>
        /// Verifies that calling <see cref="SchemaManager.RenameTableAsync"/> successfully renames an existing table in the database.
        /// </summary>
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
            _mockLogger.AssertLogged(LogLevel.Information, 1, "Table renamed from {OldName} to {NewName}",
                oldName, newName);
        }

        /// <summary>
        /// Verifies that calling <see cref="SchemaManager.RenameTableAsync"/> throws a <see cref="SQLiteException"/> when attempting to rename a non-existent table.
        /// </summary>
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
            _mockLogger.AssertLoggedWithException(LogLevel.Error, 1, typeof(SQLiteException),
                "Failed to rename table from {OldName} to {NewName}", nonExistentTable, newName);
        }

        /// <summary>
        /// Verifies that calling <see cref="SchemaManager.CreateIndexAsync"/> creates a new index on the specified table with the given column names.
        /// </summary>
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
            _mockLogger.AssertLogged(LogLevel.Information, 1, "Index {IndexName} created on table {TableName}",
                indexName, tableName);
        }

        /// <summary>
        /// Verifies that calling <see cref="SchemaManager.CreateIndexAsync"/> returns false when attempting to create an index that already exists,
        /// without throwing an exception.
        /// </summary>
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
            _mockLogger.AssertLogged(LogLevel.Warning, 1, "Index {IndexName} already exists", indexName);
        }

        /// <summary>
        /// Verifies that calling <see cref="SchemaManager.GetTablesAsync"/> returns all user-created tables in the database,
        /// excluding system tables like sqlite_sequence.
        /// </summary>
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

        /// <summary>
        /// Verifies that calling <see cref="SchemaManager.GetTablesAsync"/> returns an empty list when no tables exist in the database.
        /// </summary>
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

        /// <summary>
        /// Releases all resources used by the current test instance, including database connections.
        /// </summary>
        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}