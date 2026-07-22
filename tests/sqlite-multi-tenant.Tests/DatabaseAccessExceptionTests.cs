using System;
using FluentAssertions;
using SqliteMultiTenant.Exceptions;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class DatabaseAccessExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessageAndLeaveOptionalPropertiesNull()
    {
        // Arrange
        var message = "Simple error";

        // Act
        var ex = new DatabaseAccessException(message);

        // Assert
        ex.Message.Should().Be(message);
        ex.DatabaseId.Should().BeNull();
        ex.OperationType.Should().BeNull();
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetAllProperties()
    {
        // Arrange
        var message = "Error with inner";
        var inner = new InvalidOperationException("inner");

        // Act
        var ex = new DatabaseAccessException(message, inner);

        // Assert
        ex.Message.Should().Be(message);
        ex.InnerException.Should().BeSameAs(inner);
        ex.DatabaseId.Should().BeNull();
        ex.OperationType.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldPopulateAllProperties()
    {
        // Arrange
        var message = "Full error";
        var dbId = "db-123";
        var opType = "Query";
        var inner = new ArgumentException("bad arg");

        // Act
        var ex = new DatabaseAccessException(message, dbId, opType, inner);

        // Assert
        ex.Message.Should().Be(message);
        ex.DatabaseId.Should().Be(dbId);
        ex.OperationType.Should().Be(opType);
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ConnectionFailed_ShouldCreateExceptionWithCorrectMessageAndProperties()
    {
        // Arrange
        var dbId = "db-conn";
        var inner = new TimeoutException("timeout");

        // Act
        var ex = DatabaseAccessException.ConnectionFailed(dbId, inner);

        // Assert
        ex.Message.Should().Be($"Failed to connect to database '{dbId}'");
        ex.DatabaseId.Should().Be(dbId);
        ex.OperationType.Should().Be("Connection");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void QueryFailed_ShouldIncludeQueryInMessage()
    {
        // Arrange
        var dbId = "db-query";
        var query = "SELECT * FROM Users";
        var inner = new Exception("generic");

        // Act
        var ex = DatabaseAccessException.QueryFailed(dbId, query, inner);

        // Assert
        ex.Message.Should().Be($"Query execution failed on database '{dbId}': {query}");
        ex.DatabaseId.Should().Be(dbId);
        ex.OperationType.Should().Be("Query");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void TransactionFailed_ShouldSetCorrectOperationType()
    {
        // Arrange
        var dbId = "db-tx";
        var inner = new InvalidOperationException("tx error");

        // Act
        var ex = DatabaseAccessException.TransactionFailed(dbId, inner);

        // Assert
        ex.Message.Should().Be($"Transaction failed on database '{dbId}'");
        ex.DatabaseId.Should().Be(dbId);
        ex.OperationType.Should().Be("Transaction");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ReadOnlyViolation_ShouldReturnExceptionWithNullInnerException()
    {
        // Arrange
        var dbId = "db-ro";
        var operation = "INSERT";

        // Act
        var ex = DatabaseAccessException.ReadOnlyViolation(dbId, operation);

        // Assert
        ex.Message.Should().Be(
            $"Database '{dbId}' is in read-only mode. Write operation '{operation}' is not allowed.");
        ex.DatabaseId.Should().Be(dbId);
        ex.OperationType.Should().Be("WriteOperation");
        ex.InnerException.Should().BeNull();
    }
}
