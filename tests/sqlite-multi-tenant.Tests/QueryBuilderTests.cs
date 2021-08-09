// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using SqliteMultiTenant.DataOperations;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class QueryBuilderTests
{
    // Existing tests - modified to use QueryBuilder(tableName) constructor
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
    
    [Fact]
    public void QueryBuilder_Instance_ShouldBeCreatedSuccessfully()
    {
        // Act
        var builder = new QueryBuilder("TestTable");
        
        // Assert
        builder.Should().NotBeNull();
    }

    // New tests for QueryBuilder
    [Fact]
    public void QueryBuilder_Constructor_ThrowsArgumentException_WhenTableNameIsEmpty()
    {
        // Act
        Action act = () => new QueryBuilder("");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Table name cannot be empty (Parameter 'tableName')");
    }

    [Fact]
    public void Select_SingleColumn_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Users").Select("Id").Build();
        query.Should().Be("SELECT [Id] FROM [Users]");
    }

    [Fact]
    public void Select_MultipleColumns_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Users").Select("Id", "Name").Build();
        query.Should().Be("SELECT [Id], [Name] FROM [Users]");
    }

    [Fact]
    public void Select_NoColumns_DefaultsToSelectAll()
    {
        var query = new QueryBuilder("Users").Select().Build();
        query.Should().Be("SELECT * FROM [Users]");
    }

    [Fact]
    public void Where_SingleCondition_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Users").Where("Age > 18").Build();
        query.Should().Be("SELECT * FROM [Users] WHERE Age > 18");
    }

    [Fact]
    public void Where_MultipleConditionsWithAnd_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Users")
            .Where("Age > @age", ("age", 18))
            .And("Status = @status", ("status", "active"))
            .Build();
        query.Should().Be("SELECT * FROM [Users] WHERE (Age > @age) AND (Status = @status)");
    }

    [Fact]
    public void Where_MultipleConditionsWithOr_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Users")
            .Where("Age > @age", ("age", 60))
            .Or("Status = @status", ("status", "retired"))
            .Build();
        query.Should().Be("SELECT * FROM [Users] WHERE (Age > @age) OR (Status = @status)");
    }

    [Fact]
    public void Where_WithParameters_AppliesParametersCorrectly()
    {
        // Test implicitly via ApplyParameters test later
        var builder = new QueryBuilder("Products").Where("Price > @price", ("price", 100));
        builder.Build(); // To set internal parameters list
        using var cmd = new SQLiteCommand();
        builder.ApplyParameters(cmd);
        cmd.Parameters.Should().Contain(p => p.ParameterName == "@price" && p.Value.Equals(100));
    }

    [Fact]
    public void InnerJoin_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Orders")
            .Select("Orders.Id", "Customers.Name")
            .InnerJoin("Customers", "Orders.CustomerId = Customers.Id")
            .Build();
        query.Should().Be("SELECT [Orders.Id], [Customers.Name] FROM [Orders] INNER JOIN Customers ON Orders.CustomerId = Customers.Id");
    }

    [Fact]
    public void LeftJoin_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Products")
            .Select("Products.Name", "Categories.CategoryName")
            .LeftJoin("Categories", "Products.CategoryId = Categories.Id")
            .Build();
        query.Should().Be("SELECT [Products.Name], [Categories.CategoryName] FROM [Products] LEFT JOIN Categories ON Products.CategoryId = Categories.Id");
    }

    [Fact]
    public void OrderBy_SingleColumn_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Users").OrderBy("Name").Build();
        query.Should().Be("SELECT * FROM [Users] ORDER BY [Name] ASC");
    }

    [Fact]
    public void OrderBy_MultipleColumns_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Users")
            .OrderBy("Name")
            .OrderBy("Age", "DESC")
            .Build();
        query.Should().Be("SELECT * FROM [Users] ORDER BY [Name] ASC, [Age] DESC");
    }

    [Fact]
    public void Limit_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Products").Limit(10).Build();
        query.Should().Be("SELECT * FROM [Products] LIMIT 10");
    }

    [Fact]
    public void Offset_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Products").Offset(5).Build();
        query.Should().Be("SELECT * FROM [Products] OFFSET 5");
    }

    [Fact]
    public void LimitAndOffset_BuildsCorrectQuery()
    {
        var query = new QueryBuilder("Products").Limit(10).Offset(5).Build();
        query.Should().Be("SELECT * FROM [Products] LIMIT 10 OFFSET 5");
    }

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
        command.Parameters.Should().Contain(p => p.ParameterName == "@id" && (int)p.Value == 123);
        command.Parameters.Should().Contain(p => p.ParameterName == "@name" && (string)p.Value == "TestUser");
    }

    // New tests for InsertBuilder
    [Fact]
    public void InsertBuilder_Constructor_ThrowsArgumentException_WhenTableNameIsEmpty()
    {
        // Act
        Action act = () => new InsertBuilder("");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Table name cannot be empty (Parameter 'tableName')");
    }

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

    // New tests for UpdateBuilder
    [Fact]
    public void UpdateBuilder_Constructor_ThrowsArgumentException_WhenTableNameIsEmpty()
    {
        // Act
        Action act = () => new UpdateBuilder("");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Table name cannot be empty (Parameter 'tableName')");
    }

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

    [Fact]
    public void QueryBuilder_OrderBy_ThrowsArgumentException_WhenInvalidDirection()
    {
        // Act
        Action act = () => new QueryBuilder("Users").OrderBy("Name", "INVALID");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Direction must be ASC or DESC (Parameter 'direction')");
    }

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
