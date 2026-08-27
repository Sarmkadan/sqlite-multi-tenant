using SqliteMultiTenant.DataOperations;
using FluentAssertions;
using Xunit;
using System;
using System.Collections.Generic;

namespace SqliteMultiTenant.Tests.DataOperations
{
    /// <summary>
    /// Contains unit tests for the InsertBuilder and UpdateBuilder classes.
    /// </summary>
    public class InsertUpdateBuilderTests
    {
        #region InsertBuilder Tests

        /// <summary>
        /// Tests that inserting a single value produces the correct SQL query and parameter dictionary.
        /// </summary>
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

        /// <summary>
        /// Tests that inserting multiple values produces the correct SQL query and parameter dictionary.
        /// </summary>
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

        /// <summary>
        /// Tests that inserting values via multiple Value calls produces the correct SQL query and parameter dictionary.
        /// </summary>
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

        /// <summary>
        /// Tests that inserting a null value is stored as DBNull in the parameter dictionary.
        /// </summary>
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

        /// <summary>
        /// Tests that building an insert with no values throws an InvalidOperationException.
        /// </summary>
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

        /// <summary>
        /// Tests that inserting values with special column names (spaces, hyphens, underscores) are properly quoted in the SQL query.
        /// </summary>
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

        /// <summary>
        /// Tests that creating an InsertBuilder with an empty table name throws an ArgumentException.
        /// </summary>
        [Fact]
        public void InsertBuilder_EmptyTableName_ThrowsArgumentException()
        {
            // Arrange & Act
            Action act = () => new InsertBuilder("");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty (Parameter 'tableName')");
        }

        /// <summary>
        /// Tests that creating an InsertBuilder with a null table name throws an ArgumentException.
        /// </summary>
        [Fact]
        public void InsertBuilder_NullTableName_ThrowsArgumentException()
        {
            // Arrange & Act
            Action act = () => new InsertBuilder(null);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty (Parameter 'tableName')");
        }

        /// <summary>
        /// Tests that creating an InsertBuilder with a whitespace-only table name throws an ArgumentException.
        /// </summary>
        [Fact]
        public void InsertBuilder_WhitespaceTableName_ThrowsArgumentException()
        {
            // Arrange & Act
            Action act = () => new InsertBuilder("   ");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty (Parameter 'tableName')");
        }

        /// <summary>
        /// Tests that inserting a value with an empty column name throws an ArgumentException.
        /// </summary>
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

        /// <summary>
        /// Tests that inserting a value with a null column name throws an ArgumentException.
        /// </summary>
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

        /// <summary>
        /// Tests that inserting a value with a whitespace-only column name throws an ArgumentException.
        /// </summary>
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

        /// <summary>
        /// Tests that updating a single set value produces the correct SQL query and parameter dictionary.
        /// </summary>
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

        /// <summary>
        /// Tests that updating multiple set values produces the correct SQL query and parameter dictionary.
        /// </summary>
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

        /// <summary>
        /// Tests that updating a set value with null is stored as DBNull in the parameter dictionary.
        /// </summary>
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

        /// <summary>
        /// Tests that building an update with no set values throws an InvalidOperationException.
        /// </summary>
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

        /// <summary>
        /// Tests that building an update with no WHERE clause throws an InvalidOperationException for safety.
        /// </summary>
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

        /// <summary>
        /// Tests that updating with an empty WHERE clause throws an ArgumentException.
        /// </summary>
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

        /// <summary>
        /// Tests that updating with a null WHERE clause throws an ArgumentException.
        /// </summary>
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

        /// <summary>
        /// Tests that updating with a whitespace-only WHERE clause throws an ArgumentException.
        /// </summary>
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

        /// <summary>
        /// Tests that updating values with special column names (spaces, hyphens) are properly quoted in the SQL query.
        /// </summary>
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

        /// <summary>
        /// Tests that creating an UpdateBuilder with an empty table name throws an ArgumentException.
        /// </summary>
        [Fact]
        public void UpdateBuilder_EmptyTableName_ThrowsArgumentException()
        {
            // Arrange & Act
            Action act = () => new UpdateBuilder("");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty (Parameter 'tableName')");
        }

        /// <summary>
        /// Tests that creating an UpdateBuilder with a null table name throws an ArgumentException.
        /// </summary>
        [Fact]
        public void UpdateBuilder_NullTableName_ThrowsArgumentException()
        {
            // Arrange & Act
            Action act = () => new UpdateBuilder(null);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty (Parameter 'tableName')");
        }

        /// <summary>
        /// Tests that creating an UpdateBuilder with a whitespace-only table name throws an ArgumentException.
        /// </summary>
        [Fact]
        public void UpdateBuilder_WhitespaceTableName_ThrowsArgumentException()
        {
            // Arrange & Act
            Action act = () => new UpdateBuilder("   ");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Table name cannot be empty (Parameter 'tableName')");
        }

        /// <summary>
        /// Tests that updating a set value with an empty column name throws an ArgumentException.
        /// </summary>
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

        /// <summary>
        /// Tests that updating a set value with a null column name throws an ArgumentException.
        /// </summary>
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

        /// <summary>
        /// Tests that updating a set value with a whitespace-only column name throws an ArgumentException.
        /// </summary>
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

        /// <summary>
        /// Tests that updating with a complex WHERE clause produces the correct SQL query and parameter dictionary.
        /// </summary>
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