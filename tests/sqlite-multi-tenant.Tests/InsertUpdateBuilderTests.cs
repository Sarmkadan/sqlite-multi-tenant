using SqliteMultiTenant.DataOperations;
using FluentAssertions;
using Xunit;
using System;
using System.Collections.Generic;

namespace SqliteMultiTenant.Tests.DataOperations
{
    public class InsertUpdateBuilderTests
    {
        #region InsertBuilder Tests

        [Fact]
        public void InsertBuilder_SingleValue_BuildsCorrectQueryAndParameters()
        {
            // Arrange
            var builder = new InsertBuilder("Users");
            builder.Value("Name", "John Doe");

            // Act
            var (query, parameters) = builder.Build();

            // Assert
            query.Should().Be("INSERT INTO [Users] ([Name]) VALUES (@Name)");
            parameters.Should().BeEquivalentTo(new Dictionary<string, object> { { "Name", "John Doe" } });
        }

        [Fact]
        public void InsertBuilder_MultipleValues_BuildsCorrectQueryAndParameters()
        {
            // Arrange
            var builder = new InsertBuilder("Users");
            builder.Value("Name", "Jane Smith")
                  .Value("Email", "jane@example.com")
                  .Value("Age", 30);

            // Act
            var (query, parameters) = builder.Build();

            // Assert
            query.Should().Be("INSERT INTO [Users] ([Name], [Email], [Age]) VALUES (@Name, @Email, @Age)");
            parameters.Should().BeEquivalentTo(new Dictionary<string, object>
            {
                { "Name", "Jane Smith" },
                { "Email", "jane@example.com" },
                { "Age", 30 }
            });
        }

        [Fact]
        public void InsertBuilder_MultipleValueCalls_BuildsCorrectQueryAndParameters()
        {
            // Arrange
            var builder = new InsertBuilder("Products");
            builder.Value("Id", 1);
            builder.Value("Name", "Laptop");
            builder.Value("Price", 999.99m);

            // Act
            var (query, parameters) = builder.Build();

            // Assert
            query.Should().Be("INSERT INTO [Products] ([Id], [Name], [Price]) VALUES (@Id, @Name, @Price)");
            parameters.Should().BeEquivalentTo(new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", "Laptop" },
                { "Price", 999.99m }
            });
        }

        [Fact]
        public void InsertBuilder_ValueWithNull_StoresAsDBNull()
        {
            // Arrange
            var builder = new InsertBuilder("Users");
            builder.Value("Name", "John Doe")
                  .Value("Email", null);

            // Act
            var (query, parameters) = builder.Build();

            // Assert
            query.Should().Be("INSERT INTO [Users] ([Name], [Email]) VALUES (@Name, @Email)");
            parameters.Should().BeEquivalentTo(new Dictionary<string, object>
            {
                { "Name", "John Doe" },
                { "Email", DBNull.Value }
            });
        }

        [Fact]
        public void InsertBuilder_NoValues_ThrowsInvalidOperationException()
        {
            // Arrange
            var builder = new InsertBuilder("Users");

            // Act
            Action act = () => builder.Build();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("No values specified for insert");
        }

        [Fact]
        public void InsertBuilder_SpecialColumnNames_QuotesColumnsCorrectly()
        {
            // Arrange
            var builder = new InsertBuilder("UserData");
            builder.Value("First Name", "John")
                  .Value("Last-Name", "Doe")
                  .Value("Email_Address", "test@example.com");

            // Act
            var (query, parameters) = builder.Build();

            // Assert
            query.Should().Be("INSERT INTO [UserData] ([First Name], [Last-Name], [Email_Address]) VALUES (@First Name, @Last-Name, @Email_Address)");
            parameters.Should().BeEquivalentTo(new Dictionary<string, object>
            {
                { "First Name", "John" },
                { "Last-Name", "Doe" },
                { "Email_Address", "test@example.com" }
            });
        }

        [Fact]
        public void InsertBuilder_EmptyTableName_ThrowsArgumentException()
        {
            // Arrange & Act
            Action act = () => new InsertBuilder("");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty (Parameter 'tableName')");
        }

        [Fact]
        public void InsertBuilder_NullTableName_ThrowsArgumentException()
        {
            // Arrange & Act
            Action act = () => new InsertBuilder(null);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty (Parameter 'tableName')");
        }

        [Fact]
        public void InsertBuilder_WhitespaceTableName_ThrowsArgumentException()
        {
            // Arrange & Act
            Action act = () => new InsertBuilder("   ");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty (Parameter 'tableName')");
        }

        [Fact]
        public void InsertBuilder_EmptyColumnName_ThrowsArgumentException()
        {
            // Arrange
            var builder = new InsertBuilder("Users");

            // Act
            Action act = () => builder.Value("", "value");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Column cannot be empty (Parameter 'column')");
        }

        [Fact]
        public void InsertBuilder_NullColumnName_ThrowsArgumentException()
        {
            // Arrange
            var builder = new InsertBuilder("Users");

            // Act
            Action act = () => builder.Value(null, "value");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Column cannot be empty (Parameter 'column')");
        }

        [Fact]
        public void InsertBuilder_WhitespaceColumnName_ThrowsArgumentException()
        {
            // Arrange
            var builder = new InsertBuilder("Users");

            // Act
            Action act = () => builder.Value("   ", "value");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Column cannot be empty (Parameter 'column')");
        }

        #endregion

        #region UpdateBuilder Tests

        [Fact]
        public void UpdateBuilder_SingleSetValue_BuildsCorrectQueryAndParameters()
        {
            // Arrange
            var builder = new UpdateBuilder("Users");
            builder.Set("Name", "John Doe")
                  .Where("Id = @id");

            // Act
            var (query, parameters) = builder.Build();

            // Assert
            query.Should().Be("UPDATE [Users] SET [Name] = @Name WHERE Id = @id");
            parameters.Should().BeEquivalentTo(new Dictionary<string, object> { { "Name", "John Doe" } });
        }

        [Fact]
        public void UpdateBuilder_MultipleSetValues_BuildsCorrectQueryAndParameters()
        {
            // Arrange
            var builder = new UpdateBuilder("Users");
            builder.Set("Name", "Jane Smith")
                  .Set("Email", "jane@example.com")
                  .Set("Age", 30)
                  .Where("Id = 1");

            // Act
            var (query, parameters) = builder.Build();

            // Assert
            query.Should().Be("UPDATE [Users] SET [Name] = @Name, [Email] = @Email, [Age] = @Age WHERE Id = 1");
            parameters.Should().BeEquivalentTo(new Dictionary<string, object>
            {
                { "Name", "Jane Smith" },
                { "Email", "jane@example.com" },
                { "Age", 30 }
            });
        }

        [Fact]
        public void UpdateBuilder_SetWithNull_StoresAsDBNull()
        {
            // Arrange
            var builder = new UpdateBuilder("Users");
            builder.Set("Name", "John Doe")
                  .Set("Email", null)
                  .Where("Id = 1");

            // Act
            var (query, parameters) = builder.Build();

            // Assert
            query.Should().Be("UPDATE [Users] SET [Name] = @Name, [Email] = @Email WHERE Id = 1");
            parameters.Should().BeEquivalentTo(new Dictionary<string, object>
            {
                { "Name", "John Doe" },
                { "Email", DBNull.Value }
            });
        }

        [Fact]
        public void UpdateBuilder_NoSetValues_ThrowsInvalidOperationException()
        {
            // Arrange
            var builder = new UpdateBuilder("Users");
            builder.Where("Id = 1");

            // Act
            Action act = () => builder.Build();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("No values specified for update");
        }

        [Fact]
        public void UpdateBuilder_NoWhereClause_ThrowsInvalidOperationException()
        {
            // Arrange
            var builder = new UpdateBuilder("Users");
            builder.Set("Name", "John Doe");

            // Act
            Action act = () => builder.Build();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("WHERE condition is required for safety");
        }

        [Fact]
        public void UpdateBuilder_EmptyWhereClause_ThrowsArgumentException()
        {
            // Arrange
            var builder = new UpdateBuilder("Users");
            builder.Set("Name", "John Doe");

            // Act
            Action act = () => builder.Where("");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Condition cannot be empty (Parameter 'condition')");
        }

        [Fact]
        public void UpdateBuilder_NullWhereClause_ThrowsArgumentException()
        {
            // Arrange
            var builder = new UpdateBuilder("Users");
            builder.Set("Name", "John Doe");

            // Act
            Action act = () => builder.Where(null);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Condition cannot be empty (Parameter 'condition')");
        }

        [Fact]
        public void UpdateBuilder_WhitespaceWhereClause_ThrowsArgumentException()
        {
            // Arrange
            var builder = new UpdateBuilder("Users");
            builder.Set("Name", "John Doe");

            // Act
            Action act = () => builder.Where("   ");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Condition cannot be empty (Parameter 'condition')");
        }

        [Fact]
        public void UpdateBuilder_SpecialColumnNames_QuotesColumnsCorrectly()
        {
            // Arrange
            var builder = new UpdateBuilder("UserData");
            builder.Set("First Name", "John")
                  .Set("Last-Name", "Doe")
                  .Where("Id = 1");

            // Act
            var (query, parameters) = builder.Build();

            // Assert
            query.Should().Be("UPDATE [UserData] SET [First Name] = @First Name, [Last-Name] = @Last-Name WHERE Id = 1");
            parameters.Should().BeEquivalentTo(new Dictionary<string, object>
            {
                { "First Name", "John" },
                { "Last-Name", "Doe" }
            });
        }

        [Fact]
        public void UpdateBuilder_EmptyTableName_ThrowsArgumentException()
        {
            // Arrange & Act
            Action act = () => new UpdateBuilder("");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty (Parameter 'tableName')");
        }

        [Fact]
        public void UpdateBuilder_NullTableName_ThrowsArgumentException()
        {
            // Arrange & Act
            Action act = () => new UpdateBuilder(null);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty (Parameter 'tableName')");
        }

        [Fact]
        public void UpdateBuilder_WhitespaceTableName_ThrowsArgumentException()
        {
            // Arrange & Act
            Action act = () => new UpdateBuilder("   ");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty (Parameter 'tableName')");
        }

        [Fact]
        public void UpdateBuilder_EmptyColumnName_ThrowsArgumentException()
        {
            // Arrange
            var builder = new UpdateBuilder("Users");

            // Act
            Action act = () => builder.Set("", "value");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Column cannot be empty (Parameter 'column')");
        }

        [Fact]
        public void UpdateBuilder_NullColumnName_ThrowsArgumentException()
        {
            // Arrange
            var builder = new UpdateBuilder("Users");

            // Act
            Action act = () => builder.Set(null, "value");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Column cannot be empty (Parameter 'column')");
        }

        [Fact]
        public void UpdateBuilder_WhitespaceColumnName_ThrowsArgumentException()
        {
            // Arrange
            var builder = new UpdateBuilder("Users");

            // Act
            Action act = () => builder.Set("   ", "value");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Column cannot be empty (Parameter 'column')");
        }

        [Fact]
        public void UpdateBuilder_ComplexWhereClause_BuildsCorrectQuery()
        {
            // Arrange
            var builder = new UpdateBuilder("Orders");
            builder.Set("Status", "Shipped")
                  .Set("ShippedDate", DateTime.Now)
                  .Where("(Status = 'Pending' OR Status = 'Processing') AND CustomerId = @customerId");

            // Act
            var (query, parameters) = builder.Build();

            // Assert
            query.Should().StartWith("UPDATE [Orders] SET [Status] = @Status, [ShippedDate] = @ShippedDate WHERE ");
            query.Should().Contain("(Status = 'Pending' OR Status = 'Processing') AND CustomerId = @customerId");
            parameters.Should().ContainKey("Status").WhoseValue.Should().Be("Shipped");
            parameters.Should().ContainKey("ShippedDate");
        }

        #endregion
    }
}