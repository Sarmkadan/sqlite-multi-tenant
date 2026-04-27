using System.Collections.Generic;
using FluentAssertions;
using SqliteMultiTenant.Validation;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public sealed class ConnectionStringValidatorTests
    {
        private readonly ConnectionStringValidator _validator;

        public ConnectionStringValidatorTests()
        {
            _validator = new ConnectionStringValidator();
        }

        [Fact]
        public void ValidateSqliteConnectionString_ShouldReturnNoErrors_WhenPathContainsSpaces()
        {
            // Arrange
            var connectionString = "Data Source=My Database With Spaces.db;Version=3;";

            // Act
            var errors = _validator.ValidateSqliteConnectionString(connectionString);

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateSqliteConnectionString_ShouldReturnNoErrors_WhenPathContainsUnicode()
        {
            // Arrange
            var connectionString = "Data Source=データベース.db;Version=3;";

            // Act
            var errors = _validator.ValidateSqliteConnectionString(connectionString);

            // Assert
            errors.Should().BeEmpty();
        }
    }
}