#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using SqliteMultiTenant.DataOperations;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Contains unit tests for the <see cref="QueryBuilder"/>, <see cref="InsertBuilder"/>, and <see cref="UpdateBuilder"/> classes.
/// Tests verify that query builders correctly construct SQL queries and handle parameters for SELECT, INSERT, UPDATE operations.
/// </summary>
public sealed class QueryBuilderTests {
    /// <summary>
    /// Tests that building an empty query with a valid table name returns a valid SELECT statement.
    /// Verifies the basic constructor and Build method functionality.
    /// </summary>
    [Fact]
    public void Build_EmptyQuery_ReturnsValidString()
    {
        // Arrange
        var builder = new QueryBuilder("MyTable");

        // Act
        var result = builder.Build();

        // Assert
        result.Should().Be("SELECT * FROM [MyTable]");
    }

    /// <summary>
    /// Tests that ApplyParameters throws ArgumentNullException when passed a null SQLiteCommand.
    /// Ensures null safety for parameter application.
    /// </summary>
    [Fact]
    public void ApplyParameters_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new QueryBuilder("MyTable");

        // Act
        Action action = () => builder.ApplyParameters(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>().WithMessage("Value cannot be null. (Parameter 'command')");
    }

    /// <summary>
    /// Tests that building a query with explicit column selection returns the correct SELECT statement.
    /// Verifies that the Select method properly formats column names and builds the query string.
    /// </summary>
    [Fact]
    public void Build_WithSelectAndFrom_ReturnsCorrectQuery()
    {
        // Arrange
        var builder = new QueryBuilder("Users").Select("Id", "Name");

        // Act
        var result = builder.Build();

        // Assert
        result.Should().Be("SELECT [Id], [Name] FROM [Users]");
    }
    
    /// <summary>
    /// Tests that ApplyParameters does not throw when passed a valid SQLiteCommand.
    /// Verifies that parameter application works correctly with a properly initialized command.
    /// </summary>
    [Fact]
    public void ApplyParameters_WithValidCommand_DoesNotThrow()
    {
        // Arrange
        var builder = new QueryBuilder("MyTable");
        using var cmd = new SQLiteCommand();
        
        // Act
        Action action = () => builder.ApplyParameters(cmd);
        
        // Assert
        action.Should().NotThrow();
    }
    
    /// <summary>
    /// Tests that a QueryBuilder instance can be successfully created with a valid table name.
    /// Verifies the basic constructor functionality.
    /// </summary>
    [Fact]
    public void QueryBuilder_Instance_ShouldBeCreatedSuccessfully()
    {
        // Act
        var builder = new QueryBuilder("TestTable");
        
        // Assert
        builder.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that QueryBuilder constructor throws ArgumentException when an empty table name is provided.
    /// Ensures validation prevents invalid table names from being used.
    /// </summary>
    [Fact]
    public void QueryBuilder_Constructor_ThrowsArgumentException_WhenTableNameIsEmpty()
    {
        // Act
        Action act = () => new QueryBuilder("");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Table name cannot be empty (Parameter 'tableName')");
    }

    /// <summary>
    /// Tests that selecting a single column builds the correct SELECT query.
    /// Verifies that the Select method with a single column parameter generates the expected SQL.
    /// </summary>
    [Fact]
    public void Select_SingleColumn_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Users").Select("Id").Build();
        query.Should().Be("SELECT [Id] FROM [Users]");
    }

    /// <summary>
    /// Tests that selecting multiple columns builds the correct SELECT query.
    /// Verifies that the Select method with multiple column parameters generates the expected SQL with comma-separated columns.
    /// </summary>
    [Fact]
    public void Select_MultipleColumns_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Users").Select("Id", "Name").Build();
        query.Should().Be("SELECT [Id], [Name] FROM [Users]");
    }

    /// <summary>
    /// Tests that calling Select with no columns defaults to SELECT *.
    /// Verifies the fallback behavior when no specific columns are selected.
    /// </summary>
    [Fact]
    public void Select_NoColumns_DefaultsToSelectAll()
    {
        var query = new QueryBuilder("Users").Select().Build();
        query.Should().Be("SELECT * FROM [Users]");
    }

    /// <summary>
    /// Tests that a single WHERE condition builds the correct query.
    /// Verifies that the Where method correctly appends a WHERE clause to the SQL query.
    /// </summary>
    [Fact]
    public void Where_SingleCondition_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Users").Where("Age > 18").Build();
        query.Should().Be("SELECT * FROM [Users] WHERE Age > 18");
    }

    /// <summary>
    /// Tests that multiple WHERE conditions with AND logic build the correct query.
    /// Verifies that the Where method followed by And method correctly constructs a WHERE clause with AND conditions.
    /// </summary>
    [Fact]
    public void Where_MultipleConditionsWithAnd_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Users")
            .Where("Age > @age", ("age", 18))
            .And("Status = @status", ("status", "active"))
            .Build();
        query.Should().Be("SELECT * FROM [Users] WHERE (Age > @age) AND (Status = @status)");
    }

    /// <summary>
    /// Tests that multiple WHERE conditions with OR logic build the correct query.
    /// Verifies that the Where method followed by Or method correctly constructs a WHERE clause with OR conditions.
    /// </summary>
    [Fact]
    public void Where_MultipleConditionsWithOr_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Users")
            .Where("Age > @age", ("age", 60))
            .Or("Status = @status", ("status", "retired"))
            .Build();
        query.Should().Be("SELECT * FROM [Users] WHERE (Age > @age) OR (Status = @status)");
    }

    /// <summary>
    /// Tests that WHERE conditions with parameters are applied correctly to the SQLite command.
    /// Verifies that parameters are properly added to the SQLiteCommand when using Where with parameter placeholders.
    /// </summary>
    [Fact]
    public void Where_WithParameters_AppliesParametersCorrectly()
    {
        // Test implicitly via ApplyParameters test later
        var builder = new QueryBuilder("Products").Where("Price > @price", ("price", 100));
        builder.Build(); // To set internal parameters list
        using var cmd = new SQLiteCommand();
        builder.ApplyParameters(cmd);
        cmd.Parameters.Cast<SQLiteParameter>().Should().Contain(p => p.ParameterName == "@price" && p.Value.Equals(100));
    }

    /// <summary>
    /// Tests that an INNER JOIN clause builds the correct query.
    /// Verifies that the InnerJoin method correctly appends an INNER JOIN clause with the specified join condition.
    /// </summary>
    [Fact]
    public void InnerJoin_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Orders")
            .Select("Orders.Id", "Customers.Name")
            .InnerJoin("Customers", "Orders.CustomerId = Customers.Id")
            .Build();
        query.Should().Be("SELECT [Orders.Id], [Customers.Name] FROM [Orders] INNER JOIN Customers ON Orders.CustomerId = Customers.Id");
    }

    /// <summary>
    /// Tests that a LEFT JOIN clause builds the correct query.
    /// Verifies that the LeftJoin method correctly appends a LEFT JOIN clause with the specified join condition.
    /// </summary>
    [Fact]
    public void LeftJoin_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Products")
            .Select("Products.Name", "Categories.CategoryName")
            .LeftJoin("Categories", "Products.CategoryId = Categories.Id")
            .Build();
        query.Should().Be("SELECT [Products.Name], [Categories.CategoryName] FROM [Products] LEFT JOIN Categories ON Products.CategoryId = Categories.Id");
    }

    /// <summary>
    /// Tests that a single ORDER BY column builds the correct query with ASC direction.
    /// Verifies that the OrderBy method correctly appends an ORDER BY clause with ASC sorting.
    /// </summary>
    [Fact]
    public void OrderBy_SingleColumn_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Users").OrderBy("Name").Build();
        query.Should().Be("SELECT * FROM [Users] ORDER BY [Name] ASC");
    }

    /// <summary>
    /// Tests that multiple ORDER BY columns build the correct query with mixed directions.
    /// Verifies that multiple OrderBy calls correctly append an ORDER BY clause with multiple columns and directions.
    /// </summary>
    [Fact]
    public void OrderBy_MultipleColumns_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Users")
            .OrderBy("Name")
            .OrderBy("Age", "DESC")
            .Build();
        query.Should().Be("SELECT * FROM [Users] ORDER BY [Name] ASC, [Age] DESC");
    }

    /// <summary>
    /// Tests that a LIMIT clause builds the correct query.
    /// Verifies that the Limit method correctly appends a LIMIT clause to the SQL query.
    /// </summary>
    [Fact]
    public void Limit_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Products").Limit(10).Build();
        query.Should().Be("SELECT * FROM [Products] LIMIT 10");
    }

    /// <summary>
    /// Tests that an OFFSET clause builds the correct query.
    /// Verifies that the Offset method correctly appends an OFFSET clause to the SQL query.
    /// </summary>
    [Fact]
    public void Offset_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Products").Offset(5).Build();
        query.Should().Be("SELECT * FROM [Products] OFFSET 5");
    }

    /// <summary>
    /// Tests that combined LIMIT and OFFSET clauses build the correct query.
    /// Verifies that Limit and Offset methods work together to create a complete pagination query.
    /// </summary>
    [Fact]
    public void LimitAndOffset_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Products").Limit(10).Offset(5).Build();
        query.Should().Be("SELECT * FROM [Products] LIMIT 10 OFFSET 5");
    }

    /// <summary>
    /// Tests that the Reset method clears all query builder state and returns to a default state.
    /// Verifies that Reset properly clears SELECT, WHERE, ORDER BY, LIMIT, and OFFSET clauses.
    /// </summary>
    [Fact]
    public void Reset_ClearsAllStates()
    {
        // Arrange
        var builder = new QueryBuilder("InitialTable")
            .Select("Col1")
            .Where("Id = @id", ("id", 1))
            .OrderBy("Col1")
            .Limit(1);

        // Act
        builder.Reset();
        var query = builder.Build();

        // Assert - should revert to a default state (just select all from base table)
        query.Should().Be("SELECT * FROM [InitialTable]");
    }

    /// <summary>
    /// Tests that a complex query with multiple clauses builds the correct query string.
    /// Verifies the complete QueryBuilder functionality including SELECT, JOIN, WHERE, ORDER BY, LIMIT, and OFFSET.
    /// </summary>
    [Fact]
    public void Build_ComplexQuery_ReturnsCorrectString()
    {
        var query = new QueryBuilder("Orders")
            .Select("o.Id", "c.Name", "o.Amount")
            .InnerJoin("Customers c", "o.CustomerId = c.Id")
            .Where("o.Amount > @minAmount", ("minAmount", 100))
            .And("o.OrderDate > @startDate", ("startDate", "2023-01-01"))
            .OrderBy("o.OrderDate", "DESC")
            .Limit(5)
            .Offset(10)
            .Build();

        var expected = "SELECT [o.Id], [c.Name], [o.Amount] FROM [Orders] INNER JOIN Customers c ON o.CustomerId = c.Id WHERE (o.Amount > @minAmount) AND (o.OrderDate > @startDate) ORDER BY [o.OrderDate] DESC LIMIT 5 OFFSET 10";
        query.Should().Be(expected);
    }
    
    /// <summary>
    /// Tests that ApplyParameters correctly adds parameters to a real SQLiteCommand.
    /// Verifies that parameters from WHERE clauses are properly applied to the SQLiteCommand object.
    /// </summary>
    [Fact]
    public void ApplyParameters_WithRealCommandAndParameters_AddsParameters()
    {
        // Arrange
        var builder = new QueryBuilder("Users")
            .Where("Id = @id", ("id", 123))
            .And("Name = @name", ("name", "TestUser"));
        
        using var command = new SQLiteCommand();
        
        // Act
        builder.ApplyParameters(command);
        
        // Assert
        command.Parameters.Cast<SQLiteParameter>().Should().Contain(p => p.ParameterName == "@id" && (int)p.Value == 123);
        command.Parameters.Cast<SQLiteParameter>().Should().Contain(p => p.ParameterName == "@name" && (string)p.Value == "TestUser");
    }

    /// <summary>
    /// Tests that InsertBuilder constructor throws ArgumentException when an empty table name is provided.
    /// Ensures validation prevents invalid table names from being used in INSERT operations.
    /// </summary>
    [Fact]
    public void InsertBuilder_Constructor_ThrowsArgumentException_WhenTableNameIsEmpty()
    {
        // Act
        Action act = () => new InsertBuilder("");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Table name cannot be empty (Parameter 'tableName')");
    }

    /// <summary>
    /// Tests that InsertBuilder Build throws InvalidOperationException when no values have been specified.
    /// Verifies that INSERT operations require at least one column-value pair to be valid.
    /// </summary>
    [Fact]
    public void InsertBuilder_Build_ThrowsInvalidOperationException_WhenNoValues()
    {
        // Arrange
        var builder = new InsertBuilder("MyTable");

        // Act
        Action act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("No values specified for insert");
    }

    /// <summary>
    /// Tests that InsertBuilder with a single value builds the correct INSERT query and parameter dictionary.
    /// Verifies that the Value method correctly constructs the INSERT statement and parameter collection.
    /// </summary>
    [Fact]
    public void InsertBuilder_Build_SingleValue_BuildsCorrectQueryAndParameters()
    {
        // Arrange
        var builder = new InsertBuilder("Users").Value("Name", "John Doe");

        // Act
        var (query, parameters) = builder.Build();

        // Assert
        query.Should().Be("INSERT INTO [Users] ([Name]) VALUES (@Name)");
        parameters.Should().ContainKey("Name").And.ContainValue("John Doe");
    }

    /// <summary>
    /// Tests that InsertBuilder with multiple values builds the correct INSERT query and parameter dictionary.
    /// Verifies that multiple Value calls correctly construct the INSERT statement with multiple columns and parameter placeholders.
    /// </summary>
    [Fact]
    public void InsertBuilder_Build_MultipleValues_BuildsCorrectQueryAndParameters()
    {
        // Arrange
        var builder = new InsertBuilder("Users")
            .Value("Name", "Jane Doe")
            .Value("Age", 30);

        // Act
        var (query, parameters) = builder.Build();

        // Assert
        query.Should().Be("INSERT INTO [Users] ([Name], [Age]) VALUES (@Name, @Age)");
        parameters.Should().ContainKey("Name").And.ContainValue("Jane Doe");
        parameters.Should().ContainKey("Age").And.ContainValue(30);
    }

    /// <summary>
    /// Tests that InsertBuilder Value method handles DBNull values correctly.
    /// Verifies that null values are properly converted to DBNull.Value for SQL parameter binding.
    /// </summary>
    [Fact]
    public void InsertBuilder_Value_HandlesDbNullCorrectly()
    {
        // Arrange
        var builder = new InsertBuilder("Users").Value("Name", null);

        // Act
        var (query, parameters) = builder.Build();

        // Assert
        query.Should().Be("INSERT INTO [Users] ([Name]) VALUES (@Name)");
        parameters.Should().ContainKey("Name").And.ContainValue(DBNull.Value);
    }

    /// <summary>
    /// Tests that UpdateBuilder constructor throws ArgumentException when an empty table name is provided.
    /// Ensures validation prevents invalid table names from being used in UPDATE operations.
    /// </summary>
    [Fact]
    public void UpdateBuilder_Constructor_ThrowsArgumentException_WhenTableNameIsEmpty()
    {
        // Act
        Action act = () => new UpdateBuilder("");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Table name cannot be empty (Parameter 'tableName')");
    }

    /// <summary>
    /// Tests that UpdateBuilder Build throws InvalidOperationException when no values have been specified.
    /// Verifies that UPDATE operations require at least one SET clause to be valid.
    /// </summary>
    [Fact]
    public void UpdateBuilder_Build_ThrowsInvalidOperationException_WhenNoValues()
    {
        // Arrange
        var builder = new UpdateBuilder("MyTable").Where("Id = 1");

        // Act
        Action act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("No values specified for update");
    }

    /// <summary>
    /// Tests that UpdateBuilder Build throws InvalidOperationException when no WHERE clause is specified.
    /// Verifies that UPDATE operations require a WHERE condition for safety to prevent accidental updates to all rows.
    /// </summary>
    [Fact]
    public void UpdateBuilder_Build_ThrowsInvalidOperationException_WhenNoWhereClause()
    {
        // Arrange
        var builder = new UpdateBuilder("MyTable").Set("Name", "New Name");

        // Act
        Action act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("WHERE condition is required for safety");
    }

    /// <summary>
    /// Tests that UpdateBuilder with a single SET and WHERE clause builds the correct UPDATE query and parameter dictionary.
    /// Verifies that Set and Where methods correctly construct the UPDATE statement with parameter placeholders.
    /// </summary>
    [Fact]
    public void UpdateBuilder_Build_SingleSetWithWhere_BuildsCorrectQueryAndParameters()
    {
        // Arrange
        var builder = new UpdateBuilder("Users")
            .Set("Name", "Updated Name")
            .Where("Id = @id");

        // Act
        var (query, parameters) = builder.Build();

        // Assert
        query.Should().Be("UPDATE [Users] SET [Name] = @Name WHERE Id = @id");
        parameters.Should().ContainKey("Name").And.ContainValue("Updated Name");
    }

    /// <summary>
    /// Tests that UpdateBuilder with multiple SET clauses and a WHERE clause builds the correct UPDATE query and parameter dictionary.
    /// Verifies that multiple Set calls followed by Where correctly construct the UPDATE statement with multiple SET clauses.
    /// </summary>
    [Fact]
    public void UpdateBuilder_Build_MultipleSetsWithWhere_BuildsCorrectQueryAndParameters()
    {
        // Arrange
        var builder = new UpdateBuilder("Users")
            .Set("Name", "New Name")
            .Set("Age", 40)
            .Where("Id = @id");

        // Act
        var (query, parameters) = builder.Build();

        // Assert
        query.Should().Be("UPDATE [Users] SET [Name] = @Name, [Age] = @Age WHERE Id = @id");
        parameters.Should().ContainKey("Name").And.ContainValue("New Name");
        parameters.Should().ContainKey("Age").And.ContainValue(40);
    }

    /// <summary>
    /// Tests that QueryBuilder OrderBy throws ArgumentException when an invalid direction is provided.
    /// Verifies that OrderBy only accepts ASC or DESC as valid directions.
    /// </summary>
    [Fact]
    public void QueryBuilder_OrderBy_ThrowsArgumentException_WhenInvalidDirection()
    {
        // Act
        Action act = () => new QueryBuilder("Users").OrderBy("Name", "INVALID");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Direction must be ASC or DESC (Parameter 'direction')");
    }

    /// <summary>
    /// Tests that QueryBuilder Limit throws ArgumentException when zero or negative limit values are provided.
    /// Verifies that LIMIT must be greater than 0 for valid pagination.
    /// </summary>
    [Fact]
    public void QueryBuilder_Limit_ThrowsArgumentException_WhenLimitIsZeroOrNegative()
    {
        // Act & Assert
        new QueryBuilder("Users").Invoking(q => q.Limit(0))
            .Should().Throw<ArgumentException>()
            .WithParameterName("limit")
            .WithMessage("Limit must be greater than 0 (Parameter 'limit')");

        new QueryBuilder("Users").Invoking(q => q.Limit(-1))
            .Should().Throw<ArgumentException>()
            .WithParameterName("limit")
            .WithMessage("Limit must be greater than 0 (Parameter 'limit')");
    }

    /// <summary>
    /// Tests that QueryBuilder Offset throws ArgumentException when a negative offset value is provided.
    /// Verifies that OFFSET cannot be negative.
    /// </summary>
    [Fact]
    public void QueryBuilder_Offset_ThrowsArgumentException_WhenOffsetIsNegative()
    {
        // Act & Assert
        new QueryBuilder("Users").Invoking(q => q.Offset(-1))
            .Should().Throw<ArgumentException>()
            .WithParameterName("offset")
            .WithMessage("Offset cannot be negative (Parameter 'offset')");
    }
}
