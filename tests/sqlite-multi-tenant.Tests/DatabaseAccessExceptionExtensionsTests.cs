using System;
using FluentAssertions;
using SqliteMultiTenant.Exceptions;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class DatabaseAccessExceptionExtensionsTests
{
    private static DatabaseAccessException CreateException(
        string message = "default message",
        string? databaseId = "db-001",
        string? operationType = "Query",
        Exception? inner = null)
    {
        // DatabaseAccessException has a constructor:
        // DatabaseAccessException(string message, string databaseId, string operationType, Exception? innerException = null)
        return new DatabaseAccessException(message, databaseId, operationType, inner);
    }

    [Fact]
    public void WithMessage_ShouldReplaceMessageAndPreserveOtherProperties()
    {
        var original = CreateException(message: "original", databaseId: "db-123", operationType: "Query");
        var updated = original.WithMessage("new message");

        updated.Message.Should().Be("new message");
        updated.DatabaseId.Should().Be(original.DatabaseId);
        updated.OperationType.Should().Be(original.OperationType);
        updated.InnerException.Should().BeNull();
    }

    [Fact]
    public void WithContext_ShouldAppendContextInfoToMessage()
    {
        var original = CreateException(message: "error occurred", databaseId: "db-456", operationType: "Connection");
        var withContext = original.WithContext("UserId=42");

        withContext.Message.Should().Be("error occurred | Context: UserId=42");
        withContext.DatabaseId.Should().Be(original.DatabaseId);
        withContext.OperationType.Should().Be(original.OperationType);
    }

    [Fact]
    public void IsConnectionFailure_ShouldReturnTrueOnlyWhenOperationTypeIsConnection()
    {
        var connEx = CreateException(operationType: "Connection");
        var queryEx = CreateException(operationType: "Query");

        connEx.IsConnectionFailure().Should().BeTrue();
        queryEx.IsConnectionFailure().Should().BeFalse();
    }

    [Fact]
    public void IsQueryFailure_ShouldReturnTrueOnlyWhenOperationTypeIsQuery()
    {
        var queryEx = CreateException(operationType: "Query");
        var transEx = CreateException(operationType: "Transaction");

        queryEx.IsQueryFailure().Should().BeTrue();
        transEx.IsQueryFailure().Should().BeFalse();
    }

    [Fact]
    public void IsTransactionFailure_ShouldReturnTrueOnlyWhenOperationTypeIsTransaction()
    {
        var transEx = CreateException(operationType: "Transaction");
        var connEx = CreateException(operationType: "Connection");

        transEx.IsTransactionFailure().Should().BeTrue();
        connEx.IsTransactionFailure().Should().BeFalse();
    }

    [Fact]
    public void ToDetailedString_ShouldIncludeAllRelevantInformation()
    {
        var inner = new InvalidOperationException("inner error");
        var ex = CreateException(message: "something went wrong", databaseId: "db-789", operationType: "Transaction", inner: inner);

        var detailed = ex.ToDetailedString();

        detailed.Should().Contain("DatabaseAccessException: something went wrong");
        detailed.Should().Contain("\nDatabase: db-789");
        detailed.Should().Contain("\nOperation: Transaction");
        detailed.Should().Contain("\nInnerException: InvalidOperationException: inner error");
    }

    [Fact]
    public void WithMessage_NullException_ShouldThrowArgumentNullException()
    {
        Action act = () => ((DatabaseAccessException)null!).WithMessage("new");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithContext_NullException_ShouldThrowArgumentNullException()
    {
        Action act = () => ((DatabaseAccessException)null!).WithContext("info");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithMessage_NullNewMessage_ShouldThrowArgumentNullException()
    {
        var ex = CreateException();
        Action act = () => ex.WithMessage(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithContext_NullContextInfo_ShouldThrowArgumentNullException()
    {
        var ex = CreateException();
        Action act = () => ex.WithContext(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsConnectionFailure_NullException_ShouldThrowArgumentNullException()
    {
        Action act = () => ((DatabaseAccessException)null!).IsConnectionFailure();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsQueryFailure_NullException_ShouldThrowArgumentNullException()
    {
        Action act = () => ((DatabaseAccessException)null!).IsQueryFailure();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsTransactionFailure_NullException_ShouldThrowArgumentNullException()
    {
        Action act = () => ((DatabaseAccessException)null!).IsTransactionFailure();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDetailedString_NullException_ShouldThrowArgumentNullException()
    {
        Action act = () => ((DatabaseAccessException)null!).ToDetailedString();
        act.Should().Throw<ArgumentNullException>();
    }
}
