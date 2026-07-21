using SqliteMultiTenant.DataOperations;
using FluentAssertions;
using Xunit;

namespace SqliteMultiTenant.Tests.DataOperations
{
    public class QueryBuilderTests
    {
        [Fact]
        public void Select_Build_DefaultsToSelectAll()
        {
            var qb = new QueryBuilder("Users");
            
            var sql = qb.Build();
            
            sql.Should().Be("SELECT * FROM [Users]");
        }

        [Fact]
        public void Select_SpecificColumns_BuildsCorrectQuery()
        {
            var qb = new QueryBuilder("Users");
            qb.Select("Name", "Email");
            
            var sql = qb.Build();
            
            sql.Should().Be("SELECT [Name], [Email] FROM [Users]");
        }

        [Fact]
        public void Where_And_Or_Composition_Order()
        {
            var qb = new QueryBuilder("Users");
            qb.Where("A=1").And("B=2").Or("C=3");
            
            var sql = qb.Build();
            
            // Expected: SELECT * FROM [Users] WHERE ((A=1) AND (B=2)) OR (C=3)
            sql.Should().Be("SELECT * FROM [Users] WHERE ((A=1) AND (B=2)) OR (C=3)");
        }

        [Fact]
        public void OrderBy_DefaultDirectionAndDesc()
        {
            var qb = new QueryBuilder("Users");
            qb.OrderBy("Name");
            qb.OrderBy("Age", "DESC");
            
            var sql = qb.Build();
            
            sql.Should().Be("SELECT * FROM [Users] ORDER BY [Name] ASC, [Age] DESC");
        }

        [Fact]
        public void Limit_Offset_AppearInOutput()
        {
            var qb = new QueryBuilder("Users");
            qb.Limit(10).Offset(5);
            
            var sql = qb.Build();
            
            sql.Should().EndWith(" LIMIT 10 OFFSET 5");
        }

        [Fact]
        public void Reset_ReturnsBuilderToCleanState()
        {
            var qb = new QueryBuilder("Users");
            qb.Select("Name").Where("Id=1").Limit(1);
            qb.Build().Should().NotBe("SELECT * FROM [Users]");
            
            qb.Reset();
            
            var sql = qb.Build();
            sql.Should().Be("SELECT * FROM [Users]");
        }

        [Fact]
        public void ToString_Equals_Build()
        {
            var qb = new QueryBuilder("Users");
            qb.Select("Name").Where("Id=1");
            
            qb.ToString().Should().Be(qb.Build());
        }
    }
}
