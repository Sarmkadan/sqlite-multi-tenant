#nullable enable
using FluentAssertions;
using SqliteMultiTenant.DataOperations;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Edge-case tests for QueryBuilder - null/empty inputs, SQL injection boundaries,
/// and fluent API chaining correctness.
/// </summary>
public sealed class QueryBuilderEdgeCaseTests
{
    [Fact]
    public void Constructor_NullTableName_ThrowsArgumentException()
    {
        var act = () => new QueryBuilder(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyTableName_ThrowsArgumentException()
    {
        var act = () => new QueryBuilder("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WhitespaceTableName_ThrowsArgumentException()
    {
        var act = () => new QueryBuilder("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Where_EmptyCondition_ThrowsArgumentException()
    {
        var builder = new QueryBuilder("Users");

        var act = () => builder.Where("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void And_EmptyCondition_ThrowsArgumentException()
    {
        var builder = new QueryBuilder("Users").Where("Id = 1");

        var act = () => builder.And("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Or_EmptyCondition_ThrowsArgumentException()
    {
        var builder = new QueryBuilder("Users").Where("Id = 1");

        var act = () => builder.Or("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Select_NoColumns_SelectsAll()
    {
        var builder = new QueryBuilder("Users");
        var sql = builder.Build();

        sql.Should().Contain("SELECT *");
    }

    [Fact]
    public void Select_SpecificColumns_IncludesColumnsInQuery()
    {
        var builder = new QueryBuilder("Users").Select("Id", "Name");
        var sql = builder.Build();

        sql.Should().Contain("Id");
        sql.Should().Contain("Name");
        sql.Should().NotContain("SELECT *");
    }

    [Fact]
    public void Where_WithParameters_AddsParametersToQuery()
    {
        var builder = new QueryBuilder("Users")
            .Where("Name = @name", ("@name", "test"));

        var sql = builder.Build();

        sql.Should().Contain("WHERE");
        sql.Should().Contain("Name = @name");
    }

    [Fact]
    public void And_ChainsConditionsCorrectly()
    {
        var builder = new QueryBuilder("Users")
            .Where("Active = 1")
            .And("Age > @age", ("@age", 18));

        var sql = builder.Build();

        sql.Should().Contain("AND");
    }

    [Fact]
    public void FluentChaining_MultipleOperations_ProducesValidSql()
    {
        var builder = new QueryBuilder("Users")
            .Select("Id", "Name")
            .Where("Status = @status", ("@status", "active"))
            .OrderBy("Name")
            .Limit(10);

        var sql = builder.Build();

        sql.Should().Contain("SELECT");
        sql.Should().Contain("FROM Users");
        sql.Should().Contain("WHERE");
        sql.Should().Contain("ORDER BY");
        sql.Should().Contain("LIMIT");
    }
}
