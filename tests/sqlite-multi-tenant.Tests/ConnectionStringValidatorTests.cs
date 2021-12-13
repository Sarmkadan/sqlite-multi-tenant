using System.Collections.Generic;
using FluentAssertions;
using SqliteMultiTenant.Validation;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    /// <summary>
    /// Tests for the ConnectionStringValidator class.
    /// </summary>
    public sealed class ConnectionStringValidatorTests
    {
        private readonly ConnectionStringValidator _validator;

        /// <summary>
        /// Initializes a new instance of the ConnectionStringValidatorTests class.
        /// </summary>
        public ConnectionStringValidatorTests()
        {
            _validator = new ConnectionStringValidator();
        }

        /// <summary>
        /// Verifies that a SQLite connection string with a path containing spaces is validated correctly.
        /// </summary>
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

        /// <summary>
        /// Verifies that a SQLite connection string with a path containing Unicode characters is validated correctly.
        /// </summary>
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
